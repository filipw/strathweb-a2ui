using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.Messages;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>What a turn's inbound messages carried, once the A2UI in them was unpacked.</summary>
public sealed class A2UIInboundResult
{
    internal A2UIInboundResult(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ActionMessage> actions,
        IReadOnlyList<ErrorMessage> errors,
        A2UIRendererCapabilities? rendererCapabilities,
        IReadOnlyDictionary<string, JsonNode?> surfaceData)
    {
        Messages = messages;
        Actions = actions;
        Errors = errors;
        RendererCapabilities = rendererCapabilities;
        SurfaceData = surfaceData;
    }

    /// <summary>
    /// The messages to pass on, with each A2UI part replaced by a structured action and a sentence
    /// describing it.
    /// </summary>
    public IReadOnlyList<ChatMessage> Messages { get; }

    /// <summary>What the user did, in arrival order.</summary>
    public IReadOnlyList<ActionMessage> Actions { get; }

    /// <summary>What the renderer could not display.</summary>
    public IReadOnlyList<ErrorMessage> Errors { get; }

    /// <summary>What the renderer says it can render, when it said anything.</summary>
    public A2UIRendererCapabilities? RendererCapabilities { get; }

    /// <summary>
    /// The current data model of each surface the renderer reported, for surfaces this session
    /// created. Empty unless those surfaces asked for it with <c>sendDataModel</c>.
    /// </summary>
    public IReadOnlyDictionary<string, JsonNode?> SurfaceData { get; }

    /// <summary>Whether the renderer sent anything A2UI at all.</summary>
    public bool IsEmpty => Actions.Count == 0 && Errors.Count == 0 && SurfaceData.Count == 0;
}
