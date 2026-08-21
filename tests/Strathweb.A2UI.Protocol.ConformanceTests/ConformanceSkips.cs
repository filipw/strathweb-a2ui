namespace Strathweb.A2UI.Protocol.ConformanceTests;

/// <summary>Decides which vendored cases this library does not attempt, and why.</summary>
internal static class ConformanceSkips
{
    /// <summary>The actions this library implements a runner for.</summary>
    internal static readonly HashSet<string> SupportedActions = new(StringComparer.Ordinal)
    {
        "validate",
        "parse_full",
        "has_parts",
        "create_a2ui_part",
        "is_a2ui_part",
        "try_activate",
        "try_activate_extension",
        "select_newest",
    };

    internal static string? Reason(ConformanceCase testCase)
    {
        if (ConformanceSuite.SkipList.TryGetValue(testCase.Name, out var reason))
        {
            return reason;
        }

        if (testCase.Version is "0.8")
        {
            return "v0.8 is not a supported protocol version.";
        }

        if (testCase.Version is "1.0")
        {
            return "v1.0 is not implemented; only the v0.9.1 profile ships.";
        }

        if (!SupportedActions.Contains(testCase.Action))
        {
            return $"the '{testCase.Action}' action exercises a feature this library does not provide.";
        }

        return null;
    }
}
