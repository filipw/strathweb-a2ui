using System.Text.Json;
using System.Text.Json.Nodes;
using A2A;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Serialization;

namespace Strathweb.A2UI.A2A;

/// <summary>Builds and reads the A2A data parts that carry A2UI messages.</summary>
public static class A2UIParts
{
    /// <summary>The metadata key carrying a part's MIME type.</summary>
    public const string MimeTypeMetadataKey = "mimeType";

    /// <summary>Builds a data part carrying messages.</summary>
    /// <param name="messages">The messages. May be a single message; the wire shape is still an array.</param>
    /// <returns>A part ready to attach to an A2A message or artifact.</returns>
    public static Part Create(IEnumerable<A2UIMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var part = Part.FromData(ToElement(A2UIJson.ToJsonArray(messages)));
        part.Metadata = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            [MimeTypeMetadataKey] = ToElement(JsonValue.Create(A2UIMediaTypes.A2UIJson)),
        };

        return part;
    }

    /// <summary>Builds a data part carrying one message.</summary>
    /// <param name="message">The message.</param>
    /// <returns>A part ready to attach to an A2A message or artifact.</returns>
    public static Part Create(A2UIMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return Create([message]);
    }

    /// <summary>Whether a part carries A2UI messages.</summary>
    /// <param name="part">The part to test.</param>
    /// <returns><see langword="true"/> when the part's <c>mimeType</c> metadata marks it as A2UI.</returns>
    public static bool IsA2UI(Part? part) => A2UIMediaTypes.IsA2UI(ReadMimeType(part));

    /// <summary>Reads the messages out of a part.</summary>
    /// <param name="part">The part to read.</param>
    /// <param name="messages">The messages, in order, when this method returns <see langword="true"/>.</param>
    /// <returns><see langword="false"/> when the part is not an A2UI data part.</returns>
    /// <exception cref="A2UIParseException">
    /// The part is marked as A2UI but its payload is not an array of A2UI message envelopes.
    /// </exception>
    public static bool TryRead(Part? part, out IReadOnlyList<A2UIMessage> messages)
    {
        messages = [];

        if (part is null || !IsA2UI(part) || part.Data is not { } data)
        {
            return false;
        }

        var node = JsonNode.Parse(data.GetRawText());
        messages = node is JsonArray array
            ? A2UIJson.ListFromJsonNode(array)

            // A single message that was not wrapped. The spec requires an array, but reading one
            // costs nothing and a peer that gets it wrong is otherwise silently ignored.
            : [A2UIJson.FromJsonNode(node)];

        return true;
    }

    /// <summary>
    /// Reads the messages out of a part, reporting rather than throwing when one of them is
    /// malformed.
    /// </summary>
    /// <param name="part">The part to read.</param>
    /// <param name="messages">The messages that could be read.</param>
    /// <param name="errors">One entry per message that could not be read, in payload order.</param>
    /// <returns><see langword="false"/> when the part is not an A2UI data part.</returns>
    public static bool TryReadTolerant(
        Part? part,
        out IReadOnlyList<A2UIMessage> messages,
        out IReadOnlyList<string> errors)
    {
        messages = [];
        errors = [];

        if (part is null || !IsA2UI(part) || part.Data is not { } data)
        {
            return false;
        }

        var node = JsonNode.Parse(data.GetRawText());
        var items = node is JsonArray array ? array : [node];

        var read = new List<A2UIMessage>(items.Count);
        var failures = new List<string>();

        for (var i = 0; i < items.Count; i++)
        {
            try
            {
                read.Add(A2UIJson.FromJsonNode(items[i]));
            }
            catch (A2UIParseException ex)
            {
                failures.Add($"messages.{i}: {ex.Message}");
            }
        }

        messages = read;
        errors = failures;
        return true;
    }

    private static string? ReadMimeType(Part? part)
    {
        if (part?.Metadata is not { } metadata ||
            !metadata.TryGetValue(MimeTypeMetadataKey, out var value) ||
            value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return value.GetString();
    }

    /// <summary>
    /// Detaches a <see cref="JsonElement"/> from the document that produced it, so it stays valid
    /// after the document is disposed.
    /// </summary>
    internal static JsonElement ToElement(JsonNode? node)
    {
        using var document = JsonDocument.Parse(node?.ToJsonString() ?? "null");
        return document.RootElement.Clone();
    }
}
