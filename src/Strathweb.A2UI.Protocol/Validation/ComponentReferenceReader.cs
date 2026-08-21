using System.Text.Json.Nodes;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Components;

namespace Strathweb.A2UI.Validation;

/// <summary>Pulls the component ids out of a component's reference properties.</summary>
internal static class ComponentReferenceReader
{
    private static readonly string[] WellKnownSingle = ["child"];
    private static readonly string[] WellKnownList = ["children"];

    internal static List<A2UIComponentReference> Read(A2UIComponent component, A2UICatalog? catalog)
    {
        var results = new List<A2UIComponentReference>();
        var fields = catalog?.GetReferenceFields(component.Component);

        foreach (var property in component.Properties)
        {
            var isReference = fields is not null
                ? fields.IsReferenceProperty(property.Key)
                : Array.IndexOf(WellKnownSingle, property.Key) >= 0 ||
                  Array.IndexOf(WellKnownList, property.Key) >= 0;

            if (isReference)
            {
                Extract(property.Value, property.Key, property.Key, fields, results);
            }
        }

        return results;
    }

    private static void Extract(
        JsonNode? value,
        string path,
        string topProperty,
        A2UIComponentReferenceFields? fields,
        List<A2UIComponentReference> into)
    {
        switch (value)
        {
            case JsonValue scalar when scalar.TryGetValue<string>(out var id):
                into.Add(new A2UIComponentReference(id, path));
                return;

            case JsonArray array:
                {
                    for (var i = 0; i < array.Count; i++)
                    {
                        // A plain array of ids keeps the property's own path; anything richer is indexed.
                        var itemPath = array[i] is JsonValue item && item.TryGetValue<string>(out _) &&
                                       !path.Contains('[')
                            ? path
                            : $"{path}[{i}]";

                        Extract(array[i], itemPath, topProperty, fields, into);
                    }

                    return;
                }

            case JsonObject obj:
                {
                    // A child-list template names its component directly.
                    if (obj["componentId"] is JsonValue templateId && templateId.TryGetValue<string>(out var component))
                    {
                        into.Add(new A2UIComponentReference(component, $"{path}.componentId"));
                        return;
                    }

                    var nested = fields?.NestedReferencePropertiesOf(topProperty);
                    if (nested is { Count: > 0 } && !path.Contains('.'))
                    {
                        // Tabs.tabs[] carries a 'title' as well as a 'child'; only the marked sub-keys
                        // are references.
                        foreach (var pair in obj)
                        {
                            if (Contains(nested, pair.Key))
                            {
                                Extract(pair.Value, $"{path}.{pair.Key}", topProperty, fields, into);
                            }
                        }

                        return;
                    }

                    foreach (var pair in obj)
                    {
                        Extract(pair.Value, $"{path}.{pair.Key}", topProperty, fields, into);
                    }

                    return;
                }
        }
    }

    private static bool Contains(IReadOnlyCollection<string> keys, string key)
    {
        foreach (var candidate in keys)
        {
            if (string.Equals(candidate, key, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
