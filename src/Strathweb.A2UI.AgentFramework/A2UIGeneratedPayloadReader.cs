using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Parsing;
using Strathweb.A2UI.Serialization;
using Strathweb.A2UI.Validation;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>
/// Turns the blocks a model wrote into surfaces: parses, validates against the catalog, and reports
/// what could not be used in words the model can act on.
/// </summary>
internal sealed class A2UIGeneratedPayloadReader
{
    private readonly A2UICatalog catalog;
    private readonly A2UIVersionProfile profile;
    private readonly bool repair;
    private readonly ILogger logger;

    internal A2UIGeneratedPayloadReader(A2UICatalog catalog, A2UIVersionProfile profile, bool repair, ILogger logger)
    {
        this.catalog = catalog;
        this.profile = profile;
        this.repair = repair;
        this.logger = logger;
    }

    internal bool Repair => repair;

    /// <summary>
    /// Splits a complete response into prose and blocks. Returns <see langword="null"/> when the text
    /// holds no block at all, so callers can leave such content untouched.
    /// </summary>
    internal (string Prose, IReadOnlyList<A2UIGeneratedBlock> Blocks)? Read(string text)
    {
        if (!A2UIResponseParser.ContainsA2UIBlock(text))
        {
            return null;
        }

        IReadOnlyList<A2UIResponsePart> parts;
        try
        {
            parts = A2UIResponseParser.Parse(text, repair);
        }
        catch (A2UIParseException ex)
        {
            // The whole response is unusable as A2UI; keep the prose the model wrote around it.
            logger.GeneratedBlockUnreadable(ex.Message);
            return (StripBlocks(text), [A2UIGeneratedBlock.Invalid([ex.Message])]);
        }

        var prose = new List<string>();
        var blocks = new List<A2UIGeneratedBlock>();

        foreach (var part in parts)
        {
            if (part.Text is { Length: > 0 } t)
            {
                prose.Add(t);
            }

            if (part.Messages is { } messages)
            {
                blocks.Add(ReadBlock(messages));
            }
        }

        return (string.Join(Environment.NewLine + Environment.NewLine, prose), blocks);
    }

    /// <summary>Validates one block's messages and wraps them for transport when they pass.</summary>
    internal A2UIGeneratedBlock ReadBlock(JsonArray rawMessages)
    {
        IReadOnlyList<A2UIMessage> messages;
        try
        {
            messages = A2UIJson.ListFromJsonNode(rawMessages);
        }
        catch (A2UIParseException ex)
        {
            logger.GeneratedBlockUnreadable(ex.Message);
            return A2UIGeneratedBlock.Invalid([ex.Message]);
        }

        // A block that creates a surface owns its whole tree. One that only updates a surface the
        // renderer already holds may refer to components it does not restate.
        var createsSurface = messages.Any(m => m is CreateSurfaceMessage);

        var result = new A2UIValidator(new A2UIValidationOptions
        {
            Catalog = catalog,
            Profile = profile,
            AllowDanglingReferences = !createsSurface,
            AllowOrphanComponents = !createsSurface,
            AllowMissingRoot = !createsSurface,
        }).Validate(messages);

        if (result.IsValid)
        {
            logger.GeneratedBlockAccepted(messages.Count);
            return A2UIGeneratedBlock.Valid(messages);
        }

        var errors = result.Errors.Select(e => e.ToString()).ToList();
        logger.GeneratedBlockInvalid(string.Join("; ", errors));
        return A2UIGeneratedBlock.Invalid(errors);
    }

    /// <summary>Removes every tagged block, including one left unclosed, keeping the prose.</summary>
    internal static string StripBlocks(string text)
    {
        var result = new System.Text.StringBuilder();
        var position = 0;

        while (position < text.Length)
        {
            var open = text.IndexOf(A2UIResponseTags.Open, position, StringComparison.Ordinal);
            if (open < 0)
            {
                result.Append(text, position, text.Length - position);
                break;
            }

            result.Append(text, position, open - position);

            var close = text.IndexOf(A2UIResponseTags.Close, open, StringComparison.Ordinal);
            if (close < 0)
            {
                break;
            }

            position = close + A2UIResponseTags.Close.Length;
        }

        return result.ToString().Trim();
    }
}
