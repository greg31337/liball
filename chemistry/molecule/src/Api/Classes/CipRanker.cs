using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides cip ranker data and operations for molecular graphs.
/// </summary>
public class CipRanker : ICipRanker
{
    /// <summary>
    /// Initializes a new deterministic CIP-like priority ranker.
    /// </summary>
    public CipRanker()
    {
    }

    /// <summary>
    /// Rank tetrahedral.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="record">The record.</param>
    /// <returns>The resulting CIP tetrahedral ranking.</returns>
    public CipTetrahedralRanking RankTetrahedral(Molecule? molecule, StereoRecord? record)
    {
        CipTetrahedralRanking result = new CipTetrahedralRanking();

        if (molecule == null || record == null)
        {
            result.Report.AddError("CIP_INVALID_RECORD", "Cannot rank a null tetrahedral stereo record.");
            return result;
        }

        result.StereoRecordId = record.Id;
        result.CenterAtomId = record.CenterAtomId;

        if (!record.IsTetrahedral() || !molecule.IsValidAtomId(record.CenterAtomId))
        {
            result.Report.AddError("CIP_INVALID_TETRAHEDRAL", "Stereo record is not a valid tetrahedral record.");
            return result;
        }

        int index = 0;
        while (index < record.LigandAtomIds.Count)
        {
            result.Ligands.Add(CreateLigandRank(molecule, record.LigandAtomIds[index], record.CenterAtomId, index));
            index = index + 1;
        }

        SortLigands(result.Ligands);
        AssignRanks(result.Ligands);
        result.IsValid = result.Ligands.Count == 4 && HasUniqueRanks(result.Ligands);
        result.Label = GetTetrahedralLabel(record.Chirality);
        return result;
    }

    /// <summary>
    /// Rank double bond.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="record">The record.</param>
    /// <returns>The resulting CIP double bond ranking.</returns>
    public CipDoubleBondRanking RankDoubleBond(Molecule? molecule, StereoRecord? record)
    {
        CipDoubleBondRanking result = new CipDoubleBondRanking();

        if (molecule == null || record == null)
        {
            result.Report.AddError("CIP_INVALID_RECORD", "Cannot rank a null double-bond stereo record.");
            return result;
        }

        result.StereoRecordId = record.Id;
        result.StereoBondId = record.StereoBondId;

        if (!record.IsDoubleBond() || record.StereoBondId < 0 || record.StereoBondId >= molecule.Bonds.Count)
        {
            result.Report.AddError("CIP_INVALID_DOUBLE_BOND", "Stereo record is not a valid double-bond stereo record.");
            return result;
        }

        Bond bond = molecule.Bonds[record.StereoBondId];
        result.BeginAtomId = bond.BeginAtomId;
        result.EndAtomId = bond.EndAtomId;

        FillDoubleBondSideRanks(molecule, bond.BeginAtomId, bond.EndAtomId, result.BeginLigands);
        FillDoubleBondSideRanks(molecule, bond.EndAtomId, bond.BeginAtomId, result.EndLigands);
        SortLigands(result.BeginLigands);
        SortLigands(result.EndLigands);
        AssignRanks(result.BeginLigands);
        AssignRanks(result.EndLigands);

        if (result.BeginLigands.Count > 0)
        {
            result.BeginTopLigandAtomId = result.BeginLigands[0].AtomId;
        }

        if (result.EndLigands.Count > 0)
        {
            result.EndTopLigandAtomId = result.EndLigands[0].AtomId;
        }

        result.IsValid = result.BeginLigands.Count > 0 && result.EndLigands.Count > 0;
        result.Label = GetDoubleBondLabel(record.Configuration);
        return result;
    }

    internal List<CipLigandRank> RankLigands(Molecule? molecule, int centerAtomId)
    {
        List<CipLigandRank> result = new List<CipLigandRank>();

        if (molecule == null || !molecule.IsValidAtomId(centerAtomId))
        {
            return result;
        }

        List<int> neighbors = molecule.GetNeighborAtomIds(centerAtomId);
        int index = 0;
        while (index < neighbors.Count)
        {
            result.Add(CreateLigandRank(molecule, neighbors[index], centerAtomId, index));
            index = index + 1;
        }

        SortLigands(result);
        AssignRanks(result);
        return result;
    }

