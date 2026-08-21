using System.Text.Json.Nodes;
using Strathweb.A2UI.Internal;

namespace Strathweb.A2UI.Values;

/// <summary>
/// An event dispatched back to the agent when the user interacts with a component. The renderer
/// resolves every value in <see cref="Context"/> against the data model before sending it.
/// </summary>
public sealed class A2UIEvent
{
    /// <summary>Creates an event.</summary>
    /// <param name="name">The action name the agent will receive.</param>
    public A2UIEvent(string name)
    {
        Name = Throw.IfNullOrEmpty(name, nameof(name));
    }

    /// <summary>The action name.</summary>
    public string Name { get; }

    /// <summary>
    /// Values sent with the event. Use bindings only where the value comes from the data model.
    /// </summary>
    public IReadOnlyDictionary<string, DynamicValue>? Context { get; init; }

    /// <summary>Writes this event as its wire JSON object.</summary>
    /// <returns>A new <see cref="JsonObject"/>.</returns>
    public JsonObject ToJson()
    {
        var result = new JsonObject { ["name"] = Name };

        if (Context is { Count: > 0 })
        {
            var context = new JsonObject();
            foreach (var pair in Context)
            {
                context[pair.Key] = pair.Value?.ToJson();
            }

            result["context"] = context;
        }

        return result;
    }

    /// <summary>Reads an event from its wire JSON object.</summary>
    /// <param name="node">The JSON to read.</param>
    /// <returns>The parsed event.</returns>
    /// <exception cref="A2UIValueException">The node is not an object with a string <c>name</c>.</exception>
    public static A2UIEvent FromJson(JsonNode? node)
    {
        if (node is not JsonObject obj || obj["name"] is not JsonValue nameValue ||
            !nameValue.TryGetValue<string>(out var name))
        {
            throw new A2UIValueException("An event must be an object with a string 'name' property.");
        }

        Dictionary<string, DynamicValue>? context = null;
        if (obj["context"] is JsonObject contextObject)
        {
            context = new Dictionary<string, DynamicValue>(contextObject.Count);
            foreach (var pair in contextObject)
            {
                context[pair.Key] = DynamicValue.FromJson(pair.Value);
            }
        }

        return new A2UIEvent(name) { Context = context };
    }
}
