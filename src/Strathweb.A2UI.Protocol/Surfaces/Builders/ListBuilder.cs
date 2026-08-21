namespace Strathweb.A2UI.Surfaces;

/// <summary>A list of components, laid out in one direction.</summary>
public sealed class ListBuilder : A2UIComponentBuilder<ListBuilder>
{
    internal ListBuilder(A2UISurfaceBuilder surface)
        : base(surface, "List")
    {
    }

    /// <summary>Sets the children, in order.</summary>
    /// <param name="children">The child components.</param>
    /// <returns>This builder.</returns>
    public ListBuilder Children(params A2UIComponentBuilder[] children) => References("children", children);

    /// <summary>Repeats a component once per item in a data model list.</summary>
    /// <param name="template">The component to repeat.</param>
    /// <param name="path">The data model path to the list.</param>
    /// <returns>This builder.</returns>
    public ListBuilder ChildrenFrom(A2UIComponentBuilder template, string path) =>
        TemplateReference("children", template, path);

    /// <summary>Sets which way the list runs.</summary>
    /// <param name="direction">The direction.</param>
    /// <returns>This builder.</returns>
    public ListBuilder Direction(ListDirection direction) => Set("direction", direction.ToWireString());

    /// <summary>Sets how items are aligned across the list.</summary>
    /// <param name="align">The alignment.</param>
    /// <returns>This builder.</returns>
    public ListBuilder Align(LayoutAlign align) => Set("align", align.ToWireString());
}
