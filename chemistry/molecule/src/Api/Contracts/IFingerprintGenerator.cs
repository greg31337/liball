namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="FingerprintGenerator"/>.
/// </summary>
public interface IFingerprintGenerator
{
    /// <summary>
    /// Creates a fixed-size fingerprint from bounded simple paths through the molecular graph.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting bit fingerprint.</returns>
    BitFingerprint CreatePathFingerprint(Molecule? molecule, FingerprintOptions? options);

    /// <summary>
    /// Creates a folded path fingerprint using the configured folded size.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting folded bit fingerprint.</returns>
    BitFingerprint CreatePathFoldedFingerprint(Molecule? molecule, FingerprintOptions? options);

    /// <summary>
    /// Creates a fixed-size fingerprint by iteratively refining each atom's neighborhood.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting bit fingerprint.</returns>
    BitFingerprint CreateCircularFingerprint(Molecule? molecule, FingerprintOptions? options);

    /// <summary>
    /// Creates a folded circular fingerprint using the configured folded size.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting folded bit fingerprint.</returns>
    BitFingerprint CreateCircularFoldedFingerprint(Molecule? molecule, FingerprintOptions? options);

    /// <summary>
    /// Creates a fixed-size fingerprint from the library's built-in structural key patterns.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting bit fingerprint.</returns>
    BitFingerprint CreatePatternFingerprint(Molecule? molecule, FingerprintOptions? options);

    /// <summary>
    /// Creates a folded pattern fingerprint using the configured folded size.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting folded bit fingerprint.</returns>
    BitFingerprint CreatePatternFoldedFingerprint(Molecule? molecule, FingerprintOptions? options);

    /// <summary>
    /// Creates the fixed 166-bit built-in MACCS-like structural-key fingerprint.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting bit fingerprint.</returns>
    BitFingerprint CreateMaccsFingerprint(Molecule? molecule, FingerprintOptions? options);

    /// <summary>
    /// Creates a folded built-in MACCS-like structural-key fingerprint.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting folded bit fingerprint.</returns>
    BitFingerprint CreateMaccsFoldedFingerprint(Molecule? molecule, FingerprintOptions? options);

    /// <summary>
    /// Creates the path sparse fingerprint.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting sparse fingerprint.</returns>
    SparseFingerprint CreatePathSparseFingerprint(Molecule? molecule, FingerprintOptions? options);

    /// <summary>
    /// Creates the circular sparse fingerprint.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting sparse fingerprint.</returns>
    SparseFingerprint CreateCircularSparseFingerprint(Molecule? molecule, FingerprintOptions? options);

    /// <summary>
    /// Creates the pattern sparse fingerprint.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting sparse fingerprint.</returns>
    SparseFingerprint CreatePatternSparseFingerprint(Molecule? molecule, FingerprintOptions? options);

    /// <summary>
    /// Creates the sparse built-in MACCS-like structural-key fingerprint.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting sparse fingerprint.</returns>
    SparseFingerprint CreateMaccsSparseFingerprint(Molecule? molecule, FingerprintOptions? options);

    /// <summary>
    /// Creates the path count fingerprint.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting count fingerprint.</returns>
    CountFingerprint CreatePathCountFingerprint(Molecule? molecule, FingerprintOptions? options);

    /// <summary>
    /// Creates the circular count fingerprint.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting count fingerprint.</returns>
    CountFingerprint CreateCircularCountFingerprint(Molecule? molecule, FingerprintOptions? options);

    /// <summary>
    /// Creates the pattern count fingerprint.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting count fingerprint.</returns>
    CountFingerprint CreatePatternCountFingerprint(Molecule? molecule, FingerprintOptions? options);

    /// <summary>
    /// Creates the counted built-in MACCS-like structural-key fingerprint.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting count fingerprint.</returns>
    CountFingerprint CreateMaccsCountFingerprint(Molecule? molecule, FingerprintOptions? options);
}
