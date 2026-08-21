using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Messages;

namespace Strathweb.A2UI.Serialization;

/// <summary>
/// Reads and writes a JSON array of A2UI messages. An A2A data part always carries an array, even
/// for a single message.
/// </summary>
public sealed class A2UIMessageListConverter : JsonConverter<IReadOnlyList<A2UIMessage>>
{
    /// <inheritdoc />
    public override IReadOnlyList<A2UIMessage> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (JsonNode.Parse(ref reader) is not JsonArray array)
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

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyList<A2UIMessage> value,
        JsonSerializerOptions options)
    {
        Throw.IfNull(writer, nameof(writer));
        Throw.IfNull(value, nameof(value));
        writer.WriteStartArray();
        foreach (var message in value)
        {
            A2UIWire.ToJson(message).WriteTo(writer);
        }

        writer.WriteEndArray();
    }
}
