using System.Text.Json.Nodes;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Rendering;
using Strathweb.A2UI.Surfaces;

namespace Strathweb.A2UI.Blazor.UnitTests;

public class SurfaceViewTests
{
    private static readonly A2UICatalog Basic = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

    [Fact]
    public async Task Render_TextVariants_BecomeHeadingsAndParagraphs()
    {
        var s = A2UISurface.Create("s", Basic);
        var ui = s.Components;
        var surface = s.Root(ui.Column(
            ui.Text("Title").Variant(TextVariant.H2),
            ui.Text("Body **bold** and `code`"),
            ui.Text("small").Variant(TextVariant.Caption))).Build();

        var html = await SurfaceRendering.RenderAsync(surface);

        Assert.Contains("<h2 class=\"a2ui-text a2ui-text-h2\"", html, StringComparison.Ordinal);
        Assert.Contains("<p class=\"a2ui-text a2ui-text-body\"", html, StringComparison.Ordinal);
        Assert.Contains("<strong>bold</strong>", html, StringComparison.Ordinal);
        Assert.Contains("<code>code</code>", html, StringComparison.Ordinal);
        Assert.Contains("a2ui-text-caption", html, StringComparison.Ordinal);
        Assert.Contains("data-surface-id=\"s\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_AgentText_IsHtmlEncoded()
    {
        var s = A2UISurface.Create("s", Basic);
        var surface = s.Root(s.Components.Text("<script>alert(1)</script>")).Build();

        var html = await SurfaceRendering.RenderAsync(surface);

        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_ATemplate_RepeatsTheRowPerItemWithBoundText()
    {
        var s = A2UISurface.Create("menu", Basic);
        var ui = s.Components;
        var row = ui.Row(ui.Text(Bind.Path("name")), ui.Text(Bind.Path("price")).Variant(TextVariant.Caption)).WithId("row");
        var surface = s
            .Root(ui.List().ChildrenFrom(row, "/drinks"))
            .WithData(d => d["drinks"] = new JsonArray(
                new JsonObject { ["name"] = "Flat white", ["price"] = "3.40" },
                new JsonObject { ["name"] = "Cortado", ["price"] = "3.10" }))
            .Build();

        var html = await SurfaceRendering.RenderAsync(surface);

        Assert.Contains("Flat white", html, StringComparison.Ordinal);
        Assert.Contains("Cortado", html, StringComparison.Ordinal);
        Assert.Equal(2, Count(html, "data-component-id=\"row\""));
    }

    [Fact]
    public async Task Render_Inputs_ShowTheDataModelValues()
    {
        var s = A2UISurface.Create("form", Basic);
        var ui = s.Components;
        var surface = s
            .Root(ui.Column(
                ui.TextField("Name").Value(Bind.Path("/name")),
                ui.CheckBox("Newsletter", Bind.Path("/newsletter")),
                ui.Slider(Bind.Path("/guests"), 10).Label("Guests"),
                ui.DateTimeInput(Bind.Path("/when")).EnableDate().Label("When"),
                ui.ChoicePicker().Label("Size").DisplayStyle(ChoiceDisplayStyle.Chips)
                    .Options(("Small", "s"), ("Large", "l")).Value(Bind.Path("/size"))))
            .WithData(d =>
            {
                d["name"] = "Ada";
                d["newsletter"] = true;
                d["guests"] = 4;
                d["when"] = "2026-09-20";
                d["size"] = new JsonArray("l");
            })
            .Build();

        var html = await SurfaceRendering.RenderAsync(surface);

        Assert.Contains("value=\"Ada\"", html, StringComparison.Ordinal);
        Assert.Contains("type=\"checkbox\" checked", html, StringComparison.Ordinal);
        var range = html.Substring(html.IndexOf("type=\"range\"", StringComparison.Ordinal));
        Assert.Contains("max=\"10\"", range, StringComparison.Ordinal);
        Assert.Contains("value=\"4\"", range, StringComparison.Ordinal);
        Assert.Contains("type=\"date\" value=\"2026-09-20\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"a2ui-chip a2ui-chip-selected\" aria-pressed=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("<label class=\"a2ui-label\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_ButtonsAndCards_CarryTheirVariants()
    {
        var s = A2UISurface.Create("s", Basic);
        var ui = s.Components;
        var surface = s.Root(ui.Card(ui.Row(
            ui.Button("Go").Primary().OnClick(Act.Event("go")),
            ui.Button("Skip").Variant(ButtonVariant.Borderless).OnClick(Act.Event("skip")).Weight(2),
            ui.Icon(A2UIIcons.Check),
            ui.Divider()))).Build();

        var html = await SurfaceRendering.RenderAsync(surface);

        Assert.Contains("class=\"a2ui-button a2ui-button-primary\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"a2ui-button a2ui-button-borderless\"", html, StringComparison.Ordinal);
        Assert.Contains("flex-grow:2", html, StringComparison.Ordinal);
        Assert.Contains("class=\"a2ui-card\"", html, StringComparison.Ordinal);
        Assert.Contains("data-icon=\"check\"", html, StringComparison.Ordinal);
        Assert.Contains("a2ui-divider-horizontal", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_Tabs_ShowTheFirstPanel()
    {
        var s = A2UISurface.Create("s", Basic);
        var ui = s.Components;
        var surface = s.Root(ui.Tabs()
            .Tab("Details", ui.Text("The details"))
            .Tab("Reviews", ui.Text("The reviews"))).Build();

        var html = await SurfaceRendering.RenderAsync(surface);

        Assert.Contains("role=\"tab\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-selected=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("The details", html, StringComparison.Ordinal);
        Assert.DoesNotContain("The reviews", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_AModal_ShowsOnlyItsTriggerUntilOpened()
    {
        var s = A2UISurface.Create("s", Basic);
        var ui = s.Components;
        var surface = s.Root(ui.Modal(ui.Button("Open").OnClick(Act.Event("noop")), ui.Text("Inside the dialog"))).Build();

        var html = await SurfaceRendering.RenderAsync(surface);

        Assert.Contains("a2ui-modal-trigger", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Inside the dialog", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_ASurfaceWithoutARoot_ShowsThePendingText()
    {
        var surface = new A2UIRenderSurface("empty");

        var html = await SurfaceRendering.RenderAsync(surface);

        Assert.Contains("a2ui-pending", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_AnUnknownComponent_IsNamedNotHidden()
    {
        var surface = new A2UIRenderSurface("s");
        surface.Apply(new Messages.UpdateComponentsMessage("s", [new Components.A2UIComponent("root", "Gauge")]));

        var html = await SurfaceRendering.RenderAsync(surface);

        Assert.Contains("Unsupported component: Gauge", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_EscapesAndFormats()
    {
        Assert.Equal("a <em>b</em> <strong>c</strong><br />d &amp; e", A2UIMarkdown.ToHtml("a *b* **c**\nd & e").Value);
        Assert.Equal(string.Empty, A2UIMarkdown.ToHtml(null).Value);
        Assert.Equal("2 * 3 * 4", A2UIMarkdown.ToHtml("2 * 3 * 4").Value);
    }

    [Fact]
    public void IconGlyphs_FallBackToTheName()
    {
        Assert.Equal("★", A2UIIconGlyphs.Get("star"));
        Assert.Equal("mystery", A2UIIconGlyphs.Get("mystery"));
    }

    private static int Count(string text, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
