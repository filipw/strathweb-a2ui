namespace Strathweb.A2UI.Surfaces;

/// <summary>Converts <see cref="ChoiceDisplayStyle"/> to the value the catalog expects.</summary>
public static class ChoiceDisplayStyleExtensions
{
    /// <summary>The wire value for a <see cref="ChoiceDisplayStyle"/>.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The string the catalog's schema defines.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not a defined member.</exception>
    public static string ToWireString(this ChoiceDisplayStyle value) => value switch
    {
        ChoiceDisplayStyle.Checkbox => "checkbox",
        ChoiceDisplayStyle.Chips => "chips",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ChoiceDisplayStyle."),
    };
}
