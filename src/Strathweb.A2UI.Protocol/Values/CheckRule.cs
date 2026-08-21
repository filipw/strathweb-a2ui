using System.Text.Json.Nodes;
using Strathweb.A2UI.Internal;

namespace Strathweb.A2UI.Values;

/// <summary>
/// A client-side validation rule on an input component: a boolean-returning condition and the message
/// to show when it fails.
/// </summary>
public sealed class CheckRule
{
    /// <summary>Creates a check.</summary>
    /// <param name="condition">A condition that evaluates to a boolean. Both a call and a binding are legal.</param>
    /// <param name="message">The message shown when the condition is false. Required by the schema.</param>
    public CheckRule(DynamicValue condition, string message)
    {
        Condition = Throw.IfNull(condition, nameof(condition));
        Message = Throw.IfNull(message, nameof(message));
    }

    /// <summary>The condition to evaluate.</summary>
    public DynamicValue Condition { get; }

    /// <summary>The message shown when the check fails.</summary>
    public string Message { get; }

    /// <summary>Writes this rule as its wire JSON object.</summary>
    /// <returns>A new <see cref="JsonObject"/>.</returns>
    public JsonObject ToJson() => new()
    {
        ["condition"] = Condition.ToJson(),
        ["message"] = Message,
    };

    /// <summary>Reads a rule from its wire JSON object.</summary>
    /// <param name="node">The JSON to read.</param>
    /// <returns>The parsed rule.</returns>
    /// <exception cref="A2UIValueException">The node is not an object with a condition and a string message.</exception>
    public static CheckRule FromJson(JsonNode? node)
    {
        if (node is not JsonObject obj || !obj.ContainsKey("condition") ||
            obj["message"] is not JsonValue messageValue || !messageValue.TryGetValue<string>(out var message))
        {
            throw new A2UIValueException("A check must be an object with a 'condition' and a string 'message'.");
        }

        return new CheckRule(DynamicValue.FromJson(obj["condition"]), message);
    }
}
