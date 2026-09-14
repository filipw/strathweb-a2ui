using System.Text.Json.Nodes;
using Strathweb.A2UI.Catalogs;

namespace Strathweb.A2UI.Protocol.UnitTests;

public class CatalogSelectorTests
{
    private static A2UICatalog Catalog(string id, params string[] components)
    {
        var defs = new JsonObject();
        foreach (var component in components)
        {
            defs[component] = new JsonObject { ["type"] = "object" };
        }

        return A2UICatalog.FromJson(new JsonObject { ["catalogId"] = id, ["components"] = defs });
    }

    [Fact]
    public void Select_TheRenderersOrderIsThePriority()
    {
        var selected = A2UICatalogSelector.Select(
            [Catalog("a"), Catalog("b"), Catalog("c")],
            ["c", "b"]);

        Assert.Equal("c", selected.CatalogId);
    }

    [Fact]
    public void Select_WithNoRendererList_TheAgentsFirstChoiceWins()
    {
        Assert.Equal("a", A2UICatalogSelector.Select([Catalog("a"), Catalog("b")], null).CatalogId);
        Assert.Equal("a", A2UICatalogSelector.Select([Catalog("a"), Catalog("b")], []).CatalogId);
    }

    [Fact]
    public void Select_NoOverlap_Fails()
    {
        var thrown = Assert.Throws<A2UICatalogException>(() =>
            A2UICatalogSelector.Select([Catalog("a")], ["z"]));

        Assert.Contains("No client-supported catalog found", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Select_InlineCatalogsAreRefusedUnlessAccepted()
    {
        var thrown = Assert.Throws<A2UICatalogException>(() =>
            A2UICatalogSelector.Select([Catalog("a")], null, [Catalog("inline", "Button")], acceptsInlineCatalogs: false));

        Assert.Contains("does not accept inline catalogs", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Select_InlineComponents_AreMergedOntoTheSelectedCatalog()
    {
        var selected = A2UICatalogSelector.Select(
            [Catalog("a", "Text")],
            null,
            [Catalog("inline", "Button"), Catalog("inline2", "Icon")],
            acceptsInlineCatalogs: true);

        Assert.Equal("a", selected.CatalogId);
        Assert.True(selected.HasComponent("Text"));
        Assert.True(selected.HasComponent("Button"));
        Assert.True(selected.HasComponent("Icon"));
    }

    [Fact]
    public void Select_NoOverlapButInlineCatalogs_FallsBackToTheDefault()
    {
        var selected = A2UICatalogSelector.Select(
            [Catalog("a", "Text"), Catalog("b")],
            ["z"],
            [Catalog("inline", "Button")],
            acceptsInlineCatalogs: true);

        Assert.Equal("a", selected.CatalogId);
        Assert.True(selected.HasComponent("Button"));
    }

    [Fact]
    public void Select_NoSupportedCatalogs_IsAnArgumentError()
    {
        Assert.Throws<ArgumentException>(() => A2UICatalogSelector.Select([], null));
    }

    [Fact]
    public void WithOnlyComponents_KeepsTheNamedOnes()
    {
        var reduced = A2UICatalogs.Basic(A2UIVersion.V0_9_1).WithOnlyComponents(["Text", "Button", "Nope"]);

        Assert.Equal(["Button", "Text"], reduced.Components.Select(c => c.Key).Order(StringComparer.Ordinal));
        Assert.Equal(A2UICatalogs.Basic(A2UIVersion.V0_9_1).CatalogId, reduced.CatalogId);
    }

    [Fact]
    public void WithoutStrictValidation_RemovesTheStrictKeywordsEverywhere()
    {
        var catalog = A2UICatalog.FromJson(new JsonObject
        {
            ["catalogId"] = "strict",
            ["components"] = new JsonObject
            {
                ["Text"] = new JsonObject
                {
                    ["type"] = "object",
                    ["additionalProperties"] = false,
                    ["properties"] = new JsonObject
                    {
                        ["nested"] = new JsonObject { ["type"] = "object", ["unevaluatedProperties"] = false },
                    },
                },
            },
        });

        var relaxed = catalog.WithoutStrictValidation();

        Assert.DoesNotContain("additionalProperties", relaxed.ToJson().ToJsonString(), StringComparison.Ordinal);
        Assert.DoesNotContain("unevaluatedProperties", relaxed.ToJson().ToJsonString(), StringComparison.Ordinal);
        Assert.Contains("additionalProperties", catalog.ToJson().ToJsonString(), StringComparison.Ordinal);
    }

    [Fact]
    public void WithComponentsFrom_DoesNotChangeTheOriginal()
    {
        var original = Catalog("a", "Text");

        var merged = original.WithComponentsFrom(Catalog("b", "Button"));

        Assert.False(original.HasComponent("Button"));
        Assert.True(merged.HasComponent("Button"));
    }
}
