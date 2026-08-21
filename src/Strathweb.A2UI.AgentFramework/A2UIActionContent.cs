using A2A;
using Microsoft.Extensions.AI;
using Strathweb.A2UI.Messages;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>A user interaction that arrived from a renderer, in a form tools can read.</summary>
public sealed class A2UIActionContent : AIContent
{
    /// <summary>Wraps an inbound action.</summary>
    /// <param name="action">The parsed action.</param>
    /// <param name="part">The A2A part it arrived in.</param>
    public A2UIActionContent(ActionMessage action, Part? part = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        Action = action;
        RawRepresentation = part;
    }

    /// <summary>What the user did.</summary>
    public ActionMessage Action { get; }

    /// <summary>The action's name, as the surface author chose it.</summary>
    public string Name => Action.Name;

    /// <summary>The surface the interaction happened on.</summary>
    public string SurfaceId => Action.SurfaceId;
}
