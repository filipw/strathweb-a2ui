using System.Text.Json.Nodes;
using Strathweb.A2UI.Internal;

namespace Strathweb.A2UI.Values;

/// <summary>
/// A component property value: a literal, a binding into the surface's data model, or a call to a
/// catalog function.
/// </summary>
public sealed class DynamicValue
{
    private DynamicValue(DynamicValueKind kind, JsonNode? literal, string? path, FunctionCall? call)
    {
        Kind = kind;
        Literal = literal;
        Path = path;
        Call = call;
    }

    /// <summary>Which form this value takes.</summary>
    public DynamicValueKind Kind { get; }

    /// <summary>The literal JSON, when <see cref="Kind"/> is <see cref="DynamicValueKind.Literal"/>.</summary>
    public JsonNode? Literal { get; }

    /// <summary>
    /// The data model location, when <see cref="Kind"/> is <see cref="DynamicValueKind.Path"/>.
    /// Absolute (<c>/user/name</c>) at surface scope, or relative (<c>name</c>) inside a template.
    /// </summary>
    public string? Path { get; }

    /// <summary>The call, when <see cref="Kind"/> is <see cref="DynamicValueKind.FunctionCall"/>.</summary>
    public FunctionCall? Call { get; }

    /// <summary>Wraps a literal JSON node.</summary>
    /// <param name="node">The literal value. May be <see langword="null"/> for JSON null.</param>
    /// <returns>A literal value.</returns>
    public static DynamicValue FromLiteral(JsonNode? node) =>
        new(DynamicValueKind.Literal, node, path: null, call: null);

    /// <summary>Wraps a literal string.</summary>
    /// <param name="value">The string.</param>
    /// <returns>A literal value.</returns>
    public static DynamicValue FromString(string value) =>
        FromLiteral(JsonValue.Create(Throw.IfNull(value, nameof(value))));

    /// <summary>Wraps a literal number.</summary>
    /// <param name="value">The number.</param>
    /// <returns>A literal value.</returns>
    public static DynamicValue FromNumber(double value) => FromLiteral(JsonValue.Create(value));

    /// <summary>Wraps a literal boolean.</summary>
    /// <param name="value">The boolean.</param>
    /// <returns>A literal value.</returns>
    public static DynamicValue FromBoolean(bool value) => FromLiteral(JsonValue.Create(value));

    /// <summary>Creates a binding to a data model location.</summary>
    /// <param name="path">A JSON Pointer, or a relative path inside a template scope.</param>
    /// <returns>A binding value.</returns>
    public static DynamicValue FromPath(string path) =>
        new(DynamicValueKind.Path, literal: null, Throw.IfNullOrEmpty(path, nameof(path)), call: null);

    /// <summary>Creates a function call value.</summary>
    /// <param name="call">The call.</param>
    /// <returns>A function call value.</returns>
    public static DynamicValue FromCall(FunctionCall call) =>
        new(DynamicValueKind.FunctionCall, literal: null, path: null, Throw.IfNull(call, nameof(call)));

    /// <summary>Creates a function call value by name.</summary>
    /// <param name="function">The catalog function name.</param>
    /// <param name="args">Arguments, or <see langword="null"/>.</param>
    /// <returns>A function call value.</returns>
    public static DynamicValue FromCall(
        string function,
        System.Collections.Generic.IReadOnlyDictionary<string, DynamicValue>? args = null) =>
        FromCall(new FunctionCall(function) { Args = args });

    /// <summary>Lets a bare string stand in for a literal, so <c>Label("Rating")</c> reads as it should.</summary>
    /// <param name="value">The text. <see langword="null"/> becomes a JSON null literal.</param>
    public static implicit operator DynamicValue(string? value) =>
        value is null ? FromLiteral(null) : FromString(value);

    /// <summary>Lets a bare number stand in for a literal.</summary>
    /// <param name="value">The number.</param>
    public static implicit operator DynamicValue(double value) => FromNumber(value);

    /// <summary>Lets a bare integer stand in for a literal.</summary>
    /// <param name="value">The number.</param>
    public static implicit operator DynamicValue(int value) => FromNumber(value);

    /// <summary>Lets a bare boolean stand in for a literal.</summary>
    /// <param name="value">The boolean.</param>
    public static implicit operator DynamicValue(bool value) => FromBoolean(value);

    /// <summary>
    /// Lets a value be used wherever its JSON is wanted, so the two <c>Set</c> overloads a component
    /// builder would otherwise need collapse into one.
    /// </summary>
    /// <param name="value">The value.</param>
    public static implicit operator JsonNode?(DynamicValue? value) => value?.ToJson();

    /// <summary>Writes this value as its wire JSON.</summary>
    /// <returns>A new node, or <see langword="null"/> for a JSON null literal.</returns>
    public JsonNode? ToJson() => Kind switch
    {
        DynamicValueKind.Path => new JsonObject { ["path"] = Path },
        DynamicValueKind.FunctionCall => Call!.ToJson(),
        _ => Literal?.DeepClone(),
    };

    /// <summary>Reads a value from its wire JSON.</summary>
    /// <param name="node">The JSON to read.</param>
    /// <returns>The parsed value.</returns>
    public static DynamicValue FromJson(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            if (obj.Count == 1 && obj["path"] is JsonValue pathValue &&
                pathValue.TryGetValue<string>(out var path))
            {
                return FromPath(path);
            }

            if (FunctionCall.IsFunctionCall(obj))
            {
                return FromCall(FunctionCall.FromJson(obj));
            }
        }

        return FromLiteral(node?.DeepClone());
    }
}
