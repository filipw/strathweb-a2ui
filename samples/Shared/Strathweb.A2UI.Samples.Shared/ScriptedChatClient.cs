using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Strathweb.A2UI.Samples;

/// <summary>
/// A stand-in for a model so a sample runs without an API key. A subclass decides what to say from the
/// conversation so far; this class streams it the way a real provider does, a few words at a time,
/// with any tool calls at the end.
/// </summary>
public abstract class ScriptedChatClient : IChatClient
{
    /// <summary>How long to wait between streamed chunks, so the streaming is visible.</summary>
    public TimeSpan ChunkDelay { get; init; } = TimeSpan.FromMilliseconds(35);

    /// <summary>Decides the reply to a conversation.</summary>
    /// <param name="history">Every message the model has been given, oldest first.</param>
    /// <returns>The reply.</returns>
    protected abstract ScriptedReply Reply(IReadOnlyList<ChatMessage> history);

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var reply = Reply(messages.ToList());
        var contents = new List<AIContent>();
        if (reply.Text is { Length: > 0 })
        {
            contents.Add(new TextContent(reply.Text));
        }

        contents.AddRange(reply.ToolCalls);
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, contents)));
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var reply = Reply(messages.ToList());
        var messageId = Guid.NewGuid().ToString("N");

        foreach (var chunk in Chunks(reply.Text ?? string.Empty))
        {
            await Task.Delay(ChunkDelay, cancellationToken).ConfigureAwait(false);
            yield return new ChatResponseUpdate(ChatRole.Assistant, chunk) { MessageId = messageId, ResponseId = messageId };
        }

        if (reply.ToolCalls.Count > 0)
        {
            await Task.Delay(ChunkDelay, cancellationToken).ConfigureAwait(false);
            yield return new ChatResponseUpdate(ChatRole.Assistant, [.. reply.ToolCalls]) { MessageId = messageId, ResponseId = messageId };
        }
    }

    /// <summary>The last thing the user said, as the model sees it.</summary>
    protected static string LastUserText(IReadOnlyList<ChatMessage> history)
    {
        ArgumentNullException.ThrowIfNull(history);
        return history.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? string.Empty;
    }

    /// <summary>Whether the latest user turn is an A2UI action the renderer sent back.</summary>
    protected static bool IsAction(IReadOnlyList<ChatMessage> history, string actionName) =>
        LastUserText(history).Contains($"performed the \"{actionName}\" action", StringComparison.Ordinal);

    /// <summary>Whether the latest user turn is the library asking the model to repair an invalid A2UI block.</summary>
    protected static bool IsRepairRequest(IReadOnlyList<ChatMessage> history) =>
        LastUserText(history).StartsWith("Your previous response was invalid", StringComparison.Ordinal);

    /// <summary>Whether the model has already called a tool and is now being handed its result.</summary>
    protected static bool HasToolResult(IReadOnlyList<ChatMessage> history)
    {
        ArgumentNullException.ThrowIfNull(history);
        return history.Count > 0 && history[^1].Contents.OfType<FunctionResultContent>().Any();
    }

    /// <summary>Splits text into word-sized chunks, keeping the whitespace.</summary>
    private static IEnumerable<string> Chunks(string text)
    {
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsWhiteSpace(text[i]) && i > start)
            {
                yield return text.Substring(start, i - start + 1);
                start = i + 1;
            }
        }

        if (start < text.Length)
        {
            yield return text.Substring(start);
        }
    }
}
