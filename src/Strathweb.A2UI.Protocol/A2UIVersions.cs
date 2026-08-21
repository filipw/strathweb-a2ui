namespace Strathweb.A2UI;

/// <summary>Conversions between <see cref="A2UIVersion"/> and its wire representation.</summary>
public static class A2UIVersions
{
    /// <summary>The wire string for <paramref name="version"/>, for example <c>"v0.9.1"</c>.</summary>
    /// <param name="version">The version to convert.</param>
    /// <returns>The value to write into a message's <c>version</c> field.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a defined member.</exception>
    public static string ToWireString(A2UIVersion version) => version switch
    {
        A2UIVersion.V0_9 => "v0.9",
        A2UIVersion.V0_9_1 => "v0.9.1",
        A2UIVersion.V1_0 => "v1.0",
        _ => throw new ArgumentOutOfRangeException(nameof(version), version, "Unknown A2UI version."),
    };

    /// <summary>Parses a wire version string.</summary>
    /// <param name="value">The value of a message's <c>version</c> field.</param>
    /// <param name="version">The parsed version, when this method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="value"/> is a version this library recognises.</returns>
    public static bool TryParse(string? value, out A2UIVersion version)
    {
        switch (value)
        {
            case "v0.9":
                version = A2UIVersion.V0_9;
                return true;
            case "v0.9.1":
                version = A2UIVersion.V0_9_1;
                return true;
            case "v1.0":
                version = A2UIVersion.V1_0;
                return true;
            default:
                version = default;
                return false;
        }
    }
}
