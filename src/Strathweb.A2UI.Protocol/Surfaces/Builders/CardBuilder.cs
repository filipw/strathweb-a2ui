namespace Strathweb.A2UI.Surfaces;

/// <summary>A surface holding a single child. Wrap several elements in a Column or Row first.</summary>
public sealed class CardBuilder : A2UIComponentBuilder<CardBuilder>
{
    internal CardBuilder(A2UISurfaceBuilder surface)
        : base(surface, "Card")
    {
    }

    /// <summary>Sets the child.</summary>
    /// <param name="child">The component inside the card.</param>
    /// <returns>This builder.</returns>
    public CardBuilder Child(A2UIComponentBuilder child) => Reference("child", child);
}
