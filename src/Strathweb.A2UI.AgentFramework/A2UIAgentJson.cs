using System.Text.Json;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>Serialization options for the state this library keeps in an agent session.</summary>
public static class A2UIAgentJson
{
    /// <summary>The options to pass when reading or writing A2UI session state.</summary>
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = A2UIJsonContext.Default,
        };

        options.MakeReadOnly();
        return options;
    }
}
