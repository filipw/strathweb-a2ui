using System.Text.Json.Nodes;

namespace Strathweb.A2UI.Catalogs;

/// <summary>
/// Works out, from a catalog's JSON Schemas, which properties of each component hold references to
/// other components.
/// </summary>
internal static class CatalogReferenceFieldReader
{
    private const string ComponentIdSuffix = "/ComponentId";
    private const string ChildListSuffix = "/ChildList";

    private static readonly string[] CombinatorKeywords = ["allOf", "oneOf", "anyOf"];

    internal static Dictionary<string, A2UIComponentReferenceFields> Read(JsonObject catalogDocument)
    {
        var result = new Dictionary<string, A2UIComponentReferenceFields>(StringComparer.Ordinal);

        if (catalogDocument["components"] is not JsonObject components)
        {
            return result;
        }

        foreach (var pair in components)
        {
            if (pair.Value is not JsonObject componentSchema)
            {
                continue;
            }

            var single = new HashSet<string>(StringComparer.Ordinal);
            var list = new HashSet<string>(StringComparer.Ordinal);
            var nested = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            ReadProperties(componentSchema, componentSchema, catalogDocument, single, list, nested);

            if (single.Count > 0 || list.Count > 0)
            {
                result[pair.Key] = new A2UIComponentReferenceFields(single, list, nested);
            }
        }

        return result;
    }

    private static void ReadProperties(
        JsonObject schema,
        JsonObject componentSchema,
        JsonObject catalogDocument,
        HashSet<string> single,
        HashSet<string> list,
        Dictionary<string, HashSet<string>> nested)
    {
        if (schema["properties"] is JsonObject properties)
        {
            foreach (var property in properties)
            {
                Classify(property.Key, property.Value, componentSchema, catalogDocument, single, list, nested);
            }
        }

        // Basic Catalog components are an allOf over shared fragments plus their own properties.
        foreach (var keyword in CombinatorKeywords)
        {
            if (schema[keyword] is not JsonArray branches)
            {
                continue;
            }

            foreach (var branch in branches)
            {
                if (branch is JsonObject branchSchema)
                {
                    ReadProperties(branchSchema, componentSchema, catalogDocument, single, list, nested);
                }
            }
        }
    }

    private static void Classify(
        string propertyName,
        JsonNode? propertySchema,
        JsonObject componentSchema,
        JsonObject catalogDocument,
        HashSet<string> single,
        HashSet<string> list,
        Dictionary<string, HashSet<string>> nested)
    {
        var resolved = Resolve(propertySchema, componentSchema, catalogDocument);

        if (IsRefTo(resolved, ComponentIdSuffix))
        {
            single.Add(propertyName);
            return;
        }

        if (IsRefTo(resolved, ChildListSuffix))
        {
            list.Add(propertyName);
            return;
        }

        if (resolved is not JsonObject obj ||
            (obj["type"] as JsonValue)?.TryGetValue<string>(out var type) != true || type != "array")
        {
            return;
        }

        var items = Resolve(obj["items"], componentSchema, catalogDocument);
        if (items is not JsonObject itemSchema)
        {
            return;
        }

        if (IsRefTo(itemSchema, ComponentIdSuffix) || IsRefTo(itemSchema, ChildListSuffix))
        {
            list.Add(propertyName);
            return;
        }

        // An array of objects, each of which may carry a reference: Tabs.tabs[].child.
        if (itemSchema["properties"] is not JsonObject itemProperties)
        {
            return;
        }

        foreach (var itemProperty in itemProperties)
        {
            var resolvedItem = Resolve(itemProperty.Value, componentSchema, catalogDocument);
            if (!IsRefTo(resolvedItem, ComponentIdSuffix) && !IsRefTo(resolvedItem, ChildListSuffix))
            {
                continue;
            }

            list.Add(propertyName);
            if (!nested.TryGetValue(propertyName, out var subKeys))
            {
                nested[propertyName] = subKeys = new HashSet<string>(StringComparer.Ordinal);
            }

            subKeys.Add(itemProperty.Key);
        }
    }

    private static bool IsRefTo(JsonNode? schema, string suffix)
    {
        if (schema is not JsonObject obj)
        {
            return false;
        }

        if (obj["$ref"] is JsonValue refValue && refValue.TryGetValue<string>(out var reference) &&
            reference.EndsWith(suffix, StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var keyword in CombinatorKeywords)
        {
            if (obj[keyword] is not JsonArray branches)
            {
                continue;
            }

            foreach (var branch in branches)
            {
                if (IsRefTo(branch, suffix))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Follows document-local <c>$ref</c>s. Cross-document refs are left alone; their target names
    /// are what the reference markers are read from.
    /// </summary>
    private static JsonNode? Resolve(JsonNode? schema, JsonObject componentSchema, JsonObject catalogDocument)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);

        while (schema is JsonObject obj &&
               obj["$ref"] is JsonValue refValue &&
               refValue.TryGetValue<string>(out var reference) &&
               reference.StartsWith("#/", StringComparison.Ordinal) &&
               !reference.EndsWith(ComponentIdSuffix, StringComparison.Ordinal) &&
               !reference.EndsWith(ChildListSuffix, StringComparison.Ordinal) &&
               visited.Add(reference))
        {
            var segments = reference.Substring(2).Split('/');

            // Component-local $defs win over the catalog document's.
            var target = segments[0] == "$defs"
                ? Walk(componentSchema["$defs"], segments.Skip(1)) ?? Walk(catalogDocument, segments)
                : Walk(catalogDocument, segments);

            if (target is null)
            {
                break;
            }

            schema = target;
        }

        return schema;
    }

    private static JsonNode? Walk(JsonNode? from, IEnumerable<string> segments)
    {
        var current = from;
        foreach (var segment in segments)
        {
            if (current is not JsonObject obj || obj[segment] is not { } next)
            {
                return null;
            }

            current = next;
        }

        return current;
    }
}
