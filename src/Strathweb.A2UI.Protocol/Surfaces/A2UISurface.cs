using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Messages;

namespace Strathweb.A2UI.Surfaces;

/// <summary>
/// A finished surface: the messages that put it on screen, and the ids needed to talk about it later.
/// </summary>
public sealed class A2UISurface
{
    internal A2UISurface(string surfaceId, string catalogId, IReadOnlyList<A2UIMessage> messages)
    {
        SurfaceId = surfaceId;
        CatalogId = catalogId;
        Messages = messages;
    }

    /// <summary>The surface's identifier, unique for the renderer's lifetime.</summary>
    public string SurfaceId { get; }

    /// <summary>The catalog its components come from.</summary>
    public string CatalogId { get; }

    /// <summary>
    /// The messages to send, in order: <c>createSurface</c>, then <c>updateComponents</c>, then
    /// <c>updateDataModel</c> when the surface has data.
    /// </summary>
    public IReadOnlyList<A2UIMessage> Messages { get; }

    /// <summary>Starts building a surface.</summary>
    /// <param name="surfaceId">
    /// The surface's identifier. Must be unique for the renderer's whole lifetime, not just this
    /// turn: reusing one replaces whatever is on screen.
    /// </param>
    /// <param name="catalog">The catalog its components come from.</param>
    /// <returns>A builder.</returns>
    public static A2UISurfaceBuilder Create(string surfaceId, A2UICatalog catalog) =>
        new(Throw.IfNullOrEmpty(surfaceId, nameof(surfaceId)), Throw.IfNull(catalog, nameof(catalog)));

    /// <inheritdoc />
    public override string ToString() => $"{SurfaceId} ({Messages.Count} messages)";
}
