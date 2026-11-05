namespace molecule;


/// <summary>
/// Uses conservative path-fingerprint containment to avoid unnecessary exact substructure searches.
/// </summary>
public class FingerprintScreening : IFingerprintScreening
{
    internal FingerprintGenerator Fingerprints;
    internal SubgraphMatcher Matcher;

    /// <summary>
    /// Initializes a fingerprint screening and exact-matching service.
    /// </summary>
    public FingerprintScreening()
    {
        Fingerprints = new FingerprintGenerator();
        Matcher = new SubgraphMatcher();
    }

    /// <summary>
    /// Screens an exact molecular query with path fingerprints and runs exact matching only when the screen passes.
    /// </summary>
    /// <param name="target">The molecule that may contain the query.</param>
    /// <param name="query">The exact molecular substructure to find.</param>
    /// <param name="options">The fingerprint options used by the conservative screen.</param>
    /// <returns>The screening and exact-match result.</returns>
    public FingerprintScreeningResult FindFirst(Molecule? target, Molecule? query, FingerprintOptions? options)
    {
        FingerprintScreeningResult result = new FingerprintScreeningResult();
        FingerprintOptions safeOptions = NormalizeOptions(options);
        result.TargetFingerprint = Fingerprints.CreatePathFingerprint(target, safeOptions);
        safeOptions = NormalizeOptions(options);
        result.QueryFingerprint = Fingerprints.CreatePathFingerprint(query, safeOptions);

        if (target == null || query == null)
        {
            result.Report.AddError("FINGERPRINT_SCREEN_NULL_MOLECULE", "Fingerprint screening requires both a target and a query molecule.");
            return result;
        }

        if (query.Atoms.Count > target.Atoms.Count || query.Bonds.Count > target.Bonds.Count)
        {
            return result;
        }

        result.ScreenPassed = ContainsAllQueryBits(result.TargetFingerprint, result.QueryFingerprint);
        if (!result.ScreenPassed)
        {
            return result;
        }

        result.ExactMatchAttempted = true;
        result.Match = Matcher.FindFirstExact(target, query);
        result.Success = result.Match.Success;
        return result;
    }

    internal FingerprintOptions NormalizeOptions(FingerprintOptions? options)
    {
        FingerprintOptions normalized = new FingerprintOptions();
        normalized.BitCount = 4096;
        normalized.MinimumPathLength = 0;
        normalized.MaximumPathLength = 7;
        normalized.IncludeBondOrder = true;
        normalized.IncludeAromaticity = true;
        normalized.IncludeFormalCharge = true;
        if (options != null)
        {
            normalized.BitCount = options.BitCount;
            normalized.MinimumPathLength = options.MinimumPathLength;
            normalized.MaximumPathLength = options.MaximumPathLength;
            normalized.IncludeBondOrder = options.IncludeBondOrder;
            normalized.IncludeAromaticity = options.IncludeAromaticity;
            normalized.IncludeFormalCharge = options.IncludeFormalCharge;
        }

        if (normalized.BitCount < 1)
        {
            normalized.BitCount = 1;
        }

        if (normalized.MinimumPathLength < 0)
        {
            normalized.MinimumPathLength = 0;
        }

        if (normalized.MaximumPathLength < 0)
        {
            normalized.MaximumPathLength = 0;
        }

        return normalized;
    }

    internal bool ContainsAllQueryBits(BitFingerprint target, BitFingerprint query)
    {
        int index = 0;
        while (index < query.Size)
        {
            if (query.GetBit(index) && !target.GetBit(index))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
