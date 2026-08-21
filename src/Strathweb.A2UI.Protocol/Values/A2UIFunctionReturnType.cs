namespace Strathweb.A2UI.Values;

/// <summary>The declared return type of a catalog function call.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1720:Identifier contains type name",
    Justification = "Member names are the wire values the schema defines.")]
public enum A2UIFunctionReturnType
{
    /// <summary>Returns a string.</summary>
    String = 1,

    /// <summary>Returns a number.</summary>
    Number = 2,

    /// <summary>Returns a boolean. This is the schema default when <c>returnType</c> is omitted.</summary>
    Boolean = 3,

    /// <summary>Returns an array.</summary>
    Array = 4,

    /// <summary>Returns an object.</summary>
    Object = 5,

    /// <summary>Returns a value of any type.</summary>
    Any = 6,

    /// <summary>Returns nothing.</summary>
    Void = 7,
}
