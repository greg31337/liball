using System.Collections.Generic;

namespace molecule;

internal class AtomInvariant
{
    internal int AtomId;
    internal long Value;
    internal int AtomicNumber;
    internal int IsotopeMassNumber;
    internal int FormalCharge;
    internal int Degree;
    internal int HydrogenCount;
    internal int BondOrderSum;
    internal int AromaticBondCount;
    internal int RingCount;
    internal int NeighborScore;

    internal AtomInvariant()
    {
        AtomId = -1;
        Value = 0;
        AtomicNumber = 0;
        IsotopeMassNumber = 0;
        FormalCharge = 0;
        Degree = 0;
        HydrogenCount = 0;
        BondOrderSum = 0;
        AromaticBondCount = 0;
        RingCount = 0;
        NeighborScore = 0;
    }
}

internal class AtomInvariantSet
{
    internal List<AtomInvariant> Invariants;

    internal AtomInvariantSet()
    {
        Invariants = new List<AtomInvariant>();
    }

    internal AtomInvariant GetInvariant(int atomId)
    {
        int index = 0;
        while (index < Invariants.Count)
        {
            if (Invariants[index].AtomId == atomId)
            {
                return Invariants[index];
            }

            index = index + 1;
        }

        return new AtomInvariant();
    }
}

internal class AtomRank
{
    internal int AtomId;
    internal int Rank;
    internal long InvariantValue;
    internal long RefinementValue;
    internal int TieBreakValue;

    internal AtomRank()
    {
        AtomId = -1;
        Rank = 0;
        InvariantValue = 0;
        RefinementValue = 0;
        TieBreakValue = 0;
    }
}

internal class AtomRankingResult
{
    internal List<AtomRank> Ranks;
    internal int IterationCount;
    internal bool WasFullySeparated;

    internal AtomRankingResult()
    {
        Ranks = new List<AtomRank>();
        IterationCount = 0;
        WasFullySeparated = false;
    }

    internal AtomRank GetAtomRank(int atomId)
    {
        int index = 0;
        while (index < Ranks.Count)
        {
            if (Ranks[index].AtomId == atomId)
            {
                return Ranks[index];
            }

            index = index + 1;
        }

        return new AtomRank();
    }
}

internal class AtomRanker
{
    internal RingFinder Rings;

    internal AtomRanker()
    {
        Rings = new RingFinder();
    }

    internal AtomInvariantSet CalculateInvariants(Molecule? molecule)
    {
        AtomInvariantSet result = new AtomInvariantSet();

        if (molecule == null)
        {
            return result;
        }

        RingSet ringSet = Rings.EnsureRingCache(molecule);
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            result.Invariants.Add(CalculateAtomInvariant(molecule, ringSet, atomIndex));
            atomIndex = atomIndex + 1;
        }

