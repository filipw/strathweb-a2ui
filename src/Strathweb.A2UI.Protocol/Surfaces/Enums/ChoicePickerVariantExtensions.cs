namespace Strathweb.A2UI.Surfaces;

/// <summary>Converts <see cref="ChoicePickerVariant"/> to the value the catalog expects.</summary>
public static class ChoicePickerVariantExtensions
{
    /// <summary>The wire value for a <see cref="ChoicePickerVariant"/>.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The string the catalog's schema defines.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not a defined member.</exception>
    public static string ToWireString(this ChoicePickerVariant value) => value switch
    {
        ChoicePickerVariant.MutuallyExclusive => "mutuallyExclusive",
        ChoicePickerVariant.MultipleSelection => "multipleSelection",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ChoicePickerVariant."),
    };
}
