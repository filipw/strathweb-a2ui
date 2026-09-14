using System.Text.Json.Nodes;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Prompting;

namespace Strathweb.A2UI.Protocol.UnitTests;

public class SystemPromptTests
{
    private static readonly A2UICatalog Basic = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

    [Fact]
    public void Generate_OpensWithTheRoleAndStatesTheRules()
    {
        var prompt = A2UISystemPrompt.Generate(Basic, new A2UIPromptOptions { RoleDescription = "You book tables." });

        Assert.StartsWith("You book tables.", prompt, StringComparison.Ordinal);
        Assert.Contains("## Workflow Description:", prompt, StringComparison.Ordinal);
        Assert.Contains("The generated response MUST follow these rules:", prompt, StringComparison.Ordinal);
        Assert.Contains("<a2ui-json>", prompt, StringComparison.Ordinal);
        Assert.Contains("\"version\" property equal to \"v0.9.1\"", prompt, StringComparison.Ordinal);
        Assert.Contains(Basic.CatalogId, prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_TheCatalogsOwnInstructions_AreIncluded()
    {
        var prompt = A2UISystemPrompt.Generate(Basic, new A2UIPromptOptions { RoleDescription = "Role" });

        Assert.Contains("### Catalog Instructions:", prompt, StringComparison.Ordinal);
        Assert.Contains("REQUIRED PROPERTIES", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithoutASchema_TheSchemaSectionIsAbsent()
    {
        var prompt = A2UISystemPrompt.Generate(Basic, new A2UIPromptOptions { RoleDescription = "Role" });

        Assert.DoesNotContain("---BEGIN A2UI JSON SCHEMA---", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithASchema_EmbedsAllThreeDocuments()
    {
        var prompt = A2UISystemPrompt.Generate(Basic, new A2UIPromptOptions { RoleDescription = "Role", IncludeSchema = true });

        Assert.Contains("### Server To Client Schema:", prompt, StringComparison.Ordinal);
        Assert.Contains("### Common Types Schema:", prompt, StringComparison.Ordinal);
        Assert.Contains("### Catalog Schema:", prompt, StringComparison.Ordinal);
        Assert.Contains("\"ChoicePicker\":{", prompt, StringComparison.Ordinal);
        Assert.Contains("---END A2UI JSON SCHEMA---", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_AllowedComponents_ReduceTheCatalogSchema()
    {
        var prompt = A2UISystemPrompt.Generate(Basic, new A2UIPromptOptions
        {
            RoleDescription = "Role",
            IncludeSchema = true,
            AllowedComponents = ["Text", "Button"],
        });

        var catalogSection = prompt.Substring(prompt.IndexOf("### Catalog Schema:", StringComparison.Ordinal));
        Assert.Contains("\"Text\":{", catalogSection, StringComparison.Ordinal);
        Assert.Contains("\"Button\":{", catalogSection, StringComparison.Ordinal);
        Assert.DoesNotContain("\"ChoicePicker\":{", catalogSection, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_InlineCatalogs_AreMergedIntoTheSchema()
    {
        var inline = A2UICatalog.FromJson(new JsonObject
        {
            ["catalogId"] = "inline",
            ["components"] = new JsonObject { ["Gauge"] = new JsonObject { ["type"] = "object" } },
        });

        var prompt = A2UISystemPrompt.Generate(Basic, new A2UIPromptOptions
        {
            RoleDescription = "Role",
            IncludeSchema = true,
            InlineCatalogs = [inline],
        });

        Assert.Contains("\"Gauge\":{", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_Examples_AreDelimitedByName()
    {
        var prompt = A2UISystemPrompt.Generate(Basic, new A2UIPromptOptions
        {
            RoleDescription = "Role",
            IncludeExamples = true,
            Examples = [new A2UIPromptExample("survey", new JsonArray())],
        });

        Assert.Contains("### Examples:", prompt, StringComparison.Ordinal);
        Assert.Contains("---BEGIN survey---", prompt, StringComparison.Ordinal);
        Assert.Contains("---END survey---", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WorkflowAndUiDescriptions_AreUsedWhenGiven()
    {
        var prompt = A2UISystemPrompt.Generate(Basic, new A2UIPromptOptions
        {
            RoleDescription = "Role",
            WorkflowDescription = "Always show a form first.",
            UiDescription = "Use a card with a heading.",
        });

        Assert.Contains("Always show a form first.", prompt, StringComparison.Ordinal);
        Assert.Contains("## UI Description:", prompt, StringComparison.Ordinal);
        Assert.Contains("Use a card with a heading.", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_AnEmptyRole_IsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            A2UISystemPrompt.Generate(Basic, new A2UIPromptOptions { RoleDescription = " " }));
    }

    [Fact]
    public void Examples_FromDirectory_ReadOnlyJsonFilesInNameOrder()
    {
        var directory = Path.Combine(Path.GetTempPath(), "a2ui-examples-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "b.json"), "[]");
            File.WriteAllText(Path.Combine(directory, "a.json"), "{}");
            File.WriteAllText(Path.Combine(directory, "notes.txt"), "ignored");

            var examples = A2UIPromptExample.FromDirectory(directory);

            Assert.Equal(["a", "b"], examples.Select(e => e.Name));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Schemas_TheEmbeddedDocuments_MatchTheVendoredSpec()
    {
        var embedded = A2UISchemas.AgentToRenderer(A2UIVersion.V0_9_1);
        var vendored = JsonNode.Parse(File.ReadAllText(Path.Combine(SpecRoot(), "v0_9_1", "json", "server_to_client.json")));

        Assert.True(JsonNode.DeepEquals(vendored, embedded));
        Assert.Throws<NotSupportedException>(() => A2UISchemas.AgentToRenderer(A2UIVersion.V1_0));
    }

    private static string SpecRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "spec", "SPEC_VERSION")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory!.FullName, "spec");
    }
}
