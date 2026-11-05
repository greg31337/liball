namespace molecule;

internal class MaccsFingerprint
{
    internal PatternFingerprint Patterns;

    internal MaccsFingerprint()
    {
        Patterns = new PatternFingerprint();
    }

    internal BitFingerprint Create(Molecule? molecule, FingerprintOptions? options)
    {
        return Patterns.Create(molecule, CreateCanonicalOptions(options));
    }

    internal CountFingerprint CreateCount(Molecule? molecule, FingerprintOptions? options)
    {
        return Patterns.CreateCount(molecule, CreateCanonicalOptions(options));
    }

    internal FingerprintOptions CreateCanonicalOptions(FingerprintOptions? options)
    {
        FingerprintOptions canonical = new FingerprintOptions();
        canonical.BitCount = 166;
        if (options != null)
        {
            canonical.IncludeBondOrder = options.IncludeBondOrder;
            canonical.IncludeAromaticity = options.IncludeAromaticity;
            canonical.IncludeFormalCharge = options.IncludeFormalCharge;
            canonical.FoldedBitCount = options.FoldedBitCount;
        }

        return canonical;
    }
}
