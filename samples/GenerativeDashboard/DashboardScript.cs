using Microsoft.Extensions.AI;
using Strathweb.A2UI.Samples;

namespace GenerativeDashboard;

/// <summary>
/// Plays a model that writes A2UI JSON directly. Asked for something broken, it writes a block with
/// an invented property and an unknown component, then fixes it when the library sends the errors back.
/// </summary>
internal sealed class DashboardScript : ScriptedChatClient
{
    protected override ScriptedReply Reply(IReadOnlyList<ChatMessage> history)
    {
        var text = LastUserText(history);

        if (IsRepairRequest(history))
        {
            return new ScriptedReply(Dashboard.Response("Apologies, here is the corrected dashboard."));
        }

        if (IsAction(history, "refresh"))
        {
            return new ScriptedReply(Dashboard.RefreshResponse("Fresh numbers, same dashboard: only the data model changed."));
        }

        if (IsAction(history, "explain"))
        {
            return new ScriptedReply(
                "The East lost two wholesale accounts in August, which is most of the gap. Retail there is flat, not down.");
        }

        if (text.Contains("broken", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("repair", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("invalid", StringComparison.OrdinalIgnoreCase))
        {
            return new ScriptedReply(Dashboard.BrokenResponse(
                "Here is the dashboard. (This first attempt has an invented property and an unknown component; watch the log.)"));
        }

        if (text.Contains("sales", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("dashboard", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("region", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("show", StringComparison.OrdinalIgnoreCase))
        {
            return new ScriptedReply(Dashboard.Response("Here is Q3 by region. The North carries a third of revenue; the East is the one to watch."));
        }

        return new ScriptedReply("Ask me about sales and I will draw you a dashboard. Try \"Show Q3 sales by region\".");
    }
}
