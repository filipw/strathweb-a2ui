using System.Text.Json.Nodes;
using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Surfaces;

/// <summary>A single- or multi-line text input.</summary>
public sealed class TextFieldBuilder : A2UICheckableComponentBuilder<TextFieldBuilder>
{
    internal TextFieldBuilder(A2UISurfaceBuilder surface)
        : base(surface, "TextField")
    {
    }

    /// <summary>Sets the field's label. Required by the catalog.</summary>
    /// <param name="label">The label.</param>
    /// <returns>This builder.</returns>
    public TextFieldBuilder Label(DynamicValue label) => Set("label", label);

    /// <summary>Binds the field to a place in the data model.</summary>
    /// <param name="value">Usually <see cref="Bind.Path"/>, so the user's input lands somewhere.</param>
    /// <returns>This builder.</returns>
    public TextFieldBuilder Value(DynamicValue value) => Set("value", value);

    /// <summary>Sets what kind of text the field accepts.</summary>
    /// <param name="variant">The kind.</param>
    /// <returns>This builder.</returns>
    public TextFieldBuilder Variant(TextFieldVariant variant) => Set("variant", variant.ToWireString());

    /// <summary>Sets a regular expression the renderer checks input against.</summary>
    /// <param name="pattern">The pattern.</param>
    /// <returns>This builder.</returns>
    public TextFieldBuilder ValidationRegexp(string pattern) =>
        Set("validationRegexp", JsonValue.Create(Throw.IfNull(pattern, nameof(pattern))));
}
