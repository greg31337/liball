namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="Standardizer"/>.
/// </summary>
public interface IStandardizer
{
    /// <summary>
    /// Returns the default standardization policy.
    /// </summary>
    /// <returns>The default standardization policy.</returns>
    StandardizationPolicy GetDefaultPolicy();

    /// <summary>
    /// Runs the enabled cleanup stages and returns only the resulting molecule.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule StandardizeMolecule(Molecule? molecule, StandardizationOptions? options);

    /// <summary>
    /// Runs the enabled cleanup stages on a copy of the input molecule and returns changes and diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    StandardizationResult Standardize(Molecule? molecule, StandardizationOptions? options);

    /// <summary>
    /// Cleanup isotopes.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule CleanupIsotopes(Molecule? molecule, IsotopeCleanupPolicy? policy);

    /// <summary>
    /// Cleanup isotopess the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    IsotopeCleanupResult CleanupIsotopesWithResult(Molecule? molecule, IsotopeCleanupPolicy? policy);

    /// <summary>
    /// Cleanup radicals.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule CleanupRadicals(Molecule? molecule, RadicalCleanupPolicy? policy);

    /// <summary>
    /// Cleanup radicalss the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    RadicalCleanupResult CleanupRadicalsWithResult(Molecule? molecule, RadicalCleanupPolicy? policy);
}
