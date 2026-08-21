using System.Security.Cryptography;

namespace Strathweb.A2UI.Surfaces;

/// <summary>Generates component ids for components added to a surface the renderer already holds.</summary>
internal static class A2UIComponentId
{
    internal static string New(string componentType)
    {
        var bytes = new byte[4];
#if NETSTANDARD2_0
        using (var random = RandomNumberGenerator.Create())
        {
            random.GetBytes(bytes);
        }
#else
        RandomNumberGenerator.Fill(bytes);
#endif

        var prefix = componentType.Length == 0
            ? "component"
            : char.ToLowerInvariant(componentType[0]) + componentType.Substring(1);

#if NETSTANDARD2_0
        var suffix = BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
#elif NET9_0_OR_GREATER
        var suffix = Convert.ToHexStringLower(bytes);
#else
        var suffix = Convert.ToHexString(bytes).ToLowerInvariant();
#endif

        return prefix + "_" + suffix;
    }
}
