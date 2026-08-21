using A2A;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.TestHost;

namespace Strathweb.A2UI.IntegrationTests;

/// <summary>Hosts an agent over A2A in process, so a test can talk to it the way a renderer would.</summary>
internal sealed class A2AHost : IAsyncDisposable
{
    private readonly IHost host;
    private readonly HttpClient httpClient;

    private A2AHost(IHost host)
    {
        this.host = host;
        httpClient = host.GetTestClient();
        Client = new A2AClient(new Uri("http://localhost/a2a"), httpClient);
    }

    /// <summary>The A2A client, pointed at the in-process server.</summary>
    internal A2AClient Client { get; }

    internal static async Task<A2AHost> StartAsync(AIAgent agent)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddA2AServer(agent);
                })
                .Configure(app => app.UseRouting().UseEndpoints(endpoints =>
                    endpoints.MapA2AJsonRpc(agent, "/a2a"))))
            .Build();

        await host.StartAsync();
        return new A2AHost(host);
    }

    public async ValueTask DisposeAsync()
    {
        await host.StopAsync();
        host.Dispose();
        Client.Dispose();
        httpClient.Dispose();
    }
}
