using System.Text.Json.Nodes;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Surfaces;

namespace Strathweb.A2UI.Rendering.UnitTests;

public class RendererSessionTests
{
    private static readonly A2UICatalog Basic = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

    private static A2UISurface Card(string id, bool sendDataModel = false)
    {
        var s = A2UISurface.Create(id, Basic);
        var ui = s.Components;
        var builder = s.Root(ui.Card(ui.Text(Bind.Path("/status")))).WithData(d => d["status"] = "Placed");
        if (sendDataModel)
        {
            builder.SendDataModel();
        }

        return builder.Build();
    }

    [Fact]
    public void Apply_CreatesUpdatesAndDeletesSurfaces()
    {
        var session = new A2UIRendererSession();
        var created = new List<string>();
        var deleted = new List<string>();
        session.SurfaceCreated += (_, s) => created.Add(s.SurfaceId);
        session.SurfaceDeleted += (_, s) => deleted.Add(s.SurfaceId);

        session.Apply(Card("order").Messages);
        session.Apply(UpdateDataModelMessage.Set("order", "/status", JsonValue.Create("Ready")));

        Assert.True(session.TryGet("order", out var surface));
        Assert.Equal("Ready", (string?)surface.GetData("/status"));
        Assert.Equal(["order"], created);

        session.Apply(new DeleteSurfaceMessage("order"));

        Assert.Empty(session.Surfaces);
        Assert.Equal(["order"], deleted);
        Assert.True(surface.IsDeleted);
    }

    [Fact]
    public void Apply_AMessageForAnUnknownSurface_IsCountedNotThrown()
    {
        var session = new A2UIRendererSession();

        var ignored = session.Apply([UpdateDataModelMessage.Set("ghost", "/x", JsonValue.Create(1)), new DeleteSurfaceMessage("ghost")]);

        Assert.Equal(2, ignored);
    }

    [Fact]
    public void Apply_ReusingAnId_ReplacesTheSurface()
    {
        var session = new A2UIRendererSession();
        session.Apply(Card("order").Messages);
        session.TryGet("order", out var first);

        session.Apply(Card("order").Messages);

        Assert.True(session.TryGet("order", out var second));
        Assert.NotSame(first, second);
        Assert.Single(session.Surfaces);
    }

    [Fact]
    public void DataModels_OnlyIncludeSurfacesThatAskedForIt()
    {
        var session = new A2UIRendererSession();
        session.Apply(Card("quiet").Messages);
        session.Apply(Card("chatty", sendDataModel: true).Messages);

        var models = session.DataModels();

        Assert.Equal(["chatty"], models.Keys);
        Assert.Equal("Placed", (string?)models["chatty"]!["status"]);
    }

    [Fact]
    public void SurfaceChanged_FiresForAgentAndLocalChanges()
    {
        var session = new A2UIRendererSession();
        var changes = 0;
        session.SurfaceChanged += (_, _) => changes++;
        session.Apply(Card("order").Messages);
        session.TryGet("order", out var surface);

        var before = changes;
        session.Apply(UpdateDataModelMessage.Set("order", "/status", JsonValue.Create("Ready")));
        var text = surface.GetComponent("text_1")!;
        surface.TrySetValue(text, "text", JsonValue.Create("Done"), A2UIDataScope.Root);

        Assert.Equal(before + 2, changes);
    }

    [Fact]
    public void SupportedCatalogIds_DefaultToTheBasicCatalog()
    {
        Assert.Equal([Basic.CatalogId], new A2UIRendererSession().SupportedCatalogIds);
    }

    [Fact]
    public void Clear_RemovesEverything()
    {
        var session = new A2UIRendererSession();
        session.Apply(Card("a").Messages);
        session.Apply(Card("b").Messages);

        session.Clear();

        Assert.Empty(session.Surfaces);
    }

    [Fact]
    public void DataScope_ResolvesRelativeAndAbsolutePaths()
    {
        var item = A2UIDataScope.ForItem("/drinks/1");

        Assert.Equal("/drinks/1/name", item.Absolute("name"));
        Assert.Equal("/comment", item.Absolute("/comment"));
        Assert.Equal("/drinks/1", item.Absolute(string.Empty));
        Assert.Equal("/name", A2UIDataScope.Root.Absolute("name"));
        Assert.False(A2UIDataScope.Root.IsItem);
    }
}
