using Microsoft.Agents.AI;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>Reads and writes the A2UI state a session carries between turns.</summary>
public static class A2UIAgentSessionExtensions
{
    /// <summary>The surfaces this session has shown.</summary>
    /// <param name="session">The session.</param>
    /// <returns>The registry, empty when the session has shown nothing yet.</returns>
    public static A2UISurfaceRegistry GetA2UISurfaceRegistry(this AgentSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return session.StateBag.TryGetValue<A2UISurfaceRegistry>(
            A2UISurfaceRegistry.StateKey,
            out var registry,
            A2UIAgentJson.Options) && registry is not null
            ? registry
            : new A2UISurfaceRegistry();
    }

    /// <summary>Stores the surfaces this session has shown.</summary>
    /// <param name="session">The session.</param>
    /// <param name="registry">The registry.</param>
    public static void SetA2UISurfaceRegistry(this AgentSession session, A2UISurfaceRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(registry);

        session.StateBag.SetValue(A2UISurfaceRegistry.StateKey, registry, A2UIAgentJson.Options);
    }
}
