using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Surfaces;

/// <summary>A checkbox bound to a boolean in the data model.</summary>
public sealed class CheckBoxBuilder : A2UICheckableComponentBuilder<CheckBoxBuilder>
{
    internal CheckBoxBuilder(A2UISurfaceBuilder surface)
        : base(surface, "CheckBox")
    {
    }

    /// <summary>Sets the label. Required by the catalog.</summary>
    /// <param name="label">The label.</param>
    /// <returns>This builder.</returns>
    public CheckBoxBuilder Label(DynamicValue label) => Set("label", label);

    /// <summary>Binds the checkbox to a boolean in the data model. Required by the catalog.</summary>
    /// <param name="value">Usually <see cref="Bind.Path"/>.</param>
    /// <returns>This builder.</returns>
    public CheckBoxBuilder Value(DynamicValue value) => Set("value", value);
}
