using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Represents a mutable molecular graph together with coordinates, stereochemistry, and metadata.
/// </summary>
public class Molecule
{
    /// <summary>
    /// The atoms in identifier order; each atom identifier is its list index.
    /// </summary>
    /// <remarks>Use <see cref="MoleculeBuilder"/> for structural edits, or call <see cref="InvalidateCaches"/> after changing this list directly.</remarks>
    public List<Atom> Atoms;
    /// <summary>
    /// The bonds in identifier order; each bond identifier is its list index.
    /// </summary>
    /// <remarks>Use <see cref="MoleculeBuilder"/> for structural edits, or call <see cref="InvalidateCaches"/> after changing this list directly.</remarks>
    public List<Bond> Bonds;
    /// <summary>
    /// Names, comments, format fields, and arbitrary string metadata associated with the molecule.
    /// </summary>
    public MoleculeMetadata Metadata;
    /// <summary>
    /// Coordinate sets whose coordinate indices correspond to atom identifiers.
    /// </summary>
    public List<Conformer> Conformers;
    /// <summary>
    /// Atom-centered and bond-centered stereochemistry records.
    /// </summary>
    public List<StereoRecord> StereoRecords;
    /// <summary>
    /// Enhanced stereo groups that refer to entries in <see cref="StereoRecords"/>.
    /// </summary>
    public StereoGroups StereoGroups;
    /// <summary>
    /// Residue records whose atom identifiers refer to <see cref="Atoms"/>.
    /// </summary>
    public List<Residue> Residues;
    /// <summary>
    /// Chain records whose residue identifiers refer to <see cref="Residues"/>.
    /// </summary>
    public List<Chain> Chains;
    /// <summary>
    /// Format-independent polymer groupings over the molecule's chains and residues.
    /// </summary>
    public List<Polymer> Polymers;
    /// <summary>
    /// Models that group atoms and conformers in a multi-model structure.
    /// </summary>
    public List<MoleculeModel> Models;
    internal List<List<int>> NeighborAtomIdsByAtomId;
    internal List<List<int>> NeighborBondIdsByAtomId;
    internal List<int> ComponentIdsByAtomId;
    internal RingSet RingCache;
    internal bool IsAdjacencyCacheValid;
    internal bool IsComponentCacheValid;
    internal bool IsRingCacheValid;

    /// <summary>
    /// Initializes a new instance of the <see cref="Molecule"/> class.
    /// </summary>
    public Molecule()
    {
        Atoms = new List<Atom>();
        Bonds = new List<Bond>();
        Metadata = new MoleculeMetadata();
        Conformers = new List<Conformer>();
        StereoRecords = new List<StereoRecord>();
        StereoGroups = new StereoGroups();
        Residues = new List<Residue>();
        Chains = new List<Chain>();
        Polymers = new List<Polymer>();
        Models = new List<MoleculeModel>();
        NeighborAtomIdsByAtomId = new List<List<int>>();
        NeighborBondIdsByAtomId = new List<List<int>>();
        ComponentIdsByAtomId = new List<int>();
        RingCache = new RingSet();
        IsAdjacencyCacheValid = false;
        IsComponentCacheValid = false;
        IsRingCacheValid = false;
    }

    /// <summary>
    /// Returns the atom count.
    /// </summary>
    /// <returns>The resulting count or integer value.</returns>
    internal int GetAtomCount()
    {
        return Atoms.Count;
    }

    /// <summary>
    /// Returns the bond count.
    /// </summary>
    /// <returns>The resulting count or integer value.</returns>
    internal int GetBondCount()
    {
        return Bonds.Count;
    }

    /// <summary>
    /// Returns the atom.
    /// </summary>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The resulting atom.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">The atom identifier is outside the atom list.</exception>
    internal Atom GetAtom(int atomId)
    {
        return Atoms[atomId];
    }

    /// <summary>
    /// Returns the bond.
    /// </summary>
    /// <param name="bondId">The zero-based bond identifier.</param>
    /// <returns>The resulting bond.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">The bond identifier is outside the bond list.</exception>
    internal Bond GetBond(int bondId)
    {
        return Bonds[bondId];
    }

