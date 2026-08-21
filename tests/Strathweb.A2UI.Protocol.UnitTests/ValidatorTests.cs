using System.Text.Json.Nodes;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Components;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Validation;
using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Protocol.UnitTests;

public class ValidatorTests
{
    private static readonly A2UICatalog Basic = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

    private static A2UIValidator Validator(Action<A2UIValidationOptionsBuilder>? configure = null)
    {
        var builder = new A2UIValidationOptionsBuilder { Catalog = Basic };
        configure?.Invoke(builder);
        return new A2UIValidator(builder.Build());
    }

    [Fact]
    public void Validate_ASurfaceTheBuilderWouldProduce_IsValid()
    {
        var result = Validator().Validate(Surface(
            new A2UIComponent("root", "Card").Set("child", "column"),
            new A2UIComponent("column", "Column").Set("children", ChildList.Of("title").ToJson()),
            new A2UIComponent("title", "Text").Set("text", DynamicValue.FromString("Hello"))));

        Assert.True(result.IsValid, result.ToString());
    }

    [Fact]
    public void Validate_EmptyComponentId_ReportsMissingIdNotDuplicate()
    {
        // Two empty ids must not be reported as duplicates of each other: the real problem is that
        // neither has an id at all, and calling it a duplicate points at the wrong fix.
        var result = Validator().Validate(Surface(
            new A2UIComponent("root", "Card").Set("child", "x"),
            new A2UIComponent(string.Empty, "Text").Set("text", "a"),
            new A2UIComponent(string.Empty, "Text").Set("text", "b")));

        Assert.Equal(2, result.Errors.Count(e => e.Code == A2UIErrorCodes.MissingId));
        Assert.DoesNotContain(result.Errors, e => e.Code == A2UIErrorCodes.DuplicateId);
    }

    [Fact]
    public void Validate_DuplicateIds_AreReported()
    {
        var result = Validator().Validate(Surface(
            new A2UIComponent("root", "Column").Set("children", ChildList.Of("a").ToJson()),
            new A2UIComponent("a", "Text").Set("text", "one"),
            new A2UIComponent("a", "Text").Set("text", "two")));

        Assert.Contains(result.Errors, e => e.Code == A2UIErrorCodes.DuplicateId && e.Message.Contains('a'));
    }

    [Fact]
    public void Validate_SingularChildReference_IsFollowed()
    {
        // 'child' is as much a reference as 'children'. Checking only the plural form lets dangling
        // singular references through.
        var result = Validator().Validate(Surface(
            new A2UIComponent("root", "Card").Set("child", "missing")));

        Assert.Contains(result.Errors, e => e.Code == A2UIErrorCodes.UnresolvedReference);
    }

    [Fact]
    public void Validate_TemplateChildList_ResolvesItsTemplateComponent()
    {
        var result = Validator().Validate(Surface(
            new A2UIComponent("root", "List").Set("children", ChildList.Template("missing", "/items").ToJson())));

        Assert.Contains(
            result.Errors,
            e => e.Code == A2UIErrorCodes.UnresolvedReference && e.Message.Contains("'missing'"));
    }

    [Fact]
    public void Validate_ReferenceNestedInsideAnArrayProperty_IsFollowed()
    {
        // Tabs.tabs is an array of objects, each with a 'child'. The catalog is what says so.
        var tabs = new JsonArray
        {
            new JsonObject { ["title"] = "One", ["child"] = "present" },
            new JsonObject { ["title"] = "Two", ["child"] = "missing" },
        };

        var result = Validator().Validate(Surface(
            new A2UIComponent("root", "Tabs").Set("tabs", tabs),
            new A2UIComponent("present", "Text").Set("text", "here")));

        Assert.Contains(
            result.Errors,
            e => e.Code == A2UIErrorCodes.UnresolvedReference && e.Message.Contains("'missing'"));
    }

