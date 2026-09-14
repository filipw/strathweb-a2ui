using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using A2A;
using Microsoft.Extensions.AI;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Surfaces;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>A2UI messages attached to an agent's response, on their way to a renderer.</summary>
/// <remarks>
/// <see cref="AIContent"/> is serialized polymorphically from a closed set of types, so this one can be
/// written and read only by options it was registered with; see
/// <see cref="A2UIAgentJson.AddA2UIContentType"/>. The framework's default options are read-only and
/// cannot learn it, which is why <see cref="A2UIAgent"/> keeps it out of the chat history the inner
/// agent stores in the session.
/// </remarks>
public sealed class A2UIContent : AIContent
{
    /// <summary>Wraps messages for transport.</summary>
    /// <param name="messages">The messages to send.</param>
    public A2UIContent(IReadOnlyList<A2UIMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        Messages = messages;
        SurfaceId = CreatedSurfaceId(messages);

        // Must be set here, not lazily. A2A's ToPart() returns RawRepresentation unchanged when it
        // already holds a Part, and maps nothing else usable; built later, the surface is dropped.
        var part = A2UIParts.Create(messages);
        RawRepresentation = part;
        Payload = part.Data!.Value;
    }

    /// <summary>Wraps a built surface for transport.</summary>
    /// <param name="surface">The surface to show.</param>
    public A2UIContent(A2UISurface surface)
        : this(MessagesOf(surface))
    {
        SurfaceId = surface.SurfaceId;
    }

    /// <summary>Recreates content from its serialized form.</summary>
    /// <param name="payload">The messages, as the JSON array an A2A data part carries.</param>
    /// <param name="surfaceId">The surface the messages build, when they came from one.</param>
    /// <exception cref="ArgumentException"><paramref name="payload"/> is not a JSON array.</exception>
    [JsonConstructor]
    public A2UIContent(JsonElement payload, string? surfaceId = null)
    {
        if (payload.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException("The payload must be the JSON array an A2UI data part carries.", nameof(payload));
        }

        Messages = A2UIJson.ListFromJsonNode(JsonNode.Parse(payload.GetRawText()));
        SurfaceId = surfaceId ?? CreatedSurfaceId(Messages);

        var part = A2UIParts.Create(Messages);
        RawRepresentation = part;
        Payload = part.Data!.Value;
    }

    /// <summary>The messages this content carries.</summary>
    [JsonIgnore]
    public IReadOnlyList<A2UIMessage> Messages { get; }

    /// <summary>The messages as the JSON array an A2A data part carries. This is what is serialized.</summary>
    public JsonElement Payload { get; }

    /// <summary>The surface these messages create, when one of them is a <c>createSurface</c>.</summary>
    public string? SurfaceId { get; }

    /// <summary>The A2A data part this content travels as.</summary>
    [JsonIgnore]
    public Part Part => (Part)RawRepresentation!;

    private static IReadOnlyList<A2UIMessage> MessagesOf(A2UISurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);
        return surface.Messages;
    }

    private static string? CreatedSurfaceId(IReadOnlyList<A2UIMessage> messages)
    {
        foreach (var message in messages)
        {
            if (message is CreateSurfaceMessage create)
            {
                return create.SurfaceId;
            }
        }

        return null;
    }
}
