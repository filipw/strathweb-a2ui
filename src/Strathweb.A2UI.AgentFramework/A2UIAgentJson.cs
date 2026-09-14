using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>Serialization support for the types this library adds to an agent's messages and session.</summary>
public static class A2UIAgentJson
{
    /// <summary>The type discriminator <see cref="A2UIContent"/> is written with.</summary>
    public const string A2UIContentTypeDiscriminator = "strathweb.a2ui";

    /// <summary>The options to pass when reading or writing A2UI session state.</summary>
    public static JsonSerializerOptions Options { get; } = Create();

    /// <summary>
    /// Teaches a set of options to write and read <see cref="A2UIContent"/> wherever an
    /// <see cref="AIContent"/> is expected, for example when persisting an <c>AgentResponse</c>.
    /// </summary>
    /// <param name="options">
    /// Writable options, typically <c>new JsonSerializerOptions(AIJsonUtilities.DefaultOptions)</c>.
    /// The framework's own defaults are read-only and cannot be extended.
    /// </param>
    public static void AddA2UIContentType(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.AddAIContentType<A2UIContent>(A2UIContentTypeDiscriminator);
    }

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
