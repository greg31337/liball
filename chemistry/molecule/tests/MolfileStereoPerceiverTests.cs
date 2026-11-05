using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class MolfileStereoPerceiverTests
{
    [Test]
    public void PerceiveFromMolfileWedges_CreatesClockwiseTetrahedralRecordFromWedgeBond()
    {
        Molecule molecule = BuildImplicitChiralCarbon(BondStereo.Wedge);
        StereoPerceiver perceiver = new StereoPerceiver();

        StereoPerceptionResult result = perceiver.PerceiveFromMolfileWedges(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Report.GetCount(), Is.EqualTo(0));
        Assert.That(result.Records.Count, Is.EqualTo(1));
        Assert.That(molecule.StereoRecords.Count, Is.EqualTo(1));
        Assert.That(molecule.StereoRecords[0].IsTetrahedral(), Is.True);
        Assert.That(molecule.StereoRecords[0].CenterAtomId, Is.EqualTo(0));
        Assert.That(molecule.StereoRecords[0].Chirality, Is.EqualTo(Chirality.Clockwise));
        Assert.That(molecule.StereoRecords[0].LigandAtomIds, Is.EqualTo(new int[] { 1, 2, 3, -1 }));
        Assert.That(molecule.StereoRecords[0].Source, Is.EqualTo("molfile"));
        Assert.That(molecule.Bonds[0].Direction, Is.EqualTo(BondDirection.Up));

        StereoValidator validator = new StereoValidator();
        Assert.That(validator.Validate(molecule).GetCount(), Is.EqualTo(0));
    }

    [Test]
    public void PerceiveFromMolfileWedges_CreatesCounterClockwiseRecordFromHashBond()
    {
        Molecule molecule = BuildImplicitChiralCarbon(BondStereo.Hash);
        StereoPerceiver perceiver = new StereoPerceiver();

        StereoPerceptionResult result = perceiver.PerceiveFromMolfileWedges(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(molecule.StereoRecords[0].Chirality, Is.EqualTo(Chirality.CounterClockwise));
        Assert.That(molecule.Bonds[0].Direction, Is.EqualTo(BondDirection.Down));
    }

    [Test]
    public void PerceiveFromMolfileWedges_CreatesUnspecifiedRecordFromUpOrDownBond()
    {
        Molecule molecule = BuildImplicitChiralCarbon(BondStereo.UpOrDown);
        StereoPerceiver perceiver = new StereoPerceiver();

        StereoPerceptionResult result = perceiver.PerceiveFromMolfileWedges(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(molecule.StereoRecords[0].Chirality, Is.EqualTo(Chirality.Unspecified));
        Assert.That(molecule.Bonds[0].Direction, Is.EqualTo(BondDirection.Either));
    }

    [Test]
    public void PerceiveFromMolfileWedges_DoesNotCreateDuplicateRecordsForSameCenter()
    {
        Molecule molecule = BuildImplicitChiralCarbon(BondStereo.Wedge);
        molecule.Bonds[1].Stereo = BondStereo.Hash;
        StereoPerceiver perceiver = new StereoPerceiver();

        StereoPerceptionResult result = perceiver.PerceiveFromMolfileWedges(molecule);

        Assert.That(result.Records.Count, Is.EqualTo(1));
        Assert.That(molecule.StereoRecords.Count, Is.EqualTo(1));
        Assert.That(molecule.Bonds[0].Direction, Is.EqualTo(BondDirection.Up));
        Assert.That(molecule.Bonds[1].Direction, Is.EqualTo(BondDirection.Down));
    }

    [Test]
    public void PerceiveFromMolfileWedges_IgnoresBondsWithoutMolStereo()
    {
        Molecule molecule = BuildImplicitChiralCarbon(BondStereo.None);
        StereoPerceiver perceiver = new StereoPerceiver();

        StereoPerceptionResult result = perceiver.PerceiveFromMolfileWedges(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Records, Is.Empty);
        Assert.That(molecule.StereoRecords, Is.Empty);
        Assert.That(molecule.Bonds[0].Direction, Is.EqualTo(BondDirection.None));
    }

    [Test]
    public void PerceiveFromMolfileWedges_ReportsInvalidBondAndNullMolecule()
    {
        Molecule molecule = BuildImplicitChiralCarbon(BondStereo.Wedge);
        molecule.Bonds[0].BeginAtomId = 99;
        StereoPerceiver perceiver = new StereoPerceiver();

        StereoPerceptionResult invalid = perceiver.PerceiveFromMolfileWedges(molecule);
        StereoPerceptionResult nullResult = perceiver.PerceiveFromMolfileWedges(null);

        Assert.That(invalid.Success, Is.True);
        Assert.That(invalid.Report.GetWarningCount(), Is.EqualTo(1));
        Assert.That(invalid.Report.Get(0).Code, Is.EqualTo("MOL_STEREO_INVALID_BOND"));
        Assert.That(nullResult.Success, Is.False);
        Assert.That(nullResult.Report.Get(0).Code, Is.EqualTo("STEREO_NULL_MOLECULE"));
    }

    static Molecule BuildImplicitChiralCarbon(BondStereo stereo)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, false, "", "C"));
        int fluorine = builder.AddAtom(new AtomSpec(9, 0, 0, 0, 0, 0, 0, false, "", "F"));
        int chlorine = builder.AddAtom(new AtomSpec(17, 0, 0, 0, 0, 0, 0, false, "", "Cl"));
        int bromine = builder.AddAtom(new AtomSpec(35, 0, 0, 0, 0, 0, 0, false, "", "Br"));
        builder.AddBond(carbon, fluorine, BondSpec.Create(BondOrder.Single, false, stereo, BondDirection.None));
        builder.AddBond(carbon, chlorine, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbon, bromine, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }
}
