using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Validation;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>Wraps an agent so the surfaces its tools emit, or the model writes, reach the renderer.</summary>
public sealed class A2UIAgent : DelegatingAIAgent
{
    private readonly A2UIAgentOptions agentOptions;
    private readonly A2UIInboundNormalizer normalizer;
    private readonly A2UIGeneratedPayloadReader? generated;
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

        if (agentOptions.PromptFirst is { } promptFirst)
        {
            generated = new A2UIGeneratedPayloadReader(
                promptFirst.Catalog ?? A2UICatalogs.Basic(agentOptions.Version),
                agentOptions.Profile,
                promptFirst.RepairPayloads,
                logger);
        }
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

        var response = await RunInnerAsync(inbound.Messages, sink, inbound, session, options, cancellationToken)
            .ConfigureAwait(false);

        var emitted = new List<A2UIContent>(sink.Drain());

        if (generated is not null)
        {
            emitted.AddRange(await CollectGeneratedAsync(response, sink, inbound, session, options, cancellationToken)
                .ConfigureAwait(false));
        }

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
        var stream = generated is null ? null : new A2UIPromptFirstStream(generated);

        await foreach (var piece in StreamInnerAsync(inbound.Messages, sink, inbound, stream, session, options, cancellationToken)
                           .ConfigureAwait(false))
        {
            yield return piece;
        }

        if (stream is null)
        {
            yield break;
        }

        // The model may have written blocks that did not validate; give it the chance to fix them.
        var invalid = Invalid(stream.Blocks);
        var attempt = 0;

        while (invalid.Count > 0 && attempt < agentOptions.PromptFirst!.MaxModelRepairs)
        {
            attempt++;
            logger.GeneratedRepairRequested(attempt, agentOptions.PromptFirst.MaxModelRepairs);

            var repairStream = new A2UIPromptFirstStream(generated!, suppressText: true);
            await foreach (var piece in StreamInnerAsync(
                               [RepairMessage(invalid)], sink, inbound, repairStream, session, options, cancellationToken)
                               .ConfigureAwait(false))
            {
                yield return piece;
            }

            invalid = Invalid(repairStream.Blocks);
        }

