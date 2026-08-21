using System.Text.Json.Nodes;
using Strathweb.A2UI.Components;
using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Validation;

/// <summary>Checks an A2UI payload before it reaches a renderer.</summary>
public sealed class A2UIValidator
{
    private readonly A2UIValidationOptions options;

    /// <summary>Creates a validator.</summary>
    /// <param name="options">The rules to apply. Defaults to <see cref="A2UIValidationOptions.Default"/>.</param>
    public A2UIValidator(A2UIValidationOptions? options = null)
    {
        this.options = options ?? A2UIValidationOptions.Default;
    }

    /// <summary>Validates a raw payload: one message object, or an array of them.</summary>
    /// <param name="payload">The JSON to check.</param>
    /// <returns>Every problem found.</returns>
    public A2UIValidationResult Validate(JsonNode? payload)
    {
        var messages = payload switch
        {
            JsonArray array => array,
            null => null,
            _ => new JsonArray(payload.DeepClone()),
        };

        if (messages is null)
        {
            return new A2UIValidationResult(
            [
                new A2UIValidationError(
                    A2UIValidationErrorCategory.Validation,
                    A2UIErrorCodes.TypeMismatch,
                    "messages",
                    "A payload must be a message object or an array of them."),
            ]);
        }

        var errors = new List<A2UIValidationError>();

        EnvelopeChecker.Check(messages, options.Profile, errors);
        RecursionAndPathChecker.Check(messages, options, errors);
        CheckComponents(messages, errors);

        return errors.Count == 0 ? A2UIValidationResult.Valid : new A2UIValidationResult(errors);
    }

    /// <summary>Validates messages that have already been parsed.</summary>
    /// <param name="messages">The messages to check.</param>
    /// <returns>Every problem found.</returns>
    public A2UIValidationResult Validate(IEnumerable<A2UIMessage> messages) =>
        Validate(A2UIJson.ToJsonArray(Throw.IfNull(messages, nameof(messages))));

    private void CheckComponents(JsonArray messages, List<A2UIValidationError> errors)
    {
        // A payload that creates the surface owns its whole tree, so it must name a root. One that
        // only updates an existing surface is adding to a tree the renderer already holds.
        var createsSurface = false;
        foreach (var message in messages)
        {
            if (message is JsonObject obj && obj.ContainsKey("createSurface"))
            {
                createsSurface = true;
                break;
            }
        }

        var allowMissingRoot = options.AllowMissingRoot ?? !createsSurface;

        for (var i = 0; i < messages.Count; i++)
        {
            if (messages[i] is not JsonObject message ||
                message["updateComponents"] is not JsonObject body ||
                body["components"] is not JsonArray rawComponents ||
                rawComponents.Count == 0)
            {
                continue;
            }

            var path = $"messages.{i}.updateComponents.components";
            var components = new List<A2UIComponent>(rawComponents.Count);
            var parsed = true;

            for (var c = 0; c < rawComponents.Count; c++)
            {
                try
                {
                    components.Add(A2UIComponent.FromJson(rawComponents[c]));
                }
                catch (A2UIValueException)
                {
                    // The envelope check already reported the shape problem in detail.
                    parsed = false;
                }
            }

            if (!parsed)
            {
                continue;
            }

            if (options.CheckCatalogConformance && options.Catalog is { } catalog)
            {
                CatalogConformanceChecker.Check(components, path, catalog, errors);
            }

            ComponentGraphChecker.Check(components, path, options, allowMissingRoot, errors);
        }
    }
}
