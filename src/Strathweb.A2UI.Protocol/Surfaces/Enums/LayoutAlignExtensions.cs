namespace Strathweb.A2UI.Surfaces;

/// <summary>Converts <see cref="LayoutAlign"/> to the value the catalog expects.</summary>
public static class LayoutAlignExtensions
{
    /// <summary>The wire value for a <see cref="LayoutAlign"/>.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The string the catalog's schema defines.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not a defined member.</exception>
    public static string ToWireString(this LayoutAlign value) => value switch
    {
        LayoutAlign.Start => "start",
        LayoutAlign.Center => "center",
        LayoutAlign.End => "end",
        LayoutAlign.Stretch => "stretch",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown LayoutAlign."),
    };
}
