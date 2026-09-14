using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace Strathweb.A2UI.Blazor;

/// <summary>
/// The subset of Markdown a <c>Text</c> component may carry: emphasis, strong emphasis, inline code
/// and line breaks. Everything is HTML-encoded first, so agent text can never inject markup.
/// </summary>
public static class A2UIMarkdown
{
    private static readonly Regex Strong = new(@"\*\*(.+?)\*\*|__(.+?)__", RegexOptions.CultureInvariant | RegexOptions.Singleline);
    private static readonly Regex Emphasis = new(@"(?<![\w*])\*(?!\s)(.+?)(?<!\s)\*(?!\w)|(?<![\w_])_(?!\s)(.+?)(?<!\s)_(?!\w)", RegexOptions.CultureInvariant | RegexOptions.Singleline);
    private static readonly Regex Code = new(@"`([^`]+)`", RegexOptions.CultureInvariant);

    /// <summary>Renders text as safe HTML.</summary>
    /// <param name="text">The text, possibly with inline Markdown.</param>
    /// <returns>Markup that can be placed into a Razor component.</returns>
    public static MarkupString ToHtml(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new MarkupString(string.Empty);
        }

        var html = WebUtility.HtmlEncode(text);
        html = Code.Replace(html, "<code>$1</code>");
        html = Strong.Replace(html, m => "<strong>" + (m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value) + "</strong>");
        html = Emphasis.Replace(html, m => "<em>" + (m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value) + "</em>");
        html = html.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", "<br />", StringComparison.Ordinal);

        return new MarkupString(html);
    }
}
