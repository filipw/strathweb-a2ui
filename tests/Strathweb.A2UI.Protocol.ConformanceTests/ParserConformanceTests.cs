using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Strathweb.A2UI.Parsing;
using Strathweb.A2UI.Serialization;

namespace Strathweb.A2UI.Protocol.ConformanceTests;

/// <summary>Runs the vendored <c>parse_full</c> cases against <see cref="A2UIResponseParser"/>.</summary>
public class ParserConformanceTests
{
    public static TheoryData<string> Cases()
    {
        var data = new TheoryData<string>();
        foreach (var name in ConformanceSuite.Cases
                     .Where(c => c.Action == "parse_full")
                     .Select(c => c.Name)
                     .Order(StringComparer.Ordinal))
        {
            data.Add(name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ParseCase(string name)
    {
        var testCase = ConformanceSuite.Cases.Single(c => c.Name == name && c.Action == "parse_full");

        var skipReason = ConformanceSkips.Reason(testCase);
        Assert.SkipWhen(skipReason is not null, $"{name}: {skipReason}");

        var input = (string?)testCase.Source["input"] ?? string.Empty;
        var expectedError = testCase.Source["expect_error"];

        if (expectedError is not null)
        {
            var thrown = Assert.Throws<A2UIParseException>(() => A2UIResponseParser.Parse(input));
            var pattern = (string?)(expectedError as JsonObject)?["message"] ?? (string?)expectedError;

            Assert.True(
                pattern is null || Regex.IsMatch(thrown.Message, pattern, RegexOptions.CultureInvariant),
                $"{name}: expected a message matching /{pattern}/, got '{thrown.Message}'.");
            return;
        }

        var actual = ToConformanceShape(A2UIResponseParser.Parse(input));
        var expected = testCase.Source["expect"]!.AsArray();

        Assert.True(
            JsonNode.DeepEquals(expected, actual),
            $"""
             {name}
             expected: {expected.ToJsonString()}
             actual:   {actual.ToJsonString()}
             """);
    }

    /// <summary>
    /// The suite describes a part as <c>{text?, a2ui?}</c>. Text is omitted rather than empty on a
    /// trailing text-only part, and present-but-empty when a block had no prose before it.
    /// </summary>
    private static JsonArray ToConformanceShape(IReadOnlyList<A2UIResponsePart> parts)
    {
        var result = new JsonArray();
        foreach (var part in parts)
        {
            var entry = new JsonObject();
            if (part.Text is not null)
            {
                entry["text"] = part.Text;
            }

            if (part.Messages is not null)
            {
                entry["a2ui"] = part.Messages.DeepClone();
            }

            result.Add(entry);
        }

        return result;
    }
}
