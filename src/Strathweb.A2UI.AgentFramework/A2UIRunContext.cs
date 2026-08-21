using System.Text.Json.Nodes;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.Messages;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>What the renderer sent for the run currently executing on this call stack.</summary>
public static class A2UIRunContext
{
    private static readonly AsyncLocal<A2UIInboundResult?> Ambient = new();

    /// <summary>
    /// What the renderer sent this turn, or <see langword="null"/> when there is no run in progress.
    /// </summary>
    public static A2UIInboundResult? Current => Ambient.Value;

    /// <summary>The actions the user performed this turn.</summary>
    public static IReadOnlyList<ActionMessage> Actions => Current?.Actions ?? [];

    /// <summary>What the renderer says it can render, when it said anything this turn.</summary>
    public static A2UIRendererCapabilities? RendererCapabilities => Current?.RendererCapabilities;

    /// <summary>The data model of a surface this session created, as the renderer last reported it.</summary>
    /// <param name="surfaceId">The surface's identifier.</param>
    /// <returns>
    /// The data model, or <see langword="null"/> unless the surface was created with
    /// <c>sendDataModel</c>.
    /// </returns>
    public static JsonNode? GetSurfaceData(string surfaceId)
    {
        ArgumentNullException.ThrowIfNull(surfaceId);

        return Current?.SurfaceData.TryGetValue(surfaceId, out var data) == true ? data : null;
    }

    internal static Scope BeginScope(A2UIInboundResult inbound) => new(inbound);

    internal readonly struct Scope : IDisposable
    {
        private readonly A2UIInboundResult? previous;

        internal Scope(A2UIInboundResult inbound)
        {
            previous = Ambient.Value;
            Ambient.Value = inbound;
        }

        public void Dispose() => Ambient.Value = previous;
    }
}
