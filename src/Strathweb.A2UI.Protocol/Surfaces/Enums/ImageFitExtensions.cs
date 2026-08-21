namespace Strathweb.A2UI.Surfaces;

/// <summary>Converts <see cref="ImageFit"/> to the value the catalog expects.</summary>
public static class ImageFitExtensions
{
    /// <summary>The wire value for a <see cref="ImageFit"/>.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The string the catalog's schema defines.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not a defined member.</exception>
    public static string ToWireString(this ImageFit value) => value switch
    {
        ImageFit.Contain => "contain",
        ImageFit.Cover => "cover",
        ImageFit.Fill => "fill",
        ImageFit.None => "none",
        ImageFit.ScaleDown => "scaleDown",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ImageFit."),
    };
}
