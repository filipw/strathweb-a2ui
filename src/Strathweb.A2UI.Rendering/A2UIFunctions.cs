using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Rendering;

/// <summary>
/// The Basic Catalog's functions, evaluated on the renderer side. Arguments arrive as
/// <see cref="DynamicValue"/>s; a resolver turns each into concrete JSON before the function runs.
/// </summary>
public static class A2UIFunctions
{
    /// <summary>The value <c>openUrl</c> returns: the URL to open, for the host to act on.</summary>
    public const string OpenUrlFunction = "openUrl";

    private static readonly Regex EmailPattern = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.CultureInvariant);

    private static readonly Dictionary<string, string> CurrencySymbols = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = "$",
        ["EUR"] = "€",
        ["GBP"] = "£",
        ["JPY"] = "¥",
        ["CNY"] = "¥",
        ["INR"] = "₹",
        ["KRW"] = "₩",
        ["PLN"] = "zł",
        ["CHF"] = "CHF ",
        ["SEK"] = "kr ",
        ["NOK"] = "kr ",
        ["DKK"] = "kr ",
        ["CAD"] = "CA$",
        ["AUD"] = "A$",
        ["NZD"] = "NZ$",
        ["BRL"] = "R$",
        ["MXN"] = "MX$",
    };

    /// <summary>Evaluates a function call.</summary>
    /// <param name="call">The call, with its arguments still as dynamic values.</param>
    /// <param name="resolve">Resolves a dynamic value in the caller's scope.</param>
    /// <param name="culture">The culture for formatting. Defaults to the invariant culture.</param>
    /// <returns>The result as JSON, or <see langword="null"/> for <c>void</c> functions.</returns>
    /// <exception cref="A2UIRenderException">The function is unknown or a required argument is missing.</exception>
    public static JsonNode? Invoke(FunctionCall call, Func<DynamicValue?, JsonNode?> resolve, CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentNullException.ThrowIfNull(resolve);

        culture ??= CultureInfo.InvariantCulture;
        var args = new Arguments(call, resolve);

        return call.Call switch
        {
            "required" => Required(args.Node("value")),
            "regex" => JsonValue.Create(Regex.IsMatch(args.String("value") ?? string.Empty, args.String("pattern", required: true)!, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250))),
            "email" => JsonValue.Create(EmailPattern.IsMatch(args.String("value") ?? string.Empty)),
            "length" => Length(args),
            "numeric" => Numeric(args),
            "and" => JsonValue.Create(args.Booleans("values").All(b => b)),
            "or" => JsonValue.Create(args.Booleans("values").Any(b => b)),
            "not" => JsonValue.Create(!args.Boolean("value")),
            "formatString" => JsonValue.Create(FormatString(args.String("value", required: true)!, resolve, culture)),
            "formatNumber" => JsonValue.Create(FormatNumber(args.Number("value") ?? 0, args.OptionalNumber("decimals"), args.OptionalBoolean("grouping") ?? true, culture)),
            "formatCurrency" => JsonValue.Create(FormatCurrency(args.Number("value") ?? 0, args.String("currency", required: true)!, args.OptionalNumber("decimals"), args.OptionalBoolean("grouping") ?? true, culture)),
            "formatDate" => JsonValue.Create(FormatDate(args.Node("value"), args.String("format", required: true)!, culture)),
            "pluralize" => JsonValue.Create(Pluralize(args)),
            OpenUrlFunction => JsonValue.Create(args.String("url", required: true)),
            _ => throw new A2UIRenderException($"The catalog function '{call.Call}' is not implemented by this renderer."),
        };
    }

    /// <summary>
    /// Reads a number out of a node whichever CLR type built it: a parsed element, or a value created
    /// from a <see cref="double"/>, an <see cref="int"/>, a <see cref="long"/> or a <see cref="decimal"/>.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="number">The number, when this method returns <see langword="true"/>.</param>
    /// <returns><see langword="false"/> when the node is not numeric.</returns>
    public static bool TryGetNumber(JsonNode? node, out double number)
    {
        number = 0;
        if (node is not JsonValue value)
        {
            return false;
        }

        if (value.TryGetValue<double>(out var d))
        {
            number = d;
            return true;
        }

        if (value.TryGetValue<int>(out var i))
        {
            number = i;
            return true;
        }

        if (value.TryGetValue<long>(out var l))
        {
            number = l;
            return true;
        }

        if (value.TryGetValue<decimal>(out var m))
        {
            number = (double)m;
            return true;
        }

        if (value.TryGetValue<float>(out var f))
        {
            number = f;
            return true;
        }

        if (value.TryGetValue<System.Text.Json.JsonElement>(out var element) &&
            element.ValueKind == System.Text.Json.JsonValueKind.Number &&
            element.TryGetDouble(out var fromElement))
        {
            number = fromElement;
            return true;
        }

        return false;
    }

    /// <summary>Whether a value counts as present: not null, not an empty string, not an empty array.</summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> when present.</returns>
    public static bool IsPresent(JsonNode? value) => value switch
    {
        null => false,
        JsonValue scalar when scalar.TryGetValue<string>(out var text) => text.Length > 0,
        JsonArray array => array.Count > 0,
        _ => true,
    };

    private static JsonValue Required(JsonNode? value) => JsonValue.Create(IsPresent(value));

    private static JsonValue Length(Arguments args)
    {
        var length = (args.String("value") ?? string.Empty).Length;
        var min = args.OptionalNumber("min");
        var max = args.OptionalNumber("max");
        return JsonValue.Create((min is null || length >= min) && (max is null || length <= max));
    }

    private static JsonValue Numeric(Arguments args)
    {
        var value = args.Number("value");
        if (value is null)
        {
            return JsonValue.Create(false);
        }

        var min = args.OptionalNumber("min");
        var max = args.OptionalNumber("max");
        return JsonValue.Create((min is null || value >= min) && (max is null || value <= max));
    }

    /// <summary>
    /// Interpolates <c>${expression}</c> blocks: an absolute or relative data path, a quoted literal, a
    /// number, <c>true</c>/<c>false</c>/<c>null</c>, or a call written <c>name(arg: value, ...)</c>.
    /// <c>\${</c> writes a literal <c>${</c>.
    /// </summary>
    internal static string FormatString(string template, Func<DynamicValue?, JsonNode?> resolve, CultureInfo culture)
    {
        var result = new StringBuilder();
        var i = 0;

        while (i < template.Length)
        {
            if (template[i] == '\\' && i + 2 < template.Length && template[i + 1] == '$' && template[i + 2] == '{')
            {
                result.Append("${");
                i += 3;
                continue;
            }

            if (template[i] == '$' && i + 1 < template.Length && template[i + 1] == '{')
            {
                var end = FindClosingBrace(template, i + 2);
                if (end < 0)
                {
                    result.Append(template, i, template.Length - i);
                    break;
                }

                var expression = template.Substring(i + 2, end - i - 2).Trim();
                result.Append(Display(Evaluate(expression, resolve, culture), culture));
                i = end + 1;
                continue;
            }

            result.Append(template[i]);
            i++;
        }

        return result.ToString();
    }

    private static int FindClosingBrace(string text, int from)
    {
        var depth = 1;
        var inQuote = '\0';

        for (var i = from; i < text.Length; i++)
        {
            var c = text[i];
            if (inQuote != '\0')
            {
                if (c == inQuote)
                {
                    inQuote = '\0';
                }

                continue;
            }

            switch (c)
            {
                case '\'':
                case '"':
                    inQuote = c;
                    break;
                case '{':
                    depth++;
                    break;
                case '}':
                    if (--depth == 0)
                    {
                        return i;
                    }

                    break;
            }
        }

        return -1;
    }

    /// <summary>Evaluates one interpolation expression.</summary>
    private static JsonNode? Evaluate(string expression, Func<DynamicValue?, JsonNode?> resolve, CultureInfo culture)
    {
        if (expression.Length == 0)
        {
            return null;
        }

        if ((expression[0] == '\'' && expression[^1] == '\'') || (expression[0] == '"' && expression[^1] == '"'))
        {
            return JsonValue.Create(expression.Substring(1, expression.Length - 2));
        }

        switch (expression)
        {
            case "true":
                return JsonValue.Create(true);
            case "false":
                return JsonValue.Create(false);
            case "null":
                return null;
        }

        if (double.TryParse(expression, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return JsonValue.Create(number);
        }

        var open = expression.IndexOf('(', StringComparison.Ordinal);
        if (open > 0 && expression[^1] == ')')
        {
            var name = expression.Substring(0, open).Trim();
            var argumentList = expression.Substring(open + 1, expression.Length - open - 2);
            var args = new Dictionary<string, DynamicValue>(StringComparer.Ordinal);

            foreach (var pair in SplitArguments(argumentList))
            {
                var colon = pair.IndexOf(':', StringComparison.Ordinal);
                if (colon <= 0)
                {
                    continue;
                }

                args[pair.Substring(0, colon).Trim()] = ToDynamic(pair.Substring(colon + 1).Trim(), resolve, culture);
            }

            return Invoke(new FunctionCall(name) { Args = args }, resolve, culture);
        }

        // Anything else is a data path, absolute or relative to the current item.
        return resolve(DynamicValue.FromPath(expression));
    }

    private static DynamicValue ToDynamic(string expression, Func<DynamicValue?, JsonNode?> resolve, CultureInfo culture) =>
        DynamicValue.FromLiteral(Evaluate(expression, resolve, culture));

    private static IEnumerable<string> SplitArguments(string list)
    {
        var depth = 0;
        var inQuote = '\0';
        var start = 0;

        for (var i = 0; i < list.Length; i++)
        {
            var c = list[i];
            if (inQuote != '\0')
            {
                if (c == inQuote)
                {
                    inQuote = '\0';
                }

                continue;
            }

            switch (c)
            {
                case '\'':
                case '"':
                    inQuote = c;
                    break;
                case '(':
                    depth++;
                    break;
                case ')':
                    depth--;
                    break;
                case ',' when depth == 0:
                    yield return list.Substring(start, i - start);
                    start = i + 1;
                    break;
            }
        }

        if (start < list.Length)
        {
            yield return list.Substring(start);
        }
    }

    /// <summary>How a resolved value reads when put into text.</summary>
    public static string Display(JsonNode? value, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.InvariantCulture;

        return value switch
        {
            null => string.Empty,
            JsonValue scalar when scalar.TryGetValue<string>(out var text) => text,
            JsonValue scalar when scalar.TryGetValue<bool>(out var flag) => flag ? "true" : "false",
            JsonValue scalar when TryGetNumber(scalar, out var number) => number.ToString(culture),
            _ => value.ToJsonString(),
        };
    }

    private static string FormatNumber(double value, double? decimals, bool grouping, CultureInfo culture)
    {
        if (decimals is { } places)
        {
            // Half away from zero, as Intl.NumberFormat rounds in the browser renderers.
            value = Math.Round(value, Math.Clamp((int)places, 0, 15), MidpointRounding.AwayFromZero);
        }

        var format = new NumberFormatInfo
        {
            NumberDecimalSeparator = culture.NumberFormat.NumberDecimalSeparator,
            NumberGroupSeparator = grouping ? culture.NumberFormat.NumberGroupSeparator : string.Empty,
            NumberGroupSizes = culture.NumberFormat.NumberGroupSizes,
        };

        var pattern = decimals is { } d ? "N" + ((int)d).ToString(CultureInfo.InvariantCulture) : null;
        return pattern is null
            ? (grouping ? value.ToString("#,0.##########", format) : value.ToString("0.##########", format))
            : value.ToString(pattern, format);
    }

    private static string FormatCurrency(double value, string currency, double? decimals, bool grouping, CultureInfo culture)
    {
        var symbol = CurrencySymbols.TryGetValue(currency, out var known) ? known : currency.ToUpperInvariant() + " ";
        var places = decimals ?? (string.Equals(currency, "JPY", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(currency, "KRW", StringComparison.OrdinalIgnoreCase) ? 0 : 2);

        var amount = FormatNumber(Math.Abs(value), places, grouping, culture);
        return (value < 0 ? "-" : string.Empty) + symbol + amount;
    }

    private static string FormatDate(JsonNode? value, string pattern, CultureInfo culture)
    {
        DateTimeOffset date;
        switch (value)
        {
            case JsonValue scalar when scalar.TryGetValue<string>(out var text):
                if (!DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date))
                {
                    return text;
                }

                break;

            case JsonValue scalar when scalar.TryGetValue<double>(out var epoch):
                date = DateTimeOffset.FromUnixTimeMilliseconds((long)epoch);
                break;

            default:
                return string.Empty;
        }

        return date.ToString(ToDotNetPattern(pattern), culture);
    }

    /// <summary>Maps the TR35 tokens the catalog documents onto .NET custom format specifiers.</summary>
    internal static string ToDotNetPattern(string pattern)
    {
        var result = new StringBuilder();
        var i = 0;

        while (i < pattern.Length)
        {
            var c = pattern[i];

            if (c == '\'')
            {
                var close = pattern.IndexOf('\'', i + 1);
                var literal = close < 0 ? pattern.Substring(i + 1) : pattern.Substring(i + 1, close - i - 1);
                result.Append('"').Append(literal.Replace("\"", "\\\"", StringComparison.Ordinal)).Append('"');
                i = close < 0 ? pattern.Length : close + 1;
                continue;
            }

            var run = 1;
            while (i + run < pattern.Length && pattern[i + run] == c)
            {
                run++;
            }

            result.Append(c switch
            {
                'y' => run <= 2 ? "yy" : "yyyy",
                'M' => new string('M', Math.Min(run, 4)),
                'd' => new string('d', Math.Min(run, 2)),
                'E' => run >= 4 ? "dddd" : "ddd",
                'h' => new string('h', Math.Min(run, 2)),
                'H' => new string('H', Math.Min(run, 2)),
                'm' => new string('m', Math.Min(run, 2)),
                's' => new string('s', Math.Min(run, 2)),
                'a' => "tt",
                _ when char.IsLetter(c) => new string(c, run),
                _ => new string(c, run),
            });

            i += run;
        }

        return result.ToString();
    }

    private static string Pluralize(Arguments args)
    {
        var count = args.Number("value") ?? 0;
        var other = args.String("other", required: true)!;

        // English-style categories; a fuller CLDR mapping is a culture concern left to the host.
        var category = count switch
        {
            0 => "zero",
            1 => "one",
            2 => "two",
            _ => "other",
        };

        return args.String(category) ?? other;
    }

    /// <summary>Typed access to a call's arguments, each resolved on demand.</summary>
    private sealed class Arguments(FunctionCall call, Func<DynamicValue?, JsonNode?> resolve)
    {
        internal JsonNode? Node(string name) =>
            call.Args is { } args && args.TryGetValue(name, out var value) ? resolve(value) : null;

        internal string? String(string name, bool required = false)
        {
            var node = Node(name);
            if (node is null)
            {
                return required
                    ? throw new A2UIRenderException($"The function '{call.Call}' needs an argument named '{name}'.")
                    : null;
            }

            return Display(node);
        }

        internal double? Number(string name)
        {
            var node = Node(name);
            if (TryGetNumber(node, out var number))
            {
                return number;
            }

            return node is JsonValue scalar && scalar.TryGetValue<string>(out var text) &&
                   double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }

        internal double? OptionalNumber(string name) => Number(name);

        internal bool Boolean(string name) => Node(name) is JsonValue scalar && scalar.TryGetValue<bool>(out var b) && b;

        internal bool? OptionalBoolean(string name) =>
            Node(name) is JsonValue scalar && scalar.TryGetValue<bool>(out var b) ? b : null;

        internal IEnumerable<bool> Booleans(string name)
        {
            if (call.Args is not { } args || !args.TryGetValue(name, out var value))
            {
                yield break;
            }

            // The list itself may be a literal array of dynamic values, each resolved in turn.
            if (value.Kind == DynamicValueKind.Literal && value.Literal is JsonArray items)
            {
                foreach (var item in items)
                {
                    var resolved = resolve(DynamicValue.FromJson(item));
                    yield return resolved is JsonValue scalar && scalar.TryGetValue<bool>(out var b) && b;
                }

                yield break;
            }

            if (resolve(value) is JsonArray resolvedItems)
            {
                foreach (var item in resolvedItems)
                {
                    yield return item is JsonValue scalar && scalar.TryGetValue<bool>(out var b) && b;
                }
            }
        }
    }
}
