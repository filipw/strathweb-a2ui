using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Surfaces;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>Collects what a run wants to show, in the order it was emitted.</summary>
public sealed class A2UISurfaceSink : IA2UISurfaceSink
{
    private readonly List<A2UIContent> emitted = [];
    // net8.0 has no System.Threading.Lock, and this guards a list, not a hot path.
    private readonly object gate = new();
    private readonly A2UIRendererCapabilities? capabilities;
    private readonly A2UIUnsupportedCatalogPolicy policy;
    private readonly ILogger logger;

    /// <summary>Creates a sink that accepts everything.</summary>
    public A2UISurfaceSink()
        : this(null, A2UIUnsupportedCatalogPolicy.Warn)
    {
    }

    /// <summary>Creates a sink that checks each new surface against what the renderer said it can render.</summary>
    /// <param name="rendererCapabilities">
    /// What the renderer advertised this turn, or <see langword="null"/> when it said nothing, in
    /// which case nothing is checked.
    /// </param>
    /// <param name="unsupportedCatalogPolicy">What to do about a surface from a catalog the renderer did not list.</param>
    /// <param name="logger">Where to report. Defaults to nowhere.</param>
    public A2UISurfaceSink(
        A2UIRendererCapabilities? rendererCapabilities,
        A2UIUnsupportedCatalogPolicy unsupportedCatalogPolicy,
        ILogger? logger = null)
    {
        capabilities = rendererCapabilities;
        policy = unsupportedCatalogPolicy;
        this.logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Everything emitted so far.</summary>
    public IReadOnlyList<A2UIContent> Emitted
    {
        get
        {
            lock (gate)
            {
                return [.. emitted];
            }
        }
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// The surface's catalog is one the renderer did not list, and the policy is
    /// <see cref="A2UIUnsupportedCatalogPolicy.Throw"/>.
    /// </exception>
    public void Emit(A2UISurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);
        Add(new A2UIContent(surface));
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// A <c>createSurface</c> names a catalog the renderer did not list, and the policy is
    /// <see cref="A2UIUnsupportedCatalogPolicy.Throw"/>.
    /// </exception>
    public void Emit(IReadOnlyList<A2UIMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);
        Add(new A2UIContent(messages));
    }

    /// <summary>Takes everything emitted so far and clears the sink.</summary>
    /// <returns>The drained content, in emission order.</returns>
    public IReadOnlyList<A2UIContent> Drain()
    {
        lock (gate)
        {
            if (emitted.Count == 0)
            {
                return [];
            }

            var drained = emitted.ToArray();
            emitted.Clear();
            return drained;
        }
    }

    private void Add(A2UIContent content)
    {
        if (!Accept(content))
        {
            return;
        }

        // Tools can run concurrently.
        lock (gate)
        {
            emitted.Add(content);
        }
    }

    /// <summary>
    /// A renderer that does not know a catalog draws nothing and says nothing, so the emit call is
    /// the only place the mismatch can be caught.
    /// </summary>
    private bool Accept(A2UIContent content)
    {
        if (capabilities is null)
        {
            return true;
        }

        foreach (var message in content.Messages)
        {
            if (message is not CreateSurfaceMessage { CatalogId: { } catalogId } create || capabilities.Supports(catalogId))
            {
                continue;
            }

            var supported = string.Join(", ", capabilities.SupportedCatalogIds);
            logger.CatalogNotSupported(create.SurfaceId, catalogId, supported, policy);

            switch (policy)
            {
                case A2UIUnsupportedCatalogPolicy.Drop:
                    return false;

                case A2UIUnsupportedCatalogPolicy.Throw:
                    throw new InvalidOperationException(
                        $"Surface '{create.SurfaceId}' uses catalog '{catalogId}', which the renderer did not " +
                        $"list among the catalogs it can render ({supported}).");
            }
        }

        return true;
    }
}
