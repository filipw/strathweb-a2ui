using System.Text.Json;
using System.Text.Json.Nodes;
using A2A;
using Microsoft.Extensions.AI;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.TestSupport;

namespace Strathweb.A2UI.AgentFramework.UnitTests;

/// <summary>Everything the renderer sends is untrusted and ends up in the prompt, so it is capped.</summary>
public class InboundLimitsTests
{
    private static readonly A2UIVersionProfile Profile = A2UIVersionProfile.V0_9_1;

    private static ActionMessage Action(JsonObject? context = null) =>
        new("submit", "survey_1", "button_1", DateTimeOffset.UnixEpoch, context ?? new JsonObject { ["rating"] = 5 });

    private static ChatMessage Inbound(params A2UIMessage[] messages) =>
        new Message { Role = Role.User, Parts = [A2UIParts.Create(messages)] }.ToChatMessage();

    [Fact]
    public void Normalize_APartAboveTheSizeLimit_IsReportedRatherThanRead()
    {
        var logs = new CapturingLoggerFactory();
        var normalizer = new A2UIInboundNormalizer(
            Profile,
            new A2UIInboundLimits { MaxPartBytes = 64 },
            logs.CreateLogger("test"));

        var result = normalizer.Normalize([Inbound(Action())]);

        Assert.Empty(result.Actions);
        var text = Assert.IsType<TextContent>(Assert.Single(result.Messages.Single().Contents));
        Assert.Contains("ignored", text.Text, StringComparison.Ordinal);
        Assert.Contains(logs.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("limit is 64", StringComparison.Ordinal));
    }

    [Fact]
    public void Normalize_APartWithinTheLimit_IsRead()
    {
        var normalizer = new A2UIInboundNormalizer(Profile, new A2UIInboundLimits { MaxPartBytes = 4096 });

        var result = normalizer.Normalize([Inbound(Action())]);

        Assert.Single(result.Actions);
    }

    [Fact]
    public void Normalize_ADataModelAboveTheSizeLimit_IsIgnoredWhole()
    {
        var registry = new A2UISurfaceRegistry();
        registry.Add("survey_1", "std", DateTimeOffset.UnixEpoch);

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
                new Dictionary<string, JsonNode?> { ["survey_1"] = new JsonObject { ["comment"] = new string('x', 500) } }),
            Profile);

        var result = new A2UIInboundNormalizer(Profile, new A2UIInboundLimits { MaxDataModelBytes = 256 })
            .Normalize([a2a.ToChatMessage()], registry);

        Assert.Empty(result.SurfaceData);
        Assert.Empty(result.IgnoredSurfaceData);
    }

    [Fact]
    public void Describe_ALongValue_IsCutWithAnEllipsis()
    {
        var action = Action(new JsonObject { ["comment"] = new string('x', 2000) });

        var text = A2UIInboundNormalizer.Describe(action, 20);

        Assert.Contains("comment=\"" + new string('x', 20) + "…\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain(new string('x', 21), text, StringComparison.Ordinal);
    }

    [Fact]
    public void Describe_ALongNonStringValue_IsCutToo()
    {
        var action = Action(new JsonObject { ["items"] = new JsonArray(Enumerable.Range(0, 500).Select(i => (JsonNode)i).ToArray()) });

        var text = A2UIInboundNormalizer.Describe(action, 30);

        Assert.Contains("items=[0,1,2,", text, StringComparison.Ordinal);
        Assert.Contains("…", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Describe_TheDefaultLimit_LeavesOrdinaryValuesAlone()
    {
        var text = A2UIInboundNormalizer.Describe(Action(new JsonObject { ["comment"] = "fast fix" }));

        Assert.Contains("comment=\"fast fix\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("…", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Limits_MustBePositive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new A2UIInboundLimits { MaxPartBytes = 0 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new A2UIInboundLimits { MaxDataModelBytes = -1 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new A2UIInboundLimits { MaxDescribedValueLength = 0 });
    }

    [Fact]
    public void Normalize_CapabilitiesUnderAnotherVersionsKey_AreReportedAsAVersionMismatch()
    {
        var logs = new CapturingLoggerFactory();
        var a2a = new Message
        {
            Role = Role.User,
            Parts = [Part.FromText("hi")],
            Metadata = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["a2uiRendererCapabilities"] = JsonDocument
                    .Parse("""{"v1.0":{"supportedCatalogIds":["std"]}}""").RootElement.Clone(),
            },
        };

        var result = new A2UIInboundNormalizer(Profile, logger: logs.CreateLogger("test")).Normalize([a2a.ToChatMessage()]);

        Assert.Null(result.RendererCapabilities);
        Assert.Contains(logs.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("a2uiRendererCapabilities", StringComparison.Ordinal));
    }

    [Fact]
    public void Normalize_ADroppedDataModel_IsLoggedWithTheLikelyCause()
    {
        var logs = new CapturingLoggerFactory();
        var a2a = new Message
        {
            Role = Role.User,
            Parts = [Part.FromText("hi")],
            Metadata = new Dictionary<string, JsonElement>(StringComparer.Ordinal),
        };
        A2UIMetadata.WriteDataModel(
            a2a.Metadata,
            new A2UIRendererDataModel(A2UIVersion.V0_9_1, new Dictionary<string, JsonNode?> { ["unknown"] = new JsonObject() }),
            Profile);

        new A2UIInboundNormalizer(Profile, logger: logs.CreateLogger("test"))
            .Normalize([a2a.ToChatMessage()], new A2UISurfaceRegistry());

        Assert.Contains(logs.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("persisting sessions", StringComparison.Ordinal));
    }
}
