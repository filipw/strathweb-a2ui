using System.Text.Json;

namespace Strathweb.A2UI.Serialization;

/// <summary>
/// Thrown when JSON cannot be read as an A2UI message envelope: no message key, more than one, an
/// unknown key, or a missing required field.
/// </summary>
public sealed class A2UIParseException : JsonException
{
    /// <summary>Creates the exception.</summary>
    public A2UIParseException()
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">A description of what could not be read.</param>
    public A2UIParseException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and an inner exception.</summary>
    /// <param name="message">A description of what could not be read.</param>
    /// <param name="innerException">The underlying failure.</param>
    public A2UIParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
