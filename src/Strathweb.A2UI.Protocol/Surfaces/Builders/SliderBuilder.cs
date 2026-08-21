using System.Text.Json.Nodes;
using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Surfaces;

/// <summary>A slider over a numeric range.</summary>
public sealed class SliderBuilder : A2UICheckableComponentBuilder<SliderBuilder>
{
    internal SliderBuilder(A2UISurfaceBuilder surface)
        : base(surface, "Slider")
    {
    }

    /// <summary>Sets the label.</summary>
    /// <param name="label">The label.</param>
    /// <returns>This builder.</returns>
    public SliderBuilder Label(DynamicValue label) => Set("label", label);

    /// <summary>Binds the slider to a number in the data model. Required by the catalog.</summary>
    /// <param name="value">Usually <see cref="Bind.Path"/>.</param>
    /// <returns>This builder.</returns>
    public SliderBuilder Value(DynamicValue value) => Set("value", value);

    /// <summary>Sets the lowest selectable value.</summary>
    /// <param name="min">The minimum.</param>
    /// <returns>This builder.</returns>
    public SliderBuilder Min(double min) => Set("min", JsonValue.Create(min));

    /// <summary>Sets the highest selectable value. Required by the catalog.</summary>
    /// <param name="max">The maximum.</param>
    /// <returns>This builder.</returns>
    public SliderBuilder Max(double max) => Set("max", JsonValue.Create(max));
}