    internal void FillDoubleBondSideRanks(Molecule molecule, int terminalAtomId, int otherTerminalAtomId, List<CipLigandRank> ranks)
    {
        int originalIndex = 0;
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            int neighborAtomId = GetNeighborAtomId(bond, terminalAtomId);
            if (neighborAtomId >= 0 && neighborAtomId != otherTerminalAtomId)
            {
                ranks.Add(CreateLigandRank(molecule, neighborAtomId, terminalAtomId, originalIndex));
                originalIndex = originalIndex + 1;
            }

            bondIndex = bondIndex + 1;
        }
    }

    internal CipLigandRank CreateLigandRank(Molecule? molecule, int ligandAtomId, int previousAtomId, int originalIndex)
    {
        CipLigandRank rank = new CipLigandRank();
        rank.AtomId = ligandAtomId;
        rank.OriginalIndex = originalIndex;

        if (ligandAtomId < 0)
        {
            rank.AtomicNumber = 1;
            rank.IsImplicit = true;
            return rank;
        }

        if (molecule == null || !molecule.IsValidAtomId(ligandAtomId))
        {
            return rank;
        }

        Atom atom = molecule.Atoms[ligandAtomId];
        rank.AtomicNumber = atom.AtomicNumber;
        rank.IsotopeMassNumber = atom.IsotopeMassNumber;
        rank.FirstSphereScore = CalculateSphereScore(molecule, ligandAtomId, previousAtomId, 1);
        rank.SecondSphereScore = CalculateSphereScore(molecule, ligandAtomId, previousAtomId, 2);
        return rank;
    }

    internal int CalculateSphereScore(Molecule molecule, int atomId, int previousAtomId, int depth)
    {
        List<int> values = new List<int>();
        AddSphereAtomicNumbers(molecule, atomId, previousAtomId, depth, values);
        SortIntsDescending(values);
        return PackAtomicNumbers(values);
    }

    internal void AddSphereAtomicNumbers(Molecule molecule, int atomId, int previousAtomId, int depth, List<int> values)
    {
        if (depth <= 0)
        {
            return;
        }

        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            int neighborAtomId = GetNeighborAtomId(bond, atomId);
            if (neighborAtomId >= 0 && neighborAtomId != previousAtomId && molecule.IsValidAtomId(neighborAtomId))
            {
                int repeatCount = GetBondDuplicationCount(bond);
                int repeatIndex = 0;
                while (repeatIndex < repeatCount)
                {
                    values.Add(molecule.Atoms[neighborAtomId].AtomicNumber);
                    repeatIndex = repeatIndex + 1;
                }

                AddSphereAtomicNumbers(molecule, neighborAtomId, atomId, depth - 1, values);
            }

            bondIndex = bondIndex + 1;
        }
    }

    internal int GetBondDuplicationCount(Bond bond)
    {
        if (bond.Order == BondOrder.Triple)
        {
            return 3;
        }

        if (bond.Order == BondOrder.Double)
        {
            return 2;
        }

        return 1;
    }

    internal int PackAtomicNumbers(List<int> values)
    {
        int score = 0;
        int index = 0;
        while (index < values.Count && index < 4)
        {
            score = score * 60 + values[index];
            index = index + 1;
        }

        while (index < 4)
        {
            score = score * 60;
            index = index + 1;
        }

        return score;
    }

    internal void SortLigands(List<CipLigandRank> ligands)
    {
        int index = 1;
        while (index < ligands.Count)
        {
            CipLigandRank value = ligands[index];
            int scan = index - 1;
            while (scan >= 0 && CompareLigands(ligands[scan], value) < 0)
            {
                ligands[scan + 1] = ligands[scan];
                scan = scan - 1;
            }

            ligands[scan + 1] = value;
            index = index + 1;
        }
    }

    internal void AssignRanks(List<CipLigandRank> ligands)
    {
        int index = 0;
        int rank = 1;
        while (index < ligands.Count)
        {
            if (index > 0 && ComparePriorityOnly(ligands[index - 1], ligands[index]) != 0)
            {
                rank = index + 1;
            }

            ligands[index].Rank = rank;
            index = index + 1;
        }
    }

    internal bool HasUniqueRanks(List<CipLigandRank> ligands)
    {
        int left = 0;
        while (left < ligands.Count)
        {
            int right = left + 1;
            while (right < ligands.Count)
            {
                if (ligands[left].Rank == ligands[right].Rank)
                {
                    return false;
                }

                right = right + 1;
            }

            left = left + 1;
        }

        return true;
    }

    internal int CompareLigands(CipLigandRank left, CipLigandRank right)
    {
        int compare = ComparePriorityOnly(left, right);
        if (compare != 0)
        {
            return compare;
        }

        if (left.OriginalIndex < right.OriginalIndex)
        {
            return 1;
        }

        if (left.OriginalIndex > right.OriginalIndex)
        {
            return -1;
        }

        return 0;
    }

    internal int ComparePriorityOnly(CipLigandRank left, CipLigandRank right)
    {
        if (left.AtomicNumber != right.AtomicNumber)
        {
            return left.AtomicNumber - right.AtomicNumber;
        }

        if (left.IsotopeMassNumber != right.IsotopeMassNumber)
        {
            return left.IsotopeMassNumber - right.IsotopeMassNumber;
        }

        if (left.FirstSphereScore != right.FirstSphereScore)
        {
            if (left.FirstSphereScore > right.FirstSphereScore)
            {
                return 1;
            }

            return -1;
        }

        if (left.SecondSphereScore != right.SecondSphereScore)
        {
            if (left.SecondSphereScore > right.SecondSphereScore)
            {
                return 1;
            }

            return -1;
        }

        return 0;
    }

    internal string GetTetrahedralLabel(Chirality chirality)
    {
        if (chirality == Chirality.Clockwise)
        {
            return "R";
        }

        if (chirality == Chirality.CounterClockwise)
        {
            return "S";
        }

        if (chirality == Chirality.Unspecified)
        {
            return "?";
        }

        if (chirality == Chirality.Unknown)
        {
            return "?";
        }

        return "";
    }

    internal string GetDoubleBondLabel(DoubleBondConfiguration configuration)
    {
        if (configuration == DoubleBondConfiguration.E || configuration == DoubleBondConfiguration.Trans)
        {
            return "E";
        }

        if (configuration == DoubleBondConfiguration.Z || configuration == DoubleBondConfiguration.Cis)
        {
            return "Z";
        }

        if (configuration == DoubleBondConfiguration.Unspecified || configuration == DoubleBondConfiguration.Unknown)
        {
            return "?";
        }

        return "";
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
}

