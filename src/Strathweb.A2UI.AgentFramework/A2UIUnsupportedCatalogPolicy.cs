namespace Strathweb.A2UI.AgentFramework;

/// <summary>
/// What happens when a tool emits a surface from a catalog the renderer did not advertise. A renderer
/// that does not know a catalog draws nothing and reports nothing, so this is the only place the
/// mismatch can be seen.
/// </summary>
public enum A2UIUnsupportedCatalogPolicy
{
    /// <summary>Send the surface anyway and log a warning.</summary>
    Warn = 1,

    /// <summary>Do not send the surface, and log a warning. The tool's return value still reaches the model.</summary>
    Drop = 2,

    /// <summary>
    /// Throw <see cref="InvalidOperationException"/> from the emit call, so the tool can fall back to
    /// text or let the model see that the UI could not be shown.
    /// </summary>
    Throw = 3,
}
