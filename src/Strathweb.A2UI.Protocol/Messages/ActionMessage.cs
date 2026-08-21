using System.Text.Json.Nodes;
using Strathweb.A2UI.Internal;

namespace Strathweb.A2UI.Messages;

/// <summary>Reports a user interaction back to the agent. Sent by the renderer.</summary>
public sealed class ActionMessage : A2UIMessage
{
    /// <summary>Creates an <c>action</c> message.</summary>
    /// <param name="name">The action name, taken from the component's <c>action.event.name</c>.</param>
    /// <param name="surfaceId">The surface the interaction happened on.</param>
    /// <param name="sourceComponentId">The component that triggered the interaction.</param>
    /// <param name="timestamp">When it happened.</param>
    /// <param name="context">
    /// The component's event context with every data binding already resolved by the renderer.
    /// </param>
    public ActionMessage(
        string name,
        string surfaceId,
        string sourceComponentId,
        DateTimeOffset timestamp,
        JsonObject context)
    {
        Name = Throw.IfNullOrEmpty(name, nameof(name));
        SurfaceId = Throw.IfNullOrEmpty(surfaceId, nameof(surfaceId));
        SourceComponentId = Throw.IfNullOrEmpty(sourceComponentId, nameof(sourceComponentId));
        Timestamp = timestamp;
        Context = Throw.IfNull(context, nameof(context));
    }

    /// <inheritdoc />
    public override A2UIMessageKind Kind => A2UIMessageKind.Action;

    /// <inheritdoc />
    public override A2UIMessageDirection Direction => A2UIMessageDirection.RendererToAgent;

    /// <inheritdoc />
    public override string WireKey => "action";

    /// <summary>The action name the surface author chose.</summary>
    public string Name { get; }

    /// <summary>The surface the interaction happened on.</summary>
    public string SurfaceId { get; }

    /// <summary>The component that triggered the interaction.</summary>
    public string SourceComponentId { get; }

    /// <summary>When the interaction happened.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// The resolved event context. Values are concrete JSON; the renderer resolves bindings before
    /// sending.
    /// </summary>
    public JsonObject Context { get; }

    /// <summary>
    /// Builds a message from a payload that may be missing fields the schema requires, so that the
    /// validator can report a precise error instead of the parser throwing an opaque one.
    /// </summary>
    internal static ActionMessage FromWire(
        string name,
        string surfaceId,
        string sourceComponentId,
        DateTimeOffset timestamp,
        JsonObject context,
        A2UIVersion version) =>
        new(name, surfaceId, sourceComponentId, timestamp, context) { Version = version };
}
