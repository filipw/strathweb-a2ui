namespace Strathweb.A2UI.Surfaces;

/// <summary>Arranges its children horizontally.</summary>
public sealed class RowBuilder : A2UIComponentBuilder<RowBuilder>
{
    internal RowBuilder(A2UISurfaceBuilder surface)
        : base(surface, "Row")
    {
    }

    /// <summary>Sets the children, in order.</summary>
    /// <param name="children">The child components.</param>
    /// <returns>This builder.</returns>
    public RowBuilder Children(params A2UIComponentBuilder[] children) => References("children", children);

    /// <summary>Repeats a component once per item in a data model list.</summary>
    /// <param name="template">The component to repeat.</param>
    /// <param name="path">The data model path to the list.</param>
    /// <returns>This builder.</returns>
    public RowBuilder ChildrenFrom(A2UIComponentBuilder template, string path) =>
        TemplateReference("children", template, path);

    /// <summary>Sets how children are spread along the row.</summary>
    /// <param name="justify">The arrangement.</param>
    /// <returns>This builder.</returns>
    public RowBuilder Justify(LayoutJustify justify) => Set("justify", justify.ToWireString());

    /// <summary>Sets how children are aligned vertically within the row.</summary>
    /// <param name="align">The alignment.</param>
    /// <returns>This builder.</returns>
    public RowBuilder Align(LayoutAlign align) => Set("align", align.ToWireString());
}
