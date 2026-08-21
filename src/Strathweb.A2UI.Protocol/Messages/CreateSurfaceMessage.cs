using System.Text.Json.Nodes;
using Strathweb.A2UI.Internal;

namespace Strathweb.A2UI.Messages;

/// <summary>
/// Tells the renderer to create a surface and start rendering it. Components and data arrive in
/// subsequent <see cref="UpdateComponentsMessage"/> and <see cref="UpdateDataModelMessage"/> messages.
/// </summary>
public sealed class CreateSurfaceMessage : A2UIMessage
{
    /// <summary>Creates a <c>createSurface</c> message.</summary>
    /// <param name="surfaceId">The identifier for the new surface. Must be unique for the renderer's lifetime.</param>
    /// <param name="catalogId">The catalog whose components this surface will name. Required under v0.9.1.</param>
    public CreateSurfaceMessage(string surfaceId, string catalogId)
    {
        SurfaceId = Throw.IfNullOrEmpty(surfaceId, nameof(surfaceId));
        CatalogId = Throw.IfNullOrEmpty(catalogId, nameof(catalogId));
    }

    // Used only by FromWire, which must be able to express a payload missing its catalogId.
    private CreateSurfaceMessage()
    {
        SurfaceId = null!;
    }

    /// <summary>
    /// Builds a message from a payload that may be missing fields the schema requires, so that the
    /// validator can report a precise error instead of the parser throwing an opaque one.
    /// </summary>
    internal static CreateSurfaceMessage FromWire(
        string surfaceId,
        string? catalogId,
        A2UIVersion version,
        JsonObject? theme,
        bool? sendDataModel) =>
        new()
        {
            SurfaceId = surfaceId,
            CatalogId = catalogId,
            Version = version,
            Theme = theme,
            SendDataModel = sendDataModel,
        };

    /// <inheritdoc />
    public override A2UIMessageKind Kind => A2UIMessageKind.CreateSurface;

    /// <inheritdoc />
    public override A2UIMessageDirection Direction => A2UIMessageDirection.AgentToRenderer;

    /// <inheritdoc />
    public override string WireKey => "createSurface";

    /// <summary>The identifier of the surface to create.</summary>
    public string SurfaceId { get; private init; }

    /// <summary>
    /// The catalog this surface's components are drawn from. Required by the v0.9.1 schema; may be
    /// <see langword="null"/> only on a payload read from a peer that omitted it.
    /// </summary>
    public string? CatalogId { get; private init; }

    /// <summary>
    /// Theme parameters for the surface, validated by the catalog's <c>theme</c> schema. Omitted when
    /// <see langword="null"/>. Removed in v1.0.
    /// </summary>
    public JsonObject? Theme { get; init; }

    /// <summary>
    /// When true, the renderer sends this surface's full data model in the metadata of every message
    /// it posts back. Omitted from the wire when null; the renderer's default is false.
    /// </summary>
    public bool? SendDataModel { get; init; }
}
