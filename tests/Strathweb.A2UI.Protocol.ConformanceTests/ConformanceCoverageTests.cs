using System.Globalization;
using System.Text;

namespace Strathweb.A2UI.Protocol.ConformanceTests;

/// <summary>Accounts for every case in the vendored suite, run or not.</summary>
public class ConformanceCoverageTests
{
    [Fact]
    public void EveryCaseIsEitherRunOrSkippedForAStatedReason()
    {
        var unaccounted = ConformanceSuite.Cases
            .Where(c => ConformanceSkips.Reason(c) is null &&
                        !ConformanceSkips.SupportedActions.Contains(c.Action))
            .Select(c => $"{c.File}:{c.Name} (action '{c.Action}')")
            .ToList();

        Assert.True(
            unaccounted.Count == 0,
            $"These cases are neither run nor skipped for a reason:{Environment.NewLine}" +
            string.Join(Environment.NewLine, unaccounted));
    }

    [Fact]
    public void WriteCoverageSummary()
    {
        var runners = new Dictionary<string, int>(StringComparer.Ordinal);
        var skips = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var testCase in ConformanceSuite.Cases)
        {
            if (ConformanceSkips.Reason(testCase) is { } reason)
            {
                skips[reason] = skips.TryGetValue(reason, out var count) ? count + 1 : 1;
                continue;
            }

            runners[testCase.Action] = runners.TryGetValue(testCase.Action, out var run) ? run + 1 : 1;
        }

        var summary = new StringBuilder()
            .AppendLine("# A2UI conformance coverage")
            .AppendLine()
            .AppendLine(CultureInfo.InvariantCulture, $"Vendored cases: {ConformanceSuite.Cases.Count}")
            .AppendLine(CultureInfo.InvariantCulture, $"Run: {runners.Values.Sum()}")
            .AppendLine(CultureInfo.InvariantCulture, $"Skipped: {skips.Values.Sum()}")
            .AppendLine()
            .AppendLine("## Run");

        foreach (var pair in runners.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            summary.AppendLine(CultureInfo.InvariantCulture, $"- `{pair.Key}`: {pair.Value}");
        }

        summary.AppendLine().AppendLine("## Skipped");

        foreach (var pair in skips.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal))
        {
            summary.AppendLine(CultureInfo.InvariantCulture, $"- {pair.Value}: {pair.Key}");
        }

        var path = Path.Combine(AppContext.BaseDirectory, "conformance-coverage.md");
        File.WriteAllText(path, summary.ToString());

        Assert.True(File.Exists(path));
        Assert.NotEmpty(runners);
    }
}
