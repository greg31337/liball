using System.Globalization;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class PdbReaderTests
{
    [Test]
    public void ReadWithResult_ReadsAtomsConectModelResiduesChainsAndPdbFields()
    {
        PdbReader reader = new PdbReader();

        PdbReadResult result = reader.ReadWithResult(BuildAnnotatedPdb());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms.Count, Is.EqualTo(3));
        Assert.That(result.Molecule.Bonds.Count, Is.EqualTo(2));
        Assert.That(result.Molecule.Atoms[0].AtomicNumber, Is.EqualTo(7));
        Assert.That(result.Molecule.Atoms[0].FormalCharge, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[2].AtomicNumber, Is.EqualTo(8));
        Assert.That(result.Molecule.Atoms[2].FormalCharge, Is.EqualTo(-1));
        Assert.That(result.Molecule.Atoms[1].X, Is.EqualTo(12.56).Within(0.0001));
        Assert.That(result.Molecule.Metadata.GetPdbField("PDB_ATOM_0_RECORD"), Is.EqualTo("ATOM"));
        Assert.That(result.Molecule.Metadata.GetPdbField("PDB_ATOM_0_ALT_LOC"), Is.EqualTo("A"));
        Assert.That(result.Molecule.Metadata.GetPdbField("PDB_ATOM_0_OCCUPANCY"), Is.EqualTo("0.50"));
        Assert.That(result.Molecule.Metadata.GetPdbField("PDB_ATOM_1_B_FACTOR"), Is.EqualTo("21.50"));
        Assert.That(result.Molecule.Metadata.GetPdbField("PDB_ATOM_2_RECORD"), Is.EqualTo("HETATM"));
        Assert.That(result.Molecule.Metadata.GetPdbField("PDB_ATOM_2_MODEL"), Is.EqualTo("1"));
        Assert.That(result.Residues.Count, Is.EqualTo(2));
        Assert.That(result.Residues[0].Name, Is.EqualTo("ALA"));
        Assert.That(result.Residues[0].ChainId, Is.EqualTo("A"));
        Assert.That(result.Residues[0].AtomIds, Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(result.Residues[1].Name, Is.EqualTo("LIG"));
        Assert.That(result.Chains.Count, Is.EqualTo(2));
        Assert.That(result.Chains[0].Id, Is.EqualTo("A"));
        Assert.That(result.Chains[0].ResidueIds, Is.EqualTo(new int[] { 1 }));
        Assert.That(result.Chains[1].Id, Is.EqualTo("B"));
        Assert.That(result.Chains[1].ResidueIds, Is.EqualTo(new int[] { 2 }));
    }

    [Test]
    public void Read_ReturnsMoleculeWithoutResultWrapper()
    {
        PdbReader reader = new PdbReader();

        Molecule molecule = reader.Read(BuildAnnotatedPdb());

        Assert.That(molecule.Atoms.Count, Is.EqualTo(3));
        Assert.That(molecule.Bonds.Count, Is.EqualTo(2));
    }

    [Test]
    public void ReadWithResult_WarnsForUnknownConectSerial()
    {
        PdbReader reader = new PdbReader();
        string pdb = AtomLine("ATOM", 1, "C", "", "UNK", "A", 1, "", 0.0, 0.0, 0.0, "1.00", "10.00", "C", "")
            + "\nCONECT    1    9\nEND\n";

        PdbReadResult result = reader.ReadWithResult(pdb);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Report.GetWarningCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("PDB_CONECT_UNKNOWN_SERIAL"));
    }

    [Test]
    public void ReadWithResult_ReturnsDiagnosticForInvalidCoordinate()
    {
        PdbReader reader = new PdbReader();
        string line = AtomLine("ATOM", 1, "C", "", "UNK", "A", 1, "", 0.0, 0.0, 0.0, "1.00", "10.00", "C", "");
        line = line.Substring(0, 30) + " BADVAL " + line.Substring(38);

        PdbReadResult result = reader.ReadWithResult(line + "\n");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("PDB_INVALID_FLOAT"));
    }

    [Test]
    public void ReadWithResult_WarnsForUnknownElementSymbol()
    {
        string pdb = AtomLine("HETATM", 1, "Q1", "", "LIG", "A", 1, "", 0.0, 0.0, 0.0, "1.00", "10.00", "Xx", "") + "\nEND\n";

        PdbReadResult result = new PdbReader().ReadWithResult(pdb);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Molecule.Atoms[0].AtomicNumber, Is.EqualTo(0));
        Assert.That(result.Report.GetWarningCount(), Is.EqualTo(1));
        Assert.That(result.Report.Errors[0].Code, Is.EqualTo("PDB_UNKNOWN_ELEMENT"));
    }

    static string BuildAnnotatedPdb()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "HEADER    TEST PDB",
                "TITLE     SAMPLE STRUCTURE",
                "MODEL        1",
                AtomLine("ATOM", 1, "N", "A", "ALA", "A", 1, "", 11.104, 13.207, 2.100, "0.50", "20.00", "N", "1+"),
                AtomLine("ATOM", 2, "CA", "", "ALA", "A", 1, "", 12.560, 13.400, 2.300, "1.00", "21.50", "C", ""),
                AtomLine("HETATM", 3, "O1", "", "LIG", "B", 2, "", 13.000, 14.000, 3.000, "0.75", "30.00", "O", "1-"),
                "CONECT    1    2",
                "CONECT    2    3",
                "ENDMDL",
                "END",
                ""
            });
    }

    static string AtomLine(string record, int serial, string atomName, string alternateLocation, string residueName, string chainId, int residueNumber, string insertionCode, double x, double y, double z, string occupancy, string bFactor, string element, string charge)
    {
        string line = record.PadRight(6)
            + serial.ToString().PadLeft(5)
            + " "
            + atomName.PadLeft(4)
            + alternateLocation.PadRight(1)
            + residueName.PadRight(3)
            + " "
            + chainId.PadRight(1)
            + residueNumber.ToString().PadLeft(4)
            + insertionCode.PadRight(1)
            + "   "
            + x.ToString("0.000", CultureInfo.InvariantCulture).PadLeft(8)
            + y.ToString("0.000", CultureInfo.InvariantCulture).PadLeft(8)
            + z.ToString("0.000", CultureInfo.InvariantCulture).PadLeft(8)
            + occupancy.PadLeft(6)
            + bFactor.PadLeft(6)
            + "          "
            + element.PadLeft(2)
            + charge.PadLeft(2);
        return line;
    }
}
