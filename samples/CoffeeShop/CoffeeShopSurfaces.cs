using System.Text.Json.Nodes;
using Strathweb.A2UI;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Surfaces;

namespace CoffeeShop;

/// <summary>The surfaces this shop puts on screen.</summary>
internal static class CoffeeShopSurfaces
{
    internal const string MenuSurfaceId = "coffee_menu";
    internal const string OrderSurfaceId = "coffee_order";

    internal static A2UICatalog Catalog { get; } = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

    /// <summary>
    /// The menu, with the cart rendered from the same data model the renderer sends back.
    /// </summary>
    internal static A2UISurface Menu()
    {
        var s = A2UISurface.Create(MenuSurfaceId, Catalog);
        var ui = s.Components;

        // One row per drink, repeated over /drinks. Paths inside a template are relative to the item.
        var drinkRow = ui.Row(
                ui.Text(Bind.Path("art")).Variant(TextVariant.H2),
                ui.Column(
                        ui.Text(Bind.Path("name")).Variant(TextVariant.H4),
                        ui.Text(Bind.Path("price")).Variant(TextVariant.Caption))
                    .Weight(1),
                ui.Button("Add")
                    .OnClick(Act.Event("add_drink", ("id", Bind.Path("id")))))
            .Align(LayoutAlign.Center)
            .WithId("drink_row");

        var cartRow = ui.Row(
                ui.Text(Bind.Path("name")).Weight(1),
                ui.Text(Bind.Path("price")).Variant(TextVariant.Caption),
                ui.Button("Remove")
                    .Variant(ButtonVariant.Borderless)
                    .OnClick(Act.Event("remove_drink", ("index", Bind.Path("index")))))
            .Align(LayoutAlign.Center)
            .WithId("cart_row");

        return s
            .Root(ui.Card(ui.Column(
                ui.Text("Strathweb Coffee").Variant(TextVariant.H2),
                ui.Text("Pick something and it lands in the basket.").Variant(TextVariant.Caption),
                ui.Divider(),
                ui.List().ChildrenFrom(drinkRow, "/drinks"),
                ui.Divider(),
                ui.Text(Bind.Path("/cartHeading")).Variant(TextVariant.H4),
                ui.List().ChildrenFrom(cartRow, "/cart"),
                ui.Text(Bind.Path("/total")).Variant(TextVariant.H4),
                ui.Button("Place order")
                    .Primary()
                    .WithId("place_order")
                    .OnClick(Act.Event("place_order")))))
            .WithData(EmptyCart())

            // The renderer returns this surface's data model with every message, which is how the
            // agent knows what is in the basket without keeping its own copy.
            .SendDataModel()
            .Build();
    }

    /// <summary>The order confirmation, updated in place as the order progresses.</summary>
    internal static A2UISurface Order(string summary, string total)
    {
        var s = A2UISurface.Create(OrderSurfaceId, Catalog);
        var ui = s.Components;

        return s
            .Root(ui.Card(ui.Column(
                ui.Text("Order placed").Variant(TextVariant.H2),
                ui.Text(summary),
                ui.Text($"Total {total}").Variant(TextVariant.Caption),
                ui.Divider(),
                ui.Text(Bind.Path("/status")).Variant(TextVariant.H4).WithId("status_line"),
                ui.Row(
                    ui.Button("Refresh").OnClick(Act.Event("refresh_status")).Weight(1),
                    ui.Button("Order again").Primary().OnClick(Act.Event("start_over")).Weight(1)))))
            .WithData(data => data["status"] = "Grinding beans")

            // Refresh advances the status from whatever it currently is, so the agent has to be able
            // to read it back.
            .SendDataModel()
            .Build();
    }

    internal static JsonObject EmptyCart() => new()
    {
        ["drinks"] = global::CoffeeShop.Menu.AsJson(),
        ["cart"] = new JsonArray(),
        ["cartHeading"] = "Basket is empty",
        ["total"] = "Total 0.00",
    };
}
