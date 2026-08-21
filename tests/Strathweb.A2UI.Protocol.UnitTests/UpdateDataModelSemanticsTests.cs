using System.Text.Json.Nodes;
using Strathweb.A2UI.Messages;

namespace Strathweb.A2UI.Protocol.UnitTests;

/// <summary>
/// The presence of <c>value</c>, not its nullness, separates a write from a delete. Collapsing the
/// two would silently turn every explicit null into a delete, so it gets its own tests.
/// </summary>
public class UpdateDataModelSemanticsTests
{
    [Fact]
    public void Remove_OmitsValueEntirely()
    {
        var json = A2UIJson.ToJsonObject(UpdateDataModelMessage.Remove("s1", "/rating"))["updateDataModel"]!.AsObject();

        Assert.False(json.ContainsKey("value"));
        Assert.Equal("/rating", (string?)json["path"]);
    }

    [Fact]
    public void Set_WithNull_WritesAnExplicitJsonNull()
    {
        var json = A2UIJson.ToJsonObject(UpdateDataModelMessage.Set("s1", "/rating", null))["updateDataModel"]!
            .AsObject();

        Assert.True(json.ContainsKey("value"));
        Assert.Null(json["value"]);
    }

    [Fact]
    public void Deserialize_OmittedValue_IsADelete()
    {
        var message = (UpdateDataModelMessage)A2UIJson.Deserialize(
            """{"version":"v0.9.1","updateDataModel":{"surfaceId":"s1","path":"/rating"}}""");

        Assert.False(message.HasValue);
    }

    [Fact]
    public void Deserialize_ExplicitNullValue_IsAWrite()
    {
        var message = (UpdateDataModelMessage)A2UIJson.Deserialize(
            """{"version":"v0.9.1","updateDataModel":{"surfaceId":"s1","path":"/rating","value":null}}""");

        Assert.True(message.HasValue);
        Assert.Null(message.Value);
    }

    [Fact]
    public void DeleteAndExplicitNull_DoNotSerializeToTheSameJson()
    {
        Assert.NotEqual(
            A2UIJson.Serialize(UpdateDataModelMessage.Remove("s1", "/rating")),
            A2UIJson.Serialize(UpdateDataModelMessage.Set("s1", "/rating", null)));
    }

    [Fact]
    public void Replace_TargetsTheWholeModel()
    {
        var message = UpdateDataModelMessage.Replace("s1", new JsonObject { ["rating"] = 5 });

        Assert.Equal("/", message.Path);
        Assert.True(message.HasValue);
    }
}
