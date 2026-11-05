using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class AtomRankingTests
{
    [Test]
    public void RefineAtomRanks_PreservesSymmetryClassesWithoutTieBreaking()
    {
        Molecule molecule = BuildButane();
        AtomRanker ranker = new AtomRanker();

        AtomRankingResult result = ranker.RefineAtomRanks(molecule);

        Assert.That(result.Ranks.Count, Is.EqualTo(4));
        Assert.That(result.WasFullySeparated, Is.False);
        Assert.That(result.IterationCount, Is.GreaterThan(0));
        Assert.That(result.GetAtomRank(0).Rank, Is.EqualTo(result.GetAtomRank(3).Rank));
        Assert.That(result.GetAtomRank(1).Rank, Is.EqualTo(result.GetAtomRank(2).Rank));
        Assert.That(result.GetAtomRank(0).Rank, Is.Not.EqualTo(result.GetAtomRank(1).Rank));
    }

    [Test]
    public void RankAtoms_BreaksRemainingTiesByAtomId()
    {
        Molecule molecule = BuildEthane();
        AtomRanker ranker = new AtomRanker();

        AtomRankingResult result = ranker.RankAtoms(molecule);

        Assert.That(result.WasFullySeparated, Is.True);
        Assert.That(result.GetAtomRank(0).Rank, Is.EqualTo(1));
        Assert.That(result.GetAtomRank(1).Rank, Is.EqualTo(2));
        Assert.That(result.GetAtomRank(0).TieBreakValue, Is.EqualTo(0));
        Assert.That(result.GetAtomRank(1).TieBreakValue, Is.EqualTo(1));
    }

    [Test]
    public void RefineAtomRanks_FullySeparatesAsymmetricMolecule()
    {
        Molecule molecule = BuildEthanol();
        AtomRanker ranker = new AtomRanker();

        AtomRankingResult result = ranker.RefineAtomRanks(molecule);

        Assert.That(result.WasFullySeparated, Is.True);
        Assert.That(result.GetAtomRank(0).Rank, Is.Not.EqualTo(result.GetAtomRank(1).Rank));
        Assert.That(result.GetAtomRank(1).Rank, Is.Not.EqualTo(result.GetAtomRank(2).Rank));
        Assert.That(result.GetAtomRank(0).Rank, Is.Not.EqualTo(result.GetAtomRank(2).Rank));
    }

    [Test]
    public void RefineAtomRanks_HasStableRankClassMultisetUnderRenumbering()
    {
        Molecule forward = BuildEthanol();
        Molecule reversed = BuildEthanolReversed();
        AtomRanker ranker = new AtomRanker();

        AtomRankingResult forwardRanks = ranker.RefineAtomRanks(forward);
        AtomRankingResult reversedRanks = ranker.RefineAtomRanks(reversed);

        Assert.That(GetSortedRanks(forwardRanks), Is.EqualTo(GetSortedRanks(reversedRanks)));
    }

    [Test]
    public void BuildRefinementValue_UsesNeighborRanksAndBondOrders()
    {
        Molecule molecule = BuildCarbonylAndAlcohol();
        AtomRanker ranker = new AtomRanker();

        AtomRankingResult result = ranker.RefineAtomRanks(molecule);
        AtomRank carbonyl = result.GetAtomRank(0);
        AtomRank alcohol = result.GetAtomRank(2);

        Assert.That(carbonyl.RefinementValue, Is.Not.EqualTo(alcohol.RefinementValue));
        Assert.That(carbonyl.Rank, Is.Not.EqualTo(alcohol.Rank));
    }

    [Test]
    public void RankAtoms_ReturnsEmptyResultForNullMolecule()
    {
        AtomRanker ranker = new AtomRanker();

        AtomRankingResult result = ranker.RankAtoms(null);

        Assert.That(result.Ranks, Is.Empty);
        Assert.That(result.WasFullySeparated, Is.True);
    }

    [Test]
    public void AddHashValue_PreservesWraparoundAtLongBoundaries()
    {
        AtomRanker ranker = new AtomRanker();

        long positiveOverflow = ranker.AddHashValue(long.MaxValue, int.MaxValue);
        long negativeOverflow = ranker.AddHashValue(long.MinValue, int.MinValue);

        Assert.That(positiveOverflow, Is.EqualTo(-9223372034707292292));
        Assert.That(negativeOverflow, Is.EqualTo(9223372034707292160));
    }

    static Molecule BuildEthane()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int atom0 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int atom1 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(atom0, atom1, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildButane()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int atom0 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int atom1 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int atom2 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int atom3 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(atom0, atom1, new BondSpec());
        builder.AddBond(atom1, atom2, new BondSpec());
        builder.AddBond(atom2, atom3, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildEthanol()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(carbonA, carbonB, new BondSpec());
        builder.AddBond(carbonB, oxygen, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildEthanolReversed()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(oxygen, carbonB, new BondSpec());
        builder.AddBond(carbonB, carbonA, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildCarbonylAndAlcohol()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonylCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int carbonylOxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int alcoholCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, false, "", "C"));
        int alcoholOxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(carbonylCarbon, carbonylOxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(alcoholCarbon, alcoholOxygen, new BondSpec());
        return builder.Build();
    }

    static int[] GetSortedRanks(AtomRankingResult result)
    {
        int[] ranks = new int[result.Ranks.Count];
        int index = 0;
        while (index < result.Ranks.Count)
        {
            ranks[index] = result.Ranks[index].Rank;
            index = index + 1;
        }

        SortInts(ranks);
        return ranks;
    }

    static void SortInts(int[] values)
    {
        int index = 1;
        while (index < values.Length)
        {
            int value = values[index];
            int scan = index - 1;
            while (scan >= 0 && values[scan] > value)
            {
                values[scan + 1] = values[scan];
                scan = scan - 1;
            }

            values[scan + 1] = value;
            index = index + 1;
        }
    }
}
