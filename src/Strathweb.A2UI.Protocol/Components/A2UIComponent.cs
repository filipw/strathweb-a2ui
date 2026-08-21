using System.Text.Json.Nodes;
using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Components;

/// <summary>One entry in a surface's flat component list.</summary>
public sealed class A2UIComponent
{
    private static readonly HashSet<string> ReservedProperties =
        new(System.StringComparer.Ordinal) { "id", "component", "accessibility" };

    /// <summary>Creates a component.</summary>
    /// <param name="id">The component id, unique within the surface.</param>
    /// <param name="component">The catalog component type name, for example <c>Card</c>.</param>
    public A2UIComponent(string id, string component)
        : this(id, component, new JsonObject())
    {
    }

    /// <summary>Creates a component with an initial property bag.</summary>
    /// <param name="id">The component id, unique within the surface.</param>
    /// <param name="component">The catalog component type name, for example <c>Card</c>.</param>
    /// <param name="properties">
    /// Catalog-defined properties. Taken by reference: later mutations are reflected on the wire.
    /// </param>
    public A2UIComponent(string id, string component, JsonObject properties)
    {
        Id = Throw.IfNull(id, nameof(id));
        Component = Throw.IfNullOrEmpty(component, nameof(component));
        Properties = Throw.IfNull(properties, nameof(properties));
    }

    /// <summary>
    /// The component id. An empty id is a validation error, not a parse error, so it is representable
    /// here and reported by the validator.
    /// </summary>
    public string Id { get; }

    /// <summary>The catalog component type name.</summary>
    public string Component { get; }

    /// <summary>Assistive-technology attributes, or <see langword="null"/>.</summary>
    public A2UIAccessibility? Accessibility { get; init; }

    /// <summary>
    /// Every other property, exactly as the catalog defines it. Mutable by design.
    /// </summary>
    public JsonObject Properties { get; }

    /// <summary>Sets a catalog property.</summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The value, or <see langword="null"/> to write JSON null.</param>
    /// <returns>This component, for chaining.</returns>
    public A2UIComponent Set(string name, JsonNode? value)
    {
        Properties[Throw.IfNullOrEmpty(name, nameof(name))] = value;
        return this;
    }

    /// <summary>Writes this component as its wire JSON object.</summary>
    /// <returns>A new <see cref="JsonObject"/>.</returns>
    public JsonObject ToJson()
    {
        var result = new JsonObject
        {
            ["id"] = Id,
            ["component"] = Component,
        };

        if (Accessibility is { IsEmpty: false } accessibility)
        {
            result["accessibility"] = accessibility.ToJson();
        }

        foreach (var pair in Properties)
        {
            result[pair.Key] = pair.Value?.DeepClone();
        }

        return result;
    }

    /// <summary>Reads a component from its wire JSON object.</summary>
    /// <param name="node">The JSON to read.</param>
    /// <returns>The parsed component.</returns>
    /// <exception cref="A2UIValueException">The node is not an object with string <c>id</c> and <c>component</c>.</exception>
    public static A2UIComponent FromJson(JsonNode? node)
    {
        if (node is not JsonObject obj ||
            obj["id"] is not JsonValue idValue || !idValue.TryGetValue<string>(out var id) ||
            obj["component"] is not JsonValue componentValue ||
            !componentValue.TryGetValue<string>(out var component))
        {
            throw new A2UIValueException("A component must be an object with string 'id' and 'component' properties.");
        }

        var properties = new JsonObject();
        foreach (var pair in obj)
        {
            if (!ReservedProperties.Contains(pair.Key))
            {
                properties[pair.Key] = pair.Value?.DeepClone();
            }
        }

        return new A2UIComponent(id, component, properties)
        {
            Accessibility = obj["accessibility"] is { } accessibility
                ? A2UIAccessibility.FromJson(accessibility)
                : null,
        };
    }
}
