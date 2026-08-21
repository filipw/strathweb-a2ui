using System.Text.Json.Nodes;

namespace Strathweb.A2UI.Catalogs;

/// <summary>Follows <c>$ref</c>s inside a catalog's component schemas.</summary>
internal static class SchemaReferenceResolver
{
    internal readonly struct Result(JsonNode? node, bool isComplete)
    {
        internal JsonNode? Node { get; } = node;

        /// <summary>Whether every <c>$ref</c> on the way here could be followed.</summary>
        internal bool IsComplete { get; } = isComplete;
    }

    internal static Result Resolve(
        JsonNode? schema,
        JsonObject componentSchema,
        JsonObject catalogDocument,
        IReadOnlyDictionary<string, JsonObject> referenceDocuments)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);

        while (schema is JsonObject obj &&
               obj["$ref"] is JsonValue refValue &&
               refValue.TryGetValue<string>(out var reference) &&
               visited.Add(reference))
        {
            var target = Follow(reference, componentSchema, catalogDocument, referenceDocuments);
            if (target is null)
            {
                return new Result(schema, isComplete: false);
            }

            schema = target;
        }

        return new Result(schema, isComplete: true);
    }

    private static JsonNode? Follow(
        string reference,
        JsonObject componentSchema,
        JsonObject catalogDocument,
        IReadOnlyDictionary<string, JsonObject> referenceDocuments)
    {
        var hash = reference.IndexOf('#');
        var documentUri = hash < 0 ? reference : reference.Substring(0, hash);
        var pointer = hash < 0 ? string.Empty : reference.Substring(hash + 1);

        if (pointer.Length == 0 || pointer[0] != '/')
        {
            return null;
        }

        var segments = pointer.Substring(1).Split('/');

        if (documentUri.Length == 0)
        {
            // A document-local ref. Component-local $defs win over the catalog's.
            return (segments[0] == "$defs" ? Walk(componentSchema["$defs"], segments, 1) : null)
                ?? Walk(catalogDocument, segments, 0);
        }

        return referenceDocuments.TryGetValue(documentUri, out var document)
            ? Walk(document, segments, 0)
            : null;
    }

    private static JsonNode? Walk(JsonNode? from, string[] segments, int start)
    {
        var current = from;
        for (var i = start; i < segments.Length; i++)
        {
            if (current is not JsonObject obj || obj[segments[i]] is not { } next)
            {
                return null;
            }

            current = next;
        }

        return current;
    }
}
