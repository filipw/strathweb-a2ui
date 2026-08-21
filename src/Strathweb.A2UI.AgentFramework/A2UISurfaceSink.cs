using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Surfaces;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>Collects what a run wants to show, in the order it was emitted.</summary>
public sealed class A2UISurfaceSink : IA2UISurfaceSink
{
    private readonly List<A2UIContent> emitted = [];
    // net8.0 has no System.Threading.Lock, and this guards a list, not a hot path.
    private readonly object gate = new();

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
    public void Emit(A2UISurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);
        Add(new A2UIContent(surface));
    }

    /// <inheritdoc />
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
        // Tools can run concurrently.
        lock (gate)
        {
            emitted.Add(content);
        }
    }
}
