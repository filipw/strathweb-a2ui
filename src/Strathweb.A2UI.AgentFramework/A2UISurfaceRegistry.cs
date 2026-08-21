using System.Text.Json.Serialization;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>The surfaces a session has shown, so later turns can update or delete them.</summary>
public sealed class A2UISurfaceRegistry
{
    /// <summary>The key this registry is stored under in an agent session's state bag.</summary>
    public const string StateKey = "strathweb.a2ui.surfaces";

    /// <summary>The surfaces, oldest first.</summary>
    [JsonPropertyName("surfaces")]
    public List<A2UISurfaceRecord> Surfaces { get; set; } = [];

    /// <summary>How many surfaces to remember. Older ones are forgotten.</summary>
    [JsonPropertyName("capacity")]
    public int Capacity { get; set; } = 50;

    /// <summary>Records a surface, forgetting the oldest if the registry is full.</summary>
    /// <param name="surfaceId">The surface's identifier.</param>
    /// <param name="catalogId">The catalog its components came from.</param>
    /// <param name="createdAt">When it was created.</param>
    public void Add(string surfaceId, string? catalogId, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrEmpty(surfaceId);

        Surfaces.RemoveAll(s => string.Equals(s.SurfaceId, surfaceId, StringComparison.Ordinal));
        Surfaces.Add(new A2UISurfaceRecord
        {
            SurfaceId = surfaceId,
            CatalogId = catalogId,
            CreatedAt = createdAt,
        });

        while (Capacity > 0 && Surfaces.Count > Capacity)
        {
            Surfaces.RemoveAt(0);
        }
    }

    /// <summary>Forgets a surface, after it has been deleted.</summary>
    /// <param name="surfaceId">The surface's identifier.</param>
    /// <returns><see langword="true"/> when the surface was being tracked.</returns>
    public bool Remove(string surfaceId) =>
        Surfaces.RemoveAll(s => string.Equals(s.SurfaceId, surfaceId, StringComparison.Ordinal)) > 0;

    /// <summary>Whether this session created a surface.</summary>
    /// <param name="surfaceId">The surface's identifier.</param>
    /// <returns><see langword="true"/> when the surface is being tracked.</returns>
    public bool Contains(string surfaceId) =>
        Surfaces.Exists(s => string.Equals(s.SurfaceId, surfaceId, StringComparison.Ordinal));

    /// <summary>The most recently created surface, or <see langword="null"/> when none is tracked.</summary>
    public A2UISurfaceRecord? MostRecent => Surfaces.Count == 0 ? null : Surfaces[Surfaces.Count - 1];
}
