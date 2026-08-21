using System.Text.Json.Nodes;
using Strathweb.A2UI.Components;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Serialization;
using Strathweb.A2UI.TestSupport;
using Strathweb.A2UI.Values;
namespace Strathweb.A2UI.Protocol.UnitTests;

public class MessageRoundTripTests
{
    public static TheoryData<string, A2UIMessage> AgentToRendererMessages() => new()
    {
        { "createSurface", Messages.CreateSurface() },
        { "createSurface with theme and data model", Messages.CreateSurfaceWithOptions() },
        { "updateComponents", Messages.UpdateComponents() },
        { "updateDataModel set", Messages.UpdateDataModelSet() },
        { "updateDataModel replace", UpdateDataModelMessage.Replace("s1", new JsonObject { ["a"] = 1 }) },
        { "updateDataModel remove", UpdateDataModelMessage.Remove("s1", "/rating") },
        { "deleteSurface", new DeleteSurfaceMessage("s1") },
    };

    public static TheoryData<string, A2UIMessage> RendererToAgentMessages() => new()
    {
        { "action", Messages.Action() },
        { "error", new ErrorMessage("RENDER_FAILED", "s1", "The surface could not be rendered.") },
        {
            "validation error",
            new ErrorMessage(ErrorMessage.ValidationFailedCode, "s1", "Unknown component.")
            {
                Path = "/components/0/component",
            }
        },
    };

    [Theory]
    [MemberData(nameof(AgentToRendererMessages))]
    [MemberData(nameof(RendererToAgentMessages))]
    public void Serialize_ThenDeserialize_ProducesIdenticalJson(string description, A2UIMessage message)
    {
        var json = A2UIJson.Serialize(message);
        var round = A2UIJson.Serialize(A2UIJson.Deserialize(json));

        Assert.Equal(json, round);
        Assert.NotNull(description);
    }

    [Theory]
    [MemberData(nameof(AgentToRendererMessages))]
    public void AgentToRendererMessage_MatchesVendoredSchema(string description, A2UIMessage message)
    {
        SpecSchemas.AssertValid(SpecSchemas.AgentToRenderer, A2UIJson.ToJsonObject(message));
        Assert.NotNull(description);
    }

    [Theory]
    [MemberData(nameof(RendererToAgentMessages))]
    public void RendererToAgentMessage_MatchesVendoredSchema(string description, A2UIMessage message)
    {
        SpecSchemas.AssertValid(SpecSchemas.RendererToAgent, A2UIJson.ToJsonObject(message));
        Assert.NotNull(description);
    }

    [Fact]
    public void Serialize_EmitsVersionAndExactlyOneMessageKey()
    {
        var json = JsonNode.Parse(A2UIJson.Serialize(new DeleteSurfaceMessage("s1")))!.AsObject();

        Assert.Equal(2, json.Count);
        Assert.Equal("v0.9.1", (string?)json["version"]);
        Assert.True(json.ContainsKey("deleteSurface"));
    }

    [Fact]
    public void Serialize_WritesTheDeclaredVersion_NotAlwaysTheLatest()
    {
        var json = JsonNode.Parse(
            A2UIJson.Serialize(new DeleteSurfaceMessage("s1") { Version = A2UIVersion.V0_9 }))!.AsObject();

        Assert.Equal("v0.9", (string?)json["version"]);
    }

    [Fact]
    public void SerializeList_WritesAnArray_EvenForASingleMessage()
    {
        var json = A2UIJson.Serialize([new DeleteSurfaceMessage("s1")]);

        Assert.StartsWith("[", json, StringComparison.Ordinal);
        Assert.Single(A2UIJson.DeserializeList(json));
    }

    [Fact]
    public void Deserialize_MessageWithoutVersion_Throws()
    {
        Assert.Throws<A2UIParseException>(
            () => A2UIJson.Deserialize("""{"deleteSurface":{"surfaceId":"s1"}}"""));
    }

    [Fact]
    public void Deserialize_MessageWithTwoMessageKeys_Throws()
    {
        Assert.Throws<A2UIParseException>(() => A2UIJson.Deserialize(
            """{"version":"v0.9.1","deleteSurface":{"surfaceId":"s1"},"createSurface":{"surfaceId":"s2"}}"""));
    }

    [Fact]
    public void Deserialize_MessageWithNoMessageKey_Throws()
    {
        Assert.Throws<A2UIParseException>(() => A2UIJson.Deserialize("""{"version":"v0.9.1"}"""));
    }

    [Fact]
    public void Deserialize_UnknownVersion_Throws()
    {
        Assert.Throws<A2UIParseException>(() => A2UIJson.Deserialize(
            """{"version":"v0.8","deleteSurface":{"surfaceId":"s1"}}"""));
    }

    internal static class Messages
    {
        internal const string CatalogId = "https://a2ui.org/specification/v0_9/catalogs/basic/catalog.json";

        internal static CreateSurfaceMessage CreateSurface() => new("s1", CatalogId);

        internal static CreateSurfaceMessage CreateSurfaceWithOptions() => new("s1", CatalogId)
        {
            Theme = new JsonObject { ["primaryColor"] = "#336699" },
            SendDataModel = true,
        };

        internal static UpdateComponentsMessage UpdateComponents() => new(
            "s1",
            [
                new A2UIComponent("root", "Column")
                    .Set("children", ChildList.Of("title", "submit").ToJson()),
                new A2UIComponent("title", "Text")
                    .Set("text", DynamicValue.FromString("How satisfied were you?"))
                    .Set("variant", "h3"),
                new A2UIComponent("submit", "Button")
                {
                    Accessibility = new A2UIAccessibility { Label = DynamicValue.FromString("Submit") },
                }
                    .Set("child", "submit_label")
                    .Set("action", A2UIAction.FromEvent(new A2UIEvent("submit_satisfaction")
                    {
                        Context = new Dictionary<string, DynamicValue>
                        {
                            ["rating"] = DynamicValue.FromPath("/rating"),
                        },
                    }).ToJson()),
                new A2UIComponent("submit_label", "Text").Set("text", DynamicValue.FromString("Submit")),
            ]);

        internal static UpdateDataModelMessage UpdateDataModelSet() =>
            UpdateDataModelMessage.Set("s1", "/rating", JsonValue.Create(5));

        internal static ActionMessage Action() => new(
            "submit_satisfaction",
            "s1",
            "submit",
            new DateTimeOffset(2026, 8, 21, 9, 30, 0, TimeSpan.Zero),
            new JsonObject { ["rating"] = 5, ["comment"] = "fast fix" });
    }
}
