using System.Text;
using System.Text.Json.Nodes;
using Strathweb.A2UI.Parsing;
using Strathweb.A2UI.Serialization;

namespace Strathweb.A2UI.Protocol.UnitTests;

public class StreamParserTests
{
    private const string Response =
        "Here is your form. <a2ui-json>[" +
        """{"version":"v0.9.1","createSurface":{"surfaceId":"s1","catalogId":"c"}},""" +
        """{"version":"v0.9.1","updateComponents":{"surfaceId":"s1","components":[""" +
        """{"id":"root","component":"Text","text":"Say \"hi\" \\ ok"}]}}""" +
        "]</a2ui-json> All done.";

    [Fact]
    public void Feed_WholeResponseAtOnce_YieldsTextThenMessagesThenText()
    {
        var parser = new A2UIStreamParser();

        var parts = parser.Feed(Response).Concat(parser.Complete()).ToList();

        Assert.Equal("Here is your form. ", parts[0].Text);
        Assert.Equal(2, parts[1].Messages!.Count);
        Assert.Equal(" All done.", parts[2].Text);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(7)]
    public void Feed_InFixedSizeChunks_ProducesTheSameResult(int chunkSize)
    {
        var expected = Collect([Response]);
        var actual = Collect(Split(Response, chunkSize));

        Assert.True(
            JsonNode.DeepEquals(expected, actual),
            $"""
             chunk size {chunkSize}
             expected: {expected.ToJsonString()}
             actual:   {actual.ToJsonString()}
             """);
    }

    [Fact]
    public void Feed_SplitAtEveryPossibleBoundary_ProducesTheSameResult()
    {
        // A chunk boundary can land anywhere: mid-delimiter, mid-string, or between a backslash and
        // the character it escapes. Every one of those splits must behave like no split at all.
        var expected = Collect([Response]);

        for (var i = 1; i < Response.Length; i++)
        {
            var actual = Collect([Response.Substring(0, i), Response.Substring(i)]);
            Assert.True(JsonNode.DeepEquals(expected, actual), $"Split at index {i} changed the result.");
        }
    }

    [Fact]
    public void Feed_MessagesArriveAsSoonAsTheyClose_NotAtTheEndOfTheBlock()
    {
        var parser = new A2UIStreamParser();

        // The block is still open and the array unterminated, but the first message is complete.
        var parts = parser.Feed(
            """<a2ui-json>[{"version":"v0.9.1","createSurface":{"surfaceId":"s1","catalogId":"c"}}""");

        Assert.Single(parts);
        Assert.Single(parts[0].Messages!);
        Assert.True(parser.IsInsideA2UIBlock);
    }

    [Fact]
    public void Feed_TextThatCouldStartTheOpeningTag_IsHeldBackUntilItIsSettled()
    {
        var parser = new A2UIStreamParser();

        Assert.Empty(parser.Feed("<a2ui"));
        Assert.Equal("<a2ui is a tag", parser.Feed(" is a tag").Concat(parser.Complete()).Single().Text);
    }

    [Fact]
    public void Feed_MarkdownFence_IsNotMistakenForContent()
    {
        var parts = A2UIResponseParser.Parse(
            "Text\n<a2ui-json>\n```json\n[{\"version\":\"v0.9.1\",\"deleteSurface\":{\"surfaceId\":\"s1\"}}]\n```\n</a2ui-json>");

        Assert.Single(parts[0].Messages!);
    }

    [Fact]
    public void Feed_PastTheBufferLimit_ThrowsRatherThanGrowingForever()
    {
        var parser = new A2UIStreamParser(maxBufferLength: 64);

        var thrown = Assert.Throws<A2UIParseException>(
            () => parser.Feed("<a2ui-json>[" + new string('x', 200)));

        Assert.Contains("64 character limit", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Complete_InsideAnUnclosedBlock_Throws()
    {
        var parser = new A2UIStreamParser();
        parser.Feed("""<a2ui-json>[{"version":"v0.9.1","deleteSurface":{"surfaceId":"s1"}}""");

        Assert.Throws<A2UIParseException>(() => parser.Complete());
    }

    [Fact]
    public void Feed_InvalidJsonInsideABlock_Throws()
    {
        var parser = new A2UIStreamParser();

        Assert.Throws<A2UIParseException>(() => parser.Feed("<a2ui-json>[{\"a\": }]"));
    }

    [Fact]
    public void Feed_MultipleBlocks_AreEachReported()
    {
        var parser = new A2UIStreamParser();

        var parts = parser
            .Feed("""a<a2ui-json>[{"id":"1"}]</a2ui-json>b<a2ui-json>[{"id":"2"}]</a2ui-json>c""")
            .Concat(parser.Complete())
            .ToList();

        Assert.Equal(2, parts.Count(p => p.IsA2UI));
        Assert.Equal(["a", "b", "c"], parts.Where(p => !p.IsA2UI).Select(p => p.Text));
    }

    private static JsonArray Collect(IEnumerable<string> chunks)
    {
        var parser = new A2UIStreamParser();
        var parts = new List<A2UIResponsePart>();

        foreach (var chunk in chunks)
        {
            parts.AddRange(parser.Feed(chunk));
        }

        parts.AddRange(parser.Complete());

        // How parts are grouped depends on where the chunk boundaries fell; the sequence of text and
        // of messages does not. Merge adjacent runs of each so only the sequence is compared.
        var result = new JsonArray();
        var text = new StringBuilder();
        JsonArray? messages = null;

        void Flush()
        {
            if (text.Length > 0)
            {
                result.Add(new JsonObject { ["text"] = text.ToString() });
                text.Clear();
            }

            if (messages is not null)
            {
                result.Add(new JsonObject { ["a2ui"] = messages });
                messages = null;
            }
        }

        foreach (var part in parts)
        {
            if (part.IsA2UI)
            {
                if (text.Length > 0)
                {
                    Flush();
                }

                messages ??= [];
                foreach (var message in part.Messages!)
                {
                    messages.Add(message!.DeepClone());
                }
            }
            else
            {
                if (messages is not null)
                {
                    Flush();
                }

                text.Append(part.Text);
            }
        }

        Flush();
        return result;
    }

    private static IEnumerable<string> Split(string value, int size)
    {
        for (var i = 0; i < value.Length; i += size)
        {
            yield return value.Substring(i, Math.Min(size, value.Length - i));
        }
    }
}
