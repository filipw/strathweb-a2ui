using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Validation;

namespace Strathweb.A2UI;

/// <summary>
/// Everything that differs between protocol versions, in one place: which version string to emit,
/// which A2A metadata keys and extension URI to use, and which property combinations are legal.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1707:Identifiers should not contain underscores",
    Justification = "Member names mirror the version strings and spec directory names they stand for.")]
public abstract class A2UIVersionProfile
{
    private protected A2UIVersionProfile()
    {
    }

    /// <summary>The profile for A2UI v0.9.1. Also accepts <c>v0.9</c> payloads.</summary>
    public static A2UIVersionProfile V0_9_1 { get; } = new V0_9_1Profile();

    /// <summary>The profile this library uses when the caller does not choose one.</summary>
    public static A2UIVersionProfile Default => V0_9_1;

    /// <summary>Looks up the profile that handles a wire version.</summary>
    /// <param name="version">The version to look up.</param>
    /// <returns>The profile.</returns>
    /// <exception cref="NotSupportedException">No profile implements <paramref name="version"/>.</exception>
    public static A2UIVersionProfile For(A2UIVersion version) => version switch
    {
        A2UIVersion.V0_9 or A2UIVersion.V0_9_1 => V0_9_1,
        A2UIVersion.V1_0 => throw new NotSupportedException(
            "A2UI v1.0 is a release candidate upstream and is not implemented yet. Use v0.9.1."),
        _ => throw new ArgumentOutOfRangeException(nameof(version), version, "Unknown A2UI version."),
    };

    /// <summary>The version string written into every message this profile emits.</summary>
    public abstract A2UIVersion EmitVersion { get; }

    /// <summary>The versions this profile accepts on incoming payloads.</summary>
    public abstract IReadOnlyCollection<A2UIVersion> AcceptedVersions { get; }

    /// <summary>The A2A message-metadata key carrying renderer capabilities.</summary>
    public abstract string CapabilitiesMetadataKey { get; }

    /// <summary>The A2A message-metadata key carrying the renderer's surface data models.</summary>
    public abstract string DataModelMetadataKey { get; }

    /// <summary>The key inside the capabilities object naming the protocol family, for example <c>v0.9</c>.</summary>
    public abstract string CapabilitiesVersionKey { get; }

    /// <summary>The A2A agent extension URI that advertises A2UI support at this version.</summary>
    public abstract string ExtensionUri { get; }

    /// <summary>The MIME type written on an A2UI A2A data part.</summary>
    public abstract string MediaType { get; }

    /// <summary>
    /// MIME types accepted when reading a part, newest first. Includes the deprecated
    /// <c>application/json+a2ui</c> spelling used by v0.8 and early v0.9 peers, which is never written.
    /// </summary>
    public abstract IReadOnlyList<string> AcceptedMediaTypes { get; }

    /// <summary>
    /// Checks a message for property combinations this version does not allow, such as a
    /// <c>createSurface</c> without a <c>catalogId</c> under v0.9.1.
    /// </summary>
    /// <param name="message">The message to check.</param>
    /// <param name="path">A pointer prefix for reported errors, for example <c>messages.0</c>.</param>
    /// <returns>The problems found, empty when the message is legal at this version.</returns>
    public abstract IReadOnlyList<A2UIValidationError> CheckVersionRules(A2UIMessage message, string path);

    private sealed class V0_9_1Profile : A2UIVersionProfile
    {
        private static readonly A2UIVersion[] Accepted = [A2UIVersion.V0_9, A2UIVersion.V0_9_1];
        private static readonly string[] MediaTypes = ["application/a2ui+json", "application/json+a2ui"];

        public override A2UIVersion EmitVersion => A2UIVersion.V0_9_1;

        public override IReadOnlyCollection<A2UIVersion> AcceptedVersions => Accepted;

        public override string CapabilitiesMetadataKey => "a2uiClientCapabilities";

        public override string DataModelMetadataKey => "a2uiClientDataModel";

        public override string CapabilitiesVersionKey => "v0.9";

        public override string ExtensionUri => "https://a2ui.org/a2a-extension/a2ui/v0.9.1";

        public override string MediaType => "application/a2ui+json";

        public override IReadOnlyList<string> AcceptedMediaTypes => MediaTypes;

        public override IReadOnlyList<A2UIValidationError> CheckVersionRules(A2UIMessage message, string path)
        {
            Throw.IfNull(message, nameof(message));
            Throw.IfNull(path, nameof(path));

            List<A2UIValidationError>? errors = null;

            if (Array.IndexOf(Accepted, message.Version) < 0)
            {
                (errors ??= []).Add(new A2UIValidationError(
                    A2UIValidationErrorCategory.Validation,
                    A2UIErrorCodes.InvalidValue,
                    $"{path}.version",
                    $"Version '{A2UIVersions.ToWireString(message.Version)}' is not accepted by the v0.9.1 profile."));
            }

            if (message is CreateSurfaceMessage { CatalogId: null or "" })
            {
                (errors ??= []).Add(new A2UIValidationError(
                    A2UIValidationErrorCategory.Validation,
                    A2UIErrorCodes.MissingField,
                    $"{path}.createSurface.catalogId",
                    "'catalogId' is required on createSurface under v0.9.1."));
            }

            if (message is ErrorMessage { Code: ErrorMessage.ValidationFailedCode, Path: null or "" })
            {
                (errors ??= []).Add(new A2UIValidationError(
                    A2UIValidationErrorCategory.Validation,
                    A2UIErrorCodes.MissingField,
                    $"{path}.error.path",
                    $"'path' is required on an error with code '{ErrorMessage.ValidationFailedCode}'."));
            }

            return (IReadOnlyList<A2UIValidationError>?)errors ?? [];
        }
    }
}