    [Fact]
    public void Validate_AStringPropertyThatIsNotAReference_IsTreatedAsData()
    {
        // 'text' happens to look like an id. Only the catalog decides what is a reference.
        var result = Validator().Validate(Surface(
            new A2UIComponent("root", "Text").Set("text", "card_1")));

        Assert.True(result.IsValid, result.ToString());
    }

    [Fact]
    public void Validate_SelfReference_IsReported()
    {
        var result = Validator().Validate(Surface(new A2UIComponent("root", "Card").Set("child", "root")));

        Assert.Contains(result.Errors, e => e.Code == A2UIErrorCodes.SelfReference);
    }

    [Fact]
    public void Validate_Cycle_IsReported()
    {
        var result = Validator().Validate(Surface(
            new A2UIComponent("root", "Card").Set("child", "a"),
            new A2UIComponent("a", "Card").Set("child", "root")));

        Assert.Contains(result.Errors, e => e.Code == A2UIErrorCodes.CircularReference);
    }

    [Fact]
    public void Validate_UnreachableComponent_IsReported()
    {
        var result = Validator().Validate(Surface(
            new A2UIComponent("root", "Text").Set("text", "hi"),
            new A2UIComponent("orphan", "Text").Set("text", "nobody points at me")));

        Assert.Contains(
            result.Errors,
            e => e.Code == A2UIErrorCodes.OrphanComponent && e.Message.Contains("'orphan'"));
    }

    [Fact]
    public void Validate_MissingRoot_IsReportedWhenThePayloadCreatesTheSurface()
    {
        var result = Validator().Validate(Surface(new A2UIComponent("c1", "Text").Set("text", "hi")));

        Assert.Contains(result.Errors, e => e.Code == A2UIErrorCodes.MissingRoot);
    }

    [Fact]
    public void Validate_MissingRoot_IsAllowedInAnIncrementalUpdate()
    {
        // Without a createSurface the renderer already holds the tree; this payload only adds to it.
        var result = Validator().Validate(new A2UIMessage[]
        {
            new UpdateComponentsMessage("s1",
            [
                new A2UIComponent("card1", "Card").Set("child", "text1"),
                new A2UIComponent("text1", "Text").Set("text", "Updated"),
            ]),
        });

        Assert.True(result.IsValid, result.ToString());
    }

    [Fact]
    public void Validate_CycleInAnIncrementalUpdate_IsStillReported()
    {
        var result = Validator().Validate(new A2UIMessage[]
        {
            new UpdateComponentsMessage("s1",
            [
                new A2UIComponent("a", "Card").Set("child", "b"),
                new A2UIComponent("b", "Card").Set("child", "a"),
            ]),
        });

        Assert.Contains(result.Errors, e => e.Code == A2UIErrorCodes.CircularReference);
    }

    [Fact]
    public void Validate_UnknownComponentType_IsReported()
    {
        var result = Validator().Validate(Surface(new A2UIComponent("root", "Hologram")));

        Assert.Contains(result.Errors, e => e.Code == A2UIErrorCodes.UnknownComponent);
    }

    [Fact]
    public void Validate_UnknownProperty_IsReported()
    {
        var result = Validator().Validate(Surface(
            new A2UIComponent("root", "Text").Set("text", "hi").Set("sparkle", true)));

        Assert.Contains(
            result.Errors,
            e => e.Code == A2UIErrorCodes.UnknownProperty && e.Message.Contains("sparkle"));
    }

    [Fact]
    public void Validate_MalformedBindingPath_IsReported()
    {
        var result = Validator().Validate(Surface(
            new A2UIComponent("root", "Text").Set("text", new JsonObject { ["path"] = "/bad/escape/~2" })));

        Assert.Contains(result.Errors, e => e.Code == A2UIErrorCodes.InvalidPointer);
    }

