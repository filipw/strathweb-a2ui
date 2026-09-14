using System.ComponentModel;
using System.Text.Json.Nodes;
using Strathweb.A2UI;
using Strathweb.A2UI.AgentFramework;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Surfaces;

namespace StreamingChat;

/// <summary>The trip planner's instructions and its one tool, which puts a form on the user's screen.</summary>
internal static class TripPlanner
{
    internal const string Instructions = """
        You help people plan short trips.

        When the user wants to plan a trip, call plan_trip with the destination they named. The tool
        puts a form on the user's screen. Say one short sentence about it and stop; do not describe the
        form or ask the questions in prose.

        When the user submits the form you receive their answers as a sentence. Reply with a short,
        concrete plan for those dates, that group size, that budget and those interests: three to five
        lines, no headings.
        """;

    private static readonly A2UICatalog Catalog = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

    [Description("Show the user a short form that collects the dates, group size, budget and interests for a trip.")]
    internal static string PlanTrip([Description("The destination the user named.")] string destination)
    {
        var s = A2UISurface.Create(A2UISurfaceId.New("trip"), Catalog);
        var ui = s.Components;

        A2UIEmitter.Emit(s
            .Root(ui.Card(ui.Column(
                ui.Text($"Trip to {destination}").Variant(TextVariant.H3),
                ui.Text("A few details and I will draft an itinerary.").Variant(TextVariant.Caption),
                ui.Row(
                    ui.DateTimeInput(Bind.Path("/from")).EnableDate().Label("From")
                        .Checks(Check.Required(Bind.Path("/from"), "Pick a start date.")).Weight(1),
                    ui.DateTimeInput(Bind.Path("/to")).EnableDate().Label("To")
                        .Checks(Check.Required(Bind.Path("/to"), "Pick an end date.")).Weight(1)),
                ui.Slider(Bind.Path("/travellers"), 8).Min(1).Label("Travellers"),
                ui.ChoicePicker().Label("Budget").MutuallyExclusive().DisplayStyle(ChoiceDisplayStyle.Chips)
                    .Options(("Shoestring", "low"), ("Comfortable", "mid"), ("Treat yourself", "high"))
                    .Value(Bind.Path("/budget"))
                    .Checks(Check.Required(Bind.Path("/budget"), "Choose a budget.")),
                ui.ChoicePicker().Label("Interests").MultipleSelection().DisplayStyle(ChoiceDisplayStyle.Chips)
                    .Options(("Food", "food"), ("Museums", "museums"), ("Nightlife", "nightlife"), ("Nature", "nature"), ("Architecture", "architecture"))
                    .Value(Bind.Path("/interests")),
                ui.Button("Plan it").Primary().OnClick(Act.Event(
                    "submit_trip",
                    ("destination", destination),
                    ("from", Bind.Path("/from")),
                    ("to", Bind.Path("/to")),
                    ("travellers", Bind.Path("/travellers")),
                    ("budget", Bind.Path("/budget")),
                    ("interests", Bind.Path("/interests")))))))
            .WithData(data =>
            {
                data["from"] = string.Empty;
                data["to"] = string.Empty;
                data["travellers"] = 2;
                data["budget"] = new JsonArray();
                data["interests"] = new JsonArray();
            })
            .Build());

        // The return value is prompt text. Without it the model narrates the form it has just shown.
        return $"A trip form for {destination} is now on the user's screen. Say one short sentence and wait for the answer.";
    }
}
