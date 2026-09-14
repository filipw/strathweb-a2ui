using System.Text.Json.Nodes;
using Strathweb.A2UI.Parsing;

namespace Strathweb.A2UI.Protocol.ConformanceTests;

/// <summary>Runs the vendored <c>fix_payload</c> cases against <see cref="A2UIPayloadRepair"/>.</summary>
public class FixPayloadConformanceTests
{
    public static TheoryData<string> Cases()
    {
        var data = new TheoryData<string>();
        foreach (var name in ConformanceSuite.Cases
                     .Where(c => c.Action == "fix_payload")
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

        var actual = A2UIPayloadRepair.Fix((string?)testCase.Source["input"] ?? string.Empty);
        var expected = testCase.Source["expect"]!;

        Assert.True(
            JsonNode.DeepEquals(expected, actual),
            $"""
             {name}
             expected: {expected.ToJsonString()}
             actual:   {actual.ToJsonString()}
             """);
    }
}
