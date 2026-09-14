namespace Strathweb.A2UI.Samples;

/// <summary>One thing that crossed the wire, or one thing the library reported, for the demo's log panel.</summary>
/// <param name="Kind"><c>out</c>, <c>in</c>, <c>log</c> or <c>err</c>.</param>
/// <param name="Text">What happened, in one line.</param>
/// <param name="At">When.</param>
public sealed record WireLogEntry(string Kind, string Text, DateTimeOffset At);
