using System.Text.Json.Nodes;
using Strathweb.A2UI.Components;
using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Surfaces;

/// <summary>
/// A component being assembled. Created by <see cref="A2UISurfaceBuilder.Components"/> and turned into
/// an <see cref="A2UIComponent"/> when the surface is built.
/// </summary>
public abstract class A2UIComponentBuilder
{
    private protected A2UIComponentBuilder(A2UISurfaceBuilder surface, string componentType)
    {
        Surface = Throw.IfNull(surface, nameof(surface));
        ComponentType = Throw.IfNullOrEmpty(componentType, nameof(componentType));
        surface.Register(this);
    }

    /// <summary>The catalog component type, for example <c>Card</c>.</summary>
    public string ComponentType { get; }

    /// <summary>The id this component will have, once the surface is built. Empty until then.</summary>
    public string Id { get; internal set; } = string.Empty;

    internal A2UISurfaceBuilder Surface { get; }

    internal string? RequestedId { get; set; }

    internal JsonObject Values { get; } = [];

    internal Dictionary<string, ReferenceSlot> Slots { get; } = new(StringComparer.Ordinal);

    internal A2UIAccessibility? Accessibility { get; set; }

    /// <summary>Materialises this builder, resolving every reference to the id it was assigned.</summary>
    internal A2UIComponent ToComponent()
    {
        var component = new A2UIComponent(Id, ComponentType, (JsonObject)Values.DeepClone())
        {
            Accessibility = Accessibility is { IsEmpty: false } ? Accessibility : null,
        };

        foreach (var slot in Slots)
        {
            component.Properties[slot.Key] = Resolve(slot.Value);
        }

        return component;
    }

    private static JsonNode Resolve(ReferenceSlot slot)
    {
        switch (slot)
        {
            case SingleReferenceSlot single:
                return JsonValue.Create(single.Child.Id)!;

            case ListReferenceSlot list:
                {
                    var array = new JsonArray();
                    foreach (var child in list.Items)
                    {
                        array.Add((JsonNode)JsonValue.Create(child.Id)!);
                    }

                    return array;
                }

            case TemplateReferenceSlot template:
                return ChildList.Template(template.Template.Id, template.Path).ToJson();

            case TabsReferenceSlot tabs:
                {
                    var array = new JsonArray();
                    foreach (var tab in tabs.Tabs)
                    {
                        array.Add((JsonNode)new JsonObject
                        {
                            ["title"] = tab.Title.ToJson(),
                            ["child"] = tab.Child.Id,
                        });
                    }

                    return array;
                }

            default:
                throw new InvalidOperationException($"Unknown reference slot '{slot.GetType()}'.");
        }
    }
}
