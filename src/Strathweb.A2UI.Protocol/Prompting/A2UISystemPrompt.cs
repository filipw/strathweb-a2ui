using System.Text;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Internal;

namespace Strathweb.A2UI.Prompting;

/// <summary>
/// Builds the system prompt for prompt-first generation, where the model writes A2UI JSON itself
/// instead of a tool building it. The shape follows the reference SDK so material written for one
/// agent SDK reads the same to a model here.
/// </summary>
public static class A2UISystemPrompt
{
    /// <summary>The tag the model must open an A2UI block with.</summary>
    public const string OpenTag = Parsing.A2UIResponseTags.Open;

    /// <summary>The tag the model must close an A2UI block with.</summary>
    public const string CloseTag = Parsing.A2UIResponseTags.Close;

    /// <summary>Generates the prompt.</summary>
    /// <param name="catalog">The catalog the model generates from.</param>
    /// <param name="options">What to include.</param>
    /// <returns>The prompt text.</returns>
    /// <exception cref="ArgumentException"><see cref="A2UIPromptOptions.RoleDescription"/> is empty.</exception>
    public static string Generate(A2UICatalog catalog, A2UIPromptOptions options)
    {
        Throw.IfNull(catalog, nameof(catalog));
        Throw.IfNull(options, nameof(options));

        if (string.IsNullOrWhiteSpace(options.RoleDescription))
        {
            throw new ArgumentException("A role description is required.", nameof(options));
        }

        var version = A2UIVersions.ToWireString(options.Version);
        var prompt = new StringBuilder();

        prompt.AppendLine(options.RoleDescription.Trim());
        prompt.AppendLine();

        prompt.AppendLine("## Workflow Description:");
        prompt.AppendLine((options.WorkflowDescription ?? DefaultWorkflow).Trim());
        prompt.AppendLine();

        prompt.AppendLine("The generated response MUST follow these rules:");
        AppendRules(prompt, catalog, version);

        if (catalog.Instructions is { Length: > 0 } instructions)
        {
            prompt.AppendLine();
            prompt.AppendLine("### Catalog Instructions:");
            prompt.AppendLine(instructions.Trim());
        }

        if (options.UiDescription is { Length: > 0 } ui)
        {
            prompt.AppendLine();
            prompt.AppendLine("## UI Description:");
            prompt.AppendLine(ui.Trim());
        }

        if (options.IncludeSchema)
        {
            prompt.AppendLine();
            AppendSchemas(prompt, Effective(catalog, options), options.Version);
        }

        if (options.IncludeExamples && options.Examples.Count > 0)
        {
            prompt.AppendLine();
            AppendExamples(prompt, options.Examples);
        }

        return prompt.ToString().TrimEnd() + Environment.NewLine;
    }

    private const string DefaultWorkflow =
        "Help the user with their request. When a form, a list, a card, or another visual would serve " +
        "the user better than prose, respond with an A2UI surface. Keep any prose around it to one " +
        "short sentence, and do not describe in words what the surface already shows.";

    private static void AppendRules(StringBuilder prompt, A2UICatalog catalog, string version)
    {
        var rules = new[]
        {
            $"Wrap every A2UI payload in {OpenTag} and {CloseTag} tags, with nothing but JSON between them. " +
            "Text outside the tags is shown to the user as prose.",
            "The payload is a JSON array of message envelopes. Each envelope has a \"version\" property " +
            $"equal to \"{version}\" and exactly one of: \"createSurface\", \"updateComponents\", " +
            "\"updateDataModel\", \"deleteSurface\".",
            "To show a new surface, send in this order: \"createSurface\" with a unique \"surfaceId\" and " +
            $"\"catalogId\": \"{catalog.CatalogId}\"; then \"updateComponents\" with the complete component " +
            "list; then \"updateDataModel\" with the initial data when any component binds to it.",
            "Components form a flat list, not a tree. Every component has a unique string \"id\" and a " +
            "\"component\" type taken from the catalog. Exactly one component has the id \"root\". A " +
            "component refers to its children by id, through \"child\" or \"children\".",
            "Order components top-down: the \"root\" component first, and every parent before its " +
            "children, so the renderer can draw the surface while it is still arriving.",
            "Use only component types and properties the catalog schema defines, spelled exactly as " +
            "defined. Do not invent properties, and use only the listed values for enumerated ones.",
            "A value the user can change, or that comes from data, is a binding: {\"path\": \"/some/key\"}. " +
            "Put the initial values in \"updateDataModel\" with \"path\": \"/\" and the whole model as \"value\".",
            "A button's \"action\" is {\"event\": {\"name\": \"...\", \"context\": {...}}}. Put the bound " +
            "values the agent needs into the context so they come back with the event.",
            "To change a surface that is already shown, send \"updateDataModel\" for changed data or " +
            "\"updateComponents\" for replaced components with the same ids. Do not resend the whole surface. " +
            "Send \"deleteSurface\" to remove one.",
            "Write plain JSON: no markdown code fences, no comments, no trailing commas, straight double quotes.",
        };

        for (var i = 0; i < rules.Length; i++)
        {
            prompt.Append(i + 1).Append(". ").AppendLine(rules[i]);
        }
    }

    private static A2UICatalog Effective(A2UICatalog catalog, A2UIPromptOptions options)
    {
        var effective = catalog;

        foreach (var inline in options.InlineCatalogs)
        {
            effective = effective.WithComponentsFrom(inline);
        }

        if (options.AllowedComponents is { } allowed)
        {
            effective = effective.WithOnlyComponents(allowed);
        }

        return effective;
    }

    private static void AppendSchemas(StringBuilder prompt, A2UICatalog catalog, A2UIVersion version)
    {
        prompt.AppendLine("---BEGIN A2UI JSON SCHEMA---");

        // Compact, as the reference SDK writes it: the schemas are large and the model does not need
        // the whitespace.
        prompt.AppendLine("### Server To Client Schema:");
        prompt.AppendLine(A2UISchemas.AgentToRenderer(version).ToJsonString());
        prompt.AppendLine();

        prompt.AppendLine("### Common Types Schema:");
        prompt.AppendLine(A2UISchemas.CommonTypes(version).ToJsonString());
        prompt.AppendLine();

        prompt.AppendLine("### Catalog Schema:");
        prompt.AppendLine(catalog.ToJson().ToJsonString());

        prompt.AppendLine("---END A2UI JSON SCHEMA---");
    }

    private static void AppendExamples(StringBuilder prompt, IReadOnlyList<A2UIPromptExample> examples)
    {
        prompt.AppendLine("### Examples:");

        foreach (var example in examples)
        {
            prompt.Append("---BEGIN ").Append(example.Name).AppendLine("---");
            prompt.AppendLine(example.Payload.ToJsonString());
            prompt.Append("---END ").Append(example.Name).AppendLine("---");
        }
    }
}
