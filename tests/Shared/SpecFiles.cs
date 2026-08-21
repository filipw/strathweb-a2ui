using System.Text.Json.Nodes;

namespace Strathweb.A2UI.TestSupport;

/// <summary>
/// Locates the vendored specification at test time. Tests run out of <c>bin/</c>, so the repository
/// root is found by walking up to the directory holding <c>spec/SPEC_VERSION</c>.
/// </summary>
internal static class SpecFiles
{
    internal static string SpecRoot { get; } = FindSpecRoot();

    internal static string V0_9_1 { get; } = Path.Combine(SpecRoot, "v0_9_1");

    internal static string Path_(params string[] parts) => Path.Combine([SpecRoot, .. parts]);

    internal static JsonNode ReadJson(params string[] parts) =>
        JsonNode.Parse(File.ReadAllText(Path_(parts)))
        ?? throw new InvalidOperationException($"'{Path_(parts)}' is empty.");

    private static string FindSpecRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = System.IO.Path.Combine(directory.FullName, "spec", "SPEC_VERSION");
            if (File.Exists(candidate))
            {
                return System.IO.Path.Combine(directory.FullName, "spec");
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not find the vendored spec/ directory above " + AppContext.BaseDirectory);
    }
}
