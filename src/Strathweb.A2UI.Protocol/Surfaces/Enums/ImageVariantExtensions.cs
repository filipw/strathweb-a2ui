namespace Strathweb.A2UI.Surfaces;

/// <summary>Converts <see cref="ImageVariant"/> to the value the catalog expects.</summary>
public static class ImageVariantExtensions
{
    /// <summary>The wire value for a <see cref="ImageVariant"/>.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The string the catalog's schema defines.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not a defined member.</exception>
    public static string ToWireString(this ImageVariant value) => value switch
    {
        ImageVariant.Icon => "icon",
        ImageVariant.Avatar => "avatar",
        ImageVariant.SmallFeature => "smallFeature",
        ImageVariant.MediumFeature => "mediumFeature",
        ImageVariant.LargeFeature => "largeFeature",
        ImageVariant.Header => "header",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ImageVariant."),
    };
}
