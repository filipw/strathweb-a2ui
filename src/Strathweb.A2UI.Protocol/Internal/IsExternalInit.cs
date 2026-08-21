#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices;

/// <summary>
/// Compiler-recognised marker enabling <c>init</c> accessors. Shipped in the framework from .NET 5
/// onwards; declared here so the netstandard2.0 target can use the same source.
/// </summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
internal static class IsExternalInit
{
}
#endif
