using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class CipRankerTests
{
    [Test]
    public void RankTetrahedral_OrdersLigandsByAtomicNumberAndAssignsLabel()
    {
        Molecule molecule = BuildTetrahedralHalides();
        StereoRecord record = new StereoRecord();
        record.Id = 3;
        record.SetTetrahedral(0, 1, 2, 3, 4, Chirality.Clockwise);
        CipRanker ranker = new CipRanker();

        CipTetrahedralRanking ranking = ranker.RankTetrahedral(molecule, record);

        Assert.That(ranking.IsValid, Is.True);
        Assert.That(ranking.StereoRecordId, Is.EqualTo(3));
        Assert.That(ranking.CenterAtomId, Is.EqualTo(0));
        Assert.That(ranking.Label, Is.EqualTo("R"));
        Assert.That(ranking.Ligands[0].AtomId, Is.EqualTo(4));
        Assert.That(ranking.Ligands[0].AtomicNumber, Is.EqualTo(53));
        Assert.That(ranking.Ligands[0].Rank, Is.EqualTo(1));
        Assert.That(ranking.Ligands[1].AtomId, Is.EqualTo(3));
        Assert.That(ranking.Ligands[2].AtomId, Is.EqualTo(2));
        Assert.That(ranking.Ligands[3].AtomId, Is.EqualTo(1));
    }

    [Test]
    public void RankTetrahedral_UsesIsotopeMassAsTieBreaker()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int center = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int carbon12 = builder.AddAtom(new AtomSpec(6, 12, 0, 0, 0, 0, 0, false, "", "C"));
        int carbon13 = builder.AddAtom(new AtomSpec(6, 13, 0, 0, 0, 0, 0, false, "", "C"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 0, 0, 0, false, "", "N"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(center, carbon12, new BondSpec());
        builder.AddBond(center, carbon13, new BondSpec());
        builder.AddBond(center, nitrogen, new BondSpec());
        builder.AddBond(center, oxygen, new BondSpec());
        StereoRecord record = new StereoRecord();
        record.SetTetrahedral(center, carbon12, carbon13, nitrogen, oxygen, Chirality.CounterClockwise);
        CipRanker ranker = new CipRanker();

        CipTetrahedralRanking ranking = ranker.RankTetrahedral(builder.Build(), record);

        Assert.That(ranking.Label, Is.EqualTo("S"));
        Assert.That(ranking.Ligands[0].AtomId, Is.EqualTo(oxygen));
        Assert.That(ranking.Ligands[1].AtomId, Is.EqualTo(nitrogen));
        Assert.That(ranking.Ligands[2].AtomId, Is.EqualTo(carbon13));
        Assert.That(ranking.Ligands[3].AtomId, Is.EqualTo(carbon12));
    }

    [Test]
    public void RankLigands_UsesNextSphereAtomsToBreakDirectAtomTies()
    {
        Molecule molecule = BuildRecursiveRankingMolecule();
        CipRanker ranker = new CipRanker();

        List<CipLigandRank> ranks = ranker.RankLigands(molecule, 0);

        Assert.That(ranks[0].AtomId, Is.EqualTo(3));
        Assert.That(ranks[1].AtomId, Is.EqualTo(4));
        Assert.That(ranks[2].AtomId, Is.EqualTo(2));
        Assert.That(ranks[3].AtomId, Is.EqualTo(1));
        Assert.That(ranks[2].AtomicNumber, Is.EqualTo(6));
        Assert.That(ranks[3].AtomicNumber, Is.EqualTo(6));
        Assert.That(ranks[2].FirstSphereScore, Is.GreaterThan(ranks[3].FirstSphereScore));
    }

    [Test]
    public void RankLigands_DuplicatesMultipleBondAtomsInSphereComparison()
    {
        Molecule molecule = BuildCarbonylVsAlcoholRankingMolecule();
        CipRanker ranker = new CipRanker();

        List<CipLigandRank> ranks = ranker.RankLigands(molecule, 0);

        Assert.That(ranks[0].AtomId, Is.EqualTo(1));
        Assert.That(ranks[1].AtomId, Is.EqualTo(2));
        Assert.That(ranks[0].FirstSphereScore, Is.GreaterThan(ranks[1].FirstSphereScore));
    }

    [Test]
    public void RankDoubleBond_FindsTopLigandsOnEachSideAndMapsConfigurationLabel()
    {
        Molecule molecule = BuildSubstitutedAlkene();
        StereoRecord record = new StereoRecord();
        record.Id = 8;
        record.SetDoubleBond(0, 2, 4, DoubleBondConfiguration.Trans);
        CipRanker ranker = new CipRanker();

        CipDoubleBondRanking ranking = ranker.RankDoubleBond(molecule, record);

        Assert.That(ranking.IsValid, Is.True);
        Assert.That(ranking.StereoRecordId, Is.EqualTo(8));
        Assert.That(ranking.StereoBondId, Is.EqualTo(0));
        Assert.That(ranking.BeginTopLigandAtomId, Is.EqualTo(3));
        Assert.That(ranking.EndTopLigandAtomId, Is.EqualTo(4));
        Assert.That(ranking.BeginLigands[0].AtomicNumber, Is.EqualTo(17));
        Assert.That(ranking.EndLigands[0].AtomicNumber, Is.EqualTo(35));
        Assert.That(ranking.Label, Is.EqualTo("E"));
    }

    [Test]
    public void RankDoubleBond_MapsCisAndUnknownConfigurationLabels()
    {
        CipRanker ranker = new CipRanker();

        Assert.That(ranker.GetDoubleBondLabel(DoubleBondConfiguration.Cis), Is.EqualTo("Z"));
        Assert.That(ranker.GetDoubleBondLabel(DoubleBondConfiguration.Z), Is.EqualTo("Z"));
        Assert.That(ranker.GetDoubleBondLabel(DoubleBondConfiguration.Unknown), Is.EqualTo("?"));
    }

    [Test]
    public void Ranking_ReturnsDiagnosticsForInvalidRecords()
    {
        CipRanker ranker = new CipRanker();
        Molecule molecule = new Molecule();
        StereoRecord record = new StereoRecord();

        CipTetrahedralRanking tetrahedral = ranker.RankTetrahedral(molecule, record);
        CipDoubleBondRanking doubleBond = ranker.RankDoubleBond(molecule, record);

        Assert.That(tetrahedral.IsValid, Is.False);
        Assert.That(tetrahedral.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(tetrahedral.Report.Get(0).Code, Is.EqualTo("CIP_INVALID_TETRAHEDRAL"));
        Assert.That(doubleBond.IsValid, Is.False);
        Assert.That(doubleBond.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(doubleBond.Report.Get(0).Code, Is.EqualTo("CIP_INVALID_DOUBLE_BOND"));
    }

    [Test]
    public void Ranking_ReturnsDiagnosticsForNullMoleculesAndRecords()
    {
        ICipRanker ranker = new CipRanker();
        StereoRecord record = new StereoRecord();

        CipTetrahedralRanking nullMolecule = ranker.RankTetrahedral(null, record);
        CipTetrahedralRanking nullTetrahedral = ranker.RankTetrahedral(new Molecule(), null);
        CipDoubleBondRanking nullDoubleBond = ranker.RankDoubleBond(new Molecule(), null);

        Assert.That(nullMolecule.Report.Errors[0].Code, Is.EqualTo("CIP_INVALID_RECORD"));
        Assert.That(nullTetrahedral.Report.Errors[0].Code, Is.EqualTo("CIP_INVALID_RECORD"));
        Assert.That(nullDoubleBond.Report.Errors[0].Code, Is.EqualTo("CIP_INVALID_RECORD"));
        Assert.That(nullMolecule.IsValid, Is.False);
        Assert.That(nullDoubleBond.IsValid, Is.False);
    }

    static Molecule BuildTetrahedralHalides()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int center = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int fluorine = builder.AddAtom(new AtomSpec(9, 0, 0, 0, 0, 0, 0, false, "", "F"));
        int chlorine = builder.AddAtom(new AtomSpec(17, 0, 0, 0, 0, 0, 0, false, "", "Cl"));
        int bromine = builder.AddAtom(new AtomSpec(35, 0, 0, 0, 0, 0, 0, false, "", "Br"));
        int iodine = builder.AddAtom(new AtomSpec(53, 0, 0, 0, 0, 0, 0, false, "", "I"));
        builder.AddBond(center, fluorine, new BondSpec());
        builder.AddBond(center, chlorine, new BondSpec());
        builder.AddBond(center, bromine, new BondSpec());
        builder.AddBond(center, iodine, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildRecursiveRankingMolecule()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int center = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int methyl = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int ethyl = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", "N"));
        int ethylCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(center, methyl, new BondSpec());
        builder.AddBond(center, ethyl, new BondSpec());
        builder.AddBond(center, oxygen, new BondSpec());
        builder.AddBond(center, nitrogen, new BondSpec());
        builder.AddBond(ethyl, ethylCarbon, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildCarbonylVsAlcoholRankingMolecule()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int center = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int carbonylCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int alcoholCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygenA = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int oxygenB = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(center, carbonylCarbon, new BondSpec());
        builder.AddBond(center, alcoholCarbon, new BondSpec());
        builder.AddBond(carbonylCarbon, oxygenA, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(alcoholCarbon, oxygenB, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildSubstitutedAlkene()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int left = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int right = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int fluorine = builder.AddAtom(new AtomSpec(9, 0, 0, 0, 0, 0, 0, false, "", "F"));
        int chlorine = builder.AddAtom(new AtomSpec(17, 0, 0, 0, 0, 0, 0, false, "", "Cl"));
        int bromine = builder.AddAtom(new AtomSpec(35, 0, 0, 0, 0, 0, 0, false, "", "Br"));
        builder.AddBond(left, right, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(left, fluorine, new BondSpec());
        builder.AddBond(left, chlorine, new BondSpec());
        builder.AddBond(right, bromine, new BondSpec());
        return builder.Build();
    }
}
