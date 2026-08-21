namespace Strathweb.A2UI.Surfaces;

/// <summary>What kind of text a field accepts, and how it is drawn.</summary>
public enum TextFieldVariant
{
    /// <summary>The <c>shortText</c> value.</summary>
    ShortText = 1,

    /// <summary>The <c>longText</c> value.</summary>
    LongText = 2,

    /// <summary>The <c>number</c> value.</summary>
    Number = 3,

    /// <summary>The <c>obscured</c> value.</summary>
    Obscured = 4,
}
