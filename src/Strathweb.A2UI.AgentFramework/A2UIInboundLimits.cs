namespace Strathweb.A2UI.AgentFramework;

/// <summary>
/// Caps on what is accepted from a renderer. Everything the renderer sends is untrusted input that
/// ends up in the prompt, so each cap has a default that fits a form and rejects a flood.
/// </summary>
public sealed class A2UIInboundLimits
{
    private int maxPartBytes = 256 * 1024;
    private int maxDataModelBytes = 256 * 1024;
    private int maxDescribedValueLength = 500;

    /// <summary>
    /// The largest A2UI data part that is read, in bytes of its JSON. Larger parts are reported to
    /// the model as unreadable. Defaults to 256 KiB.
    /// </summary>
    public int MaxPartBytes
    {
        get => maxPartBytes;
        set => maxPartBytes = Positive(value);
    }

    /// <summary>
    /// The largest surface data model payload that is read from message metadata, in bytes of its
    /// JSON. A larger payload is ignored whole. Defaults to 256 KiB.
    /// </summary>
    public int MaxDataModelBytes
    {
        get => maxDataModelBytes;
        set => maxDataModelBytes = Positive(value);
    }

    /// <summary>
    /// The most characters of any one context value that make it into the sentence the model reads.
    /// Longer values are cut with an ellipsis. Defaults to 500.
    /// </summary>
    public int MaxDescribedValueLength
    {
        get => maxDescribedValueLength;
        set => maxDescribedValueLength = Positive(value);
    }

    private static int Positive(int value) =>
        value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "The limit must be positive.");
}
