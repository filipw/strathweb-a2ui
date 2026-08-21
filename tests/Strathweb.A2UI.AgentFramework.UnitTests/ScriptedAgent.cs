using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Strathweb.A2UI.AgentFramework.UnitTests;

/// <summary>
/// An agent that runs a callback instead of a model, standing in for the tool calls a real run would
/// make.
/// </summary>
internal sealed class ScriptedAgent(Action run, string reply = "Done.") : AIAgent
{
    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default) =>
        new(new ScriptedSession());

    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
        AgentSession session,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default) =>
        new(session.StateBag.Serialize());

    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
        JsonElement serializedSession,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default) =>
        new(new ScriptedSession(AgentSessionStateBag.Deserialize(serializedSession)));

    protected override Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        run();
        return Task.FromResult(new AgentResponse(new ChatMessage(ChatRole.Assistant, reply)));
    }

    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        run();
        await Task.Yield();
        yield return new AgentResponseUpdate(ChatRole.Assistant, reply);
    }
}

/// <summary>A session that keeps its state in memory, nothing more.</summary>
internal sealed class ScriptedSession : AgentSession
{
    internal ScriptedSession()
    {
    }

    internal ScriptedSession(AgentSessionStateBag stateBag)
        : base(stateBag)
    {
    }
}
