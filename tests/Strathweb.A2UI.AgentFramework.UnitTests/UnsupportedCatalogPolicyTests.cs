using System.Text.Json;
using A2A;
using Microsoft.Extensions.AI;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Surfaces;
using Strathweb.A2UI.TestSupport;

namespace Strathweb.A2UI.AgentFramework.UnitTests;

/// <summary>
/// A renderer that does not know a catalog draws nothing and reports nothing, so the emit call is
/// where the mismatch has to surface.
/// </summary>
public class UnsupportedCatalogPolicyTests
{
    private static readonly A2UICatalog Basic = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

    private static A2UISurface Survey()
    {
        var s = A2UISurface.Create("survey_1", Basic);
        var ui = s.Components;
        return s.Root(ui.Card(ui.Text("How did we do?"))).Build();
    }

    /// <summary>A turn from a renderer that advertised the given catalogs, the way A2A delivers it.</summary>
    private static ChatMessage TurnFrom(params string[] supportedCatalogIds)
    {
        var a2a = new Message
        {
            Role = Role.User,
            Parts = [Part.FromText("hi")],
            Metadata = new Dictionary<string, JsonElement>(StringComparer.Ordinal),
        };

        A2UIMetadata.WriteCapabilities(a2a.Metadata, new A2UIRendererCapabilities(supportedCatalogIds), A2UIVersionProfile.V0_9_1);
        return a2a.ToChatMessage();
    }

    [Fact]
    public async Task Drop_ASurfaceFromACatalogTheRendererDidNotList_IsNotSent()
    {
        var agent = new ScriptedAgent(() => A2UIEmitter.Emit(Survey()))
            .WithA2UI(o => o.UnsupportedCatalogPolicy = A2UIUnsupportedCatalogPolicy.Drop);

        var response = await agent.RunAsync([TurnFrom("https://example.com/other-catalog")]);

        Assert.Empty(response.Messages.SelectMany(m => m.Contents).OfType<A2UIContent>());
    }

    [Fact]
    public async Task Throw_TheEmitCallThrows_SoTheToolCanFallBack()
    {
        InvalidOperationException? thrown = null;
        var agent = new ScriptedAgent(() =>
                thrown = Assert.Throws<InvalidOperationException>(() => A2UIEmitter.Emit(Survey())))
            .WithA2UI(o => o.UnsupportedCatalogPolicy = A2UIUnsupportedCatalogPolicy.Throw);

        await agent.RunAsync([TurnFrom("https://example.com/other-catalog")]);

        Assert.NotNull(thrown);
        Assert.Contains(Basic.CatalogId, thrown.Message, StringComparison.Ordinal);
        Assert.Contains("https://example.com/other-catalog", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Warn_TheSurfaceIsSentAndAWarningIsLogged()
    {
        var logs = new CapturingLoggerFactory();
        var agent = new ScriptedAgent(() => A2UIEmitter.Emit(Survey()))
            .WithA2UI(o => o.LoggerFactory = logs);

        var response = await agent.RunAsync([TurnFrom("https://example.com/other-catalog")]);

        Assert.Single(response.Messages.SelectMany(m => m.Contents).OfType<A2UIContent>());
        var warning = Assert.Single(logs.Entries, e => e.Level == LogLevel.Warning);
        Assert.Contains(Basic.CatalogId, warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ARendererThatSaidNothing_IsNotChecked()
    {
        var agent = new ScriptedAgent(() => A2UIEmitter.Emit(Survey()))
            .WithA2UI(o => o.UnsupportedCatalogPolicy = A2UIUnsupportedCatalogPolicy.Throw);

        var response = await agent.RunAsync("hi");

        Assert.Single(response.Messages.SelectMany(m => m.Contents).OfType<A2UIContent>());
    }

    [Fact]
    public async Task AListedCatalog_PassesUnderTheStrictestPolicy()
    {
        var agent = new ScriptedAgent(() => A2UIEmitter.Emit(Survey()))
            .WithA2UI(o => o.UnsupportedCatalogPolicy = A2UIUnsupportedCatalogPolicy.Throw);

        var response = await agent.RunAsync([TurnFrom("https://example.com/other-catalog", Basic.CatalogId)]);

        Assert.Single(response.Messages.SelectMany(m => m.Contents).OfType<A2UIContent>());
    }

    [Fact]
    public async Task RunAsync_WhatWasSentAndReceived_IsLogged()
    {
        var logs = new CapturingLoggerFactory();
        var agent = new ScriptedAgent(() => A2UIEmitter.Emit(Survey())).WithA2UI(o => o.LoggerFactory = logs);

        await agent.RunAsync("hi");

        Assert.Contains(logs.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("survey_1", StringComparison.Ordinal));
        Assert.All(logs.Entries, e => Assert.Equal("Strathweb.A2UI.AgentFramework.A2UIAgent", e.Category));
    }
}
