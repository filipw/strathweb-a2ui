using Strathweb.A2UI.Internal;

namespace Strathweb.A2UI.Messages;

/// <summary>Tells the renderer to delete a surface.</summary>
public sealed class DeleteSurfaceMessage : A2UIMessage
{
    /// <summary>Creates a <c>deleteSurface</c> message.</summary>
    /// <param name="surfaceId">The surface to delete.</param>
    public DeleteSurfaceMessage(string surfaceId)
    {
        SurfaceId = Throw.IfNullOrEmpty(surfaceId, nameof(surfaceId));
    }

    /// <inheritdoc />
    public override A2UIMessageKind Kind => A2UIMessageKind.DeleteSurface;

    /// <inheritdoc />
    public override A2UIMessageDirection Direction => A2UIMessageDirection.AgentToRenderer;

    /// <inheritdoc />
    public override string WireKey => "deleteSurface";

    /// <summary>The identifier of the surface to delete.</summary>
    public string SurfaceId { get; }
}
