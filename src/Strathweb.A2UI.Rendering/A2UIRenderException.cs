namespace Strathweb.A2UI.Rendering;

/// <summary>Thrown when a surface cannot be rendered as sent: a function the catalog does not define, an argument of the wrong shape.</summary>
public sealed class A2UIRenderException : Exception
{
    /// <summary>Creates the exception.</summary>
    public A2UIRenderException()
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">What could not be rendered.</param>
    public A2UIRenderException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and an inner exception.</summary>
    /// <param name="message">What could not be rendered.</param>
    /// <param name="innerException">The underlying failure.</param>
    public A2UIRenderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
