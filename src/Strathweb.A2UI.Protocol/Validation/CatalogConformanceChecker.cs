using System.Text.Json.Nodes;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Components;

namespace Strathweb.A2UI.Validation;

/// <summary>Checks that components name only types and properties the catalog declares.</summary>
internal static class CatalogConformanceChecker
{
    private static readonly string[] CombinatorKeywords = ["allOf", "oneOf", "anyOf"];

    internal static void Check(
        IReadOnlyList<A2UIComponent> components,
        string path,
        A2UICatalog catalog,
        List<A2UIValidationError> errors)
    {
        for (var i = 0; i < components.Count; i++)
        {
            var component = components[i];
            var schema = catalog.GetComponent(component.Component);

            if (schema is null)
            {
                errors.Add(new A2UIValidationError(
                    A2UIValidationErrorCategory.Catalog,
                    A2UIErrorCodes.UnknownComponent,
                    $"{path}.{i}.component",
                    $"Catalog '{catalog.CatalogId}' does not define a component named " +
                    $"'{component.Component}'."));
                continue;
            }

            var declared = new HashSet<string>(StringComparer.Ordinal);
            var complete = CollectPropertyNames(schema, schema, catalog, declared);

            if (declared.Count == 0 || !complete)
            {
                // A schema with no properties constrains nothing, and one with unresolved $refs does
                // not say what is undeclared. Guessing would report real properties as unknown.
                continue;
            }

            foreach (var property in component.Properties)
            {
                if (!declared.Contains(property.Key))
                {
                    errors.Add(new A2UIValidationError(
                        A2UIValidationErrorCategory.Catalog,
                        A2UIErrorCodes.UnknownProperty,
                        $"{path}.{i}.{property.Key}",
                        $"Component '{component.Component}' has no property '{property.Key}' in " +
                        $"catalog '{catalog.CatalogId}'."));
                }
            }
        }
    }

    /// <returns><see langword="false"/> when some <c>$ref</c> could not be followed.</returns>
    private static bool CollectPropertyNames(
        JsonObject schema,
        JsonObject componentSchema,
        A2UICatalog catalog,
        HashSet<string> into)
    {
        var complete = true;

        if (schema["properties"] is JsonObject properties)
        {
            foreach (var property in properties)
            {
                into.Add(property.Key);
            }
        }

        foreach (var keyword in CombinatorKeywords)
        {
            if (schema[keyword] is not JsonArray branches)
            {
                continue;
            }

            foreach (var branch in branches)
            {
                var resolved = SchemaReferenceResolver.Resolve(
                    branch,
                    componentSchema,
                    catalog.Source,
                    catalog.ReferenceDocuments);

                if (!resolved.IsComplete)
                {
                    complete = false;
                    continue;
                }

                if (resolved.Node is JsonObject branchSchema)
                {
                    complete &= CollectPropertyNames(branchSchema, componentSchema, catalog, into);
                }
            }
        }

        return complete;
    }
}
