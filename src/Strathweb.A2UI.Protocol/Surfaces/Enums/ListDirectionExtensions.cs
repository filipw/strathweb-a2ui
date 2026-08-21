namespace Strathweb.A2UI.Surfaces;

/// <summary>Converts <see cref="ListDirection"/> to the value the catalog expects.</summary>
public static class ListDirectionExtensions
{
    /// <summary>The wire value for a <see cref="ListDirection"/>.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The string the catalog's schema defines.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not a defined member.</exception>
    public static string ToWireString(this ListDirection value) => value switch
    {
        ListDirection.Vertical => "vertical",
        ListDirection.Horizontal => "horizontal",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ListDirection."),
    };
}
