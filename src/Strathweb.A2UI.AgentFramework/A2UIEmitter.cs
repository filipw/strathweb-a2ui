using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Surfaces;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>The sink for the run currently executing on this call stack.</summary>
public static class A2UIEmitter
{
    private static readonly AsyncLocal<IA2UISurfaceSink?> Ambient = new();

    /// <summary>The sink for the current run.</summary>
    /// <exception cref="InvalidOperationException">
    /// There is no run in progress on this call stack: the agent was not wrapped with
    /// <c>WithA2UI()</c>, or the call escaped the run's async context.
    /// </exception>
    public static IA2UISurfaceSink Current =>
        Ambient.Value ?? throw new InvalidOperationException(
            "No A2UI run is in progress on this call stack. Emit from inside a run of an agent " +
            "wrapped with WithA2UI(), or pass an IA2UISurfaceSink explicitly.");

    /// <summary>Whether a run is in progress on this call stack.</summary>
    public static bool IsInRun => Ambient.Value is not null;

    /// <summary>Shows a surface. Shorthand for <c>A2UIEmitter.Current.Emit(surface)</c>.</summary>
    /// <param name="surface">The surface to show.</param>
    public static void Emit(A2UISurface surface) => Current.Emit(surface);

    /// <summary>Sends raw messages. Shorthand for <c>A2UIEmitter.Current.Emit(messages)</c>.</summary>
    /// <param name="messages">The messages to send.</param>
    public static void Emit(IReadOnlyList<A2UIMessage> messages) => Current.Emit(messages);

    /// <summary>Applies an update to a live surface.</summary>
    /// <param name="update">The update, built with <see cref="Surfaces.A2UISurfaceUpdate.For"/>.</param>
    public static void Emit(Surfaces.A2UISurfaceUpdate update) => Current.Emit(update);

    /// <summary>Writes a value into a live surface's data model.</summary>
    /// <param name="surfaceId">The surface to change.</param>
    /// <param name="path">A JSON Pointer, such as <c>/rating</c>.</param>
    /// <param name="value">The value.</param>
    public static void SetData(string surfaceId, string path, System.Text.Json.Nodes.JsonNode? value) =>
        Current.SetData(surfaceId, path, value);

    /// <summary>Takes a surface off the screen.</summary>
    /// <param name="surfaceId">The surface to remove.</param>
    public static void Delete(string surfaceId) => Current.Delete(surfaceId);

    internal static Scope BeginScope(IA2UISurfaceSink sink) => new(sink);

    internal readonly struct Scope : IDisposable
    {
        private readonly IA2UISurfaceSink? previous;

        internal Scope(IA2UISurfaceSink sink)
        {
            previous = Ambient.Value;
            Ambient.Value = sink;
        }

        public void Dispose() => Ambient.Value = previous;
    }
}