    /// <summary>
    /// Returns the atoms.
    /// </summary>
    /// <remarks>The returned list is the live molecule storage. Prefer <see cref="MoleculeBuilder"/> for edits so identifiers, conformers, and caches stay synchronized.</remarks>
    /// <returns>The resulting values in deterministic order.</returns>
    internal List<Atom> GetAtoms()
    {
        return Atoms;
    }

    /// <summary>
    /// Returns the bonds.
    /// </summary>
    /// <remarks>The returned list is the live molecule storage. Prefer <see cref="MoleculeBuilder"/> for edits so identifiers and caches stay synchronized.</remarks>
    /// <returns>The resulting values in deterministic order.</returns>
    internal List<Bond> GetBonds()
    {
        return Bonds;
    }

    /// <summary>
    /// Returns the conformers.
    /// </summary>
    /// <returns>The resulting values in deterministic order.</returns>
    internal List<Conformer> GetConformers()
    {
        return Conformers;
    }

    /// <summary>
    /// Returns the conformer count.
    /// </summary>
    /// <returns>The resulting count or integer value.</returns>
    internal int GetConformerCount()
    {
        return Conformers.Count;
    }

    /// <summary>
    /// Determines whether the value is valid conformer identifier.
    /// </summary>
    /// <param name="conformerId">The conformer identifier.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsValidConformerId(int conformerId)
    {
        return conformerId >= 0 && conformerId < Conformers.Count;
    }

    /// <summary>
    /// Returns the conformer.
    /// </summary>
    /// <param name="conformerId">The conformer identifier.</param>
    /// <returns>The resulting conformer.</returns>
    internal Conformer GetConformer(int conformerId)
    {
        if (!IsValidConformerId(conformerId))
        {
            return Conformer.CreateEmpty();
        }

        return Conformers[conformerId];
    }

    /// <summary>
    /// Adds the conformer.
    /// </summary>
    /// <param name="conformer">The conformer.</param>
    /// <returns>The zero-based identifier, or <c>-1</c> when the operation cannot resolve one.</returns>
    internal int AddConformer(Conformer? conformer)
    {
        Conformer stored;
        if (conformer == null)
        {
            stored = new Conformer(Atoms.Count);
        }
        else
        {
            stored = conformer.Clone();
            stored.EnsureCoordinateCount(Atoms.Count);
        }

        stored.Id = Conformers.Count;
        Conformers.Add(stored);
        return stored.Id;
    }

    /// <summary>
    /// Clears the conformers.
    /// </summary>
    internal void ClearConformers()
    {
        Conformers.Clear();
    }

    internal void EnsureConformerCoordinateCounts()
    {
        int index = 0;
        while (index < Conformers.Count)
        {
            Conformers[index].EnsureCoordinateCount(Atoms.Count);
            index = index + 1;
        }
    }

    internal void RemoveAtomCoordinatesFromConformers(int atomId)
    {
        int index = 0;
        while (index < Conformers.Count)
        {
            Conformers[index].RemoveAtomCoordinate(atomId);
            index = index + 1;
        }
    }

    /// <summary>
    /// Returns the stereo records.
    /// </summary>
    /// <returns>The resulting values in deterministic order.</returns>
    internal List<StereoRecord> GetStereoRecords()
    {
        return StereoRecords;
    }

    /// <summary>
    /// Returns the stereo groups.
    /// </summary>
    /// <returns>The resulting stereo groups.</returns>
    internal StereoGroups GetStereoGroups()
    {
        return StereoGroups;
    }

