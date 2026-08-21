using System.Text.Json.Serialization;

namespace Strathweb.A2UI.AgentFramework;

// Source-generated so session state does not need the reflection-based serializer. Internal because
// the generated members are not nullable-annotated; consumers use A2UIAgentJson.Options.
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(A2UISurfaceRegistry))]
internal sealed partial class A2UIJsonContext : JsonSerializerContext
{
}
