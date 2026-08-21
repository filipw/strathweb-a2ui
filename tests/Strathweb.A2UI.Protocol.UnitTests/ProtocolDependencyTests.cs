
namespace Strathweb.A2UI.Protocol.UnitTests;

/// <summary>The protocol assembly depends on nothing but the BCL. See AGENTS.md.</summary>
public class ProtocolDependencyTests
{
    [Fact]
    public void ProtocolAssembly_ReferencesOnlyTheBaseClassLibrary()
    {
        var referenced = typeof(A2UIJson).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => !name.StartsWith("System.", StringComparison.Ordinal) &&
                           name is not ("System" or "netstandard" or "mscorlib"))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(referenced);
    }
}
