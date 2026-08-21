using System.Globalization;
using System.Text.Json.Nodes;
using Strathweb.A2UI.Components;
using Strathweb.A2UI.Messages;

namespace Strathweb.A2UI.Serialization;

/// <summary>Converts between <see cref="A2UIMessage"/> and its wire JSON.</summary>
internal static class A2UIWire
{
    private const string VersionKey = "version";

    private static readonly string[] KnownMessageKeys =
    [
        "createSurface",
        "updateComponents",
        "updateDataModel",
        "deleteSurface",
        "action",
        "error",
    ];

    internal static JsonObject ToJson(A2UIMessage message)
    {
        var body = message switch
        {
            CreateSurfaceMessage create => WriteCreateSurface(create),
            UpdateComponentsMessage update => WriteUpdateComponents(update),
            UpdateDataModelMessage update => WriteUpdateDataModel(update),
            DeleteSurfaceMessage delete => new JsonObject { ["surfaceId"] = delete.SurfaceId },
            ActionMessage action => WriteAction(action),
            ErrorMessage error => WriteError(error),
            _ => throw new A2UIParseException($"Cannot serialize message type '{message.GetType()}'."),
        };

        return new JsonObject
        {
            [VersionKey] = A2UIVersions.ToWireString(message.Version),
            [message.WireKey] = body,
        };
    }

    internal static A2UIMessage FromJson(JsonNode? node)
    {
        if (node is not JsonObject obj)
        {
            throw new A2UIParseException("An A2UI message must be a JSON object.");
        }

        if (obj[VersionKey] is not JsonValue versionValue ||
            !versionValue.TryGetValue<string>(out var rawVersion))
        {
            throw new A2UIParseException("An A2UI message must carry a string 'version' property.");
        }

        if (!A2UIVersions.TryParse(rawVersion, out var version))
        {
            throw new A2UIParseException($"Unknown A2UI protocol version '{rawVersion}'.");
        }

        string? key = null;
        foreach (var candidate in KnownMessageKeys)
        {
            if (!obj.ContainsKey(candidate))
            {
                continue;
            }

            if (key is not null)
            {
                throw new A2UIParseException(
                    $"An A2UI message must carry exactly one message key; found both '{key}' and '{candidate}'.");
            }

            key = candidate;
        }

        if (key is null)
        {
            throw new A2UIParseException(
                "An A2UI message must carry exactly one message key (createSurface, updateComponents, " +
                "updateDataModel, deleteSurface, action or error).");
        }

        if (obj[key] is not JsonObject body)
        {
            throw new A2UIParseException($"The '{key}' property must be an object.");
        }

        return key switch
        {
            "createSurface" => ReadCreateSurface(body, version),
            "updateComponents" => ReadUpdateComponents(body, version),
            "updateDataModel" => ReadUpdateDataModel(body, version),
            "deleteSurface" => new DeleteSurfaceMessage(RequiredString(body, "surfaceId", key)) { Version = version },
            "action" => ReadAction(body, version),
            "error" => ReadError(body, version),
            _ => throw new A2UIParseException($"Unknown message key '{key}'."),
        };
    }

    private static JsonObject WriteCreateSurface(CreateSurfaceMessage message)
    {
        var body = new JsonObject { ["surfaceId"] = message.SurfaceId };

        if (message.CatalogId is not null)
        {
            body["catalogId"] = message.CatalogId;
        }

        if (message.Theme is { } theme)
        {
            body["theme"] = theme.DeepClone();
        }

        if (message.SendDataModel is { } sendDataModel)
        {
            body["sendDataModel"] = sendDataModel;
        }

        return body;
    }

    private static CreateSurfaceMessage ReadCreateSurface(JsonObject body, A2UIVersion version)
    {
        bool? sendDataModel = null;
        if (body["sendDataModel"] is JsonValue sendDataModelValue)
        {
            if (!sendDataModelValue.TryGetValue<bool>(out var parsed))
            {
                throw new A2UIParseException("'createSurface.sendDataModel' must be a boolean when present.");
            }

            sendDataModel = parsed;
        }

        return CreateSurfaceMessage.FromWire(
            RequiredString(body, "surfaceId", "createSurface"),
            OptionalString(body, "catalogId", "createSurface"),
            version,
            body["theme"] is JsonObject theme ? (JsonObject)theme.DeepClone() : null,
            sendDataModel);
    }

    private static JsonObject WriteUpdateComponents(UpdateComponentsMessage message)
    {
        var components = new JsonArray();
        foreach (var component in message.Components)
        {
            components.Add((JsonNode)component.ToJson());
        }

        return new JsonObject
        {
            ["surfaceId"] = message.SurfaceId,
            ["components"] = components,
        };
    }

