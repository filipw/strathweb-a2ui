using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Strathweb.A2UI.Messages;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>Wraps an agent so the surfaces its tools emit reach the renderer.</summary>
public sealed class A2UIAgent : DelegatingAIAgent
{
    private readonly A2UIAgentOptions agentOptions;
    private readonly A2UIInboundNormalizer normalizer;
    private readonly ILogger logger;

    /// <summary>Wraps an agent.</summary>
    /// <param name="innerAgent">The agent to wrap.</param>
    /// <param name="options">How to handle A2UI.</param>
    public A2UIAgent(AIAgent innerAgent, A2UIAgentOptions? options = null)
        : base(innerAgent)
    {
        ArgumentNullException.ThrowIfNull(innerAgent);

        agentOptions = options ?? new A2UIAgentOptions();

        var loggerFactory = agentOptions.LoggerFactory
            ?? innerAgent.GetService<ILoggerFactory>()
            ?? NullLoggerFactory.Instance;

        logger = loggerFactory.CreateLogger<A2UIAgent>();
        normalizer = new A2UIInboundNormalizer(agentOptions.Profile, agentOptions.InboundLimits, logger);
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
        var inbound = Receive(messages, session);
        var sink = CreateSink(inbound);

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

        Track(session, emitted);
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
        var inbound = Receive(messages, session);
        var sink = CreateSink(inbound);

        // An AsyncLocal written inside an async iterator is reverted every time control returns to
        // the consumer, so a scope opened once here would be gone by the time a tool runs after the
        // first update. The scopes are re-entered around every step of the inner run instead.
        var inner = base.RunCoreStreamingAsync(inbound.Messages, session, options, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        try
        {
            while (true)
            {
                bool moved;
                using (A2UIEmitter.BeginScope(sink))
                using (A2UIRunContext.BeginScope(inbound))
                {
                    moved = await inner.MoveNextAsync().ConfigureAwait(false);
                }

                if (!moved)
                {
                    break;
                }

                yield return inner.Current;

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
        }
        finally
        {
            await inner.DisposeAsync().ConfigureAwait(false);
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

    private A2UISurfaceSink CreateSink(A2UIInboundResult inbound) =>
        new(inbound.RendererCapabilities, agentOptions.UnsupportedCatalogPolicy, logger);

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

        Track(session, emitted);

        foreach (var content in emitted)
        {
            yield return new AgentResponseUpdate(ChatRole.Assistant, (IList<AIContent>)[content]);
        }
    }

    /// <summary>
    /// Adds the surfaces to the response as a message of their own. The inner agent has already put
    /// its messages into the chat history it keeps in the session; adding to one of those would put
    /// content the framework cannot serialize into every session store.
    /// </summary>
    private static void Attach(AgentResponse response, IReadOnlyList<A2UIContent> emitted)
    {
        var last = response.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant);

        response.Messages.Add(new ChatMessage(ChatRole.Assistant, (IList<AIContent>)[.. emitted])
        {
            AuthorName = last?.AuthorName,
            MessageId = Guid.NewGuid().ToString("N"),
        });
    }

    /// <summary>Logs what was sent and remembers the surfaces in the session, when there is one.</summary>
    private void Track(AgentSession? session, IReadOnlyList<A2UIContent> emitted)
    {
        var registry = session?.GetA2UISurfaceRegistry();
        if (registry is null)
        {
            logger.NoSession();
        }
        else
        {
            registry.Capacity = agentOptions.SurfaceHistoryCapacity;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var content in emitted)
        {
            foreach (var message in content.Messages)
            {
                switch (message)
                {
                    case CreateSurfaceMessage create:
                        logger.SurfaceCreated(create.SurfaceId, create.CatalogId);
                        registry?.Add(create.SurfaceId, create.CatalogId, now);
                        break;

                    case DeleteSurfaceMessage delete:
                        logger.SurfaceDeleted(delete.SurfaceId);
                        registry?.Remove(delete.SurfaceId);
                        break;

                    case UpdateComponentsMessage update:
                        logger.MessageSent(message.Kind, update.SurfaceId);
                        break;

                    case UpdateDataModelMessage update:
                        logger.MessageSent(message.Kind, update.SurfaceId);
                        break;
                }
            }
        }

        if (registry is not null)
        {
            session!.SetA2UISurfaceRegistry(registry);
        }
    }
}
