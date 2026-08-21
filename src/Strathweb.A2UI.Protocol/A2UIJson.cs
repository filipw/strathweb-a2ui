using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Serialization;

namespace Strathweb.A2UI;

/// <summary>Serialization entry points for A2UI messages.</summary>
public static class A2UIJson
{
    /// <summary>The options the pre-built <see cref="JsonTypeInfo{T}"/> instances below are bound to.</summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    /// <summary>The converter for a single message, for adding to your own options.</summary>
    public static JsonConverter<A2UIMessage> MessageConverter { get; } = new A2UIMessageConverter();

    /// <summary>The converter for a message array, for adding to your own options.</summary>
    public static JsonConverter<IReadOnlyList<A2UIMessage>> MessageListConverter { get; } =
        new A2UIMessageListConverter();

    /// <summary>Type metadata for a single message.</summary>
    public static JsonTypeInfo<A2UIMessage> MessageTypeInfo { get; } =
        JsonMetadataServices.CreateValueInfo<A2UIMessage>(Options, MessageConverter);

    /// <summary>Type metadata for the message array carried by an A2A data part.</summary>
    public static JsonTypeInfo<IReadOnlyList<A2UIMessage>> MessageListTypeInfo { get; } =
        JsonMetadataServices.CreateValueInfo<IReadOnlyList<A2UIMessage>>(Options, MessageListConverter);

    /// <summary>Serializes one message.</summary>
    /// <param name="message">The message.</param>
    /// <returns>Compact JSON.</returns>
    public static string Serialize(A2UIMessage message) =>
        JsonSerializer.Serialize(Throw.IfNull(message, nameof(message)), MessageTypeInfo);

    /// <summary>Serializes a list of messages as a JSON array.</summary>
    /// <param name="messages">The messages.</param>
    /// <returns>Compact JSON.</returns>
    public static string Serialize(IReadOnlyList<A2UIMessage> messages) =>
        JsonSerializer.Serialize(Throw.IfNull(messages, nameof(messages)), MessageListTypeInfo);

    /// <summary>Deserializes one message.</summary>
    /// <param name="json">The JSON to read.</param>
    /// <returns>The message.</returns>
    /// <exception cref="A2UIParseException">The JSON is not an A2UI message envelope.</exception>
    public static A2UIMessage Deserialize(string json) =>
        JsonSerializer.Deserialize(Throw.IfNull(json, nameof(json)), MessageTypeInfo)
        ?? throw new A2UIParseException("An A2UI message must be a JSON object.");

    /// <summary>Deserializes a JSON array of messages.</summary>
    /// <param name="json">The JSON to read.</param>
    /// <returns>The messages, in order.</returns>
    /// <exception cref="A2UIParseException">The JSON is not an array of A2UI message envelopes.</exception>
    public static IReadOnlyList<A2UIMessage> DeserializeList(string json) =>
        JsonSerializer.Deserialize(Throw.IfNull(json, nameof(json)), MessageListTypeInfo)
        ?? throw new A2UIParseException("An A2UI message list must be a JSON array.");

    /// <summary>Writes a message as a <see cref="JsonObject"/>, for embedding without re-parsing.</summary>
    /// <param name="message">The message.</param>
    /// <returns>A new object.</returns>
    public static JsonObject ToJsonObject(A2UIMessage message) =>
        A2UIWire.ToJson(Throw.IfNull(message, nameof(message)));

    /// <summary>Writes messages as a <see cref="JsonArray"/>, the shape an A2A data part carries.</summary>
    /// <param name="messages">The messages.</param>
    /// <returns>A new array.</returns>
    public static JsonArray ToJsonArray(IEnumerable<A2UIMessage> messages)
    {
        Throw.IfNull(messages, nameof(messages));

        var array = new JsonArray();
        foreach (var message in messages)
        {
            array.Add((JsonNode)A2UIWire.ToJson(message));
        }

        return array;
    }

    /// <summary>Reads a message from an already-parsed node.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The message.</returns>
    /// <exception cref="A2UIParseException">The node is not an A2UI message envelope.</exception>
    public static A2UIMessage FromJsonNode(JsonNode? node) => A2UIWire.FromJson(node);

    /// <summary>Reads messages from an already-parsed array.</summary>
    /// <param name="node">The node, which must be a JSON array.</param>
    /// <returns>The messages, in order.</returns>
    /// <exception cref="A2UIParseException">The node is not an array of A2UI message envelopes.</exception>
    public static IReadOnlyList<A2UIMessage> ListFromJsonNode(JsonNode? node)
    {
        if (node is not JsonArray array)
        {
            throw new A2UIParseException("An A2UI message list must be a JSON array.");
        }

        var messages = new List<A2UIMessage>(array.Count);
        foreach (var item in array)
        {
            messages.Add(A2UIWire.FromJson(item));
        }

        return messages;
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,

            // An empty resolver keeps MakeReadOnly happy without pulling in the reflection-based
            // default resolver, which would put trim and AOT warnings into every consumer.
            TypeInfoResolver = JsonTypeInfoResolver.Combine(),
        };

        options.Converters.Add(new A2UIMessageConverter());
        options.Converters.Add(new A2UIMessageListConverter());
        options.MakeReadOnly();
        return options;
    }
}
