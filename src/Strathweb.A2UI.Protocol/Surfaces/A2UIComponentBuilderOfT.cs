using System.Text.Json.Nodes;
using Strathweb.A2UI.Components;
using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Surfaces;

/// <summary>Adds fluent chaining to <see cref="A2UIComponentBuilder"/>.</summary>
/// <typeparam name="TSelf">The concrete builder type, so setters return it rather than the base.</typeparam>
public abstract class A2UIComponentBuilder<TSelf> : A2UIComponentBuilder
    where TSelf : A2UIComponentBuilder<TSelf>
{
    private protected A2UIComponentBuilder(A2UISurfaceBuilder surface, string componentType)
        : base(surface, componentType)
    {
    }

    private protected TSelf Self => (TSelf)this;

    /// <summary>
    /// Gives this component a fixed id instead of a generated one. Useful when a renderer or a test
    /// needs to address it by name.
    /// </summary>
    /// <param name="id">The id. Must be unique within the surface.</param>
    /// <returns>This builder.</returns>
    public TSelf WithId(string id)
    {
        RequestedId = Throw.IfNullOrEmpty(id, nameof(id));
        return Self;
    }

    /// <summary>Sets a catalog property this builder does not model.</summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">
    /// The value. A <see cref="DynamicValue"/> or a bare literal both convert to one implicitly.
    /// </param>
    /// <returns>This builder.</returns>
    public TSelf Set(string name, JsonNode? value)
    {
        Values[Throw.IfNullOrEmpty(name, nameof(name))] = value;
        return Self;
    }

    /// <summary>Adds attributes for assistive technologies.</summary>
    /// <param name="label">A short label, typically one to three words.</param>
    /// <param name="description">Extra detail: instructions, format, or what the action will do.</param>
    /// <returns>This builder.</returns>
    public TSelf Describe(DynamicValue? label = null, DynamicValue? description = null)
    {
        Accessibility = new A2UIAccessibility
        {
            Label = label ?? Accessibility?.Label,
            Description = description ?? Accessibility?.Description,
        };

        return Self;
    }

    /// <summary>
    /// Sets how much of a <c>Row</c> or <c>Column</c>'s spare space this component takes, like CSS
    /// <c>flex-grow</c>. Only meaningful on a direct child of a row or column.
    /// </summary>
    /// <param name="weight">The relative weight.</param>
    /// <returns>This builder.</returns>
    public TSelf Weight(double weight) => Set("weight", JsonValue.Create(weight));

    private protected TSelf Reference(string name, A2UIComponentBuilder child)
    {
        Throw.IfNull(child, nameof(child));
        Slots[name] = new SingleReferenceSlot(child);
        return Self;
    }

    private protected TSelf References(string name, IReadOnlyList<A2UIComponentBuilder> children)
    {
        Throw.IfNull(children, nameof(children));
        Slots[name] = new ListReferenceSlot(children);
        return Self;
    }

    private protected TSelf TemplateReference(string name, A2UIComponentBuilder template, string path)
    {
        Throw.IfNull(template, nameof(template));
        Throw.IfNullOrEmpty(path, nameof(path));
        Slots[name] = new TemplateReferenceSlot(template, path);
        return Self;
    }
}
