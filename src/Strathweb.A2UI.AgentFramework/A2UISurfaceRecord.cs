using System.Text.Json.Serialization;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>One surface this session has shown.</summary>
public sealed class A2UISurfaceRecord
{
    /// <summary>The surface's identifier.</summary>
    [JsonPropertyName("surfaceId")]
    public string SurfaceId { get; set; } = string.Empty;

    /// <summary>The catalog its components came from.</summary>
    [JsonPropertyName("catalogId")]
    public string? CatalogId { get; set; }

    /// <summary>When it was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }
}
