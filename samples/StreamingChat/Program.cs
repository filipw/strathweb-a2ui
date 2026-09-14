using A2A;
using Microsoft.Agents.AI.Hosting;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;
using Strathweb.A2UI;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.AgentFramework;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Samples;
using StreamingChat;
using StreamingChat.Components;

var builder = WebApplication.CreateBuilder(args);

// What the A2UI library logs shows up in the page's wire log next to the traffic.
var demoLogs = new DemoLogProvider();
builder.Logging.AddProvider(demoLogs);
builder.Services.AddSingleton(demoLogs);

// A real model when a key is configured, otherwise a script that plays one.
var chatClient = SampleModels.TryCreateOpenAI(builder.Configuration) ?? new TripPlannerScript();

var agent = chatClient
    .AsAIAgent(
        instructions: TripPlanner.Instructions,
        name: "trip-planner",
        description: "Plans short trips, collecting the details with a form.",
        tools: [AIFunctionFactory.Create(TripPlanner.PlanTrip, "plan_trip")])
    .WithA2UI(options => options.LoggerFactory = LoggerFactory.Create(logging => logging.AddProvider(demoLogs)));

// The agent, hosted over A2A in this same process. The page talks to it the way any client would.
builder.Services.AddKeyedSingleton<AgentSessionStore>(agent.Name, new InMemoryAgentSessionStore());
builder.Services.AddA2AServer(agent);

// The page: Blazor Server, one chat session per circuit, speaking message/stream to /a2a.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHttpClient();
builder.Services.AddScoped(services => new A2UIChatSession(() => new A2AClient(
    new Uri(new Uri(services.GetRequiredService<NavigationManager>().BaseUri), "a2a"),
    services.GetRequiredService<IHttpClientFactory>().CreateClient())));
builder.Services.AddSingleton(new SampleInfo(SampleModels.Describe(chatClient)));

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapA2AJsonRpc(agent, "/a2a");
app.MapGet("/.well-known/agent-card.json", () => new AgentCard
{
    Name = agent.Name!,
    Description = agent.Description ?? string.Empty,
    Version = "0.1.0",
    DefaultInputModes = ["text/plain"],
    DefaultOutputModes = ["text/plain", A2UIMediaTypes.A2UIJson],
    Capabilities = new AgentCapabilities { Streaming = true },
}.AddA2UIExtension(A2UIVersion.V0_9_1, supportedCatalogIds: [A2UICatalogs.Basic(A2UIVersion.V0_9_1).CatalogId]));

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
