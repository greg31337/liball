using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides query molecule data and operations for molecular graphs.
/// </summary>
public class QueryMolecule
{
    /// <summary>
    /// The atoms in identifier order; each atom identifier is its list index.
    /// </summary>
    public List<QueryAtom> Atoms;
    /// <summary>
    /// The bonds in identifier order; each bond identifier is its list index.
    /// </summary>
    public List<QueryBond> Bonds;
    /// <summary>
    /// Component-group identifiers by atom identifier; <c>-1</c> means the atom is not parenthesized into a component group.
    /// </summary>
    public List<int> ComponentGroupIdsByAtomId;
    internal List<List<int>> NeighborAtomIdsByAtomId;
    internal List<List<int>> NeighborBondIdsByAtomId;
    internal List<int> ComponentIdsByAtomId;
    internal bool IsAdjacencyCacheValid;
    internal bool IsComponentCacheValid;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryMolecule"/> class.
    /// </summary>
    public QueryMolecule()
    {
        Atoms = new List<QueryAtom>();
        Bonds = new List<QueryBond>();
        ComponentGroupIdsByAtomId = new List<int>();
        NeighborAtomIdsByAtomId = new List<List<int>>();
        NeighborBondIdsByAtomId = new List<List<int>>();
        ComponentIdsByAtomId = new List<int>();
        IsAdjacencyCacheValid = false;
        IsComponentCacheValid = false;
    }

    /// <summary>
    /// Adds the atom.
    /// </summary>
    /// <param name="atom">The atom.</param>
    /// <returns>The zero-based identifier, or <c>-1</c> when the operation cannot resolve one.</returns>
    internal int AddAtom(QueryAtom atom)
    {
        QueryAtom queryAtom = CloneAtom(atom);
        queryAtom.Id = Atoms.Count;
        Atoms.Add(queryAtom);
        ComponentGroupIdsByAtomId.Add(-1);
        InvalidateCaches();
        return queryAtom.Id;
    }

    /// <summary>
    /// Adds the any atom.
    /// </summary>
    /// <returns>The zero-based identifier, or <c>-1</c> when the operation cannot resolve one.</returns>
    internal int AddAnyAtom()
    {
        QueryAtom atom = new QueryAtom();
        atom.SetAny();
        return AddAtom(atom);
    }

    /// <summary>
    /// Adds the bond.
    /// </summary>
    /// <param name="beginQueryAtomId">The begin query atom identifier.</param>
    /// <param name="endQueryAtomId">The end query atom identifier.</param>
    /// <param name="bond">The bond.</param>
    /// <returns>The zero-based identifier, or <c>-1</c> when the operation cannot resolve one.</returns>
    internal int AddBond(int beginQueryAtomId, int endQueryAtomId, QueryBond bond)
    {
        if (!IsValidAtomId(beginQueryAtomId) || !IsValidAtomId(endQueryAtomId))
        {
            return -1;
        }

        QueryBond queryBond = CloneBond(bond);
        queryBond.Id = Bonds.Count;
        queryBond.BeginQueryAtomId = beginQueryAtomId;
        queryBond.EndQueryAtomId = endQueryAtomId;
        Bonds.Add(queryBond);
        InvalidateCaches();
        return queryBond.Id;
    }

    /// <summary>
    /// Adds the any bond.
    /// </summary>
    /// <param name="beginQueryAtomId">The begin query atom identifier.</param>
    /// <param name="endQueryAtomId">The end query atom identifier.</param>
    /// <returns>The zero-based identifier, or <c>-1</c> when the operation cannot resolve one.</returns>
    internal int AddAnyBond(int beginQueryAtomId, int endQueryAtomId)
    {
        QueryBond bond = new QueryBond();
        bond.SetAny();
        return AddBond(beginQueryAtomId, endQueryAtomId, bond);
    }

    /// <summary>
    /// Returns the atom.
    /// </summary>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The resulting query atom.</returns>
    internal QueryAtom GetAtom(int atomId)
    {
        if (!IsValidAtomId(atomId))
        {
            return new QueryAtom();
        }

        return Atoms[atomId];
    }

