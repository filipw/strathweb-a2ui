using System.Text.Json;
using System.Text.Json.Nodes;
using A2A;

namespace Strathweb.A2UI.A2A.UnitTests;

public class A2UIMetadataTests
{
    private static readonly A2UIVersionProfile Profile = A2UIVersionProfile.V0_9_1;

    [Fact]
    public void Capabilities_UseTheClientSpelledKeyAndVersionKeyUnderV0_9_1()
    {
        // Under v1.0 both are spelled 'renderer'. Writing the wrong one silently disables
        // negotiation instead of failing, so the exact strings matter.
        var metadata = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        A2UIMetadata.WriteCapabilities(metadata, new A2UIRendererCapabilities(["std"]), Profile);

        Assert.True(metadata.ContainsKey("a2uiClientCapabilities"));
        var body = JsonNode.Parse(metadata["a2uiClientCapabilities"].GetRawText())!.AsObject();
        Assert.True(body.ContainsKey("v0.9"));
    }

    [Fact]
    public void Capabilities_RoundTrip()
    {
        var metadata = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        A2UIMetadata.WriteCapabilities(metadata, new A2UIRendererCapabilities(["a", "b"]), Profile);

        Assert.True(A2UIMetadata.TryReadCapabilities(metadata, Profile, out var read));

        Assert.Equal(["a", "b"], read.SupportedCatalogIds);
        Assert.True(read.Supports("a"));
        Assert.False(read.Supports("c"));
    }

    [Fact]
    public void Capabilities_UnderADifferentVersionKey_AreNotRead()
    {
        var metadata = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["a2uiClientCapabilities"] = JsonDocument
                .Parse("""{"v1.0":{"supportedCatalogIds":["std"]}}""").RootElement.Clone(),
        };