        return result;
    }

    internal AtomRankingResult RankAtoms(Molecule? molecule)
    {
        AtomRankingResult result = RefineAtomRanks(molecule);
        BreakTiesByAtomId(result);
        result.WasFullySeparated = CountRankClasses(result.Ranks) == result.Ranks.Count;
        return result;
    }

    internal AtomRankingResult RefineAtomRanks(Molecule? molecule)
    {
        AtomRankingResult result = new AtomRankingResult();

        if (molecule == null)
        {
            return result;
        }

        AtomInvariantSet invariants = CalculateInvariants(molecule);
        int invariantIndex = 0;
        while (invariantIndex < invariants.Invariants.Count)
        {
            AtomInvariant invariant = invariants.Invariants[invariantIndex];
            AtomRank rank = new AtomRank();
            rank.AtomId = invariant.AtomId;
            rank.InvariantValue = invariant.Value;
            rank.RefinementValue = invariant.Value;
            result.Ranks.Add(rank);
            invariantIndex = invariantIndex + 1;
        }

        CompressRanksByRefinementValue(result.Ranks);

        bool changed = true;
        int iteration = 0;
        while (changed && iteration < molecule.Atoms.Count + 1)
        {
            int[] previousRanks = CopyRanksByAtomId(result.Ranks, molecule.Atoms.Count);
            int atomIndex = 0;
            while (atomIndex < result.Ranks.Count)
            {
                AtomRank rank = result.Ranks[atomIndex];
                rank.RefinementValue = BuildRefinementValue(molecule, result.Ranks, rank.AtomId);
                atomIndex = atomIndex + 1;
            }

            CompressRanksByRefinementValue(result.Ranks);
            changed = RanksChanged(previousRanks, result.Ranks);
            iteration = iteration + 1;
        }

        result.IterationCount = iteration;
        result.WasFullySeparated = CountRankClasses(result.Ranks) == result.Ranks.Count;
        SortRanksByAtomId(result.Ranks);
        return result;
    }

    internal long BuildRefinementValue(Molecule molecule, List<AtomRank> ranks, int atomId)
    {
        long value = GetRankForAtom(ranks, atomId);
        List<int> neighborDescriptors = new List<int>();
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            int neighborAtomId = GetNeighborAtomId(bond, atomId);
            if (neighborAtomId >= 0)
            {
                int descriptor = GetRankForAtom(ranks, neighborAtomId) * 10 + GetBondOrderValue(bond);
                neighborDescriptors.Add(descriptor);
            }

            bondIndex = bondIndex + 1;
        }

        SortIntsDescending(neighborDescriptors);
        int index = 0;
        while (index < neighborDescriptors.Count)
        {
            value = AddHashValue(value, neighborDescriptors[index]);
            index = index + 1;
        }

        return value;
    }

    internal void BreakTiesByAtomId(AtomRankingResult result)
    {
        SortRanksByRankThenAtomId(result.Ranks);
        int index = 0;
        while (index < result.Ranks.Count)
        {
            result.Ranks[index].TieBreakValue = result.Ranks[index].AtomId;
            result.Ranks[index].Rank = index + 1;
            index = index + 1;
        }

        SortRanksByAtomId(result.Ranks);
    }

    internal void CompressRanksByRefinementValue(List<AtomRank> ranks)
    {
        SortRanksByRefinementValue(ranks);
        int index = 0;
        int rank = 1;
        while (index < ranks.Count)
        {
            if (index > 0 && ranks[index].RefinementValue != ranks[index - 1].RefinementValue)
            {
                rank = rank + 1;
            }

            ranks[index].Rank = rank;
            index = index + 1;
        }

        SortRanksByAtomId(ranks);
    }

    internal int[] CopyRanksByAtomId(List<AtomRank> ranks, int atomCount)
    {
        int[] values = new int[atomCount];
        int index = 0;
        while (index < atomCount)
        {
            values[index] = 0;
            index = index + 1;
        }

        index = 0;
        while (index < ranks.Count)
        {
            if (ranks[index].AtomId >= 0 && ranks[index].AtomId < atomCount)
            {
                values[ranks[index].AtomId] = ranks[index].Rank;
            }

            index = index + 1;
        }

        return values;
    }

    internal bool RanksChanged(int[] previousRanks, List<AtomRank> ranks)
    {
        int index = 0;
        while (index < ranks.Count)
        {
            int atomId = ranks[index].AtomId;
            if (atomId >= 0 && atomId < previousRanks.Length)
            {
                if (previousRanks[atomId] != ranks[index].Rank)
                {
                    return true;
                }
            }

            index = index + 1;
        }

        return false;
    }

    internal int CountRankClasses(List<AtomRank> ranks)
    {
        List<int> classes = new List<int>();
        int index = 0;
        while (index < ranks.Count)
        {
            if (!ContainsInt(classes, ranks[index].Rank))
            {
                classes.Add(ranks[index].Rank);
            }

            index = index + 1;
        }

        return classes.Count;
    }

    internal int GetRankForAtom(List<AtomRank> ranks, int atomId)
    {
        int index = 0;
        while (index < ranks.Count)
        {
            if (ranks[index].AtomId == atomId)
            {
                return ranks[index].Rank;
            }

            index = index + 1;
        }

        return 0;
    }

    internal AtomInvariant CalculateAtomInvariant(Molecule? molecule, RingSet ringSet, int atomId)
    {
        AtomInvariant invariant = new AtomInvariant();

        if (molecule == null || !molecule.IsValidAtomId(atomId))
        {
            return invariant;
        }

        Atom atom = molecule.Atoms[atomId];
        invariant.AtomId = atomId;
        invariant.AtomicNumber = atom.AtomicNumber;
        invariant.IsotopeMassNumber = atom.IsotopeMassNumber;
        invariant.FormalCharge = atom.FormalCharge;
        invariant.Degree = molecule.GetDegree(atomId);
        invariant.HydrogenCount = atom.ExplicitHydrogenCount + atom.ImplicitHydrogenCount;
        invariant.BondOrderSum = GetBondOrderSum(molecule, atomId);
        invariant.AromaticBondCount = GetAromaticBondCount(molecule, atomId);
        invariant.RingCount = ringSet.GetAtomRingCount(atomId);
        invariant.NeighborScore = GetNeighborScore(molecule, atomId);
        invariant.Value = BuildInvariantValue(atom, invariant);
        return invariant;
    }

    internal long BuildInvariantValue(Atom atom, AtomInvariant invariant)
    {
        long value = 17;
        value = AddHashValue(value, invariant.AtomicNumber);
        value = AddHashValue(value, invariant.IsotopeMassNumber);
        value = AddHashValue(value, invariant.FormalCharge + 128);
        if (atom.AtomMapNumber != 0)
        {
            value = AddHashValue(value, atom.AtomMapNumber);
        }

        value = AddHashValue(value, atom.RadicalElectronCount);
        value = AddHashValue(value, invariant.Degree);
        value = AddHashValue(value, invariant.HydrogenCount);
        value = AddHashValue(value, invariant.BondOrderSum);
        value = AddHashValue(value, invariant.AromaticBondCount);
        value = AddHashValue(value, invariant.RingCount);
        value = AddHashValue(value, atom.IsAromatic ? 1 : 0);
        value = AddHashValue(value, invariant.NeighborScore);
        return value;
    }

    internal long AddHashValue(long current, int value)
    {
        const decimal modulus = 18446744073709551616m;
        decimal expanded = (decimal)current * 131 + value;
        decimal wrapped = expanded % modulus;
        if (wrapped > long.MaxValue)
        {
            wrapped = wrapped - modulus;
        }
        else if (wrapped < long.MinValue)
        {
            wrapped = wrapped + modulus;
        }

        return (long)wrapped;
    }

    internal int GetBondOrderSum(Molecule molecule, int atomId)
    {
        int sum = 0;
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (bond.BeginAtomId == atomId || bond.EndAtomId == atomId)
            {
                sum = sum + GetBondOrderValue(bond);
            }

            bondIndex = bondIndex + 1;
        }

        return sum;
    }

    internal int GetAromaticBondCount(Molecule molecule, int atomId)
    {
        int count = 0;
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (bond.BeginAtomId == atomId || bond.EndAtomId == atomId)
            {
                if (bond.IsAromatic || bond.Order == BondOrder.Aromatic)
                {
                    count = count + 1;
                }
            }

            bondIndex = bondIndex + 1;
        }

        return count;
    }

    internal int GetNeighborScore(Molecule molecule, int atomId)
    {
        List<int> descriptors = new List<int>();
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            int neighborAtomId = GetNeighborAtomId(bond, atomId);
            if (neighborAtomId >= 0 && molecule.IsValidAtomId(neighborAtomId))
            {
                Atom neighbor = molecule.Atoms[neighborAtomId];
                int descriptor = neighbor.AtomicNumber * 100 + GetBondOrderValue(bond) * 10 + (neighbor.IsAromatic ? 1 : 0);
                descriptors.Add(descriptor);
            }

            bondIndex = bondIndex + 1;
        }

        SortIntsDescending(descriptors);
        return PackDescriptors(descriptors);
    }

    internal int PackDescriptors(List<int> descriptors)
    {
        int value = 0;
        int index = 0;
        while (index < descriptors.Count && index < 2)
        {
            value = value * 1000 + descriptors[index];
            index = index + 1;
        }

        while (index < 2)
        {
            value = value * 1000;
            index = index + 1;
        }

        return value;
    }

    internal int GetBondOrderValue(Bond? bond)
    {
        if (bond == null)
        {
            return 0;
        }

        if (bond.IsAromatic || bond.Order == BondOrder.Aromatic)
        {
            return 4;
        }

        if (bond.Order == BondOrder.Single)
        {
            return 1;
        }

        if (bond.Order == BondOrder.Double)
        {
            return 2;
        }

        if (bond.Order == BondOrder.Triple)
        {
            return 3;
        }

        if (bond.Order == BondOrder.Quadruple)
        {
            return 4;
        }

        if (bond.Order == BondOrder.Dative || bond.IsDative)
        {
            return 1;
        }

        return 0;
    }

    internal int GetNeighborAtomId(Bond bond, int atomId)
    {
        if (bond.BeginAtomId == atomId)
        {
            return bond.EndAtomId;
        }

        if (bond.EndAtomId == atomId)
        {
            return bond.BeginAtomId;
        }

        return -1;
    }

    internal void SortIntsDescending(List<int> values)
    {
        int index = 1;
        while (index < values.Count)
        {
            int value = values[index];
            int scan = index - 1;
            while (scan >= 0 && values[scan] < value)
            {
                values[scan + 1] = values[scan];
                scan = scan - 1;
            }

            values[scan + 1] = value;
            index = index + 1;
        }
    }

    internal bool ContainsInt(List<int> values, int value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void SortRanksByRefinementValue(List<AtomRank> ranks)
    {
        int index = 1;
        while (index < ranks.Count)
        {
            AtomRank value = ranks[index];
            int scan = index - 1;
            while (scan >= 0 && CompareRanksByRefinementValue(ranks[scan], value) > 0)
            {
                ranks[scan + 1] = ranks[scan];
                scan = scan - 1;
            }

            ranks[scan + 1] = value;
            index = index + 1;
        }
    }

    internal int CompareRanksByRefinementValue(AtomRank left, AtomRank right)
    {
        if (left.RefinementValue < right.RefinementValue)
        {
            return -1;
        }

        if (left.RefinementValue > right.RefinementValue)
        {
            return 1;
        }

        return left.AtomId - right.AtomId;
    }

    internal void SortRanksByAtomId(List<AtomRank> ranks)
    {
        int index = 1;
        while (index < ranks.Count)
        {
            AtomRank value = ranks[index];
            int scan = index - 1;
            while (scan >= 0 && ranks[scan].AtomId > value.AtomId)
            {
                ranks[scan + 1] = ranks[scan];
                scan = scan - 1;
            }

            ranks[scan + 1] = value;
            index = index + 1;
        }
    }

    internal void SortRanksByRankThenAtomId(List<AtomRank> ranks)
    {
        int index = 1;
        while (index < ranks.Count)
        {
            AtomRank value = ranks[index];
            int scan = index - 1;
            while (scan >= 0 && CompareRanksByRankThenAtomId(ranks[scan], value) > 0)
            {
                ranks[scan + 1] = ranks[scan];
                scan = scan - 1;
            }

            ranks[scan + 1] = value;
            index = index + 1;
        }
    }

    internal int CompareRanksByRankThenAtomId(AtomRank left, AtomRank right)
    {
        if (left.Rank != right.Rank)
        {
            return left.Rank - right.Rank;
        }

        return left.AtomId - right.AtomId;
    }
}
