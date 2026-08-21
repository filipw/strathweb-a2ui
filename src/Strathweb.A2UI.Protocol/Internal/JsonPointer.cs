using System.Globalization;
using System.Text.Json.Nodes;

namespace Strathweb.A2UI.Internal;

/// <summary>
/// The subset of RFC 6901 the data model needs: reading, writing and removing a value at a pointer,
/// creating intermediate objects on the way in.
/// </summary>
internal static class JsonPointer
{
    internal static bool IsWholeDocument(string? pointer) =>
        pointer is null || pointer.Length == 0 || pointer == "/";

    internal static string[] Parse(string pointer)
    {
        if (IsWholeDocument(pointer))
        {
            return [];
        }

        if (pointer[0] != '/')
        {
            throw new ArgumentException(
                $"A JSON Pointer must be empty or start with '/'; got '{pointer}'.",
                nameof(pointer));
        }

        var segments = pointer.Substring(1).Split('/');
        for (var i = 0; i < segments.Length; i++)
        {
            // ~1 before ~0, or an escaped tilde would be decoded twice.
            segments[i] = segments[i].Replace("~1", "/").Replace("~0", "~");
        }

        return segments;
    }

    internal static JsonNode? Get(JsonNode? root, string pointer)
    {
        var current = root;
        foreach (var segment in Parse(pointer))
        {
            switch (current)
            {
                case JsonObject obj when obj.TryGetPropertyValue(segment, out var next):
                    current = next;
                    break;

                case JsonArray array when TryParseIndex(segment, out var index) && index < array.Count:
                    current = array[index];
                    break;

                default:
                    return null;
            }
        }

        return current;
    }

    /// <summary>
    /// Writes <paramref name="value"/> at <paramref name="pointer"/>, creating missing objects along
    /// the way, and returns the (possibly replaced) root.
    /// </summary>
    internal static JsonNode? Set(JsonNode? root, string pointer, JsonNode? value)
    {
        var segments = Parse(pointer);
        if (segments.Length == 0)
        {
            return value;
        }

        root ??= new JsonObject();
        var current = root;

        for (var i = 0; i < segments.Length - 1; i++)
        {
            current = Descend(current, segments[i], segments[i + 1]);
        }

        Assign(current, segments[segments.Length - 1], value);
        return root;
    }

    internal static JsonNode? Remove(JsonNode? root, string pointer)
    {
        var segments = Parse(pointer);
        if (segments.Length == 0)
        {
            return null;
        }

        var current = root;
        for (var i = 0; i < segments.Length - 1 && current is not null; i++)
        {
            current = current switch
            {
                JsonObject obj => obj[segments[i]],
                JsonArray array when TryParseIndex(segments[i], out var index) && index < array.Count =>
                    array[index],
                _ => null,
            };
        }

        var last = segments[segments.Length - 1];
        switch (current)
        {
            case JsonObject obj:
                obj.Remove(last);
                break;

            case JsonArray array when TryParseIndex(last, out var index) && index < array.Count:
                array.RemoveAt(index);
                break;
        }

        return root;
    }

    private static JsonNode Descend(JsonNode current, string segment, string nextSegment)
    {
        switch (current)
        {
            case JsonObject obj:
                {
                    if (obj[segment] is { } existing and (JsonObject or JsonArray))
                    {
                        return existing;
                    }

                    // The next segment decides whether the missing level is a list or a map.
                    JsonNode created = TryParseIndex(nextSegment, out _) ? new JsonArray() : new JsonObject();
                    obj[segment] = created;
                    return created;
                }

            case JsonArray array when TryParseIndex(segment, out var index):
                {
                    while (array.Count <= index)
                    {
                        array.Add((JsonNode?)null);
                    }

                    if (array[index] is { } existing and (JsonObject or JsonArray))
                    {
                        return existing;
                    }

                    JsonNode created = TryParseIndex(nextSegment, out _) ? new JsonArray() : new JsonObject();
                    array[index] = created;
                    return created;
                }

            default:
                throw new ArgumentException(
                    $"Cannot descend into '{segment}': the value there is not an object or an array.");
        }
    }

    private static void Assign(JsonNode current, string segment, JsonNode? value)
    {
        switch (current)
        {
            case JsonObject obj:
                obj[segment] = value;
                return;

            case JsonArray array when segment == "-":
                array.Add(value);
                return;

            case JsonArray array when TryParseIndex(segment, out var index):
                while (array.Count <= index)
                {
                    array.Add((JsonNode?)null);
                }

                array[index] = value;
                return;

            default:
                throw new ArgumentException(
                    $"Cannot write '{segment}': the value there is not an object or an array.");
        }
    }

    private static bool TryParseIndex(string segment, out int index) =>
        int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out index);
}
