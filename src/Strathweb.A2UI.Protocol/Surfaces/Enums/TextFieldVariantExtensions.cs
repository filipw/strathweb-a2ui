namespace Strathweb.A2UI.Surfaces;

/// <summary>Converts <see cref="TextFieldVariant"/> to the value the catalog expects.</summary>
public static class TextFieldVariantExtensions
{
    /// <summary>The wire value for a <see cref="TextFieldVariant"/>.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The string the catalog's schema defines.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not a defined member.</exception>
    public static string ToWireString(this TextFieldVariant value) => value switch
    {
        TextFieldVariant.ShortText => "shortText",
        TextFieldVariant.LongText => "longText",
        TextFieldVariant.Number => "number",
        TextFieldVariant.Obscured => "obscured",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown TextFieldVariant."),
    };
}