    private static UpdateComponentsMessage ReadUpdateComponents(JsonObject body, A2UIVersion version)
    {
        var surfaceId = RequiredString(body, "surfaceId", "updateComponents");

        if (body["components"] is not JsonArray array)
        {
            throw new A2UIParseException("'updateComponents.components' must be an array.");
        }

        var components = new List<A2UIComponent>(array.Count);
        foreach (var item in array)
        {
            components.Add(A2UIComponent.FromJson(item));
        }

        return new UpdateComponentsMessage(surfaceId, components) { Version = version };
    }

    private static JsonObject WriteUpdateDataModel(UpdateDataModelMessage message)
    {
        var body = new JsonObject { ["surfaceId"] = message.SurfaceId };

        if (message.Path is not null)
        {
            body["path"] = message.Path;
        }

        // Presence, not nullness, is what separates a write from a delete.
        if (message.HasValue)
        {
            body["value"] = message.Value?.DeepClone();
        }

        return body;
    }

    private static UpdateDataModelMessage ReadUpdateDataModel(JsonObject body, A2UIVersion version)
    {
        var surfaceId = RequiredString(body, "surfaceId", "updateDataModel");
        var path = OptionalString(body, "path", "updateDataModel");
        var hasValue = body.ContainsKey("value");

        return UpdateDataModelMessage.FromWire(
            surfaceId,
            path,
            hasValue ? body["value"]?.DeepClone() : null,
            hasValue,
            version);
    }

    private static JsonObject WriteAction(ActionMessage message) => new()
    {
        ["name"] = message.Name,
        ["surfaceId"] = message.SurfaceId,
        ["sourceComponentId"] = message.SourceComponentId,
        ["timestamp"] = message.Timestamp.ToString("O", CultureInfo.InvariantCulture),
        ["context"] = message.Context.DeepClone(),
    };

    private static ActionMessage ReadAction(JsonObject body, A2UIVersion version)
    {
        var rawTimestamp = RequiredString(body, "timestamp", "action");
        if (!DateTimeOffset.TryParse(
                rawTimestamp,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var timestamp))
        {
            throw new A2UIParseException($"'action.timestamp' is not an ISO 8601 date-time: '{rawTimestamp}'.");
        }

        if (body["context"] is not JsonObject context)
        {
            throw new A2UIParseException("'action.context' must be an object.");
        }

        return ActionMessage.FromWire(
            RequiredString(body, "name", "action"),
            RequiredString(body, "surfaceId", "action"),
            RequiredString(body, "sourceComponentId", "action"),
            timestamp,
            (JsonObject)context.DeepClone(),
            version);
    }

    private static JsonObject WriteError(ErrorMessage message)
    {
        var body = new JsonObject();

        if (message.Extensions is { } extensions)
        {
            foreach (var pair in extensions)
            {
                body[pair.Key] = pair.Value?.DeepClone();
            }
        }

        body["code"] = message.Code;
        body["surfaceId"] = message.SurfaceId;
        body["message"] = message.Message;

        if (message.Path is not null)
        {
            body["path"] = message.Path;
        }

        return body;
    }

    private static ErrorMessage ReadError(JsonObject body, A2UIVersion version)
    {
        var code = RequiredString(body, "code", "error");

        JsonObject? extensions = null;
        foreach (var pair in body)
        {
            if (pair.Key is "code" or "surfaceId" or "message" or "path")
            {
                continue;
            }

            (extensions ??= [])[pair.Key] = pair.Value?.DeepClone();
        }

        return new ErrorMessage(code, RequiredString(body, "surfaceId", "error"), RequiredString(body, "message", "error"))
        {
            Version = version,
            Path = OptionalString(body, "path", "error"),
            Extensions = extensions,
        };
    }

    private static string RequiredString(JsonObject body, string name, string messageKey)
    {
        if (body[name] is not JsonValue value || !value.TryGetValue<string>(out var result))
        {
            throw new A2UIParseException($"'{messageKey}.{name}' is required and must be a string.");
        }

        return result;
    }

    private static string? OptionalString(JsonObject body, string name, string messageKey)
    {
        if (!body.ContainsKey(name))
        {
            return null;
        }

        if (body[name] is not JsonValue value || !value.TryGetValue<string>(out var result))
        {
            throw new A2UIParseException($"'{messageKey}.{name}' must be a string when present.");
        }

        return result;
    }
}
