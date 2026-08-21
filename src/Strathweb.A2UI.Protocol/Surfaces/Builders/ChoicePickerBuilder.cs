using System.Text.Json.Nodes;
using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Surfaces;

/// <summary>A picker over a fixed set of choices.</summary>
public sealed class ChoicePickerBuilder : A2UICheckableComponentBuilder<ChoicePickerBuilder>
{
    internal ChoicePickerBuilder(A2UISurfaceBuilder surface)
        : base(surface, "ChoicePicker")
    {
    }

    /// <summary>Sets the label.</summary>
    /// <param name="label">The label.</param>
    /// <returns>This builder.</returns>
    public ChoicePickerBuilder Label(DynamicValue label) => Set("label", label);

    /// <summary>Sets the available choices. Required by the catalog.</summary>
    /// <param name="options">
    /// The choices. A <c>(label, value)</c> tuple converts to one, so these can be written inline.
    /// </param>
    /// <returns>This builder.</returns>
    public ChoicePickerBuilder Options(params A2UIChoice[] options)
    {
        Throw.IfNull(options, nameof(options));

        var array = new JsonArray();
        foreach (var option in options)
        {
            array.Add((JsonNode)option.ToJson());
        }

        return Set("options", array);
    }

    /// <summary>
    /// Binds the picker's selection to the data model. Required by the catalog, and the reason the
    /// user's answer reaches the agent at all.
    /// </summary>
    /// <param name="value">Usually <see cref="Bind.Path"/>.</param>
    /// <returns>This builder.</returns>
    public ChoicePickerBuilder Value(DynamicValue value) => Set("value", value);

    /// <summary>Sets whether one choice or several may be selected.</summary>
    /// <param name="variant">The selection mode.</param>
    /// <returns>This builder.</returns>
    public ChoicePickerBuilder Variant(ChoicePickerVariant variant) => Set("variant", variant.ToWireString());

    /// <summary>Allows only one choice.</summary>
    /// <returns>This builder.</returns>
    public ChoicePickerBuilder MutuallyExclusive() => Variant(ChoicePickerVariant.MutuallyExclusive);

    /// <summary>Allows several choices.</summary>
    /// <returns>This builder.</returns>
    public ChoicePickerBuilder MultipleSelection() => Variant(ChoicePickerVariant.MultipleSelection);

    /// <summary>Sets how the choices are drawn.</summary>
    /// <param name="displayStyle">The style.</param>
    /// <returns>This builder.</returns>
    public ChoicePickerBuilder DisplayStyle(ChoiceDisplayStyle displayStyle) =>
        Set("displayStyle", displayStyle.ToWireString());

    /// <summary>Lets the user filter a long list of choices.</summary>
    /// <param name="filterable">Whether filtering is offered.</param>
    /// <returns>This builder.</returns>
    public ChoicePickerBuilder Filterable(bool filterable = true) => Set("filterable", JsonValue.Create(filterable));
}
