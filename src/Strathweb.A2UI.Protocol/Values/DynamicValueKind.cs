namespace Strathweb.A2UI.Values;

/// <summary>Which of the three forms a <see cref="DynamicValue"/> takes.</summary>
public enum DynamicValueKind
{
    /// <summary>A literal JSON value written directly into the component.</summary>
    Literal = 1,

    /// <summary>A binding to a location in the surface's data model.</summary>
    Path = 2,

    /// <summary>A call to a function the renderer's catalog defines.</summary>
    FunctionCall = 3,
}
