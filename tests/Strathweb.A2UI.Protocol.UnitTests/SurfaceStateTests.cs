using System.Text.Json.Nodes;
using Strathweb.A2UI.Components;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.State;

using Strathweb.A2UI.TestSupport;
namespace Strathweb.A2UI.Protocol.UnitTests;

public class SurfaceStateTests
{
    [Fact]
    public void Apply_BuildsTheComponentMapAcrossSeveralMessages()
    {
        var state = new A2UISurfaceState("s1");

        state.Apply(new A2UIMessage[]
        {
            new CreateSurfaceMessage("s1", "c"),
            new UpdateComponentsMessage("s1", [new A2UIComponent("root", "Card").Set("child", "a")]),
            new UpdateComponentsMessage("s1", [new A2UIComponent("a", "Text").Set("text", "hi")]),
        });

        Assert.Equal(["a", "root"], state.Components.Keys.Order(StringComparer.Ordinal));
        Assert.Equal("Card", state.Root!.Component);
    }

    [Fact]
    public void Apply_UpdateComponents_ReplacesAComponentWithTheSameId()
    {
        var state = new A2UISurfaceState("s1");

        state.Apply(new UpdateComponentsMessage("s1", [new A2UIComponent("a", "Text").Set("text", "one")]));
        state.Apply(new UpdateComponentsMessage("s1", [new A2UIComponent("a", "Text").Set("text", "two")]));

        Assert.Equal("two", (string?)state.Components["a"].Properties["text"]);
    }

    [Fact]
    public void Apply_SetAtAPath_CreatesMissingLevels()
    {
        var state = new A2UISurfaceState("s1");

        state.Apply(UpdateDataModelMessage.Set("s1", "/user/name", JsonValue.Create("Ada")));

        Assert.Equal("Ada", (string?)state.GetData("/user/name"));
    }

    [Fact]
    public void Apply_SetAtAnArrayIndexPastTheEnd_AppendsToTheList()
    {
        // The spec's own incremental example writes /restaurants/3 into a three-item list.
        var state = new A2UISurfaceState("s1");
        state.Apply(UpdateDataModelMessage.Replace("s1", new JsonObject
        {
            ["items"] = new JsonArray { "a", "b", "c" },
        }));

        state.Apply(UpdateDataModelMessage.Set("s1", "/items/3", JsonValue.Create("d")));

        Assert.Equal(4, state.GetData("/items")!.AsArray().Count);
        Assert.Equal("d", (string?)state.GetData("/items/3"));
    }

    [Fact]
    public void Apply_RemoveAtAPath_DeletesTheKey()
    {
        var state = new A2UISurfaceState("s1");
        state.Apply(UpdateDataModelMessage.Replace("s1", new JsonObject { ["a"] = 1, ["b"] = 2 }));

        state.Apply(UpdateDataModelMessage.Remove("s1", "/a"));

        Assert.False(state.DataModel!.AsObject().ContainsKey("a"));
        Assert.True(state.DataModel.AsObject().ContainsKey("b"));
    }

    [Fact]
    public void Apply_SetToNull_KeepsTheKeyWithANullValue()
    {
        // The delete/write distinction is presence of 'value', not its nullness.
        var state = new A2UISurfaceState("s1");
        state.Apply(UpdateDataModelMessage.Replace("s1", new JsonObject { ["a"] = 1 }));

        state.Apply(UpdateDataModelMessage.Set("s1", "/a", null));

        Assert.True(state.DataModel!.AsObject().ContainsKey("a"));
        Assert.Null(state.DataModel["a"]);
    }

    [Fact]
    public void Apply_ReplaceAtTheRoot_SwapsTheWholeModel()
    {
        var state = new A2UISurfaceState("s1");
        state.Apply(UpdateDataModelMessage.Replace("s1", new JsonObject { ["a"] = 1 }));

        state.Apply(UpdateDataModelMessage.Replace("s1", new JsonObject { ["b"] = 2 }));

        Assert.False(state.DataModel!.AsObject().ContainsKey("a"));
    }

    [Fact]
    public void Apply_EscapedPointerSegments_AreDecoded()
    {
        var state = new A2UISurfaceState("s1");

        state.Apply(UpdateDataModelMessage.Set("s1", "/a~1b", JsonValue.Create(1)));

        Assert.True(state.DataModel!.AsObject().ContainsKey("a/b"));
    }

    [Fact]
    public void Apply_MessageForAnotherSurface_Throws()
    {
        var state = new A2UISurfaceState("s1");

        Assert.Throws<ArgumentException>(() => state.Apply(new DeleteSurfaceMessage("s2")));
    }

    [Fact]
    public void Apply_Delete_ClearsTheSurface()
    {
        var state = new A2UISurfaceState("s1");
        state.Apply(new UpdateComponentsMessage("s1", [new A2UIComponent("root", "Text").Set("text", "hi")]));

        state.Apply(new DeleteSurfaceMessage("s1"));

        Assert.True(state.IsDeleted);
        Assert.Empty(state.Components);
    }

    [Fact]
    public void Apply_CreateSurface_RecordsTheDataModelRequest()
    {
        var state = new A2UISurfaceState("s1");

        state.Apply(new CreateSurfaceMessage("s1", "c") { SendDataModel = true });

        Assert.True(state.SendDataModel);
        Assert.Equal("c", state.CatalogId);
    }

    [Fact]
    public void SurfaceSet_RoutesMessagesByTheirSurfaceId()
    {
        var set = new A2UISurfaceSet();

        set.Apply(new A2UIMessage[]
        {
            new CreateSurfaceMessage("a", "c"),
            new CreateSurfaceMessage("b", "c"),
            new UpdateComponentsMessage("a", [new A2UIComponent("root", "Text").Set("text", "in a")]),
        });

        Assert.Equal(2, set.Surfaces.Count);
        Assert.True(set.TryGet("a", out var a));
        Assert.Single(a.Components);
        Assert.True(set.TryGet("b", out var b));
        Assert.Empty(b.Components);
    }

    [Fact]
    public void SurfaceSet_DropsASurfaceOnceItIsDeleted()
    {
        var set = new A2UISurfaceSet();
        set.Apply(new CreateSurfaceMessage("a", "c"));

        set.Apply(new DeleteSurfaceMessage("a"));

        Assert.Empty(set.Surfaces);
    }

    [Fact]
    public void SurfaceSet_ReplayingTheSpecsIncrementalExample_EndsInTheExpectedState()
    {
        var messages = SpecFiles
            .ReadJson("v0_9_1", "catalogs", "basic", "examples", "00_incremental.json")
            .AsObject()["messages"]!
            .AsArray()
            .Select(A2UIJson.FromJsonNode)
            .ToList();

        var set = new A2UISurfaceSet();
        set.Apply(messages);

        Assert.True(set.TryGet("gallery-incremental", out var surface));
        Assert.Equal(4, surface.GetData("/restaurants")!.AsArray().Count);
        Assert.Equal("Spice Route", (string?)surface.GetData("/restaurants/3/title"));

        // The template component gained a button in a later message, without losing its earlier children.
        var template = surface.Components["restaurant_card"];
        Assert.Equal(4, template.Properties["children"]!.AsArray().Count);
    }
}
