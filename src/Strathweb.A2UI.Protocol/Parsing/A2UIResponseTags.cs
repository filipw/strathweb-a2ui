namespace Strathweb.A2UI.Parsing;

/// <summary>The delimiters a model wraps A2UI output in, and the markdown fences it tends to add anyway.</summary>
public static class A2UIResponseTags
{
    /// <summary>The tag that opens an A2UI block in a model's response.</summary>
    public const string Open = "<a2ui-json>";

    /// <summary>The tag that closes an A2UI block in a model's response.</summary>
    public const string Close = "</a2ui-json>";

    /// <summary>
    /// Strips a markdown code fence from a block. Models emit them even when told not to, and the
    /// fence is not part of the JSON.
    /// </summary>
    internal static string StripCodeFence(string block)
    {
        var trimmed = block.Trim();

        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var firstLineEnd = trimmed.IndexOf('\n');
        if (firstLineEnd < 0)
        {
            return trimmed;
        }

        var body = trimmed.Substring(firstLineEnd + 1);
        var fenceEnd = body.LastIndexOf("```", StringComparison.Ordinal);

        return (fenceEnd >= 0 ? body.Substring(0, fenceEnd) : body).Trim();
    }
}
