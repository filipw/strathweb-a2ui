using System.Globalization;
using System.Text.Json.Nodes;

namespace CoffeeShop;

/// <summary>What the shop sells.</summary>
internal static class Menu
{
    internal static IReadOnlyList<Drink> Drinks { get; } =
    [
        new("flat_white", "Flat white", 3.40m, "☕"),
        new("cortado", "Cortado", 3.10m, "\U0001F95B"),
        new("cold_brew", "Cold brew", 3.80m, "\U0001F9CA"),
        new("matcha", "Matcha latte", 4.20m, "\U0001F375"),
    ];

    internal static Drink? Find(string? id) =>
        Drinks.FirstOrDefault(d => string.Equals(d.Id, id, StringComparison.Ordinal));

    internal static JsonArray AsJson()
    {
        var array = new JsonArray();
        foreach (var drink in Drinks)
        {
            array.Add((JsonNode)drink.ToJson());
        }

        return array;
    }

    internal static string Money(decimal amount) =>
        amount.ToString("0.00", CultureInfo.InvariantCulture);
}

/// <summary>One item on the menu.</summary>
/// <param name="Id">Stable identifier sent back in actions.</param>
/// <param name="Name">Display name.</param>
/// <param name="Price">Price in euros.</param>
/// <param name="Art">An emoji standing in for a product photo.</param>
internal sealed record Drink(string Id, string Name, decimal Price, string Art)
{
    internal JsonObject ToJson() => new()
    {
        ["id"] = Id,
        ["name"] = Name,
        ["price"] = Menu.Money(Price),
        ["art"] = Art,
    };
}
