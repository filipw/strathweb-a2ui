using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Strathweb.A2UI.Catalogs;

namespace Strathweb.A2UI.Protocol.ConformanceTests;

/// <summary>Runs the vendored <c>select_catalog</c> cases against <see cref="A2UICatalogSelector"/>.</summary>
public class CatalogSelectionConformanceTests
{
    public static TheoryData<string> Cases()
    {
        var data = new TheoryData<string>();
        foreach (var name in ConformanceSuite.Cases
                     .Where(c => c.Action == "select_catalog")
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

        var args = testCase.Args;
        var supported = args["supported_catalogs"]!.AsArray().Select(c => A2UICatalog.FromJson(c)).ToList();
        var capabilities = args["client_capabilities"] as JsonObject ?? [];
        var rendererIds = (capabilities["supportedCatalogIds"] as JsonArray)?.Select(id => (string)id!).ToList();
        var inline = (capabilities["inlineCatalogs"] as JsonArray)?.Select(c => A2UICatalog.FromJson(c)).ToList();
        var acceptsInline = (bool?)args["accepts_inline_catalogs"] ?? false;

        if (testCase.Source["expect_error"] is JsonObject expectedError)
        {
            var thrown = Assert.Throws<A2UICatalogException>(() =>
                A2UICatalogSelector.Select(supported, rendererIds, inline, acceptsInline));

            var pattern = (string?)expectedError["message"];
            Assert.True(
                pattern is null || Regex.IsMatch(thrown.Message, pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
                $"{name}: expected a message matching /{pattern}/, got '{thrown.Message}'.");
            return;
        }

        var selected = A2UICatalogSelector.Select(supported, rendererIds, inline, acceptsInline);

        if ((string?)testCase.Source["expect_selected"] is { } expectedId)
        {
            Assert.Equal(expectedId, selected.CatalogId);
        }

        if (testCase.Source["expect_catalog_schema"] is JsonObject expectedSchema)
        {
            Assert.Equal((string?)expectedSchema["catalogId"], selected.CatalogId);
            Assert.True(
                JsonNode.DeepEquals(expectedSchema["components"], selected.Components),
                $"""
                 {name}
                 expected: {expectedSchema["components"]!.ToJsonString()}
                 actual:   {selected.Components.ToJsonString()}
                 """);
        }
    }
}
