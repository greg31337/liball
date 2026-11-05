using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class StereoDepictionAssignmentTests
{
    [Test]
    public void AssignWedgeHashFromStereo_AssignsWedgeForClockwiseTetrahedralRecord()
    {
        Molecule molecule = BuildTetrahedralMolecule();
        StereoRecord record = new StereoRecord();
        record.SetTetrahedral(0, 1, 2, 3, 4, Chirality.Clockwise);
        molecule.StereoRecords.Add(record);
        Layout2D layout = new Layout2D();

        int count = layout.AssignWedgeHashFromStereo(molecule);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(molecule.Bonds[0].Stereo, Is.EqualTo(BondStereo.Wedge));
        Assert.That(molecule.Bonds[0].Direction, Is.EqualTo(BondDirection.Up));
    }

    [Test]
    public void AssignWedgeHashFromStereo_AssignsHashForCounterClockwiseTetrahedralRecord()
    {
        Molecule molecule = BuildTetrahedralMolecule();
        StereoRecord record = new StereoRecord();
        record.SetTetrahedral(0, 1, 2, 3, 4, Chirality.CounterClockwise);
        molecule.StereoRecords.Add(record);
        Layout2D layout = new Layout2D();

        int count = layout.AssignWedgeHashFromStereo(molecule);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(molecule.Bonds[0].Stereo, Is.EqualTo(BondStereo.Hash));
        Assert.That(molecule.Bonds[0].Direction, Is.EqualTo(BondDirection.Down));
    }

    [Test]
    public void AssignWedgeHashFromStereo_AssignsUpOrDownForUnspecifiedTetrahedralRecord()
    {
        Molecule molecule = BuildTetrahedralMolecule();
        StereoRecord record = new StereoRecord();
        record.SetTetrahedral(0, 1, 2, 3, 4, Chirality.Unspecified);
        molecule.StereoRecords.Add(record);
        Layout2D layout = new Layout2D();

        int count = layout.AssignWedgeHashFromStereo(molecule);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(molecule.Bonds[0].Stereo, Is.EqualTo(BondStereo.UpOrDown));
        Assert.That(molecule.Bonds[0].Direction, Is.EqualTo(BondDirection.Either));
    }

    [Test]
    public void AssignWedgeHashFromStereo_ChoosesUndecoratedLigandBondWhenAvailable()
    {
        Molecule molecule = BuildTetrahedralMolecule();
        molecule.Bonds[0].Stereo = BondStereo.Wedge;
        molecule.Bonds[0].Direction = BondDirection.Up;
        StereoRecord record = new StereoRecord();
        record.SetTetrahedral(0, 1, 2, 3, 4, Chirality.CounterClockwise);
        molecule.StereoRecords.Add(record);
        Layout2D layout = new Layout2D();

        int count = layout.AssignWedgeHashFromStereo(molecule);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(molecule.Bonds[0].Stereo, Is.EqualTo(BondStereo.Wedge));
        Assert.That(molecule.Bonds[1].Stereo, Is.EqualTo(BondStereo.Hash));
        Assert.That(molecule.Bonds[1].Direction, Is.EqualTo(BondDirection.Down));
    }

    [Test]
    public void AssignStereoDepiction_MarksDoubleBondStereoRecords()
    {
        Molecule molecule = BuildSubstitutedAlkene();
        StereoRecord record = new StereoRecord();
        record.SetDoubleBond(0, 2, 3, DoubleBondConfiguration.E);
        molecule.StereoRecords.Add(record);
        Layout2D layout = new Layout2D();

        int count = layout.AssignStereoDepiction(molecule);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(molecule.Bonds[0].Stereo, Is.EqualTo(BondStereo.E));
    }

    [TestCase(DoubleBondConfiguration.Z, BondStereo.Z)]
    [TestCase(DoubleBondConfiguration.Cis, BondStereo.Cis)]
    [TestCase(DoubleBondConfiguration.Trans, BondStereo.Trans)]
    [TestCase(DoubleBondConfiguration.Unspecified, BondStereo.Unknown)]
    [TestCase(DoubleBondConfiguration.Unknown, BondStereo.Unknown)]
    public void AssignStereoDepiction_MapsEverySupportedDoubleBondConfiguration(DoubleBondConfiguration configuration, BondStereo expected)
    {
        Molecule molecule = BuildSubstitutedAlkene();
        StereoRecord record = new StereoRecord();
        record.SetDoubleBond(0, 2, 3, configuration);
        molecule.StereoRecords.Add(record);

        int count = new Layout2D().AssignStereoDepiction(molecule);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(molecule.Bonds[0].Stereo, Is.EqualTo(expected));
    }

    [Test]
    public void AssignStereoDepiction_RejectsUnsupportedTetrahedralAndDoubleBondValues()
    {
        Molecule tetrahedral = BuildTetrahedralMolecule();
        StereoRecord tetrahedralRecord = new StereoRecord();
        tetrahedralRecord.SetTetrahedral(0, 1, 2, 3, 4, (Chirality)99);
        tetrahedral.StereoRecords.Add(tetrahedralRecord);
        Molecule alkene = BuildSubstitutedAlkene();
        StereoRecord doubleBondRecord = new StereoRecord();
        doubleBondRecord.SetDoubleBond(0, 2, 3, (DoubleBondConfiguration)99);
        alkene.StereoRecords.Add(doubleBondRecord);

        Assert.That(new Layout2D().AssignStereoDepiction(tetrahedral), Is.EqualTo(0));
        Assert.That(new Layout2D().AssignStereoDepiction(alkene), Is.EqualTo(0));
        Assert.That(tetrahedral.Bonds[0].Stereo, Is.EqualTo(BondStereo.None));
        Assert.That(alkene.Bonds[0].Stereo, Is.EqualTo(BondStereo.None));
    }

    [Test]
    public void AssignStereoDepiction_IgnoresInvalidRecords()
    {
        Molecule molecule = BuildTetrahedralMolecule();
        StereoRecord record = new StereoRecord();
        record.SetTetrahedral(0, -1, -2, -3, -4, Chirality.Clockwise);
        molecule.StereoRecords.Add(record);
        Layout2D layout = new Layout2D();

        int count = layout.AssignStereoDepiction(molecule);

        Assert.That(count, Is.EqualTo(0));
        Assert.That(layout.AssignStereoDepiction(null), Is.EqualTo(0));
    }

    static Molecule BuildTetrahedralMolecule()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int fluorine = builder.AddAtom(new AtomSpec(9, 0, 0, 0, 0, 0, 0, false, "", "F"));
        int chlorine = builder.AddAtom(new AtomSpec(17, 0, 0, 0, 0, 0, 0, false, "", "Cl"));
        int bromine = builder.AddAtom(new AtomSpec(35, 0, 0, 0, 0, 0, 0, false, "", "Br"));
        int iodine = builder.AddAtom(new AtomSpec(53, 0, 0, 0, 0, 0, 0, false, "", "I"));
        builder.AddBond(carbon, fluorine, new BondSpec());
        builder.AddBond(carbon, chlorine, new BondSpec());
        builder.AddBond(carbon, bromine, new BondSpec());
        builder.AddBond(carbon, iodine, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildSubstitutedAlkene()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int left = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int right = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int chlorine = builder.AddAtom(new AtomSpec(17, 0, 0, 0, 0, 0, 0, false, "", "Cl"));
        int bromine = builder.AddAtom(new AtomSpec(35, 0, 0, 0, 0, 0, 0, false, "", "Br"));
        builder.AddBond(left, right, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(left, chlorine, new BondSpec());
        builder.AddBond(right, bromine, new BondSpec());
        return builder.Build();
    }
}
