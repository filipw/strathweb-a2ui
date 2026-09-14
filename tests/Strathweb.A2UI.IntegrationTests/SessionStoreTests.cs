using System.Text.Json.Nodes;
using A2A;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.AgentFramework;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Surfaces;
using Strathweb.A2UI.TestSupport;

namespace Strathweb.A2UI.IntegrationTests;

/// <summary>
/// The production shape: a model-backed agent behind the A2A host with a session store that
/// serializes the session after every turn.
/// </summary>
public class SessionStoreTests
{
    private static readonly A2UICatalog Basic = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

    [Fact]
    public async Task WithASessionStore_ASurfaceAndAnActionSurviveFourTurns()
    {
        var model = new ToolCallingChatClient("show_survey");
        var agent = model
            .AsAIAgent(
                instructions: "Show the survey when asked.",
                name: "survey-agent",
                tools: [AIFunctionFactory.Create(ShowSurvey, "show_survey")])
            .WithA2UI();

        await using var host = await A2AHost.StartAsync(
            agent,
            services => services.AddKeyedSingleton<AgentSessionStore>(agent.Name, new InMemoryAgentSessionStore()));

        var first = await host.Client.SendMessageAsync(Text("How did that go?"));
        Assert.Single(first.Message!.Parts, A2UIParts.IsA2UI);

        var second = await host.Client.SendMessageAsync(Text("Still there?"));
        Assert.NotNull(second.Message);

        var third = await host.Client.SendMessageAsync(Action());
        Assert.NotNull(third.Message);

        var fourth = await host.Client.SendMessageAsync(Text("Thanks!"));
        Assert.NotNull(fourth.Message);

        // The history the model saw on the last turn carries the action as a sentence, and the
        // surface itself is not in it.
        var history = model.Requests[^1].SelectMany(m => m.Contents).ToList();
        Assert.Contains(history.OfType<TextContent>(), c => c.Text.Contains("submit_satisfaction", StringComparison.Ordinal));
        Assert.DoesNotContain(history, c => c is A2UIContent);
    }

    private static string ShowSurvey()
    {
        var s = A2UISurface.Create("satisfaction_1", Basic);
        var ui = s.Components;

        A2UIEmitter.Emit(s
            .Root(ui.Card(ui.Column(
                ui.Text("How satisfied were you?").Variant(TextVariant.H3),
                ui.Button("Submit").Primary().OnClick(Act.Event(
                    "submit_satisfaction",
                    ("rating", Bind.Path("/rating")))))))
            .WithData(data => data["rating"] = null)
            .Build());

        return "A satisfaction survey is on the user's screen.";
    }

    private static SendMessageRequest Text(string text) => new()
    {
        Message = new Message
        {
            Role = Role.User,
            MessageId = Guid.NewGuid().ToString("N"),
            ContextId = "c1",
            Parts = [Part.FromText(text)],
        },
    };

    private static SendMessageRequest Action()
    {
        var action = new ActionMessage(
            "submit_satisfaction",
            "satisfaction_1",
            "button_1",
            DateTimeOffset.UtcNow,
            new JsonObject { ["rating"] = 5 });

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
