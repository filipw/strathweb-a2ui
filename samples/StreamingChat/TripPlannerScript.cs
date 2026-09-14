using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Strathweb.A2UI.Samples;

namespace StreamingChat;

/// <summary>Plays the model when no API key is configured: calls the tool, then plans from the answers.</summary>
internal sealed partial class TripPlannerScript : ScriptedChatClient
{
    protected override ScriptedReply Reply(IReadOnlyList<ChatMessage> history)
    {
        if (HasToolResult(history))
        {
            return new ScriptedReply("I have put a short form on your screen. Fill it in and I will draft the plan.");
        }

        var text = LastUserText(history);

        if (IsAction(history, "submit_trip"))
        {
            return new ScriptedReply(Plan(text));
        }

        if (WantsTrip().IsMatch(text))
        {
            return new ScriptedReply(
                "Nice. Let me grab a few details first.",
                ScriptedReply.Call("plan_trip", ("destination", Destination(text))));
        }

        return new ScriptedReply("I plan short trips. Try \"Plan a weekend in Lisbon\" or name a city you have in mind.");
    }

    /// <summary>The place named after "to" or "in", or a default.</summary>
    private static string Destination(string text)
    {
        var match = DestinationPattern().Match(text);
        return match.Success ? match.Groups["place"].Value.Trim().TrimEnd('.', '!', '?') : "Lisbon";
    }

    /// <summary>Builds a plan from the sentence the library wrote about the submitted form.</summary>
    private static string Plan(string sentence)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match pair in ContextPairs().Matches(sentence))
        {
            values[pair.Groups["key"].Value] = pair.Groups["value"].Value.Replace("\"", string.Empty, StringComparison.Ordinal);
        }

        var destination = values.GetValueOrDefault("destination", "your destination");
        var travellers = values.GetValueOrDefault("travellers", "2");
        var budget = values.GetValueOrDefault("budget", "mid") switch
        {
            var b when b.Contains("low", StringComparison.Ordinal) => "keeping it cheap",
            var b when b.Contains("high", StringComparison.Ordinal) => "going all in",
            _ => "at a comfortable budget",
        };
        var interests = values.GetValueOrDefault("interests", string.Empty)
            .Trim('[', ']')
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var lines = new List<string>
        {
            $"{destination} for {travellers} from {values.GetValueOrDefault("from", "?")} to {values.GetValueOrDefault("to", "?")}, {budget}.",
        };

        var day = 1;
        foreach (var interest in interests.Take(3))
        {
            lines.Add($"Day {day++}: {Suggestion(interest, destination)}");
        }

        if (day == 1)
        {
            lines.Add($"Day 1: an unhurried walk through the old town of {destination}, then dinner wherever the locals queue.");
        }

        lines.Add("Want me to adjust the pace or swap a day?");
        return string.Join("\n", lines);
    }

    private static string Suggestion(string interest, string destination) => interest switch
    {
        "food" => $"a market breakfast, a long lunch, and the tasting menu everyone in {destination} talks about.",
        "museums" => "the two museums worth the queue, with a proper coffee in between.",
        "nightlife" => "a late start, then the bar street after ten.",
        "nature" => "out of the city early for the coast or the hills, back for sunset.",
        "architecture" => "a self-guided walk past the buildings that made the city famous.",
        _ => $"a free day to wander {destination}.",
    };

    [GeneratedRegex(@"\b(trip|plan|weekend|visit|holiday|vacation|travel)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WantsTrip();

    [GeneratedRegex(@"\b(?:to|in)\s+(?<place>[A-Z][\w' -]+)", RegexOptions.CultureInvariant)]
    private static partial Regex DestinationPattern();

    [GeneratedRegex(@"(?<key>\w+)=(?<value>""[^""]*""|\[[^\]]*\]|\S+?)(?=,\s|\.$|$)", RegexOptions.CultureInvariant)]
    private static partial Regex ContextPairs();
}
