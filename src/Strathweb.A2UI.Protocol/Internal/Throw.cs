namespace Strathweb.A2UI.Internal;

/// <summary>
/// Argument guards. <c>ArgumentNullException.ThrowIfNull</c> and friends do not exist on
/// netstandard2.0, so every public entry point in this assembly funnels through here instead.
/// </summary>
internal static class Throw
{
    internal static T IfNull<T>(T? value, string paramName)
        where T : class
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }

        return value;
    }

    internal static string IfNullOrEmpty(string? value, string paramName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("Value cannot be empty.", paramName);
        }

        return value;
    }
}
