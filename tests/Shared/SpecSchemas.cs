using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

namespace Strathweb.A2UI.TestSupport;

/// <summary>The vendored v0.9.1 JSON Schemas, wired up so that cross-file <c>$ref</c>s resolve offline.</summary>
internal static class SpecSchemas
{
    private const string BaseUri = "https://a2ui.org/specification/v0_9/";

    private static readonly Lazy<Loaded> Schemas = new(Load);

    internal static JsonSchema AgentToRenderer => Schemas.Value.AgentToRenderer;

    internal static JsonSchema RendererToAgent => Schemas.Value.RendererToAgent;

    /// <summary>Asserts that <paramref name="message"/> validates against the vendored schema.</summary>
    internal static void AssertValid(JsonSchema schema, JsonNode message)
    {
        using var document = JsonDocument.Parse(message.ToJsonString());
        var results = schema.Evaluate(
            document.RootElement,
            new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical, IncludeApplicatorErrors = true });

        Assert.True(
            results.IsValid,
            $"Schema validation failed for:{Environment.NewLine}{message.ToJsonString(Indented)}" +
            $"{Environment.NewLine}{Describe(results)}");
    }

    /// <summary>Asserts that <paramref name="message"/> is rejected by the vendored schema.</summary>
    internal static void AssertInvalid(JsonSchema schema, JsonNode message)
    {
        using var document = JsonDocument.Parse(message.ToJsonString());
        var results = schema.Evaluate(document.RootElement, new EvaluationOptions());

        Assert.False(
            results.IsValid,
            $"Expected schema validation to fail for:{Environment.NewLine}{message.ToJsonString(Indented)}");
    }

    private static JsonSerializerOptions Indented { get; } = new() { WriteIndented = true };

    private static string Describe(EvaluationResults results)
    {
        var lines = new List<string>();
        Collect(results, lines);
        return lines.Count == 0 ? "(no detail)" : string.Join(Environment.NewLine, lines);

        static void Collect(EvaluationResults node, List<string> into)
        {
            foreach (var error in node.Errors ?? [])
            {
                into.Add($"  {node.InstanceLocation}: {error.Key}: {error.Value}");
            }

            foreach (var child in node.Details ?? [])
            {
                Collect(child, into);
            }
        }
    }

    private static Loaded Load()
    {
        var registry = new SchemaRegistry();
        var buildOptions = new BuildOptions { SchemaRegistry = registry };

        JsonSchema Register(string path, string relativeUri)
        {
            var schema = JsonSchema.FromText(File.ReadAllText(path), buildOptions, new Uri(BaseUri + relativeUri));
            registry.Register(new Uri(BaseUri + relativeUri), schema);
            return schema;
        }

        Register(Path.Combine(SpecFiles.V0_9_1, "json", "common_types.json"), "common_types.json");

        var catalog = Register(
            Path.Combine(SpecFiles.V0_9_1, "catalogs", "basic", "catalog.json"),
            "catalog.json");
        registry.Register(new Uri(BaseUri + "catalogs/basic/catalog.json"), catalog);

        return new Loaded(
            Register(Path.Combine(SpecFiles.V0_9_1, "json", "server_to_client.json"), "server_to_client.json"),
            Register(Path.Combine(SpecFiles.V0_9_1, "json", "client_to_server.json"), "client_to_server.json"));
    }

    private sealed record Loaded(JsonSchema AgentToRenderer, JsonSchema RendererToAgent);
}
