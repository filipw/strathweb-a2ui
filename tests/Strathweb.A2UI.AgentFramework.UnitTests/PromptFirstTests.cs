using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.TestSupport;
using Strathweb.A2UI.Validation;

namespace Strathweb.A2UI.AgentFramework.UnitTests;

/// <summary>
/// Prompt-first generation: the model writes the A2UI itself, and the agent turns the blocks into
/// surfaces, keeps the prose, and sends invalid blocks back for repair.
/// </summary>
public class PromptFirstTests
{
    private static readonly string CatalogId = A2UICatalogs.Basic(A2UIVersion.V0_9_1).CatalogId;

    private static readonly string ValidBlock = $$$"""
        <a2ui-json>
        [{"version":"v0.9.1","createSurface":{"surfaceId":"dash_1","catalogId":"{{{CatalogId}}}"}},
         {"version":"v0.9.1","updateComponents":{"surfaceId":"dash_1","components":[
           {"id":"root","component":"Card","child":"title"},
           {"id":"title","component":"Text","text":"Sales by region"}]}}]
        </a2ui-json>
        """;

    private static readonly string InvalidBlock = $$$"""
        <a2ui-json>
        [{"version":"v0.9.1","createSurface":{"surfaceId":"dash_1","catalogId":"{{{CatalogId}}}"}},
         {"version":"v0.9.1","updateComponents":{"surfaceId":"dash_1","components":[
           {"id":"root","component":"Txt","text":"Sales by region"}]}}]
        </a2ui-json>
        """;

    private static readonly string SloppyBlock = $$$"""
        <a2ui-json>
        ```json
        [{"version":"v0.9.1","createSurface":{"surfaceId":"dash_1","catalogId":"{{{CatalogId}}}",},},
         {"version":"v0.9.1","updateComponents":{"surfaceId":"dash_1","components":[
           {"id":"root","component":"Text","text":"Sales by region",},],},},]
        ```
        </a2ui-json>
        """;

    private static (A2UIAgent Agent, ScriptedTextChatClient Model) Build(
        Action<A2UIPromptFirstOptions>? configure = null,
        Action<A2UIAgentOptions>? configureAgent = null,
        params string[] replies)
    {
        var model = new ScriptedTextChatClient(replies);
        var agent = model
            .AsAIAgent(instructions: "You write A2UI.", name: "generator")
            .WithA2UI(o =>
            {
                o.PromptFirst = new A2UIPromptFirstOptions();
                configure?.Invoke(o.PromptFirst);
                configureAgent?.Invoke(o);
            });

        return (agent, model);
    }

    private static async Task<(string Text, List<A2UIContent> Surfaces)> StreamAsync(AIAgent agent, string input)
    {
        var text = new System.Text.StringBuilder();
        var surfaces = new List<A2UIContent>();

        await foreach (var update in agent.RunStreamingAsync(input))
        {
            foreach (var content in update.Contents)
            {
                switch (content)
                {
                    case TextContent t:
                        text.Append(t.Text);
                        break;
                    case A2UIContent s:
                        surfaces.Add(s);
                        break;
                }
            }
        }

        return (text.ToString(), surfaces);
    }

