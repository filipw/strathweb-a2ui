using System.Text.Json.Nodes;
using Strathweb.A2UI.Components;
using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Messages;

namespace Strathweb.A2UI.State;

/// <summary>
/// What one surface looks like after a sequence of messages has been applied.
/// </summary>
public sealed class A2UISurfaceState
{
    private readonly Dictionary<string, A2UIComponent> components = new(StringComparer.Ordinal);

    /// <summary>Creates an empty surface.</summary>
    /// <param name="surfaceId">The surface's identifier.</param>
    /// <param name="catalogId">The catalog its components come from.</param>
    public A2UISurfaceState(string surfaceId, string? catalogId = null)
    {
        SurfaceId = Throw.IfNullOrEmpty(surfaceId, nameof(surfaceId));
        CatalogId = catalogId;
    }

    /// <summary>The surface's identifier.</summary>
    public string SurfaceId { get; }

    /// <summary>The catalog its components come from.</summary>
    public string? CatalogId { get; private set; }

    /// <summary>The theme sent with <c>createSurface</c>, if any.</summary>
    public JsonObject? Theme { get; private set; }

    /// <summary>Whether the renderer was asked to send this surface's data model back on every message.</summary>
    public bool SendDataModel { get; private set; }

    /// <summary>Whether a <c>deleteSurface</c> has been applied.</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>The components currently on the surface, keyed by id.</summary>
    public IReadOnlyDictionary<string, A2UIComponent> Components => components;

    /// <summary>The surface's data model.</summary>
    public JsonNode? DataModel { get; private set; }

    /// <summary>The root component, or <see langword="null"/> while the surface is still incomplete.</summary>
    public A2UIComponent? Root => components.TryGetValue("root", out var root) ? root : null;

    /// <summary>Applies one message.</summary>
    /// <param name="message">The message to apply. Must target this surface.</param>
    /// <exception cref="ArgumentException"><paramref name="message"/> targets a different surface.</exception>
    public void Apply(A2UIMessage message)
    {
        Throw.IfNull(message, nameof(message));

        switch (message)
        {
            case CreateSurfaceMessage create:
                Require(create.SurfaceId);
                CatalogId = create.CatalogId ?? CatalogId;
                Theme = create.Theme;
                SendDataModel = create.SendDataModel ?? false;
                IsDeleted = false;
                break;

            case UpdateComponentsMessage update:
                Require(update.SurfaceId);
                foreach (var component in update.Components)
                {
                    // Later messages replace a component with the same id rather than adding to it.
                    components[component.Id] = component;
                }

                break;

            case UpdateDataModelMessage update:
                Require(update.SurfaceId);
                ApplyDataModel(update);
                break;

            case DeleteSurfaceMessage delete:
                Require(delete.SurfaceId);
                IsDeleted = true;
                components.Clear();
                DataModel = null;
                break;

            default:
                // Renderer-to-agent messages do not change what is on screen.
                break;
        }
    }

    /// <summary>Applies a sequence of messages in order.</summary>
    /// <param name="messages">The messages to apply.</param>
    public void Apply(IEnumerable<A2UIMessage> messages)
    {
        foreach (var message in Throw.IfNull(messages, nameof(messages)))
        {
            Apply(message);
        }
    }

    /// <summary>Reads a value out of the data model.</summary>
    /// <param name="path">A JSON Pointer. Empty or <c>/</c> returns the whole model.</param>
    /// <returns>The value, or <see langword="null"/> when nothing is there.</returns>
    public JsonNode? GetData(string path) => JsonPointer.Get(DataModel, Throw.IfNull(path, nameof(path)));

    private void ApplyDataModel(UpdateDataModelMessage message)
    {
        if (JsonPointer.IsWholeDocument(message.Path))
        {
            // An omitted value at the root clears the model rather than replacing it with null.
            DataModel = message.HasValue ? message.Value?.DeepClone() : null;
            return;
        }

        // Presence, not nullness, decides: an omitted value deletes, an explicit null writes null.
        DataModel = message.HasValue
            ? JsonPointer.Set(DataModel, message.Path!, message.Value?.DeepClone())
            : JsonPointer.Remove(DataModel, message.Path!);
    }

    private void Require(string surfaceId)
    {
        if (!string.Equals(surfaceId, SurfaceId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"This state tracks surface '{SurfaceId}', but the message targets '{surfaceId}'.",
                nameof(surfaceId));
        }
    }
}
