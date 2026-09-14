using System.Text;
using Strathweb.A2UI.Rendering;

namespace Strathweb.A2UI.Samples;

/// <summary>One turn in the transcript: who said it, the text so far, and the surfaces it produced.</summary>
public sealed class ChatEntry
{
    private readonly StringBuilder text = new();

    /// <summary>Creates an entry.</summary>
    /// <param name="role"><c>user</c> or <c>agent</c>.</param>
    /// <param name="text">The initial text, if any.</param>
    public ChatEntry(string role, string? text = null)
    {
        Role = role;
        if (text is not null)
        {
            this.text.Append(text);
        }
    }

    /// <summary>Who wrote it.</summary>
    public string Role { get; }

    /// <summary>The text so far. Grows while the agent streams.</summary>
    public string Text => text.ToString();

    /// <summary>The surfaces the agent created during this turn, in order.</summary>
    public List<A2UIRenderSurface> Surfaces { get; } = [];

    /// <summary>Whether the entry has anything to show.</summary>
    public bool IsEmpty => text.Length == 0 && Surfaces.Count == 0;

    /// <summary>Appends streamed text.</summary>
    /// <param name="chunk">The text that arrived.</param>
    public void Append(string chunk) => text.Append(chunk);
}
