using Microsoft.Agents.AI;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>Adds A2UI to an agent.</summary>
public static class A2UIAgentExtensions
{
    /// <summary>Wraps an agent so that surfaces its tools emit reach the renderer.</summary>
    /// <param name="agent">The agent to wrap.</param>
    /// <param name="configure">Adjusts the defaults.</param>
    /// <returns>The wrapped agent.</returns>
    /// <example>
    /// <code>
    /// AIAgent agent = chatClient
    ///     .CreateAIAgent(instructions: "...", tools: [askSatisfaction])
    ///     .WithA2UI(o => o.SurfaceIdPrefix = "survey");
    /// </code>
    /// </example>
    public static A2UIAgent WithA2UI(this AIAgent agent, Action<A2UIAgentOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(agent);

        var options = new A2UIAgentOptions();
        configure?.Invoke(options);

        return new A2UIAgent(agent, options);
    }
}
