using Strathweb.A2UI.Internal;

namespace Strathweb.A2UI.Catalogs;

/// <summary>
/// Picks the catalog an agent should generate from, given what it can emit and what the renderer said
/// it can render.
/// </summary>
public static class A2UICatalogSelector
{
    /// <summary>Selects a catalog.</summary>
    /// <param name="supported">The catalogs this agent can generate from, most preferred first. At least one.</param>
    /// <param name="rendererCatalogIds">
    /// The catalog ids the renderer advertised, most preferred first, or <see langword="null"/> or
    /// empty when it advertised none.
    /// </param>
    /// <param name="inlineCatalogs">Catalogs the renderer sent inline, to be merged onto the selected one.</param>
    /// <param name="acceptsInlineCatalogs">
    /// Whether this agent trusts inline catalogs. An inline catalog is third-party input; when this
    /// is <see langword="false"/> and the renderer sent one, selection fails rather than silently
    /// ignoring it.
    /// </param>
    /// <returns>
    /// The selected catalog. With inline catalogs, a copy of the selected catalog carrying their
    /// components too; its id stays the selected catalog's.
    /// </returns>
    /// <exception cref="A2UICatalogException">
    /// The renderer listed catalogs and none is one this agent supports, or it sent inline catalogs
    /// this agent does not accept.
    /// </exception>
    public static A2UICatalog Select(
        IReadOnlyList<A2UICatalog> supported,
        IReadOnlyList<string>? rendererCatalogIds,
        IReadOnlyList<A2UICatalog>? inlineCatalogs = null,
        bool acceptsInlineCatalogs = false)
    {
        Throw.IfNull(supported, nameof(supported));

        if (supported.Count == 0)
        {
            throw new ArgumentException("At least one supported catalog is required.", nameof(supported));
        }

        var inline = inlineCatalogs ?? [];
        var hasInline = inline.Count > 0;
        if (hasInline && !acceptsInlineCatalogs)
        {
            throw new A2UICatalogException(
                "The renderer sent inline catalogs, but the agent does not accept inline catalogs.");
        }

        var selected = Match(supported, rendererCatalogIds);

        if (selected is null)
        {
            if (!hasInline)
            {
                throw new A2UICatalogException(
                    "No client-supported catalog found: the renderer listed " +
                    $"[{string.Join(", ", rendererCatalogIds!)}] and this agent supports " +
                    $"[{string.Join(", ", supported.Select(c => c.CatalogId))}].");
            }

            // The renderer brought its own components; they extend the agent's default catalog.
            selected = supported[0];
        }

        if (!hasInline)
        {
            return selected;
        }

        foreach (var catalog in inline)
        {
            selected = selected.WithComponentsFrom(catalog);
        }

        return selected;
    }

    /// <summary>
    /// The renderer's order is the priority: the first id it lists that this agent supports wins.
    /// With no list, the agent's own first choice is used.
    /// </summary>
    private static A2UICatalog? Match(IReadOnlyList<A2UICatalog> supported, IReadOnlyList<string>? rendererCatalogIds)
    {
        if (rendererCatalogIds is null || rendererCatalogIds.Count == 0)
        {
            return supported[0];
        }

        foreach (var id in rendererCatalogIds)
        {
            foreach (var catalog in supported)
            {
                if (string.Equals(catalog.CatalogId, id, StringComparison.Ordinal))
                {
                    return catalog;
                }
            }
        }

        return null;
    }
}
