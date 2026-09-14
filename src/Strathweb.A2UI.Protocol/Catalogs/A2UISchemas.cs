using System.Reflection;
using System.Text.Json.Nodes;

namespace Strathweb.A2UI.Catalogs;

/// <summary>The specification's JSON Schemas, embedded from the vendored specification.</summary>
public static class A2UISchemas
{
    private const string ServerToClientResource = "Strathweb.A2UI.Schemas.v0_9_1.server_to_client.json";
    private const string CommonTypesResource = "Strathweb.A2UI.Catalogs.v0_9_1.common_types.json";

    /// <summary>The URI the v0.9 catalogs use to refer to the common types document.</summary>
    public const string CommonTypesUri = "https://a2ui.org/specification/v0_9/common_types.json";

    /// <summary>The schema every agent-to-renderer message must satisfy.</summary>
    /// <param name="version">The protocol version.</param>
    /// <returns>A fresh copy of the schema document.</returns>
    /// <exception cref="NotSupportedException">No schema ships for <paramref name="version"/>.</exception>
    public static JsonObject AgentToRenderer(A2UIVersion version) => Load(version, ServerToClientResource);

    /// <summary>The shared type definitions the catalogs and message schemas refer to.</summary>
    /// <param name="version">The protocol version.</param>
    /// <returns>A fresh copy of the schema document.</returns>
    /// <exception cref="NotSupportedException">No schema ships for <paramref name="version"/>.</exception>
    public static JsonObject CommonTypes(A2UIVersion version) => Load(version, CommonTypesResource);

    private static JsonObject Load(A2UIVersion version, string resource)
    {
        if (version is not (A2UIVersion.V0_9 or A2UIVersion.V0_9_1))
        {
            throw new NotSupportedException(
                $"No schemas ship for A2UI {A2UIVersions.ToWireString(version)}; only v0.9.1 is implemented.");
        }

        using var stream = typeof(A2UISchemas).GetTypeInfo().Assembly.GetManifestResourceStream(resource)
            ?? throw new A2UICatalogException($"Embedded schema '{resource}' is missing from the assembly.");

        return JsonNode.Parse(stream)?.AsObject()
            ?? throw new A2UICatalogException($"Embedded schema '{resource}' is not an object.");
    }
}