    /// <summary>
    /// Returns the bond.
    /// </summary>
    /// <param name="bondId">The zero-based bond identifier.</param>
    /// <returns>The resulting query bond.</returns>
    internal QueryBond GetBond(int bondId)
    {
        if (!IsValidBondId(bondId))
        {
            return new QueryBond();
        }

        return Bonds[bondId];
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

    internal bool IsValidAtomId(int atomId)
    {
        return atomId >= 0 && atomId < Atoms.Count;
    }

    internal bool IsValidBondId(int bondId)
    {
        return bondId >= 0 && bondId < Bonds.Count;
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
            QueryBond bond = Bonds[bondIndex];
            if (IsValidAtomId(bond.BeginQueryAtomId) && IsValidAtomId(bond.EndQueryAtomId))
            {
                NeighborAtomIdsByAtomId[bond.BeginQueryAtomId].Add(bond.EndQueryAtomId);
                NeighborBondIdsByAtomId[bond.BeginQueryAtomId].Add(bond.Id);
                if (bond.BeginQueryAtomId != bond.EndQueryAtomId)
                {
                    NeighborAtomIdsByAtomId[bond.EndQueryAtomId].Add(bond.BeginQueryAtomId);
                    NeighborBondIdsByAtomId[bond.EndQueryAtomId].Add(bond.Id);
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
    /// Returns the bond identifier between.
    /// </summary>
    /// <param name="atomIdA">The atom identifier a.</param>
    /// <param name="atomIdB">The atom identifier b.</param>
    /// <returns>The zero-based identifier, or <c>-1</c> when the operation cannot resolve one.</returns>
    internal int GetBondIdBetween(int atomIdA, int atomIdB)
    {
        int index = 0;
        while (index < Bonds.Count)
        {
            QueryBond bond = Bonds[index];
            if (bond.BeginQueryAtomId == atomIdA && bond.EndQueryAtomId == atomIdB)
            {
                return bond.Id;
            }

            if (bond.BeginQueryAtomId == atomIdB && bond.EndQueryAtomId == atomIdA)
            {
                return bond.Id;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void InvalidateCaches()
    {
        NeighborAtomIdsByAtomId.Clear();
        NeighborBondIdsByAtomId.Clear();
        ComponentIdsByAtomId.Clear();
        IsAdjacencyCacheValid = false;
        IsComponentCacheValid = false;
    }

    internal void SetComponentGroupId(int atomId, int componentGroupId)
    {
        if (atomId >= 0 && atomId < ComponentGroupIdsByAtomId.Count)
        {
            ComponentGroupIdsByAtomId[atomId] = componentGroupId;
            Atoms[atomId].ComponentGroupId = componentGroupId;
        }
    }

    internal int GetComponentGroupId(int atomId)
    {
        if (atomId < 0 || atomId >= ComponentGroupIdsByAtomId.Count)
        {
            return -1;
        }

        return ComponentGroupIdsByAtomId[atomId];
    }

    internal void EnsureComponentCache()
    {
        if (IsComponentCacheValid)
        {
            return;
        }

        EnsureAdjacencyCache();
        ComponentIdsByAtomId.Clear();
        int atomIndex = 0;
        while (atomIndex < Atoms.Count)
        {
            ComponentIdsByAtomId.Add(-1);
            atomIndex = atomIndex + 1;
        }

        int componentId = 0;
        atomIndex = 0;
        while (atomIndex < Atoms.Count)
        {
            if (ComponentIdsByAtomId[atomIndex] < 0)
            {
                AssignComponent(atomIndex, componentId);
                componentId = componentId + 1;
            }

            atomIndex = atomIndex + 1;
        }

        IsComponentCacheValid = true;
    }

    internal void AssignComponent(int startAtomId, int componentId)
    {
        List<int> queue = new List<int>();
        ComponentIdsByAtomId[startAtomId] = componentId;
        queue.Add(startAtomId);
        int queueIndex = 0;
        while (queueIndex < queue.Count)
        {
            int atomId = queue[queueIndex];
            List<int> neighbors = NeighborAtomIdsByAtomId[atomId];
            int neighborIndex = 0;
            while (neighborIndex < neighbors.Count)
            {
                int neighborAtomId = neighbors[neighborIndex];
                if (ComponentIdsByAtomId[neighborAtomId] < 0)
                {
                    ComponentIdsByAtomId[neighborAtomId] = componentId;
                    queue.Add(neighborAtomId);
                }

                neighborIndex = neighborIndex + 1;
            }

            queueIndex = queueIndex + 1;
        }
    }

    /// <summary>
    /// Returns the component identifier.
    /// </summary>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The zero-based identifier, or <c>-1</c> when the operation cannot resolve one.</returns>
    internal int GetComponentId(int atomId)
    {
        EnsureComponentCache();
        if (!IsValidAtomId(atomId))
        {
            return -1;
        }

        return ComponentIdsByAtomId[atomId];
    }

    /// <summary>
    /// Returns the component count.
    /// </summary>
    /// <returns>The resulting count or integer value.</returns>
    internal int GetComponentCount()
    {
        EnsureComponentCache();
        int count = 0;
        int index = 0;
        while (index < ComponentIdsByAtomId.Count)
        {
            if (ComponentIdsByAtomId[index] + 1 > count)
            {
                count = ComponentIdsByAtomId[index] + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal QueryAtom CloneAtom(QueryAtom? source)
    {
        QueryAtom atom = new QueryAtom();
        if (source == null)
        {
            return atom;
        }

        atom.Id = source.Id;
        atom.ComponentGroupId = source.ComponentGroupId;
        atom.RawText = source.RawText;
        atom.Expression = source.Expression;
        atom.MatchesAny = source.MatchesAny;
        atom.AtomicNumber = source.AtomicNumber;
        atom.HasAtomicNumber = source.HasAtomicNumber;
        atom.UsesAtomicNumberList = source.UsesAtomicNumberList;
        atom.NegateAtomicNumber = source.NegateAtomicNumber;
        atom.HasAromaticity = source.HasAromaticity;
        atom.IsAromatic = source.IsAromatic;
        atom.HasFormalCharge = source.HasFormalCharge;
        atom.FormalCharge = source.FormalCharge;
        atom.HasIsotopeMassNumber = source.HasIsotopeMassNumber;
        atom.IsotopeMassNumber = source.IsotopeMassNumber;
        atom.HasHydrogenCount = source.HasHydrogenCount;
        atom.HydrogenCount = source.HydrogenCount;
        atom.HasDegree = source.HasDegree;
        atom.Degree = source.Degree;
        atom.HasRingCount = source.HasRingCount;
        atom.RingCount = source.RingCount;
        atom.HasValence = source.HasValence;
        atom.Valence = source.Valence;

        int index = 0;
        while (index < source.AtomicNumbers.Count)
        {
            atom.AtomicNumbers.Add(source.AtomicNumbers[index]);
            index = index + 1;
        }

        return atom;
    }

    internal QueryBond CloneBond(QueryBond? source)
    {
        QueryBond bond = new QueryBond();
        if (source == null)
        {
            return bond;
        }

        bond.Id = source.Id;
        bond.BeginQueryAtomId = source.BeginQueryAtomId;
        bond.EndQueryAtomId = source.EndQueryAtomId;
        bond.RawText = source.RawText;
        bond.Expression = source.Expression;
        bond.MatchesAny = source.MatchesAny;
        bond.HasOrder = source.HasOrder;
        bond.Order = source.Order;
        bond.HasAromaticity = source.HasAromaticity;
        bond.IsAromatic = source.IsAromatic;
        bond.HasDirection = source.HasDirection;
        bond.Direction = source.Direction;
        bond.HasStereo = source.HasStereo;
        bond.Stereo = source.Stereo;
        bond.HasDative = source.HasDative;
        bond.IsDative = source.IsDative;
        bond.IsRingBond = source.IsRingBond;
        return bond;
    }
}
