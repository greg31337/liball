namespace icc;

/// <summary>
/// Evaluates profile gamut membership and availability.
/// </summary>
public interface IIccGamutAnalyzer
{
    /// <summary>
    /// Evaluates whether a profile-space colour belongs to the profile gamut.
    /// </summary>
    /// <param name="profile">The profile supplying the gamut model.</param>
    /// <param name="color">The colour-channel values.</param>
    /// <returns>The gamut status.</returns>
    IccGamutStatus EvaluateProfileGamut(IccProfile profile, System.ReadOnlySpan<float> color);

    /// <summary>
    /// Evaluates gamut membership and reports nearest-boundary information when a mesh is available.
    /// </summary>
    /// <param name="profile">The profile supplying the gamut model.</param>
    /// <param name="color">The PCS colour-channel values.</param>
    /// <returns>The detailed gamut result.</returns>
    /// <exception cref="IccProfileValidationException">The profile or colour is invalid for its selected gamut model.</exception>
    IccGamutResult EvaluateProfileGamutDetails(IccProfile profile, System.ReadOnlySpan<float> color);

    /// <summary>
    /// Checks whether a profile contains a usable gamut model.
    /// </summary>
    /// <param name="profile">The profile to check.</param>
    /// <returns>The gamut-model capability outcome.</returns>
    IccOperationResult ProbeProfileGamut(IccProfile profile);
}
