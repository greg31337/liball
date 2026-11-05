using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides copy-based inspection of molecular graphs and associated records.
/// </summary>
public class MoleculeInspector : IMoleculeInspector
{
    internal Components Components;
    internal RingFinder Rings;
    internal ValenceCalculator Valences;

    /// <summary>Initializes a molecule inspector.</summary>
    public MoleculeInspector()
    {
        Components = new Components();
        Rings = new RingFinder();
        Valences = new ValenceCalculator();
    }

    /// <summary>Returns the number of atoms.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <returns>The atom count, or <c>0</c> for <c>null</c>.</returns>
    public int GetAtomCount(Molecule? molecule)
    {
        if (molecule == null)
        {
            return 0;
        }

        return molecule.Atoms.Count;
    }

    /// <summary>Returns the number of bonds.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <returns>The bond count, or <c>0</c> for <c>null</c>.</returns>
    public int GetBondCount(Molecule? molecule)
    {
        if (molecule == null)
        {
            return 0;
        }

        return molecule.Bonds.Count;
    }

    /// <summary>Returns an independent copy of an atom.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The copied atom, or an empty atom when unavailable.</returns>
    public Atom GetAtom(Molecule? molecule, int atomId)
    {
        if (molecule == null || !molecule.IsValidAtomId(atomId))
        {
            return Atom.CreateEmpty();
        }

        return MoleculeBuilder.CloneAtom(molecule.Atoms[atomId], atomId);
    }

    /// <summary>Returns an independent copy of a bond.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="bondId">The zero-based bond identifier.</param>
    /// <returns>The copied bond, or an empty bond when unavailable.</returns>
    public Bond GetBond(Molecule? molecule, int bondId)
    {
        if (molecule == null || bondId < 0 || bondId >= molecule.Bonds.Count)
        {
            return Bond.CreateEmpty();
        }

        Bond bond = molecule.Bonds[bondId];
        return MoleculeBuilder.CloneBond(bond, bond.Id, bond.BeginAtomId, bond.EndAtomId);
    }

    /// <summary>Returns an independent copy of the bond connecting two atoms.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="atomIdA">The first atom identifier.</param>
    /// <param name="atomIdB">The second atom identifier.</param>
    /// <returns>The copied bond, or an empty bond when none exists.</returns>
    public Bond GetBondBetween(Molecule? molecule, int atomIdA, int atomIdB)
    {
        if (molecule == null)
        {
            return Bond.CreateEmpty();
        }

        int bondId = molecule.GetBondIdBetween(atomIdA, atomIdB);
        return GetBond(molecule, bondId);
    }

    /// <summary>Returns neighbor atom identifiers in bond order.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>An independent list, or an empty list when unavailable.</returns>
    public List<int> GetNeighborAtomIds(Molecule? molecule, int atomId)
    {
        if (molecule == null)
        {
            return new List<int>();
        }

        return CopyInts(molecule.GetNeighborAtomIds(atomId));
    }

    /// <summary>Returns neighbor bond identifiers in bond order.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>An independent list, or an empty list when unavailable.</returns>
    public List<int> GetNeighborBondIds(Molecule? molecule, int atomId)
    {
        if (molecule == null)
        {
            return new List<int>();
        }

        return CopyInts(molecule.GetNeighborBondIds(atomId));
    }

    /// <summary>Returns the atom degree.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The degree, or <c>0</c> when unavailable.</returns>
    public int GetDegree(Molecule? molecule, int atomId)
    {
        if (molecule == null)
        {
            return 0;
        }

        return molecule.GetDegree(atomId);
    }

    /// <summary>Calculates valence information for an atom.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The valence result, with an unset atom identifier when unavailable.</returns>
    public ValenceResult GetValence(Molecule? molecule, int atomId)
    {
        return Valences.CalculateAtomValence(molecule, atomId);
    }

    /// <summary>Determines whether an atom belongs to a ring.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns><c>true</c> when the atom is in a ring; otherwise, <c>false</c>.</returns>
    public bool AtomIsInRing(Molecule? molecule, int atomId)
    {
        return Rings.AtomIsInRing(molecule, atomId);
    }

    /// <summary>Determines whether a bond belongs to a ring.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="bondId">The zero-based bond identifier.</param>
    /// <returns><c>true</c> when the bond is in a ring; otherwise, <c>false</c>.</returns>
    public bool BondIsInRing(Molecule? molecule, int bondId)
    {
        return Rings.BondIsInRing(molecule, bondId);
    }

    /// <summary>Returns the number of rings containing an atom.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The ring membership count.</returns>
    public int GetAtomRingCount(Molecule? molecule, int atomId)
    {
        return Rings.EnsureRingCache(molecule).GetAtomRingCount(atomId);
    }

    /// <summary>Returns the number of rings containing a bond.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="bondId">The zero-based bond identifier.</param>
    /// <returns>The ring membership count.</returns>
    public int GetBondRingCount(Molecule? molecule, int bondId)
    {
        return Rings.EnsureRingCache(molecule).GetBondRingCount(bondId);
    }

    /// <summary>Returns the number of connected components.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <returns>The component count.</returns>
    public int GetComponentCount(Molecule? molecule)
    {
        return Components.FindComponents(molecule).Count;
    }

