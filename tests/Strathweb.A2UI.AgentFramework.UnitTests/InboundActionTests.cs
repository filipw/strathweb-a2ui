using System.Text.Json;
using System.Text.Json.Nodes;
using A2A;
using Microsoft.Extensions.AI;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.Messages;

namespace Strathweb.A2UI.AgentFramework.UnitTests;

public class InboundActionTests
{
    private static readonly A2UIVersionProfile Profile = A2UIVersionProfile.V0_9_1;

    private static ActionMessage Action(string name = "submit_satisfaction", string surfaceId = "survey_1") =>
        new(
            name,
            surfaceId,
            "button_1",
            new DateTimeOffset(2026, 8, 21, 9, 30, 0, TimeSpan.Zero),
            new JsonObject { ["rating"] = 5, ["comment"] = "fast fix" });

    /// <summary>Builds the message a renderer would post back, the way A2A delivers it.</summary>
    private static ChatMessage Inbound(params A2UIMessage[] messages)
    {
        var a2a = new Message
        {
            Role = Role.User,
            Parts = [A2UIParts.Create(messages)],
        };

        return a2a.ToChatMessage();
    }

    [Fact]
    public void Normalize_AnAction_BecomesStructuredContentAndASentence()
    {
        var result = new A2UIInboundNormalizer().Normalize([Inbound(Action())]);

        var contents = result.Messages.Single().Contents;
        Assert.Contains(contents, c => c is A2UIActionContent);
        Assert.Contains(contents, c => c is TextContent);
    }

    [Fact]
    public void Normalize_TheRawJsonBlobIsGone()
    {
        // A JSON payload left in the conversation makes the model answer in JSON.
        var result = new A2UIInboundNormalizer().Normalize([Inbound(Action())]);

        Assert.DoesNotContain(
            result.Messages.Single().Contents,
            c => c is DataContent d && A2UIParts.IsA2UI(d.RawRepresentation as Part));
    }

    [Fact]
    public void Describe_NamesTheActionTheSurfaceAndEveryValue()
    {
        var text = A2UIInboundNormalizer.Describe(Action());

        Assert.Contains("submit_satisfaction", text, StringComparison.Ordinal);
        Assert.Contains("survey_1", text, StringComparison.Ordinal);
        Assert.Contains("rating=5", text, StringComparison.Ordinal);
        Assert.Contains("comment=\"fast fix\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Describe_AnActionWithNoContext_IsStillReadable()
    {
        var action = new ActionMessage("dismiss", "survey_1", "button_1", DateTimeOffset.UnixEpoch, []);

        Assert.Equal(
            "The user performed the \"dismiss\" action on surface survey_1.",
            A2UIInboundNormalizer.Describe(action));
    }

    [Fact]
    public void Normalize_TheParsedActionIsAvailableToCode()
    {
        var result = new A2UIInboundNormalizer().Normalize([Inbound(Action())]);

        var action = Assert.Single(result.Actions);
        Assert.Equal("submit_satisfaction", action.Name);
        Assert.Equal(5, (int)action.Context["rating"]!);
    }

    [Fact]
    public void Normalize_ARendererError_IsExplainedToTheModel()
    {
        var error = new ErrorMessage("RENDER_FAILED", "survey_1", "The catalog is not supported.");

        var result = new A2UIInboundNormalizer().Normalize([Inbound(error)]);

        Assert.Single(result.Errors);
        Assert.Contains(
            result.Messages.Single().Contents.OfType<TextContent>(),
            c => c.Text.Contains("could not display", StringComparison.Ordinal));
    }

    [Fact]
    public void Normalize_AMalformedMessageInAList_DoesNotLoseTheGoodOnes()
    {
        var payload = "[" + A2UIJson.Serialize(Action()) + ",{\"nonsense\":true}]";
        var part = Part.FromData(JsonDocument.Parse(payload).RootElement.Clone());
        part.Metadata = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            [A2UIParts.MimeTypeMetadataKey] = JsonDocument.Parse("\"application/a2ui+json\"").RootElement.Clone(),
        };

        var message = new Message { Role = Role.User, Parts = [part] }.ToChatMessage();

        var result = new A2UIInboundNormalizer().Normalize([message]);

        Assert.Single(result.Actions);
        Assert.Contains(
            result.Messages.Single().Contents.OfType<TextContent>(),
            c => c.Text.Contains("could not be read", StringComparison.Ordinal));
    }

    [Fact]
    public void Normalize_ReadsRendererCapabilitiesFromMessageMetadata()
    {
        var a2a = new Message
        {
            Role = Role.User,
            Parts = [Part.FromText("hi")],
            Metadata = new Dictionary<string, JsonElement>(StringComparer.Ordinal),
        };

        A2UIMetadata.WriteCapabilities(a2a.Metadata, new A2UIRendererCapabilities(["std"]), Profile);

        var result = new A2UIInboundNormalizer().Normalize([a2a.ToChatMessage()]);

        Assert.Equal(["std"], result.RendererCapabilities!.SupportedCatalogIds);
    }

    [Fact]
    public void Normalize_ADataModelForASurfaceThisSessionCreated_IsKept()
    {
        var registry = new A2UISurfaceRegistry();
        registry.Add("survey_1", "std", DateTimeOffset.UnixEpoch);

        var result = new A2UIInboundNormalizer().Normalize([WithDataModel("survey_1")], registry);

        Assert.True(result.SurfaceData.ContainsKey("survey_1"));
    }

    [Fact]
    public void Normalize_ADataModelForSomebodyElsesSurface_IsDropped()
    {
        // A surface's data model belongs to the agent that created it. This one did not.
        var result = new A2UIInboundNormalizer().Normalize(
            [WithDataModel("someone_elses_surface")],
            new A2UISurfaceRegistry());

        Assert.Empty(result.SurfaceData);
    }

    [Fact]
    public void Normalize_AMessageWithNoA2UI_IsPassedThroughUnchanged()
    {
        var message = new ChatMessage(ChatRole.User, "just talking");

        var result = new A2UIInboundNormalizer().Normalize([message]);

        Assert.Same(message, result.Messages.Single());
        Assert.True(result.IsEmpty);
    }

    private static ChatMessage WithDataModel(string surfaceId)
    {
        var a2a = new Message
        {
            Role = Role.User,
            Parts = [Part.FromText("hi")],
            Metadata = new Dictionary<string, JsonElement>(StringComparer.Ordinal),
        };

        A2UIMetadata.WriteDataModel(
            a2a.Metadata,
            new A2UIRendererDataModel(
                A2UIVersion.V0_9_1,
                new Dictionary<string, JsonNode?> { [surfaceId] = new JsonObject { ["rating"] = 5 } }),
            Profile);

        return a2a.ToChatMessage();
    }
}
