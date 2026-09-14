using A2A;
using BookingWizard;
using BookingWizard.Components;
using Microsoft.Agents.AI.Hosting;
using Microsoft.AspNetCore.Components;
using Strathweb.A2UI;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.AgentFramework;
using Strathweb.A2UI.Samples;

var builder = WebApplication.CreateBuilder(args);

var demoLogs = new DemoLogProvider();
builder.Logging.AddProvider(demoLogs);
builder.Services.AddSingleton(demoLogs);

// No model: the wizard is driven by the actions the renderer sends back.
var agent = new BookingAgent().WithA2UI(options => options.LoggerFactory = LoggerFactory.Create(logging => logging.AddProvider(demoLogs)));

// The wizard reads the data model the renderer reports back, which only works when the session
// survives between turns.
builder.Services.AddKeyedSingleton<AgentSessionStore>(agent.Name, new InMemoryAgentSessionStore());
builder.Services.AddA2AServer(agent);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHttpClient();
builder.Services.AddScoped(services => new A2UIChatSession(() => new A2AClient(
    new Uri(new Uri(services.GetRequiredService<NavigationManager>().BaseUri), "a2a"),
    services.GetRequiredService<IHttpClientFactory>().CreateClient())));
builder.Services.AddSingleton(new SampleInfo("No model involved: the agent reacts to the actions the form sends."));

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
}.AddA2UIExtension(A2UIVersion.V0_9_1, supportedCatalogIds: [BookingSurfaces.Catalog.CatalogId]));

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
