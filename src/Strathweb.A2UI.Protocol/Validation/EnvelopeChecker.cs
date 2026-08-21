using System.Text.Json.Nodes;
using Strathweb.A2UI.Messages;

namespace Strathweb.A2UI.Validation;

/// <summary>Checks the shape of each message envelope against the schema, on the raw JSON.</summary>
internal static class EnvelopeChecker
{
    private static readonly string[] MessageKeys =
    [
        "createSurface", "updateComponents", "updateDataModel", "deleteSurface", "action", "error",
    ];

    internal static void Check(
        JsonArray messages,
        A2UIVersionProfile profile,
        List<A2UIValidationError> errors)
    {
        for (var i = 0; i < messages.Count; i++)
        {
            var path = $"messages.{i}";

            if (messages[i] is not JsonObject message)
            {
                errors.Add(Error(A2UIErrorCodes.TypeMismatch, path, "Message must be an object."));
                continue;
            }

            CheckVersion(message, path, profile, errors);
            CheckBody(message, path, profile, errors);
        }
    }

    private static void CheckVersion(
        JsonObject message,
        string path,
        A2UIVersionProfile profile,
        List<A2UIValidationError> errors)
    {
        if (!message.ContainsKey("version"))
        {
            errors.Add(Error(A2UIErrorCodes.MissingField, $"{path}.version", "'version' is a required property."));
            return;
        }

        if (message["version"] is not JsonValue value || !value.TryGetValue<string>(out var raw))
        {
            errors.Add(Error(A2UIErrorCodes.TypeMismatch, $"{path}.version", "'version' must be a string."));
            return;
        }

        if (!A2UIVersions.TryParse(raw, out var version) || !Accepts(profile, version))
        {
            errors.Add(Error(
                A2UIErrorCodes.InvalidValue,
                $"{path}.version",
                $"'{raw}' is not a protocol version this profile accepts."));
        }
    }

    private static void CheckBody(
        JsonObject message,
        string path,
        A2UIVersionProfile profile,
        List<A2UIValidationError> errors)
    {
        string? key = null;
        foreach (var candidate in MessageKeys)
        {
            if (!message.ContainsKey(candidate))
            {
                continue;
            }

            if (key is not null)
            {
                errors.Add(Error(
                    A2UIErrorCodes.ExtraField,
                    $"{path}.{candidate}",
                    $"A message carries exactly one message key; found both '{key}' and '{candidate}'."));
                return;
            }

            key = candidate;
        }

        if (key is null)
        {
            errors.Add(Error(
                A2UIErrorCodes.InvalidValue,
                path,
                "A message must carry exactly one message key (createSurface, updateComponents, " +
                "updateDataModel, deleteSurface, action or error)."));
            return;
        }

        if (message[key] is not JsonObject body)
        {
            errors.Add(Error(A2UIErrorCodes.TypeMismatch, $"{path}.{key}", $"'{key}' must be an object."));
            return;
        }

        var bodyPath = $"{path}.{key}";
        switch (key)
        {
            case "createSurface":
                RequireString(body, "surfaceId", bodyPath, errors);
                if (RequiresCatalogId(profile))
                {
                    RequireString(body, "catalogId", bodyPath, errors);
                }
                else
                {
                    OptionalString(body, "catalogId", bodyPath, errors);
                }

                OptionalOfType(body, "theme", bodyPath, JsonValueKindOf.Object, errors);
                OptionalOfType(body, "sendDataModel", bodyPath, JsonValueKindOf.Boolean, errors);
                break;

            case "updateComponents":
                RequireString(body, "surfaceId", bodyPath, errors);
                CheckComponents(body, bodyPath, errors);
                break;

            case "updateDataModel":
                RequireString(body, "surfaceId", bodyPath, errors);
                OptionalString(body, "path", bodyPath, errors);
                break;

            case "deleteSurface":
                RequireString(body, "surfaceId", bodyPath, errors);
                break;

            case "action":
                RequireString(body, "name", bodyPath, errors);
                RequireString(body, "surfaceId", bodyPath, errors);
                RequireString(body, "sourceComponentId", bodyPath, errors);
                RequireString(body, "timestamp", bodyPath, errors);
                RequireOfType(body, "context", bodyPath, JsonValueKindOf.Object, errors);
                break;

            case "error":
                RequireString(body, "code", bodyPath, errors);
                RequireString(body, "surfaceId", bodyPath, errors);
                RequireString(body, "message", bodyPath, errors);
                if ((body["code"] as JsonValue)?.TryGetValue<string>(out var code) == true &&
                    code == ErrorMessage.ValidationFailedCode)
                {
                    RequireString(body, "path", bodyPath, errors);
                }

                break;
        }
    }

