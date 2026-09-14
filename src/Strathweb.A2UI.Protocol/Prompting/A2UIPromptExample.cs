using System.Text.Json.Nodes;
using Strathweb.A2UI.Internal;

namespace Strathweb.A2UI.Prompting;

/// <summary>A worked example shown to the model: a name and the A2UI payload it should learn from.</summary>
public sealed class A2UIPromptExample
{
    /// <summary>Creates an example.</summary>
    /// <param name="name">A short name, such as a file stem. Appears in the delimiters around the example.</param>
    /// <param name="payload">The example payload, typically a message array.</param>
    public A2UIPromptExample(string name, JsonNode payload)
    {
        Name = Throw.IfNullOrEmpty(name, nameof(name));
        Payload = Throw.IfNull(payload, nameof(payload));
    }

    /// <summary>The example's name.</summary>
    public string Name { get; }

    /// <summary>The example payload.</summary>
    public JsonNode Payload { get; }

    /// <summary>
    /// Loads every <c>*.json</c> file in a directory as an example named by its file stem, in
    /// ordinal name order. Other files are ignored.
    /// </summary>
    /// <param name="directory">The directory to read.</param>
    /// <returns>The examples.</returns>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    public static IReadOnlyList<A2UIPromptExample> FromDirectory(string directory)
    {
        Throw.IfNullOrEmpty(directory, nameof(directory));

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"The examples directory '{directory}' does not exist.");
        }

        var files = Directory.GetFiles(directory, "*.json");
        Array.Sort(files, StringComparer.Ordinal);

        var examples = new List<A2UIPromptExample>(files.Length);
        foreach (var file in files)
        {
            var payload = JsonNode.Parse(File.ReadAllText(file))
                ?? throw new InvalidOperationException($"The example '{file}' is empty.");

            examples.Add(new A2UIPromptExample(Path.GetFileNameWithoutExtension(file), payload));
        }

        return examples;
    }
}
