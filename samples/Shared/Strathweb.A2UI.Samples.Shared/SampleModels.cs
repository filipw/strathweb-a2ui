using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

namespace Strathweb.A2UI.Samples;

/// <summary>Picks the model a sample runs on: OpenAI when a key is configured, otherwise the sample's script.</summary>
public static class SampleModels
{
    /// <summary>
    /// Creates an OpenAI chat client from <c>OpenAI:ApiKey</c> (or <c>OPENAI_API_KEY</c>) and
    /// <c>OpenAI:Model</c>, or returns <see langword="null"/> when no key is configured.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The client, or <see langword="null"/>.</returns>
    public static IChatClient? TryCreateOpenAI(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var apiKey = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        var model = configuration["OpenAI:Model"] ?? "gpt-4.1-mini";
        return new OpenAIClient(apiKey).GetChatClient(model).AsIChatClient();
    }

    /// <summary>A one-line description of which model is in use, for the page header.</summary>
    /// <param name="chatClient">The client chosen.</param>
    /// <returns>The description.</returns>
    public static string Describe(IChatClient chatClient) =>
        chatClient is ScriptedChatClient
            ? "Running on a scripted stand-in for the model. Set OpenAI:ApiKey or OPENAI_API_KEY to use a real one."
            : "Running on OpenAI.";
}
