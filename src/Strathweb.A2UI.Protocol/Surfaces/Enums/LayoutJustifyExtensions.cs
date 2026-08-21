namespace Strathweb.A2UI.Surfaces;

/// <summary>Converts <see cref="LayoutJustify"/> to the value the catalog expects.</summary>
public static class LayoutJustifyExtensions
{
    /// <summary>The wire value for a <see cref="LayoutJustify"/>.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The string the catalog's schema defines.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not a defined member.</exception>
    public static string ToWireString(this LayoutJustify value) => value switch
    {
        LayoutJustify.Start => "start",
        LayoutJustify.Center => "center",
        LayoutJustify.End => "end",
        LayoutJustify.SpaceBetween => "spaceBetween",
        LayoutJustify.SpaceAround => "spaceAround",
        LayoutJustify.SpaceEvenly => "spaceEvenly",
        LayoutJustify.Stretch => "stretch",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown LayoutJustify."),
    };
}
