using System.Text.Json.Nodes;
using Strathweb.A2UI.Messages;

using Strathweb.A2UI.TestSupport;
namespace Strathweb.A2UI.Protocol.UnitTests;

/// <summary>
/// Reads every worked example shipped with the Basic Catalog. These are the closest thing to a
/// golden-file suite the specification provides: real surfaces, written by the spec authors.
/// </summary>
public class SpecExampleTests
{
    public static TheoryData<string> ExampleFiles()
    {
        var data = new TheoryData<string>();
        foreach (var file in Directory.EnumerateFiles(
                     Path.Combine(SpecFiles.V0_9_1, "catalogs", "basic", "examples"),
                     "*.json").Order(StringComparer.Ordinal))
        {
            data.Add(Path.GetFileName(file));
        }

        return data;
    }

    [Fact]
    public void TheSpecShipsExamples()
    {
        Assert.NotEmpty(ExampleFiles());
    }

    [Theory]
    [MemberData(nameof(ExampleFiles))]
    public void Example_RoundTripsThroughTheModel(string fileName)
    {
        foreach (var original in ReadMessages(fileName))
        {
            var message = A2UIJson.FromJsonNode(original);
            var written = A2UIJson.ToJsonObject(message);

            Assert.True(
                JsonNode.DeepEquals(Normalize(original), Normalize(written)),
                $"""
                 {fileName} did not round-trip.
                 original: {original.ToJsonString()}
                 written:  {written.ToJsonString()}
                 """);
        }
    }

    [Theory]
    [MemberData(nameof(ExampleFiles))]
    public void Example_ValidatesAgainstTheVendoredSchema(string fileName)
    {
        foreach (var original in ReadMessages(fileName))
        {
            SpecSchemas.AssertValid(SpecSchemas.AgentToRenderer, original);
            SpecSchemas.AssertValid(SpecSchemas.AgentToRenderer, A2UIJson.ToJsonObject(A2UIJson.FromJsonNode(original)));
        }
    }

    [Theory]
    [MemberData(nameof(ExampleFiles))]
    public void Example_DeclaresExactlyOneRootComponent(string fileName)
    {
        var roots = ReadMessages(fileName)
            .Select(A2UIJson.FromJsonNode)
            .OfType<UpdateComponentsMessage>()
            .SelectMany(m => m.Components)
            .Count(c => c.Id == "root");

        Assert.Equal(1, roots);
    }

    private static List<JsonObject> ReadMessages(string fileName)
    {
        var document = SpecFiles.ReadJson("v0_9_1", "catalogs", "basic", "examples", fileName).AsObject();
        var messages = document["messages"]?.AsArray()
            ?? throw new InvalidOperationException($"'{fileName}' has no 'messages' array.");

        return [.. messages.Select(m => m!.AsObject())];
    }

    /// <summary>
    /// Property order is not part of the wire contract, so comparison is by content. The model writes
    /// a canonical order rather than preserving the source's.
    /// </summary>
    private static JsonNode Normalize(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                {
                    var sorted = new JsonObject();
                    foreach (var pair in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
                    {
                        sorted[pair.Key] = pair.Value is null ? null : Normalize(pair.Value);
                    }

                    return sorted;
                }

            case JsonArray array:
                {
                    var result = new JsonArray();
                    foreach (var item in array)
                    {
                        result.Add(item is null ? null : Normalize(item));
                    }

                    return result;
                }

            default:
                return node.DeepClone();
        }
    }
}
