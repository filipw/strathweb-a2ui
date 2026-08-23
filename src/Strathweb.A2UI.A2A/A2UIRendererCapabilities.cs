using System.Text.Json.Nodes;
using Strathweb.A2UI.Catalogs;

namespace Strathweb.A2UI.A2A;

/// <summary>What a renderer says it can render, sent in the metadata of the A2A messages it posts.</summary>
public sealed class A2UIRendererCapabilities
{
    /// <summary>Creates capabilities.</summary>
    /// <param name="supportedCatalogIds">The catalogs the renderer can render.</param>
    public A2UIRendererCapabilities(IEnumerable<string> supportedCatalogIds)
    {
        ArgumentNullException.ThrowIfNull(supportedCatalogIds);
        SupportedCatalogIds = [.. supportedCatalogIds];
    }

    /// <summary>The catalogs the renderer can render, by id.</summary>
    public IReadOnlyList<string> SupportedCatalogIds { get; }

    /// <summary>Catalogs the renderer supplied inline, when it was told the agent accepts them.</summary>
    public IReadOnlyList<A2UICatalog> InlineCatalogs { get; init; } = [];

    /// <summary>Whether the renderer says it can render a catalog.</summary>
    /// <param name="catalogId">The catalog id.</param>
    /// <returns><see langword="true"/> when the catalog is listed.</returns>
    public bool Supports(string catalogId)
    {
        ArgumentNullException.ThrowIfNull(catalogId);

        foreach (var supported in SupportedCatalogIds)
        {
            if (string.Equals(supported, catalogId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Writes the capabilities object for a version.</summary>
    /// <param name="profile">The version whose shape to write.</param>
    /// <returns>The value to place under the profile's capabilities metadata key.</returns>
    public JsonObject ToJson(A2UIVersionProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var ids = new JsonArray();
        foreach (var id in SupportedCatalogIds)
        {
            ids.Add((JsonNode)JsonValue.Create(id)!);
        }

        var body = new JsonObject { ["supportedCatalogIds"] = ids };

        if (InlineCatalogs.Count > 0)
        {
            var catalogs = new JsonArray();
            foreach (var catalog in InlineCatalogs)
            {
                catalogs.Add((JsonNode)catalog.ToJson());
            }

            body["inlineCatalogs"] = catalogs;
        }

        return new JsonObject { [profile.CapabilitiesVersionKey] = body };
    }

    /// <summary>Reads capabilities from the value of a capabilities metadata key.</summary>
    /// <param name="node">The metadata value.</param>
    /// <param name="profile">The version whose shape to read.</param>
    /// <param name="capabilities">The capabilities, when this method returns <see langword="true"/>.</param>
    /// <returns><see langword="false"/> when the value does not describe capabilities for this version.</returns>
    public static bool TryParse(
        JsonNode? node,
        A2UIVersionProfile profile,
        out A2UIRendererCapabilities capabilities)
    {
        ArgumentNullException.ThrowIfNull(profile);

        capabilities = null!;

        if (node is not JsonObject obj)
        {
            return false;
        }

        JsonArray? ids = null;
        JsonObject? body = null;
        foreach (var key in profile.AcceptedCapabilitiesVersionKeys)
        {
            if (obj[key] is JsonObject candidate && candidate["supportedCatalogIds"] is JsonArray candidateIds)
            {
                body = candidate;
                ids = candidateIds;
                break;
            }
        }

        if (body is null || ids is null)
        {
            return false;
        }

        var supported = new List<string>(ids.Count);
        foreach (var id in ids)
        {
            if (id is JsonValue value && value.TryGetValue<string>(out var text))
            {
                supported.Add(text);
            }
        }

        var inline = new List<A2UICatalog>();
        if (body["inlineCatalogs"] is JsonArray catalogs)
        {
            foreach (var catalog in catalogs)
            {
                try
                {
                    inline.Add(A2UICatalog.FromJson(catalog));
                }
                catch (A2UICatalogException)
                {
                    // A renderer's malformed inline catalog must not take down the whole handshake.
                }
            }
        }

        capabilities = new A2UIRendererCapabilities(supported) { InlineCatalogs = inline };
        return true;
    }
}
