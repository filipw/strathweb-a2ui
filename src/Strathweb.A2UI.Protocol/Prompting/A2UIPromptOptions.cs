using Strathweb.A2UI.Catalogs;

namespace Strathweb.A2UI.Prompting;

/// <summary>What goes into the system prompt that teaches a model to emit A2UI directly.</summary>
public sealed class A2UIPromptOptions
{
    /// <summary>Who the agent is and what it does for the user. Required; it opens the prompt.</summary>
    public string RoleDescription { get; set; } = string.Empty;

    /// <summary>
    /// How the agent should decide when to show UI and what to say around it. When
    /// <see langword="null"/>, a general description is used.
    /// </summary>
    public string? WorkflowDescription { get; set; }

    /// <summary>What the UI should look like or contain, in the agent author's words. Omitted when null.</summary>
    public string? UiDescription { get; set; }

    /// <summary>
    /// Whether to embed the message schema, the common types and the catalog schema. Large, but it
    /// is what lets a model produce valid payloads for a catalog it has never seen.
    /// </summary>
    public bool IncludeSchema { get; set; }

    /// <summary>Whether to embed <see cref="Examples"/>.</summary>
    public bool IncludeExamples { get; set; }

    /// <summary>Worked examples, shown when <see cref="IncludeExamples"/> is set.</summary>
    public IReadOnlyList<A2UIPromptExample> Examples { get; set; } = [];

    /// <summary>
    /// The only components the model may use. When set, the catalog schema in the prompt is reduced
    /// to these. <see langword="null"/> allows the whole catalog.
    /// </summary>
    public IReadOnlyList<string>? AllowedComponents { get; set; }

    /// <summary>
    /// Catalogs the renderer sent inline, merged into the catalog schema shown to the model. Only
    /// pass these when the agent accepts inline catalogs; they are third-party input.
    /// </summary>
    public IReadOnlyList<A2UICatalog> InlineCatalogs { get; set; } = [];

    /// <summary>The protocol version to write and to fetch schemas for. Defaults to v0.9.1.</summary>
    public A2UIVersion Version { get; set; } = A2UIVersion.V0_9_1;
}
