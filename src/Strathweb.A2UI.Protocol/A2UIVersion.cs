namespace Strathweb.A2UI;

/// <summary>A protocol version as it appears in the <c>version</c> field of an A2UI message.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1707:Identifiers should not contain underscores",
    Justification = "Member names mirror the version strings and spec directory names they stand for.")]
public enum A2UIVersion
{
    /// <summary>The <c>"v0.9"</c> wire version. Readable and writable under the v0.9.1 profile.</summary>
    V0_9 = 1,

    /// <summary>The <c>"v0.9.1"</c> wire version. The default this library emits.</summary>
    V0_9_1 = 2,

    /// <summary>
    /// The <c>"v1.0"</c> wire version. Recognised so that a v1.0 payload produces a clear error
    /// rather than a confusing one; no profile implements it yet.
    /// </summary>
    V1_0 = 3,
}
