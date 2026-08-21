namespace Strathweb.A2UI.Surfaces;

/// <summary>Converts <see cref="DividerAxis"/> to the value the catalog expects.</summary>
public static class DividerAxisExtensions
{
    /// <summary>The wire value for a <see cref="DividerAxis"/>.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The string the catalog's schema defines.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not a defined member.</exception>
    public static string ToWireString(this DividerAxis value) => value switch
    {
        DividerAxis.Horizontal => "horizontal",
        DividerAxis.Vertical => "vertical",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown DividerAxis."),
    };
}
