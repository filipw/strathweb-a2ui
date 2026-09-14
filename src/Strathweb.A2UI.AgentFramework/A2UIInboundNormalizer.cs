using System.Text;
using System.Text.Json.Nodes;
using A2A;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.Messages;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>Turns the A2UI parts a renderer sends into something an agent and a model can both use.</summary>
public sealed class A2UIInboundNormalizer
{
    /// <summary>Every capabilities key any supported version uses; the ones not ours mark a version mismatch.</summary>
    private static readonly string[] KnownCapabilitiesKeys = ["a2uiClientCapabilities", "a2uiRendererCapabilities"];

    private readonly A2UIVersionProfile profile;
    private readonly A2UIInboundLimits limits;
    private readonly ILogger logger;

    /// <summary>Creates a normalizer.</summary>
    /// <param name="profile">The version whose metadata keys to read. Defaults to v0.9.1.</param>
    /// <param name="limits">Caps on what is read. Defaults to <see cref="A2UIInboundLimits"/>'s defaults.</param>
    /// <param name="logger">Where to report what was dropped or ignored. Defaults to nowhere.</param>
    public A2UIInboundNormalizer(
        A2UIVersionProfile? profile = null,
        A2UIInboundLimits? limits = null,
        ILogger? logger = null)
    {
        this.profile = profile ?? A2UIVersionProfile.Default;
        this.limits = limits ?? new A2UIInboundLimits();
        this.logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Reads whatever A2UI a turn's messages carry.</summary>
    /// <param name="messages">
    /// The inbound messages. Expected to be this turn's messages only, as the A2A host passes them;
    /// actions in older messages would be reported again.
    /// </param>
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
        var ignored = new List<string>();
        A2UIRendererCapabilities? capabilities = null;

        foreach (var message in messages)
        {
            var metadata = message.AdditionalProperties is { } properties ? ToMetadata(properties) : null;

            capabilities ??= ReadCapabilities(metadata);
            ReadSurfaceData(metadata, knownSurfaces, surfaceData, ignored);
            normalized.Add(NormalizeMessage(message, actions, errors));
        }

        return new A2UIInboundResult(normalized, actions, errors, capabilities, surfaceData, ignored);
    }

    /// <summary>Describes an action in the plainest sentence that still carries every value the user chose.</summary>
    /// <param name="action">The action.</param>
    /// <returns>Text for the model to read, with each value cut at the default length limit.</returns>
    public static string Describe(ActionMessage action) => Describe(action, new A2UIInboundLimits().MaxDescribedValueLength);

    /// <summary>Describes an action in the plainest sentence that still carries every value the user chose.</summary>
    /// <param name="action">The action.</param>
    /// <param name="maxValueLength">The most characters of any one value to include.</param>
    /// <returns>Text for the model to read.</returns>
    public static string Describe(ActionMessage action, int maxValueLength)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxValueLength);

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
            text.Append(pair.Key).Append('=').Append(Format(pair.Value, maxValueLength));
        }

        return text.Append('.').ToString();
    }

    /// <summary>
    /// Replaces each A2UI part with text for the model. The structured actions go to the run context
    /// rather than into the message: the inner agent stores the message in its chat history, and
    /// content the framework cannot serialize would break every session store.
    /// </summary>
    private ChatMessage NormalizeMessage(
        ChatMessage message,
        List<ActionMessage> actions,
        List<ErrorMessage> errors)
    {
        List<AIContent>? replacement = null;

        for (var i = 0; i < message.Contents.Count; i++)
        {
            var content = message.Contents[i];

            if (content.RawRepresentation is Part part && A2UIParts.IsA2UI(part))
            {
                // One forward pass keeps several parts in the order they arrived.
                replacement ??= [.. message.Contents.Take(i)];
                replacement.AddRange(Expand(part, actions, errors));
            }
            else
            {
                replacement?.Add(content);
            }
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

    private List<AIContent> Expand(Part part, List<ActionMessage> actions, List<ErrorMessage> errors)
    {
        var contents = new List<AIContent>();

        var bytes = part.Data is { } data ? Encoding.UTF8.GetByteCount(data.GetRawText()) : 0;
        if (bytes > limits.MaxPartBytes)
        {
            logger.InboundPartTooLarge(bytes, limits.MaxPartBytes);
            contents.Add(new TextContent(
                $"An A2UI message from the renderer was ignored: it was {bytes} bytes, above the limit of " +
                $"{limits.MaxPartBytes}."));
            return contents;
        }

        // A message list is not a transactional unit: report what cannot be read and use the rest.
        if (!A2UIParts.TryReadTolerant(part, out var messages, out var failures))
        {
            return contents;
        }

        foreach (var failure in failures)
        {
            logger.InboundMessageUnreadable(failure);
            contents.Add(new TextContent($"An A2UI message from the renderer could not be read: {failure}"));
        }

        foreach (var message in messages)
        {
            switch (message)
            {
                case ActionMessage action:
                    logger.ActionReceived(action.Name, action.SurfaceId);
                    actions.Add(action);
                    contents.Add(new TextContent(Describe(action, limits.MaxDescribedValueLength)));
                    break;

                case ErrorMessage error:
                    logger.RendererError(error.Code, error.SurfaceId, error.Message);
                    errors.Add(error);
                    contents.Add(new TextContent(
                        $"The renderer could not display surface {error.SurfaceId}: {error.Message} " +
                        $"({error.Code})"));
                    break;
            }
        }

        return contents;
    }

    private A2UIRendererCapabilities? ReadCapabilities(Dictionary<string, JsonNode?>? metadata)
    {
        if (metadata is null)
        {
            return null;
        }

        if (A2UIMetadata.TryReadCapabilities(metadata, profile, out var capabilities))
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.CapabilitiesRead(string.Join(", ", capabilities.SupportedCatalogIds));
            }

            return capabilities;
        }

        // Capabilities under another version's key are the one visible sign of version skew; the
        // surfaces themselves would just render blank.
        foreach (var key in KnownCapabilitiesKeys)
        {
            if (!string.Equals(key, profile.CapabilitiesMetadataKey, StringComparison.Ordinal) &&
                metadata.ContainsKey(key))
            {
                logger.CapabilitiesVersionMismatch(key, profile.CapabilitiesMetadataKey);
            }
        }

        return null;
    }

    private void ReadSurfaceData(
        Dictionary<string, JsonNode?>? metadata,
        A2UISurfaceRegistry? knownSurfaces,
        Dictionary<string, JsonNode?> into,
        List<string> ignored)
    {
        if (metadata is null || !metadata.TryGetValue(profile.DataModelMetadataKey, out var raw) || raw is null)
        {
            return;
        }

        var bytes = Encoding.UTF8.GetByteCount(raw.ToJsonString());
        if (bytes > limits.MaxDataModelBytes)
        {
            logger.DataModelTooLarge(bytes, limits.MaxDataModelBytes);
            return;
        }

        if (!A2UIMetadata.TryReadDataModel(metadata, profile, out var dataModel))
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
            else
            {
                logger.SurfaceDataIgnored(pair.Key);
                ignored.Add(pair.Key);
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

    private static string Format(JsonNode? value, int maxLength)
    {
        switch (value)
        {
            case null:
                return "(empty)";

            case JsonValue scalar when scalar.TryGetValue<string>(out var text):
                return text.Length == 0 ? "(empty)" : "\"" + Cut(text, maxLength) + "\"";

            default:
                return Cut(value.ToJsonString(), maxLength);
        }
    }

    private static string Cut(string text, int maxLength) =>
        text.Length <= maxLength ? text : string.Concat(text.AsSpan(0, maxLength), "…");
}
