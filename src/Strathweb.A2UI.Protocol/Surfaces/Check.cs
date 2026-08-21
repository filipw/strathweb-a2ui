using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Surfaces;

/// <summary>Builds the client-side validation rules the Basic Catalog's input components accept.</summary>
public static class Check
{
    /// <summary>Requires a value to be present and non-empty.</summary>
    /// <param name="value">What to check, usually <see cref="Bind.Path"/>.</param>
    /// <param name="message">What to tell the user when it fails.</param>
    /// <returns>The rule.</returns>
    public static CheckRule Required(DynamicValue value, string message) =>
        new(DynamicValue.FromCall("required", Args(("value", value))), message);

    /// <summary>Requires a value to match a regular expression.</summary>
    /// <param name="value">What to check.</param>
    /// <param name="pattern">The pattern.</param>
    /// <param name="message">What to tell the user when it fails.</param>
    /// <returns>The rule.</returns>
    public static CheckRule Regex(DynamicValue value, string pattern, string message) =>
        new(
            DynamicValue.FromCall("regex", Args(("value", value), ("pattern", DynamicValue.FromString(pattern)))),
            message);

    /// <summary>Requires a value to look like an email address.</summary>
    /// <param name="value">What to check.</param>
    /// <param name="message">What to tell the user when it fails.</param>
    /// <returns>The rule.</returns>
    public static CheckRule Email(DynamicValue value, string message) =>
        new(DynamicValue.FromCall("email", Args(("value", value))), message);

    /// <summary>Constrains a value's length.</summary>
    /// <param name="value">What to check.</param>
    /// <param name="message">What to tell the user when it fails.</param>
    /// <param name="min">The shortest allowed length, if any.</param>
    /// <param name="max">The longest allowed length, if any.</param>
    /// <returns>The rule.</returns>
    public static CheckRule Length(DynamicValue value, string message, double? min = null, double? max = null) =>
        new(DynamicValue.FromCall("length", Bounds(value, min, max)), message);

    /// <summary>Constrains a numeric value's range.</summary>
    /// <param name="value">What to check.</param>
    /// <param name="message">What to tell the user when it fails.</param>
    /// <param name="min">The lowest allowed value, if any.</param>
    /// <param name="max">The highest allowed value, if any.</param>
    /// <returns>The rule.</returns>
    public static CheckRule Numeric(DynamicValue value, string message, double? min = null, double? max = null) =>
        new(DynamicValue.FromCall("numeric", Bounds(value, min, max)), message);

    private static Dictionary<string, DynamicValue> Args(params (string Key, DynamicValue Value)[] args)
    {
        var result = new Dictionary<string, DynamicValue>(args.Length, StringComparer.Ordinal);
        foreach (var entry in args)
        {
            result[entry.Key] = entry.Value;
        }

        return result;
    }

    private static Dictionary<string, DynamicValue> Bounds(DynamicValue value, double? min, double? max)
    {
        var args = Args(("value", value));

        if (min is { } lower)
        {
            args["min"] = DynamicValue.FromNumber(lower);
        }

        if (max is { } upper)
        {
            args["max"] = DynamicValue.FromNumber(upper);
        }

        return args;
    }
}
