using System.Text.Json.Nodes;

namespace Strathweb.A2UI.Protocol.ConformanceTests;

/// <summary>One case from the vendored suite, kept as raw JSON so nothing is lost in translation.</summary>
internal sealed class ConformanceCase(string file, JsonObject source)
{
    internal string File { get; } = file;

    internal JsonObject Source { get; } = source;

    internal string Name => (string?)Source["name"] ?? "(unnamed)";

    internal string Action => (string?)Source["action"] ?? "(none)";

    /// <summary>
    /// The protocol version the case targets, or <see langword="null"/> when it is version-agnostic.
    /// Validator cases carry it under <c>catalog</c>; prompt cases under <c>args</c>.
    /// </summary>
    internal string? Version =>
        (string?)(Source["catalog"] as JsonObject)?["version"] ??
        (string?)(Source["args"] as JsonObject)?["version"];

    /// <summary>The <c>args</c> object, empty when the case has none.</summary>
    internal JsonObject Args => Source["args"] as JsonObject ?? [];

    internal JsonNode? CatalogSchema => (Source["catalog"] as JsonObject)?["catalog_schema"];

    /// <summary>
    /// The steps to run. A case may give a single <c>payload</c> or a list of <c>steps</c>; both are
    /// presented here as steps so the runner has one shape to deal with.
    /// </summary>
    internal IReadOnlyList<JsonObject> Steps =>
        Source["steps"] is JsonArray steps
            ? [.. steps.OfType<JsonObject>()]
            : Source.ContainsKey("payload") ? [Source] : [];

    public override string ToString() => $"{File}:{Name}";
}
