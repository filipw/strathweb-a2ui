using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Serialization;

namespace Strathweb.A2UI.Parsing;

/// <summary>
/// Repairs the ways a model most often gets JSON wrong: markdown fences, typographic quotes, trailing
/// commas, and a single message left outside its array. Anything else is still an error.
/// </summary>
public static class A2UIPayloadRepair
{
    /// <summary>Repairs and parses a block of A2UI JSON.</summary>
    /// <param name="payload">The raw text between the response tags, or a bare JSON value.</param>
    /// <returns>The message array. A single object is wrapped.</returns>
    /// <exception cref="A2UIParseException">The text is not JSON even after repair, or is empty.</exception>
    public static JsonArray Fix(string payload)
    {
        Throw.IfNull(payload, nameof(payload));

        var json = RemoveTrailingCommas(NormalizeQuotes(A2UIResponseTags.StripCodeFence(payload)));

        if (json.Length == 0)
        {
            throw new A2UIParseException("A2UI JSON part is empty.");
        }

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new A2UIParseException($"Failed to parse the A2UI JSON block: {ex.Message}", ex);
        }

        return node switch
        {
            JsonArray array => array,
            JsonObject obj => new JsonArray(obj),
            _ => throw new A2UIParseException("Failed to parse the A2UI JSON block: expected an array of messages."),
        };
    }

    /// <summary>Turns typographic quotes into the ASCII ones JSON requires.</summary>
    /// <param name="text">The text to normalize.</param>
    /// <returns>The text with double curly quotes as <c>"</c> and single curly quotes as <c>'</c>.</returns>
    public static string NormalizeQuotes(string text)
    {
        Throw.IfNull(text, nameof(text));

        return text
            .Replace('“', '"')
            .Replace('”', '"')
            .Replace('„', '"')
            .Replace('‟', '"')
            .Replace('‘', '\'')
            .Replace('’', '\'')
            .Replace('‚', '\'')
            .Replace('‛', '\'');
    }

    /// <summary>Removes a comma that sits right before a closing bracket, outside of strings.</summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The text without trailing commas. Commas inside strings are left alone.</returns>
    public static string RemoveTrailingCommas(string json)
    {
        Throw.IfNull(json, nameof(json));

        var result = new StringBuilder(json.Length);
        var inString = false;
        var escaped = false;

        for (var i = 0; i < json.Length; i++)
        {
            var c = json[i];

            if (inString)
            {
                result.Append(c);
                if (escaped)
                {
                    escaped = false;
                }
                else if (c == '\\')
                {
                    escaped = true;
                }
                else if (c == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (c == '"')
            {
                inString = true;
                result.Append(c);
                continue;
            }

            if (c == ',' && NextSignificant(json, i + 1) is '}' or ']')
            {
                continue;
            }

            result.Append(c);
        }

        return result.ToString();
    }

    private static char? NextSignificant(string json, int from)
    {
        for (var i = from; i < json.Length; i++)
        {
            if (!char.IsWhiteSpace(json[i]))
            {
                return json[i];
            }
        }

        return null;
    }
}
