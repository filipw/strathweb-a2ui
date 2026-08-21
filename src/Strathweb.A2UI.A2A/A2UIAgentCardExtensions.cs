using System.Text.Json.Nodes;
using A2A;

namespace Strathweb.A2UI.A2A;

/// <summary>Advertises A2UI support on an A2A agent card.</summary>
public static class A2UIAgentCardExtensions
{
    /// <summary>Adds the A2UI extension to a card's capabilities.</summary>
    /// <param name="card">The card to extend.</param>
    /// <param name="version">The protocol version to advertise.</param>
    /// <param name="supportedCatalogIds">
    /// The catalogs this agent can emit surfaces from. Omitted from the advertised parameters when empty.
    /// </param>
    /// <param name="acceptsInlineCatalogs">
    /// Whether renderers may send their own catalog definitions. An inline catalog is untrusted
    /// third-party input; leave this false unless the renderers are known to be trusted.
    /// </param>
    /// <param name="required">
    /// Whether a client must activate the extension to talk to this agent. Leave
    /// <see langword="false"/> so clients that do not render A2UI still work.
    /// </param>
    /// <returns>The same card, for chaining.</returns>
    public static AgentCard AddA2UIExtension(
        this AgentCard card,
        A2UIVersion version,
        IEnumerable<string>? supportedCatalogIds = null,
        bool acceptsInlineCatalogs = false,
        bool required = false)
    {
        ArgumentNullException.ThrowIfNull(card);

        card.Capabilities ??= new AgentCapabilities();
        card.Capabilities.Extensions ??= [];

        var uri = A2UIExtensionUris.For(version);
        card.Capabilities.Extensions.RemoveAll(e => string.Equals(e.Uri, uri, StringComparison.Ordinal));

        var extension = new AgentExtension
        {
            Uri = uri,
            Description = "Declarative UI surfaces over A2A, per the A2UI specification.",
        };

        if (required)
        {
            extension.Required = true;
        }

        if (BuildParams(supportedCatalogIds, acceptsInlineCatalogs) is { } parameters)
        {
            extension.Params = A2UIParts.ToElement(parameters);
        }

        card.Capabilities.Extensions.Add(extension);
        return card;
    }

    /// <summary>The A2UI extension URIs a card advertises, newest first.</summary>
    /// <param name="card">The card to read.</param>
    /// <returns>The URIs, in no particular order. Empty when the card advertises none.</returns>
    public static IReadOnlyList<string> GetA2UIExtensionUris(this AgentCard card)
    {
        ArgumentNullException.ThrowIfNull(card);

        var uris = new List<string>();
        foreach (var extension in card.Capabilities?.Extensions ?? [])
        {
            if (A2UIExtensionUris.IsA2UIExtension(extension.Uri))
            {
                uris.Add(extension.Uri);
            }
        }

        return uris;
    }

    /// <summary>
    /// Builds the extension's <c>params</c> object, or null when there is nothing to declare.
    /// </summary>
    private static JsonObject? BuildParams(IEnumerable<string>? supportedCatalogIds, bool acceptsInlineCatalogs)
    {
        JsonObject? parameters = null;

        if (supportedCatalogIds is not null)
        {
            var ids = new JsonArray();
            foreach (var id in supportedCatalogIds)
            {
                ids.Add((JsonNode)JsonValue.Create(id)!);
            }

            if (ids.Count > 0)
            {
                (parameters ??= [])["supportedCatalogIds"] = ids;
            }
        }

        if (acceptsInlineCatalogs)
        {
            (parameters ??= [])["acceptsInlineCatalogs"] = true;
        }

        return parameters;
    }
}
