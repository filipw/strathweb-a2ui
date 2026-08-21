using System.Text;
using System.Text.Json;
using A2A;
using Microsoft.Extensions.AI;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.Messages;

namespace Strathweb.A2UI.AgentFramework.UnitTests;

/// <summary>
/// Pins the behaviour of the A2A SDK and the agent framework this integration is built on. If one
/// fails after a package bump, work out what changed on the wire before touching the test.
/// </summary>
public class HostConstraintTests
{
    [Fact]
    public void ToPart_ReturnsTheExactPartPlacedInRawRepresentation()
    {
        // This short-circuit is the integration seam: without it a custom payload cannot reach the
        // wire without changing the agent framework.
        var part = A2UIParts.Create(new DeleteSurfaceMessage("s1"));
        var content = new A2UIContent([new DeleteSurfaceMessage("s1")]) { RawRepresentation = part };

        Assert.Same(part, content.ToPart());
    }

    [Fact]
    public void ToPart_A2UIContentCarriesItsOwnPart()
    {
        var content = new A2UIContent([new DeleteSurfaceMessage("s1")]);

        var part = content.ToPart();

        Assert.NotNull(part);
        Assert.True(A2UIParts.IsA2UI(part));
        Assert.Same(content.RawRepresentation, part);
    }

    [Fact]
    public void ToPart_FunctionResultContent_IsDropped()
    {
        // A tool's return value never reaches the wire, hence the out-of-band sink.
        Assert.Null(new FunctionResultContent("call-1", "result").ToPart());
    }

    [Fact]
    public void ToPart_DataContent_ProducesRawBytesRatherThanAJsonDataPart()
    {
        // Returning DataContent with the A2UI MIME type produces a base64 blob, not the JSON data
        // part the specification requires.
        var part = new DataContent(Encoding.UTF8.GetBytes("""{"a":1}"""), "application/json").ToPart();

        Assert.NotNull(part);
        Assert.Null(part.Data);
        Assert.NotNull(part.Raw);
    }

    [Fact]
    public void Message_ToChatMessage_PreservesMessageMetadata()
    {
        // This is how a renderer's capabilities reach the agent.
        var message = new Message
        {
            Role = Role.User,
            Parts = [Part.FromText("hi")],
            Metadata = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["a2uiClientCapabilities"] = JsonDocument
                    .Parse("""{"v0.9":{"supportedCatalogIds":["std"]}}""").RootElement.Clone(),
            },
        };

        var chat = message.ToChatMessage();

        Assert.True(chat.AdditionalProperties!.ContainsKey("a2uiClientCapabilities"));
    }

    [Fact]
    public void Part_ToAIContent_KeepsThePartAndItsMetadata()
    {
        var part = A2UIParts.Create(new DeleteSurfaceMessage("s1"));

        var content = part.ToAIContent();

        Assert.Same(part, content.RawRepresentation);
        Assert.True(content.AdditionalProperties!.ContainsKey(A2UIParts.MimeTypeMetadataKey));
    }

    [Fact]
    public void Part_ToAIContent_DoesNotPromoteTheA2UIMimeTypeOntoTheContent()
    {
        // Inbound detection must read the part's metadata. Checking DataContent.MediaType would never
        // match, and the failure mode is an action the agent silently ignores.
        var content = A2UIParts.Create(new DeleteSurfaceMessage("s1")).ToAIContent();

        Assert.Equal("application/json", Assert.IsType<DataContent>(content).MediaType);
    }
}
