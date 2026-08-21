using Strathweb.A2UI.Internal;

namespace Strathweb.A2UI.Validation;

/// <summary>
/// Thrown when an invalid payload reaches a code path that cannot report a list of problems, such as
/// a surface builder.
/// </summary>
public sealed class A2UIValidationException : Exception
{
    /// <summary>Creates the exception from a failed validation.</summary>
    /// <param name="result">The result carrying the problems found.</param>
    public A2UIValidationException(A2UIValidationResult result)
        : base(Throw.IfNull(result, nameof(result)).ToString())
    {
        Result = result;
    }

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">A description of the failure.</param>
    public A2UIValidationException(string message)
        : base(message)
    {
        Result = A2UIValidationResult.Valid;
    }

    /// <summary>Creates the exception with a message and an inner exception.</summary>
    /// <param name="message">A description of the failure.</param>
    /// <param name="innerException">The underlying failure.</param>
    public A2UIValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Result = A2UIValidationResult.Valid;
    }

    /// <summary>Creates the exception.</summary>
    public A2UIValidationException()
    {
        Result = A2UIValidationResult.Valid;
    }

    /// <summary>The problems that caused the failure.</summary>
    public A2UIValidationResult Result { get; }
}
