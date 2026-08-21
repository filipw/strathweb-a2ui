namespace Strathweb.A2UI.Values;

/// <summary>Thrown when a JSON fragment cannot be read as the A2UI value type it appears in.</summary>
public sealed class A2UIValueException : Exception
{
    /// <summary>Creates the exception.</summary>
    public A2UIValueException()
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">A description of what could not be read.</param>
    public A2UIValueException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and an inner exception.</summary>
    /// <param name="message">A description of what could not be read.</param>
    /// <param name="innerException">The underlying failure.</param>
    public A2UIValueException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