    internal void EnsureAdjacencyCache()
    {
        if (IsAdjacencyCacheValid)
        {
            return;
        }

        NeighborAtomIdsByAtomId.Clear();
        NeighborBondIdsByAtomId.Clear();

        int atomIndex = 0;
        while (atomIndex < Atoms.Count)
        {
            NeighborAtomIdsByAtomId.Add(new List<int>());
            NeighborBondIdsByAtomId.Add(new List<int>());
            atomIndex = atomIndex + 1;
        }

        int bondIndex = 0;
        while (bondIndex < Bonds.Count)
        {
            Bond bond = Bonds[bondIndex];
            if (IsValidAtomId(bond.BeginAtomId) && IsValidAtomId(bond.EndAtomId))
            {
                NeighborAtomIdsByAtomId[bond.BeginAtomId].Add(bond.EndAtomId);
                NeighborBondIdsByAtomId[bond.BeginAtomId].Add(bond.Id);

                if (bond.BeginAtomId != bond.EndAtomId)
                {
                    NeighborAtomIdsByAtomId[bond.EndAtomId].Add(bond.BeginAtomId);
                    NeighborBondIdsByAtomId[bond.EndAtomId].Add(bond.Id);
                }
            }

            bondIndex = bondIndex + 1;
        }

        IsAdjacencyCacheValid = true;
    }

    /// <summary>
    /// Returns the neighbor atom identifiers.
    /// </summary>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    internal List<int> GetNeighborAtomIds(int atomId)
    {
        EnsureAdjacencyCache();
        if (!IsValidAtomId(atomId))
        {
            return new List<int>();
        }

        return NeighborAtomIdsByAtomId[atomId];
    }

    /// <summary>
    /// Returns the neighbor bond identifiers.
    /// </summary>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    internal List<int> GetNeighborBondIds(int atomId)
    {
        EnsureAdjacencyCache();
        if (!IsValidAtomId(atomId))
        {
            return new List<int>();
        }

        return NeighborBondIdsByAtomId[atomId];
    }

    /// <summary>
    /// Returns the degree.
    /// </summary>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The resulting count or integer value.</returns>
    internal int GetDegree(int atomId)
    {
        EnsureAdjacencyCache();
        if (!IsValidAtomId(atomId))
        {
            return 0;
        }

        return NeighborAtomIdsByAtomId[atomId].Count;
    }

    /// <summary>
    /// Returns the bond between.
    /// </summary>
    /// <param name="atomIdA">The atom identifier a.</param>
    /// <param name="atomIdB">The atom identifier b.</param>
    /// <returns>The resulting bond, or <c>null</c> when no bond connects the atoms.</returns>
    internal Bond? GetBondBetween(int atomIdA, int atomIdB)
    {
        int bondId = GetBondIdBetween(atomIdA, atomIdB);
        if (bondId < 0)
        {
            return null;
        }

        return Bonds[bondId];
    }

    /// <summary>
    /// Returns the bond identifier between.
    /// </summary>
    /// <param name="atomIdA">The atom identifier a.</param>
    /// <param name="atomIdB">The atom identifier b.</param>
    /// <returns>The zero-based identifier, or <c>-1</c> when the operation cannot resolve one.</returns>
    internal int GetBondIdBetween(int atomIdA, int atomIdB)
    {
        if (!IsValidAtomId(atomIdA) || !IsValidAtomId(atomIdB))
        {
            return -1;
        }

        int index = 0;
        while (index < Bonds.Count)
        {
            Bond bond = Bonds[index];
            if (bond.BeginAtomId == atomIdA && bond.EndAtomId == atomIdB)
            {
                return bond.Id;
            }

            if (bond.BeginAtomId == atomIdB && bond.EndAtomId == atomIdA)
            {
                return bond.Id;
            }

            index = index + 1;
        }

        return -1;
    }

    /// <summary>
    /// Determines whether the value is valid atom identifier.
    /// </summary>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsValidAtomId(int atomId)
    {
        return atomId >= 0 && atomId < Atoms.Count;
    }

    /// <summary>
    /// Invalidate caches.
    /// </summary>
    internal void InvalidateCaches()
    {
        ClearCaches();
    }

    internal void InvalidateAdjacencyCache()
    {
        NeighborAtomIdsByAtomId.Clear();
        NeighborBondIdsByAtomId.Clear();
        IsAdjacencyCacheValid = false;
    }

    internal void ClearCaches()
    {
        NeighborAtomIdsByAtomId.Clear();
        NeighborBondIdsByAtomId.Clear();
        ComponentIdsByAtomId.Clear();
        RingCache = new RingSet();
        IsAdjacencyCacheValid = false;
        IsComponentCacheValid = false;
        IsRingCacheValid = false;
    }
}
