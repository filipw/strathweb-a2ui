using System.Text.Json.Nodes;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Surfaces;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>Shorthands for changing a surface the renderer already holds.</summary>
public static class A2UISurfaceSinkExtensions
{
    /// <summary>Applies an update to a live surface.</summary>
    /// <param name="sink">Where to send it.</param>
    /// <param name="update">The update, built with <see cref="A2UISurfaceUpdate.For"/>.</param>
    public static void Emit(this IA2UISurfaceSink sink, A2UISurfaceUpdate update)
    {
        ArgumentNullException.ThrowIfNull(sink);
        ArgumentNullException.ThrowIfNull(update);

        sink.Emit(update.Build());
    }

    /// <summary>Writes a value into a live surface's data model.</summary>
    /// <param name="sink">Where to send it.</param>
    /// <param name="surfaceId">The surface to change.</param>
    /// <param name="path">A JSON Pointer, such as <c>/rating</c>.</param>
    /// <param name="value">The value. <see langword="null"/> writes JSON null.</param>
    public static void SetData(this IA2UISurfaceSink sink, string surfaceId, string path, JsonNode? value)
    {
        ArgumentNullException.ThrowIfNull(sink);
        sink.Emit([UpdateDataModelMessage.Set(surfaceId, path, value)]);
    }

    /// <summary>Deletes a value from a live surface's data model.</summary>
    /// <param name="sink">Where to send it.</param>
    /// <param name="surfaceId">The surface to change.</param>
    /// <param name="path">A JSON Pointer.</param>
    public static void RemoveData(this IA2UISurfaceSink sink, string surfaceId, string path)
    {
        ArgumentNullException.ThrowIfNull(sink);
        sink.Emit([UpdateDataModelMessage.Remove(surfaceId, path)]);
    }

    /// <summary>Takes a surface off the screen.</summary>
    /// <param name="sink">Where to send it.</param>
    /// <param name="surfaceId">The surface to remove.</param>
    public static void Delete(this IA2UISurfaceSink sink, string surfaceId)
    {
        ArgumentNullException.ThrowIfNull(sink);
        sink.Emit([new DeleteSurfaceMessage(surfaceId)]);
    }
}
