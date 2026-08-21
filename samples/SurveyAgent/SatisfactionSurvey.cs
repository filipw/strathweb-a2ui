using Strathweb.A2UI;
using Strathweb.A2UI.AgentFramework;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Surfaces;

namespace SurveyAgent;

/// <summary>The surface this sample exists to show: a rating, a comment box, and a submit button.</summary>
internal static class SatisfactionSurvey
{
    private static readonly A2UICatalog Catalog = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

    internal static A2UISurface Build(string question, string? surfaceId = null)
    {
        var s = A2UISurface.Create(surfaceId ?? A2UISurfaceId.New("survey"), Catalog);
        var ui = s.Components;

        return s
            .Root(ui.Card(ui.Column(
                    ui.Text(question).Variant(TextVariant.H3),
                    ui.ChoicePicker()
                        .Label("Your rating")
                        .MutuallyExclusive()
                        .DisplayStyle(ChoiceDisplayStyle.Chips)
                        .Options(
                            ("Very satisfied", "5"),
                            ("Somewhat satisfied", "4"),
                            ("Neutral", "3"),
                            ("Not really", "2"),
                            ("Not at all", "1"))
                        .Value(Bind.Path("/rating"))
                        .Checks(Check.Required(Bind.Path("/rating"), "Please pick a rating.")),
                    ui.TextField("Anything else you would like to add?")
                        .Value(Bind.Path("/comment"))
                        .Variant(TextFieldVariant.LongText),
                    ui.Button("Submit")
                        .Primary()
                        .Describe(label: "Submit your rating")
                        .OnClick(Act.Event(
                            "submit_satisfaction",
                            ("rating", Bind.Path("/rating")),
                            ("comment", Bind.Path("/comment")))))
                .Align(LayoutAlign.Stretch)))
            .WithData(data =>
            {
                data["rating"] = null;
                data["comment"] = string.Empty;
            })
            .Build();
    }
}
