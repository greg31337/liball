using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="AtomTyper"/>.
/// </summary>
public interface IAtomTyper
{
    /// <summary>
    /// Calculates hydrogen counts for one atom.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The calculated hydrogen counts.</returns>
    HydrogenCalculationResult CalculateHydrogens(Molecule? molecule, int atomId);

    /// <summary>
    /// Calculates valence information for one atom.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The calculated valence information.</returns>
    ValenceResult CalculateValence(Molecule? molecule, int atomId);

    /// <summary>
    /// Returns the radical state represented by an atom's radical electron count.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The radical state, or <see cref="RadicalState.None"/> when the atom is unavailable.</returns>
    RadicalState GetRadicalState(Molecule? molecule, int atomId);

    /// <summary>
    /// Classifies one atom and reports its elemental and supported functional-group features.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    AtomTypeResult TypeAtom(Molecule? molecule, int atomId);

    /// <summary>
    /// Classifies every atom in identifier order.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    List<AtomTypeResult> TypeAtoms(Molecule? molecule);

    /// <summary>
    /// Determines whether the value is metal.
    /// </summary>
    /// <param name="atomicNumber">The atomic number.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsMetal(int atomicNumber);

    /// <summary>
    /// Determines whether the value is halogen.
    /// </summary>
    /// <param name="atomicNumber">The atomic number.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsHalogen(int atomicNumber);
}
