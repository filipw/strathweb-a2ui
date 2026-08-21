namespace Strathweb.A2UI.Surfaces;

/// <summary>Converts <see cref="ButtonVariant"/> to the value the catalog expects.</summary>
public static class ButtonVariantExtensions
{
    /// <summary>The wire value for a <see cref="ButtonVariant"/>.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The string the catalog's schema defines.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not a defined member.</exception>
    public static string ToWireString(this ButtonVariant value) => value switch
    {
        ButtonVariant.Default => "default",
        ButtonVariant.Primary => "primary",
        ButtonVariant.Borderless => "borderless",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ButtonVariant."),
    };
}
