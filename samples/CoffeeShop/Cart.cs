using System.Globalization;
using System.Text.Json.Nodes;

namespace CoffeeShop;

/// <summary>
/// The basket, read out of the surface's data model and written back to it.
/// </summary>
internal sealed class Cart
{
    private readonly List<Drink> items = [];

    internal IReadOnlyList<Drink> Items => items;

    internal decimal Total => items.Sum(d => d.Price);

    internal string Summary => items.Count switch
    {
        0 => "Nothing ordered",
        1 => items[0].Name,
        _ => $"{items[0].Name} and {items.Count - 1} more",
    };

    /// <summary>Reads the basket back out of the data model the renderer reported.</summary>
    internal static Cart From(JsonNode? dataModel)
    {
        var cart = new Cart();

        foreach (var item in dataModel?["cart"]?.AsArray() ?? [])
        {
            if (Menu.Find((string?)item?["id"]) is { } drink)
            {
                cart.items.Add(drink);
            }
        }

        return cart;
    }

    internal void Add(Drink drink) => items.Add(drink);

    internal void RemoveAt(int index)
    {
        if (index >= 0 && index < items.Count)
        {
            items.RemoveAt(index);
        }
    }

    /// <summary>The cart as the surface's data model renders it.</summary>
    internal JsonArray ToJson()
    {
        var array = new JsonArray();
        for (var i = 0; i < items.Count; i++)
        {
            array.Add((JsonNode)new JsonObject
            {
                ["id"] = items[i].Id,
                ["name"] = items[i].Name,
                ["price"] = Menu.Money(items[i].Price),

                // The index travels with the row so Remove knows which one it is.
                ["index"] = i.ToString(CultureInfo.InvariantCulture),
            });
        }

        return array;
    }

    internal string Heading => items.Count switch
    {
        0 => "Basket is empty",
        1 => "Basket: 1 item",
        _ => $"Basket: {items.Count} items",
    };

    internal string TotalText => $"Total {Menu.Money(Total)}";
}
