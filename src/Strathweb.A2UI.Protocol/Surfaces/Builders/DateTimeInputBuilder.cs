using System.Text.Json.Nodes;
using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Surfaces;

/// <summary>A date and/or time picker.</summary>
public sealed class DateTimeInputBuilder : A2UICheckableComponentBuilder<DateTimeInputBuilder>
{
    internal DateTimeInputBuilder(A2UISurfaceBuilder surface)
        : base(surface, "DateTimeInput")
    {
    }

    /// <summary>Sets the label.</summary>
    /// <param name="label">The label.</param>
    /// <returns>This builder.</returns>
    public DateTimeInputBuilder Label(DynamicValue label) => Set("label", label);

    /// <summary>Binds the picker to the data model. Required by the catalog.</summary>
    /// <param name="value">Usually <see cref="Bind.Path"/>.</param>
    /// <returns>This builder.</returns>
    public DateTimeInputBuilder Value(DynamicValue value) => Set("value", value);

    /// <summary>Turns on the date part.</summary>
    /// <param name="enabled">Whether a date can be picked.</param>
    /// <returns>This builder.</returns>
    public DateTimeInputBuilder EnableDate(bool enabled = true) => Set("enableDate", JsonValue.Create(enabled));

    /// <summary>Turns on the time part.</summary>
    /// <param name="enabled">Whether a time can be picked.</param>
    /// <returns>This builder.</returns>
    public DateTimeInputBuilder EnableTime(bool enabled = true) => Set("enableTime", JsonValue.Create(enabled));
}
