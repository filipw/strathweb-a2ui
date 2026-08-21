using Strathweb.A2UI.Parsing;

namespace Strathweb.A2UI.Protocol.ConformanceTests;

/// <summary>
/// Runs the vendored <c>has_parts</c> cases against <see cref="A2UIResponseParser.ContainsA2UIBlock"/>.
/// </summary>
public class HasPartsConformanceTests
{
    public static TheoryData<string> Cases()
    {
        var data = new TheoryData<string>();
        foreach (var name in ConformanceSuite.Cases
                     .Where(c => c.Action == "has_parts")
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

        Assert.Equal(
            (bool)testCase.Source["expect"]!,
            A2UIResponseParser.ContainsA2UIBlock((string?)testCase.Source["input"] ?? string.Empty));
    }
}
