using System.Text.Json.Nodes;

namespace Strathweb.A2UI.Protocol.ConformanceTests;

/// <summary>The vendored conformance cases, loaded once.</summary>
internal static class ConformanceSuite
{
    internal static string Root { get; } = FindConformanceRoot();

    internal static IReadOnlyList<ConformanceCase> Cases { get; } = LoadCases();

    internal static IReadOnlyDictionary<string, string> SkipList { get; } = LoadSkipList();

    /// <summary>Resolves a path a case gives relative to the conformance directory.</summary>
    internal static JsonNode ReadReferencedFile(string relativePath) =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(Root, relativePath)))
        ?? throw new InvalidOperationException($"'{relativePath}' is empty.");

    private static List<ConformanceCase> LoadCases()
    {
        var cases = new List<ConformanceCase>();

        foreach (var file in Directory
                     .EnumerateFiles(Root, "*.yaml", SearchOption.AllDirectories)
                     .Order(StringComparer.Ordinal))
        {
            var relative = Path.GetRelativePath(Root, file).Replace('\\', '/');
            foreach (var node in YamlToJson.LoadDocuments(file))
            {
                if (node is JsonObject obj)
                {
                    cases.Add(new ConformanceCase(relative, obj));
                }
            }
        }

        return cases;
    }

    private static Dictionary<string, string> LoadSkipList()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "skip-list.yaml");
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        if (!File.Exists(path))
        {
            return result;
        }

        foreach (var entry in YamlToJson.LoadDocuments(path))
        {
            if (entry is not JsonObject obj ||
                (string?)obj["name"] is not { } name ||
                (string?)obj["reason"] is not { } reason)
            {
                throw new InvalidOperationException("Every skip-list entry needs a 'name' and a 'reason'.");
            }

            result[name] = reason;
        }

        return result;
    }

    private static string FindConformanceRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "spec", "conformance");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not find spec/conformance above " + AppContext.BaseDirectory);
    }
}
