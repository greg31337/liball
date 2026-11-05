namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="ParentMoleculeFactory"/>.
/// </summary>
public interface IParentMoleculeFactory
{
    /// <summary>
    /// Creates the stereo parent.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule CreateStereoParent(Molecule? molecule);

    /// <summary>
    /// Creates the stereo parent using the specified options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule CreateStereoParentWithOptions(Molecule? molecule, StandardizationOptions? options);

    /// <summary>
    /// Creates the fragment parent.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule CreateFragmentParent(Molecule? molecule);

    /// <summary>
    /// Creates the fragment parent using the specified options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule CreateFragmentParentWithOptions(Molecule? molecule, StandardizationOptions? options);

    /// <summary>
    /// Creates the charge parent.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule CreateChargeParent(Molecule? molecule);

    /// <summary>
    /// Creates the charge parent using the specified options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule CreateChargeParentWithOptions(Molecule? molecule, StandardizationOptions? options);

    /// <summary>
    /// Creates an isotope parent by removing isotope mass-number distinctions.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An independently owned isotope parent.</returns>
    Molecule CreateIsotopeParent(Molecule? molecule);

    /// <summary>
    /// Creates an isotope parent using the supplied parent-workflow options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The parent-workflow options; isotope distinctions are always removed.</param>
    /// <returns>An independently owned isotope parent.</returns>
    Molecule CreateIsotopeParentWithOptions(Molecule? molecule, StandardizationOptions? options);

    /// <summary>
    /// Creates the tautomer parent.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule CreateTautomerParent(Molecule? molecule);

    /// <summary>
    /// Creates the tautomer parent using the specified options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule CreateTautomerParentWithOptions(Molecule? molecule, StandardizationOptions? options);

    /// <summary>
    /// Creates the super parent.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule CreateSuperParent(Molecule? molecule);

    /// <summary>
    /// Creates the super parent using the specified options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule CreateSuperParentWithOptions(Molecule? molecule, StandardizationOptions? options);

    /// <summary>
    /// Creates the parent.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule CreateParent(Molecule? molecule, IdentityPolicy policy);

    /// <summary>
    /// Creates the parent selected by an identity policy using explicit parent-workflow options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="policy">The identity or parent policy to apply.</param>
    /// <param name="options">The options used by configurable parent workflows.</param>
    /// <returns>An independently owned policy parent.</returns>
    Molecule CreateParentWithOptions(Molecule? molecule, IdentityPolicy policy, StandardizationOptions? options);
}
