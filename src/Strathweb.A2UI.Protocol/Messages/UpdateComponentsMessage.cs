using Strathweb.A2UI.Components;
using Strathweb.A2UI.Internal;

namespace Strathweb.A2UI.Messages;

/// <summary>
/// Adds or replaces components on an existing surface. Components already on the surface and not
/// named in this message are left alone; a component with an id that is already present is replaced.
/// </summary>
public sealed class UpdateComponentsMessage : A2UIMessage
{
    /// <summary>Creates an <c>updateComponents</c> message.</summary>
    /// <param name="surfaceId">The surface to update.</param>
    /// <param name="components">The components to add or replace. The schema requires at least one.</param>
    public UpdateComponentsMessage(string surfaceId, IEnumerable<A2UIComponent> components)
    {
        SurfaceId = Throw.IfNullOrEmpty(surfaceId, nameof(surfaceId));
        Components = Throw.IfNull(components, nameof(components)).ToArray();
    }

    /// <inheritdoc />
    public override A2UIMessageKind Kind => A2UIMessageKind.UpdateComponents;

    /// <inheritdoc />
    public override A2UIMessageDirection Direction => A2UIMessageDirection.AgentToRenderer;

    /// <inheritdoc />
    public override string WireKey => "updateComponents";

    /// <summary>The surface these components belong to.</summary>
    public string SurfaceId { get; }

    /// <summary>The components carried by this message.</summary>
    public IReadOnlyList<A2UIComponent> Components { get; }
}
