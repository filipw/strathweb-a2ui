using System.Text.Json;
using Microsoft.Extensions.AI;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Surfaces;

namespace Strathweb.A2UI.AgentFramework.UnitTests;

public class A2UIContentSerializationTests
{
    private static readonly A2UICatalog Basic = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

    private static A2UISurface Survey()
    {
        var s = A2UISurface.Create("survey_1", Basic);
        var ui = s.Components;
        return s.Root(ui.Card(ui.Column(
            ui.Text("How did we do?").Variant(TextVariant.H3),
            ui.Button("Submit").Primary().OnClick(Act.Event("submit"))))).Build();
    }

    private static JsonSerializerOptions Registered()
    {
        var options = new JsonSerializerOptions(AIJsonUtilities.DefaultOptions);
        A2UIAgentJson.AddA2UIContentType(options);
        return options;
    }

    [Fact]
    public void Serialize_WithTheTypeRegistered_RoundTripsAsAIContent()
    {
        var options = Registered();
        var content = new A2UIContent(Survey());

        var json = JsonSerializer.Serialize<AIContent>(content, options);
        var restored = Assert.IsType<A2UIContent>(JsonSerializer.Deserialize<AIContent>(json, options));

        Assert.Equal("survey_1", restored.SurfaceId);
        Assert.Equal(content.Messages.Count, restored.Messages.Count);
        Assert.IsType<CreateSurfaceMessage>(restored.Messages[0]);
        Assert.True(A2UIParts.IsA2UI(restored.ToPart()));
    }

    [Fact]
    public void Serialize_AChatMessageCarryingTheContent_RoundTrips()
    {
        var options = Registered();
        var message = new ChatMessage(ChatRole.Assistant, [new TextContent("Here you go."), new A2UIContent(Survey())]);

        var restored = JsonSerializer.Deserialize<ChatMessage>(JsonSerializer.Serialize(message, options), options)!;

        Assert.Equal(2, restored.Contents.Count);
        Assert.Single(restored.Contents.OfType<A2UIContent>());
    }

    [Fact]
    public void Serialize_WithoutRegistration_FailsLoudly()
    {
        // The framework's defaults cannot be extended, which is why the agent keeps this content out of
        // the history it stores. This pins the behaviour the design works around.
        var content = new A2UIContent(Survey());

        Assert.Throws<NotSupportedException>(() =>
            JsonSerializer.Serialize<AIContent>(content, AIJsonUtilities.DefaultOptions));
    }

    [Fact]
    public void Constructor_APayloadThatIsNotAnArray_IsRejected()
    {
        var payload = JsonDocument.Parse("""{"version":"v0.9.1","deleteSurface":{"surfaceId":"s1"}}""").RootElement;

        Assert.Throws<ArgumentException>(() => new A2UIContent(payload));
    }
}
