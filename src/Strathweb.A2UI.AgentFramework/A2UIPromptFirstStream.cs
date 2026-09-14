using System.Text.Json.Nodes;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Strathweb.A2UI.Parsing;
using Strathweb.A2UI.Serialization;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>
/// Splits a streamed reply into prose for the user and surfaces for the renderer while it arrives.
/// Text is passed on as soon as it cannot be the start of a tag; a surface is emitted the moment its
/// block closes and validates.
/// </summary>
internal sealed class A2UIPromptFirstStream
{
    private readonly A2UIGeneratedPayloadReader reader;
    private readonly bool suppressText;
    private readonly List<JsonNode> pendingBlock = [];
    private readonly List<A2UIGeneratedBlock> blocks = [];

    private A2UIStreamParser parser;
    private bool skippingBrokenBlock;

    internal A2UIPromptFirstStream(A2UIGeneratedPayloadReader reader, bool suppressText = false)
    {
        this.reader = reader;
        this.suppressText = suppressText;
        parser = new A2UIStreamParser(repair: reader.Repair);
    }

    /// <summary>Every block seen so far, valid or not.</summary>
    internal IReadOnlyList<A2UIGeneratedBlock> Blocks => blocks;

    /// <summary>Processes one update from the inner agent and returns what to pass on in its place.</summary>
    internal IEnumerable<StreamPiece> Process(AgentResponseUpdate update)
    {
        var texts = update.Contents.OfType<TextContent>().ToList();
        if (texts.Count == 0)
        {
            yield return StreamPiece.PassThrough(update);
            yield break;
        }

        var others = update.Contents.Where(c => c is not TextContent).ToList();
        if (others.Count > 0)
        {
            yield return StreamPiece.PassThrough(Clone(update, others));
        }

        foreach (var text in texts)
        {
            foreach (var piece in Feed(text.Text ?? string.Empty, update))
            {
                yield return piece;
            }
        }
    }

    /// <summary>Signals the end of the reply and returns anything still held back.</summary>
    internal IEnumerable<StreamPiece> Complete(AgentResponseUpdate? last)
    {
        if (skippingBrokenBlock)
        {
            skippingBrokenBlock = false;
            yield break;
        }

        IReadOnlyList<A2UIResponsePart> trailing;
        try
        {
            trailing = parser.Complete();
        }
        catch (A2UIParseException ex)
        {
            pendingBlock.Clear();
            blocks.Add(A2UIGeneratedBlock.Invalid([ex.Message]));
            yield break;
        }

        foreach (var part in trailing)
        {
            if (part.Text is { Length: > 0 } text && !suppressText && last is not null)
            {
                yield return StreamPiece.PassThrough(Clone(last, [new TextContent(text)]));
            }
        }
    }

    private IEnumerable<StreamPiece> Feed(string text, AgentResponseUpdate update)
    {
        var position = 0;

        while (position < text.Length)
        {
            if (skippingBrokenBlock)
            {
                // The block is unreadable; drop everything up to its closing tag and start clean.
                var close = text.IndexOf(A2UIResponseTags.Close, position, StringComparison.Ordinal);
                if (close < 0)
                {
                    yield break;
                }

                position = close + A2UIResponseTags.Close.Length;
                skippingBrokenBlock = false;
                parser = new A2UIStreamParser(repair: reader.Repair);
                continue;
            }

            // Feed up to and including one closing tag at a time, so each flush belongs to one block.
            var end = text.IndexOf(A2UIResponseTags.Close, position, StringComparison.Ordinal);
            var segment = end < 0
                ? text.Substring(position)
                : text.Substring(position, end + A2UIResponseTags.Close.Length - position);
            position += segment.Length;

            var wasInside = parser.IsInsideA2UIBlock;
            IReadOnlyList<A2UIResponsePart> parts;
            try
            {
                parts = parser.Feed(segment);
            }
            catch (A2UIParseException ex)
            {
                blocks.Add(A2UIGeneratedBlock.Invalid([ex.Message]));
                pendingBlock.Clear();
                skippingBrokenBlock = true;
                continue;
            }

            foreach (var part in parts)
            {
                if (part.Text is { Length: > 0 } prose && !suppressText)
                {
                    yield return StreamPiece.PassThrough(Clone(update, [new TextContent(prose)]));
                }

                if (part.Messages is { } messages)
                {
                    foreach (var message in messages)
                    {
                        pendingBlock.Add(message!.DeepClone());
                    }
                }
            }

            // The parser leaves the block when it has seen the whole closing tag, which may have
            // arrived in pieces; a segment that contains the tag closes a block it also opened.
            if ((wasInside || end >= 0) && !parser.IsInsideA2UIBlock)
            {
                foreach (var piece in Flush())
                {
                    yield return piece;
                }
            }
        }
    }

    private IEnumerable<StreamPiece> Flush()
    {
        if (pendingBlock.Count == 0)
        {
            blocks.Add(A2UIGeneratedBlock.Invalid(["A2UI JSON part is empty."]));
            yield break;
        }

        var array = new JsonArray();
        foreach (var message in pendingBlock)
        {
            array.Add(message);
        }

        pendingBlock.Clear();

        var block = reader.ReadBlock(array);
        blocks.Add(block);

        if (block.Content is { } content)
        {
            yield return StreamPiece.Surface(content);
        }
    }

    private static AgentResponseUpdate Clone(AgentResponseUpdate update, IList<AIContent> contents) =>
        new(update.Role, contents)
        {
            AuthorName = update.AuthorName,
            MessageId = update.MessageId,
            ResponseId = update.ResponseId,
            CreatedAt = update.CreatedAt,
            AdditionalProperties = update.AdditionalProperties,
        };

    /// <summary>What the stream produces: an update to pass on, or a surface to emit.</summary>
    internal readonly struct StreamPiece
    {
        private StreamPiece(AgentResponseUpdate? update, A2UIContent? content)
        {
            Update = update;
            Content = content;
        }

        internal AgentResponseUpdate? Update { get; }

        internal A2UIContent? Content { get; }

        internal static StreamPiece PassThrough(AgentResponseUpdate update) => new(update, null);

        internal static StreamPiece Surface(A2UIContent content) => new(null, content);
    }
}
