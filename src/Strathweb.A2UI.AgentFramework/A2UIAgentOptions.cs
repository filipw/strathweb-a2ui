using Strathweb.A2UI.Catalogs;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>How an agent should handle A2UI.</summary>
public sealed class A2UIAgentOptions
{
    /// <summary>The protocol version to emit. Defaults to v0.9.1.</summary>
    public A2UIVersion Version { get; set; } = A2UIVersion.V0_9_1;

    /// <summary>
    /// The catalog surfaces are built from when the caller does not name one. Defaults to the Basic
    /// Catalog for <see cref="Version"/>.
    /// </summary>
    public A2UICatalog? DefaultCatalog { get; set; }

    /// <summary>
    /// The prefix generated surface ids start with. Useful for telling one agent's surfaces from
    /// another's in a log.
    /// </summary>
    public string SurfaceIdPrefix { get; set; } = "surface";

    /// <summary>How many surfaces a session remembers before forgetting the oldest.</summary>
    public int SurfaceHistoryCapacity { get; set; } = 50;

    /// <summary>
    /// Whether surfaces emitted during a streaming run are sent as they appear rather than at the end.
    /// </summary>
    public bool StreamSurfacesAsTheyAppear { get; set; } = true;

    /// <summary>Whether inbound A2UI parts are turned into structured actions and a sentence for the model.</summary>
    public bool NormalizeInboundActions { get; set; } = true;

    /// <summary>The version rules to apply, derived from <see cref="Version"/>.</summary>
    public A2UIVersionProfile Profile => A2UIVersionProfile.For(Version);

    /// <summary>The catalog to use, resolving the default when none was set.</summary>
    public A2UICatalog ResolveCatalog() => DefaultCatalog ?? A2UICatalogs.Basic(Version);
}
