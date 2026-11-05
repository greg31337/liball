namespace icc;

/// <summary>Validates profile models and encoded profile inputs.</summary>
public class IccProfileValidator : IIccProfileValidator
{
    internal ValidationReportCore Core;

    /// <summary>Initializes a profile validator.</summary>
    public IccProfileValidator()
    {
        Core = new ValidationReportCore();
    }

    /// <summary>Samples curve and transform quality for a bidirectional profile.</summary>
    /// <param name="profile">The profile to analyze.</param>
    /// <param name="sampleCount">The deterministic sample count.</param>
    /// <returns>The sampled quality statistics.</returns>
    public IccQualityStatistics AnalyzeProfileQuality(IccProfile profile, int sampleCount)
    {
        return QualityValidator.Analyze(profile, sampleCount);
    }

    /// <summary>Checks whether profile quality can be sampled.</summary>
    /// <param name="profile">The profile to probe.</param>
    /// <returns>The quality-analysis capability outcome.</returns>
    public IccOperationResult ProbeProfileQuality(IccProfile profile)
    {
        return QualityValidator.Probe(profile);
    }

    /// <summary>Compares optional characterization pairs with profile output.</summary>
    /// <param name="profile">The profile to evaluate.</param>
    /// <param name="samples">The optional characterization samples.</param>
    /// <param name="tolerance">The maximum permitted CIEDE2000 difference.</param>
    /// <returns>The characterization comparison report.</returns>
    public IccComparisonReport CompareProfileCharacterization(IccProfile profile,
        IccCharacterizationSample[] samples, double tolerance)
    {
        return CharacterizationComparison.Compare(profile, samples, tolerance);
    }

    /// <summary>Builds a detailed report for a decoded profile.</summary>
    /// <param name="profile">The profile to validate.</param>
    /// <param name="policy">The validation policy.</param>
    /// <returns>The detailed report.</returns>
    public IccValidationReport ValidateProfileDataReport(IccProfile profile, IccValidationPolicy policy)
    {
        return Core.Build(profile, policy);
    }

    /// <summary>Builds a detailed report for encoded bytes.</summary>
    /// <param name="profileBytes">The encoded bytes.</param>
    /// <param name="options">The reading options.</param>
    /// <returns>The detailed report.</returns>
    public IccValidationReport ValidateProfileBytesReport(System.ReadOnlySpan<byte> profileBytes, IccReadOptions options)
    {
        return Core.BuildBytes(profileBytes, options);
    }

    /// <summary>Builds a detailed report for encoded memory.</summary>
    /// <param name="profileMemory">The encoded memory.</param>
    /// <param name="options">The reading options.</param>
    /// <returns>The detailed report.</returns>
    public IccValidationReport ValidateProfileMemoryReport(System.ReadOnlyMemory<byte> profileMemory, IccReadOptions options)
    {
        return Core.BuildMemory(profileMemory, options);
    }

    /// <summary>Builds a detailed report from a stream.</summary>
    /// <param name="profileStream">The profile stream.</param>
    /// <param name="options">The reading options.</param>
    /// <returns>The detailed report.</returns>
    public IccValidationReport ValidateProfileStreamReport(System.IO.Stream profileStream, IccReadOptions options)
    {
        return Core.BuildStream(profileStream, options);
    }

    /// <summary>Validates a profile model.</summary>
    /// <param name="profile">The profile to validate.</param>
    /// <returns>The validation result.</returns>
    public IccOperationResult ValidateProfileData(IccProfile profile)
    {
        return Core.Build(profile, new IccValidationPolicy()).Result;
    }

    /// <summary>Validates encoded profile bytes.</summary>
    /// <param name="profileBytes">The encoded bytes.</param>
    /// <param name="options">The reading options.</param>
    /// <returns>The validation result.</returns>
    public IccOperationResult ValidateProfileBytes(System.ReadOnlySpan<byte> profileBytes, IccReadOptions options)
    {
        return Core.BuildBytes(profileBytes, options).Result;
    }

    /// <summary>Validates encoded profile memory.</summary>
    /// <param name="profileMemory">The encoded memory.</param>
    /// <param name="options">The reading options.</param>
    /// <returns>The validation result.</returns>
    public IccOperationResult ValidateProfileMemory(System.ReadOnlyMemory<byte> profileMemory, IccReadOptions options)
    {
        return Core.BuildMemory(profileMemory, options).Result;
    }

    /// <summary>Validates an encoded profile stream.</summary>
    /// <param name="profileStream">The encoded stream.</param>
    /// <param name="options">The reading options.</param>
    /// <returns>The validation result.</returns>
    public IccOperationResult ValidateProfileStream(System.IO.Stream profileStream, IccReadOptions options)
    {
        return Core.BuildStream(profileStream, options).Result;
    }
}
