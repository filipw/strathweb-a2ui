using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Strathweb.A2UI.TestSupport;

/// <summary>
/// Stands in for a model that answers with scripted text, one reply per call. Streaming splits the
/// reply into small chunks so tags and JSON are cut at arbitrary places, the way real tokens are.
/// </summary>
internal sealed class ScriptedTextChatClient(params string[] replies) : IChatClient
{
    private int calls;

    /// <summary>How many characters each streamed chunk carries.</summary>
    public int ChunkSize { get; init; } = 7;

    /// <summary>Every request the model received, oldest first.</summary>
    public List<IReadOnlyList<ChatMessage>> Requests { get; } = [];

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Requests.Add(messages.ToList());
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, Next())));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Requests.Add(messages.ToList());
        var reply = Next();
        var messageId = Guid.NewGuid().ToString("N");

        for (var i = 0; i < reply.Length; i += ChunkSize)
        {
            await Task.Yield();
            var chunk = reply.Substring(i, Math.Min(ChunkSize, reply.Length - i));
            yield return new ChatResponseUpdate(ChatRole.Assistant, chunk) { MessageId = messageId, ResponseId = messageId };
        }
    }

    /// <summary>The next scripted reply; the last one repeats once the script runs out.</summary>
    private string Next()
    {
        var index = Interlocked.Increment(ref calls) - 1;
        return replies[Math.Min(index, replies.Length - 1)];
    }
}
