using System.Reflection;

namespace Strathweb.A2UI.Catalogs;

/// <summary>The catalogs shipped with this library.</summary>
public static class A2UICatalogs
{
    private const string CatalogResource = "Strathweb.A2UI.Catalogs.v0_9_1.basic.catalog.json";
    private const string InstructionsResource = "Strathweb.A2UI.Catalogs.v0_9_1.basic.rules.txt";
    private const string CommonTypesResource = "Strathweb.A2UI.Catalogs.v0_9_1.common_types.json";
    private const string CommonTypesUri = "https://a2ui.org/specification/v0_9/common_types.json";

    private static A2UICatalog? basicV0_9_1;

    /// <summary>The official Basic Catalog, embedded from the vendored specification.</summary>
    /// <param name="version">The protocol version whose catalog to load.</param>
    /// <returns>The catalog. The same instance is returned on every call.</returns>
    /// <exception cref="NotSupportedException">No catalog ships for <paramref name="version"/>.</exception>
    public static A2UICatalog Basic(A2UIVersion version) => version switch
    {
        A2UIVersion.V0_9 or A2UIVersion.V0_9_1 => basicV0_9_1 ??= LoadBasicV0_9_1(),
        A2UIVersion.V1_0 => throw new NotSupportedException(
            "A2UI v1.0 is a release candidate upstream and is not implemented yet. Use v0.9.1."),
        _ => throw new ArgumentOutOfRangeException(nameof(version), version, "Unknown A2UI version."),
    };

    private static A2UICatalog LoadBasicV0_9_1()
    {
        var assembly = typeof(A2UICatalogs).GetTypeInfo().Assembly;

        using var catalogStream = assembly.GetManifestResourceStream(CatalogResource)
            ?? throw new A2UICatalogException($"Embedded catalog '{CatalogResource}' is missing from the assembly.");

        var catalog = A2UICatalog.Load(catalogStream);

        using (var commonTypesStream = assembly.GetManifestResourceStream(CommonTypesResource))
        {
            if (commonTypesStream is null)
            {
                throw new A2UICatalogException(
                    $"Embedded schema '{CommonTypesResource}' is missing from the assembly.");
            }

            // The catalog's components are an allOf over fragments defined here; without it their
            // shared properties look undeclared.
            catalog = catalog.WithReferenceDocument(
                CommonTypesUri,
                System.Text.Json.Nodes.JsonNode.Parse(commonTypesStream)?.AsObject()
                ?? throw new A2UICatalogException($"Embedded schema '{CommonTypesResource}' is not an object."));
        }

        using var instructionsStream = assembly.GetManifestResourceStream(InstructionsResource);
        if (instructionsStream is null)
        {
            return catalog;
        }

        using var reader = new StreamReader(instructionsStream);
        return catalog.WithInstructions(reader.ReadToEnd());
    }
}
