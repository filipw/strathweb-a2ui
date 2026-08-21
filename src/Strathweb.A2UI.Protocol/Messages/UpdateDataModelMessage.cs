using System.Text.Json.Nodes;
using Strathweb.A2UI.Internal;

namespace Strathweb.A2UI.Messages;

/// <summary>Writes to or deletes from a surface's data model.</summary>
public sealed class UpdateDataModelMessage : A2UIMessage
{
    private UpdateDataModelMessage(string surfaceId, string? path, JsonNode? value, bool hasValue)
    {
        SurfaceId = surfaceId;
        Path = path;
        Value = value;
        HasValue = hasValue;
    }

    /// <inheritdoc />
    public override A2UIMessageKind Kind => A2UIMessageKind.UpdateDataModel;

    /// <inheritdoc />
    public override A2UIMessageDirection Direction => A2UIMessageDirection.AgentToRenderer;

    /// <inheritdoc />
    public override string WireKey => "updateDataModel";

    /// <summary>The surface whose data model this message updates.</summary>
    public string SurfaceId { get; }

    /// <summary>
    /// A JSON Pointer into the data model. <see langword="null"/> or <c>"/"</c> addresses the whole
    /// model.
    /// </summary>
    public string? Path { get; }

    /// <summary>
    /// The value to write. Meaningful only when <see cref="HasValue"/> is <see langword="true"/>;
    /// a <see langword="null"/> value then means JSON null, not "no value".
    /// </summary>
    public JsonNode? Value { get; }

    /// <summary>
    /// Whether this message carries a value. When <see langword="false"/> the message deletes whatever
    /// is at <see cref="Path"/>.
    /// </summary>
    public bool HasValue { get; }

    /// <summary>Writes a value at a path, creating it if it does not exist.</summary>
    /// <param name="surfaceId">The surface to update.</param>
    /// <param name="path">A JSON Pointer into the data model.</param>
    /// <param name="value">The value to write. <see langword="null"/> writes JSON null.</param>
    /// <param name="version">The protocol version to declare.</param>
    /// <returns>The message.</returns>
    public static UpdateDataModelMessage Set(
        string surfaceId,
        string path,
        JsonNode? value,
        A2UIVersion version = A2UIVersion.V0_9_1) =>
        new(
            Throw.IfNullOrEmpty(surfaceId, nameof(surfaceId)),
            Throw.IfNullOrEmpty(path, nameof(path)),
            value,
            hasValue: true)
        { Version = version };

    /// <summary>Replaces the entire data model.</summary>
    /// <param name="surfaceId">The surface to update.</param>
    /// <param name="value">The new data model.</param>
    /// <param name="version">The protocol version to declare.</param>
    /// <returns>The message.</returns>
    public static UpdateDataModelMessage Replace(
        string surfaceId,
        JsonNode? value,
        A2UIVersion version = A2UIVersion.V0_9_1) =>
        new(Throw.IfNullOrEmpty(surfaceId, nameof(surfaceId)), "/", value, hasValue: true) { Version = version };

    /// <summary>Deletes whatever is at a path.</summary>
    /// <param name="surfaceId">The surface to update.</param>
    /// <param name="path">A JSON Pointer into the data model.</param>
    /// <param name="version">The protocol version to declare.</param>
    /// <returns>The message.</returns>
    public static UpdateDataModelMessage Remove(
        string surfaceId,
        string path,
        A2UIVersion version = A2UIVersion.V0_9_1) =>
        new(
            Throw.IfNullOrEmpty(surfaceId, nameof(surfaceId)),
            Throw.IfNullOrEmpty(path, nameof(path)),
            value: null,
            hasValue: false)
        { Version = version };

    /// <summary>
    /// Builds a message from a payload that may be missing fields the schema requires, so that the
    /// validator can report a precise error instead of the parser throwing an opaque one.
    /// </summary>
    internal static UpdateDataModelMessage FromWire(
        string surfaceId,
        string? path,
        JsonNode? value,
        bool hasValue,
        A2UIVersion version) =>
        new(surfaceId, path, value, hasValue) { Version = version };
}
