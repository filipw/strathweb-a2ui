using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Messages;

namespace Strathweb.A2UI.Serialization;

/// <summary>
/// Reads and writes an A2UI message envelope, dispatching on the single message key that sits
/// alongside <c>version</c>.
/// </summary>
public sealed class A2UIMessageConverter : JsonConverter<A2UIMessage>
{
    /// <inheritdoc />
    public override A2UIMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        A2UIWire.FromJson(JsonNode.Parse(ref reader));

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, A2UIMessage value, JsonSerializerOptions options)
    {
        Throw.IfNull(writer, nameof(writer));
        Throw.IfNull(value, nameof(value));
        A2UIWire.ToJson(value).WriteTo(writer);
    }
}
