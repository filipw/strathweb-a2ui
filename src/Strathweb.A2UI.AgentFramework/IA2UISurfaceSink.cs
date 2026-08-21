using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Surfaces;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>Where a tool sends a surface it wants shown.</summary>
public interface IA2UISurfaceSink
{
    /// <summary>Shows a surface.</summary>
    /// <param name="surface">The surface to show.</param>
    void Emit(A2UISurface surface);

    /// <summary>Sends raw messages, such as an update or a delete against a live surface.</summary>
    /// <param name="messages">The messages to send.</param>
    void Emit(IReadOnlyList<A2UIMessage> messages);
}
