namespace Strathweb.A2UI.Blazor;

/// <summary>
/// A text glyph for each icon name the Basic Catalog defines, so icons render without an icon font.
/// Hosts that ship one can style <c>.a2ui-icon[data-icon]</c> instead.
/// </summary>
public static class A2UIIconGlyphs
{
    private static readonly Dictionary<string, string> Glyphs = new(StringComparer.Ordinal)
    {
        ["accountCircle"] = "☺",
        ["add"] = "+",
        ["arrowBack"] = "←",
        ["arrowForward"] = "→",
        ["attachFile"] = "\U0001F4CE",
        ["calendarToday"] = "\U0001F4C5",
        ["call"] = "☎",
        ["camera"] = "\U0001F4F7",
        ["check"] = "✓",
        ["close"] = "✕",
        ["delete"] = "\U0001F5D1",
        ["download"] = "⬇",
        ["edit"] = "✎",
        ["error"] = "⚠",
        ["event"] = "\U0001F4C6",
        ["fastForward"] = "⏩",
        ["favorite"] = "♥",
        ["favoriteOff"] = "♡",
        ["folder"] = "\U0001F4C1",
        ["help"] = "?",
        ["home"] = "⌂",
        ["info"] = "ℹ",
        ["locationOn"] = "\U0001F4CD",
        ["lock"] = "\U0001F512",
        ["lockOpen"] = "\U0001F513",
        ["mail"] = "✉",
        ["menu"] = "☰",
        ["moreHoriz"] = "⋯",
        ["moreVert"] = "⋮",
        ["notifications"] = "\U0001F514",
        ["notificationsOff"] = "\U0001F515",
        ["pause"] = "⏸",
        ["payment"] = "\U0001F4B3",
        ["person"] = "\U0001F464",
        ["phone"] = "\U0001F4F1",
        ["photo"] = "\U0001F5BC",
        ["play"] = "▶",
        ["print"] = "\U0001F5A8",
        ["refresh"] = "↻",
        ["rewind"] = "⏪",
        ["search"] = "\U0001F50D",
        ["send"] = "➤",
        ["settings"] = "⚙",
        ["share"] = "↗",
        ["shoppingCart"] = "\U0001F6D2",
        ["skipNext"] = "⏭",
        ["skipPrevious"] = "⏮",
        ["star"] = "★",
        ["starHalf"] = "⯨",
        ["starOff"] = "☆",
        ["stop"] = "■",
        ["upload"] = "⬆",
        ["visibility"] = "\U0001F441",
        ["visibilityOff"] = "⦸",
        ["volumeDown"] = "\U0001F509",
        ["volumeMute"] = "\U0001F507",
        ["volumeOff"] = "\U0001F508",
        ["volumeUp"] = "\U0001F50A",
        ["warning"] = "⚠",
    };

    /// <summary>The glyph for an icon name.</summary>
    /// <param name="name">A name from the catalog's icon set.</param>
    /// <returns>The glyph, or the name itself when it is not a known icon.</returns>
    public static string Get(string? name) =>
        name is not null && Glyphs.TryGetValue(name, out var glyph) ? glyph : name ?? string.Empty;
}
