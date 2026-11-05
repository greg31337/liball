namespace molecule;


/// <summary>
/// Creates fixed-size, sparse, or counted molecular fingerprints using supported algorithms.
/// </summary>
public class FingerprintGenerator : IFingerprintGenerator
{
    internal PathFingerprint PathFingerprints;
    internal CircularFingerprint CircularFingerprints;
    internal PatternFingerprint PatternFingerprints;
    internal MaccsFingerprint MaccsFingerprints;

    /// <summary>
    /// Initializes a new instance of the <see cref="FingerprintGenerator"/> class.
    /// </summary>
    public FingerprintGenerator()
    {
        PathFingerprints = new PathFingerprint();
        CircularFingerprints = new CircularFingerprint();
        PatternFingerprints = new PatternFingerprint();
        MaccsFingerprints = new MaccsFingerprint();
    }

    /// <summary>
    /// Creates a fixed-size fingerprint from bounded simple paths through the molecular graph.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting bit fingerprint.</returns>
    public BitFingerprint CreatePathFingerprint(Molecule? molecule, FingerprintOptions? options)
    {
        return PathFingerprints.Create(molecule, options);
    }

    /// <summary>
    /// Creates a folded path fingerprint using the configured folded size.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting folded bit fingerprint.</returns>
    public BitFingerprint CreatePathFoldedFingerprint(Molecule? molecule, FingerprintOptions? options)
    {
        return Fold(PathFingerprints.Create(molecule, options), GetFoldedBitCount(options));
    }

    /// <summary>
    /// Creates a fixed-size fingerprint by iteratively refining each atom's neighborhood.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting bit fingerprint.</returns>
    public BitFingerprint CreateCircularFingerprint(Molecule? molecule, FingerprintOptions? options)
    {
        return CircularFingerprints.Create(molecule, options);
    }

    /// <summary>
    /// Creates a folded circular fingerprint using the configured folded size.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting folded bit fingerprint.</returns>
    public BitFingerprint CreateCircularFoldedFingerprint(Molecule? molecule, FingerprintOptions? options)
    {
        return Fold(CircularFingerprints.Create(molecule, options), GetFoldedBitCount(options));
    }

    /// <summary>
    /// Creates a fixed-size fingerprint from the library's built-in structural key patterns.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting bit fingerprint.</returns>
    public BitFingerprint CreatePatternFingerprint(Molecule? molecule, FingerprintOptions? options)
    {
        return PatternFingerprints.Create(molecule, options);
    }

    /// <summary>
    /// Creates a folded pattern fingerprint using the configured folded size.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting folded bit fingerprint.</returns>
    public BitFingerprint CreatePatternFoldedFingerprint(Molecule? molecule, FingerprintOptions? options)
    {
        return Fold(PatternFingerprints.Create(molecule, options), GetFoldedBitCount(options));
    }

    /// <summary>
    /// Creates the fixed 166-bit built-in MACCS-like structural-key fingerprint.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting bit fingerprint.</returns>
    public BitFingerprint CreateMaccsFingerprint(Molecule? molecule, FingerprintOptions? options)
    {
        return MaccsFingerprints.Create(molecule, options);
    }

    /// <summary>
    /// Creates a folded built-in MACCS-like structural-key fingerprint.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting folded bit fingerprint.</returns>
    public BitFingerprint CreateMaccsFoldedFingerprint(Molecule? molecule, FingerprintOptions? options)
    {
        return Fold(MaccsFingerprints.Create(molecule, options), GetFoldedBitCount(options));
    }

    /// <summary>
    /// Creates the path sparse fingerprint.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting sparse fingerprint.</returns>
    public SparseFingerprint CreatePathSparseFingerprint(Molecule? molecule, FingerprintOptions? options)
    {
        return SparseFingerprint.FromBitFingerprint(CreatePathFingerprint(molecule, options));
    }

    /// <summary>
    /// Creates the circular sparse fingerprint.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting sparse fingerprint.</returns>
    public SparseFingerprint CreateCircularSparseFingerprint(Molecule? molecule, FingerprintOptions? options)
    {
        return SparseFingerprint.FromBitFingerprint(CreateCircularFingerprint(molecule, options));
    }

    /// <summary>
    /// Creates the pattern sparse fingerprint.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting sparse fingerprint.</returns>
    public SparseFingerprint CreatePatternSparseFingerprint(Molecule? molecule, FingerprintOptions? options)
    {
        return SparseFingerprint.FromBitFingerprint(CreatePatternFingerprint(molecule, options));
    }

    /// <summary>
    /// Creates the sparse built-in MACCS-like structural-key fingerprint.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting sparse fingerprint.</returns>
    public SparseFingerprint CreateMaccsSparseFingerprint(Molecule? molecule, FingerprintOptions? options)
    {
        return SparseFingerprint.FromBitFingerprint(CreateMaccsFingerprint(molecule, options));
    }

    /// <summary>
    /// Creates the path count fingerprint.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting count fingerprint.</returns>
    public CountFingerprint CreatePathCountFingerprint(Molecule? molecule, FingerprintOptions? options)
    {
        return PathFingerprints.CreateCount(molecule, options);
    }

    /// <summary>
    /// Creates the circular count fingerprint.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting count fingerprint.</returns>
    public CountFingerprint CreateCircularCountFingerprint(Molecule? molecule, FingerprintOptions? options)
    {
        return CircularFingerprints.CreateCount(molecule, options);
    }

    /// <summary>
    /// Creates the pattern count fingerprint.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting count fingerprint.</returns>
    public CountFingerprint CreatePatternCountFingerprint(Molecule? molecule, FingerprintOptions? options)
    {
        return PatternFingerprints.CreateCount(molecule, options);
    }

    /// <summary>
    /// Creates the counted built-in MACCS-like structural-key fingerprint.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting count fingerprint.</returns>
    public CountFingerprint CreateMaccsCountFingerprint(Molecule? molecule, FingerprintOptions? options)
    {
        return MaccsFingerprints.CreateCount(molecule, options);
    }

    internal int GetFoldedBitCount(FingerprintOptions? options)
    {
        int size = 128;
        if (options != null)
        {
            size = options.FoldedBitCount;
        }

        if (size < 1)
        {
            size = 1;
        }

        return size;
    }

    internal BitFingerprint Fold(BitFingerprint? source, int size)
    {
        BitFingerprint folded = new BitFingerprint(size);
        if (source == null)
        {
            return folded;
        }

        int index = 0;
        while (index < source.Size)
        {
            if (source.GetBit(index))
            {
                folded.SetBit(index % folded.Size);
            }

            index = index + 1;
        }

        return folded;
    }
}
