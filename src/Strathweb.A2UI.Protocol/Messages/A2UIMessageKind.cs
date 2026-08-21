namespace Strathweb.A2UI.Messages;

/// <summary>Identifies which of the single-key message envelopes a <see cref="A2UIMessage"/> represents.</summary>
public enum A2UIMessageKind
{
    /// <summary>An agent-to-renderer <c>createSurface</c> message.</summary>
    CreateSurface = 1,

    /// <summary>An agent-to-renderer <c>updateComponents</c> message.</summary>
    UpdateComponents = 2,

    /// <summary>An agent-to-renderer <c>updateDataModel</c> message.</summary>
    UpdateDataModel = 3,

    /// <summary>An agent-to-renderer <c>deleteSurface</c> message.</summary>
    DeleteSurface = 4,

    /// <summary>A renderer-to-agent <c>action</c> message.</summary>
    Action = 5,

    /// <summary>A renderer-to-agent <c>error</c> message.</summary>
    Error = 6,
}
