namespace Strathweb.A2UI.Catalogs;

/// <summary>Thrown when a catalog document cannot be read.</summary>
public sealed class A2UICatalogException : Exception
{
    /// <summary>Creates the exception.</summary>
    public A2UICatalogException()
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">A description of what could not be read.</param>
    public A2UICatalogException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and an inner exception.</summary>
    /// <param name="message">A description of what could not be read.</param>
    /// <param name="innerException">The underlying failure.</param>
    public A2UICatalogException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
