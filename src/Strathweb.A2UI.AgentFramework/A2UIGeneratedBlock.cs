using Strathweb.A2UI.Messages;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>One A2UI block the model wrote, and what became of it.</summary>
public sealed class A2UIGeneratedBlock
{
    private A2UIGeneratedBlock(A2UIContent? content, IReadOnlyList<string> errors)
    {
        Content = content;
        Errors = errors;
    }

    /// <summary>The surface content, when the block was valid.</summary>
    public A2UIContent? Content { get; }

    /// <summary>Why the block was rejected, one entry per problem. Empty when it was valid.</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>Whether the block became a surface.</summary>
    public bool IsValid => Content is not null;

    internal static A2UIGeneratedBlock Valid(IReadOnlyList<A2UIMessage> messages) =>
        new(new A2UIContent(messages), []);

    internal static A2UIGeneratedBlock Invalid(IReadOnlyList<string> errors) => new(null, errors);
}
