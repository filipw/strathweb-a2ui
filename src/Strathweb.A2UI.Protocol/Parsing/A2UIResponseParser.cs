using System.Text.Json;
using System.Text.Json.Nodes;
using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Serialization;

namespace Strathweb.A2UI.Parsing;

/// <summary>Splits a complete model response into prose and A2UI blocks.</summary>
public static class A2UIResponseParser
{
    /// <summary>Parses a response.</summary>
    /// <param name="response">The model's output.</param>
    /// <returns>
    /// The parts in order. Each block is returned together with the prose that introduced it; prose
    /// after the last block is returned as a part of its own.
    /// </returns>
    /// <exception cref="A2UIParseException">
    /// The response contains no A2UI block, a block is empty, or a block is not valid JSON.
    /// </exception>
    public static IReadOnlyList<A2UIResponsePart> Parse(string response)
    {
        Throw.IfNull(response, nameof(response));

        var parts = new List<A2UIResponsePart>();
        var position = 0;
        var found = false;

        while (position < response.Length)
        {
            var open = response.IndexOf(A2UIResponseTags.Open, position, StringComparison.Ordinal);
            if (open < 0)
            {
                break;
            }

            var leadingText = response.Substring(position, open - position).Trim();
            var bodyStart = open + A2UIResponseTags.Open.Length;
            var close = response.IndexOf(A2UIResponseTags.Close, bodyStart, StringComparison.Ordinal);

            if (close < 0)
            {
                throw new A2UIParseException(
                    $"An A2UI block was opened but never closed with '{A2UIResponseTags.Close}'.");
            }

            found = true;
            parts.Add(A2UIResponsePart.Create(
                leadingText,
                ParseBlock(response.Substring(bodyStart, close - bodyStart))));

            position = close + A2UIResponseTags.Close.Length;
        }

        if (!found)
        {
            throw new A2UIParseException(
                $"An A2UI block delimited by '{A2UIResponseTags.Open}' was not found in response.");
        }

        var trailing = response.Substring(position).Trim();
        if (trailing.Length > 0)
        {
            parts.Add(A2UIResponsePart.FromText(trailing));
        }

        return parts;
    }

    /// <summary>Whether a response contains at least one complete A2UI block.</summary>
    /// <param name="response">The model's output.</param>
    /// <returns>
    /// <see langword="true"/> only when both delimiters are present, in order. An opening delimiter
    /// on its own means the output was cut short, not that there is a block to read.
    /// </returns>
    public static bool ContainsA2UIBlock(string response)
    {
        Throw.IfNull(response, nameof(response));

        var open = response.IndexOf(A2UIResponseTags.Open, StringComparison.Ordinal);
        return open >= 0 &&
               response.IndexOf(A2UIResponseTags.Close, open + A2UIResponseTags.Open.Length, StringComparison.Ordinal) >= 0;
    }

    internal static JsonArray ParseBlock(string block)
    {
        var json = A2UIResponseTags.StripCodeFence(block);

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

            // A single message without its wrapping array is a common model slip and costs nothing
            // to accept; the wire format itself always uses an array.
            JsonObject obj => [obj],
            _ => throw new A2UIParseException("Failed to parse the A2UI JSON block: expected an array of messages."),
        };
    }
}
