using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Strathweb.A2UI.TestSupport;

/// <summary>
/// Stands in for a model that calls one tool and then answers. It streams the way real providers do,
/// text first and the function call last, so anything that only works before the first update is
/// caught.
/// </summary>
internal sealed class ToolCallingChatClient(string toolName, string reply = "Done.") : IChatClient
{
    private int calls;

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
        var request = Record(messages);

        var message = ShouldCallTool(request)
            ? new ChatMessage(
                ChatRole.Assistant,
                [new TextContent("One moment. "), new FunctionCallContent(NextCallId(), toolName)])
            : new ChatMessage(ChatRole.Assistant, reply);

        return Task.FromResult(new ChatResponse(message));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = Record(messages);
        var messageId = Guid.NewGuid().ToString("N");

        await Task.Yield();

        if (!ShouldCallTool(request))
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, reply) { MessageId = messageId, ResponseId = messageId };
            yield break;
        }

        yield return new ChatResponseUpdate(ChatRole.Assistant, "One moment. ") { MessageId = messageId, ResponseId = messageId };

        await Task.Yield();

        yield return new ChatResponseUpdate(ChatRole.Assistant, [new FunctionCallContent(NextCallId(), toolName)])
        {
            MessageId = messageId,
            ResponseId = messageId,
        };
    }

    private List<ChatMessage> Record(IEnumerable<ChatMessage> messages)
    {
        var request = messages.ToList();
        Requests.Add(request);
        return request;
    }

    /// <summary>The tool is called once per conversation: never after its own result is in the history.</summary>
    private static bool ShouldCallTool(IReadOnlyList<ChatMessage> request) =>
        !request.Any(m => m.Contents.OfType<FunctionResultContent>().Any());

    private string NextCallId() => $"call_{Interlocked.Increment(ref calls)}";
}
