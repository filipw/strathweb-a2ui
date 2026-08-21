using Strathweb.A2UI.Internal;

namespace Strathweb.A2UI.Validation;

/// <summary>Everything wrong with a payload, rather than only the first problem.</summary>
public sealed class A2UIValidationResult
{
    /// <summary>A result with no errors.</summary>
    public static A2UIValidationResult Valid { get; } = new([]);

    /// <summary>Creates a result.</summary>
    /// <param name="errors">The problems found, in the order they were detected.</param>
    public A2UIValidationResult(IReadOnlyList<A2UIValidationError> errors)
    {
        Errors = Throw.IfNull(errors, nameof(errors));
    }

    /// <summary>The problems found. Empty when the payload is valid.</summary>
    public IReadOnlyList<A2UIValidationError> Errors { get; }

    /// <summary>Whether the payload is valid.</summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>Throws when the payload is invalid.</summary>
    /// <exception cref="A2UIValidationException">The payload is invalid.</exception>
    public void ThrowIfInvalid()
    {
        if (!IsValid)
        {
            throw new A2UIValidationException(this);
        }
    }

    /// <inheritdoc />
    public override string ToString() => IsValid
        ? "valid"
        : string.Join(Environment.NewLine, Errors.Select(e => e.ToString()));
}
