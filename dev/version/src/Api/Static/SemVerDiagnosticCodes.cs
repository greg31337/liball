namespace version;

/// <summary>
/// Defines stable diagnostic codes emitted by SemVer operations.
/// </summary>
/// <remarks>
/// Compare these constants with diagnostic codes returned by result objects; messages may provide additional context for a specific input.
/// </remarks>
public static class SemVerDiagnosticCodes
{
    /// <summary>
    /// Identifies the null input condition.
    /// </summary>
    public const string NullInput = "SEMVER_NULL_INPUT";
    /// <summary>
    /// Identifies the input too long condition.
    /// </summary>
    public const string InputTooLong = "SEMVER_INPUT_TOO_LONG";
    /// <summary>
    /// Identifies the missing major condition.
    /// </summary>
    public const string MissingMajor = "SEMVER_MISSING_MAJOR";
    /// <summary>
    /// Identifies the missing minor condition.
    /// </summary>
    public const string MissingMinor = "SEMVER_MISSING_MINOR";
    /// <summary>
    /// Identifies the missing patch condition.
    /// </summary>
    public const string MissingPatch = "SEMVER_MISSING_PATCH";
    /// <summary>
    /// Identifies the expected dot condition.
    /// </summary>
    public const string ExpectedDot = "SEMVER_EXPECTED_DOT";
    /// <summary>
    /// Identifies the numeric component leading zero condition.
    /// </summary>
    public const string NumericComponentLeadingZero = "SEMVER_NUMERIC_COMPONENT_LEADING_ZERO";
    /// <summary>
    /// Identifies the prerelease leading zero condition.
    /// </summary>
    public const string PrereleaseLeadingZero = "SEMVER_PRERELEASE_LEADING_ZERO";
    /// <summary>
    /// Identifies the empty prerelease identifier condition.
    /// </summary>
    public const string EmptyPrereleaseIdentifier = "SEMVER_EMPTY_PRERELEASE_IDENTIFIER";
    /// <summary>
    /// Identifies the empty build identifier condition.
    /// </summary>
    public const string EmptyBuildIdentifier = "SEMVER_EMPTY_BUILD_IDENTIFIER";
    /// <summary>
    /// Identifies the illegal character condition.
    /// </summary>
    public const string IllegalCharacter = "SEMVER_ILLEGAL_CHARACTER";
    /// <summary>
    /// Identifies the malformed prerelease condition.
    /// </summary>
    public const string MalformedPrerelease = "SEMVER_MALFORMED_PRERELEASE";
    /// <summary>
    /// Identifies the malformed build condition.
    /// </summary>
    public const string MalformedBuild = "SEMVER_MALFORMED_BUILD";
    /// <summary>
    /// Identifies the trailing content condition.
    /// </summary>
    public const string TrailingContent = "SEMVER_TRAILING_CONTENT";
    /// <summary>
    /// Identifies the no coercible version condition.
    /// </summary>
    public const string NoCoercibleVersion = "SEMVER_NO_COERCIBLE_VERSION";
}