        FinishInvalid(invalid);
    }

    /// <summary>Runs the inner agent once, inside the scopes tools need.</summary>
    private async Task<AgentResponse> RunInnerAsync(
        IReadOnlyList<ChatMessage> messages,
        A2UISurfaceSink sink,
        A2UIInboundResult inbound,
        AgentSession? session,
        AgentRunOptions? options,
        CancellationToken cancellationToken)
    {
        using (A2UIEmitter.BeginScope(sink))
        using (A2UIRunContext.BeginScope(inbound))
        {
            return await base.RunCoreAsync(messages, session, options, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Streams the inner agent once. An AsyncLocal written inside an async iterator is reverted every
    /// time control returns to the consumer, so the scopes are re-entered around every step rather
    /// than opened once; a scope opened at the top would be gone by the time a tool runs.
    /// </summary>
    private async IAsyncEnumerable<AgentResponseUpdate> StreamInnerAsync(
        IReadOnlyList<ChatMessage> messages,
        A2UISurfaceSink sink,
        A2UIInboundResult inbound,
        A2UIPromptFirstStream? stream,
        AgentSession? session,
        AgentRunOptions? options,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var inner = base.RunCoreStreamingAsync(messages, session, options, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        AgentResponseUpdate? last = null;

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

                last = inner.Current;

                if (stream is null)
                {
                    yield return inner.Current;
                }
                else
                {
                    foreach (var piece in stream.Process(inner.Current))
                    {
                        foreach (var update in Deliver(piece, session))
                        {
                            yield return update;
                        }
                    }
                }

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

        if (stream is not null)
        {
            foreach (var piece in stream.Complete(last))
            {
                foreach (var update in Deliver(piece, session))
                {
                    yield return update;
                }
            }
        }

        foreach (var content in Emit(session, sink.Drain()))
        {
            yield return content;
        }
    }

    private IEnumerable<AgentResponseUpdate> Deliver(A2UIPromptFirstStream.StreamPiece piece, AgentSession? session)
    {
        if (piece.Update is { } update)
        {
            yield return update;
        }

        if (piece.Content is { } content)
        {
            foreach (var emitted in Emit(session, [content]))
            {
                yield return emitted;
            }
        }
    }

    /// <summary>
    /// Reads the blocks the model wrote out of the response, strips them from its text, and asks the
    /// model to repair the ones that did not validate.
    /// </summary>
    private async Task<List<A2UIContent>> CollectGeneratedAsync(
        AgentResponse response,
        A2UISurfaceSink sink,
        A2UIInboundResult inbound,
        AgentSession? session,
        AgentRunOptions? options,
        CancellationToken cancellationToken)
    {
        var contents = new List<A2UIContent>();
        var blocks = ExtractBlocks(response, keepProse: true);
        contents.AddRange(blocks.Where(b => b.Content is not null).Select(b => b.Content!));

        var invalid = Invalid(blocks);
        var attempt = 0;

        while (invalid.Count > 0 && attempt < agentOptions.PromptFirst!.MaxModelRepairs)
        {
            attempt++;
            logger.GeneratedRepairRequested(attempt, agentOptions.PromptFirst.MaxModelRepairs);

            var repair = await RunInnerAsync([RepairMessage(invalid)], sink, inbound, session, options, cancellationToken)
                .ConfigureAwait(false);

            // Tools may have run during the repair round too.
            contents.AddRange(sink.Drain());

            var repaired = ExtractBlocks(repair, keepProse: false);
            contents.AddRange(repaired.Where(b => b.Content is not null).Select(b => b.Content!));
            invalid = Invalid(repaired);
        }

        FinishInvalid(invalid);
        return contents;
    }

    /// <summary>
    /// Pulls every block out of the assistant text in a response. The text is rewritten to the prose
    /// around the blocks, or removed when nothing but a block was there.
    /// </summary>
    private List<A2UIGeneratedBlock> ExtractBlocks(AgentResponse response, bool keepProse)
    {
        var blocks = new List<A2UIGeneratedBlock>();

        foreach (var message in response.Messages)
        {
            if (message.Role != ChatRole.Assistant)
            {
                continue;
            }

            for (var i = message.Contents.Count - 1; i >= 0; i--)
            {
                if (message.Contents[i] is not TextContent text || generated!.Read(text.Text ?? string.Empty) is not { } read)
                {
                    continue;
                }

                blocks.AddRange(read.Blocks);

                if (keepProse && read.Prose.Length > 0)
                {
                    text.Text = read.Prose;
                }
                else
                {
                    message.Contents.RemoveAt(i);
                }
            }
        }

        return blocks;
    }

    private static List<string> Invalid(IReadOnlyList<A2UIGeneratedBlock> blocks) =>
        blocks.Where(b => !b.IsValid).SelectMany(b => b.Errors).ToList();

    private ChatMessage RepairMessage(List<string> errors) =>
        new(ChatRole.User, agentOptions.PromptFirst!.RepairInstruction.Replace(
            "{errors}",
            string.Join(Environment.NewLine, errors),
            StringComparison.Ordinal));

    private void FinishInvalid(List<string> invalid)
    {
        if (invalid.Count == 0)
        {
            return;
        }

        if (agentOptions.PromptFirst!.OnInvalid == A2UIInvalidPayloadPolicy.Throw)
        {
            throw new A2UIValidationException(
                "The model wrote an A2UI payload that is still invalid after every repair attempt:" +
                Environment.NewLine + string.Join(Environment.NewLine, invalid));
        }

        logger.GeneratedBlocksDropped(invalid.Count);
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
