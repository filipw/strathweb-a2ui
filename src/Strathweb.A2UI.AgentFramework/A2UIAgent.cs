using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Strathweb.A2UI.Messages;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>Wraps an agent so the surfaces its tools emit reach the renderer.</summary>
public sealed class A2UIAgent : DelegatingAIAgent
{
    private readonly A2UIAgentOptions agentOptions;
    private readonly A2UIInboundNormalizer normalizer;

    /// <summary>Wraps an agent.</summary>
    /// <param name="innerAgent">The agent to wrap.</param>
    /// <param name="options">How to handle A2UI.</param>
    public A2UIAgent(AIAgent innerAgent, A2UIAgentOptions? options = null)
        : base(innerAgent)
    {
        agentOptions = options ?? new A2UIAgentOptions();
        normalizer = new A2UIInboundNormalizer(agentOptions.Profile);
    }

    /// <summary>The options this agent was configured with.</summary>
    public A2UIAgentOptions Options => agentOptions;

    /// <inheritdoc />
    protected override async Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var sink = new A2UISurfaceSink();
        var inbound = Receive(messages, session);

        AgentResponse response;
        using (A2UIEmitter.BeginScope(sink))
        using (A2UIRunContext.BeginScope(inbound))
        {
            response = await base.RunCoreAsync(inbound.Messages, session, options, cancellationToken)
                .ConfigureAwait(false);
        }

        var emitted = sink.Drain();
        if (emitted.Count == 0)
        {
            return response;
        }

        Record(session, emitted);
        Attach(response, emitted);
        return response;
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sink = new A2UISurfaceSink();
        var inbound = Receive(messages, session);

        using var emitterScope = A2UIEmitter.BeginScope(sink);
        using var inboundScope = A2UIRunContext.BeginScope(inbound);

        await foreach (var update in base
                           .RunCoreStreamingAsync(inbound.Messages, session, options, cancellationToken)
                           .ConfigureAwait(false))
        {
            yield return update;

            if (!agentOptions.StreamSurfacesAsTheyAppear)
            {
                continue;
            }

            // A surface that is ready should be painted now, not after the model stops talking.
            foreach (var content in Emit(session, sink.Drain()))
            {
                yield return content;
            }
        }

        foreach (var content in Emit(session, sink.Drain()))
        {
            yield return content;
        }
    }

    /// <summary>
    /// Unpacks whatever A2UI the renderer sent, so tools can read the user's answers and the model
    /// gets a sentence rather than a JSON blob.
    /// </summary>
    private A2UIInboundResult Receive(IEnumerable<ChatMessage> messages, AgentSession? session)
    {
        var materialized = messages as IReadOnlyList<ChatMessage> ?? [.. messages];

        return agentOptions.NormalizeInboundActions
            ? normalizer.Normalize(materialized, session?.GetA2UISurfaceRegistry())
            : EmptyInbound(materialized);
    }

    private static A2UIInboundResult EmptyInbound(IReadOnlyList<ChatMessage> messages) =>
        new(
            messages,
            [],
            [],
            null,
            new Dictionary<string, System.Text.Json.Nodes.JsonNode?>(StringComparer.Ordinal),
            []);

    private IEnumerable<AgentResponseUpdate> Emit(AgentSession? session, IReadOnlyList<A2UIContent> emitted)
    {
        if (emitted.Count == 0)
        {
            yield break;
        }

        Record(session, emitted);

        foreach (var content in emitted)
        {
            yield return new AgentResponseUpdate(ChatRole.Assistant, (IList<AIContent>)[content]);
        }
    }

    private static void Attach(AgentResponse response, IReadOnlyList<A2UIContent> emitted)
    {
        var message = response.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant);
        if (message is null)
        {
            message = new ChatMessage(ChatRole.Assistant, (IList<AIContent>)[]);
            response.Messages.Add(message);
        }

        foreach (var content in emitted)
        {
            message.Contents.Add(content);
        }
    }

    private void Record(AgentSession? session, IReadOnlyList<A2UIContent> emitted)
    {
        if (session is null)
        {
            return;
        }

        var registry = session.GetA2UISurfaceRegistry();
        registry.Capacity = agentOptions.SurfaceHistoryCapacity;

        var now = DateTimeOffset.UtcNow;
        foreach (var content in emitted)
        {
            foreach (var message in content.Messages)
            {
                switch (message)
                {
                    case CreateSurfaceMessage create:
                        registry.Add(create.SurfaceId, create.CatalogId, now);
                        break;

                    case DeleteSurfaceMessage delete:
                        registry.Remove(delete.SurfaceId);
                        break;
                }
            }
        }

        session.SetA2UISurfaceRegistry(registry);
    }
}
