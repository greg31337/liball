namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="MassCalculator"/>.
/// </summary>
public interface IMassCalculator
{
    /// <summary>
    /// Calculates average molecular weight including attached hydrogens.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The calculated numeric value.</returns>
    double MolecularWeight(Molecule? molecule);

    /// <summary>
    /// Calculates monoisotopic mass, using explicit isotope masses where present.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The calculated numeric value.</returns>
    double ExactMass(Molecule? molecule);

    /// <summary>
    /// Calculates integer nominal mass including attached hydrogens.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting count or integer value.</returns>
    int NominalMass(Molecule? molecule);
}

