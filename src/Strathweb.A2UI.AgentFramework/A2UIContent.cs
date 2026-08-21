using Microsoft.Extensions.AI;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Surfaces;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>A2UI messages attached to an agent's response, on their way to a renderer.</summary>
public sealed class A2UIContent : AIContent
{
    /// <summary>Wraps messages for transport.</summary>
    /// <param name="messages">The messages to send.</param>
    public A2UIContent(IReadOnlyList<A2UIMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        Messages = messages;

        // Must be set here, not lazily. A2A's ToPart() returns RawRepresentation unchanged when it
        // already holds a Part, and maps nothing else usable; built later, the surface is dropped.
        RawRepresentation = A2UIParts.Create(messages);
    }

    /// <summary>Wraps a built surface for transport.</summary>
    /// <param name="surface">The surface to show.</param>
    public A2UIContent(A2UISurface surface)
        : this(MessagesOf(surface))
    {
        SurfaceId = surface.SurfaceId;
    }

    /// <summary>The messages this content carries.</summary>
    public IReadOnlyList<A2UIMessage> Messages { get; }

    /// <summary>The surface these messages build, when they came from one.</summary>
    public string? SurfaceId { get; }

    private static IReadOnlyList<A2UIMessage> MessagesOf(A2UISurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);
        return surface.Messages;
    }
}
