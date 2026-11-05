using System.Globalization;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class PdbWriterTests
{
    [Test]
    public void Write_EmitsHeaderModelAtomHetatmConectAndEndRecords()
    {
        PdbWriter writer = new PdbWriter();

        string pdb = writer.Write(BuildAnnotatedMolecule());

        Assert.That(pdb, Does.StartWith("HEADER    TEST PDB\nTITLE     SAMPLE STRUCTURE\nMODEL        1\n"));
        Assert.That(pdb, Does.Contain("ATOM      1    NAALA A   1"));
        Assert.That(pdb, Does.Contain("ATOM      2   CA ALA A   1"));
        Assert.That(pdb, Does.Contain("HETATM    3   O1 LIG B   2"));
        Assert.That(pdb, Does.Contain("  0.50 20.00           N1+"));
        Assert.That(pdb, Does.Contain("CONECT    1    2\n"));
        Assert.That(pdb, Does.Contain("CONECT    2    3\n"));
        Assert.That(pdb, Does.EndWith("ENDMDL\nEND\n"));
    }

    [Test]
    public void Write_RoundTripsThroughPdbReader()
    {
        PdbWriter writer = new PdbWriter();
        PdbReader reader = new PdbReader();

        PdbReadResult result = reader.ReadWithResult(writer.Write(BuildAnnotatedMolecule()));

        Assert.That(result.Success, Is.True);
        Assert.That(result.Molecule.Atoms.Count, Is.EqualTo(3));
        Assert.That(result.Molecule.Bonds.Count, Is.EqualTo(2));
        Assert.That(result.Molecule.Atoms[0].FormalCharge, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[2].FormalCharge, Is.EqualTo(-1));
        Assert.That(result.Molecule.Metadata.GetPdbField("PDB_ATOM_0_ALT_LOC"), Is.EqualTo("A"));
        Assert.That(result.Molecule.Metadata.GetPdbField("PDB_ATOM_1_B_FACTOR"), Is.EqualTo("21.50"));
        Assert.That(result.Molecule.Metadata.GetPdbField("PDB_ATOM_2_RECORD"), Is.EqualTo("HETATM"));
        Assert.That(result.Molecule.Metadata.GetPdbField("PDB_ATOM_2_MODEL"), Is.EqualTo("1"));
        Assert.That(result.Residues.Count, Is.EqualTo(2));
        Assert.That(result.Chains.Count, Is.EqualTo(2));
    }

    [Test]
    public void Write_InfersDefaultsForPlainMolecule()
    {
        PdbWriter writer = new PdbWriter();
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, -1, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(carbon, oxygen, new BondSpec());

        string pdb = writer.Write(builder.Build());

        Assert.That(pdb, Does.Contain("ATOM      1    C UNK A   1"));
        Assert.That(pdb, Does.Contain("ATOM      2    O UNK A   1"));
        Assert.That(pdb, Does.Contain("CONECT    1    2"));
        Assert.That(pdb, Does.Contain("           O1-"));
    }

    [Test]
    public void Write_ReturnsEmptyTextForNullMolecule()
    {
        PdbWriter writer = new PdbWriter();

        Assert.That(writer.Write(null), Is.EqualTo(""));
    }

    [Test]
    public void WritePdbWithResult_IsUsableThroughFormatContractAndReportsErrors()
    {
        IPdbWriter writer = new PdbWriter();
        PdbReadResult source = new PdbReader().ReadWithResult(BuildTwoModelPdb());

        PdbWriteResult result = writer.WritePdbWithResult(source.Molecule);
        string direct = writer.WritePdb(source.Molecule);
        PdbWriteResult nullResult = writer.WritePdbWithResult(null);
        Molecule invalid = new Molecule();
        invalid.Atoms.Add(new Atom(0, new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C")));
        invalid.Bonds.Add(new Bond(0, 0, 3, new BondSpec()));
        PdbWriteResult invalidResult = writer.WritePdbWithResult(invalid);

        Assert.That(result.Success, Is.True);
        Assert.That(result.AtomRecordCount, Is.EqualTo(4));
        Assert.That(result.ConectRecordCount, Is.EqualTo(2));
        Assert.That(result.ModelCount, Is.EqualTo(2));
        Assert.That(result.ResidueCount, Is.EqualTo(2));
        Assert.That(result.ChainCount, Is.EqualTo(2));
        Assert.That(direct, Is.EqualTo(result.Text));
        Assert.That(nullResult.Success, Is.False);
        Assert.That(nullResult.Report.Errors[0].Code, Is.EqualTo("PDB_NULL_MOLECULE"));
        Assert.That(invalidResult.Success, Is.False);
        Assert.That(invalidResult.Text, Is.Empty);
        Assert.That(invalidResult.Report.Errors[0].Code, Is.EqualTo("PDB_INVALID_BOND_ATOM"));
    }

    [Test]
    public void Write_RoundTripsSeparateModelsWithRepeatedSerialsAndConnectivity()
    {
        PdbReader reader = new PdbReader();
        PdbReadResult first = reader.ReadWithResult(BuildTwoModelPdb());

        PdbWriteResult written = new PdbWriter().WritePdbWithResult(first.Molecule);
        PdbReadResult second = reader.ReadWithResult(written.Text);

        Assert.That(first.Success, Is.True);
        Assert.That(first.Models, Has.Count.EqualTo(2));
        Assert.That(first.AtomRecordCount, Is.EqualTo(4));
        Assert.That(first.ConectRecordCount, Is.EqualTo(2));
        Assert.That(written.Success, Is.True);
        Assert.That(written.Text, Does.Contain("MODEL        1\n"));
        Assert.That(written.Text, Does.Contain("ENDMDL\nMODEL        2\n"));
        Assert.That(second.Success, Is.True);
        Assert.That(second.Molecule.Atoms, Has.Count.EqualTo(4));
        Assert.That(second.Molecule.Bonds, Has.Count.EqualTo(2));
        Assert.That(second.Models, Has.Count.EqualTo(2));
        Assert.That(second.Models[0].AtomIds, Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(second.Models[1].AtomIds, Is.EqualTo(new int[] { 2, 3 }));
        Assert.That(second.Molecule.Bonds[0].BeginAtomId, Is.EqualTo(0));
        Assert.That(second.Molecule.Bonds[0].EndAtomId, Is.EqualTo(1));
        Assert.That(second.Molecule.Bonds[1].BeginAtomId, Is.EqualTo(2));
        Assert.That(second.Molecule.Bonds[1].EndAtomId, Is.EqualTo(3));
        Assert.That(second.Molecule.Atoms[2].AlternateLocation, Is.EqualTo("B"));
        Assert.That(second.Molecule.Atoms[2].InsertionCode, Is.EqualTo("C"));
        Assert.That(second.Molecule.Atoms[2].Occupancy, Is.EqualTo(0.65).Within(0.001));
        Assert.That(second.Molecule.Atoms[2].TemperatureFactor, Is.EqualTo(18.25).Within(0.001));
        Assert.That(second.Molecule.Atoms[3].FormalCharge, Is.EqualTo(-1));
    }

    [Test]
    public void WritePdbWithResult_OmitsCrossModelConnectivityWithWarning()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int first = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int second = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(first, second, new BondSpec());
        Molecule molecule = builder.Build();
        molecule.Atoms[0].ModelId = 1;
        molecule.Atoms[1].ModelId = 2;

        PdbWriteResult result = new PdbWriter().WritePdbWithResult(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ModelCount, Is.EqualTo(2));
        Assert.That(result.ConectRecordCount, Is.EqualTo(0));
        Assert.That(result.Text, Does.Not.Contain("CONECT"));
        Assert.That(result.Report.GetWarningCount(), Is.EqualTo(1));
        Assert.That(result.Report.Errors[0].Code, Is.EqualTo("PDB_CROSS_MODEL_BOND_OMITTED"));
    }

    [Test]
    public void WritePdbWithResult_WritesUnassignedAtomsAndConnectivityBeforeModels()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int modelAtom = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int firstUnassigned = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        int secondUnassigned = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", "N"));
        builder.AddBond(firstUnassigned, secondUnassigned, new BondSpec());
        Molecule molecule = builder.Build();
        molecule.Atoms[modelAtom].ModelId = 4;

        PdbWriteResult result = new PdbWriter().WritePdbWithResult(molecule);

        int unassignedAtomPosition = result.Text.IndexOf("ATOM      2");
        int unassignedConectPosition = result.Text.IndexOf("CONECT    2    3");
        int modelPosition = result.Text.IndexOf("MODEL        4");
        Assert.That(result.Success, Is.True);
        Assert.That(result.ModelCount, Is.EqualTo(1));
        Assert.That(result.ConectRecordCount, Is.EqualTo(1));
        Assert.That(unassignedAtomPosition, Is.GreaterThanOrEqualTo(0));
        Assert.That(unassignedConectPosition, Is.GreaterThan(unassignedAtomPosition));
        Assert.That(modelPosition, Is.GreaterThan(unassignedConectPosition));
    }

    static Molecule BuildAnnotatedMolecule()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 1, 0, 0, 0, 0, false, "", "N"));
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "CA"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, -1, 0, 0, 0, 0, false, "", "O1"));
        builder.AddBond(nitrogen, carbon, new BondSpec());
        builder.AddBond(carbon, oxygen, new BondSpec());
        Molecule molecule = builder.Build();
        molecule.Metadata.AddPdbField("HEADER", "TEST PDB");
        molecule.Metadata.AddComment("SAMPLE STRUCTURE");
        SetAtomPdb(molecule, 0, "1", "ATOM", "A", "ALA", "A", "1", "", "0.50", "20.00", "N", "1+", "1");
        SetAtomPdb(molecule, 1, "2", "ATOM", "", "ALA", "A", "1", "", "1.00", "21.50", "C", "", "1");
        SetAtomPdb(molecule, 2, "3", "HETATM", "", "LIG", "B", "2", "", "0.75", "30.00", "O", "1-", "1");
        SetCoordinates(molecule.Atoms[0], 11.104, 13.207, 2.100);
        SetCoordinates(molecule.Atoms[1], 12.560, 13.400, 2.300);
        SetCoordinates(molecule.Atoms[2], 13.000, 14.000, 3.000);
        return molecule;
    }

    static string BuildTwoModelPdb()
    {
        return "MODEL        1\n"
            + AtomLine(1, "N", "", "ALA", "A", 1, "", 0.0, 0.0, 0.0, "1.00", "10.00", "N", "1+") + "\n"
            + AtomLine(2, "CA", "", "ALA", "A", 1, "", 1.2, 0.0, 0.0, "1.00", "11.00", "C", "") + "\n"
            + "CONECT    1    2\nENDMDL\n"
            + "MODEL        2\n"
            + AtomLine(1, "N", "B", "ALA", "A", 1, "C", 0.1, 0.0, 0.0, "0.65", "18.25", "N", "") + "\n"
            + AtomLine(2, "O", "", "ALA", "A", 1, "C", 1.3, 0.0, 0.0, "0.75", "19.50", "O", "1-") + "\n"
            + "CONECT    1    2\nENDMDL\nEND\n";
    }

    static string AtomLine(int serial, string atomName, string alternateLocation, string residueName, string chainId, int residueNumber, string insertionCode, double x, double y, double z, string occupancy, string temperatureFactor, string element, string charge)
    {
        return "ATOM  "
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
            + temperatureFactor.PadLeft(6)
            + "          "
            + element.PadLeft(2)
            + charge.PadLeft(2);
    }

    static void SetAtomPdb(Molecule molecule, int atomId, string serial, string record, string altLoc, string residueName, string chainId, string residueNumber, string insertionCode, string occupancy, string bFactor, string element, string charge, string model)
    {
        molecule.Metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_SERIAL", serial);
        molecule.Metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_RECORD", record);
        molecule.Metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_ALT_LOC", altLoc);
        molecule.Metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_RES_NAME", residueName);
        molecule.Metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_CHAIN_ID", chainId);
        molecule.Metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_RES_SEQ", residueNumber);
        molecule.Metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_INSERTION_CODE", insertionCode);
        molecule.Metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_OCCUPANCY", occupancy);
        molecule.Metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_B_FACTOR", bFactor);
        molecule.Metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_ELEMENT", element);
        molecule.Metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_CHARGE", charge);
        molecule.Metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_MODEL", model);
    }

    static void SetCoordinates(Atom atom, double x, double y, double z)
    {
        atom.HasCoordinates = true;
        atom.X = x;
        atom.Y = y;
        atom.Z = z;
    }
}
