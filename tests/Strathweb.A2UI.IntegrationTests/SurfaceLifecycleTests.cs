using System.Text.Json.Nodes;
using A2A;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.AgentFramework;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.State;
using Strathweb.A2UI.Surfaces;

namespace Strathweb.A2UI.IntegrationTests;

/// <summary>
/// A surface created, changed twice and removed across four turns, with the renderer's view
/// reconstructed after each.
/// </summary>
public class SurfaceLifecycleTests
{
    private static readonly A2UICatalog Basic = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

    private const string SurfaceId = "order_1";

    [Fact]
    public async Task ASurfaceCanBeCreated_Changed_AndRemovedAcrossFourTurns()
    {
        var script = new Queue<Action>(
        [
            () => A2UIEmitter.Emit(BuildOrder()),
            () => A2UIEmitter.SetData(SurfaceId, "/status", JsonValue.Create("Preparing")),
            () => A2UIEmitter.Emit(A2UISurfaceUpdate.For(SurfaceId, Basic)
                .SetData("/status", JsonValue.Create("On its way"))
                .SetData("/eta", JsonValue.Create("12 minutes"))),
            () => A2UIEmitter.Delete(SurfaceId),
        ]);

        var inner = new ScriptedAgent(_ => script.Dequeue()(), "Done.", "order-agent");
        await using var host = await A2AHost.StartAsync(inner.WithA2UI());

        // Everything the renderer has been told, in order, is replayed into one view.
        var renderer = new A2UISurfaceSet();

        await ApplyTurn(host, renderer);
        Assert.True(renderer.TryGet(SurfaceId, out var afterCreate));
        Assert.Equal("Placed", (string?)afterCreate.GetData("/status"));
        Assert.NotNull(afterCreate.Root);

        await ApplyTurn(host, renderer);
        Assert.True(renderer.TryGet(SurfaceId, out var afterFirstUpdate));
        Assert.Equal("Preparing", (string?)afterFirstUpdate.GetData("/status"));

        await ApplyTurn(host, renderer);
        Assert.True(renderer.TryGet(SurfaceId, out var afterSecondUpdate));
        Assert.Equal("On its way", (string?)afterSecondUpdate.GetData("/status"));
        Assert.Equal("12 minutes", (string?)afterSecondUpdate.GetData("/eta"));

        // The components were never resent; only the data changed.
        Assert.Equal(afterCreate.Components.Count, afterSecondUpdate.Components.Count);

        await ApplyTurn(host, renderer);
        Assert.Empty(renderer.Surfaces);
    }

    [Fact]
    public async Task AnUpdateCanReplaceAComponentByItsId()
    {
        var script = new Queue<Action>(
        [
            () => A2UIEmitter.Emit(BuildOrder()),
            () =>
            {
                var update = A2UISurfaceUpdate.For(SurfaceId, Basic);
                update.Components.Text("Delivered").Variant(TextVariant.H3).WithId("status_line");
                A2UIEmitter.Emit(update);
            },
        ]);

        var inner = new ScriptedAgent(_ => script.Dequeue()(), "Done.", "order-agent");
        await using var host = await A2AHost.StartAsync(inner.WithA2UI());

        var renderer = new A2UISurfaceSet();
        await ApplyTurn(host, renderer);
        await ApplyTurn(host, renderer);

        renderer.TryGet(SurfaceId, out var surface);

        // Replaced in place: the id is the same component, now saying something else.
        Assert.Equal("Delivered", (string?)surface.Components["status_line"].Properties["text"]);
        Assert.Equal("h3", (string?)surface.Components["status_line"].Properties["variant"]);
    }

    [Fact]
    public async Task SurfaceIdsDoNotCollideAcrossALongConversation()
    {
        var inner = new ScriptedAgent(
            _ => A2UIEmitter.Emit(BuildOrder(A2UISurfaceId.New("order"))),
            "Done.",
            "order-agent");

        await using var host = await A2AHost.StartAsync(inner.WithA2UI());
        var seen = new List<string>();

        for (var turn = 0; turn < 30; turn++)
        {
            var response = await host.Client.SendMessageAsync(Request());
            foreach (var part in response.Message!.Parts.Where(A2UIParts.IsA2UI))
            {
                A2UIParts.TryRead(part, out var messages);
                seen.AddRange(messages.OfType<CreateSurfaceMessage>().Select(m => m.SurfaceId));
            }
        }

        Assert.Equal(30, seen.Count);
        Assert.Equal(seen.Count, seen.Distinct(StringComparer.Ordinal).Count());
    }

    private static async Task ApplyTurn(A2AHost host, A2UISurfaceSet renderer)
    {
        var response = await host.Client.SendMessageAsync(Request());

        foreach (var part in response.Message!.Parts.Where(A2UIParts.IsA2UI))
        {
            Assert.True(A2UIParts.TryRead(part, out var messages));
            renderer.Apply(messages);
        }
    }

    private static A2UISurface BuildOrder(string surfaceId = SurfaceId)
    {
        var s = A2UISurface.Create(surfaceId, Basic);
        var ui = s.Components;

        return s
            .Root(ui.Card(ui.Column(
                ui.Text("Your order").Variant(TextVariant.H3),
                ui.Text(Bind.Path("/status")).WithId("status_line"),
                ui.Text(Bind.Path("/eta")))))
            .WithData(data =>
            {
                data["status"] = "Placed";
                data["eta"] = "unknown";
            })
            .Build();
    }

    private static SendMessageRequest Request() => new()
    {
        Message = new Message
        {
            Role = Role.User,
            MessageId = Guid.NewGuid().ToString("N"),
            ContextId = "c1",
            Parts = [Part.FromText("and now?")],
        },
    };
}