    [Fact]
    public void Validate_RelativePathInsideATemplate_IsAccepted()
    {
        var result = Validator().Validate(Surface(
            new A2UIComponent("root", "List").Set("children", ChildList.Template("row", "/items").ToJson()),
            new A2UIComponent("row", "Text").Set("text", DynamicValue.FromPath("title"))));

        Assert.True(result.IsValid, result.ToString());
    }

    [Fact]
    public void Validate_ChainDeeperThanTheLimit_IsReported()
    {
        var result = Validator().Validate(Surface(Chain(60)));

        Assert.Contains(
            result.Errors,
            e => e.Code == A2UIErrorCodes.RecursionLimit && e.Message.Contains("logical depth"));
    }

    [Fact]
    public void Validate_AVeryLongChain_DoesNotOverflowTheStack()
    {
        // 20,000 deep with the depth limit lifted. A recursive walk would die here; the point of the
        // explicit-stack traversal is that this returns a result instead of crashing the process.
        var result = Validator(o => o.MaxDepth = int.MaxValue).Validate(Surface(Chain(20_000)));

        Assert.True(result.IsValid, result.ToString());
    }

    [Fact]
    public void Validate_MissingCatalogId_IsReportedAtItsExactLocation()
    {
        var payload = JsonNode.Parse("""[{"version":"v0.9.1","createSurface":{"surfaceId":"s1"}}]""");

        var result = Validator().Validate(payload);

        Assert.Contains(
            result.Errors,
            e => e.Code == A2UIErrorCodes.MissingField && e.Path == "messages.0.createSurface.catalogId");
    }

    [Fact]
    public void Validate_MistypedSurfaceId_IsReportedAsATypeMismatch()
    {
        var payload = JsonNode.Parse("""[{"version":"v0.9.1","createSurface":{"surfaceId":123,"catalogId":"c"}}]""");

        var result = Validator().Validate(payload);

        Assert.Contains(
            result.Errors,
            e => e.Code == A2UIErrorCodes.TypeMismatch && e.Path == "messages.0.createSurface.surfaceId");
    }

    [Fact]
    public void Validate_CollectsEveryProblemRatherThanStoppingAtTheFirst()
    {
        var result = Validator().Validate(Surface(
            new A2UIComponent("root", "Card").Set("child", "missing"),
            new A2UIComponent("orphan", "Hologram")));

        Assert.True(result.Errors.Count >= 3, result.ToString());
        Assert.Contains(result.Errors, e => e.Code == A2UIErrorCodes.UnresolvedReference);
        Assert.Contains(result.Errors, e => e.Code == A2UIErrorCodes.UnknownComponent);
        Assert.Contains(result.Errors, e => e.Code == A2UIErrorCodes.OrphanComponent);
    }

    [Fact]
    public void ThrowIfInvalid_CarriesTheErrorsOnTheException()
    {
        var result = Validator().Validate(Surface(new A2UIComponent("c1", "Text").Set("text", "hi")));

        var thrown = Assert.Throws<A2UIValidationException>(result.ThrowIfInvalid);

        Assert.Same(result, thrown.Result);
    }

    private static A2UIComponent[] Chain(int length)
    {
        var components = new A2UIComponent[length];
        for (var i = 0; i < length - 1; i++)
        {
            components[i] = new A2UIComponent(i == 0 ? "root" : $"c{i}", "Card").Set("child", $"c{i + 1}");
        }

        components[length - 1] = new A2UIComponent($"c{length - 1}", "Text").Set("text", "end");
        return components;
    }

    private static A2UIMessage[] Surface(params A2UIComponent[] components) =>
    [
        new CreateSurfaceMessage("s1", Basic.CatalogId),
        new UpdateComponentsMessage("s1", components),
    ];

    internal sealed class A2UIValidationOptionsBuilder
    {
        internal A2UICatalog? Catalog { get; set; }

        internal int MaxDepth { get; set; } = A2UIValidationOptions.Default.MaxDepth;

        internal A2UIValidationOptions Build() => new() { Catalog = Catalog, MaxDepth = MaxDepth };
    }
}
