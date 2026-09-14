using A2A;
using CoffeeShop;
using Microsoft.Agents.AI.Hosting;
using Strathweb.A2UI;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.AgentFramework;

var builder = WebApplication.CreateBuilder(args);

var agent = new CoffeeShopAgent().WithA2UI();

// Without a session store the A2A host hands the agent a fresh session every turn, which quietly
// disables everything that spans turns: the surface registry, and with it the data models the
// renderer reports back.
builder.Services.AddKeyedSingleton<AgentSessionStore>(agent.Name, new InMemoryAgentSessionStore());
builder.Services.AddA2AServer(agent);
builder.Services.AddRouting();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapA2AJsonRpc(agent, "/a2a");

app.MapGet("/.well-known/agent-card.json", () => new AgentCard
{
    Name = agent.Name ?? "coffee-shop",
    Description = agent.Description ?? string.Empty,
    Version = "0.1.0",
    DefaultInputModes = ["text/plain"],
    DefaultOutputModes = ["text/plain"],
    Capabilities = new AgentCapabilities { Streaming = true },
}.AddA2UIExtension(
    A2UIVersion.V0_9_1,
    supportedCatalogIds: [CoffeeShopSurfaces.Catalog.CatalogId]));

app.Run();
