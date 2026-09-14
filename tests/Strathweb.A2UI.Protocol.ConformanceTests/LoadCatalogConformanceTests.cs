using System.Text.Json.Nodes;
using Strathweb.A2UI.Catalogs;

namespace Strathweb.A2UI.Protocol.ConformanceTests;

/// <summary>
/// Runs the vendored <c>load_catalog</c> cases: catalogs loaded from files, optionally relaxed by the
/// <c>remove_strict_validation</c> modifier.
/// </summary>
public class LoadCatalogConformanceTests
{
    public static TheoryData<string> Cases()
    {
        var data = new TheoryData<string>();
        foreach (var name in ConformanceSuite.Cases
                     .Where(c => c.Action == "load_catalog")
                     .Select(c => c.Name)
                     .Order(StringComparer.Ordinal))
        {
            data.Add(name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void RunCase(string name)
    {
        var testCase = ConformanceSuite.Cases.Single(c => c.Name == name);

        var skipReason = ConformanceSkips.Reason(testCase);
        Assert.SkipWhen(skipReason is not null, $"{name}: {skipReason}");

        var modifiers = (testCase.Source["modifiers"] as JsonArray)?.Select(m => (string)m!).ToList() ?? [];
        var catalogs = new List<A2UICatalog>();

        foreach (var config in testCase.Source["catalog_configs"]!.AsArray().OfType<JsonObject>())
        {
            var path = Path.Combine(ConformanceSuite.Root, (string)config["path"]!);
            using var stream = File.OpenRead(path);
            var catalog = A2UICatalog.Load(stream);

            foreach (var modifier in modifiers)
            {
                catalog = modifier switch
                {
                    "remove_strict_validation" => catalog.WithoutStrictValidation(),
                    _ => throw new InvalidOperationException($"{name}: unknown modifier '{modifier}'."),
                };
            }

            catalogs.Add(catalog);
        }

        var expect = testCase.Source["expect"]!.AsObject();

        if (expect["catalog_schema"] is JsonObject expectedSchema)
        {
            var catalog = Assert.Single(catalogs);
            Assert.Equal((string?)expectedSchema["catalogId"], catalog.CatalogId);
            Assert.True(
                JsonNode.DeepEquals(expectedSchema["components"], catalog.Components),
                $"""
                 {name}
                 expected: {expectedSchema["components"]!.ToJsonString()}
                 actual:   {catalog.Components.ToJsonString()}
                 """);
        }

        if (expect["supported_catalog_ids"] is JsonArray expectedIds)
        {
            Assert.Equal(expectedIds.Select(id => (string)id!), catalogs.Select(c => c.CatalogId));
        }
    }
}
