using Strathweb.A2UI.Catalogs;

namespace Strathweb.A2UI.Validation;

/// <summary>Knobs for <see cref="A2UIValidator"/>.</summary>
public sealed class A2UIValidationOptions
{
    /// <summary>The strict defaults.</summary>
    public static A2UIValidationOptions Default { get; } = new();

    /// <summary>The protocol version rules to apply. Defaults to v0.9.1.</summary>
    public A2UIVersionProfile Profile { get; init; } = A2UIVersionProfile.Default;

    /// <summary>
    /// The catalog whose components the payload may name. When null, catalog checks are skipped and
    /// reference detection falls back to <c>child</c> and <c>children</c>.
    /// </summary>
    public A2UICatalog? Catalog { get; init; }

    /// <summary>The id the root component must have. Defaults to <c>root</c>.</summary>
    public string RootComponentId { get; init; } = "root";

    /// <summary>
    /// The nesting limit applied to both the JSON payload and the component graph. Defaults to 50,
    /// the limit the conformance suite asserts.
    /// </summary>
    public int MaxDepth { get; init; } = 50;

    /// <summary>The tighter limit on nested function calls. Defaults to 5.</summary>
    public int MaxFunctionCallDepth { get; init; } = 5;

    /// <summary>Whether components no path from the root reaches are permitted.</summary>
    public bool AllowOrphanComponents { get; init; }

    /// <summary>Whether references to components this payload does not define are permitted.</summary>
    public bool AllowDanglingReferences { get; init; }

    /// <summary>
    /// Whether a payload may omit the root component. When null, the default, the validator decides
    /// per payload: one that creates a surface must define its root, an incremental update need not.
    /// </summary>
    public bool? AllowMissingRoot { get; init; }

    /// <summary>Whether component properties are checked against the catalog's component schemas.</summary>
    public bool CheckCatalogConformance { get; init; } = true;
}
