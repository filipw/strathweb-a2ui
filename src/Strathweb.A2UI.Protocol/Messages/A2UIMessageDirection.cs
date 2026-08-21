namespace Strathweb.A2UI.Messages;

/// <summary>Which way an A2UI message travels.</summary>
public enum A2UIMessageDirection
{
    /// <summary>Sent by the agent, applied by the renderer.</summary>
    AgentToRenderer = 1,

    /// <summary>Sent by the renderer, received by the agent.</summary>
    RendererToAgent = 2,
}
