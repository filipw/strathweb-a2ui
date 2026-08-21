using System.Text.Json.Nodes;
using Strathweb.A2UI.Internal;

namespace Strathweb.A2UI.Messages;

/// <summary>Reports a renderer-side error. Sent by the renderer.</summary>
public sealed class ErrorMessage : A2UIMessage
{
    /// <summary>The code the schema reserves for a payload that failed the renderer's validation.</summary>
    public const string ValidationFailedCode = "VALIDATION_FAILED";

    /// <summary>Creates an <c>error</c> message.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="surfaceId">The surface the error relates to.</param>
    /// <param name="message">A one or two sentence description of what went wrong.</param>
    public ErrorMessage(string code, string surfaceId, string message)
    {
        Code = Throw.IfNullOrEmpty(code, nameof(code));
        SurfaceId = Throw.IfNullOrEmpty(surfaceId, nameof(surfaceId));
        Message = Throw.IfNull(message, nameof(message));
    }

    /// <inheritdoc />
    public override A2UIMessageKind Kind => A2UIMessageKind.Error;

    /// <inheritdoc />
    public override A2UIMessageDirection Direction => A2UIMessageDirection.RendererToAgent;

    /// <inheritdoc />
    public override string WireKey => "error";

    /// <summary>The error code.</summary>
    public string Code { get; }

    /// <summary>The surface the error relates to.</summary>
    public string SurfaceId { get; }

    /// <summary>A short description of the failure.</summary>
    public string Message { get; }

    /// <summary>
    /// A JSON Pointer to the field that failed validation, for example <c>/components/0/text</c>.
    /// Required when <see cref="Code"/> is <see cref="ValidationFailedCode"/>.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Extra properties carried by a non-validation error, which the schema permits. Empty for a
    /// <see cref="ValidationFailedCode"/> error, where extras are forbidden.
    /// </summary>
    public JsonObject? Extensions { get; init; }
}
