using System.Globalization;
using System.Text.Json.Nodes;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Strathweb.A2UI.Protocol.ConformanceTests;

/// <summary>
/// Converts a YAML document to <see cref="JsonNode"/>, resolving plain scalars the way the YAML core
/// schema does.
/// </summary>
internal static class YamlToJson
{
    internal static JsonArray LoadDocuments(string path)
    {
        var stream = new YamlStream();
        using (var reader = new StreamReader(path))
        {
            stream.Load(reader);
        }

        var result = new JsonArray();
        foreach (var document in stream.Documents)
        {
            if (Convert(document.RootNode) is JsonArray cases)
            {
                foreach (var item in cases.ToArray())
                {
                    cases.Remove(item);
                    result.Add(item);
                }
            }
        }

        return result;
    }

    private static JsonNode? Convert(YamlNode node)
    {
        switch (node)
        {
            case YamlMappingNode mapping:
                {
                    var obj = new JsonObject();
                    foreach (var pair in mapping.Children)
                    {
                        obj[((YamlScalarNode)pair.Key).Value ?? string.Empty] = Convert(pair.Value);
                    }

                    return obj;
                }

            case YamlSequenceNode sequence:
                {
                    var array = new JsonArray();
                    foreach (var item in sequence.Children)
                    {
                        array.Add(Convert(item));
                    }

                    return array;
                }

            case YamlScalarNode scalar:
                return ConvertScalar(scalar);

            default:
                return null;
        }
    }

    private static JsonValue? ConvertScalar(YamlScalarNode scalar)
    {
        var value = scalar.Value ?? string.Empty;

        // Quoting is how YAML says "this is text"; only plain scalars get type resolution.
        if (scalar.Style is not (ScalarStyle.Plain or ScalarStyle.Any))
        {
            return JsonValue.Create(value);
        }

        if (value.Length == 0 || value is "null" or "~" or "Null" or "NULL")
        {
            return null;
        }

        if (value is "true" or "True" or "TRUE")
        {
            return JsonValue.Create(true);
        }

        if (value is "false" or "False" or "FALSE")
        {
            return JsonValue.Create(false);
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
        {
            return JsonValue.Create(integer);
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) &&
            value.IndexOfAny(['.', 'e', 'E']) >= 0)
        {
            return JsonValue.Create(number);
        }

        return JsonValue.Create(value);
    }
}
