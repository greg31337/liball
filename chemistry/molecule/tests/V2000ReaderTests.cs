using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class V2000ReaderTests
{
    [Test]
    public void ReadWithResult_ReadsHeaderAtomsBondsCoordinatesAndProperties()
    {
        V2000Reader reader = new V2000Reader();

        MolfileReadResult result = reader.ReadWithResult(BuildAnnotatedMolfile());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(0));
        Assert.That(result.Molecule.Metadata.GetPrimaryName(), Is.EqualTo("Annotated amide"));
        Assert.That(result.Molecule.Atoms.Count, Is.EqualTo(4));
        Assert.That(result.Molecule.Bonds.Count, Is.EqualTo(3));
        Assert.That(result.Molecule.Atoms[0].AtomicNumber, Is.EqualTo(6));
        Assert.That(result.Molecule.Atoms[0].IsotopeMassNumber, Is.EqualTo(13));
        Assert.That(result.Molecule.Atoms[0].HasCoordinates, Is.True);
        Assert.That(result.Molecule.Atoms[0].X, Is.EqualTo(0.0).Within(0.0001));
        Assert.That(result.Molecule.Atoms[3].FormalCharge, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[3].RadicalElectronCount, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[3].Alias, Is.EqualTo("amide nitrogen"));
        Assert.That(result.Molecule.Bonds[1].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(result.Molecule.Bonds[2].Stereo, Is.EqualTo(BondStereo.Wedge));
        Assert.That(result.Molecule.Bonds[2].Direction, Is.EqualTo(BondDirection.Up));
    }

    [Test]
    public void Read_ReadsAtomBlockChargeAndMassDifference()
    {
        V2000Reader reader = new V2000Reader();

        Molecule molecule = reader.Read(BuildAtomBlockPropertiesMolfile());

        Assert.That(molecule.Atoms.Count, Is.EqualTo(2));
        Assert.That(molecule.Atoms[0].FormalCharge, Is.EqualTo(1));
        Assert.That(molecule.Atoms[1].IsotopeMassNumber, Is.EqualTo(13));
    }

    [Test]
    public void MolfileReader_DelegatesToV2000Reader()
    {
        MolfileReader reader = new MolfileReader();

        MolfileReadResult result = reader.ReadWithResult(BuildAnnotatedMolfile());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Molecule.Bonds[1].Order, Is.EqualTo(BondOrder.Double));
    }

    [Test]
    public void ReadWithResult_ReturnsDiagnosticForMissingCountsLine()
    {
        V2000Reader reader = new V2000Reader();

        MolfileReadResult result = reader.ReadWithResult("only name\nprogram\ncomment");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("MOLFILE_COUNTS_MISSING"));
    }

    [Test]
    public void ReadWithResult_ReturnsDiagnosticForInvalidBondAtom()
    {
        V2000Reader reader = new V2000Reader();

        MolfileReadResult result = reader.ReadWithResult(BuildInvalidBondMolfile());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("MOLFILE_INVALID_BOND_ATOM"));
    }

    [Test]
    public void ReadWithResult_WarnsForUnknownElementSymbol()
    {
        V2000Reader reader = new V2000Reader();

        MolfileReadResult result = reader.ReadWithResult(BuildUnknownElementMolfile());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Report.GetWarningCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("MOLFILE_UNKNOWN_ELEMENT"));
        Assert.That(result.Molecule.Atoms[0].AtomicNumber, Is.EqualTo(0));
    }

    static string BuildAnnotatedMolfile()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "Annotated amide",
                "  molecule tests",
                "coordinates, records, alias",
                "  4  3  0  0  0  0            999 V2000",
                "    0.0000    0.0000    0.0000 C   0  0  0  0  0  0  0  0  0  0  0  0",
                "    1.5000    0.0000    0.0000 C   0  0  0  0  0  0  0  0  0  0  0  0",
                "    2.2500    1.2500    0.0000 O   0  0  0  0  0  0  0  0  0  0  0  0",
                "    2.2500   -1.2500    0.0000 N   0  0  0  0  0  0  0  0  0  0  0  0",
                "  1  2  1  0  0  0  0",
                "  2  3  2  0  0  0  0",
                "  2  4  1  1  0  0  0",
                "M  CHG  1   4   1",
                "M  ISO  1   1  13",
                "M  RAD  1   4   2",
                "A    4",
                "amide nitrogen",
                "M  END"
            });
    }

    static string BuildAtomBlockPropertiesMolfile()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "Atom block",
                "",
                "",
                "  2  0  0  0  0  0            999 V2000",
                "    0.0000    0.0000    0.0000 N   0  3  0  0  0  0  0  0  0  0  0  0",
                "    1.0000    0.0000    0.0000 C   1  0  0  0  0  0  0  0  0  0  0  0",
                "M  END"
            });
    }

    static string BuildInvalidBondMolfile()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "Invalid bond",
                "",
                "",
                "  1  1  0  0  0  0            999 V2000",
                "    0.0000    0.0000    0.0000 C   0  0  0  0  0  0  0  0  0  0  0  0",
                "  1  2  1  0  0  0  0",
                "M  END"
            });
    }

    static string BuildUnknownElementMolfile()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "Unknown",
                "",
                "",
                "  1  0  0  0  0  0            999 V2000",
                "    0.0000    0.0000    0.0000 Xx  0  0  0  0  0  0  0  0  0  0  0  0",
                "M  END"
            });
    }
}
