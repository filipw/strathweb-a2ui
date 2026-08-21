using A2A;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.AgentFramework;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.State;
using Strathweb.A2UI.Surfaces;
using Strathweb.A2UI.TestSupport;

namespace Strathweb.A2UI.IntegrationTests;

/// <summary>
/// Proves that a surface a tool emits survives the real A2A hosting path and arrives as a
/// spec-compliant data part.
/// </summary>
public class A2ATransportTests
{
    private static readonly A2UICatalog Basic = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

    [Fact]
    public async Task MessageMode_TheResponseCarriesASchemaValidA2UISurface()
    {
        await using var host = await A2AHost.StartAsync(SurveyAgent());

        var response = await host.Client.SendMessageAsync(Request());

        AssertSurfaceArrived(PartsOf(response));
    }

    [Fact]
    public async Task StreamingMode_TheStreamCarriesASchemaValidA2UISurface()
    {
        await using var host = await A2AHost.StartAsync(SurveyAgent());

        var parts = new List<Part>();
        await foreach (var chunk in host.Client.SendStreamingMessageAsync(Request()))
        {
            parts.AddRange(PartsOf(chunk));
        }

        AssertSurfaceArrived(parts);
    }

    [Fact]
    public async Task MessageMode_ThePartUsesTheCurrentMimeTypeAndAnArrayPayload()
    {
        await using var host = await A2AHost.StartAsync(SurveyAgent());

        var part = Assert.Single(PartsOf(await host.Client.SendMessageAsync(Request())), A2UIParts.IsA2UI);

        Assert.Equal(
            "application/a2ui+json",
            part.Metadata![A2UIParts.MimeTypeMetadataKey].GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Array, part.Data!.Value.ValueKind);
    }

    [Fact]
    public async Task MessageMode_TheModelsProseSurvivesAlongsideTheSurface()
    {
        // The tool tells the model a UI was shown; that text is what stops it describing the form in
        // prose as well, so it has to arrive.
        await using var host = await A2AHost.StartAsync(SurveyAgent());

        var parts = PartsOf(await host.Client.SendMessageAsync(Request()));

        Assert.Contains(parts, p => p.Text?.Contains("survey", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task MessageMode_AnAgentThatShowsNothing_SendsNoA2UIPart()
    {
        await using var host = await A2AHost.StartAsync(
            new ScriptedAgent(_ => { }, "Nothing to show.", "plain-agent").WithA2UI());

        var parts = PartsOf(await host.Client.SendMessageAsync(Request()));

        Assert.DoesNotContain(parts, A2UIParts.IsA2UI);
    }

    private static void AssertSurfaceArrived(IReadOnlyList<Part> parts)
    {
        var part = Assert.Single(parts, A2UIParts.IsA2UI);

        // Exactly the payload a renderer would apply, checked against the vendored schema.
        foreach (var message in System.Text.Json.Nodes.JsonNode.Parse(part.Data!.Value.GetRawText())!.AsArray())
        {
            SpecSchemas.AssertValid(SpecSchemas.AgentToRenderer, message!);
        }

        Assert.True(A2UIParts.TryRead(part, out var messages));
        Assert.Equal(3, messages.Count);

        var surfaces = new A2UISurfaceSet();
        surfaces.Apply(messages);

        Assert.True(surfaces.TryGet("satisfaction_1", out var surface));
        Assert.NotNull(surface.Root);
        Assert.Contains(surface.Components.Values, c => c.Component == "ChoicePicker");
        Assert.True(surface.DataModel!.AsObject().ContainsKey("rating"));
    }

    private static A2UIAgent SurveyAgent() =>
        new ScriptedAgent(
            _ => A2UIEmitter.Emit(BuildSurvey()),
            "A satisfaction survey has been shown to you. Please answer it.",
            "survey-agent").WithA2UI();

    private static A2UISurface BuildSurvey()
    {
        var s = A2UISurface.Create("satisfaction_1", Basic);
        var ui = s.Components;

        return s
            .Root(ui.Card(ui.Column(
                ui.Text("How satisfied were you?").Variant(TextVariant.H3),
                ui.ChoicePicker()
                    .Label("Rating")
                    .MutuallyExclusive()
                    .Options(("Very", "5"), ("Somewhat", "4"), ("Not at all", "1"))
                    .Value(Bind.Path("/rating")),
                ui.Button("Submit").Primary().OnClick(Act.Event(
                    "submit_satisfaction",
                    ("rating", Bind.Path("/rating")))))))
            .WithData(data => data["rating"] = null)
            .Build();
    }

    private static SendMessageRequest Request() => new()
    {
        Message = new Message
        {
            Role = Role.User,
            MessageId = Guid.NewGuid().ToString("N"),
            ContextId = "c1",
            Parts = [Part.FromText("How did that go?")],
        },
    };

    private static List<Part> PartsOf(SendMessageResponse response) =>
        response.Message is { } message ? [.. message.Parts] : PartsOf(response.Task);

    private static List<Part> PartsOf(StreamResponse chunk) =>
    [
        .. chunk.Message?.Parts ?? [],
        .. chunk.ArtifactUpdate?.Artifact?.Parts ?? [],
        .. chunk.StatusUpdate?.Status?.Message?.Parts ?? [],
        .. PartsOf(chunk.Task),
    ];

    private static List<Part> PartsOf(AgentTask? task)
    {
        if (task is null)
        {
            return [];
        }

        List<Part> parts = [.. task.Status?.Message?.Parts ?? []];
        foreach (var artifact in task.Artifacts ?? [])
        {
            parts.AddRange(artifact.Parts ?? []);
        }

        return parts;
    }
}
