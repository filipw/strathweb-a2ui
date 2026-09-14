using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Surfaces;

namespace Strathweb.A2UI.AgentFramework.UnitTests;

public class A2UIAgentTests
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

    [Fact]
    public async Task RunAsync_AttachesTheEmittedSurfaceToTheAssistantMessage()
    {
        var agent = new ScriptedAgent(() => A2UIEmitter.Emit(Survey())).WithA2UI();

        var response = await agent.RunAsync("hello");

        var content = Assert.Single(response.Messages.SelectMany(m => m.Contents).OfType<A2UIContent>());
        Assert.Equal("survey_1", content.SurfaceId);
    }

    [Fact]
    public async Task RunAsync_TheAttachedContentAlreadyCarriesItsWirePart()
    {
        // If the part were built later the hosting layer would drop it, so this is checked at the
        // boundary rather than trusted.
        var agent = new ScriptedAgent(() => A2UIEmitter.Emit(Survey())).WithA2UI();

        var response = await agent.RunAsync("hello");
        var content = response.Messages.SelectMany(m => m.Contents).OfType<A2UIContent>().Single();

        Assert.True(A2UIParts.IsA2UI(content.ToPart()));
    }

    [Fact]
    public async Task RunAsync_KeepsTheModelsOwnReply()
    {
        var agent = new ScriptedAgent(() => A2UIEmitter.Emit(Survey()), "A survey has been shown.").WithA2UI();

        var response = await agent.RunAsync("hello");

        Assert.Contains("A survey has been shown.", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_WithNothingEmitted_ChangesNothing()
    {
        var agent = new ScriptedAgent(() => { }).WithA2UI();

        var response = await agent.RunAsync("hello");

        Assert.Empty(response.Messages.SelectMany(m => m.Contents).OfType<A2UIContent>());
    }

    [Fact]
    public async Task RunStreamingAsync_YieldsTheSurfaceAsItsOwnUpdate()
    {
        // A2A hosting takes a different path for streaming, so both are covered.
        var agent = new ScriptedAgent(() => A2UIEmitter.Emit(Survey())).WithA2UI();

        var updates = new List<AgentResponseUpdate>();
        await foreach (var update in agent.RunStreamingAsync("hello"))
        {
            updates.Add(update);
        }

        var content = Assert.Single(updates.SelectMany(u => u.Contents).OfType<A2UIContent>());
        Assert.Equal("survey_1", content.SurfaceId);
    }

    [Fact]
    public async Task RunStreamingAsync_ASurfaceEmittedAfterTheFirstUpdate_StillArrives()
    {
        // An AsyncLocal set inside an async iterator does not survive a yield. The scope has to be
        // re-entered for every step, or the tool runs outside it.
        var agent = new ScriptedAgent(() => A2UIEmitter.Emit(Survey()), runAfterFirstUpdate: true).WithA2UI();

        var updates = new List<AgentResponseUpdate>();
        await foreach (var update in agent.RunStreamingAsync("hello"))
        {
            updates.Add(update);
        }

        Assert.Single(updates.SelectMany(u => u.Contents).OfType<A2UIContent>());
    }

    [Fact]
    public async Task RunStreamingAsync_TheRunContextIsStillThereAfterTheFirstUpdate()
    {
        var agent = new ScriptedAgent(
            () => Assert.NotNull(A2UIRunContext.Current),
            runAfterFirstUpdate: true).WithA2UI();

        await foreach (var _ in agent.RunStreamingAsync("hello"))
        {
        }
    }

    [Fact]
    public async Task RunAsync_SeveralSurfaces_ArriveInTheOrderTheyWereEmitted()
    {
        var agent = new ScriptedAgent(() =>
        {
            A2UIEmitter.Emit(Survey("first"));
            A2UIEmitter.Emit(Survey("second"));
        }).WithA2UI();

        var response = await agent.RunAsync("hello");

        Assert.Equal(
            ["first", "second"],
            response.Messages.SelectMany(m => m.Contents).OfType<A2UIContent>().Select(c => c.SurfaceId));
    }

    [Fact]
    public async Task RunAsync_RecordsTheSurfaceInTheSession()
    {
        var agent = new ScriptedAgent(() => A2UIEmitter.Emit(Survey())).WithA2UI();
        var session = await agent.CreateSessionAsync();

        await agent.RunAsync("hello", session);

        var registry = session.GetA2UISurfaceRegistry();
        Assert.True(registry.Contains("survey_1"));
        Assert.Equal("survey_1", registry.MostRecent!.SurfaceId);
    }

    [Fact]
    public async Task RunAsync_DeletingASurface_StopsTrackingIt()
    {
        var agent = new ScriptedAgent(() => A2UIEmitter.Emit(Survey())).WithA2UI();
        var session = await agent.CreateSessionAsync();
        await agent.RunAsync("hello", session);

        var deleter = new ScriptedAgent(() =>
            A2UIEmitter.Emit([new DeleteSurfaceMessage("survey_1")])).WithA2UI();
        await deleter.RunAsync("bye", session);

        Assert.False(session.GetA2UISurfaceRegistry().Contains("survey_1"));
    }

    [Fact]
    public void Emit_OutsideARun_SaysWhatWentWrong()
    {
        // A NullReferenceException here would send someone hunting in the wrong place.
        var thrown = Assert.Throws<InvalidOperationException>(() => A2UIEmitter.Emit(Survey()));

        Assert.Contains("WithA2UI", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Emit_TheScopeIsClosedWhenTheRunEnds()
    {
        var agent = new ScriptedAgent(() => Assert.True(A2UIEmitter.IsInRun)).WithA2UI();

        await agent.RunAsync("hello");

        Assert.False(A2UIEmitter.IsInRun);
    }

    [Fact]
    public async Task Emit_TheScopeIsClosedEvenWhenTheRunThrows()
    {
        var agent = new ScriptedAgent(() => throw new InvalidOperationException("boom")).WithA2UI();

        await Assert.ThrowsAsync<InvalidOperationException>(() => agent.RunAsync("hello"));

        Assert.False(A2UIEmitter.IsInRun);
    }

    [Fact]
    public async Task Emit_ConcurrentRunsDoNotSeeEachOthersSurfaces()
    {
        var first = new ScriptedAgent(() => A2UIEmitter.Emit(Survey("first"))).WithA2UI();
        var second = new ScriptedAgent(() => A2UIEmitter.Emit(Survey("second"))).WithA2UI();

        var responses = await Task.WhenAll(first.RunAsync("a"), second.RunAsync("b"));

        Assert.Equal(
            ["first"],
            responses[0].Messages.SelectMany(m => m.Contents).OfType<A2UIContent>().Select(c => c.SurfaceId));
        Assert.Equal(
            ["second"],
            responses[1].Messages.SelectMany(m => m.Contents).OfType<A2UIContent>().Select(c => c.SurfaceId));
    }

    [Fact]
    public async Task SurfaceRegistry_SurvivesSessionSerialization()
    {
        var agent = new ScriptedAgent(() => A2UIEmitter.Emit(Survey())).WithA2UI();
        var session = await agent.CreateSessionAsync();
        await agent.RunAsync("hello", session);

        var serialized = await agent.SerializeSessionAsync(session);
        var restored = await agent.DeserializeSessionAsync(serialized);

        Assert.True(restored.GetA2UISurfaceRegistry().Contains("survey_1"));
    }

    [Fact]
    public void SurfaceRegistry_ForgetsTheOldestOnceItIsFull()
    {
        // Unbounded session state is a real leak in a long conversation.
        var registry = new A2UISurfaceRegistry { Capacity = 2 };

        registry.Add("a", "c", DateTimeOffset.UnixEpoch);
        registry.Add("b", "c", DateTimeOffset.UnixEpoch);
        registry.Add("c", "c", DateTimeOffset.UnixEpoch);

        Assert.False(registry.Contains("a"));
        Assert.True(registry.Contains("b"));
        Assert.True(registry.Contains("c"));
    }

    [Fact]
    public void SurfaceId_IsNeverReused()
    {
        var ids = Enumerable.Range(0, 1000).Select(_ => A2UISurfaceId.New("survey")).ToList();

        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.All(ids, id => Assert.StartsWith("survey_", id, StringComparison.Ordinal));
    }
}
