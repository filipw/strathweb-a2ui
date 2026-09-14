using System.Globalization;
using System.Text.Json.Nodes;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Messages;

namespace Strathweb.A2UI.Rendering;

/// <summary>
/// Every surface a renderer currently shows, and the renderer-side state a conversation needs: which
/// catalogs it can draw, and which data models go back to the agent with the next message.
/// </summary>
public sealed class A2UIRendererSession
{
    private readonly Dictionary<string, A2UIRenderSurface> surfaces = new(StringComparer.Ordinal);
    private readonly List<A2UIRenderSurface> order = [];
    private readonly CultureInfo culture;

    /// <summary>Creates a session that renders the Basic Catalog.</summary>
    /// <param name="profile">The protocol version. Defaults to v0.9.1.</param>
    /// <param name="culture">The culture used when formatting values. Defaults to the invariant culture.</param>
    public A2UIRendererSession(A2UIVersionProfile? profile = null, CultureInfo? culture = null)
    {
        Profile = profile ?? A2UIVersionProfile.Default;
        this.culture = culture ?? CultureInfo.InvariantCulture;
        SupportedCatalogIds = [A2UICatalogs.Basic(Profile.EmitVersion).CatalogId];
    }

    /// <summary>Raised when a surface is created. Also raised when an existing id is reused.</summary>
    public event EventHandler<A2UIRenderSurface>? SurfaceCreated;

    /// <summary>Raised when a surface is deleted.</summary>
    public event EventHandler<A2UIRenderSurface>? SurfaceDeleted;

    /// <summary>Raised after any surface changed, from the agent or from a local edit.</summary>
    public event EventHandler<A2UIRenderSurface>? SurfaceChanged;

    /// <summary>Raised when a surface asks the host to open a URL.</summary>
    public event EventHandler<A2UIOpenUrlEventArgs>? OpenUrlRequested;

    /// <summary>The protocol version this renderer speaks.</summary>
    public A2UIVersionProfile Profile { get; }

    /// <summary>The catalogs this renderer can draw. Advertised to the agent in message metadata.</summary>
    public IReadOnlyList<string> SupportedCatalogIds { get; init; }

    /// <summary>The live surfaces, oldest first.</summary>
    public IReadOnlyList<A2UIRenderSurface> Surfaces => order;

    /// <summary>Applies messages from the agent, routing each to its surface.</summary>
    /// <param name="messages">The messages, in order.</param>
    /// <returns>How many messages targeted a surface this session does not have, and were ignored.</returns>
    public int Apply(IEnumerable<A2UIMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var ignored = 0;
        foreach (var message in messages)
        {
            if (!Apply(message))
            {
                ignored++;
            }
        }

        return ignored;
    }

    /// <summary>Applies one message from the agent.</summary>
    /// <param name="message">The message.</param>
    /// <returns><see langword="false"/> when the message targets a surface this session does not have.</returns>
    public bool Apply(A2UIMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        switch (message)
        {
            case CreateSurfaceMessage create:
                Create(create);
                return true;

            case DeleteSurfaceMessage delete:
                return Delete(delete.SurfaceId);

            case UpdateComponentsMessage update:
                return Route(update.SurfaceId, update);

            case UpdateDataModelMessage update:
                return Route(update.SurfaceId, update);

            default:
                // Renderer-to-agent messages do not change what is on screen.
                return true;
        }
    }

    /// <summary>Looks up a surface.</summary>
    /// <param name="surfaceId">The surface id.</param>
    /// <param name="surface">The surface, when this method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when the surface is live.</returns>
    public bool TryGet(string surfaceId, out A2UIRenderSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surfaceId);
        return surfaces.TryGetValue(surfaceId, out surface!);
    }

    /// <summary>
    /// The data models to send back with the next message: one per surface the agent created with
    /// <c>sendDataModel</c>, keyed by surface id.
    /// </summary>
    /// <returns>The data models. Empty when no surface asked for it.</returns>
    public IReadOnlyDictionary<string, JsonNode?> DataModels()
    {
        var models = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        foreach (var surface in order)
        {
            if (surface.SendDataModel)
            {
                models[surface.SurfaceId] = surface.DataModelSnapshot();
            }
        }

        return models;
    }

    /// <summary>Removes every surface, for a renderer that starts a new conversation.</summary>
    public void Clear()
    {
        foreach (var surface in order.ToArray())
        {
            Delete(surface.SurfaceId);
        }
    }

    private void Create(CreateSurfaceMessage create)
    {
        // Reusing an id replaces whatever is on screen, per the specification.
        if (surfaces.TryGetValue(create.SurfaceId, out var existing))
        {
            Detach(existing);
            order.Remove(existing);
            surfaces.Remove(create.SurfaceId);
        }

        var surface = new A2UIRenderSurface(create.SurfaceId, create.CatalogId, culture);
        surface.Apply(create);
        surface.Changed += OnSurfaceChanged;
        surface.OpenUrlRequested += OnOpenUrlRequested;

        surfaces[create.SurfaceId] = surface;
        order.Add(surface);
        SurfaceCreated?.Invoke(this, surface);
    }

    private bool Delete(string surfaceId)
    {
        if (!surfaces.TryGetValue(surfaceId, out var surface))
        {
            return false;
        }

        surface.Apply(new DeleteSurfaceMessage(surfaceId) { Version = Profile.EmitVersion });
        Detach(surface);
        surfaces.Remove(surfaceId);
        order.Remove(surface);
        SurfaceDeleted?.Invoke(this, surface);
        return true;
    }

    private bool Route(string surfaceId, A2UIMessage message)
    {
        if (!surfaces.TryGetValue(surfaceId, out var surface))
        {
            return false;
        }

        surface.Apply(message);
        return true;
    }

    private void Detach(A2UIRenderSurface surface)
    {
        surface.Changed -= OnSurfaceChanged;
        surface.OpenUrlRequested -= OnOpenUrlRequested;
    }

    private void OnSurfaceChanged(object? sender, EventArgs e)
    {
        if (sender is A2UIRenderSurface surface)
        {
            SurfaceChanged?.Invoke(this, surface);
        }
    }

    private void OnOpenUrlRequested(object? sender, A2UIOpenUrlEventArgs e) => OpenUrlRequested?.Invoke(this, e);
}
