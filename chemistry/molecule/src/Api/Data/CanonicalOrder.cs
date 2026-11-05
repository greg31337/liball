using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides canonical order data and operations for molecular graphs.
/// </summary>
public class CanonicalOrder
{
    /// <summary>
    /// The atom identifiers by canonical index.
    /// </summary>
    public List<int> AtomIdsByCanonicalIndex;
    /// <summary>
    /// The canonical atom index by atom identifier.
    /// </summary>
    public List<int> CanonicalAtomIndexByAtomId;
    /// <summary>
    /// The bond identifiers by canonical index.
    /// </summary>
    public List<int> BondIdsByCanonicalIndex;
    /// <summary>
    /// The canonical bond index by bond identifier.
    /// </summary>
    public List<int> CanonicalBondIndexByBondId;
    /// <summary>
    /// The bond begin canonical atom identifiers by canonical index.
    /// </summary>
    public List<int> BondBeginCanonicalAtomIdsByCanonicalIndex;
    /// <summary>
    /// The bond end canonical atom identifiers by canonical index.
    /// </summary>
    public List<int> BondEndCanonicalAtomIdsByCanonicalIndex;
    internal AtomRanker Ranker;

    /// <summary>
    /// Initializes a new instance of the <see cref="CanonicalOrder"/> class.
    /// </summary>
    public CanonicalOrder()
    {
        AtomIdsByCanonicalIndex = new List<int>();
        CanonicalAtomIndexByAtomId = new List<int>();
        BondIdsByCanonicalIndex = new List<int>();
        CanonicalBondIndexByBondId = new List<int>();
        BondBeginCanonicalAtomIdsByCanonicalIndex = new List<int>();
        BondEndCanonicalAtomIdsByCanonicalIndex = new List<int>();
        Ranker = new AtomRanker();
    }

    /// <summary>
    /// From molecule.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting canonical order.</returns>
    internal static CanonicalOrder FromMolecule(Molecule? molecule)
    {
        CanonicalOrder order = new CanonicalOrder();
        order.Build(molecule);
        return order;
    }

    /// <summary>
    /// Returns the molecule currently held by the builder.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    internal void Build(Molecule? molecule)
    {
        Clear();

        if (molecule == null)
        {
            return;
        }

        AtomRankingResult ranks = Ranker.RankAtoms(molecule);
        FillAtomOrder(molecule, ranks);
        FillBondOrder(molecule);
    }

    /// <summary>
    /// Resets this object to its empty state.
    /// </summary>
    internal void Clear()
    {
        AtomIdsByCanonicalIndex.Clear();
        CanonicalAtomIndexByAtomId.Clear();
        BondIdsByCanonicalIndex.Clear();
        CanonicalBondIndexByBondId.Clear();
        BondBeginCanonicalAtomIdsByCanonicalIndex.Clear();
        BondEndCanonicalAtomIdsByCanonicalIndex.Clear();
    }

    /// <summary>
    /// Returns the original atom identifier.
    /// </summary>
    /// <param name="canonicalAtomIndex">The canonical atom index.</param>
    /// <returns>The zero-based identifier, or <c>-1</c> when the operation cannot resolve one.</returns>
    internal int GetOriginalAtomId(int canonicalAtomIndex)
    {
        if (canonicalAtomIndex < 0 || canonicalAtomIndex >= AtomIdsByCanonicalIndex.Count)
        {
            return -1;
        }

        return AtomIdsByCanonicalIndex[canonicalAtomIndex];
    }

    /// <summary>
    /// Returns the canonical atom index.
    /// </summary>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The resulting count or integer value.</returns>
    internal int GetCanonicalAtomIndex(int atomId)
    {
        if (atomId < 0 || atomId >= CanonicalAtomIndexByAtomId.Count)
        {
            return -1;
        }

        return CanonicalAtomIndexByAtomId[atomId];
    }

    /// <summary>
    /// Returns the original bond identifier.
    /// </summary>
    /// <param name="canonicalBondIndex">The canonical bond index.</param>
    /// <returns>The zero-based identifier, or <c>-1</c> when the operation cannot resolve one.</returns>
    internal int GetOriginalBondId(int canonicalBondIndex)
    {
        if (canonicalBondIndex < 0 || canonicalBondIndex >= BondIdsByCanonicalIndex.Count)
        {
            return -1;
        }

        return BondIdsByCanonicalIndex[canonicalBondIndex];
    }

