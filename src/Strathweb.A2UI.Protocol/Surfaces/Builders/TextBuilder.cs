using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Surfaces;

/// <summary>A run of text. Simple Markdown is supported, but a dedicated component usually reads better.</summary>
public sealed class TextBuilder : A2UIComponentBuilder<TextBuilder>
{
    internal TextBuilder(A2UISurfaceBuilder surface)
        : base(surface, "Text")
    {
    }

    /// <summary>Sets the text.</summary>
    /// <param name="text">The text, a binding, or a formatting call.</param>
    /// <returns>This builder.</returns>
    public TextBuilder Text(DynamicValue text) => Set("text", text);

    /// <summary>Sets how the text is styled.</summary>
    /// <param name="variant">The style.</param>
    /// <returns>This builder.</returns>
    public TextBuilder Variant(TextVariant variant) => Set("variant", variant.ToWireString());
}
