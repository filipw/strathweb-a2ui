using System.Text.Json.Nodes;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Surfaces;
using Strathweb.A2UI.TestSupport;
using Strathweb.A2UI.Validation;
namespace Strathweb.A2UI.Protocol.UnitTests;

public class SurfaceBuilderTests
{
    private static readonly A2UICatalog Basic = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

    /// <summary>
    /// A form with a rating, a comment and a submit action.
    /// </summary>
    private static A2UISurface BuildSurvey(string surfaceId = "survey_1")
    {
        var s = A2UISurface.Create(surfaceId, Basic);
        var ui = s.Components;

        var rating = ui.ChoicePicker()
            .Label("How satisfied were you?")
            .MutuallyExclusive()
            .Options(("Very", "5"), ("Somewhat", "4"), ("Neutral", "3"), ("Not really", "2"), ("Not at all", "1"))
            .Value(Bind.Path("/rating"))
            .Checks(Check.Required(Bind.Path("/rating"), "Please pick a rating."));

        var comment = ui.TextField("Anything else?")
            .Value(Bind.Path("/comment"))
            .Variant(TextFieldVariant.LongText);

        var submit = ui.Button("Submit")
            .Primary()
            .OnClick(Act.Event(
                "submit_satisfaction",
                ("rating", Bind.Path("/rating")),
                ("comment", Bind.Path("/comment"))));

        return s
            .Root(ui.Card(ui.Column(
                ui.Text("How did we do?").Variant(TextVariant.H3),
                rating,
                comment,
                submit)))
            .WithData(data =>
            {
                data["rating"] = null;
                data["comment"] = string.Empty;
            })
            .Build();
    }

    [Fact]
    public void Build_ProducesCreateThenComponentsThenData()
    {
        var surface = BuildSurvey();

        Assert.Collection(
            surface.Messages,
            m => Assert.IsType<CreateSurfaceMessage>(m),
            m => Assert.IsType<UpdateComponentsMessage>(m),
            m => Assert.IsType<UpdateDataModelMessage>(m));
    }

    [Fact]
    public void Build_EveryMessageValidatesAgainstTheVendoredSchema()
    {
        foreach (var message in BuildSurvey().Messages)
        {
            SpecSchemas.AssertValid(SpecSchemas.AgentToRenderer, A2UIJson.ToJsonObject(message));
        }
    }

