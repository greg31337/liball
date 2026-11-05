using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class GoldenCorpusTests
{
    [Test]
    public void GoldenCorpus_SmilesParsesCanonicalAcetamide()
    {
        MoleculeFormats formats = new MoleculeFormats();
        FormatOptions options = new FormatOptions();
        options.KindHint = FormatKind.Smiles;

        Molecule molecule = formats.ReadString("CC(=O)N", options);

        Assert.That(molecule.Atoms.Count, Is.EqualTo(4));
        Assert.That(molecule.Bonds.Count, Is.EqualTo(3));
        Assert.That(molecule.Atoms[2].AtomicNumber, Is.EqualTo(8));
        Assert.That(molecule.Bonds[1].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(formats.WriteString(molecule, options), Is.EqualTo("CC(=O)N"));
    }

    [Test]
    public void GoldenCorpus_SmartsFindsCarbonylSubstructure()
    {
        SmilesParser smiles = new SmilesParser();
        SmartsParser smarts = new SmartsParser();
        SubgraphMatcher matcher = new SubgraphMatcher();

        Molecule molecule = smiles.Parse("CC(=O)N");
        QueryMolecule query = smarts.Parse("[#6]=[#8]");
        SubgraphMatch match = matcher.FindFirstQuery(molecule, query);

        Assert.That(query.GetAtomCount(), Is.EqualTo(2));
        Assert.That(query.GetBond(0).Order, Is.EqualTo(BondOrder.Double));
        Assert.That(match.Success, Is.True);
        Assert.That(match.AtomMapping, Is.EqualTo(new int[] { 1, 2 }));
    }

    [Test]
    public void GoldenCorpus_MolV2000ReadsCoordinatesChargesIsotopesAndStereo()
    {
        MoleculeFormats formats = new MoleculeFormats();

        Molecule molecule = formats.ReadStringAs(BuildGoldenMolfile(), FormatKind.MolV2000);

        Assert.That(molecule.Metadata.GetPrimaryName(), Is.EqualTo("Golden amide"));
        Assert.That(molecule.Atoms.Count, Is.EqualTo(4));
        Assert.That(molecule.Bonds.Count, Is.EqualTo(3));
        Assert.That(molecule.Atoms[0].IsotopeMassNumber, Is.EqualTo(13));
        Assert.That(molecule.Atoms[3].FormalCharge, Is.EqualTo(1));
        Assert.That(molecule.Bonds[1].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(molecule.Bonds[2].Stereo, Is.EqualTo(BondStereo.Wedge));
    }

    [Test]
    public void GoldenCorpus_SdfReadsDataFieldsAndRecordSeparator()
    {
        SdfReader reader = new SdfReader();

        List<SdfRecord> records = reader.ReadRecords(BuildGoldenSdf());

        Assert.That(records.Count, Is.EqualTo(1));
        Assert.That(records[0].Success, Is.True);
        Assert.That(records[0].HasRecordSeparator, Is.True);
        Assert.That(records[0].Molecule.Metadata.GetPrimaryName(), Is.EqualTo("Golden SDF"));
        Assert.That(records[0].Molecule.Metadata.GetSdfField("GOLDEN_ID"), Is.EqualTo("SDF-001"));
        Assert.That(records[0].Molecule.Metadata.GetSdfField("NOTE"), Is.EqualTo("first line\nsecond line"));
    }

    [Test]
    public void GoldenCorpus_Mol2ReadsTypesChargesResiduesAndBonds()
    {
        MoleculeFormats formats = new MoleculeFormats();

        Molecule molecule = formats.ReadStringAs(BuildGoldenMol2(), FormatKind.Mol2);

        Assert.That(molecule.Metadata.GetPrimaryName(), Is.EqualTo("GoldenMol2"));
        Assert.That(molecule.Atoms.Count, Is.EqualTo(4));
        Assert.That(molecule.Bonds.Count, Is.EqualTo(3));
        Assert.That(molecule.Atoms[0].QueryValue, Is.EqualTo("C.3"));
        Assert.That(molecule.Metadata.GetVendorField("MOL2_ATOM_0_CHARGE"), Is.EqualTo("-0.12"));
        Assert.That(molecule.Metadata.GetVendorField("MOL2_ATOM_3_RESIDUE_NAME"), Is.EqualTo("LIG1"));
        Assert.That(molecule.Bonds[1].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(molecule.Bonds[2].QueryValue, Is.EqualTo("am"));
    }

    [Test]
    public void GoldenCorpus_PdbReadsResiduesChainsCoordinatesAndConect()
    {
        MoleculeFormats formats = new MoleculeFormats();

        Molecule molecule = formats.ReadStringAs(BuildGoldenPdb(), FormatKind.Pdb);

        Assert.That(molecule.Atoms.Count, Is.EqualTo(3));
        Assert.That(molecule.Bonds.Count, Is.EqualTo(2));
        Assert.That(molecule.Atoms[0].AtomicNumber, Is.EqualTo(7));
        Assert.That(molecule.Atoms[0].FormalCharge, Is.EqualTo(1));
        Assert.That(molecule.Atoms[2].FormalCharge, Is.EqualTo(-1));
        Assert.That(molecule.Atoms[1].X, Is.EqualTo(12.560).Within(0.0001));
        Assert.That(molecule.Metadata.GetPdbField("PDB_ATOM_0_RECORD"), Is.EqualTo("ATOM"));
        Assert.That(molecule.Metadata.GetPdbField("PDB_ATOM_2_RECORD"), Is.EqualTo("HETATM"));
    }

    [Test]
    public void GoldenCorpus_StandardizationRunsParentCleanupPipeline()
    {
        Standardizer standardizer = new Standardizer();
        StandardizationOptions options = new StandardizationOptions();
        options.EnableParentCleanup();
        options.IsotopeCleanup.ConfigureRemoveAll();
        options.RadicalCleanup.ConfigureRemoveAll();

        StandardizationResult result = standardizer.Standardize(BuildDecoratedEnolWithSalt(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Changed, Is.True);
        Assert.That(result.StepCount, Is.EqualTo(10));
        Assert.That(result.Mutations[2].StepName, Is.EqualTo("explicit-hydrogen-normalization"));
        Assert.That(result.Mutations[8].StepName, Is.EqualTo("aromaticity-normalization"));
        Assert.That(result.Molecule.Atoms.Count, Is.EqualTo(3));
        Assert.That(result.Molecule.Atoms[1].IsotopeMassNumber, Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms[2].RadicalElectronCount, Is.EqualTo(0));
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
    }

    [Test]
    public void GoldenCorpus_InChIWorkflowAttachesAndExportsIdentifiers()
    {
        MoleculeFormats formats = new MoleculeFormats();
        Molecule molecule = new Molecule();

        bool attached = new InChIWorkflows().AttachIdentifiers(molecule, "InChI=1S/H2O/h1H2", "XLYOFNOQVPJJNP-UHFFFAOYSA-N");

        Assert.That(attached, Is.True);
        Assert.That(formats.WriteStringAs(molecule, FormatKind.InChI), Is.EqualTo("InChI=1S/H2O/h1H2"));
        Assert.That(formats.WriteStringAs(molecule, FormatKind.InChIKey), Is.EqualTo("XLYOFNOQVPJJNP-UHFFFAOYSA-N"));
        Assert.That(molecule.Metadata.GetStringValue("INCHI_FORMULA"), Is.EqualTo("H2O"));
        Assert.That(molecule.Metadata.GetStringValue("INCHI_KEY_FIRST_BLOCK"), Is.EqualTo("XLYOFNOQVPJJNP"));
    }

    static string BuildGoldenMolfile()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "Golden amide",
                "  molecule tests",
                "golden mol",
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
                "M  END"
            });
    }

    static string BuildGoldenSdf()
    {
        return BuildSimpleMolfile("Golden SDF")
            + ">  <GOLDEN_ID>\n"
            + "SDF-001\n"
            + "\n"
            + ">  <NOTE>\n"
            + "first line\n"
            + "second line\n"
            + "\n"
            + "$$$$\n";
    }

    static string BuildSimpleMolfile(string name)
    {
        return string.Join(
            "\n",
            new string[]
            {
                name,
                "  molecule tests",
                "",
                "  1  0  0  0  0  0            999 V2000",
                "    0.0000    0.0000    0.0000 C   0  0  0  0  0  0  0  0  0  0  0  0",
                "M  END",
                ""
            });
    }

    static string BuildGoldenMol2()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "@<TRIPOS>MOLECULE",
                "GoldenMol2",
                "4 3 1 0 0",
                "SMALL",
                "USER_CHARGES",
                "@<TRIPOS>ATOM",
                "1 C1 0.0 0.0 0.0 C.3 1 LIG1 -0.12",
                "2 C2 1.5 0.0 0.0 C.2 1 LIG1 0.55",
                "3 O1 2.25 1.25 0.0 O.2 1 LIG1 -0.50",
                "4 N1 2.25 -1.25 0.0 N.am 1 LIG1 0.07",
                "@<TRIPOS>BOND",
                "1 1 2 1",
                "2 2 3 2",
                "3 2 4 am",
                "@<TRIPOS>SUBSTRUCTURE",
                "1 LIG1 1 GROUP 0 A **** 0 ROOT",
                ""
            });
    }

    static string BuildGoldenPdb()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "HEADER    GOLDEN PDB",
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

    static Molecule BuildDecoratedEnolWithSalt()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 18, 0, 0, 1, 0, 0, false, "", "O"));
        int terminalCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 1, 0, false, "", "C"));
        builder.AddBond(carbon, oxygen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbon, terminalCarbon, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddAtom(new AtomSpec(11, 0, 1, 0, 0, 0, 0, false, "", "Na"));
        return builder.Build();
    }
}
