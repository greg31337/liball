namespace icc;

/// <summary>Compares profile semantics and sampled behavior.</summary>
public class IccProfileComparer : IIccProfileComparer
{
    internal ProfileComparisonCore Core;

    /// <summary>Initializes a profile comparer.</summary>
    public IccProfileComparer()
    {
        Core = new ProfileComparisonCore();
    }

    /// <summary>Compares profiles exactly.</summary>
    /// <param name="firstProfile">The first profile.</param>
    /// <param name="secondProfile">The second profile.</param>
    /// <returns>Whether the profiles match.</returns>
    public bool CompareProfilesExactly(IccProfile firstProfile, IccProfile secondProfile)
    {
        return Core.CompareExact(firstProfile, secondProfile);
    }

    /// <summary>Compares deterministic transform samples.</summary>
    /// <param name="firstProfile">The first profile.</param>
    /// <param name="secondProfile">The second profile.</param>
    /// <param name="sampleCount">The sample count.</param>
    /// <returns>The largest sampled difference.</returns>
    public double CompareProfileSamples(IccProfile firstProfile, IccProfile secondProfile, int sampleCount)
    {
        return Core.CompareSamples(firstProfile, secondProfile, sampleCount);
    }

    /// <summary>Compares exact semantics and sampled behaviour against a tolerance.</summary>
    /// <param name="firstProfile">The first profile.</param>
    /// <param name="secondProfile">The second profile.</param>
    /// <param name="sampleCount">The sample count.</param>
    /// <param name="tolerance">The maximum permitted CIEDE2000 difference.</param>
    /// <returns>The detailed substitutability report.</returns>
    public IccComparisonReport CompareProfiles(IccProfile firstProfile, IccProfile secondProfile,
        int sampleCount, double tolerance)
    {
        return Core.CompareReport(firstProfile, secondProfile, sampleCount, tolerance);
    }

    /// <summary>Computes a semantic fingerprint.</summary>
    /// <param name="profile">The profile to fingerprint.</param>
    /// <returns>The fingerprint bytes.</returns>
    public byte[] ComputeProfileFingerprint(IccProfile profile)
    {
        return Core.ComputeFingerprint(profile);
    }

    /// <summary>Probes whether profiles can be compared.</summary>
    /// <param name="firstProfile">The first profile.</param>
    /// <param name="secondProfile">The second profile.</param>
    /// <returns>The probe result.</returns>
    public IccOperationResult ProbeProfileComparison(IccProfile firstProfile, IccProfile secondProfile)
    {
        return Core.Probe(firstProfile, secondProfile);
    }
}
