using System.Globalization;
using System.Text.Json.Nodes;
using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Rendering.UnitTests;

public class FunctionsTests
{
    private static readonly JsonObject Data = new()
    {
        ["name"] = "Ada",
        ["price"] = 1234.5,
        ["count"] = 3,
        ["email"] = "ada@example.com",
        ["when"] = "2026-01-16T14:30:00Z",
        ["flags"] = new JsonArray(true, false),
    };

    /// <summary>Resolves paths against <see cref="Data"/>, the way a surface would.</summary>
    private static JsonNode? Resolve(DynamicValue? value) => value?.Kind switch
    {
        null => null,
        DynamicValueKind.Literal => value.Literal,
        DynamicValueKind.Path => Data[value.Path!.TrimStart('/')]?.DeepClone(),
        DynamicValueKind.FunctionCall => A2UIFunctions.Invoke(value.Call!, Resolve),
        _ => null,
    };

    private static JsonNode? Call(string name, params (string Key, DynamicValue Value)[] args) =>
        A2UIFunctions.Invoke(new FunctionCall(name) { Args = args.ToDictionary(a => a.Key, a => a.Value, StringComparer.Ordinal) }, Resolve);

    private static string Text(JsonNode? node) => A2UIFunctions.Display(node);

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("x", true)]
    public void Required_TellsPresenceApart(string? value, bool expected)
    {
        Assert.Equal(expected, (bool)Call("required", ("value", value is null ? DynamicValue.FromLiteral(null) : value))!);
    }

    [Fact]
    public void Required_AnEmptyArrayIsAbsent_AFullOneIsPresent()
    {
        Assert.False((bool)Call("required", ("value", DynamicValue.FromLiteral(new JsonArray())))!);
        Assert.True((bool)Call("required", ("value", DynamicValue.FromLiteral(new JsonArray("a"))))!);
    }

    [Fact]
    public void Length_ChecksBounds()
    {
        Assert.True((bool)Call("length", ("value", "abc"), ("min", 2), ("max", 3))!);
        Assert.False((bool)Call("length", ("value", "abcd"), ("max", 3))!);
        Assert.False((bool)Call("length", ("value", "a"), ("min", 2))!);
    }

    [Fact]
    public void Numeric_ParsesStringsAndRejectsNonNumbers()
    {
        Assert.True((bool)Call("numeric", ("value", "42"), ("min", 1), ("max", 100))!);
        Assert.False((bool)Call("numeric", ("value", "many"), ("min", 1))!);
        Assert.False((bool)Call("numeric", ("value", 101), ("max", 100))!);
    }

    [Fact]
    public void Regex_And_Email_Match()
    {
        Assert.True((bool)Call("regex", ("value", "A-12"), ("pattern", "^[A-Z]-\\d+$"))!);
        Assert.True((bool)Call("email", ("value", Bind("/email")))!);
        Assert.False((bool)Call("email", ("value", "nope"))!);
    }

    [Fact]
    public void And_Or_Not_CombineBooleans()
    {
        Assert.False((bool)Call("and", ("values", DynamicValue.FromLiteral(new JsonArray(true, false))))!);
        Assert.True((bool)Call("or", ("values", DynamicValue.FromLiteral(new JsonArray(true, false))))!);
        Assert.True((bool)Call("or", ("values", Bind("/flags")))!);
        Assert.True((bool)Call("not", ("value", false))!);
    }

    [Fact]
    public void FormatString_InterpolatesPathsLiteralsAndCalls()
    {
        var text = Text(Call("formatString", ("value", "Hi ${/name}, ${'you'} owe ${formatCurrency(value: /price, currency: 'USD')} for ${/count} items. Escaped: \\${not}")));

        Assert.Equal("Hi Ada, you owe $1,234.50 for 3 items. Escaped: ${not}", text);
    }

    [Fact]
    public void FormatString_AnUnknownPath_IsEmpty()
    {
        Assert.Equal("[]", Text(Call("formatString", ("value", "[${/missing}]"))));
    }

    [Fact]
    public void FormatNumber_GroupsAndRounds()
    {
        Assert.Equal("1,234.57", Text(Call("formatNumber", ("value", 1234.567), ("decimals", 2))));
        Assert.Equal("1234.57", Text(Call("formatNumber", ("value", 1234.567), ("decimals", 2), ("grouping", false))));
        Assert.Equal("1,234.567", Text(Call("formatNumber", ("value", 1234.567))));
    }

    [Fact]
    public void FormatCurrency_UsesKnownSymbolsAndZeroDecimalCurrencies()
    {
        Assert.Equal("$1,234.50", Text(Call("formatCurrency", ("value", Bind("/price")), ("currency", "USD"))));
        Assert.Equal("€1,234.50", Text(Call("formatCurrency", ("value", 1234.5), ("currency", "EUR"))));
        Assert.Equal("¥1,235", Text(Call("formatCurrency", ("value", 1234.5), ("currency", "JPY"))));
        Assert.Equal("-$5.00", Text(Call("formatCurrency", ("value", -5), ("currency", "USD"))));
        Assert.Equal("XYZ 1.00", Text(Call("formatCurrency", ("value", 1), ("currency", "xyz"))));
    }

    [Theory]
    [InlineData("MMM dd, yyyy", "Jan 16, 2026")]
    [InlineData("HH:mm", "14:30")]
    [InlineData("h:mm a", "2:30 PM")]
    [InlineData("EEEE, d MMMM", "Friday, 16 January")]
    [InlineData("yyyy-MM-dd 'at' HH:mm", "2026-01-16 at 14:30")]
    public void FormatDate_MapsTheDocumentedTokens(string pattern, string expected)
    {
        Assert.Equal(expected, Text(Call("formatDate", ("value", Bind("/when")), ("format", pattern))));
    }

    [Fact]
    public void FormatDate_AnUnparseableValue_PassesThrough()
    {
        Assert.Equal("soon", Text(Call("formatDate", ("value", "soon"), ("format", "yyyy"))));
    }

    [Fact]
    public void Pluralize_PicksTheCategoryAndFallsBackToOther()
    {
        Assert.Equal("one item", Text(Call("pluralize", ("value", 1), ("one", "one item"), ("other", "many items"))));
        Assert.Equal("many items", Text(Call("pluralize", ("value", Bind("/count")), ("one", "one item"), ("other", "many items"))));
        Assert.Equal("none", Text(Call("pluralize", ("value", 0), ("zero", "none"), ("other", "some"))));
    }

    [Fact]
    public void Numbers_AreReadWhateverTypeBuiltTheNode()
    {
        // A data model built in code holds ints and decimals; one parsed from the wire holds elements.
        Assert.Equal("one item", Text(Call("pluralize", ("value", DynamicValue.FromLiteral(JsonValue.Create(1))), ("one", "one item"), ("other", "items"))));
        Assert.Equal("2.5", Text(Call("formatNumber", ("value", DynamicValue.FromLiteral(JsonValue.Create(2.5m))), ("decimals", 1))));
        Assert.Equal("7", A2UIFunctions.Display(JsonNode.Parse("7")));
        Assert.True(A2UIFunctions.TryGetNumber(JsonValue.Create(3L), out var l) && l == 3);
        Assert.False(A2UIFunctions.TryGetNumber(JsonValue.Create("x"), out _));
    }

    [Fact]
    public void OpenUrl_ReturnsTheUrlForTheHost()
    {
        Assert.Equal("https://example.com", Text(Call("openUrl", ("url", "https://example.com"))));
    }

    [Fact]
    public void Invoke_AnUnknownFunction_Throws()
    {
        Assert.Throws<A2UIRenderException>(() => Call("teleport", ("value", 1)));
    }

    [Fact]
    public void Invoke_AMissingRequiredArgument_Throws()
    {
        Assert.Throws<A2UIRenderException>(() => Call("regex", ("value", "x")));
    }

    [Fact]
    public void Display_WritesScalarsPlainly()
    {
        // The repository builds with invariant globalization, so cultures do not change digits here.
        Assert.Equal("1.5", A2UIFunctions.Display(JsonValue.Create(1.5), CultureInfo.InvariantCulture));
        Assert.Equal("true", A2UIFunctions.Display(JsonValue.Create(true)));
        Assert.Equal(string.Empty, A2UIFunctions.Display(null));
    }

    private static DynamicValue Bind(string path) => DynamicValue.FromPath(path);
}
