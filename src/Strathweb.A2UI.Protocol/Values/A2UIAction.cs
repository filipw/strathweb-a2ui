using System.Text.Json.Nodes;
using Strathweb.A2UI.Internal;

namespace Strathweb.A2UI.Values;

/// <summary>
/// What a component does when the user interacts with it: either dispatch an event to the agent, or
/// run a function locally in the renderer.
/// </summary>
public sealed class A2UIAction
{
    private A2UIAction(A2UIEvent? @event, FunctionCall? functionCall)
    {
        Event = @event;
        FunctionCall = functionCall;
    }

    /// <summary>The event to dispatch, or <see langword="null"/> when this action runs a function.</summary>
    public A2UIEvent? Event { get; }

    /// <summary>The function to run locally, or <see langword="null"/> when this action dispatches an event.</summary>
    public FunctionCall? FunctionCall { get; }

    /// <summary>Creates an action that dispatches an event to the agent.</summary>
    /// <param name="event">The event.</param>
    /// <returns>An event action.</returns>
    public static A2UIAction FromEvent(A2UIEvent @event) =>
        new(Throw.IfNull(@event, nameof(@event)), null);

    /// <summary>Creates an action that runs a catalog function in the renderer.</summary>
    /// <param name="call">The call.</param>
    /// <returns>A function action.</returns>
    public static A2UIAction FromFunctionCall(FunctionCall call) =>
        new(null, Throw.IfNull(call, nameof(call)));

    /// <summary>Writes this action as its wire JSON object.</summary>
    /// <returns>A new <see cref="JsonObject"/>.</returns>
    public JsonObject ToJson() => Event is not null
        ? new JsonObject { ["event"] = Event.ToJson() }
        : new JsonObject { ["functionCall"] = FunctionCall!.ToJson() };

    /// <summary>Reads an action from its wire JSON object.</summary>
    /// <param name="node">The JSON to read.</param>
    /// <returns>The parsed action.</returns>
    /// <exception cref="A2UIValueException">The node has neither an <c>event</c> nor a <c>functionCall</c>.</exception>
    public static A2UIAction FromJson(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            if (obj["event"] is { } eventNode)
            {
                return FromEvent(A2UIEvent.FromJson(eventNode));
            }

            if (obj["functionCall"] is { } callNode)
            {
                return FromFunctionCall(FunctionCall.FromJson(callNode));
            }
        }

        throw new A2UIValueException("An action must be an object with either an 'event' or a 'functionCall'.");
    }
}
