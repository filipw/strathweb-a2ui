using System.Text.Json.Nodes;

namespace Strathweb.A2UI.A2A;

/// <summary>
/// The data models a renderer sends back for the surfaces that asked for them, in the metadata of
/// every A2A message it posts.
/// </summary>
public sealed class A2UIRendererDataModel
{
    /// <summary>Creates a data model payload.</summary>
    /// <param name="version">The protocol version the renderer declared.</param>
    /// <param name="surfaces">The data model of each surface, keyed by surface id.</param>
    public A2UIRendererDataModel(A2UIVersion version, IReadOnlyDictionary<string, JsonNode?> surfaces)
    {
        ArgumentNullException.ThrowIfNull(surfaces);
        Version = version;
        Surfaces = surfaces;
    }

    /// <summary>The protocol version the renderer declared.</summary>
    public A2UIVersion Version { get; }

    /// <summary>Each surface's data model, keyed by surface id.</summary>
    public IReadOnlyDictionary<string, JsonNode?> Surfaces { get; }

    /// <summary>Writes the data model object.</summary>
    /// <returns>The value to place under the profile's data model metadata key.</returns>
    public JsonObject ToJson()
    {
        var surfaces = new JsonObject();
        foreach (var pair in Surfaces)
        {
            surfaces[pair.Key] = pair.Value?.DeepClone();
        }

        return new JsonObject
        {
            ["version"] = A2UIVersions.ToWireString(Version),
            ["surfaces"] = surfaces,
        };
    }

    /// <summary>Reads a data model payload from the value of a data model metadata key.</summary>
    /// <param name="node">The metadata value.</param>
    /// <param name="dataModel">The payload, when this method returns <see langword="true"/>.</param>
    /// <returns><see langword="false"/> when the value does not describe surface data models.</returns>
    public static bool TryParse(JsonNode? node, out A2UIRendererDataModel dataModel)
    {
        dataModel = null!;

        if (node is not JsonObject obj ||
            obj["version"] is not JsonValue versionValue ||
            !versionValue.TryGetValue<string>(out var rawVersion) ||
            !A2UIVersions.TryParse(rawVersion, out var version) ||
            obj["surfaces"] is not JsonObject surfaces)
        {
            return false;
        }

        var models = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        foreach (var pair in surfaces)
        {
            models[pair.Key] = pair.Value?.DeepClone();
        }

        dataModel = new A2UIRendererDataModel(version, models);
        return true;
    }
}
