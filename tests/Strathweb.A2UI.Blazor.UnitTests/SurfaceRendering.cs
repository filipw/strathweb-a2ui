using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Strathweb.A2UI.Rendering;
using Strathweb.A2UI.Surfaces;

namespace Strathweb.A2UI.Blazor.UnitTests;

/// <summary>Renders a surface to static HTML the way prerendering does, so markup can be asserted on.</summary>
internal static class SurfaceRendering
{
    internal static async Task<string> RenderAsync(A2UISurface surface)
    {
        var render = new A2UIRenderSurface(surface.SurfaceId, surface.CatalogId);
        render.Apply(surface.Messages);
        return await RenderAsync(render);
    }

    internal static async Task<string> RenderAsync(A2UIRenderSurface surface)
    {
        await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(A2UISurfaceView.Surface)] = surface,
            });

            var output = await renderer.RenderComponentAsync<A2UISurfaceView>(parameters);
            return output.ToHtmlString();
        });
    }
}
