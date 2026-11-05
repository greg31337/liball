namespace version;

/// <summary>
/// Defines stable diagnostic codes emitted by NuGet Target Framework operations.
/// </summary>
/// <remarks>
/// Compare these constants with diagnostic codes returned by result objects; messages may provide additional context for a specific input.
/// </remarks>
public static class NuGetTargetFrameworkDiagnosticCodes
{
    /// <summary>
    /// Identifies the null input condition.
    /// </summary>
    public const string NullInput = "NUGET_TFM_NULL_INPUT";
    /// <summary>
    /// Identifies the empty input condition.
    /// </summary>
    public const string EmptyInput = "NUGET_TFM_EMPTY_INPUT";
    /// <summary>
    /// Identifies the unsupported framework condition.
    /// </summary>
    public const string UnsupportedFramework = "NUGET_TFM_UNSUPPORTED_FRAMEWORK";
    /// <summary>
    /// Identifies the invalid version condition.
    /// </summary>
    public const string InvalidVersion = "NUGET_TFM_INVALID_VERSION";
    /// <summary>
    /// Identifies the invalid profile condition.
    /// </summary>
    public const string InvalidProfile = "NUGET_TFM_INVALID_PROFILE";
    /// <summary>
    /// Identifies the invalid fallback list condition.
    /// </summary>
    public const string InvalidFallbackList = "NUGET_TFM_INVALID_FALLBACK_LIST";
}
