using System.Text.Json.Nodes;
using A2A;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Surfaces;
using Strathweb.A2UI.TestSupport;

namespace Strathweb.A2UI.AgentFramework.UnitTests;

/// <summary>
/// The wrapper around a real <c>ChatClientAgent</c> whose tool emits a surface. The scripted doubles
/// elsewhere run their callback before yielding and keep no chat history; a model-backed agent does
/// neither, and that is where the integration has to hold.
/// </summary>
public class ModelBackedAgentTests
{
    private static readonly A2UICatalog Basic = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

    private static A2UISurface Survey(string id = "survey_1")
    {
        var s = A2UISurface.Create(id, Basic);
        var ui = s.Components;
        return s.Root(ui.Card(ui.Column(
            ui.Text("How did we do?").Variant(TextVariant.H3),
            ui.Button("Submit").Primary().OnClick(Act.Event("submit"))))).Build();
    }

    private static (A2UIAgent Agent, ToolCallingChatClient Model) Build(Action? onToolCalled = null)
    {
        var model = new ToolCallingChatClient("show_survey");

        string ShowSurvey()
        {
            onToolCalled?.Invoke();
            A2UIEmitter.Emit(Survey());
            return "A survey is on the user's screen.";
        }

        var agent = model
            .AsAIAgent(
                instructions: "Show the survey when asked.",
                name: "survey-agent",
                tools: [AIFunctionFactory.Create(ShowSurvey, "show_survey")])
            .WithA2UI();

        return (agent, model);
    }

    [Fact]
    public async Task RunAsync_AToolCalledByTheModel_EmitsTheSurface()
    {
        var (agent, _) = Build();

        var response = await agent.RunAsync("hello");

        Assert.Single(response.Messages.SelectMany(m => m.Contents).OfType<A2UIContent>());
    }

    [Fact]
    public async Task RunStreamingAsync_AToolCalledAfterTheFirstUpdate_StillEmitsTheSurface()
    {
        // Providers stream the model's words before the function call, so the tool always runs after
        // at least one update has been yielded.
        var (agent, _) = Build();

        var updates = new List<AgentResponseUpdate>();
        await foreach (var update in agent.RunStreamingAsync("hello"))
        {
            updates.Add(update);
        }

        var contents = updates.SelectMany(u => u.Contents).ToList();
        Assert.Single(contents.OfType<A2UIContent>());
        Assert.DoesNotContain(contents.OfType<FunctionResultContent>(), r => r.Exception is not null);
    }

    [Fact]
    public async Task RunStreamingAsync_TheToolStillSeesTheRunAfterTheFirstUpdate()
    {
        var sawRun = false;
        var sawContext = false;
        var (agent, _) = Build(() =>
        {
            sawRun = A2UIEmitter.IsInRun;
            sawContext = A2UIRunContext.Current is not null;
        });

        await foreach (var _ in agent.RunStreamingAsync("hello"))
        {
        }

        Assert.True(sawRun);
        Assert.True(sawContext);
    }

    [Fact]
    public async Task SerializeSessionAsync_AfterTheModelShowedASurface_Succeeds()
    {
        // A durable session store serializes the session. The emitted surface must not poison it.
        var (agent, _) = Build();
        var session = await agent.CreateSessionAsync();
        await agent.RunAsync("hello", session);

        var serialized = await agent.SerializeSessionAsync(session);
        var restored = await agent.DeserializeSessionAsync(serialized);

        Assert.True(restored.GetA2UISurfaceRegistry().Contains("survey_1"));
    }

    [Fact]
    public async Task SerializeSessionAsync_AfterAnInboundAction_Succeeds()
    {
        var (agent, _) = Build();
        var session = await agent.CreateSessionAsync();

        await agent.RunAsync([InboundAction()], session);

        await agent.SerializeSessionAsync(session);
    }

    [Fact]
    public async Task RunAsync_TheSurfaceIsNotReplayedToTheModelOnTheNextTurn()
    {
        var (agent, model) = Build();
        var session = await agent.CreateSessionAsync();

        await agent.RunAsync("hello", session);
        await agent.RunAsync("thanks", session);

        Assert.DoesNotContain(
            model.Requests[^1].SelectMany(m => m.Contents),
            c => c is A2UIContent);
    }

    [Fact]
    public async Task RunAsync_AnInboundAction_ReachesTheModelAsASentenceOnly()
    {
        var (agent, model) = Build();
        var session = await agent.CreateSessionAsync();

        await agent.RunAsync([InboundAction()], session);

        var contents = model.Requests[^1].Single(m => m.Role == ChatRole.User).Contents;
        var text = Assert.Single(contents);
        Assert.Contains("submit_satisfaction", Assert.IsType<TextContent>(text).Text, StringComparison.Ordinal);
    }

    private static ChatMessage InboundAction()
    {
        var action = new ActionMessage(
            "submit_satisfaction",
            "survey_1",
            "button_1",
            DateTimeOffset.UnixEpoch,
            new JsonObject { ["rating"] = 5 });

        return new Message { Role = Role.User, Parts = [A2UIParts.Create(action)] }.ToChatMessage();
    }
}
