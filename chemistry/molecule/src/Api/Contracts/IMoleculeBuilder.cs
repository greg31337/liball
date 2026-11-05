using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="MoleculeBuilder"/>.
/// </summary>
public interface IMoleculeBuilder
{
    /// <summary>
    /// Replaces the editor contents with an independent copy of a molecule.
    /// </summary>
    /// <param name="molecule">The source molecule, or <c>null</c> to reset the editor.</param>
    void Initialize(Molecule? molecule);

    /// <summary>
    /// Adds the atom.
    /// </summary>
    /// <param name="spec">The spec.</param>
    /// <returns>The zero-based identifier, or <c>-1</c> when the operation cannot resolve one.</returns>
    int AddAtom(AtomSpec spec);

    /// <summary>
    /// Adds the bond.
    /// </summary>
    /// <param name="beginAtomId">The begin atom identifier.</param>
    /// <param name="endAtomId">The end atom identifier.</param>
    /// <param name="spec">The spec.</param>
    /// <returns>The zero-based identifier, or <c>-1</c> when the operation cannot resolve one.</returns>
    int AddBond(int beginAtomId, int endAtomId, BondSpec spec);

    /// <summary>
    /// Removes the atom.
    /// </summary>
    /// <param name="atomId">The zero-based atom identifier.</param>
    void RemoveAtom(int atomId);

    /// <summary>
    /// Removes the bond.
    /// </summary>
    /// <param name="bondId">The zero-based bond identifier.</param>
    void RemoveBond(int bondId);

    /// <summary>
    /// Replace atom.
    /// </summary>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <param name="spec">The spec.</param>
    void ReplaceAtom(int atomId, AtomSpec spec);

    /// <summary>
    /// Normalizes atom, bond, stereo, residue, and group references to current storage order.
    /// </summary>
    void Renumber();

    /// <summary>
    /// Returns the molecule currently held by the builder.
    /// </summary>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule Build();

    /// <summary>
    /// Creates an independent copy of the value and its mutable collections.
    /// </summary>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule Clone();

    /// <summary>
    /// Appends a copy of another molecule and remaps its atom and bond identifiers.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    void Merge(Molecule? molecule);

    /// <summary>
    /// Splits the components.
    /// </summary>
    /// <returns>The resulting values in deterministic order.</returns>
    List<Molecule> SplitComponents();
}
