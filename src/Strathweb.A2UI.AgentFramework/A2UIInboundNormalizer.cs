using System.Text;
using System.Text.Json.Nodes;
using A2A;
using Microsoft.Extensions.AI;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.Messages;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>Turns the A2UI parts a renderer sends into something an agent and a model can both use.</summary>
public sealed class A2UIInboundNormalizer
{
    private readonly A2UIVersionProfile profile;

    /// <summary>Creates a normalizer.</summary>
    /// <param name="profile">The version whose metadata keys to read. Defaults to v0.9.1.</param>
    public A2UIInboundNormalizer(A2UIVersionProfile? profile = null)
    {
        this.profile = profile ?? A2UIVersionProfile.Default;
    }

    /// <summary>Reads whatever A2UI a turn's messages carry.</summary>
    /// <param name="messages">The inbound messages.</param>
    /// <param name="knownSurfaces">
    /// The surfaces this session created. Data models for anything else are dropped: a surface's data
    /// belongs to the agent that created it.
    /// </param>
    /// <returns>The normalized messages, and what was found in them.</returns>
    public A2UIInboundResult Normalize(
        IEnumerable<ChatMessage> messages,
        A2UISurfaceRegistry? knownSurfaces = null)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var normalized = new List<ChatMessage>();
        var actions = new List<ActionMessage>();
        var errors = new List<ErrorMessage>();
        var surfaceData = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        A2UIRendererCapabilities? capabilities = null;

        foreach (var message in messages)
        {
            capabilities ??= ReadCapabilities(message);
            ReadSurfaceData(message, knownSurfaces, surfaceData);
            normalized.Add(NormalizeMessage(message, actions, errors));
        }

        return new A2UIInboundResult(normalized, actions, errors, capabilities, surfaceData);
    }

    /// <summary>Describes an action in the plainest sentence that still carries every value the user chose.</summary>
    /// <param name="action">The action.</param>
    /// <returns>Text for the model to read.</returns>
    public static string Describe(ActionMessage action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var text = new StringBuilder()
            .Append("The user performed the \"")
            .Append(action.Name)
            .Append("\" action on surface ")
            .Append(action.SurfaceId);

        if (action.Context.Count == 0)
        {
            return text.Append('.').ToString();
        }

        text.Append(", with ");

        var first = true;
        foreach (var pair in action.Context)
        {
            if (!first)
            {
                text.Append(", ");
            }

            first = false;
            text.Append(pair.Key).Append('=').Append(Format(pair.Value));
        }

        return text.Append('.').ToString();
    }

    private static ChatMessage NormalizeMessage(
        ChatMessage message,
        List<ActionMessage> actions,
        List<ErrorMessage> errors)
    {
        List<AIContent>? replacement = null;

        for (var i = 0; i < message.Contents.Count; i++)
        {
            if (message.Contents[i].RawRepresentation is not Part part || !A2UIParts.IsA2UI(part))
            {
                continue;
            }

            replacement ??= [.. message.Contents];
            var expanded = Expand(part, actions, errors);

            replacement.RemoveAt(replacement.IndexOf(message.Contents[i]));
            replacement.InsertRange(Math.Min(i, replacement.Count), expanded);
        }

        if (replacement is null)
        {
            return message;
        }

        return new ChatMessage(message.Role, replacement)
        {
            AuthorName = message.AuthorName,
            MessageId = message.MessageId,
            AdditionalProperties = message.AdditionalProperties,
            RawRepresentation = message.RawRepresentation,
        };
    }

    private static List<AIContent> Expand(Part part, List<ActionMessage> actions, List<ErrorMessage> errors)
    {
        var contents = new List<AIContent>();

        // A message list is not a transactional unit: report what cannot be read and use the rest.
        if (!A2UIParts.TryReadTolerant(part, out var messages, out var failures))
        {
            return contents;
        }

        foreach (var failure in failures)
        {
            contents.Add(new TextContent($"An A2UI message from the renderer could not be read: {failure}"));
        }

        foreach (var message in messages)
        {
            switch (message)
            {
                case ActionMessage action:
                    actions.Add(action);
                    contents.Add(new A2UIActionContent(action, part));
                    contents.Add(new TextContent(Describe(action)));
                    break;

                case ErrorMessage error:
                    errors.Add(error);
                    contents.Add(new TextContent(
                        $"The renderer could not display surface {error.SurfaceId}: {error.Message} " +
                        $"({error.Code})"));
                    break;
            }
        }

        return contents;
    }

    private A2UIRendererCapabilities? ReadCapabilities(ChatMessage message) =>
        message.AdditionalProperties is { } properties &&
        A2UIMetadata.TryReadCapabilities(ToMetadata(properties), profile, out var capabilities)
            ? capabilities
            : null;

    private void ReadSurfaceData(
        ChatMessage message,
        A2UISurfaceRegistry? knownSurfaces,
        Dictionary<string, JsonNode?> into)
    {
        if (message.AdditionalProperties is not { } properties ||
            !A2UIMetadata.TryReadDataModel(ToMetadata(properties), profile, out var dataModel))
        {
            return;
        }

        foreach (var pair in dataModel.Surfaces)
        {
            // The specification is explicit: a surface's data model goes only to the agent that
            // created it. Anything else on this connection belongs to somebody else.
            if (knownSurfaces is null || knownSurfaces.Contains(pair.Key))
            {
                into[pair.Key] = pair.Value;
            }
        }
    }

    /// <summary>
    /// Message metadata arrives as loosely typed properties; A2A fills them from JSON, so the values
    /// are elements or nodes depending on how the message was built.
    /// </summary>
    private static Dictionary<string, JsonNode?> ToMetadata(AdditionalPropertiesDictionary properties)
    {
        var metadata = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        foreach (var pair in properties)
        {
            metadata[pair.Key] = pair.Value switch
            {
                JsonNode node => node,
                System.Text.Json.JsonElement element => JsonNode.Parse(element.GetRawText()),
                _ => null,
            };
        }

        return metadata;
    }

    private static string Format(JsonNode? value) => value switch
    {
        null => "(empty)",
        JsonValue scalar when scalar.TryGetValue<string>(out var text) =>
            text.Length == 0 ? "(empty)" : "\"" + text + "\"",
        _ => value.ToJsonString(),
    };
}
