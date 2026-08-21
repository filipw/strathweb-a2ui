using System.Text.Json;
using System.Text.Json.Nodes;
using A2A;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Serialization;

namespace Strathweb.A2UI.A2A.UnitTests;

public class A2UIPartsTests
{
    private static readonly A2UIMessage[] Messages =
    [
        new CreateSurfaceMessage("s1", "https://example.com/catalog.json"),
        new DeleteSurfaceMessage("s1"),
    ];

    [Fact]
    public void Create_PutsTheMessagesInDataAndTheMimeTypeInMetadata()
    {
        var part = A2UIParts.Create(Messages);

        Assert.Equal(JsonValueKind.Array, part.Data!.Value.ValueKind);
        Assert.Equal(
            "application/a2ui+json",
            part.Metadata![A2UIParts.MimeTypeMetadataKey].GetString());
    }

    [Fact]
    public void Create_WithOneMessage_StillWritesAnArray()
    {
        // The wire format is always a list, even for a single message.
        var part = A2UIParts.Create(new DeleteSurfaceMessage("s1"));

        Assert.Equal(JsonValueKind.Array, part.Data!.Value.ValueKind);
        Assert.Equal(1, part.Data.Value.GetArrayLength());
    }

    [Fact]
    public void Create_MatchesTheDataPartShapeInTheExtensionSpec()
    {
        var part = A2UIParts.Create(new CreateSurfaceMessage("sales-dashboard", "std"));
        var data = JsonNode.Parse(part.Data!.Value.GetRawText())!.AsArray();

        Assert.True(JsonNode.DeepEquals(
            JsonNode.Parse("""
                [{"version":"v0.9.1","createSurface":{"surfaceId":"sales-dashboard","catalogId":"std"}}]
                """),
            data));
    }

    [Fact]
    public void TryRead_RoundTripsTheMessages()
    {
        Assert.True(A2UIParts.TryRead(A2UIParts.Create(Messages), out var read));

        Assert.Equal(2, read.Count);
        Assert.Equal("s1", ((CreateSurfaceMessage)read[0]).SurfaceId);
        Assert.IsType<DeleteSurfaceMessage>(read[1]);
    }

    [Fact]
    public void TryRead_APartThatIsNotA2UI_ReturnsFalse()
    {
        Assert.False(A2UIParts.TryRead(Part.FromText("hello"), out _));
        Assert.False(A2UIParts.TryRead(null, out _));
    }

    [Fact]
    public void IsA2UI_AcceptsTheDeprecatedMimeTypeOnRead()
    {
        // v0.8 and early v0.9 peers spell it the other way round.
        var part = Part.FromData(JsonDocument.Parse("[]").RootElement.Clone());
        part.Metadata = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            [A2UIParts.MimeTypeMetadataKey] = JsonDocument.Parse("\"application/json+a2ui\"").RootElement.Clone(),
        };

        Assert.True(A2UIParts.IsA2UI(part));
    }

    [Fact]
    public void Create_NeverWritesTheDeprecatedMimeType()
    {
        Assert.DoesNotContain(
            "json+a2ui",
            A2UIParts.Create(Messages).Metadata![A2UIParts.MimeTypeMetadataKey].GetString()!,
            StringComparison.Ordinal);
    }

    [Fact]
    public void IsA2UI_DoesNotLookAtThePartsMediaType()
    {
        // An inbound A2UI data part arrives as generic application/json; only the metadata survives.
        var part = Part.FromData(JsonDocument.Parse("[]").RootElement.Clone());
        part.MediaType = "application/a2ui+json";

        Assert.False(A2UIParts.IsA2UI(part));
    }

    [Fact]
    public void TryRead_AMalformedMessage_Throws()
    {
        var part = Part.FromData(JsonDocument.Parse("""[{"nonsense":true}]""").RootElement.Clone());
        part.Metadata = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            [A2UIParts.MimeTypeMetadataKey] = JsonDocument.Parse("\"application/a2ui+json\"").RootElement.Clone(),
        };

        Assert.Throws<A2UIParseException>(() => A2UIParts.TryRead(part, out _));
    }

    [Fact]
    public void TryReadTolerant_KeepsTheGoodMessagesAndReportsTheBadOnes()
    {
        // The spec is explicit that a message list is not a transactional unit.
        var part = Part.FromData(JsonDocument.Parse("""
            [{"version":"v0.9.1","deleteSurface":{"surfaceId":"s1"}},{"nonsense":true}]
            """).RootElement.Clone());
        part.Metadata = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            [A2UIParts.MimeTypeMetadataKey] = JsonDocument.Parse("\"application/a2ui+json\"").RootElement.Clone(),
        };

        Assert.True(A2UIParts.TryReadTolerant(part, out var messages, out var errors));

        Assert.Single(messages);
        Assert.Single(errors);
        Assert.StartsWith("messages.1:", errors[0], StringComparison.Ordinal);
    }
}
