namespace version;

/// <summary>
/// Defines stable diagnostic codes emitted by Go operations.
/// </summary>
/// <remarks>
/// Compare these constants with diagnostic codes returned by result objects; messages may provide additional context for a specific input.
/// </remarks>
public static class GoDiagnosticCodes
{
    /// <summary>
    /// Identifies the missing leading v condition.
    /// </summary>
    public const string MissingLeadingV = "GO_MISSING_LEADING_V";
    /// <summary>
    /// Identifies the missing module path condition.
    /// </summary>
    public const string MissingModulePath = "GO_MODULE_MISSING_PATH";
    /// <summary>
    /// Identifies the invalid module path condition.
    /// </summary>
    public const string InvalidModulePath = "GO_MODULE_INVALID_PATH";
    /// <summary>
    /// Identifies the invalid path element condition.
    /// </summary>
    public const string InvalidPathElement = "GO_MODULE_INVALID_PATH_ELEMENT";
    /// <summary>
    /// Identifies the invalid first path element condition.
    /// </summary>
    public const string InvalidFirstPathElement = "GO_MODULE_INVALID_FIRST_PATH_ELEMENT";
    /// <summary>
    /// Identifies the invalid major version suffix condition.
    /// </summary>
    public const string InvalidMajorVersionSuffix = "GO_MODULE_INVALID_MAJOR_VERSION_SUFFIX";
    /// <summary>
    /// Identifies the invalid gopkg in path condition.
    /// </summary>
    public const string InvalidGopkgInPath = "GO_MODULE_INVALID_GOPKG_IN_PATH";
    /// <summary>
    /// Identifies the invalid escaped path element condition.
    /// </summary>
    public const string InvalidEscapedPathElement = "GO_MODULE_INVALID_ESCAPED_PATH_ELEMENT";
    /// <summary>
    /// Identifies the missing major version suffix condition.
    /// </summary>
    public const string MissingMajorVersionSuffix = "GO_MODULE_MISSING_MAJOR_VERSION_SUFFIX";
    /// <summary>
    /// Identifies the unexpected major version suffix condition.
    /// </summary>
    public const string UnexpectedMajorVersionSuffix = "GO_MODULE_UNEXPECTED_MAJOR_VERSION_SUFFIX";
    /// <summary>
    /// Identifies the major version mismatch condition.
    /// </summary>
    public const string MajorVersionMismatch = "GO_MODULE_MAJOR_VERSION_MISMATCH";
    /// <summary>
    /// Identifies the invalid incompatible version condition.
    /// </summary>
    public const string InvalidIncompatibleVersion = "GO_MODULE_INVALID_INCOMPATIBLE_VERSION";
}