    /// <summary>
    /// Returns the canonical bond index.
    /// </summary>
    /// <param name="bondId">The zero-based bond identifier.</param>
    /// <returns>The resulting count or integer value.</returns>
    internal int GetCanonicalBondIndex(int bondId)
    {
        if (bondId < 0 || bondId >= CanonicalBondIndexByBondId.Count)
        {
            return -1;
        }

        return CanonicalBondIndexByBondId[bondId];
    }

    /// <summary>
    /// Returns the canonical bond begin atom index.
    /// </summary>
    /// <param name="canonicalBondIndex">The canonical bond index.</param>
    /// <returns>The resulting count or integer value.</returns>
    internal int GetCanonicalBondBeginAtomIndex(int canonicalBondIndex)
    {
        if (canonicalBondIndex < 0 || canonicalBondIndex >= BondBeginCanonicalAtomIdsByCanonicalIndex.Count)
        {
            return -1;
        }

        return BondBeginCanonicalAtomIdsByCanonicalIndex[canonicalBondIndex];
    }

    /// <summary>
    /// Returns the canonical bond end atom index.
    /// </summary>
    /// <param name="canonicalBondIndex">The canonical bond index.</param>
    /// <returns>The resulting count or integer value.</returns>
    internal int GetCanonicalBondEndAtomIndex(int canonicalBondIndex)
    {
        if (canonicalBondIndex < 0 || canonicalBondIndex >= BondEndCanonicalAtomIdsByCanonicalIndex.Count)
        {
            return -1;
        }

        return BondEndCanonicalAtomIdsByCanonicalIndex[canonicalBondIndex];
    }

    internal void FillAtomOrder(Molecule molecule, AtomRankingResult ranks)
    {
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            CanonicalAtomIndexByAtomId.Add(-1);
            atomIndex = atomIndex + 1;
        }

        List<AtomRank> orderedRanks = CopyRanks(ranks);
        SortAtomRanksByRank(orderedRanks);

