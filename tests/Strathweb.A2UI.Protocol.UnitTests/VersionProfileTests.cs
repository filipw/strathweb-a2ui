using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Validation;

namespace Strathweb.A2UI.Protocol.UnitTests;

public class VersionProfileTests
{
    private const string CatalogId = "https://a2ui.org/specification/v0_9/catalogs/basic/catalog.json";

    [Theory]
    [InlineData(A2UIVersion.V0_9)]
    [InlineData(A2UIVersion.V0_9_1)]
    public void For_V0_9Family_ReturnsTheV0_9_1Profile(A2UIVersion version)
    {
        Assert.Same(A2UIVersionProfile.V0_9_1, A2UIVersionProfile.For(version));
    }

    [Fact]
    public void For_V1_0_ThrowsBecauseNoProfileImplementsIt()
    {
        Assert.Throws<NotSupportedException>(() => A2UIVersionProfile.For(A2UIVersion.V1_0));
    }

    [Fact]
    public void V0_9_1Profile_UsesTheClientSpellingOfTheMetadataKeys()
    {
        var profile = A2UIVersionProfile.V0_9_1;

        Assert.Equal("a2uiClientCapabilities", profile.CapabilitiesMetadataKey);
        Assert.Equal("a2uiClientDataModel", profile.DataModelMetadataKey);
        Assert.Equal("v0.9", profile.CapabilitiesVersionKey);
        Assert.Equal("https://a2ui.org/a2a-extension/a2ui/v0.9.1", profile.ExtensionUri);
    }

    [Fact]
    public void V0_9_1Profile_WritesTheCurrentMediaTypeAndStillReadsTheDeprecatedOne()
    {
        var profile = A2UIVersionProfile.V0_9_1;

        Assert.Equal("application/a2ui+json", profile.MediaType);
        Assert.Contains("application/a2ui+json", profile.AcceptedMediaTypes);
        Assert.Contains("application/json+a2ui", profile.AcceptedMediaTypes);
        Assert.DoesNotContain("application/json+a2ui", profile.MediaType);
    }

    [Fact]
    public void CheckVersionRules_CreateSurfaceWithCatalogId_IsLegal()
    {
        var errors = A2UIVersionProfile.V0_9_1.CheckVersionRules(
            new CreateSurfaceMessage("s1", CatalogId),
            "messages.0");

        Assert.Empty(errors);
    }

    [Fact]
    public void CheckVersionRules_CreateSurfaceWithoutCatalogId_ReportsMissingField()
    {
        // v1.0 makes catalogId optional, so a peer can send one without it. That is a version error
        // here, not a parse error.
        var message = A2UIJson.Deserialize("""{"version":"v0.9.1","createSurface":{"surfaceId":"s1"}}""");

        var error = Assert.Single(A2UIVersionProfile.V0_9_1.CheckVersionRules(message, "messages.0"));

        Assert.Equal(A2UIErrorCodes.MissingField, error.Code);
        Assert.Equal("messages.0.createSurface.catalogId", error.Path);
    }

    [Fact]
    public void CheckVersionRules_ValidationFailedErrorWithoutPath_ReportsMissingField()
    {
        var message = new ErrorMessage(ErrorMessage.ValidationFailedCode, "s1", "Bad component.");

        var error = Assert.Single(A2UIVersionProfile.V0_9_1.CheckVersionRules(message, "messages.0"));

        Assert.Equal(A2UIErrorCodes.MissingField, error.Code);
        Assert.Equal("messages.0.error.path", error.Path);
    }

    [Fact]
    public void CheckVersionRules_V1_0Message_IsRejectedByTheV0_9_1Profile()
    {
        var message = new DeleteSurfaceMessage("s1") { Version = A2UIVersion.V1_0 };

        var error = Assert.Single(A2UIVersionProfile.V0_9_1.CheckVersionRules(message, "messages.0"));

        Assert.Equal(A2UIErrorCodes.InvalidValue, error.Code);
        Assert.Equal("messages.0.version", error.Path);
    }
}