    private static void CheckComponents(JsonObject body, string bodyPath, List<A2UIValidationError> errors)
    {
        if (!body.ContainsKey("components"))
        {
            errors.Add(Error(
                A2UIErrorCodes.MissingField,
                $"{bodyPath}.components",
                "'components' is a required property."));
            return;
        }

        if (body["components"] is not JsonArray components)
        {
            errors.Add(Error(
                A2UIErrorCodes.TypeMismatch,
                $"{bodyPath}.components",
                "'components' must be an array."));
            return;
        }

        if (components.Count == 0)
        {
            errors.Add(Error(
                A2UIErrorCodes.InvalidValue,
                $"{bodyPath}.components",
                "'components' must contain at least one component."));
            return;
        }

        for (var i = 0; i < components.Count; i++)
        {
            var path = $"{bodyPath}.components.{i}";
            if (components[i] is not JsonObject component)
            {
                errors.Add(Error(A2UIErrorCodes.TypeMismatch, path, "A component must be an object."));
                continue;
            }

            RequireString(component, "id", path, errors);
            RequireString(component, "component", path, errors);
        }
    }

    private static bool Accepts(A2UIVersionProfile profile, A2UIVersion version)
    {
        foreach (var accepted in profile.AcceptedVersions)
        {
            if (accepted == version)
            {
                return true;
            }
        }

        return false;
    }

    private static bool RequiresCatalogId(A2UIVersionProfile profile) =>
        profile.EmitVersion is A2UIVersion.V0_9 or A2UIVersion.V0_9_1;

    private static void RequireString(
        JsonObject body,
        string name,
        string path,
        List<A2UIValidationError> errors)
    {
        if (!body.ContainsKey(name))
        {
            errors.Add(Error(
                A2UIErrorCodes.MissingField,
                $"{path}.{name}",
                $"'{name}' is a required property."));
            return;
        }

        OptionalString(body, name, path, errors);
    }

    private static void OptionalString(
        JsonObject body,
        string name,
        string path,
        List<A2UIValidationError> errors)
    {
        if (body.ContainsKey(name) &&
            (body[name] is not JsonValue value || !value.TryGetValue<string>(out _)))
        {
            errors.Add(Error(A2UIErrorCodes.TypeMismatch, $"{path}.{name}", $"'{name}' must be a string."));
        }
    }

    private static void RequireOfType(
        JsonObject body,
        string name,
        string path,
        JsonValueKindOf kind,
        List<A2UIValidationError> errors)
    {
        if (!body.ContainsKey(name))
        {
            errors.Add(Error(
                A2UIErrorCodes.MissingField,
                $"{path}.{name}",
                $"'{name}' is a required property."));
            return;
        }

        OptionalOfType(body, name, path, kind, errors);
    }

    private static void OptionalOfType(
        JsonObject body,
        string name,
        string path,
        JsonValueKindOf kind,
        List<A2UIValidationError> errors)
    {
        if (!body.ContainsKey(name))
        {
            return;
        }

        var matches = kind switch
        {
            JsonValueKindOf.Object => body[name] is JsonObject,
            JsonValueKindOf.Boolean => body[name] is JsonValue value && value.TryGetValue<bool>(out _),
            _ => true,
        };

        if (!matches)
        {
            errors.Add(Error(
                A2UIErrorCodes.TypeMismatch,
                $"{path}.{name}",
                $"'{name}' must be {(kind == JsonValueKindOf.Object ? "an object" : "a boolean")}."));
        }
    }

    private static A2UIValidationError Error(string code, string path, string message) =>
        new(A2UIValidationErrorCategory.Validation, code, path, message);

    private enum JsonValueKindOf
    {
        Object,
        Boolean,
    }
}