        int canonicalIndex = 0;
        while (canonicalIndex < orderedRanks.Count)
        {
            int atomId = orderedRanks[canonicalIndex].AtomId;
            AtomIdsByCanonicalIndex.Add(atomId);
            if (atomId >= 0 && atomId < CanonicalAtomIndexByAtomId.Count)
            {
                CanonicalAtomIndexByAtomId[atomId] = canonicalIndex;
            }

            canonicalIndex = canonicalIndex + 1;
        }
    }

    internal void FillBondOrder(Molecule molecule)
    {
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            CanonicalBondIndexByBondId.Add(-1);
            bondIndex = bondIndex + 1;
        }

        List<CanonicalBondOrderEntry> entries = new List<CanonicalBondOrderEntry>();
        bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            entries.Add(CreateBondOrderEntry(bond));
            bondIndex = bondIndex + 1;
        }

        SortBondOrderEntries(entries);

        int canonicalIndex = 0;
        while (canonicalIndex < entries.Count)
        {
            CanonicalBondOrderEntry entry = entries[canonicalIndex];
            BondIdsByCanonicalIndex.Add(entry.BondId);
            BondBeginCanonicalAtomIdsByCanonicalIndex.Add(entry.BeginCanonicalAtomIndex);
            BondEndCanonicalAtomIdsByCanonicalIndex.Add(entry.EndCanonicalAtomIndex);
            if (entry.BondId >= 0 && entry.BondId < CanonicalBondIndexByBondId.Count)
            {
                CanonicalBondIndexByBondId[entry.BondId] = canonicalIndex;
            }

            canonicalIndex = canonicalIndex + 1;
        }
    }

    internal CanonicalBondOrderEntry CreateBondOrderEntry(Bond? bond)
    {
        CanonicalBondOrderEntry entry = new CanonicalBondOrderEntry();

        if (bond == null)
        {
            return entry;
        }

        int beginCanonicalAtomIndex = GetCanonicalAtomIndex(bond.BeginAtomId);
        int endCanonicalAtomIndex = GetCanonicalAtomIndex(bond.EndAtomId);
        entry.BondId = bond.Id;

        if (beginCanonicalAtomIndex <= endCanonicalAtomIndex)
        {
            entry.BeginCanonicalAtomIndex = beginCanonicalAtomIndex;
            entry.EndCanonicalAtomIndex = endCanonicalAtomIndex;
        }
        else
        {
            entry.BeginCanonicalAtomIndex = endCanonicalAtomIndex;
            entry.EndCanonicalAtomIndex = beginCanonicalAtomIndex;
        }

        entry.OrderValue = (int)bond.Order;
        entry.IsAromaticValue = BoolToInt(bond.IsAromatic);
        entry.IsDativeValue = BoolToInt(bond.IsDative);
        entry.StereoValue = (int)bond.Stereo;
        entry.DirectionValue = (int)bond.Direction;
        entry.QueryOperationValue = (int)bond.QueryOperation;
        entry.QueryValue = NormalizeString(bond.QueryValue);
        entry.SourceFlags = NormalizeString(bond.SourceFlags);
        return entry;
    }

    internal List<AtomRank> CopyRanks(AtomRankingResult? ranks)
    {
        List<AtomRank> copy = new List<AtomRank>();

        if (ranks == null)
        {
            return copy;
        }

        int index = 0;
        while (index < ranks.Ranks.Count)
        {
            AtomRank source = ranks.Ranks[index];
            AtomRank rank = new AtomRank();
            rank.AtomId = source.AtomId;
            rank.Rank = source.Rank;
            rank.InvariantValue = source.InvariantValue;
            rank.RefinementValue = source.RefinementValue;
            rank.TieBreakValue = source.TieBreakValue;
            copy.Add(rank);
            index = index + 1;
        }

        return copy;
    }

    internal void SortAtomRanksByRank(List<AtomRank> ranks)
    {
        int index = 1;
        while (index < ranks.Count)
        {
            AtomRank value = ranks[index];
            int scan = index - 1;
            while (scan >= 0 && CompareAtomRanksByCanonicalOrder(ranks[scan], value) > 0)
            {
                ranks[scan + 1] = ranks[scan];
                scan = scan - 1;
            }

            ranks[scan + 1] = value;
            index = index + 1;
        }
    }

    internal int CompareAtomRanksByCanonicalOrder(AtomRank left, AtomRank right)
    {
        if (left.Rank != right.Rank)
        {
            return left.Rank - right.Rank;
        }

        return left.AtomId - right.AtomId;
    }

    internal void SortBondOrderEntries(List<CanonicalBondOrderEntry> entries)
    {
        int index = 1;
        while (index < entries.Count)
        {
            CanonicalBondOrderEntry value = entries[index];
            int scan = index - 1;
            while (scan >= 0 && CompareBondOrderEntries(entries[scan], value) > 0)
            {
                entries[scan + 1] = entries[scan];
                scan = scan - 1;
            }

            entries[scan + 1] = value;
            index = index + 1;
        }
    }

    internal int CompareBondOrderEntries(CanonicalBondOrderEntry left, CanonicalBondOrderEntry right)
    {
        int comparison = CompareInt(left.BeginCanonicalAtomIndex, right.BeginCanonicalAtomIndex);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = CompareInt(left.EndCanonicalAtomIndex, right.EndCanonicalAtomIndex);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = CompareInt(left.OrderValue, right.OrderValue);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = CompareInt(left.IsAromaticValue, right.IsAromaticValue);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = CompareInt(left.IsDativeValue, right.IsDativeValue);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = CompareInt(left.StereoValue, right.StereoValue);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = CompareInt(left.DirectionValue, right.DirectionValue);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = CompareInt(left.QueryOperationValue, right.QueryOperationValue);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = string.CompareOrdinal(left.QueryValue, right.QueryValue);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = string.CompareOrdinal(left.SourceFlags, right.SourceFlags);
        if (comparison != 0)
        {
            return comparison;
        }

        return CompareInt(left.BondId, right.BondId);
    }

    internal int CompareInt(int left, int right)
    {
        if (left < right)
        {
            return -1;
        }

        if (left > right)
        {
            return 1;
        }

        return 0;
    }

    internal int BoolToInt(bool value)
    {
        if (value)
        {
            return 1;
        }

        return 0;
    }

    internal string NormalizeString(string? value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }
}

