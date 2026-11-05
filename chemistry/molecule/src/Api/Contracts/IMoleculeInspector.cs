using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines read-only graph inspection operations provided by <see cref="MoleculeInspector"/>.
/// </summary>
public interface IMoleculeInspector
{
    /// <summary>Returns the number of atoms.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <returns>The atom count, or <c>0</c> for <c>null</c>.</returns>
    int GetAtomCount(Molecule? molecule);

    /// <summary>Returns the number of bonds.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <returns>The bond count, or <c>0</c> for <c>null</c>.</returns>
    int GetBondCount(Molecule? molecule);

    /// <summary>Returns an independent copy of an atom.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The copied atom, or an empty atom when unavailable.</returns>
    Atom GetAtom(Molecule? molecule, int atomId);

    /// <summary>Returns an independent copy of a bond.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="bondId">The zero-based bond identifier.</param>
    /// <returns>The copied bond, or an empty bond when unavailable.</returns>
    Bond GetBond(Molecule? molecule, int bondId);

    /// <summary>Returns an independent copy of the bond connecting two atoms.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="atomIdA">The first atom identifier.</param>
    /// <param name="atomIdB">The second atom identifier.</param>
    /// <returns>The copied bond, or an empty bond when none exists.</returns>
    Bond GetBondBetween(Molecule? molecule, int atomIdA, int atomIdB);

    /// <summary>Returns neighbor atom identifiers in bond order.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>An independent list, or an empty list when unavailable.</returns>
    List<int> GetNeighborAtomIds(Molecule? molecule, int atomId);

    /// <summary>Returns neighbor bond identifiers in bond order.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>An independent list, or an empty list when unavailable.</returns>
    List<int> GetNeighborBondIds(Molecule? molecule, int atomId);

    /// <summary>Returns the atom degree.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The degree, or <c>0</c> when unavailable.</returns>
    int GetDegree(Molecule? molecule, int atomId);

    /// <summary>Calculates valence information for an atom.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The valence result, with an unset atom identifier when unavailable.</returns>
    ValenceResult GetValence(Molecule? molecule, int atomId);

    /// <summary>Determines whether an atom belongs to a ring.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns><c>true</c> when the atom is in a ring; otherwise, <c>false</c>.</returns>
    bool AtomIsInRing(Molecule? molecule, int atomId);

    /// <summary>Determines whether a bond belongs to a ring.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="bondId">The zero-based bond identifier.</param>
    /// <returns><c>true</c> when the bond is in a ring; otherwise, <c>false</c>.</returns>
    bool BondIsInRing(Molecule? molecule, int bondId);

    /// <summary>Returns the number of rings containing an atom.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The ring membership count.</returns>
    int GetAtomRingCount(Molecule? molecule, int atomId);

    /// <summary>Returns the number of rings containing a bond.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="bondId">The zero-based bond identifier.</param>
    /// <returns>The ring membership count.</returns>
    int GetBondRingCount(Molecule? molecule, int bondId);

    /// <summary>Returns the number of connected components.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <returns>The component count.</returns>
    int GetComponentCount(Molecule? molecule);

    /// <summary>Returns the component identifier for an atom.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The component identifier, or <c>-1</c> when unavailable.</returns>
    int GetComponentId(Molecule? molecule, int atomId);

    /// <summary>Returns the component identifier for a bond.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="bondId">The zero-based bond identifier.</param>
    /// <returns>The component identifier, or <c>-1</c> when unavailable.</returns>
    int GetBondComponentId(Molecule? molecule, int bondId);

    /// <summary>Returns the atom identifiers in a component.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="componentId">The zero-based component identifier.</param>
    /// <returns>An independent member list.</returns>
    List<int> GetComponentAtomIds(Molecule? molecule, int componentId);

    /// <summary>Returns the bond identifiers in a component.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="componentId">The zero-based component identifier.</param>
    /// <returns>An independent member list.</returns>
    List<int> GetComponentBondIds(Molecule? molecule, int componentId);

    /// <summary>Returns the number of conformers.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <returns>The conformer count.</returns>
    int GetConformerCount(Molecule? molecule);

    /// <summary>Returns an independent copy of a conformer.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="conformerId">The zero-based conformer identifier.</param>
    /// <returns>The copied conformer, or an empty conformer when unavailable.</returns>
    Conformer GetConformer(Molecule? molecule, int conformerId);

    /// <summary>Returns independent copies of all conformers.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <returns>The conformers in identifier order.</returns>
    List<Conformer> GetConformers(Molecule? molecule);

    /// <summary>Returns the number of stereo records.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <returns>The stereo record count.</returns>
    int GetStereoRecordCount(Molecule? molecule);

    /// <summary>Returns an independent copy of a stereo record.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="stereoRecordId">The zero-based stereo record identifier.</param>
    /// <returns>The copied record, or an empty record when unavailable.</returns>
    StereoRecord GetStereoRecord(Molecule? molecule, int stereoRecordId);

    /// <summary>Returns independent copies of all stereo records.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <returns>The stereo records in identifier order.</returns>
    List<StereoRecord> GetStereoRecords(Molecule? molecule);

    /// <summary>Returns an independent copy of all molecule metadata.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <returns>The copied metadata, or empty metadata for <c>null</c>.</returns>
    MoleculeMetadata GetMetadata(Molecule? molecule);
}
