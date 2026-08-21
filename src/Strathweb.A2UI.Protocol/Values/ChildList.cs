using System.Text.Json.Nodes;
using Strathweb.A2UI.Internal;

namespace Strathweb.A2UI.Values;

/// <summary>
/// The children of a layout component: either a fixed list of component ids, or a template that
/// repeats one component over a list in the data model.
/// </summary>
public sealed class ChildList
{
    private ChildList(IReadOnlyList<string>? componentIds, string? templateComponentId, string? templatePath)
    {
        ComponentIds = componentIds;
        TemplateComponentId = templateComponentId;
        TemplatePath = templatePath;
    }

    /// <summary>The fixed child ids, or <see langword="null"/> when this is a template.</summary>
    public IReadOnlyList<string>? ComponentIds { get; }

    /// <summary>The component repeated per data item, or <see langword="null"/> when this is a fixed list.</summary>
    public string? TemplateComponentId { get; }

    /// <summary>The data model path to the list to repeat over, or <see langword="null"/> when this is a fixed list.</summary>
    public string? TemplatePath { get; }

    /// <summary>Whether this list repeats a template over data.</summary>
    public bool IsTemplate => TemplateComponentId is not null;

    /// <summary>Creates a fixed list of children.</summary>
    /// <param name="componentIds">The child component ids, in render order.</param>
    /// <returns>A fixed child list.</returns>
    public static ChildList Of(IEnumerable<string> componentIds) =>
        new(Throw.IfNull(componentIds, nameof(componentIds)).ToArray(), null, null);

    /// <summary>Creates a fixed list of children.</summary>
    /// <param name="componentIds">The child component ids, in render order.</param>
    /// <returns>A fixed child list.</returns>
    public static ChildList Of(params string[] componentIds) => Of((IEnumerable<string>)componentIds);

    /// <summary>Creates a template that repeats a component over a data model list.</summary>
    /// <param name="componentId">The component to use as the template.</param>
    /// <param name="path">The data model path to the list of items.</param>
    /// <returns>A templated child list.</returns>
    public static ChildList Template(string componentId, string path) =>
        new(
            null,
            Throw.IfNullOrEmpty(componentId, nameof(componentId)),
            Throw.IfNullOrEmpty(path, nameof(path)));

    /// <summary>Writes this list as its wire JSON.</summary>
    /// <returns>A JSON array of ids, or a template object.</returns>
    public JsonNode ToJson()
    {
        if (IsTemplate)
        {
            return new JsonObject
            {
                ["componentId"] = TemplateComponentId,
                ["path"] = TemplatePath,
            };
        }

        var array = new JsonArray();
        foreach (var id in ComponentIds!)
        {
            array.Add((JsonNode)JsonValue.Create(id)!);
        }

        return array;
    }

    /// <summary>Reads a child list from its wire JSON.</summary>
    /// <param name="node">The JSON to read.</param>
    /// <returns>The parsed list.</returns>
    /// <exception cref="A2UIValueException">The node is neither a string array nor a template object.</exception>
    public static ChildList FromJson(JsonNode? node)
    {
        switch (node)
        {
            case JsonArray array:
                {
                    var ids = new List<string>(array.Count);
                    foreach (var item in array)
                    {
                        if (item is not JsonValue value || !value.TryGetValue<string>(out var id))
                        {
                            throw new A2UIValueException("A fixed child list must contain only component id strings.");
                        }

                        ids.Add(id);
                    }

                    return new ChildList(ids, null, null);
                }

            case JsonObject obj
                when obj["componentId"] is JsonValue componentIdValue &&
                     componentIdValue.TryGetValue<string>(out var componentId) &&
                     obj["path"] is JsonValue pathValue &&
                     pathValue.TryGetValue<string>(out var path):
                return new ChildList(null, componentId, path);

            default:
                throw new A2UIValueException(
                    "A child list must be an array of component ids or an object with 'componentId' and 'path'.");
        }
    }
}
