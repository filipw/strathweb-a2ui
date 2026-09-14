using System.Text.Json.Nodes;
using Strathweb.A2UI.Parsing;
using Strathweb.A2UI.Serialization;

namespace Strathweb.A2UI.Protocol.UnitTests;

public class PayloadRepairTests
{
    [Fact]
    public void Fix_ATrailingCommaInsideAString_IsLeftAlone()
    {
        var fixed_ = A2UIPayloadRepair.Fix("""[{"text": "a,}", "n": 1,}]""");

        Assert.Equal("a,}", (string?)fixed_[0]!["text"]);
        Assert.Equal(1, (int)fixed_[0]!["n"]!);
    }

    [Fact]
    public void Fix_AnEscapedQuoteInsideAString_DoesNotEndTheString()
    {
        var fixed_ = A2UIPayloadRepair.Fix("""[{"text": "say \"hi\",", "n": 1,}]""");

        Assert.Equal("say \"hi\",", (string?)fixed_[0]!["text"]);
    }

    [Fact]
    public void Fix_ACodeFenceAroundTheBlock_IsStripped()
    {
        var fixed_ = A2UIPayloadRepair.Fix("```json\n[{\"a\": 1}]\n```");

        Assert.Equal(1, (int)fixed_[0]!["a"]!);
    }

    [Fact]
    public void Fix_TypographicQuotes_BecomeAsciiOnes()
    {
        var fixed_ = A2UIPayloadRepair.Fix("{“text”: “It’s fine”}");

        Assert.Equal("It's fine", (string?)fixed_[0]!["text"]);
    }

    [Fact]
    public void Fix_ASingleObject_IsWrappedInAnArray()
    {
        var fixed_ = A2UIPayloadRepair.Fix("""{"version": "v0.9.1", "deleteSurface": {"surfaceId": "s1"}}""");

        Assert.Single(fixed_);
    }

    [Fact]
    public void Fix_EmptyInput_Throws()
    {
        var thrown = Assert.Throws<A2UIParseException>(() => A2UIPayloadRepair.Fix("   "));

        Assert.Contains("empty", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Fix_UnrepairableJson_Throws()
    {
        Assert.Throws<A2UIParseException>(() => A2UIPayloadRepair.Fix("[{\"a\": }]"));
    }

    [Fact]
    public void Fix_AScalar_IsRejected()
    {
        Assert.Throws<A2UIParseException>(() => A2UIPayloadRepair.Fix("42"));
    }

    [Fact]
    public void ResponseParser_WithRepair_AcceptsATrailingComma()
    {
        var parts = A2UIResponseParser.Parse("Here:<a2ui-json>[{\"a\": 1,},]</a2ui-json>", repair: true);

        var part = Assert.Single(parts);
        Assert.Equal(1, (int)part.Messages![0]!["a"]!);
    }

    [Fact]
    public void ResponseParser_WithoutRepair_StillRejectsATrailingComma()
    {
        Assert.Throws<A2UIParseException>(() =>
            A2UIResponseParser.Parse("<a2ui-json>[{\"a\": 1,}]</a2ui-json>", repair: false));
    }

    [Fact]
    public void StreamParser_WithRepair_AcceptsATrailingCommaInsideAMessage()
    {
        var parser = new A2UIStreamParser(repair: true);

        var parts = new List<A2UIResponsePart>();
        parts.AddRange(parser.Feed("<a2ui-json>[{\"version\": \"v0.9.1\", \"deleteSurface\": {\"surfaceId\": \"s1\",},}"));
        parts.AddRange(parser.Feed("]</a2ui-json>"));
        parts.AddRange(parser.Complete());

        var messages = Assert.Single(parts, p => p.IsA2UI).Messages!;
        Assert.Equal("s1", (string?)messages[0]!["deleteSurface"]!["surfaceId"]);
        Assert.False(parser.IsInsideA2UIBlock);
    }

    [Fact]
    public void RemoveTrailingCommas_NestedStructures_AreAllCleaned()
    {
        var cleaned = A2UIPayloadRepair.RemoveTrailingCommas("{\"a\": [1, 2,], \"b\": {\"c\": 1,},}");

        Assert.NotNull(JsonNode.Parse(cleaned));
        Assert.DoesNotContain(",]", cleaned, StringComparison.Ordinal);
        Assert.DoesNotContain(",}", cleaned, StringComparison.Ordinal);
    }
}
