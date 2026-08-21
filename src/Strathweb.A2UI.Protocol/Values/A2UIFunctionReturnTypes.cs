namespace Strathweb.A2UI.Values;

/// <summary>Conversions between <see cref="A2UIFunctionReturnType"/> and its wire representation.</summary>
public static class A2UIFunctionReturnTypes
{
    /// <summary>The wire string for <paramref name="returnType"/>.</summary>
    /// <param name="returnType">The return type to convert.</param>
    /// <returns>The value to write into a <c>returnType</c> field.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="returnType"/> is not a defined member.</exception>
    public static string ToWireString(A2UIFunctionReturnType returnType) => returnType switch
    {
        A2UIFunctionReturnType.String => "string",
        A2UIFunctionReturnType.Number => "number",
        A2UIFunctionReturnType.Boolean => "boolean",
        A2UIFunctionReturnType.Array => "array",
        A2UIFunctionReturnType.Object => "object",
        A2UIFunctionReturnType.Any => "any",
        A2UIFunctionReturnType.Void => "void",
        _ => throw new ArgumentOutOfRangeException(nameof(returnType), returnType, "Unknown return type."),
    };

    /// <summary>Parses a wire <c>returnType</c> value.</summary>
    /// <param name="value">The raw value.</param>
    /// <param name="returnType">The parsed return type, when this method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="value"/> names a known return type.</returns>
    public static bool TryParse(string? value, out A2UIFunctionReturnType returnType)
    {
        switch (value)
        {
            case "string": returnType = A2UIFunctionReturnType.String; return true;
            case "number": returnType = A2UIFunctionReturnType.Number; return true;
            case "boolean": returnType = A2UIFunctionReturnType.Boolean; return true;
            case "array": returnType = A2UIFunctionReturnType.Array; return true;
            case "object": returnType = A2UIFunctionReturnType.Object; return true;
            case "any": returnType = A2UIFunctionReturnType.Any; return true;
            case "void": returnType = A2UIFunctionReturnType.Void; return true;
            default: returnType = default; return false;
        }
    }
}
