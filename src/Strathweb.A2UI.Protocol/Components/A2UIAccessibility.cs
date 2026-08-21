using System.Text.Json.Nodes;
using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Components;

/// <summary>Attributes read by assistive technologies. Both values may be literals or data bindings.</summary>
public sealed class A2UIAccessibility
{
    /// <summary>
    /// A short label conveying the element's purpose, such as "User ID" or "Mute".
    /// </summary>
    public DynamicValue? Label { get; init; }

    /// <summary>
    /// Additional detail: instructions, format requirements, or what an action will do.
    /// </summary>
    public DynamicValue? Description { get; init; }

    /// <summary>Whether both attributes are absent, in which case the object is omitted from the wire.</summary>
    public bool IsEmpty => Label is null && Description is null;

    /// <summary>Writes these attributes as their wire JSON object.</summary>
    /// <returns>A new <see cref="JsonObject"/>.</returns>
    public JsonObject ToJson()
    {
        var result = new JsonObject();

        if (Label is not null)
        {
            result["label"] = Label.ToJson();
        }

        if (Description is not null)
        {
            result["description"] = Description.ToJson();
        }

        return result;
    }

    /// <summary>Reads accessibility attributes from their wire JSON object.</summary>
    /// <param name="node">The JSON to read.</param>
    /// <returns>The parsed attributes.</returns>
    /// <exception cref="A2UIValueException">The node is not an object.</exception>
    public static A2UIAccessibility FromJson(JsonNode? node)
    {
        if (node is not JsonObject obj)
        {
            throw new A2UIValueException("Accessibility attributes must be an object.");
        }

        return new A2UIAccessibility
        {
            Label = obj.ContainsKey("label") ? DynamicValue.FromJson(obj["label"]) : null,
            Description = obj.ContainsKey("description") ? DynamicValue.FromJson(obj["description"]) : null,
        };
    }
}
