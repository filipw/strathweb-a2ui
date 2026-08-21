using Strathweb.A2UI.Internal;

namespace Strathweb.A2UI.Catalogs;

/// <summary>Which of a component's properties hold references to other components.</summary>
public sealed class A2UIComponentReferenceFields
{
    private readonly HashSet<string> single;
    private readonly HashSet<string> list;
    private readonly Dictionary<string, HashSet<string>> nested;

    internal A2UIComponentReferenceFields(
        HashSet<string> single,
        HashSet<string> list,
        Dictionary<string, HashSet<string>> nested)
    {
        this.single = single;
        this.list = list;
        this.nested = nested;
    }

    /// <summary>Properties holding a single component id, such as <c>Card.child</c>.</summary>
    public IReadOnlyCollection<string> SingleReferenceProperties => single;

    /// <summary>Properties holding a child list, such as <c>Column.children</c>.</summary>
    public IReadOnlyCollection<string> ListReferenceProperties => list;

    /// <summary>
    /// For array-of-object properties, the sub-keys inside each element that hold references, such
    /// as the <c>child</c> in <c>Tabs.tabs[].child</c>.
    /// </summary>
    public IReadOnlyCollection<string> NestedReferencePropertiesOf(string propertyName) =>
        nested.TryGetValue(Throw.IfNull(propertyName, nameof(propertyName)), out var keys)
            ? keys
            : [];

    /// <summary>Whether a property holds component references at all.</summary>
    /// <param name="propertyName">The property name.</param>
    /// <returns><see langword="true"/> when the property is a reference property.</returns>
    public bool IsReferenceProperty(string propertyName)
    {
        Throw.IfNull(propertyName, nameof(propertyName));
        return single.Contains(propertyName) || list.Contains(propertyName);
    }

    /// <summary>Whether the component has any reference properties.</summary>
    public bool IsEmpty => single.Count == 0 && list.Count == 0;
}
