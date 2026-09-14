using System.Globalization;
using System.Text.Json.Nodes;
using Strathweb.A2UI;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Surfaces;

namespace GenerativeDashboard;

/// <summary>
/// The dashboard as the scripted model writes it. With a real model this file is only the prompt
/// text; the JSON comes from the model. The scripted stand-in builds the same JSON so the page runs
/// without a key, and can hand back a broken version to show the repair round.
/// </summary>
internal static class Dashboard
{
    internal const string SurfaceId = "sales_dashboard";

    internal const string Role =
        "You are a sales analyst for a small coffee roaster. You answer questions about sales with a " +
        "dashboard drawn as an A2UI surface, and a single sentence of commentary.";

    internal const string UiDescription =
        "A dashboard is a Card holding a Column: a heading, one line of caption, a Row of two or three " +
        "headline metrics (each a Column with a caption Text and an h3 Text), a Divider, and a List of " +
        "region rows built from a template over a data model list, each row a Row with the region " +
        "name, its share as a caption, and its revenue. End with a Row of Buttons whose actions are " +
        "events named refresh and explain. Put all numbers in the data model and bind to them.";

    private static readonly A2UICatalog Catalog = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

    private static readonly (string Region, double Share)[] Regions =
    [
        ("North", 0.34), ("West", 0.27), ("South", 0.22), ("East", 0.17),
    ];

    /// <summary>The dashboard as a response the model could have written: prose plus an A2UI block.</summary>
    internal static string Response(string prose) => prose + "\n" + Block(A2UIJson.ToJsonArray(Surface().Messages));

    /// <summary>The same dashboard with two mistakes a model makes: an invented property and a component the catalog lacks.</summary>
    internal static string BrokenResponse(string prose)
    {
        var messages = A2UIJson.ToJsonArray(Surface().Messages);
        var components = messages[1]!["updateComponents"]!["components"]!.AsArray();

        components[1]!["colour"] = "#ff0000";
        components[^1]!["component"] = "ButtonRow";

        return prose + "\n" + Block(messages);
    }

    /// <summary>An update to the live dashboard with new numbers, as an A2UI block.</summary>
    internal static string RefreshResponse(string prose) =>
        prose + "\n" + Block(A2UIJson.ToJsonArray([UpdateDataModelMessage.Replace(SurfaceId, Data(Random.Shared))]));

    private static string Block(JsonArray messages) => "<a2ui-json>\n" + messages.ToJsonString() + "\n</a2ui-json>";

    private static A2UISurface Surface()
    {
        var s = A2UISurface.Create(SurfaceId, Catalog);
        var ui = s.Components;

        var row = ui.Row(
                ui.Text(Bind.Path("name")).Weight(1),
                ui.Text(Bind.Path("share")).Variant(TextVariant.Caption),
                ui.Text(Bind.Path("revenue")).Variant(TextVariant.H5))
            .Align(LayoutAlign.Center)
            .WithId("region_row");

        return s
            .Root(ui.Card(ui.Column(
                ui.Text("Q3 sales by region").Variant(TextVariant.H2),
                ui.Text(Bind.Path("/caption")).Variant(TextVariant.Caption),
                ui.Row(
                    Metric(ui, "Revenue", "/metrics/revenue"),
                    Metric(ui, "Orders", "/metrics/orders"),
                    Metric(ui, "Avg. basket", "/metrics/basket")),
                ui.Divider(),
                ui.List().ChildrenFrom(row, "/regions"),
                ui.Row(
                    ui.Button("Refresh numbers").OnClick(Act.Event("refresh")),
                    ui.Button("Explain the drop in the East").Variant(ButtonVariant.Borderless).OnClick(Act.Event("explain")))
                    .Justify(LayoutJustify.End))))
            .WithData(Data(new Random(3)))
            .Build();
    }

    private static ColumnBuilder Metric(BasicComponents ui, string label, string path) =>
        ui.Column(
                ui.Text(label).Variant(TextVariant.Caption),
                ui.Text(Bind.Path(path)).Variant(TextVariant.H3))
            .Weight(1);

    private static JsonObject Data(Random random)
    {
        var revenue = 180_000 + random.Next(0, 40_000);
        var orders = 6_000 + random.Next(0, 1_500);

        var regions = new JsonArray();
        foreach (var (region, share) in Regions)
        {
            var jitter = 1 + (random.NextDouble() - 0.5) * 0.1;
            regions.Add((JsonNode)new JsonObject
            {
                ["name"] = region,
                ["share"] = (share * jitter).ToString("P0", CultureInfo.InvariantCulture),
                ["revenue"] = Money(revenue * share * jitter),
            });
        }

        return new JsonObject
        {
            ["caption"] = $"Generated {DateTimeOffset.Now:HH:mm:ss}. Numbers live in the data model; the components never change.",
            ["metrics"] = new JsonObject
            {
                ["revenue"] = Money(revenue),
                ["orders"] = orders.ToString("N0", CultureInfo.InvariantCulture),
                ["basket"] = Money((double)revenue / orders),
            },
            ["regions"] = regions,
        };
    }

    private static string Money(double amount) => "€" + amount.ToString("N0", CultureInfo.InvariantCulture);
}
