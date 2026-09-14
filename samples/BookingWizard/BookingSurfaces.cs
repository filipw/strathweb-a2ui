using System.Text.Json.Nodes;
using Strathweb.A2UI;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Surfaces;
using Strathweb.A2UI.Values;

namespace BookingWizard;

/// <summary>
/// One surface, three steps. Moving between steps replaces the <c>step</c> and <c>nav</c> components
/// by id with <c>updateComponents</c>; the rest of the card, and the data the user has typed, stay put.
/// </summary>
internal static class BookingSurfaces
{
    internal const string SurfaceId = "table_booking";

    internal static A2UICatalog Catalog { get; } = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

    /// <summary>The whole card, opened on step one.</summary>
    internal static A2UISurface Start()
    {
        var s = A2UISurface.Create(SurfaceId, Catalog);
        var ui = s.Components;

        return s
            .Root(ui.Card(ui.Column(
                    ui.Text("Book a table").Variant(TextVariant.H2),
                    ui.Text(Bind.Path("/stepLabel")).Variant(TextVariant.Caption),
                    ui.Divider(),
                    StepOne(ui),
                    NavOne(ui))
                .WithId("wizard")))
            .WithData(data =>
            {
                data["step"] = 1;
                data["stepLabel"] = "Step 1 of 3: who is coming";
                data["name"] = string.Empty;
                data["guests"] = 2;
                data["occasion"] = new JsonArray();
                data["date"] = string.Empty;
                data["time"] = new JsonArray();
                data["window"] = false;
                data["notes"] = string.Empty;
            })

            // The agent reads the answers out of the data model the renderer sends back.
            .SendDataModel()
            .Build();
    }

    /// <summary>Swaps in the components for a step. Data written on other steps is untouched.</summary>
    internal static IReadOnlyList<A2UIMessage> GoToStep(int step)
    {
        var update = A2UISurfaceUpdate.For(SurfaceId, Catalog);
        var ui = update.Components;

        switch (step)
        {
            case 1:
                StepOne(ui);
                NavOne(ui);
                break;
            case 2:
                StepTwo(ui);
                NavMiddle(ui);
                break;
            default:
                StepThree(ui);
                NavLast(ui);
                break;
        }

        return update
            .SetData("/step", JsonValue.Create(step))
            .SetData("/stepLabel", JsonValue.Create(step switch
            {
                1 => "Step 1 of 3: who is coming",
                2 => "Step 2 of 3: when",
                _ => "Step 3 of 3: review",
            }))
            .Build();
    }

    /// <summary>Replaces the card's contents with the confirmation.</summary>
    internal static IReadOnlyList<A2UIMessage> Confirmed(string summary)
    {
        var update = A2UISurfaceUpdate.For(SurfaceId, Catalog);
        var ui = update.Components;

        ui.Column(
                ui.Text("Booked").Variant(TextVariant.H2),
                ui.Text(summary),
                ui.Text("A confirmation is on its way. Nothing else to do.").Variant(TextVariant.Caption),
                ui.Divider(),
                ui.Button("Book another table").Primary().OnClick(Act.Event("start_over")))
            .WithId("wizard");

        return update.Build();
    }

    private static ColumnBuilder StepOne(BasicComponents ui) =>
        ui.Column(
                ui.TextField("Your name").Value(Bind.Path("/name"))
                    .Checks(Check.Required(Bind.Path("/name"), "We need a name for the booking.")),
                ui.Slider(Bind.Path("/guests"), 10).Min(1).Label("Guests"),
                ui.ChoicePicker().Label("Occasion").MutuallyExclusive().DisplayStyle(ChoiceDisplayStyle.Chips)
                    .Options(("Just dinner", "dinner"), ("Birthday", "birthday"), ("Anniversary", "anniversary"), ("Business", "business"))
                    .Value(Bind.Path("/occasion")))
            .WithId("step");

    private static ColumnBuilder StepTwo(BasicComponents ui) =>
        ui.Column(
                ui.DateTimeInput(Bind.Path("/date")).EnableDate().Label("Date")
                    .Checks(Check.Required(Bind.Path("/date"), "Pick a date.")),
                ui.ChoicePicker().Label("Time").MutuallyExclusive().DisplayStyle(ChoiceDisplayStyle.Chips)
                    .Options(("18:00", "18:00"), ("19:00", "19:00"), ("20:00", "20:00"), ("21:00", "21:00"))
                    .Value(Bind.Path("/time"))
                    .Checks(Check.Required(Bind.Path("/time"), "Pick a time.")),
                ui.CheckBox("A window table if there is one", Bind.Path("/window")))
            .WithId("step");

    private static ColumnBuilder StepThree(BasicComponents ui) =>
        ui.Column(
                ui.Tabs()
                    .Tab("Summary", ui.Column(
                        ui.Text(Format("${/name}, party of ${/guests}")).Variant(TextVariant.H4),
                        ui.Text(Format("${/date} at ${/time}")),
                        ui.Text(Format("Occasion: ${/occasion}")).Variant(TextVariant.Caption)))
                    .Tab("Notes", ui.TextField("Anything the kitchen should know?").Variant(TextFieldVariant.LongText).Value(Bind.Path("/notes"))),
                ui.Modal(
                    ui.Button("House rules").Variant(ButtonVariant.Borderless).OnClick(Act.Event("noop")),
                    ui.Column(
                        ui.Text("House rules").Variant(TextVariant.H3),
                        ui.Text("Tables are held for fifteen minutes. Parties of eight or more are seated at a shared table. Corkage is a flat fee."))))
            .WithId("step");

    private static RowBuilder NavOne(BasicComponents ui) =>
        ui.Row(ui.Button("Next").Primary().OnClick(Act.Event("next"))).Justify(LayoutJustify.End).WithId("nav");

    private static RowBuilder NavMiddle(BasicComponents ui) =>
        ui.Row(
                ui.Button("Back").OnClick(Act.Event("back")),
                ui.Button("Next").Primary().OnClick(Act.Event("next")))
            .Justify(LayoutJustify.SpaceBetween)
            .WithId("nav");

    private static RowBuilder NavLast(BasicComponents ui) =>
        ui.Row(
                ui.Button("Back").OnClick(Act.Event("back")),
                ui.Button("Confirm booking").Primary().OnClick(Act.Event(
                    "confirm",
                    ("name", Bind.Path("/name")),
                    ("guests", Bind.Path("/guests")),
                    ("date", Bind.Path("/date")),
                    ("time", Bind.Path("/time")),
                    ("window", Bind.Path("/window")),
                    ("notes", Bind.Path("/notes")))))
            .Justify(LayoutJustify.SpaceBetween)
            .WithId("nav");

    /// <summary>A <c>formatString</c> call, evaluated by the renderer against the live data model.</summary>
    private static DynamicValue Format(string template) =>
        DynamicValue.FromCall("formatString", new Dictionary<string, DynamicValue>(StringComparer.Ordinal) { ["value"] = template });
}
