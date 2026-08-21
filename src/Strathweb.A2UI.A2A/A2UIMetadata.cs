using System.Text.Json;
using System.Text.Json.Nodes;

namespace Strathweb.A2UI.A2A;

/// <summary>Reads and writes the A2UI entries in an A2A message's metadata.</summary>
public static class A2UIMetadata
{
    /// <summary>Reads the renderer's capabilities from already-parsed message metadata.</summary>
    /// <param name="metadata">The metadata, keyed as it arrived.</param>
    /// <param name="profile">The version whose keys and shapes to use.</param>
    /// <param name="capabilities">The capabilities, when this method returns <see langword="true"/>.</param>
    /// <returns><see langword="false"/> when the metadata carries no capabilities for this version.</returns>
    public static bool TryReadCapabilities(
        IReadOnlyDictionary<string, JsonNode?>? metadata,
        A2UIVersionProfile profile,
        out A2UIRendererCapabilities capabilities)
    {
        ArgumentNullException.ThrowIfNull(profile);

        capabilities = null!;
        return metadata is not null &&
               metadata.TryGetValue(profile.CapabilitiesMetadataKey, out var node) &&
               A2UIRendererCapabilities.TryParse(node, profile, out capabilities);
    }

    /// <summary>Reads the renderer's surface data models from already-parsed message metadata.</summary>
    /// <param name="metadata">The metadata, keyed as it arrived.</param>
    /// <param name="profile">The version whose keys and shapes to use.</param>
    /// <param name="dataModel">The data models, when this method returns <see langword="true"/>.</param>
    /// <returns><see langword="false"/> when the metadata carries no data models for this version.</returns>
    public static bool TryReadDataModel(
        IReadOnlyDictionary<string, JsonNode?>? metadata,
        A2UIVersionProfile profile,
        out A2UIRendererDataModel dataModel)
    {
        ArgumentNullException.ThrowIfNull(profile);

        dataModel = null!;
        return metadata is not null &&
               metadata.TryGetValue(profile.DataModelMetadataKey, out var node) &&
               A2UIRendererDataModel.TryParse(node, out dataModel);
    }

    /// <summary>Reads the renderer's capabilities from message metadata.</summary>
    /// <param name="metadata">The A2A message's metadata.</param>
    /// <param name="profile">The version whose keys and shapes to use.</param>
    /// <param name="capabilities">The capabilities, when this method returns <see langword="true"/>.</param>
    /// <returns><see langword="false"/> when the metadata carries no capabilities for this version.</returns>
    public static bool TryReadCapabilities(
        IDictionary<string, JsonElement>? metadata,
        A2UIVersionProfile profile,
        out A2UIRendererCapabilities capabilities)
    {
        ArgumentNullException.ThrowIfNull(profile);

        capabilities = null!;
        return TryReadNode(metadata, profile.CapabilitiesMetadataKey, out var node) &&
               A2UIRendererCapabilities.TryParse(node, profile, out capabilities);
    }

    /// <summary>Reads the renderer's surface data models from message metadata.</summary>
    /// <param name="metadata">The A2A message's metadata.</param>
    /// <param name="profile">The version whose keys and shapes to use.</param>
    /// <param name="dataModel">The data models, when this method returns <see langword="true"/>.</param>
    /// <returns><see langword="false"/> when the metadata carries no data models for this version.</returns>
    public static bool TryReadDataModel(
        IDictionary<string, JsonElement>? metadata,
        A2UIVersionProfile profile,
        out A2UIRendererDataModel dataModel)
    {
        ArgumentNullException.ThrowIfNull(profile);

        dataModel = null!;
        return TryReadNode(metadata, profile.DataModelMetadataKey, out var node) &&
               A2UIRendererDataModel.TryParse(node, out dataModel);
    }

    /// <summary>Writes capabilities into message metadata. Mainly useful for building test doubles.</summary>
    /// <param name="metadata">The metadata to write into.</param>
    /// <param name="capabilities">The capabilities to advertise.</param>
    /// <param name="profile">The version whose keys and shapes to use.</param>
    public static void WriteCapabilities(
        IDictionary<string, JsonElement> metadata,
        A2UIRendererCapabilities capabilities,
        A2UIVersionProfile profile)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(profile);

        metadata[profile.CapabilitiesMetadataKey] = A2UIParts.ToElement(capabilities.ToJson(profile));
    }

    /// <summary>Writes surface data models into message metadata.</summary>
    /// <param name="metadata">The metadata to write into.</param>
    /// <param name="dataModel">The data models to send.</param>
    /// <param name="profile">The version whose keys and shapes to use.</param>
    public static void WriteDataModel(
        IDictionary<string, JsonElement> metadata,
        A2UIRendererDataModel dataModel,
        A2UIVersionProfile profile)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(dataModel);
        ArgumentNullException.ThrowIfNull(profile);

        metadata[profile.DataModelMetadataKey] = A2UIParts.ToElement(dataModel.ToJson());
    }

    private static bool TryReadNode(
        IDictionary<string, JsonElement>? metadata,
        string key,
        out JsonNode? node)
    {
        node = null;

        if (metadata is null || !metadata.TryGetValue(key, out var value))
        {
            return false;
        }

        node = JsonNode.Parse(value.GetRawText());
        return node is not null;
    }
}
