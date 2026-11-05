using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class V3000WriterTests
{
    [Test]
    public void Write_EmitsV3000CtabAtomsBondsPropertiesAndCollections()
    {
        V3000Writer writer = new V3000Writer();

        string molfile = writer.Write(BuildAnnotatedMolecule());

        Assert.That(molfile, Does.StartWith("V3000 annotated\n  molecule tests\nwriter round trip\n"));
        Assert.That(molfile, Does.Contain("V3000"));
        Assert.That(molfile, Does.Contain("M  V30 BEGIN CTAB"));
        Assert.That(molfile, Does.Contain("M  V30 COUNTS 4 3 0 0 1"));
        Assert.That(molfile, Does.Contain("M  V30 1 C 0.0000 0.0000 0.0000 7 MASS=13"));
        Assert.That(molfile, Does.Contain("M  V30 4 N 2.2500 -1.2500 0.0000 0 CHG=1 RAD=2 CFG=1"));
        Assert.That(molfile, Does.Contain("M  V30 2 2 2 3"));
        Assert.That(molfile, Does.Contain("M  V30 3 1 2 4 CFG=1"));
        Assert.That(molfile, Does.Contain("M  V30 BEGIN COLLECTION"));
        Assert.That(molfile, Does.Contain("M  V30 MDLV30/STEREL1 ATOMS=(1 4)"));
        Assert.That(molfile, Does.EndWith("M  END\n"));
    }

    [Test]
    public void Write_RoundTripsThroughV3000Reader()
    {
        V3000Writer writer = new V3000Writer();
        V3000Reader reader = new V3000Reader();

        MolfileReadResult result = reader.ReadWithResult(writer.Write(BuildAnnotatedMolecule()));

        Assert.That(result.Success, Is.True);
        Assert.That(result.Molecule.Atoms.Count, Is.EqualTo(4));
        Assert.That(result.Molecule.Bonds.Count, Is.EqualTo(3));
        Assert.That(result.Molecule.Atoms[0].IsotopeMassNumber, Is.EqualTo(13));
        Assert.That(result.Molecule.Atoms[0].AtomMapNumber, Is.EqualTo(7));
        Assert.That(result.Molecule.Atoms[3].FormalCharge, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[3].RadicalElectronCount, Is.EqualTo(1));
        Assert.That(result.Molecule.Bonds[2].Stereo, Is.EqualTo(BondStereo.Wedge));
        Assert.That(result.Molecule.StereoRecords.Count, Is.EqualTo(1));
        Assert.That(result.Molecule.StereoGroups.Groups[0].Type, Is.EqualTo("or"));
        Assert.That(result.Molecule.StereoGroups.Groups[0].StereoRecordIds, Is.EqualTo(new int[] { 0 }));
    }

    [Test]
    public void MolfileWriter_WriteV3000DelegatesToV3000Writer()
    {
        MolfileWriter writer = new MolfileWriter();

        string molfile = writer.WriteV3000(BuildAnnotatedMolecule());

        Assert.That(molfile, Does.Contain("M  V30 BEGIN CTAB"));
        Assert.That(molfile, Does.Contain("MDLV30/STEREL1"));
    }

    [Test]
    public void Write_ReturnsEmptyTextForNullMolecule()
    {
        V3000Writer writer = new V3000Writer();

        Assert.That(writer.Write(null), Is.EqualTo(""));
    }

    [Test]
    public void Write_EmitsCountsBeyondV2000Limit()
    {
        V3000Writer writer = new V3000Writer();
        V3000Reader reader = new V3000Reader();
        Molecule molecule = BuildLargeMolecule(1001);

        string molfile = writer.Write(molecule);
        MolfileReadResult result = reader.ReadWithResult(molfile);

        Assert.That(molfile, Does.Contain("M  V30 COUNTS 1001 0 0 0 0"));
        Assert.That(result.Success, Is.True);
        Assert.That(result.Molecule.Atoms.Count, Is.EqualTo(1001));
    }

    static Molecule BuildAnnotatedMolecule()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int methyl = builder.AddAtom(new AtomSpec(6, 13, 0, 0, 3, 0, 7, false, "", "C"));
        int carbonyl = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 1, 0, 2, 1, 0, false, "", "N"));
        builder.AddBond(methyl, carbonyl, new BondSpec());
        builder.AddBond(carbonyl, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbonyl, nitrogen, BondSpec.Create(BondOrder.Single, false, BondStereo.Wedge, BondDirection.Up));
        Molecule molecule = builder.Build();
        molecule.Metadata.AddName("V3000 annotated");
        molecule.Metadata.AddComment("  molecule tests");
        molecule.Metadata.AddComment("writer round trip");
        SetCoordinates(molecule.Atoms[0], 0.0, 0.0, 0.0);
        SetCoordinates(molecule.Atoms[1], 1.5, 0.0, 0.0);
        SetCoordinates(molecule.Atoms[2], 2.25, 1.25, 0.0);
        SetCoordinates(molecule.Atoms[3], 2.25, -1.25, 0.0);

        StereoRecord record = new StereoRecord();
        record.Id = 0;
        record.SetTetrahedral(nitrogen, carbonyl, -1, -1, -1, Chirality.Clockwise);
        record.Source = "V3000";
        molecule.StereoRecords.Add(record);
        EnhancedStereoGroup group = molecule.StereoGroups.AddGroup("or", "V3000", "MDLV30/STEREL1");
        molecule.StereoGroups.AssignRecordToGroup(molecule, record.Id, group.Id);
        return molecule;
    }

    static Molecule BuildLargeMolecule(int atomCount)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int index = 0;
        while (index < atomCount)
        {
            builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
            index = index + 1;
        }

        return builder.Build();
    }

    static void SetCoordinates(Atom atom, double x, double y, double z)
    {
        atom.HasCoordinates = true;
        atom.X = x;
        atom.Y = y;
        atom.Z = z;
    }
}
