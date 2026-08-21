namespace Strathweb.A2UI.Surfaces;

/// <summary>Arranges its children vertically. Nest rows inside a column to build a grid.</summary>
public sealed class ColumnBuilder : A2UIComponentBuilder<ColumnBuilder>
{
    internal ColumnBuilder(A2UISurfaceBuilder surface)
        : base(surface, "Column")
    {
    }

    /// <summary>Sets the children, in order.</summary>
    /// <param name="children">The child components.</param>
    /// <returns>This builder.</returns>
    public ColumnBuilder Children(params A2UIComponentBuilder[] children) => References("children", children);

    /// <summary>Repeats a component once per item in a data model list.</summary>
    /// <param name="template">The component to repeat.</param>
    /// <param name="path">The data model path to the list.</param>
    /// <returns>This builder.</returns>
    public ColumnBuilder ChildrenFrom(A2UIComponentBuilder template, string path) =>
        TemplateReference("children", template, path);

    /// <summary>Sets how children are spread down the column.</summary>
    /// <param name="justify">The arrangement.</param>
    /// <returns>This builder.</returns>
    public ColumnBuilder Justify(LayoutJustify justify) => Set("justify", justify.ToWireString());

    /// <summary>Sets how children are aligned horizontally within the column.</summary>
    /// <param name="align">The alignment.</param>
    /// <returns>This builder.</returns>
    public ColumnBuilder Align(LayoutAlign align) => Set("align", align.ToWireString());
}
