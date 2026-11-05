using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class SmilesStereoPerceiverTests
{
    [Test]
    public void PerceiveFromSmiles_CreatesTetrahedralRecordFromBracketAtMarker()
    {
        Molecule molecule = BuildImplicitChiralCarbon();
        StereoPerceiver perceiver = new StereoPerceiver();

        StereoPerceptionResult result = perceiver.PerceiveFromSmiles(molecule, "[C@H](F)(Cl)Br");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Report.GetCount(), Is.EqualTo(0));
        Assert.That(result.Records.Count, Is.EqualTo(1));
        Assert.That(molecule.StereoRecords.Count, Is.EqualTo(1));
        Assert.That(molecule.StereoRecords[0].IsTetrahedral(), Is.True);
        Assert.That(molecule.StereoRecords[0].CenterAtomId, Is.EqualTo(0));
        Assert.That(molecule.StereoRecords[0].Chirality, Is.EqualTo(Chirality.Clockwise));
        Assert.That(molecule.StereoRecords[0].LigandAtomIds, Is.EqualTo(new int[] { 1, 2, 3, -1 }));
        Assert.That(molecule.StereoRecords[0].Source, Is.EqualTo("smiles"));

        StereoValidator validator = new StereoValidator();
        Assert.That(validator.Validate(molecule).GetCount(), Is.EqualTo(0));
    }

    [Test]
    public void PerceiveFromSmiles_CreatesCounterClockwiseRecordFromDoubleAtMarker()
    {
        Molecule molecule = BuildImplicitChiralCarbon();
        StereoPerceiver perceiver = new StereoPerceiver();

        StereoPerceptionResult result = perceiver.PerceiveFromSmiles(molecule, "[C@@H](F)(Cl)Br");

        Assert.That(result.Success, Is.True);
        Assert.That(molecule.StereoRecords[0].Chirality, Is.EqualTo(Chirality.CounterClockwise));
    }

    [Test]
    public void PerceiveFromSmiles_AssignsSlashBackslashDirectionsAndCisDoubleBondRecord()
    {
        Molecule molecule = BuildButene();
        StereoPerceiver perceiver = new StereoPerceiver();

        StereoPerceptionResult result = perceiver.PerceiveFromSmiles(molecule, "C/C=C\\C");

        Assert.That(result.Success, Is.True);
        Assert.That(molecule.Bonds[0].Direction, Is.EqualTo(BondDirection.Up));
        Assert.That(molecule.Bonds[2].Direction, Is.EqualTo(BondDirection.Down));
        Assert.That(molecule.StereoRecords.Count, Is.EqualTo(1));
        Assert.That(molecule.StereoRecords[0].IsDoubleBond(), Is.True);
        Assert.That(molecule.StereoRecords[0].StereoBondId, Is.EqualTo(1));
        Assert.That(molecule.StereoRecords[0].BeginLigandAtomId, Is.EqualTo(0));
        Assert.That(molecule.StereoRecords[0].EndLigandAtomId, Is.EqualTo(3));
        Assert.That(molecule.StereoRecords[0].Configuration, Is.EqualTo(DoubleBondConfiguration.Cis));
        Assert.That(molecule.StereoRecords[0].Source, Is.EqualTo("smiles"));

        StereoValidator validator = new StereoValidator();
        Assert.That(validator.Validate(molecule).GetCount(), Is.EqualTo(0));
    }

    [Test]
    public void PerceiveFromSmiles_AssignsTransWhenDirectionalMarkersMatch()
    {
        Molecule molecule = BuildButene();
        StereoPerceiver perceiver = new StereoPerceiver();

        perceiver.PerceiveFromSmiles(molecule, "C/C=C/C");

        Assert.That(molecule.Bonds[0].Direction, Is.EqualTo(BondDirection.Up));
        Assert.That(molecule.Bonds[2].Direction, Is.EqualTo(BondDirection.Up));
        Assert.That(molecule.StereoRecords[0].Configuration, Is.EqualTo(DoubleBondConfiguration.Trans));
    }

    [Test]
    public void PerceiveFromSmiles_DoesNotCreateRecordsWithoutStereoMarkers()
    {
        Molecule molecule = BuildButene();
        StereoPerceiver perceiver = new StereoPerceiver();

        StereoPerceptionResult result = perceiver.PerceiveFromSmiles(molecule, "CC=CC");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Records, Is.Empty);
        Assert.That(molecule.StereoRecords, Is.Empty);
        Assert.That(molecule.Bonds[0].Direction, Is.EqualTo(BondDirection.None));
    }

    [Test]
    public void PerceiveFromSmiles_ReturnsDiagnosticsForNullInputs()
    {
        StereoPerceiver perceiver = new StereoPerceiver();
        Molecule molecule = BuildButene();

        StereoPerceptionResult nullMolecule = perceiver.PerceiveFromSmiles(null, "C/C=C/C");
        StereoPerceptionResult nullSmiles = perceiver.PerceiveFromSmiles(molecule, null);

        Assert.That(nullMolecule.Success, Is.False);
        Assert.That(nullMolecule.Report.Get(0).Code, Is.EqualTo("STEREO_NULL_MOLECULE"));
        Assert.That(nullSmiles.Success, Is.False);
        Assert.That(nullSmiles.Report.Get(0).Code, Is.EqualTo("SMILES_STEREO_NULL"));
    }

    static Molecule BuildImplicitChiralCarbon()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, false, "", "C"));
        int fluorine = builder.AddAtom(new AtomSpec(9, 0, 0, 0, 0, 0, 0, false, "", "F"));
        int chlorine = builder.AddAtom(new AtomSpec(17, 0, 0, 0, 0, 0, 0, false, "", "Cl"));
        int bromine = builder.AddAtom(new AtomSpec(35, 0, 0, 0, 0, 0, 0, false, "", "Br"));
        builder.AddBond(carbon, fluorine, new BondSpec());
        builder.AddBond(carbon, chlorine, new BondSpec());
        builder.AddBond(carbon, bromine, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildButene()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int atom0 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int atom1 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, false, "", "C"));
        int atom2 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, false, "", "C"));
        int atom3 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(atom0, atom1, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(atom1, atom2, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(atom2, atom3, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }
}
