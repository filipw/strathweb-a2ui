using System.Text.Json.Nodes;
using Strathweb.A2UI.Internal;

namespace Strathweb.A2UI.Values;

/// <summary>
/// Invokes a function the renderer's catalog defines, for example a formatter or a validation check.
/// </summary>
public sealed class FunctionCall
{
    /// <summary>Creates a function call.</summary>
    /// <param name="call">The catalog function name.</param>
    public FunctionCall(string call)
    {
        Call = Throw.IfNullOrEmpty(call, nameof(call));
    }

    /// <summary>The name of the function to call.</summary>
    public string Call { get; }

    /// <summary>Arguments passed to the function, or <see langword="null"/> when it takes none.</summary>
    public IReadOnlyDictionary<string, DynamicValue>? Args { get; init; }

    /// <summary>
    /// The expected return type. Omitted from the wire when <see langword="null"/>, in which case the
    /// schema default of <see cref="A2UIFunctionReturnType.Boolean"/> applies.
    /// </summary>
    public A2UIFunctionReturnType? ReturnType { get; init; }

    /// <summary>Writes this call as its wire JSON object.</summary>
    /// <returns>A new <see cref="JsonObject"/>.</returns>
    public JsonObject ToJson()
    {
        var result = new JsonObject { ["call"] = Call };

        if (Args is { Count: > 0 })
        {
            var args = new JsonObject();
            foreach (var pair in Args)
            {
                args[pair.Key] = pair.Value?.ToJson();
            }

            result["args"] = args;
        }

        if (ReturnType is { } returnType)
        {
            result["returnType"] = A2UIFunctionReturnTypes.ToWireString(returnType);
        }

        return result;
    }

    /// <summary>Reads a function call from its wire JSON object.</summary>
    /// <param name="node">The JSON to read.</param>
    /// <returns>The parsed call.</returns>
    /// <exception cref="A2UIValueException">The node is not an object with a string <c>call</c> property.</exception>
    public static FunctionCall FromJson(JsonNode? node)
    {
        if (node is not JsonObject obj || obj["call"] is not JsonValue callValue ||
            !callValue.TryGetValue<string>(out var call))
        {
            throw new A2UIValueException("A function call must be an object with a string 'call' property.");
        }

        Dictionary<string, DynamicValue>? args = null;
        if (obj["args"] is JsonObject argsObject)
        {
            args = new Dictionary<string, DynamicValue>(argsObject.Count);
            foreach (var pair in argsObject)
            {
                args[pair.Key] = DynamicValue.FromJson(pair.Value);
            }
        }

        A2UIFunctionReturnType? returnType = null;
        if (obj["returnType"] is JsonValue returnTypeValue &&
            returnTypeValue.TryGetValue<string>(out var rawReturnType))
        {
            if (!A2UIFunctionReturnTypes.TryParse(rawReturnType, out var parsed))
            {
                throw new A2UIValueException($"Unknown function return type '{rawReturnType}'.");
            }

            returnType = parsed;
        }

        return new FunctionCall(call) { Args = args, ReturnType = returnType };
    }

    /// <summary>Recognises the wire shape of a function call: an object carrying a <c>call</c> property.</summary>
    /// <param name="node">The node to test.</param>
    /// <returns><see langword="true"/> if <paramref name="node"/> looks like a function call.</returns>
    public static bool IsFunctionCall(JsonNode? node) =>
        node is JsonObject obj && obj.ContainsKey("call");
}
