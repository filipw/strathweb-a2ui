namespace Strathweb.A2UI.Surfaces;

/// <summary>Converts <see cref="TextVariant"/> to the value the catalog expects.</summary>
public static class TextVariantExtensions
{
    /// <summary>The wire value for a <see cref="TextVariant"/>.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The string the catalog's schema defines.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not a defined member.</exception>
    public static string ToWireString(this TextVariant value) => value switch
    {
        TextVariant.H1 => "h1",
        TextVariant.H2 => "h2",
        TextVariant.H3 => "h3",
        TextVariant.H4 => "h4",
        TextVariant.H5 => "h5",
        TextVariant.Caption => "caption",
        TextVariant.Body => "body",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown TextVariant."),
    };
}
