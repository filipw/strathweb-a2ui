using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Surfaces;

/// <summary>A set of tabs, each with a title and a component behind it.</summary>
public sealed class TabsBuilder : A2UIComponentBuilder<TabsBuilder>
{
    private readonly List<(DynamicValue Title, A2UIComponentBuilder Child)> tabs = [];

    internal TabsBuilder(A2UISurfaceBuilder surface)
        : base(surface, "Tabs")
    {
    }

    /// <summary>Adds a tab.</summary>
    /// <param name="title">The tab's title.</param>
    /// <param name="child">The component shown when the tab is selected.</param>
    /// <returns>This builder.</returns>
    public TabsBuilder Tab(DynamicValue title, A2UIComponentBuilder child)
    {
        Throw.IfNull(title, nameof(title));
        Throw.IfNull(child, nameof(child));

        tabs.Add((title, child));
        Slots["tabs"] = new TabsReferenceSlot(tabs);
        return this;
    }

    /// <summary>Adds a tab with a literal title.</summary>
    /// <param name="title">The tab's title.</param>
    /// <param name="child">The component shown when the tab is selected.</param>
    /// <returns>This builder.</returns>
    public TabsBuilder Tab(string title, A2UIComponentBuilder child) =>
        Tab(DynamicValue.FromString(Throw.IfNull(title, nameof(title))), child);
}
