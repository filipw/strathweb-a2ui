using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Strathweb.A2UI.Validation;

namespace Strathweb.A2UI.Protocol.ConformanceTests;

/// <summary>What a case says should go wrong: a message pattern, a category, or specific error details.</summary>
internal sealed class ConformanceExpectation
{
    private readonly string? messagePattern;
    private readonly A2UIValidationErrorCategory? category;
    private readonly IReadOnlyList<(string Path, string Code)> details;

    private ConformanceExpectation(
        string? messagePattern,
        A2UIValidationErrorCategory? category,
        IReadOnlyList<(string, string)> details)
    {
        this.messagePattern = messagePattern;
        this.category = category;
        this.details = details;
    }

    internal static ConformanceExpectation? From(JsonNode? expectError)
    {
        switch (expectError)
        {
            case null:
                return null;

            case JsonValue value when value.TryGetValue<string>(out var pattern):
                return new ConformanceExpectation(pattern, null, []);

            case JsonObject obj:
                {
                    var details = new List<(string, string)>();
                    if (obj["details"] is JsonArray array)
                    {
                        foreach (var detail in array.OfType<JsonObject>())
                        {
                            details.Add(((string)detail["path"]!, (string)detail["code"]!));
                        }
                    }

                    return new ConformanceExpectation(
                        (string?)obj["message"],
                        MapCategory((string?)obj["category"]),
                        details);
                }

            default:
                throw new InvalidOperationException($"Unsupported expect_error shape: {expectError.ToJsonString()}");
        }
    }

    /// <summary>
    /// Describes why <paramref name="result"/> does not meet this expectation, or
    /// <see langword="null"/> when it does.
    /// </summary>
    internal string? Explain(A2UIValidationResult result)
    {
        if (result.IsValid)
        {
            return "expected validation to fail, but it succeeded";
        }

        if (category is { } expectedCategory && !result.Errors.Any(e => e.Category == expectedCategory))
        {
            return $"expected an error of category {expectedCategory}";
        }

        if (messagePattern is { } pattern &&
            !result.Errors.Any(e => Regex.IsMatch(e.Message, pattern, RegexOptions.CultureInvariant)))
        {
            return $"expected an error matching /{pattern}/";
        }

        foreach (var (path, code) in details)
        {
            if (!result.Errors.Any(e =>
                    string.Equals(e.Path, path, StringComparison.Ordinal) &&
                    string.Equals(e.Code, code, StringComparison.Ordinal)))
            {
                return $"expected error '{code}' at '{path}'";
            }
        }

        return null;
    }

    private static A2UIValidationErrorCategory? MapCategory(string? category) => category switch
    {
        "ValidationError" => A2UIValidationErrorCategory.Validation,
        "IntegrityError" => A2UIValidationErrorCategory.Integrity,
        "RecursionError" => A2UIValidationErrorCategory.Recursion,
        "CatalogError" => A2UIValidationErrorCategory.Catalog,
        _ => null,
    };
}
