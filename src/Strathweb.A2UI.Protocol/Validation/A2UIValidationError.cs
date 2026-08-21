using Strathweb.A2UI.Internal;

namespace Strathweb.A2UI.Validation;

/// <summary>One problem found in an A2UI payload.</summary>
public sealed class A2UIValidationError
{
    /// <summary>Creates an error.</summary>
    /// <param name="category">The kind of problem.</param>
    /// <param name="code">A code from <see cref="A2UIErrorCodes"/>.</param>
    /// <param name="path">A pointer to the offending location, for example <c>messages.0.version</c>.</param>
    /// <param name="message">A human-readable description.</param>
    public A2UIValidationError(
        A2UIValidationErrorCategory category,
        string code,
        string path,
        string message)
    {
        Category = category;
        Code = Throw.IfNullOrEmpty(code, nameof(code));
        Path = Throw.IfNull(path, nameof(path));
        Message = Throw.IfNull(message, nameof(message));
    }

    /// <summary>The kind of problem.</summary>
    public A2UIValidationErrorCategory Category { get; }

    /// <summary>The error code.</summary>
    public string Code { get; }

    /// <summary>Where in the payload the problem is.</summary>
    public string Path { get; }

    /// <summary>What is wrong.</summary>
    public string Message { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Category}/{Code} at {Path}: {Message}";
}
