using System.Text.Json.Nodes;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Surfaces;

namespace Strathweb.A2UI.Rendering.UnitTests;

public class RenderSurfaceTests
{
    private static readonly A2UICatalog Basic = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

    /// <summary>A menu with a templated list, a bound comment field with a check, and two actions.</summary>
    private static A2UIRenderSurface Menu()
    {
        var s = A2UISurface.Create("menu", Basic);
        var ui = s.Components;

        var row = ui.Row(
                ui.Text(Bind.Path("name")).WithId("row_name"),
                ui.Button("Add").WithId("row_add").OnClick(Act.Event("add", ("id", Bind.Path("id")), ("note", Bind.Path("/comment")))))
            .WithId("row");

        var surface = s
            .Root(ui.Column(
                ui.Text("Menu").Variant(TextVariant.H2).WithId("title"),
                ui.List().ChildrenFrom(row, "/drinks").WithId("drinks"),
                ui.TextField("Comment").WithId("comment").Value(Bind.Path("/comment"))
                    .Checks(Check.Required(Bind.Path("/comment"), "Say something.")),
                ui.Button("Docs").WithId("docs").OnClick(Act.OpenUrl("https://example.com/menu")),
                ui.Button("Local").WithId("local").OnClick(Act.OpenUrl("file:///etc/passwd"))))
            .WithData(data =>
            {
                data["drinks"] = new JsonArray(
                    new JsonObject { ["id"] = "flat_white", ["name"] = "Flat white" },
                    new JsonObject { ["id"] = "cortado", ["name"] = "Cortado" });
                data["comment"] = string.Empty;
            })
            .Build();

        var render = new A2UIRenderSurface(surface.SurfaceId, surface.CatalogId);
        render.Apply(surface.Messages);
        return render;
    }

    [Fact]
    public void Apply_BuildsTheComponentsAndData()
    {
        var surface = Menu();

        Assert.NotNull(surface.Root);
        Assert.Equal("Column", surface.Root!.Component);
        Assert.Equal("Menu", surface.ResolveString(surface.GetComponent("title")!, "text", A2UIDataScope.Root));
        Assert.Equal(2, surface.GetData("/drinks")!.AsArray().Count);
    }

    [Fact]
    public void Children_AFixedList_KeepsTheIdsAndTheScope()
    {
        var surface = Menu();

        var children = surface.Children(surface.Root!, "children", A2UIDataScope.Root);

        Assert.Equal(["title", "drinks", "comment", "docs", "local"], children.Select(c => c.ComponentId));
        Assert.All(children, c => Assert.Same(A2UIDataScope.Root, c.Scope));
    }

    [Fact]
    public void Children_ATemplate_RepeatsOncePerItemWithItsOwnScope()
    {
        var surface = Menu();

        var rows = surface.Children(surface.GetComponent("drinks")!, "children", A2UIDataScope.Root);

        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.Equal("row", r.ComponentId));
        Assert.Equal(["/drinks/0", "/drinks/1"], rows.Select(r => r.Key));
        Assert.Equal("Cortado", surface.ResolveString(surface.GetComponent("row_name")!, "text", rows[1].Scope));
    }

    [Fact]
    public void Children_ATemplateOverMissingData_IsEmpty()
    {
        var surface = Menu();
        surface.Apply(UpdateDataModelMessage.Remove("menu", "/drinks"));

        Assert.Empty(surface.Children(surface.GetComponent("drinks")!, "children", A2UIDataScope.Root));
    }

    [Fact]
    public void TrySetValue_WritesThroughTheBinding()
    {
        var surface = Menu();
        var changed = 0;
        surface.Changed += (_, _) => changed++;

        var written = surface.TrySetValue(surface.GetComponent("comment")!, "value", JsonValue.Create("extra hot"), A2UIDataScope.Root);

        Assert.True(written);
        Assert.Equal("extra hot", (string?)surface.GetData("/comment"));
        Assert.Equal(1, changed);
    }

    [Fact]
    public void TrySetValue_ALiteralProperty_CannotBeWritten()
    {
        var surface = Menu();

        Assert.False(surface.TrySetValue(surface.GetComponent("title")!, "text", JsonValue.Create("x"), A2UIDataScope.Root));
    }

    [Fact]
    public void Check_FailsUntilTheValueIsPresent()
    {
        var surface = Menu();
        var field = surface.GetComponent("comment")!;

        Assert.Equal(["Say something."], surface.Check(field, A2UIDataScope.Root));

        surface.TrySetValue(field, "value", JsonValue.Create("ok"), A2UIDataScope.Root);

        Assert.Empty(surface.Check(field, A2UIDataScope.Root));
    }

    [Fact]
    public void CheckAll_ReportsFailuresByComponent()
    {
        var surface = Menu();

        var failures = surface.CheckAll();

        Assert.Equal(["comment"], failures.Keys);
    }

    [Fact]
    public void CreateAction_ResolvesTheContextInTheItemScope()
    {
        var surface = Menu();
        surface.TrySetValue(surface.GetComponent("comment")!, "value", JsonValue.Create("no sugar"), A2UIDataScope.Root);
        var rows = surface.Children(surface.GetComponent("drinks")!, "children", A2UIDataScope.Root);

        var action = surface.CreateAction(surface.GetComponent("row_add")!, rows[1].Scope);

        Assert.NotNull(action);
        Assert.Equal("add", action!.Name);
        Assert.Equal("menu", action.SurfaceId);
        Assert.Equal("row_add", action.SourceComponentId);
        Assert.Equal("cortado", (string?)action.Context["id"]);
        Assert.Equal("no sugar", (string?)action.Context["note"]);
    }

    [Fact]
    public void CreateAction_OpenUrl_RaisesTheEventAndSendsNothing()
    {
        var surface = Menu();
        Uri? opened = null;
        surface.OpenUrlRequested += (_, e) => opened = e.Url;

        var action = surface.CreateAction(surface.GetComponent("docs")!, A2UIDataScope.Root);

        Assert.Null(action);
        Assert.Equal(new Uri("https://example.com/menu"), opened);
    }

    [Fact]
    public void CreateAction_OpenUrl_RefusesNonWebSchemes()
    {
        var surface = Menu();
        var raised = false;
        surface.OpenUrlRequested += (_, _) => raised = true;

        surface.CreateAction(surface.GetComponent("local")!, A2UIDataScope.Root);

        Assert.False(raised);
    }

    [Fact]
    public void CreateAction_AComponentWithoutAnAction_IsNull()
    {
        var surface = Menu();

        Assert.Null(surface.CreateAction(surface.GetComponent("title")!, A2UIDataScope.Root));
    }

    [Fact]
    public void ResolveStrings_AChoiceValue_IsAList()
    {
        var surface = Menu();
        surface.Apply(UpdateDataModelMessage.Set("menu", "/picked", new JsonArray("a", "b")));
        var component = new Components.A2UIComponent("picker", "ChoicePicker")
            .Set("value", new JsonObject { ["path"] = "/picked" });

        Assert.Equal(["a", "b"], surface.ResolveStrings(component, "value", A2UIDataScope.Root));
    }

    [Fact]
    public void ChildId_ReadsASingleChildReference()
    {
        var surface = Menu();
        var button = surface.GetComponent("docs")!;

        Assert.NotNull(A2UIRenderSurface.ChildId(button, "child"));
        Assert.Null(A2UIRenderSurface.ChildId(button, "children"));
    }
}
