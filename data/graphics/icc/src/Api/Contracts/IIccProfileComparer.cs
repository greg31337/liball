namespace icc;

/// <summary>
/// Compares profile semantics and sampled transform behavior and computes stable fingerprints.
/// </summary>
public interface IIccProfileComparer
{
    /// <summary>
    /// Compares two profiles for exact decoded semantic equality.
    /// </summary>
    /// <param name="firstProfile">The first profile.</param>
    /// <param name="secondProfile">The second profile.</param>
    /// <returns><c>true</c> when the profiles are semantically equal; otherwise <c>false</c>.</returns>
    bool CompareProfilesExactly(IccProfile firstProfile, IccProfile secondProfile);

    /// <summary>
    /// Compares two profiles through deterministic transform samples.
    /// </summary>
    /// <param name="firstProfile">The first profile.</param>
    /// <param name="secondProfile">The second profile.</param>
    /// <param name="sampleCount">The requested deterministic sample count.</param>
    /// <returns>The largest sampled difference.</returns>
    double CompareProfileSamples(IccProfile firstProfile, IccProfile secondProfile, int sampleCount);

    /// <summary>
    /// Compares exact profile semantics and sampled transform behaviour against a caller-selected tolerance.
    /// </summary>
    /// <param name="firstProfile">The first profile.</param>
    /// <param name="secondProfile">The second profile.</param>
    /// <param name="sampleCount">The deterministic sample count.</param>
    /// <param name="tolerance">The maximum permitted CIEDE2000 difference.</param>
    /// <returns>A detailed substitutability report.</returns>
    IccComparisonReport CompareProfiles(IccProfile firstProfile, IccProfile secondProfile, int sampleCount, double tolerance);

    /// <summary>
    /// Computes a stable semantic profile fingerprint.
    /// </summary>
    /// <param name="profile">The profile to fingerprint.</param>
    /// <returns>The fingerprint bytes.</returns>
    byte[] ComputeProfileFingerprint(IccProfile profile);

    /// <summary>
    /// Checks whether two profiles have enough supported transform data for approximate comparison.
    /// </summary>
    /// <param name="firstProfile">The first profile.</param>
    /// <param name="secondProfile">The second profile.</param>
    /// <returns>The comparison capability outcome.</returns>
    IccOperationResult ProbeProfileComparison(IccProfile firstProfile, IccProfile secondProfile);
}
