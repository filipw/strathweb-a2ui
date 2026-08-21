using System.Globalization;

namespace Strathweb.A2UI.A2A;

/// <summary>
/// The A2A extension URIs that advertise A2UI support, and the negotiation between what a client
/// requests and what an agent advertises.
/// </summary>
public static class A2UIExtensionUris
{
    /// <summary>The prefix every A2UI extension URI shares.</summary>
    public const string Prefix = "https://a2ui.org/a2a-extension/a2ui/v";

    /// <summary>The extension URI for a protocol version.</summary>
    /// <param name="version">The version to advertise.</param>
    /// <returns>The URI, for example <c>https://a2ui.org/a2a-extension/a2ui/v0.9.1</c>.</returns>
    public static string For(A2UIVersion version) => Prefix + Trim(A2UIVersions.ToWireString(version));

    /// <summary>Whether a URI advertises A2UI at some version.</summary>
    /// <param name="uri">The URI to test.</param>
    /// <returns><see langword="true"/> when the URI is an A2UI extension URI.</returns>
    public static bool IsA2UIExtension(string? uri) => TryGetVersion(uri, out _);

    /// <summary>Whether any of a client's requested extensions is an A2UI extension.</summary>
    /// <param name="uris">The URIs the client asked to activate.</param>
    /// <returns><see langword="true"/> when at least one is an A2UI extension URI.</returns>
    public static bool ContainsA2UIExtension(IEnumerable<string>? uris)
    {
        if (uris is null)
        {
            return false;
        }

        foreach (var uri in uris)
        {
            if (IsA2UIExtension(uri))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Reads the version out of an extension URI.</summary>
    /// <param name="uri">The URI to read.</param>
    /// <param name="version">The version without its <c>v</c> prefix, such as <c>0.9.1</c>.</param>
    /// <returns><see langword="true"/> when the URI is an A2UI extension URI.</returns>
    public static bool TryGetVersion(string? uri, out string version)
    {
        if (uri is not null && uri.StartsWith(Prefix, StringComparison.Ordinal) && uri.Length > Prefix.Length)
        {
            version = uri.Substring(Prefix.Length);
            return true;
        }

        version = string.Empty;
        return false;
    }

    /// <summary>The newest A2UI extension URI in a set.</summary>
    /// <param name="uris">The URIs to choose between. Non-A2UI URIs are ignored.</param>
    /// <returns>The newest, or <see langword="null"/> when none is an A2UI extension URI.</returns>
    public static string? SelectNewest(IEnumerable<string>? uris)
    {
        if (uris is null)
        {
            return null;
        }

        string? best = null;
        foreach (var uri in uris)
        {
            if (IsA2UIExtension(uri) && (best is null || Compare(uri, best) > 0))
            {
                best = uri;
            }
        }

        return best;
    }

    /// <summary>Picks the extension to activate: the newest version both sides support.</summary>
    /// <param name="requested">What the client asked to activate.</param>
    /// <param name="advertised">What this agent advertises on its card.</param>
    /// <returns>The URI to activate, or <see langword="null"/> when there is no overlap.</returns>
    public static string? Activate(IEnumerable<string>? requested, IEnumerable<string>? advertised)
    {
        if (requested is null || advertised is null)
        {
            return null;
        }

        var supported = new HashSet<string>(advertised, StringComparer.Ordinal);
        var candidates = new List<string>();

        foreach (var uri in requested)
        {
            if (supported.Contains(uri))
            {
                candidates.Add(uri);
            }
        }

        return SelectNewest(candidates);
    }

    private static string Trim(string wireVersion) =>
        wireVersion.StartsWith('v') ? wireVersion.Substring(1) : wireVersion;

    private static int Compare(string left, string right)
    {
        TryGetVersion(left, out var leftVersion);
        TryGetVersion(right, out var rightVersion);

        var leftParts = leftVersion.Split('.');
        var rightParts = rightVersion.Split('.');
        var length = Math.Max(leftParts.Length, rightParts.Length);

        for (var i = 0; i < length; i++)
        {
            var comparison = Segment(leftParts, i).CompareTo(Segment(rightParts, i));
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return string.CompareOrdinal(leftVersion, rightVersion);
    }

    private static int Segment(string[] parts, int index) =>
        index < parts.Length &&
        int.TryParse(parts[index], NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
}