    /// <summary>Returns the component identifier for an atom.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The component identifier, or <c>-1</c> when unavailable.</returns>
    public int GetComponentId(Molecule? molecule, int atomId)
    {
        if (molecule == null || !molecule.IsValidAtomId(atomId))
        {
            return -1;
        }

        return Components.GetComponentIds(molecule)[atomId];
    }

    /// <summary>Returns the component identifier for a bond.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="bondId">The zero-based bond identifier.</param>
    /// <returns>The component identifier, or <c>-1</c> when unavailable.</returns>
    public int GetBondComponentId(Molecule? molecule, int bondId)
    {
        if (molecule == null || bondId < 0 || bondId >= molecule.Bonds.Count)
        {
            return -1;
        }

        Bond bond = molecule.Bonds[bondId];
        if (!molecule.IsValidAtomId(bond.BeginAtomId) || !molecule.IsValidAtomId(bond.EndAtomId))
        {
            return -1;
        }

        return GetComponentId(molecule, bond.BeginAtomId);
    }

    /// <summary>Returns the atom identifiers in a component.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="componentId">The zero-based component identifier.</param>
    /// <returns>An independent member list.</returns>
    public List<int> GetComponentAtomIds(Molecule? molecule, int componentId)
    {
        ComponentInfo component = FindComponent(molecule, componentId);
        return CopyInts(component.AtomIds);
    }

    /// <summary>Returns the bond identifiers in a component.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="componentId">The zero-based component identifier.</param>
    /// <returns>An independent member list.</returns>
    public List<int> GetComponentBondIds(Molecule? molecule, int componentId)
    {
        ComponentInfo component = FindComponent(molecule, componentId);
        return CopyInts(component.BondIds);
    }

    /// <summary>Returns the number of conformers.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <returns>The conformer count.</returns>
    public int GetConformerCount(Molecule? molecule)
    {
        if (molecule == null)
        {
            return 0;
        }

        return molecule.Conformers.Count;
    }

    /// <summary>Returns an independent copy of a conformer.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="conformerId">The zero-based conformer identifier.</param>
    /// <returns>The copied conformer, or an empty conformer when unavailable.</returns>
    public Conformer GetConformer(Molecule? molecule, int conformerId)
    {
        if (molecule == null || conformerId < 0 || conformerId >= molecule.Conformers.Count)
        {
            return Conformer.CreateEmpty();
        }

        return molecule.Conformers[conformerId].Clone();
    }

    /// <summary>Returns independent copies of all conformers.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <returns>The conformers in identifier order.</returns>
    public List<Conformer> GetConformers(Molecule? molecule)
    {
        List<Conformer> conformers = new List<Conformer>();
        if (molecule == null)
        {
            return conformers;
        }

        int index = 0;
        while (index < molecule.Conformers.Count)
        {
            conformers.Add(molecule.Conformers[index].Clone());
            index = index + 1;
        }

        return conformers;
    }

    /// <summary>Returns the number of stereo records.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <returns>The stereo record count.</returns>
    public int GetStereoRecordCount(Molecule? molecule)
    {
        if (molecule == null)
        {
            return 0;
        }

        return molecule.StereoRecords.Count;
    }

    /// <summary>Returns an independent copy of a stereo record.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="stereoRecordId">The zero-based stereo record identifier.</param>
    /// <returns>The copied record, or an empty record when unavailable.</returns>
    public StereoRecord GetStereoRecord(Molecule? molecule, int stereoRecordId)
    {
        if (molecule == null || stereoRecordId < 0 || stereoRecordId >= molecule.StereoRecords.Count)
        {
            return new StereoRecord();
        }

        return MoleculeBuilder.CloneStereoRecord(molecule.StereoRecords[stereoRecordId], stereoRecordId);
    }

    /// <summary>Returns independent copies of all stereo records.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <returns>The stereo records in identifier order.</returns>
    public List<StereoRecord> GetStereoRecords(Molecule? molecule)
    {
        List<StereoRecord> records = new List<StereoRecord>();
        if (molecule == null)
        {
            return records;
        }

        int index = 0;
        while (index < molecule.StereoRecords.Count)
        {
            records.Add(MoleculeBuilder.CloneStereoRecord(molecule.StereoRecords[index], index));
            index = index + 1;
        }

        return records;
    }

    /// <summary>Returns an independent copy of all molecule metadata.</summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <returns>The copied metadata, or empty metadata for <c>null</c>.</returns>
    public MoleculeMetadata GetMetadata(Molecule? molecule)
    {
        if (molecule == null)
        {
            return new MoleculeMetadata();
        }

        return molecule.Metadata.Clone();
    }

    internal ComponentInfo FindComponent(Molecule? molecule, int componentId)
    {
        List<ComponentInfo> components = Components.FindComponents(molecule);
        int index = 0;
        while (index < components.Count)
        {
            if (components[index].ComponentId == componentId)
            {
                return components[index];
            }

            index = index + 1;
        }

        return new ComponentInfo();
    }

    internal List<int> CopyInts(List<int> source)
    {
        List<int> copy = new List<int>();
        int index = 0;
        while (index < source.Count)
        {
            copy.Add(source[index]);
            index = index + 1;
        }

        return copy;
    }
}
