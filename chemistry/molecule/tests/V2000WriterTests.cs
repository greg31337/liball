using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class V2000WriterTests
{
    [Test]
    public void Write_EmitsHeaderCountsAtomsBondsPropertiesAliasesAndEnd()
    {
        V2000Writer writer = new V2000Writer();

        string molfile = writer.Write(BuildAnnotatedMolecule());

        Assert.That(molfile, Does.StartWith("Annotated amide\n  molecule tests\nwriter round trip\n"));
        Assert.That(molfile, Does.Contain("  4  3  0  0  0  0            999 V2000"));
        Assert.That(molfile, Does.Contain("    2.2500   -1.2500    0.0000 N"));
        Assert.That(molfile, Does.Contain("  2  3  2  0"));
        Assert.That(molfile, Does.Contain("  2  4  1  1"));
        Assert.That(molfile, Does.Contain("M  CHG  1   4   1"));
        Assert.That(molfile, Does.Contain("M  ISO  1   1  13"));
        Assert.That(molfile, Does.Contain("M  RAD  1   4   2"));
        Assert.That(molfile, Does.Contain("A    4\namide nitrogen\n"));
        Assert.That(molfile, Does.EndWith("M  END\n"));
    }

    [Test]
    public void Write_RoundTripsThroughV2000Reader()
    {
        V2000Writer writer = new V2000Writer();
        V2000Reader reader = new V2000Reader();

        MolfileReadResult result = reader.ReadWithResult(writer.Write(BuildAnnotatedMolecule()));

        Assert.That(result.Success, Is.True);
        Assert.That(result.Molecule.Atoms.Count, Is.EqualTo(4));
        Assert.That(result.Molecule.Bonds.Count, Is.EqualTo(3));
        Assert.That(result.Molecule.Atoms[0].IsotopeMassNumber, Is.EqualTo(13));
        Assert.That(result.Molecule.Atoms[3].FormalCharge, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[3].RadicalElectronCount, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[3].Alias, Is.EqualTo("amide nitrogen"));
        Assert.That(result.Molecule.Bonds[2].Stereo, Is.EqualTo(BondStereo.Wedge));
        Assert.That(result.Molecule.Atoms[2].Y, Is.EqualTo(1.25).Within(0.0001));
    }

    [Test]
    public void MolfileWriter_DelegatesToV2000Writer()
    {
        MolfileWriter writer = new MolfileWriter();

        string molfile = writer.Write(BuildAnnotatedMolecule());

        Assert.That(molfile, Does.Contain("V2000"));
        Assert.That(molfile, Does.Contain("M  CHG"));
    }

    [Test]
    public void Write_ReturnsEmptyTextForNullMolecule()
    {
        V2000Writer writer = new V2000Writer();

        Assert.That(writer.Write(null), Is.EqualTo(""));
    }

    [Test]
    public void Write_UsesFallbackCoordinatesAndSymbols()
    {
        V2000Writer writer = new V2000Writer();
        Molecule molecule = new Molecule();
        Atom atom = new Atom(0, new AtomSpec(0, 0, 0, 0, 0, 0, 0, false, "", "R#"));
        molecule.Atoms.Add(atom);

        string molfile = writer.Write(molecule);

        Assert.That(molfile, Does.Contain("    0.0000    0.0000    0.0000 R#"));
    }

    static Molecule BuildAnnotatedMolecule()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int methyl = builder.AddAtom(new AtomSpec(6, 13, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonyl = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 1, 0, 2, 1, 0, false, "amide nitrogen", "N"));
        builder.AddBond(methyl, carbonyl, new BondSpec());
        builder.AddBond(carbonyl, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbonyl, nitrogen, BondSpec.Create(BondOrder.Single, false, BondStereo.Wedge, BondDirection.Up));
        Molecule molecule = builder.Build();
        molecule.Metadata.AddName("Annotated amide");
        molecule.Metadata.AddComment("  molecule tests");
        molecule.Metadata.AddComment("writer round trip");
        SetCoordinates(molecule.Atoms[0], 0.0, 0.0, 0.0);
        SetCoordinates(molecule.Atoms[1], 1.5, 0.0, 0.0);
        SetCoordinates(molecule.Atoms[2], 2.25, 1.25, 0.0);
        SetCoordinates(molecule.Atoms[3], 2.25, -1.25, 0.0);
        return molecule;
    }

    static void SetCoordinates(Atom atom, double x, double y, double z)
    {
        atom.HasCoordinates = true;
        atom.X = x;
        atom.Y = y;
        atom.Z = z;
    }
}
