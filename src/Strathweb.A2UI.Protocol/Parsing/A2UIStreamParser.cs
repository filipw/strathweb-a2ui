using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Serialization;

namespace Strathweb.A2UI.Parsing;

/// <summary>Turns a stream of model output into prose and A2UI messages as they complete.</summary>
public sealed class A2UIStreamParser
{
    private readonly StringBuilder pending = new();
    private readonly JsonElementScanner scanner = new();

    private bool insideBlock;

    /// <summary>Creates a parser.</summary>
    /// <param name="maxBufferLength">The largest amount of unyielded input to hold. Defaults to one mebibyte.</param>
    public A2UIStreamParser(int maxBufferLength = 1024 * 1024)
    {
        if (maxBufferLength <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxBufferLength),
                maxBufferLength,
                "The buffer limit must be positive.");
        }

        MaxBufferLength = maxBufferLength;
    }

    /// <summary>The largest amount of unyielded input this parser will hold.</summary>
    public int MaxBufferLength { get; }

    /// <summary>Whether the parser is currently inside an A2UI block.</summary>
    public bool IsInsideA2UIBlock => insideBlock;

    /// <summary>Feeds a chunk and returns whatever it completed.</summary>
    /// <param name="chunk">The text that just arrived. May split any token.</param>
    /// <returns>The parts completed by this chunk, possibly none.</returns>
    /// <exception cref="A2UIParseException">
    /// A message inside a block is not valid JSON, or the buffer limit was exceeded.
    /// </exception>
    public IReadOnlyList<A2UIResponsePart> Feed(string chunk)
    {
        Throw.IfNull(chunk, nameof(chunk));

        pending.Append(chunk);
        if (pending.Length > MaxBufferLength)
        {
            var length = pending.Length;
            pending.Clear();
            throw new A2UIParseException(
                $"The parser buffered {length} characters without completing a part, past the " +
                $"{MaxBufferLength} character limit. Is a block missing its closing " +
                $"'{A2UIResponseTags.Close}'?");
        }

        var parts = new List<A2UIResponsePart>();
        while (Step(parts))
        {
            // Each step consumes what it can; keep going until nothing more can be completed.
        }

        return parts;
    }

    /// <summary>Signals the end of the stream and returns any trailing prose.</summary>
    /// <returns>The final parts, possibly none.</returns>
    /// <exception cref="A2UIParseException">The stream ended inside an unclosed A2UI block.</exception>
    public IReadOnlyList<A2UIResponsePart> Complete()
    {
        if (insideBlock)
        {
            pending.Clear();
            insideBlock = false;
            scanner.Reset();

            throw new A2UIParseException(
                $"The stream ended inside an A2UI block, before '{A2UIResponseTags.Close}'.");
        }

        var text = pending.ToString();
        pending.Clear();

        return text.Length > 0 ? [A2UIResponsePart.FromText(text)] : [];
    }

    private bool Step(List<A2UIResponsePart> parts)
    {
        return insideBlock ? StepInsideBlock(parts) : StepOutsideBlock(parts);
    }

    private bool StepOutsideBlock(List<A2UIResponsePart> parts)
    {
        var buffer = pending.ToString();
        var open = buffer.IndexOf(A2UIResponseTags.Open, StringComparison.Ordinal);

        if (open >= 0)
        {
            if (open > 0)
            {
                parts.Add(A2UIResponsePart.FromText(buffer.Substring(0, open)));
            }

            pending.Remove(0, open + A2UIResponseTags.Open.Length);
            insideBlock = true;
            scanner.Reset();
            return true;
        }

        // Hold back anything that could still turn out to be the start of the opening delimiter.
        var safeLength = buffer.Length - LongestPrefixOfTagAtEnd(buffer, A2UIResponseTags.Open);
        if (safeLength <= 0)
        {
            return false;
        }

        parts.Add(A2UIResponsePart.FromText(buffer.Substring(0, safeLength)));
        pending.Remove(0, safeLength);
        return false;
    }

    private bool StepInsideBlock(List<A2UIResponsePart> parts)
    {
        var buffer = pending.ToString();
        var consumed = scanner.Scan(buffer, out var messages, out var blockEnded);

        if (consumed > 0)
        {
            pending.Remove(0, consumed);
            scanner.Consume(consumed);
        }

        if (messages is { Count: > 0 })
        {
            parts.Add(A2UIResponsePart.FromMessages(messages));
        }

        if (blockEnded)
        {
            insideBlock = false;
            return true;
        }

        return consumed > 0;
    }

    /// <summary>
    /// How many characters at the end of <paramref name="buffer"/> could be the beginning of
    /// <paramref name="tag"/>. Those must not be emitted as text yet.
    /// </summary>
    private static int LongestPrefixOfTagAtEnd(string buffer, string tag)
    {
        var max = Math.Min(buffer.Length, tag.Length - 1);
        for (var length = max; length > 0; length--)
        {
            if (string.CompareOrdinal(buffer, buffer.Length - length, tag, 0, length) == 0)
            {
                return length;
            }
        }

        return 0;
    }

    /// <summary>
    /// Tracks where one JSON value ends and the next begins inside a block, without parsing the value
    /// itself.
    /// </summary>
    private sealed class JsonElementScanner
    {
        private int depth;
        private bool inString;
        private bool escaped;
        private bool sawArrayStart;
        private int scanned;
        private int valueStart = -1;

        internal void Reset()
        {
            depth = 0;
            inString = false;
            escaped = false;
            sawArrayStart = false;
            scanned = 0;
            valueStart = -1;
        }

        /// <summary>Shifts the scan position after the caller drops a consumed prefix.</summary>
        internal void Consume(int count)
        {
            scanned = Math.Max(0, scanned - count);
            if (valueStart >= 0)
            {
                valueStart = Math.Max(0, valueStart - count);
            }
        }

        internal int Scan(string buffer, out JsonArray? messages, out bool blockEnded)
        {
            messages = null;
            blockEnded = false;

            var consumed = 0;

            for (var i = scanned; i < buffer.Length; i++)
            {
                scanned = i + 1;
                var c = buffer[i];

                if (inString)
                {
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

                switch (c)
                {
                    case '"':
                        inString = true;
                        if (depth == 0 && valueStart < 0)
                        {
                            valueStart = i;
                        }

                        break;

                    case '[' when depth == 0 && !sawArrayStart:
                        // The array that wraps the messages, not a value of its own.
                        sawArrayStart = true;
                        consumed = i + 1;
                        break;

                    case '{':
                    case '[':
                        if (depth == 0)
                        {
                            valueStart = i;
                        }

                        depth++;
                        break;

                    case '}':
                    case ']':
                        if (depth == 0)
                        {
                            // The closing bracket of the wrapping array.
                            consumed = i + 1;
                            break;
                        }

                        depth--;
                        if (depth == 0 && valueStart >= 0)
                        {
                            (messages ??= []).Add(Parse(buffer.Substring(valueStart, i - valueStart + 1)));
                            valueStart = -1;
                            consumed = i + 1;
                        }

                        break;

                    case '<' when depth == 0:
                        {
                            var remaining = buffer.Length - i;
                            var compareLength = Math.Min(remaining, A2UIResponseTags.Close.Length);
                            if (string.CompareOrdinal(buffer, i, A2UIResponseTags.Close, 0, compareLength) != 0)
                            {
                                break;
                            }

                            if (remaining < A2UIResponseTags.Close.Length)
                            {
                                // A partial closing delimiter; wait for the rest, and rescan from here.
                                scanned = i;
                                return consumed;
                            }

                            Reset();
                            blockEnded = true;
                            return i + A2UIResponseTags.Close.Length;
                        }
                }
            }

            return consumed;
        }

        private static JsonNode Parse(string json)
        {
            try
            {
                return JsonNode.Parse(json)
                    ?? throw new A2UIParseException("Failed to parse the A2UI JSON block: found a bare null.");
            }
            catch (JsonException ex)
            {
                throw new A2UIParseException($"Failed to parse the A2UI JSON block: {ex.Message}", ex);
            }
        }
    }
}