        Assert.False(A2UIMetadata.TryReadCapabilities(metadata, Profile, out _));
    }

    [Fact]
    public void Capabilities_InlineCatalogs_AreReadWhenPresent()
    {
        var metadata = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["a2uiClientCapabilities"] = JsonDocument.Parse("""
                {"v0.9":{"supportedCatalogIds":["std"],
                 "inlineCatalogs":[{"catalogId":"example.com:custom","components":{"Gauge":{"type":"object"}}}]}}
                """).RootElement.Clone(),
        };

        Assert.True(A2UIMetadata.TryReadCapabilities(metadata, Profile, out var capabilities));

        var inline = Assert.Single(capabilities.InlineCatalogs);
        Assert.Equal("example.com:custom", inline.CatalogId);
        Assert.True(inline.HasComponent("Gauge"));
    }

    [Fact]
    public void Capabilities_AMalformedInlineCatalog_DoesNotSinkTheHandshake()
    {
        var metadata = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["a2uiClientCapabilities"] = JsonDocument.Parse("""
                {"v0.9":{"supportedCatalogIds":["std"],"inlineCatalogs":[{"components":{}}]}}
                """).RootElement.Clone(),
        };

        Assert.True(A2UIMetadata.TryReadCapabilities(metadata, Profile, out var capabilities));

        Assert.Equal(["std"], capabilities.SupportedCatalogIds);
        Assert.Empty(capabilities.InlineCatalogs);
    }

    [Fact]
    public void DataModel_RoundTripsUnderTheClientSpelledKey()
    {
        var metadata = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        var model = new A2UIRendererDataModel(
            A2UIVersion.V0_9_1,
            new Dictionary<string, JsonNode?> { ["s1"] = new JsonObject { ["rating"] = 5 } });

        A2UIMetadata.WriteDataModel(metadata, model, Profile);

        Assert.True(metadata.ContainsKey("a2uiClientDataModel"));
        Assert.True(A2UIMetadata.TryReadDataModel(metadata, Profile, out var read));
        Assert.Equal(5, (int)read.Surfaces["s1"]!["rating"]!);
    }

    [Fact]
    public void DataModel_MissingFromMetadata_ReturnsFalse()
    {
        Assert.False(A2UIMetadata.TryReadDataModel((IDictionary<string, JsonElement>?)null, Profile, out _));
        Assert.False(A2UIMetadata.TryReadDataModel(
            new Dictionary<string, JsonElement>(StringComparer.Ordinal),
            Profile,
            out _));
    }

    [Fact]
    public void AgentCard_AdvertisesTheVersionedExtensionUri()
    {
        var card = new AgentCard().AddA2UIExtension(A2UIVersion.V0_9_1, ["std"]);

        var extension = Assert.Single(card.Capabilities!.Extensions!);
        Assert.Equal("https://a2ui.org/a2a-extension/a2ui/v0.9.1", extension.Uri);
        Assert.Equal(["std"], (string[])[.. extension.Params!.Value.GetProperty("supportedCatalogIds")
            .EnumerateArray().Select(e => e.GetString()!)]);
    }

    [Fact]
    public void AgentCard_WithNothingToDeclare_AdvertisesNoParameters()
    {
        var card = new AgentCard().AddA2UIExtension(A2UIVersion.V0_9_1);

        Assert.Null(Assert.Single(card.Capabilities!.Extensions!).Params);
    }

    [Fact]
    public void AgentCard_InlineCatalogsAreOffUnlessAskedFor()
    {
        var strict = new AgentCard().AddA2UIExtension(A2UIVersion.V0_9_1, ["std"]);
        var permissive = new AgentCard().AddA2UIExtension(A2UIVersion.V0_9_1, ["std"], acceptsInlineCatalogs: true);

        Assert.False(strict.Capabilities!.Extensions![0].Params!.Value
            .TryGetProperty("acceptsInlineCatalogs", out _));
        Assert.True(permissive.Capabilities!.Extensions![0].Params!.Value
            .GetProperty("acceptsInlineCatalogs").GetBoolean());
    }

    [Fact]
    public void AgentCard_AddingTheSameVersionTwice_ReplacesRatherThanDuplicates()
    {
        var card = new AgentCard()
            .AddA2UIExtension(A2UIVersion.V0_9_1, ["a"])
            .AddA2UIExtension(A2UIVersion.V0_9_1, ["b"]);

        Assert.Single(card.Capabilities!.Extensions!);
        Assert.Equal(["https://a2ui.org/a2a-extension/a2ui/v0.9.1"], card.GetA2UIExtensionUris());
    }

    [Fact]
    public void AgentCard_IsNotMarkedRequiredByDefault()
    {
        // A client that cannot render A2UI must still be able to talk to the agent.
        Assert.Null(new AgentCard().AddA2UIExtension(A2UIVersion.V0_9_1).Capabilities!.Extensions![0].Required);
    }

    [Theory]
    [InlineData("https://a2ui.org/a2a-extension/a2ui/v1.10.0", "https://a2ui.org/a2a-extension/a2ui/v1.2.0")]
    [InlineData("https://a2ui.org/a2a-extension/a2ui/v0.9.1", "https://a2ui.org/a2a-extension/a2ui/v0.9")]
    [InlineData("https://a2ui.org/a2a-extension/a2ui/v2.0.0", "https://a2ui.org/a2a-extension/a2ui/v1.10.0")]
    public void ExtensionUris_AreComparedByNumericSegment(string newer, string older)
    {
        Assert.Equal(newer, A2UIExtensionUris.SelectNewest([older, newer]));
        Assert.Equal(newer, A2UIExtensionUris.SelectNewest([newer, older]));
    }

    [Fact]
    public void ExtensionUris_IgnoreUrisThatAreNotA2UI()
    {
        Assert.Null(A2UIExtensionUris.SelectNewest(["https://example.com/other"]));
        Assert.False(A2UIExtensionUris.ContainsA2UIExtension(["https://example.com/other"]));
    }
}
