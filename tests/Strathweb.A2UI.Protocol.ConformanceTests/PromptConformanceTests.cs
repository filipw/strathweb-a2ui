using System.Text.Json.Nodes;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Prompting;

namespace Strathweb.A2UI.Protocol.ConformanceTests;

/// <summary>Runs the vendored <c>generate_prompt</c> cases against <see cref="A2UISystemPrompt"/>.</summary>
public class PromptConformanceTests
{
    public static TheoryData<string> Cases()
    {
        var data = new TheoryData<string>();
        foreach (var name in ConformanceSuite.Cases
                     .Where(c => c.Action == "generate_prompt")
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

        var args = testCase.Args;
        var capabilities = args["client_ui_capabilities"] as JsonObject ?? [];

        var options = new A2UIPromptOptions
        {
            RoleDescription = (string?)args["role_description"] ?? string.Empty,
            WorkflowDescription = (string?)args["workflow_description"],
            UiDescription = (string?)args["ui_description"],
            IncludeSchema = (bool?)args["include_schema"] ?? false,
            IncludeExamples = (bool?)args["include_examples"] ?? false,
            AllowedComponents = (args["allowed_components"] as JsonArray)?.Select(c => (string)c!).ToList(),
            Version = A2UIVersion.V0_9_1,
        };

        if ((string?)args["examples_path"] is { } examplesPath)
        {
            options.Examples = A2UIPromptExample.FromDirectory(Path.Combine(ConformanceSuite.Root, examplesPath));
        }

        if (((bool?)args["accepts_inline_catalogs"] ?? false) && capabilities["inlineCatalogs"] is JsonArray inline)
        {
            options.InlineCatalogs = inline.Select(c => A2UICatalog.FromJson(c)).ToList();
        }

        var prompt = A2UISystemPrompt.Generate(A2UICatalogs.Basic(A2UIVersion.V0_9_1), options);

        foreach (var expected in testCase.Source["expect_contains"]!.AsArray().Select(e => (string)e!))
        {
            Assert.True(
                prompt.Contains(expected, StringComparison.Ordinal),
                $"{name}: the prompt does not contain '{expected}'.");
        }
    }
}
