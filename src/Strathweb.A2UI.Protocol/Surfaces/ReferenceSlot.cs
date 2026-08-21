using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Surfaces;

/// <summary>A place in a component where another component goes, held as a builder rather than an id.</summary>
internal abstract class ReferenceSlot
{
    internal abstract IEnumerable<A2UIComponentBuilder> Children { get; }
}

internal sealed class SingleReferenceSlot(A2UIComponentBuilder child) : ReferenceSlot
{
    internal A2UIComponentBuilder Child { get; } = child;

    internal override IEnumerable<A2UIComponentBuilder> Children => [Child];
}

internal sealed class ListReferenceSlot(IReadOnlyList<A2UIComponentBuilder> children) : ReferenceSlot
{
    internal IReadOnlyList<A2UIComponentBuilder> Items { get; } = children;

    internal override IEnumerable<A2UIComponentBuilder> Children => Items;
}

internal sealed class TemplateReferenceSlot(A2UIComponentBuilder template, string path) : ReferenceSlot
{
    internal A2UIComponentBuilder Template { get; } = template;

    internal string Path { get; } = path;

    internal override IEnumerable<A2UIComponentBuilder> Children => [Template];
}

internal sealed class TabsReferenceSlot(IReadOnlyList<(DynamicValue Title, A2UIComponentBuilder Child)> tabs)
    : ReferenceSlot
{
    internal IReadOnlyList<(DynamicValue Title, A2UIComponentBuilder Child)> Tabs { get; } = tabs;

    internal override IEnumerable<A2UIComponentBuilder> Children
    {
        get
        {
            foreach (var tab in Tabs)
            {
                yield return tab.Child;
            }
        }
    }
}
