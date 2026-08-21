using System.Text.Json.Nodes;
using Strathweb.A2UI.Components;
using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Protocol.UnitTests;

public class ValueTypeTests
{
    [Fact]
    public void DynamicValue_ObjectWithOnlyAPath_ReadsAsABinding()
    {
        var value = DynamicValue.FromJson(JsonNode.Parse("""{"path":"/user/name"}"""));

        Assert.Equal(DynamicValueKind.Path, value.Kind);
        Assert.Equal("/user/name", value.Path);
    }

    [Fact]
    public void DynamicValue_ObjectWithACall_ReadsAsAFunctionCall()
    {
        var value = DynamicValue.FromJson(JsonNode.Parse("""{"call":"formatDate","args":{"format":"d"}}"""));

        Assert.Equal(DynamicValueKind.FunctionCall, value.Kind);
        Assert.Equal("formatDate", value.Call!.Call);
        Assert.Equal("d", value.Call.Args!["format"].Literal!.GetValue<string>());
    }

    [Fact]
    public void DynamicValue_ObjectWithAPathAndOtherKeys_ReadsAsALiteral()
    {
        // A binding forbids extra properties, so this is data that happens to have a 'path' key.
        var value = DynamicValue.FromJson(JsonNode.Parse("""{"path":"/a","label":"x"}"""));

        Assert.Equal(DynamicValueKind.Literal, value.Kind);
    }

    [Theory]
    [InlineData("\"hello\"")]
    [InlineData("42")]
    [InlineData("true")]
    [InlineData("[1,2,3]")]
    [InlineData("{\"a\":1}")]
    public void DynamicValue_Literal_RoundTrips(string json)
    {
        var node = JsonNode.Parse(json);

        Assert.True(JsonNode.DeepEquals(node, DynamicValue.FromJson(node).ToJson()));
    }

    [Fact]
    public void DynamicValue_FromPath_RejectsAnEmptyPath()
    {
        Assert.Throws<ArgumentException>(() => DynamicValue.FromPath(string.Empty));
    }

    [Fact]
    public void ChildList_FixedList_RoundTrips()
    {
        var list = ChildList.Of("a", "b", "c");
        var round = ChildList.FromJson(list.ToJson());

        Assert.False(round.IsTemplate);
        Assert.Equal(["a", "b", "c"], round.ComponentIds!);
    }

    [Fact]
    public void ChildList_Template_RoundTrips()
    {
        var round = ChildList.FromJson(ChildList.Template("row", "/items").ToJson());

        Assert.True(round.IsTemplate);
        Assert.Equal("row", round.TemplateComponentId);
        Assert.Equal("/items", round.TemplatePath);
    }

    [Fact]
    public void ChildList_ArrayContainingANonString_Throws()
    {
        Assert.Throws<A2UIValueException>(() => ChildList.FromJson(JsonNode.Parse("""["a", 1]""")));
    }

    [Fact]
    public void ChildList_TemplateMissingItsPath_Throws()
    {
        Assert.Throws<A2UIValueException>(() => ChildList.FromJson(JsonNode.Parse("""{"componentId":"row"}""")));
    }

    [Fact]
    public void CheckRule_RequiresAMessage()
    {
        Assert.Throws<A2UIValueException>(
            () => CheckRule.FromJson(JsonNode.Parse("""{"condition":{"call":"required"}}""")));
    }

    [Fact]
    public void CheckRule_RoundTrips()
    {
        var rule = new CheckRule(
            DynamicValue.FromCall(new FunctionCall("required")
            {
                Args = new Dictionary<string, DynamicValue> { ["value"] = DynamicValue.FromPath("/rating") },
            }),
            "Please pick a rating.");

        var round = CheckRule.FromJson(rule.ToJson());

        Assert.Equal("Please pick a rating.", round.Message);
        Assert.Equal("required", round.Condition.Call!.Call);
    }

    [Fact]
    public void Action_Event_RoundTrips()
    {
        var action = A2UIAction.FromEvent(new A2UIEvent("submit")
        {
            Context = new Dictionary<string, DynamicValue> { ["rating"] = DynamicValue.FromPath("/rating") },
        });

        var round = A2UIAction.FromJson(action.ToJson());

        Assert.Equal("submit", round.Event!.Name);
        Assert.Equal("/rating", round.Event.Context!["rating"].Path);
    }

    [Fact]
    public void Action_FunctionCall_RoundTrips()
    {
        var round = A2UIAction.FromJson(A2UIAction.FromFunctionCall(new FunctionCall("openUrl")).ToJson());

        Assert.Null(round.Event);
        Assert.Equal("openUrl", round.FunctionCall!.Call);
    }

    [Fact]
    public void Action_WithNeitherEventNorFunctionCall_Throws()
    {
        Assert.Throws<A2UIValueException>(() => A2UIAction.FromJson(JsonNode.Parse("""{"other":1}""")));
    }

    [Fact]
    public void Component_KeepsUnknownCatalogPropertiesVerbatim()
    {
        var component = A2UIComponent.FromJson(JsonNode.Parse("""
            {"id":"c1","component":"Custom","somethingWeDoNotModel":{"nested":[1,2]}}
            """));

        Assert.Equal("c1", component.Id);
        Assert.True(JsonNode.DeepEquals(
            JsonNode.Parse("""{"nested":[1,2]}"""),
            component.Properties["somethingWeDoNotModel"]));
    }

    [Fact]
    public void Component_AccessibilityIsLiftedOutOfThePropertyBag()
    {
        var component = A2UIComponent.FromJson(JsonNode.Parse("""
            {"id":"c1","component":"Button","accessibility":{"label":"Mute"},"child":"c2"}
            """));

        Assert.Equal("Mute", component.Accessibility!.Label!.Literal!.GetValue<string>());
        Assert.False(component.Properties.ContainsKey("accessibility"));
    }

    [Fact]
    public void Component_EmptyAccessibilityIsNotWritten()
    {
        var json = new A2UIComponent("c1", "Text") { Accessibility = new A2UIAccessibility() }.ToJson();

        Assert.False(json.ContainsKey("accessibility"));
    }

    [Fact]
    public void Component_EmptyIdIsRepresentable_SoTheValidatorCanReportIt()
    {
        // An empty id is a validation error, not a parse error, and a missing id rather than a
        // duplicate.
        var component = A2UIComponent.FromJson(JsonNode.Parse("""{"id":"","component":"Text"}"""));

        Assert.Equal(string.Empty, component.Id);
    }

    [Fact]
    public void FunctionCall_OmitsEmptyArgs()
    {
        var json = new FunctionCall("required") { Args = new Dictionary<string, DynamicValue>() }.ToJson();

        Assert.False(json.ContainsKey("args"));
    }

    [Fact]
    public void FunctionCall_UnknownReturnType_Throws()
    {
        Assert.Throws<A2UIValueException>(
            () => FunctionCall.FromJson(JsonNode.Parse("""{"call":"x","returnType":"widget"}""")));
    }
}
