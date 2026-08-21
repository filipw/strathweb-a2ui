namespace Strathweb.A2UI.A2A;

/// <summary>The MIME types that mark an A2A data part as carrying A2UI messages.</summary>
public static class A2UIMediaTypes
{
    /// <summary>The MIME type this library writes: <c>application/a2ui+json</c>.</summary>
    public const string A2UIJson = "application/a2ui+json";

    /// <summary>The spelling used by v0.8 and early v0.9 peers: <c>application/json+a2ui</c>.</summary>
    public const string DeprecatedA2UIJson = "application/json+a2ui";

    /// <summary>Whether a MIME type marks a part as A2UI, in either spelling.</summary>
    /// <param name="mediaType">The value of the part's <c>mimeType</c> metadata.</param>
    /// <returns><see langword="true"/> for either spelling.</returns>
    public static bool IsA2UI(string? mediaType) =>
        string.Equals(mediaType, A2UIJson, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(mediaType, DeprecatedA2UIJson, StringComparison.OrdinalIgnoreCase);
}
