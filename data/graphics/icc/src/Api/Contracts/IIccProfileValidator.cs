namespace icc;

/// <summary>
/// Validates ICC profile data without throwing for expected profile defects.
/// </summary>
public interface IIccProfileValidator
{
    /// <summary>
    /// Samples the selected media-relative forward and reverse transforms and their curves.
    /// </summary>
    /// <param name="profile">The decoded profile.</param>
    /// <param name="sampleCount">The deterministic sample count.</param>
    /// <returns>Curve, round-trip, continuity, and smoothness quality statistics.</returns>
    IccQualityStatistics AnalyzeProfileQuality(IccProfile profile, int sampleCount);

    /// <summary>
    /// Checks whether a profile supplies the transform pair required for quality sampling.
    /// </summary>
    /// <param name="profile">The decoded profile.</param>
    /// <returns>The quality-analysis capability outcome.</returns>
    IccOperationResult ProbeProfileQuality(IccProfile profile);

    /// <summary>
    /// Compares supplied characterization pairs with the selected media-relative profile output.
    /// </summary>
    /// <param name="profile">The decoded profile.</param>
    /// <param name="samples">The optional device and reference colour pairs.</param>
    /// <param name="tolerance">The maximum permitted CIEDE2000 difference.</param>
    /// <returns>Per-sample differences, aggregate statistics, and an explicit applicability outcome.</returns>
    IccComparisonReport CompareProfileCharacterization(IccProfile profile,
        IccCharacterizationSample[] samples, double tolerance);

    /// <summary>
    /// Builds a deterministic validation report for a decoded profile.
    /// </summary>
    /// <param name="profile">The decoded profile.</param>
    /// <param name="policy">The reporting and validity policy.</param>
    /// <returns>The detailed validation report.</returns>
    IccValidationReport ValidateProfileDataReport(IccProfile profile, IccValidationPolicy policy);

    /// <summary>
    /// Builds a deterministic validation report for encoded profile bytes.
    /// </summary>
    /// <param name="profileBytes">The encoded profile bytes.</param>
    /// <param name="options">The read and resource-limit options.</param>
    /// <returns>The detailed validation report.</returns>
    IccValidationReport ValidateProfileBytesReport(System.ReadOnlySpan<byte> profileBytes, IccReadOptions options);

    /// <summary>
    /// Builds a deterministic validation report for encoded profile memory.
    /// </summary>
    /// <param name="profileMemory">The encoded profile memory.</param>
    /// <param name="options">The read and resource-limit options.</param>
    /// <returns>The detailed validation report.</returns>
    IccValidationReport ValidateProfileMemoryReport(System.ReadOnlyMemory<byte> profileMemory, IccReadOptions options);

    /// <summary>
    /// Builds a deterministic validation report from the current stream position.
    /// </summary>
    /// <param name="profileStream">The readable profile stream.</param>
    /// <param name="options">The read and resource-limit options.</param>
    /// <returns>The detailed validation report.</returns>
    IccValidationReport ValidateProfileStreamReport(System.IO.Stream profileStream, IccReadOptions options);

    /// <summary>
    /// Validates a profile data model and reports the overall outcome.
    /// </summary>
    /// <param name="profile">The profile data model to validate.</param>
    /// <returns>The validation outcome and associated byte location.</returns>
    IccOperationResult ValidateProfileData(IccProfile profile);

    /// <summary>
    /// Validates encoded profile bytes without requiring a separate read operation.
    /// </summary>
    /// <param name="profileBytes">The encoded profile bytes.</param>
    /// <param name="options">The read and resource-limit options.</param>
    /// <returns>The validation outcome and associated byte location.</returns>
    IccOperationResult ValidateProfileBytes(System.ReadOnlySpan<byte> profileBytes, IccReadOptions options);

    /// <summary>
    /// Validates encoded profile memory without requiring a separate read operation.
    /// </summary>
    /// <param name="profileMemory">The encoded profile memory.</param>
    /// <param name="options">The read and resource-limit options.</param>
    /// <returns>The validation outcome and associated byte location.</returns>
    IccOperationResult ValidateProfileMemory(System.ReadOnlyMemory<byte> profileMemory, IccReadOptions options);

    /// <summary>
    /// Validates an encoded profile read from the current stream position.
    /// </summary>
    /// <param name="profileStream">The readable profile stream.</param>
    /// <param name="options">The read and resource-limit options.</param>
    /// <returns>The validation outcome and associated byte location.</returns>
    IccOperationResult ValidateProfileStream(System.IO.Stream profileStream, IccReadOptions options);
}