    [Fact]
    public async Task RunAsync_AValidBlock_BecomesASurfaceAndTheProseStays()
    {
        var (agent, _) = Build(replies: ["Here is the dashboard you asked for.\n" + ValidBlock + "\nLet me know if you want more."]);

        var response = await agent.RunAsync("Show me sales by region");

        var surface = Assert.Single(response.Messages.SelectMany(m => m.Contents).OfType<A2UIContent>());
        Assert.Equal("dash_1", surface.SurfaceId);
        Assert.DoesNotContain("<a2ui-json>", response.Text, StringComparison.Ordinal);
        Assert.Contains("Here is the dashboard you asked for.", response.Text, StringComparison.Ordinal);
        Assert.Contains("Let me know if you want more.", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunStreamingAsync_ABlockCutIntoTokens_BecomesASurface()
    {
        var (agent, _) = Build(replies: ["Here it is.\n" + ValidBlock + "\nAnything else?"]);

        var (text, surfaces) = await StreamAsync(agent, "Show me sales");

        var surface = Assert.Single(surfaces);
        Assert.Equal("dash_1", surface.SurfaceId);
        Assert.DoesNotContain("<a2ui", text, StringComparison.Ordinal);
        Assert.DoesNotContain("createSurface", text, StringComparison.Ordinal);
        Assert.Contains("Here it is.", text, StringComparison.Ordinal);
        Assert.Contains("Anything else?", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunStreamingAsync_TwoBlocks_YieldTwoSurfaces()
    {
        var second = ValidBlock.Replace("dash_1", "dash_2", StringComparison.Ordinal);
        var (agent, _) = Build(replies: ["First:\n" + ValidBlock + "\nSecond:\n" + second]);

        var (_, surfaces) = await StreamAsync(agent, "Two please");

        Assert.Equal(["dash_1", "dash_2"], surfaces.Select(s => s.SurfaceId));
    }

    [Fact]
    public async Task RunAsync_AnInvalidBlock_IsSentBackToTheModelWithTheErrors()
    {
        var (agent, model) = Build(replies: [InvalidBlock, ValidBlock]);

        var response = await agent.RunAsync("Show me sales");

        Assert.Single(response.Messages.SelectMany(m => m.Contents).OfType<A2UIContent>());
        Assert.Equal(2, model.Requests.Count);

        var repairRequest = model.Requests[1][^1];
        Assert.Equal(ChatRole.User, repairRequest.Role);
        Assert.Contains("Txt", repairRequest.Text, StringComparison.Ordinal);
        Assert.Contains("corrected <a2ui-json> block", repairRequest.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_TheRepairRoundsProse_IsNotShownToTheUser()
    {
        var (agent, _) = Build(replies: ["Here:\n" + InvalidBlock, "Sorry, here is the corrected version:\n" + ValidBlock]);

        var response = await agent.RunAsync("Show me sales");

        Assert.Single(response.Messages.SelectMany(m => m.Contents).OfType<A2UIContent>());
        Assert.DoesNotContain("Sorry", response.Text, StringComparison.Ordinal);
        Assert.Contains("Here:", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunStreamingAsync_AnInvalidBlock_IsRepairedAfterTheStreamEnds()
    {
        var (agent, model) = Build(replies: ["Working on it.\n" + InvalidBlock, ValidBlock]);

        var (text, surfaces) = await StreamAsync(agent, "Show me sales");

        Assert.Single(surfaces);
        Assert.Equal(2, model.Requests.Count);
        Assert.Contains("Working on it.", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_StillInvalidAfterTheRepair_IsDroppedAndLogged()
    {
        var logs = new CapturingLoggerFactory();
        var (agent, model) = Build(configureAgent: o => o.LoggerFactory = logs, replies: ["Here:\n" + InvalidBlock, InvalidBlock]);

        var response = await agent.RunAsync("Show me sales");

        Assert.Empty(response.Messages.SelectMany(m => m.Contents).OfType<A2UIContent>());
        Assert.Equal(2, model.Requests.Count);
        Assert.Contains("Here:", response.Text, StringComparison.Ordinal);
        Assert.Contains(logs.Entries, e => e.Message.Contains("dropped", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunAsync_StillInvalidWithTheThrowPolicy_Throws()
    {
        var (agent, _) = Build(o => o.OnInvalid = A2UIInvalidPayloadPolicy.Throw, replies: [InvalidBlock, InvalidBlock]);

        var thrown = await Assert.ThrowsAsync<A2UIValidationException>(() => agent.RunAsync("Show me sales"));

        Assert.Contains("Txt", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_NoModelRepairs_DropsAfterTheFirstTry()
    {
        var (agent, model) = Build(o => o.MaxModelRepairs = 0, replies: [InvalidBlock, ValidBlock]);

        var response = await agent.RunAsync("Show me sales");

        Assert.Empty(response.Messages.SelectMany(m => m.Contents).OfType<A2UIContent>());
        Assert.Single(model.Requests);
    }

    [Fact]
    public async Task RunAsync_SloppyJson_IsRepairedWithoutAModelRoundTrip()
    {
        var (agent, model) = Build(replies: [SloppyBlock]);

        var response = await agent.RunAsync("Show me sales");

        Assert.Single(response.Messages.SelectMany(m => m.Contents).OfType<A2UIContent>());
        Assert.Single(model.Requests);
    }

    [Fact]
    public async Task RunAsync_WithPayloadRepairOff_SloppyJsonGoesBackToTheModel()
    {
        var (agent, model) = Build(o => o.RepairPayloads = false, replies: [SloppyBlock, ValidBlock]);

        var response = await agent.RunAsync("Show me sales");

        Assert.Single(response.Messages.SelectMany(m => m.Contents).OfType<A2UIContent>());
        Assert.Equal(2, model.Requests.Count);
    }

    [Fact]
    public async Task RunAsync_AReplyWithoutBlocks_IsUntouched()
    {
        var (agent, _) = Build(replies: ["Just words."]);

        var response = await agent.RunAsync("Hi");

        Assert.Equal("Just words.", response.Text);
        Assert.Empty(response.Messages.SelectMany(m => m.Contents).OfType<A2UIContent>());
    }

    [Fact]
    public async Task RunAsync_AnUpdateToALiveSurface_PassesValidation()
    {
        var update = """
            <a2ui-json>
            [{"version":"v0.9.1","updateDataModel":{"surfaceId":"dash_1","path":"/total","value":42}}]
            </a2ui-json>
            """;
        var (agent, _) = Build(replies: [update]);

        var response = await agent.RunAsync("Bump the total");

        Assert.Single(response.Messages.SelectMany(m => m.Contents).OfType<A2UIContent>());
    }

    [Fact]
    public async Task RunStreamingAsync_AnUnclosedBlock_IsTreatedAsInvalidAndRepaired()
    {
        var (agent, model) = Build(replies: ["<a2ui-json>[{\"version\":\"v0.9.1\",\"createSurface\":{\"surfaceId\":\"dash_1\"", ValidBlock]);

        var (_, surfaces) = await StreamAsync(agent, "Show me sales");

        Assert.Single(surfaces);
        Assert.Equal(2, model.Requests.Count);
    }

    [Fact]
    public async Task RunAsync_GeneratedSurfaces_AreRecordedInTheSession()
    {
        var (agent, _) = Build(replies: [ValidBlock]);
        var session = await agent.CreateSessionAsync();

        await agent.RunAsync("Show me sales", session);

        Assert.True(session.GetA2UISurfaceRegistry().Contains("dash_1"));
        await agent.SerializeSessionAsync(session);
    }

    [Fact]
    public async Task RunAsync_ToolSurfacesAndGeneratedSurfaces_BothArrive()
    {
        var model = new ScriptedTextChatClient(ValidBlock);
        var inner = new ScriptedAgent(() => A2UIEmitter.Emit(A2UISurfaceFixtures.Survey("survey_1")), ValidBlock);
        var agent = inner.WithA2UI(o => o.PromptFirst = new A2UIPromptFirstOptions());

        var response = await agent.RunAsync("Both");

        Assert.Equal(
            ["survey_1", "dash_1"],
            response.Messages.SelectMany(m => m.Contents).OfType<A2UIContent>().Select(c => c.SurfaceId));
        GC.KeepAlive(model);
    }
}
