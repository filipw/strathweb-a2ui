using System.Text.Json.Nodes;
using A2A;
using Microsoft.Extensions.AI;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.AgentFramework;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Surfaces;

namespace Strathweb.A2UI.IntegrationTests;

/// <summary>
/// The full round trip: the agent shows a surface, the renderer posts what the user did, and the
/// agent reads it back.
/// </summary>
public class ActionRoundTripTests
{
    private static readonly A2UICatalog Basic = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

    [Fact]
    public async Task AnActionPostedByTheRenderer_ReachesTheAgentAsStructuredContent()
    {
        var inner = SurveyInner();
        await using var host = await A2AHost.StartAsync(inner.WithA2UI());

        await host.Client.SendMessageAsync(TextRequest("How did that go?"));
        await host.Client.SendMessageAsync(ActionRequest());

        var action = Assert.Single(inner.LastMessages
            .SelectMany(m => m.Contents)
            .OfType<A2UIActionContent>());

        Assert.Equal("submit_satisfaction", action.Name);
        Assert.Equal("satisfaction_1", action.SurfaceId);
        Assert.Equal(5, (int)action.Action.Context["rating"]!);
    }

    [Fact]
    public async Task AnActionPostedByTheRenderer_ReachesTheModelAsASentence()
    {
        // What the model reads decides how it replies. A JSON blob here makes it answer in JSON.
        var inner = SurveyInner();
        await using var host = await A2AHost.StartAsync(inner.WithA2UI());

        await host.Client.SendMessageAsync(ActionRequest());

        var text = string.Join(
            " ",
            inner.LastMessages.SelectMany(m => m.Contents).OfType<TextContent>().Select(c => c.Text));

        Assert.Contains("submit_satisfaction", text, StringComparison.Ordinal);
        Assert.Contains("rating=5", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\"version\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AfterAnAction_TheNextTurnStillWorks()
    {
        // A malformed conversation fails on the *next* turn, silently, so that is what is asserted.
        var inner = SurveyInner();
        await using var host = await A2AHost.StartAsync(inner.WithA2UI());

        await host.Client.SendMessageAsync(TextRequest("How did that go?"));
        await host.Client.SendMessageAsync(ActionRequest());
        var third = await host.Client.SendMessageAsync(TextRequest("Thanks!"));

        Assert.NotNull(third.Message ?? (object?)third.Task);
        Assert.Contains(inner.LastMessages, m => m.Text.Contains("Thanks!", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AnActionForASurfaceNobodyShowed_StillReachesTheAgent()
    {
        // Actions are not filtered by surface: the renderer is the authority on what the user clicked.
        // Only the *data model* is scoped to surfaces this session created.
        var inner = SurveyInner();
        await using var host = await A2AHost.StartAsync(inner.WithA2UI());

        await host.Client.SendMessageAsync(ActionRequest(surfaceId: "some_other_surface"));

        Assert.Single(inner.LastMessages.SelectMany(m => m.Contents).OfType<A2UIActionContent>());
    }

    private static ScriptedAgent SurveyInner() =>
        new(
            _ => A2UIEmitter.Emit(BuildSurvey()),
            "A satisfaction survey has been shown to you.",
            "survey-agent");

    private static A2UISurface BuildSurvey()
    {
        var s = A2UISurface.Create("satisfaction_1", Basic);
        var ui = s.Components;

        return s
            .Root(ui.Card(ui.Column(
                ui.Text("How satisfied were you?").Variant(TextVariant.H3),
                ui.Button("Submit").Primary().OnClick(Act.Event(
                    "submit_satisfaction",
                    ("rating", Bind.Path("/rating")))))))
            .WithData(data => data["rating"] = null)
            .Build();
    }

    private static SendMessageRequest TextRequest(string text) => new()
    {
        Message = new Message
        {
            Role = Role.User,
            MessageId = Guid.NewGuid().ToString("N"),
            ContextId = "c1",
            Parts = [Part.FromText(text)],
        },
    };

    private static SendMessageRequest ActionRequest(string surfaceId = "satisfaction_1")
    {
        var action = new ActionMessage(
            "submit_satisfaction",
            surfaceId,
            "button_1",
            DateTimeOffset.UtcNow,
            new JsonObject { ["rating"] = 5, ["comment"] = "fast fix" });

        return new SendMessageRequest
        {
            Message = new Message
            {
                Role = Role.User,
                MessageId = Guid.NewGuid().ToString("N"),
                ContextId = "c1",
                Parts = [A2UIParts.Create(action)],
            },
        };
    }
}
