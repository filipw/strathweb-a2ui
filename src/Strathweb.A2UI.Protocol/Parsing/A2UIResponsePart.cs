using System.Text.Json.Nodes;
using Strathweb.A2UI.Internal;

namespace Strathweb.A2UI.Parsing;

/// <summary>
/// One piece of a model response: prose meant for the user, a block of A2UI messages, or the prose
/// that introduced a block.
/// </summary>
public sealed class A2UIResponsePart
{
    private A2UIResponsePart(string? text, JsonArray? messages)
    {
        Text = text;
        Messages = messages;
    }

    /// <summary>The prose, or <see langword="null"/> when this part carries only messages.</summary>
    public string? Text { get; }

    /// <summary>
    /// The raw A2UI messages, or <see langword="null"/> when this part carries only prose. Kept as
    /// JSON so a malformed message can be reported by position rather than failing the block.
    /// </summary>
    public JsonArray? Messages { get; }

    /// <summary>Whether this part carries A2UI messages.</summary>
    public bool IsA2UI => Messages is not null;

    /// <summary>Creates a text-only part.</summary>
    /// <param name="text">The prose.</param>
    /// <returns>The part.</returns>
    public static A2UIResponsePart FromText(string text) =>
        new(Throw.IfNull(text, nameof(text)), null);

    /// <summary>Creates a messages-only part.</summary>
    /// <param name="messages">The raw messages.</param>
    /// <returns>The part.</returns>
    public static A2UIResponsePart FromMessages(JsonArray messages) =>
        new(null, Throw.IfNull(messages, nameof(messages)));

    /// <summary>Creates a part carrying a block and the prose that introduced it.</summary>
    /// <param name="text">The prose. May be empty.</param>
    /// <param name="messages">The raw messages.</param>
    /// <returns>The part.</returns>
    public static A2UIResponsePart Create(string text, JsonArray messages) =>
        new(Throw.IfNull(text, nameof(text)), Throw.IfNull(messages, nameof(messages)));

    /// <inheritdoc />
    public override string ToString() => IsA2UI
        ? $"a2ui[{Messages!.Count}]{(Text is { Length: > 0 } ? $" after {Text.Length} chars of text" : string.Empty)}"
        : $"text({Text!.Length})";
}
