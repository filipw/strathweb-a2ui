using System.Text.Json.Nodes;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.Messages;

namespace Strathweb.A2UI.Protocol.ConformanceTests;

/// <summary>Runs the vendored A2A extension cases that describe behaviour this library provides.</summary>
public class A2AConformanceTests
{
    private static readonly string[] Actions =
    [
        "create_a2ui_part", "is_a2ui_part", "try_activate", "try_activate_extension", "select_newest",
    ];

    public static TheoryData<string> Cases()
    {
        var data = new TheoryData<string>();
        foreach (var name in ConformanceSuite.Cases
                     .Where(c => Actions.Contains(c.Action, StringComparer.Ordinal))
                     .Select(c => c.Name)
                     .Order(StringComparer.Ordinal))
        {
            data.Add(name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void RunCase(string name)
    {
        var testCase = ConformanceSuite.Cases.Single(c => c.Name == name);

        var skipReason = ConformanceSkips.Reason(testCase);
        Assert.SkipWhen(skipReason is not null, $"{name}: {skipReason}");

        var args = testCase.Source["args"]!.AsObject();
        var expect = testCase.Source["expect"];

        switch (testCase.Action)
        {
            case "create_a2ui_part":
                {
                    var part = A2UIParts.Create(new DeleteSurfaceMessage("s1"));
                    var mimeType = part.Metadata![A2UIParts.MimeTypeMetadataKey].GetString();

                    Assert.Equal((string?)expect!["mime_type"], mimeType);
                    break;
                }

            case "is_a2ui_part":
                Assert.Equal((bool)expect!, A2UIMediaTypes.IsA2UI((string?)args["mime_type"]));
                break;

            case "try_activate_extension":
                Assert.Equal((bool)expect!, A2UIExtensionUris.ContainsA2UIExtension(Strings(args["uris"])));
                break;

            case "try_activate":
                {
                    var activated = A2UIExtensionUris.Activate(
                        Strings(args["requested"]),
                        Strings(args["advertised"]));

                    Assert.Equal((string?)expect!["activated"], activated);

                    if ((string?)expect["version"] is { } expectedVersion)
                    {
                        Assert.True(A2UIExtensionUris.TryGetVersion(activated, out var version));
                        Assert.Equal(expectedVersion, version);
                    }

                    break;
                }

            case "select_newest":
                {
                    // Only versions both sides know are candidates, compared by numeric segment.
                    var newest = A2UIExtensionUris.Activate(
                        Strings(args["requested"]),
                        Strings(args["advertised"]));

                    Assert.Equal((string?)expect!["newest"], newest);
                    break;
                }

            default:
                Assert.Fail($"No runner for action '{testCase.Action}'.");
                break;
        }
    }

    private static IReadOnlyList<string> Strings(JsonNode? node) =>
        node is JsonArray array ? [.. array.Select(n => (string)n!)] : [];
}
