using System.Text.Json.Nodes;
using Strathweb.A2UI.Catalogs;

using Strathweb.A2UI.TestSupport;
namespace Strathweb.A2UI.Protocol.UnitTests;

public class CatalogTests
{
    private static readonly string[] BasicComponents =
    [
        "Text", "Image", "Icon", "Video", "AudioPlayer", "Row", "Column", "List", "Card", "Tabs",
        "Modal", "Divider", "Button", "TextField", "CheckBox", "ChoicePicker", "Slider", "DateTimeInput",
    ];

    private static readonly string[] BasicFunctions =
    [
        "required", "regex", "length", "numeric", "email", "formatString", "formatNumber",
        "formatCurrency", "formatDate", "pluralize", "openUrl", "and", "or", "not",
    ];

    [Fact]
    public void Basic_DefinesEveryComponentTheSpecListsAndNothingElse()
    {
        var catalog = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

        Assert.Equal(
            BasicComponents.Order(StringComparer.Ordinal),
            catalog.Components.Select(p => p.Key).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Basic_DefinesEveryFunctionTheSpecLists()
    {
        var catalog = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

        Assert.Equal(
            BasicFunctions.Order(StringComparer.Ordinal),
            catalog.Functions.Select(p => p.Key).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Basic_IsTheEmbeddedCopyOfTheVendoredCatalog()
    {
        var embedded = A2UICatalogs.Basic(A2UIVersion.V0_9_1).ToJson();
        var vendored = SpecFiles.ReadJson("v0_9_1", "catalogs", "basic", "catalog.json");

        Assert.True(JsonNode.DeepEquals(vendored, embedded));
    }

    [Fact]
    public void Basic_CarriesTheAuthoringRulesAsInstructions()
    {
        var catalog = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

        Assert.NotNull(catalog.Instructions);
        Assert.Contains("REQUIRED PROPERTIES", catalog.Instructions, StringComparison.Ordinal);
    }

    [Fact]
    public void Basic_IsCachedRatherThanReloadedPerCall()
    {
        Assert.Same(A2UICatalogs.Basic(A2UIVersion.V0_9_1), A2UICatalogs.Basic(A2UIVersion.V0_9));
    }

    [Fact]
    public void Basic_ForV1_0_ThrowsUntilThatVersionIsImplemented()
    {
        Assert.Throws<NotSupportedException>(() => A2UICatalogs.Basic(A2UIVersion.V1_0));
    }

    [Fact]
    public void Basic_ExposesTheThemeSchema()
    {
        Assert.NotNull(A2UICatalogs.Basic(A2UIVersion.V0_9_1).Theme);
    }

    [Fact]
    public void FromJson_InlineCatalogWithFunctionArray_NormalizesToTheNameKeyedMap()
    {
        // A renderer's inline catalog in A2A capabilities encodes functions as an array of named
        // objects, unlike a catalog document. Both must read back the same way.
        var catalog = A2UICatalog.FromJson(JsonNode.Parse("""
            {
              "catalogId": "example.com:inline",
              "components": { "Text": { "type": "object" } },
              "functions": [
                { "name": "required", "parameters": { "type": "object" }, "returnType": "boolean" }
              ]
            }
            """));

        Assert.True(catalog.HasFunction("required"));
        Assert.Equal("boolean", (string?)catalog.Functions["required"]!["returnType"]);
        Assert.False(catalog.Functions["required"]!.AsObject().ContainsKey("name"));
    }

    [Fact]
    public void FromJson_WithoutCatalogId_Throws()
    {
        Assert.Throws<A2UICatalogException>(() => A2UICatalog.FromJson(JsonNode.Parse("""{"components":{}}""")));
    }

    [Fact]
    public void HasComponent_IsCaseSensitive()
    {
        var catalog = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

        Assert.True(catalog.HasComponent("Text"));
        Assert.False(catalog.HasComponent("text"));
    }
}
