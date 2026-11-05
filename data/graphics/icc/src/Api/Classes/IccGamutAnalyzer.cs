namespace icc;

/// <summary>Evaluates profile gamut membership and availability.</summary>
public class IccGamutAnalyzer : IIccGamutAnalyzer
{
    internal GamutAnalysisCore Core;

    /// <summary>Initializes a gamut analyzer.</summary>
    public IccGamutAnalyzer()
    {
        Core = new GamutAnalysisCore();
    }

    /// <summary>Evaluates profile gamut membership.</summary>
    /// <param name="profile">The profile to evaluate.</param>
    /// <param name="color">The colour values.</param>
    /// <returns>The gamut status.</returns>
    public IccGamutStatus EvaluateProfileGamut(IccProfile profile, System.ReadOnlySpan<float> color)
    {
        return Core.Evaluate(profile, color).Status;
    }

    /// <summary>Evaluates gamut membership with nearest-boundary details.</summary>
    /// <param name="profile">The profile to evaluate.</param>
    /// <param name="color">The PCS colour values.</param>
    /// <returns>The detailed gamut result.</returns>
    public IccGamutResult EvaluateProfileGamutDetails(IccProfile profile, System.ReadOnlySpan<float> color)
    {
        return Core.Evaluate(profile, color);
    }

    /// <summary>Probes profile gamut support.</summary>
    /// <param name="profile">The profile to probe.</param>
    /// <returns>The probe result.</returns>
    public IccOperationResult ProbeProfileGamut(IccProfile profile)
    {
        return Core.Probe(profile);
    }
}
