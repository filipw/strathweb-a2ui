using System.Text.Json.Nodes;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Validation;

namespace Strathweb.A2UI.Protocol.ConformanceTests;

/// <summary>Runs the vendored <c>validate</c> cases against <see cref="A2UIValidator"/>.</summary>
public class ValidatorConformanceTests
{
    public static TheoryData<string> Cases()
    {
        var data = new TheoryData<string>();
        foreach (var name in ConformanceSuite.Cases
                     .Where(c => c.Action == "validate")
                     .Select(c => c.Name)
                     .Order(StringComparer.Ordinal))
        {
            data.Add(name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ValidateCase(string name)
    {
        var testCase = ConformanceSuite.Cases.Single(c => c.Name == name && c.Action == "validate");

        var skipReason = ConformanceSkips.Reason(testCase);
        Assert.SkipWhen(skipReason is not null, $"{name}: {skipReason}");

        var catalog = ReadCatalog(testCase);

        for (var i = 0; i < testCase.Steps.Count; i++)
        {
            var step = testCase.Steps[i];
            var validator = new A2UIValidator(new A2UIValidationOptions
            {
                Catalog = catalog,

                // The suite's inline catalogs are trimmed to the properties each case exercises, so a
                // strict unknown-property check would fail cases that are about something else.
                CheckCatalogConformance = false,
            });

            var result = validator.Validate(step["payload"]);
            var expectation = ConformanceExpectation.From(step["expect_error"]);

            if (expectation is null)
            {
                Assert.True(
                    result.IsValid,
                    $"{name} step {i}: expected the payload to validate, but:{Environment.NewLine}{result}");
                continue;
            }

            var problem = expectation.Explain(result);
            Assert.True(
                problem is null,
                $"{name} step {i}: {problem}. Actual errors:{Environment.NewLine}{result}");
        }
    }

    private static A2UICatalog? ReadCatalog(ConformanceCase testCase) => testCase.CatalogSchema switch
    {
        JsonValue value when value.TryGetValue<string>(out var path) =>
            A2UICatalog.FromJson(ConformanceSuite.ReadReferencedFile(path)),
        JsonObject inline => A2UICatalog.FromJson(inline),
        _ => null,
    };
}
