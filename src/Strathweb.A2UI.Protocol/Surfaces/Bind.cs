using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Surfaces;

/// <summary>Builds the values that connect a component to the surface's data model.</summary>
public static class Bind
{
    /// <summary>Binds to a place in the data model.</summary>
    /// <param name="path">
    /// A JSON Pointer such as <c>/rating</c>. Inside a template, a relative path such as <c>title</c>
    /// addresses the current item.
    /// </param>
    /// <returns>The binding.</returns>
    public static DynamicValue Path(string path) => DynamicValue.FromPath(path);

    /// <summary>A literal string.</summary>
    /// <param name="value">The text.</param>
    /// <returns>The value.</returns>
    public static DynamicValue Text(string value) => DynamicValue.FromString(value);

    /// <summary>A literal number.</summary>
    /// <param name="value">The number.</param>
    /// <returns>The value.</returns>
    public static DynamicValue Number(double value) => DynamicValue.FromNumber(value);

    /// <summary>A literal boolean.</summary>
    /// <param name="value">The boolean.</param>
    /// <returns>The value.</returns>
    public static DynamicValue Boolean(bool value) => DynamicValue.FromBoolean(value);
}
