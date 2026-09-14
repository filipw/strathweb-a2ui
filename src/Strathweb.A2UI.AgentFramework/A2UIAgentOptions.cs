using Microsoft.Extensions.Logging;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>How an agent should handle A2UI.</summary>
public sealed class A2UIAgentOptions
{
    /// <summary>The protocol version whose metadata keys and rules apply. Defaults to v0.9.1.</summary>
    public A2UIVersion Version { get; set; } = A2UIVersion.V0_9_1;

    /// <summary>How many surfaces a session remembers before forgetting the oldest.</summary>
    public int SurfaceHistoryCapacity { get; set; } = 50;

    /// <summary>
    /// Whether surfaces emitted during a streaming run are sent as they appear rather than at the end.
    /// </summary>
    public bool StreamSurfacesAsTheyAppear { get; set; } = true;

    /// <summary>Whether inbound A2UI parts are turned into structured actions and a sentence for the model.</summary>
    public bool NormalizeInboundActions { get; set; } = true;

    /// <summary>
    /// What to do when a tool emits a surface from a catalog the renderer did not list in its
    /// capabilities. Only applies when the renderer sent capabilities this turn. Defaults to
    /// <see cref="A2UIUnsupportedCatalogPolicy.Warn"/>.
    /// </summary>
    public A2UIUnsupportedCatalogPolicy UnsupportedCatalogPolicy { get; set; } = A2UIUnsupportedCatalogPolicy.Warn;

    /// <summary>Caps on what is read from the renderer. The renderer is not trusted.</summary>
    public A2UIInboundLimits InboundLimits { get; set; } = new();

    /// <summary>
    /// Turns on prompt-first generation: A2UI blocks the model writes in its replies become surfaces.
    /// <see langword="null"/>, the default, leaves the model's text alone.
    /// </summary>
    public A2UIPromptFirstOptions? PromptFirst { get; set; }

    /// <summary>
    /// Where to log. When <see langword="null"/>, the wrapped agent is asked for an
    /// <see cref="ILoggerFactory"/> through <c>GetService</c>, and nothing is logged if it has none.
    /// </summary>
    public ILoggerFactory? LoggerFactory { get; set; }

    /// <summary>The version rules to apply, derived from <see cref="Version"/>.</summary>
    public A2UIVersionProfile Profile => A2UIVersionProfile.For(Version);
}