    [Fact]
    public void Build_AssignsExactlyOneRootAndGivesEveryComponentAnId()
    {
        var components = Components(BuildSurvey());

        Assert.Equal(1, components.Count(c => c.Id == "root"));
        Assert.All(components, c => Assert.NotEqual(string.Empty, c.Id));
        Assert.Equal(components.Count, components.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Build_GeneratesTheSameIdsEveryTime()
    {
        // Deterministic ids keep golden files and diffs readable, and make a surface reproducible.
        Assert.Equal(
            Components(BuildSurvey()).Select(c => c.Id),
            Components(BuildSurvey()).Select(c => c.Id));
    }

    [Fact]
    public void Build_IdsAreReadableRatherThanOpaque()
    {
        var ids = Components(BuildSurvey()).Select(c => c.Id).ToList();

        Assert.Contains(ids, id => id.StartsWith("choicePicker_", StringComparison.Ordinal));
        Assert.Contains(ids, id => id.StartsWith("text_", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_WiresChildrenByIdRatherThanByNesting()
    {
        var components = Components(BuildSurvey());
        var root = components.Single(c => c.Id == "root");
        var childId = (string?)root.Properties["child"];

        Assert.NotNull(childId);
        Assert.Contains(components, c => c.Id == childId && c.Component == "Column");
    }

    [Fact]
    public void Build_CarriesTheInitialDataModel()
    {
        var data = (UpdateDataModelMessage)BuildSurvey().Messages[2];

        Assert.Equal("/", data.Path);
        Assert.True(data.Value!.AsObject().ContainsKey("rating"));
    }

    [Fact]
    public void Build_WithoutARoot_SaysSo()
    {
        var s = A2UISurface.Create("s1", Basic);
        s.Components.Text("orphaned");

        var thrown = Assert.Throws<InvalidOperationException>(() => s.Build());

        Assert.Contains("root", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_WithAComponentNobodyAttached_FailsValidationRatherThanDroppingIt()
    {
        // Silently dropping it would turn a typo into a missing widget with no explanation.
        var s = A2UISurface.Create("s1", Basic);
        var ui = s.Components;
        ui.Text("I was never attached");
        s.Root(ui.Text("Hello"));

        var thrown = Assert.Throws<A2UIValidationException>(() => s.Build());

        Assert.Contains(thrown.Result.Errors, e => e.Code == A2UIErrorCodes.OrphanComponent);
    }

    [Fact]
    public void Build_AnInvalidSurfaceNeverReachesTheCaller()
    {
        var s = A2UISurface.Create("s1", Basic);
        var ui = s.Components;

        // Text requires 'text'; Set with a name the catalog does not know is caught too.
        s.Root(ui.Text("hi").Set("sparkle", true));

        Assert.Throws<A2UIValidationException>(() => s.Build());
    }

    [Fact]
    public void WithId_PinsAComponentsIdForARendererToAddress()
    {
        var s = A2UISurface.Create("s1", Basic);
        var ui = s.Components;
        s.Root(ui.Card(ui.Text("hi").WithId("greeting")));

        Assert.Contains(Components(s.Build()), c => c.Id == "greeting");
    }

    [Fact]
    public void WithId_RejectsAClashBetweenTwoComponents()
    {
        var s = A2UISurface.Create("s1", Basic);
        var ui = s.Components;
        s.Root(ui.Column(ui.Text("a").WithId("same"), ui.Text("b").WithId("same")));

        Assert.Throws<InvalidOperationException>(() => s.Build());
    }

    [Fact]
    public void Build_TabsReferenceTheirChildrenById()
    {
        var s = A2UISurface.Create("s1", Basic);
        var ui = s.Components;
        var first = ui.Text("first");
        var second = ui.Text("second");
        s.Root(ui.Tabs().Tab("One", first).Tab("Two", second));

        var surface = s.Build();
        var root = Components(surface).Single(c => c.Id == "root");
        var tabs = root.Properties["tabs"]!.AsArray();

        Assert.Equal(2, tabs.Count);
        Assert.Equal(first.Id, (string?)tabs[0]!["child"]);
        Assert.Equal(second.Id, (string?)tabs[1]!["child"]);
    }

    [Fact]
    public void Build_ATemplateListReferencesItsTemplateComponent()
    {
        var s = A2UISurface.Create("s1", Basic);
        var ui = s.Components;
        var row = ui.Text(Bind.Path("title"));
        s.Root(ui.Column().ChildrenFrom(row, "/items")).WithData(new JsonObject
        {
            ["items"] = new JsonArray { new JsonObject { ["title"] = "one" } },
        });

        var root = Components(s.Build()).Single(c => c.Id == "root");

        Assert.Equal(row.Id, (string?)root.Properties["children"]!["componentId"]);
        Assert.Equal("/items", (string?)root.Properties["children"]!["path"]);
    }

    [Fact]
    public void SendDataModel_IsOffUnlessAskedFor()
    {
        var withoutIt = (CreateSurfaceMessage)BuildSurvey().Messages[0];

        var s = A2UISurface.Create("s2", Basic);
        var ui = s.Components;
        var withIt = (CreateSurfaceMessage)s.Root(ui.Text("hi")).SendDataModel().Build().Messages[0];

        Assert.Null(withoutIt.SendDataModel);
        Assert.True(withIt.SendDataModel);
    }

    [Fact]
    public void Build_ChecksAreCarriedOnTheComponent()
    {
        var picker = Components(BuildSurvey()).Single(c => c.Component == "ChoicePicker");

        var checks = picker.Properties["checks"]!.AsArray();
        Assert.Single(checks);
        Assert.Equal("Please pick a rating.", (string?)checks[0]!["message"]);
        Assert.Equal("required", (string?)checks[0]!["condition"]!["call"]);
    }

    [Fact]
    public void Build_AccessibilityLandsOnTheComponentNotInThePropertyBag()
    {
        var s = A2UISurface.Create("s1", Basic);
        var ui = s.Components;
        s.Root(ui.Button("Mute").Describe(Bind.Text("Mute"), Bind.Text("Silences notifications"))
            .OnClick(Act.Event("mute")));

        var button = Components(s.Build()).Single(c => c.Component == "Button");

        Assert.Equal("Mute", button.Accessibility!.Label!.Literal!.GetValue<string>());
        Assert.False(button.Properties.ContainsKey("accessibility"));
    }

    private static IReadOnlyList<Components.A2UIComponent> Components(A2UISurface surface) =>
        surface.Messages.OfType<UpdateComponentsMessage>().Single().Components;
}
