using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Surfaces;

/// <summary>A button. Its label is a child component, usually a <c>Text</c>.</summary>
public sealed class ButtonBuilder : A2UICheckableComponentBuilder<ButtonBuilder>
{
    internal ButtonBuilder(A2UISurfaceBuilder surface)
        : base(surface, "Button")
    {
    }

    /// <summary>Sets the button's label component.</summary>
    /// <param name="child">The component drawn inside the button.</param>
    /// <returns>This builder.</returns>
    public ButtonBuilder Child(A2UIComponentBuilder child) => Reference("child", child);

    /// <summary>Sets how prominent the button is.</summary>
    /// <param name="variant">The style.</param>
    /// <returns>This builder.</returns>
    public ButtonBuilder Variant(ButtonVariant variant) => Set("variant", variant.ToWireString());

    /// <summary>Marks the button as the primary action.</summary>
    /// <returns>This builder.</returns>
    public ButtonBuilder Primary() => Variant(ButtonVariant.Primary);

    /// <summary>Sets what happens when the button is pressed.</summary>
    /// <param name="action">The action, built with <see cref="Act"/>.</param>
    /// <returns>This builder.</returns>
    public ButtonBuilder OnClick(A2UIAction action) =>
        Set("action", Throw.IfNull(action, nameof(action)).ToJson());
}
