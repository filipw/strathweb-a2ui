using System.ComponentModel;
using A2A;
using Microsoft.Extensions.AI;
using OpenAI;
using Strathweb.A2UI;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.AgentFramework;
using Strathweb.A2UI.Catalogs;
using SurveyAgent;

var builder = WebApplication.CreateBuilder(args);

// Any IChatClient will do; OpenAI is here only so the sample runs out of the box.
var apiKey = builder.Configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var model = builder.Configuration["OpenAI:Model"] ?? "gpt-4.1-mini";

const string Instructions = """
    You help people who have just finished a support conversation.

    When it is time to ask how the conversation went, call ask_for_user_satisfaction. It puts a form
    on the user's screen. Do not repeat the question or describe the form afterwards. Say at most one
    short sentence, then wait for their answer.
    """;

[Description("Show the user a satisfaction survey and wait for them to fill it in.")]
static string AskForUserSatisfaction(
    [Description("The question to put at the top of the survey.")] string question)
{
    A2UIEmitter.Emit(SatisfactionSurvey.Build(question));

    // The return value is prompt text. Without it the model narrates the form it has just shown.
    return "A satisfaction survey is now on the user's screen. Do not describe it. Wait for their answer.";
}

if (apiKey is { Length: > 0 })
{
    var agent = new OpenAIClient(apiKey)
        .GetChatClient(model)
        .AsIChatClient()
        .AsAIAgent(
            instructions: Instructions,
            name: "survey-agent",
            description: "Asks how a support conversation went, using a form rather than prose.",
            tools: [AIFunctionFactory.Create(AskForUserSatisfaction, "ask_for_user_satisfaction")])
        .WithA2UI();

    builder.Services.AddA2AServer(agent);
    builder.Services.AddSingleton(agent);
}

builder.Services.AddRouting();

var app = builder.Build();

if (apiKey is { Length: > 0 })
{
    app.MapA2AJsonRpc(app.Services.GetRequiredService<A2UIAgent>(), "/a2a");
}

// The agent card a renderer reads to discover that this agent speaks A2UI.
app.MapGet("/.well-known/agent-card.json", () => new AgentCard
{
    Name = "survey-agent",
    Description = "Asks how a support conversation went, using a form rather than prose.",
    Version = "0.1.0",
    DefaultInputModes = ["text/plain"],
    DefaultOutputModes = ["text/plain"],
    Capabilities = new AgentCapabilities { Streaming = true },
}.AddA2UIExtension(
    A2UIVersion.V0_9_1,
    supportedCatalogIds: [A2UICatalogs.Basic(A2UIVersion.V0_9_1).CatalogId]));

// The survey on its own, with no model involved. Paste the result into the A2UI Composer to see what
// the renderer will draw, or use it to check a renderer without wiring up an agent first.
app.MapGet("/surfaces/satisfaction", (string? question) =>
{
    var surface = SatisfactionSurvey.Build(
        question ?? "How satisfied were you with how we handled this?",
        surfaceId: "satisfaction_preview");

    return Results.Text(
        A2UIJson.ToJsonArray(surface.Messages).ToJsonString(new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
        }),
        "application/a2ui+json");
});

app.MapGet("/", () => Results.Text(
    apiKey is { Length: > 0 }
        ? """
          survey-agent

            POST /a2a                        A2A JSON-RPC endpoint
            GET  /.well-known/agent-card.json
            GET  /surfaces/satisfaction      the surface on its own, for pasting into a renderer
          """
        : """
          survey-agent (no model configured)

            Set OpenAI:ApiKey in user secrets, or OPENAI_API_KEY in the environment, to enable /a2a.

            GET  /surfaces/satisfaction      the surface on its own, for pasting into a renderer
          """,
    "text/plain"));

app.Run();
