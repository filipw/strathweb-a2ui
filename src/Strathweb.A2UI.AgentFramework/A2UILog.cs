using Microsoft.Extensions.Logging;
using Strathweb.A2UI.Messages;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>
/// Everything this library reports. Most of these are the silent failures A2UI is prone to: a surface
/// that renders blank, a data model that never arrives, an action that is ignored.
/// </summary>
internal static partial class A2UILog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "A2UI surface {SurfaceId} created from catalog {CatalogId}.")]
    internal static partial void SurfaceCreated(this ILogger logger, string surfaceId, string? catalogId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "A2UI surface {SurfaceId} deleted.")]
    internal static partial void SurfaceDeleted(this ILogger logger, string surfaceId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "A2UI {Kind} message sent for surface {SurfaceId}.")]
    internal static partial void MessageSent(this ILogger logger, A2UIMessageKind kind, string surfaceId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Surfaces emitted in this run are not tracked: the agent was given no session.")]
    internal static partial void NoSession(this ILogger logger);

    [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "A2UI action {ActionName} received from surface {SurfaceId}.")]
    internal static partial void ActionReceived(this ILogger logger, string actionName, string surfaceId);

    [LoggerMessage(EventId = 11, Level = LogLevel.Warning, Message = "The renderer reported {Code} on surface {SurfaceId}: {Message}")]
    internal static partial void RendererError(this ILogger logger, string code, string surfaceId, string message);

    [LoggerMessage(EventId = 12, Level = LogLevel.Warning, Message = "An inbound A2UI message could not be read: {Reason}")]
    internal static partial void InboundMessageUnreadable(this ILogger logger, string reason);

    [LoggerMessage(EventId = 13, Level = LogLevel.Warning, Message = "An inbound A2UI part of {Bytes} bytes was ignored; the limit is {Limit} bytes.")]
    internal static partial void InboundPartTooLarge(this ILogger logger, int bytes, int limit);

    [LoggerMessage(EventId = 14, Level = LogLevel.Warning, Message = "The renderer's surface data models ({Bytes} bytes) were ignored; the limit is {Limit} bytes.")]
    internal static partial void DataModelTooLarge(this ILogger logger, int bytes, int limit);

    [LoggerMessage(EventId = 15, Level = LogLevel.Warning, Message = "Data model for surface {SurfaceId} ignored: this session did not create it. If it should have, the host is not persisting sessions between turns.")]
    internal static partial void SurfaceDataIgnored(this ILogger logger, string surfaceId);

    [LoggerMessage(EventId = 16, Level = LogLevel.Debug, Message = "The renderer can render catalogs: {CatalogIds}")]
    internal static partial void CapabilitiesRead(this ILogger logger, string catalogIds);

    [LoggerMessage(EventId = 17, Level = LogLevel.Warning, Message = "The renderer advertised A2UI capabilities under {Key}, which belongs to a different protocol version than this agent's {Expected}. Nothing it can render is known to this agent.")]
    internal static partial void CapabilitiesVersionMismatch(this ILogger logger, string key, string expected);

    [LoggerMessage(EventId = 20, Level = LogLevel.Warning, Message = "Surface {SurfaceId} uses catalog {CatalogId}, which the renderer did not list among the catalogs it can render ({Supported}). Policy: {Policy}.")]
    internal static partial void CatalogNotSupported(
        this ILogger logger,
        string surfaceId,
        string? catalogId,
        string supported,
        A2UIUnsupportedCatalogPolicy policy);
}
