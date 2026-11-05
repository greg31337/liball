using System.Globalization;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class PdbLigandWorkflowTests
{
    [Test]
    public void ReadLigands_ExtractsHeteroResiduesAndExcludesWaterByDefault()
    {
        IPdbReader reader = new PdbReader();

        PdbLigandExtractionResult result = reader.ReadLigands(BuildProteinLigandPdb(), null);
        string writtenLigand = new PdbWriter().WritePdb(result.Ligands[0]);
        PdbReadResult roundTrip = new PdbReader().ReadWithResult(writtenLigand);

        Assert.That(result.Success, Is.True);
        Assert.That(result.CandidateCount, Is.EqualTo(3));
        Assert.That(result.ExtractedCount, Is.EqualTo(2));
        Assert.That(result.SkippedWaterCount, Is.EqualTo(1));
        Assert.That(result.Residues[0].Name, Is.EqualTo("LIG"));
        Assert.That(result.Residues[1].Name, Is.EqualTo("ZN"));
        Assert.That(result.Ligands[0].Atoms, Has.Count.EqualTo(2));
        Assert.That(result.Ligands[0].Bonds, Has.Count.EqualTo(1));
        Assert.That(result.Ligands[0].Atoms[0].ResidueName, Is.EqualTo("LIG"));
        Assert.That(result.Ligands[0].Atoms[0].QueryValue, Is.EqualTo("HETATM"));
        Assert.That(result.Ligands[1].Atoms, Has.Count.EqualTo(1));
        Assert.That(result.Ligands[1].Atoms[0].AtomicNumber, Is.EqualTo(30));
        Assert.That(writtenLigand, Does.Contain("HETATM    1   C1 LIG B  10"));
        Assert.That(writtenLigand, Does.Contain("HETATM    2   O1 LIG B  10"));
        Assert.That(roundTrip.Success, Is.True);
        Assert.That(roundTrip.Molecule.Atoms[0].QueryValue, Is.EqualTo("HETATM"));
        Assert.That(roundTrip.Molecule.Atoms[1].AtomicNumber, Is.EqualTo(8));
    }

    [Test]
    public void ExtractLigands_AppliesWaterSingleAtomAndModelPoliciesWithoutMutation()
    {
        PdbReader reader = new PdbReader();
        Molecule molecule = reader.Read(BuildProteinLigandPdb());
        PdbLigandOptions multiAtomOptions = new PdbLigandOptions();
        multiAtomOptions.IncludeSingleAtomResidues = false;
        PdbLigandOptions waterOptions = new PdbLigandOptions();
        waterOptions.IncludeWater = true;
        PdbLigandOptions modelOptions = new PdbLigandOptions();
        modelOptions.ModelId = 2;

        PdbLigandExtractionResult multiAtom = reader.ExtractLigands(molecule, multiAtomOptions);
        PdbLigandExtractionResult withWater = reader.ExtractLigands(molecule, waterOptions);
        PdbLigandExtractionResult otherModel = reader.ExtractLigands(molecule, modelOptions);
        PdbLigandExtractionResult nullResult = reader.ExtractLigands(null, null);
        multiAtom.Ligands[0].Atoms[0].FormalCharge = 7;

        Assert.That(multiAtom.Success, Is.True);
        Assert.That(multiAtom.ExtractedCount, Is.EqualTo(1));
        Assert.That(multiAtom.Residues[0].Name, Is.EqualTo("LIG"));
        Assert.That(withWater.ExtractedCount, Is.EqualTo(3));
        Assert.That(withWater.SkippedWaterCount, Is.Zero);
        Assert.That(otherModel.ExtractedCount, Is.Zero);
        Assert.That(molecule.Atoms[1].FormalCharge, Is.Zero);
        Assert.That(nullResult.Success, Is.False);
        Assert.That(nullResult.Report.Errors[0].Code, Is.EqualTo("PDB_LIGAND_NULL_MOLECULE"));
    }

    [Test]
    public void ReadLigands_PropagatesPdbReadFailuresWithoutExtractingPartialLigands()
    {
        string line = AtomLine("HETATM", 1, "C1", "LIG", "B", 10, 0.0, 0.0, 0.0, "C");
        line = line.Substring(0, 30) + " BADVAL " + line.Substring(38);

        PdbLigandExtractionResult result = new PdbReader().ReadLigands(line + "\n", null);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Ligands, Is.Empty);
        Assert.That(result.Residues, Is.Empty);
        Assert.That(result.Report.Errors[0].Code, Is.EqualTo("PDB_INVALID_FLOAT"));
    }

    static string BuildProteinLigandPdb()
    {
        return "MODEL        1\n"
            + AtomLine("ATOM", 1, "CA", "ALA", "A", 1, 0.0, 0.0, 0.0, "C") + "\n"
            + AtomLine("HETATM", 2, "C1", "LIG", "B", 10, 1.2, 0.0, 0.0, "C") + "\n"
            + AtomLine("HETATM", 3, "O1", "LIG", "B", 10, 2.4, 0.0, 0.0, "O") + "\n"
            + AtomLine("HETATM", 4, "O", "HOH", "W", 20, 5.0, 0.0, 0.0, "O") + "\n"
            + AtomLine("HETATM", 5, "ZN", "ZN", "Z", 30, 7.0, 0.0, 0.0, "Zn") + "\n"
            + "CONECT    1    2\n"
            + "CONECT    2    3\n"
            + "ENDMDL\nEND\n";
    }

    static string AtomLine(string record, int serial, string atomName, string residueName, string chainId, int residueNumber, double x, double y, double z, string element)
    {
        return record.PadRight(6)
            + serial.ToString().PadLeft(5)
            + " "
            + atomName.PadLeft(4)
            + " "
            + residueName.PadRight(3)
            + " "
            + chainId.PadRight(1)
            + residueNumber.ToString().PadLeft(4)
            + " "
            + "   "
            + x.ToString("0.000", CultureInfo.InvariantCulture).PadLeft(8)
            + y.ToString("0.000", CultureInfo.InvariantCulture).PadLeft(8)
            + z.ToString("0.000", CultureInfo.InvariantCulture).PadLeft(8)
            + "1.00".PadLeft(6)
            + "10.00".PadLeft(6)
            + "          "
            + element.PadLeft(2)
            + "  ";
    }
}
