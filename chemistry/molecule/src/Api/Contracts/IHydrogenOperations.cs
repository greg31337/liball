namespace molecule;


/// <summary>
/// Defines public calculations and copy-based edits for molecular hydrogens.
/// </summary>
public interface IHydrogenOperations
{
    /// <summary>
    /// Calculates hydrogen and valence information for one atom without changing the molecule.
    /// </summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The calculated hydrogen information, including atom and valence validity.</returns>
    HydrogenCalculationResult CalculateAtomHydrogens(Molecule? molecule, int atomId);

    /// <summary>
    /// Calculates and assigns implicit hydrogen counts on an independent molecule copy.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The copied molecule, per-atom calculations, changes, and diagnostics.</returns>
    HydrogenOperationResult CalculateImplicitHydrogens(Molecule? molecule);

    /// <summary>
    /// Calculates missing implicit hydrogens and expands them into explicit graph atoms on an independent copy.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The copied molecule, per-atom calculations, changes, and diagnostics.</returns>
    HydrogenOperationResult AddExplicitHydrogens(Molecule? molecule);

    /// <summary>
    /// Removes eligible explicit graph hydrogen atoms on an independent copy.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">Options controlling isotope preservation; <c>null</c> uses safe defaults.</param>
    /// <returns>The copied molecule, per-atom calculations, changes, and diagnostics.</returns>
    HydrogenOperationResult RemoveExplicitHydrogens(Molecule? molecule, HydrogenOperationOptions? options);

    /// <summary>
    /// Converts eligible explicit graph hydrogens to calculated implicit counts on an independent copy.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">Options controlling isotope preservation; <c>null</c> uses safe defaults.</param>
    /// <returns>The copied molecule, per-atom calculations, changes, and diagnostics.</returns>
    HydrogenOperationResult NormalizeHydrogens(Molecule? molecule, HydrogenOperationOptions? options);
}
