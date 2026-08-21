using System.Globalization;
using System.Security.Cryptography;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>Generates surface identifiers.</summary>
public static class A2UISurfaceId
{
    /// <summary>Generates an identifier.</summary>
    /// <param name="prefix">A readable prefix, such as the tool or agent name.</param>
    /// <returns>The identifier.</returns>
    public static string New(string prefix = "surface")
    {
        ArgumentNullException.ThrowIfNull(prefix);

        // var infers byte* here, not Span<byte>.
        Span<byte> bytes = stackalloc byte[10];
        RandomNumberGenerator.Fill(bytes);

#if NET9_0_OR_GREATER
        var suffix = Convert.ToHexStringLower(bytes);
#else
        var suffix = Convert.ToHexString(bytes).ToLowerInvariant();
#endif
        return prefix.Length == 0
            ? suffix
            : string.Create(CultureInfo.InvariantCulture, $"{prefix}_{suffix}");
    }
}
