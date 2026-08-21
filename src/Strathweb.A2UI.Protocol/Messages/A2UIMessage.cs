namespace Strathweb.A2UI.Messages;

/// <summary>
/// Base type for every A2UI message. On the wire a message is an object with a <c>version</c> field
/// and exactly one other key naming the message kind.
/// </summary>
public abstract class A2UIMessage
{
    private protected A2UIMessage()
    {
    }

    /// <summary>The wire protocol version this message declares.</summary>
    public A2UIVersion Version { get; init; } = A2UIVersion.V0_9_1;

    /// <summary>Which message envelope this instance represents.</summary>
    public abstract A2UIMessageKind Kind { get; }

    /// <summary>Which way this message travels.</summary>
    public abstract A2UIMessageDirection Direction { get; }

    /// <summary>The wire key for this message, for example <c>"createSurface"</c>.</summary>
    public abstract string WireKey { get; }
}
