using Microsoft.Extensions.AI;

namespace Strathweb.A2UI.Samples;

/// <summary>What a scripted model says: text, tool calls, or both.</summary>
public sealed class ScriptedReply
{
    /// <summary>Creates a text reply.</summary>
    /// <param name="text">What the model says.</param>
    public ScriptedReply(string text)
    {
        Text = text;
    }

    /// <summary>Creates a reply that says something and then calls tools.</summary>
    /// <param name="text">What the model says first, if anything.</param>
    /// <param name="toolCalls">The calls.</param>
    public ScriptedReply(string? text, params FunctionCallContent[] toolCalls)
    {
        Text = text;
        ToolCalls = toolCalls;
    }

    /// <summary>The text, or <see langword="null"/>.</summary>
    public string? Text { get; }

    /// <summary>The tool calls, possibly none.</summary>
    public IReadOnlyList<FunctionCallContent> ToolCalls { get; } = [];

    /// <summary>A call to a tool with no arguments or with the given ones.</summary>
    /// <param name="name">The tool name.</param>
    /// <param name="arguments">The arguments.</param>
    /// <returns>The call.</returns>
    public static FunctionCallContent Call(string name, params (string Name, object? Value)[] arguments) =>
        new(Guid.NewGuid().ToString("N"), name, arguments.ToDictionary(a => a.Name, a => a.Value, StringComparer.Ordinal));
}
