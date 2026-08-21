using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Messages;

namespace Strathweb.A2UI.State;

/// <summary>
/// Every surface a renderer currently holds, so a message stream can be replayed without the caller
/// routing each message by surface id.
/// </summary>
public sealed class A2UISurfaceSet
{
    private readonly Dictionary<string, A2UISurfaceState> surfaces = new(StringComparer.Ordinal);

    /// <summary>The surfaces, keyed by id. Deleted surfaces are removed.</summary>
    public IReadOnlyDictionary<string, A2UISurfaceState> Surfaces => surfaces;

    /// <summary>Applies one message to the surface it names.</summary>
    /// <param name="message">The message to apply.</param>
    public void Apply(A2UIMessage message)
    {
        var surfaceId = SurfaceIdOf(Throw.IfNull(message, nameof(message)));
        if (surfaceId is null)
        {
            return;
        }

        if (!surfaces.TryGetValue(surfaceId, out var surface))
        {
            surfaces[surfaceId] = surface = new A2UISurfaceState(
                surfaceId,
                (message as CreateSurfaceMessage)?.CatalogId);
        }

        surface.Apply(message);

        if (surface.IsDeleted)
        {
            surfaces.Remove(surfaceId);
        }
    }

    /// <summary>Applies a sequence of messages in order.</summary>
    /// <param name="messages">The messages to apply.</param>
    public void Apply(IEnumerable<A2UIMessage> messages)
    {
        foreach (var message in Throw.IfNull(messages, nameof(messages)))
        {
            Apply(message);
        }
    }

    /// <summary>Looks up a surface.</summary>
    /// <param name="surfaceId">The surface id.</param>
    /// <param name="surface">The surface, when this method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when the surface is live.</returns>
    public bool TryGet(string surfaceId, out A2UISurfaceState surface) =>
        surfaces.TryGetValue(Throw.IfNull(surfaceId, nameof(surfaceId)), out surface!);

    private static string? SurfaceIdOf(A2UIMessage message) => message switch
    {
        CreateSurfaceMessage create => create.SurfaceId,
        UpdateComponentsMessage update => update.SurfaceId,
        UpdateDataModelMessage update => update.SurfaceId,
        DeleteSurfaceMessage delete => delete.SurfaceId,
        ActionMessage action => action.SurfaceId,
        _ => null,
    };
}
