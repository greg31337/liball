using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class Mol2WriterTests
{
    [Test]
    public void Write_EmitsMoleculeAtomBondAndSubstructureSections()
    {
        Mol2Writer writer = new Mol2Writer();

        string mol2 = writer.Write(BuildAcetamideMolecule());

        Assert.That(mol2, Does.StartWith("@<TRIPOS>MOLECULE\nAcetamide\n4 3 1 0 0\nSMALL\nUSER_CHARGES\n"));
        Assert.That(mol2, Does.Contain("@<TRIPOS>ATOM\n"));
        Assert.That(mol2, Does.Contain("1 C1 0.0000 0.0000 0.0000 C.3 1 LIG1 -0.12\n"));
        Assert.That(mol2, Does.Contain("4 N1 2.2500 -1.2500 0.0000 N.am 1 LIG1 0.07\n"));
        Assert.That(mol2, Does.Contain("@<TRIPOS>BOND\n"));
        Assert.That(mol2, Does.Contain("2 2 3 2\n"));
        Assert.That(mol2, Does.Contain("3 2 4 am\n"));
        Assert.That(mol2, Does.Contain("@<TRIPOS>SUBSTRUCTURE\n"));
        Assert.That(mol2, Does.Contain("1 LIG1 1 GROUP 0 A **** 0 ROOT\n"));
    }

    [Test]
    public void Write_RoundTripsThroughMol2Reader()
    {
        Mol2Writer writer = new Mol2Writer();
        Mol2Reader reader = new Mol2Reader();

        Mol2ReadResult result = reader.ReadWithResult(writer.Write(BuildAcetamideMolecule()));

        Assert.That(result.Success, Is.True);
        Assert.That(result.Molecule.Atoms.Count, Is.EqualTo(4));
        Assert.That(result.Molecule.Bonds.Count, Is.EqualTo(3));
        Assert.That(result.Molecule.Atoms[0].QueryValue, Is.EqualTo("C.3"));
        Assert.That(result.Molecule.Metadata.GetVendorField("MOL2_ATOM_0_CHARGE"), Is.EqualTo("-0.12"));
        Assert.That(result.Molecule.Bonds[2].QueryValue, Is.EqualTo("am"));
        Assert.That(result.Residues.Count, Is.EqualTo(1));
        Assert.That(result.Residues[0].Name, Is.EqualTo("LIG1"));
        Assert.That(result.Residues[0].AtomIds, Is.EqualTo(new int[] { 0, 1, 2, 3 }));
    }

    [Test]
    public void Write_InfersDefaultsForPlainMolecule()
    {
        Mol2Writer writer = new Mol2Writer();
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 2, 0, 0, false, "", "O"));
        builder.AddBond(carbon, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        Molecule molecule = builder.Build();
        molecule.Metadata.AddName("Carbonyl");

        string mol2 = writer.Write(molecule);

        Assert.That(mol2, Does.Contain("Carbonyl\n2 1 0 0 0\nSMALL\nNO_CHARGES\n"));
        Assert.That(mol2, Does.Contain("1 C 0.0000 0.0000 0.0000 C\n"));
        Assert.That(mol2, Does.Contain("2 O 0.0000 0.0000 0.0000 O\n"));
        Assert.That(mol2, Does.Contain("1 1 2 2\n"));
        Assert.That(mol2, Does.Not.Contain("@<TRIPOS>SUBSTRUCTURE"));
    }

    [Test]
    public void Write_ReturnsEmptyTextForNullMolecule()
    {
        Mol2Writer writer = new Mol2Writer();

        Assert.That(writer.Write(null), Is.EqualTo(""));
    }

    [Test]
    public void WriteMol2WithResult_ReturnsFieldsCountsAndDiagnostics()
    {
        Mol2Writer writer = new Mol2Writer();
        IMol2Writer contract = writer;

        Mol2WriteResult result = contract.WriteMol2WithResult(BuildAcetamideMolecule());
        string directText = contract.WriteMol2(BuildAcetamideMolecule());
        Mol2WriteResult nullResult = contract.WriteMol2WithResult(null);
        Molecule invalidMolecule = new Molecule();
        invalidMolecule.Atoms.Add(new Atom(0, new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C")));
        invalidMolecule.Bonds.Add(new Bond(0, 0, 2, new BondSpec()));
        Mol2WriteResult invalidResult = writer.WriteMol2WithResult(invalidMolecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.AtomCount, Is.EqualTo(4));
        Assert.That(result.BondCount, Is.EqualTo(3));
        Assert.That(result.SubstructureCount, Is.EqualTo(1));
        Assert.That(result.MoleculeType, Is.EqualTo("SMALL"));
        Assert.That(result.ChargeType, Is.EqualTo("USER_CHARGES"));
        Assert.That(directText, Is.EqualTo(result.Text));
        Assert.That(nullResult.Success, Is.False);
        Assert.That(nullResult.Report.Errors[0].Code, Is.EqualTo("MOL2_NULL_MOLECULE"));
        Assert.That(invalidResult.Success, Is.False);
        Assert.That(invalidResult.Text, Is.Empty);
        Assert.That(invalidResult.Report.Errors[0].Code, Is.EqualTo("MOL2_INVALID_BOND_ATOM"));
    }

    [Test]
    public void ReadWriteRead_PreservesTypedMol2Fields()
    {
        Mol2Reader reader = new Mol2Reader();
        Mol2ReadResult first = reader.ReadWithResult(BuildTypedRoundTripMol2());

        Mol2WriteResult written = new Mol2Writer().WriteMol2WithResult(first.Molecule);
        Mol2ReadResult second = reader.ReadWithResult(written.Text);

        Assert.That(first.Success, Is.True);
        Assert.That(first.DeclaredAtomCount, Is.EqualTo(3));
        Assert.That(first.DeclaredBondCount, Is.EqualTo(2));
        Assert.That(first.DeclaredSubstructureCount, Is.EqualTo(2));
        Assert.That(first.MoleculeType, Is.EqualTo("BIOPOLYMER"));
        Assert.That(first.ChargeType, Is.EqualTo("USER_CHARGES"));
        Assert.That(written.Success, Is.True);
        Assert.That(written.AtomCount, Is.EqualTo(3));
        Assert.That(written.BondCount, Is.EqualTo(2));
        Assert.That(written.SubstructureCount, Is.EqualTo(2));
        Assert.That(written.MoleculeType, Is.EqualTo("BIOPOLYMER"));
        Assert.That(written.ChargeType, Is.EqualTo("USER_CHARGES"));
        Assert.That(second.Success, Is.True);
        Assert.That(second.Molecule.Metadata.GetPrimaryName(), Is.EqualTo("Typed fragment"));
        Assert.That(second.Molecule.Metadata.Comments, Is.EqualTo(new string[] { "round-trip comment" }));
        Assert.That(second.Molecule.Atoms[0].Label, Is.EqualTo("N1"));
        Assert.That(second.Molecule.Atoms[0].AtomType, Is.EqualTo("N.am"));
        Assert.That(second.Molecule.Atoms[0].ResidueId, Is.EqualTo(101));
        Assert.That(second.Molecule.Atoms[0].ResidueName, Is.EqualTo("ALA"));
        Assert.That(second.Molecule.Atoms[0].HasPartialCharge, Is.True);
        Assert.That(second.Molecule.Atoms[0].PartialCharge, Is.EqualTo(-0.31).Within(0.0001));
        Assert.That(second.Molecule.Bonds[0].QueryValue, Is.EqualTo("am"));
        Assert.That(second.Molecule.Bonds[1].QueryValue, Is.EqualTo("ar"));
        Assert.That(second.Molecule.Bonds[1].IsAromatic, Is.True);
        Assert.That(second.Residues, Has.Count.EqualTo(2));
        Assert.That(second.Residues[0].Id, Is.EqualTo(101));
        Assert.That(second.Residues[0].Name, Is.EqualTo("ALA"));
        Assert.That(second.Residues[0].Type, Is.EqualTo("RESIDUE"));
        Assert.That(second.Residues[0].RootAtomId, Is.EqualTo(0));
        Assert.That(second.Residues[0].ChainId, Is.EqualTo("A"));
        Assert.That(second.Residues[0].AtomIds, Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(second.Residues[1].Id, Is.EqualTo(202));
        Assert.That(second.Residues[1].Type, Is.EqualTo("GROUP"));
        Assert.That(second.Residues[1].ChainId, Is.Empty);
        Assert.That(second.Residues[1].AtomIds, Is.EqualTo(new int[] { 2 }));
    }

    [Test]
    public void Write_UsesTypedResidueMembershipWithoutVendorMetadata()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "CA"));
        Molecule molecule = builder.Build();
        molecule.Atoms[carbon].AtomType = "C.3";
        molecule.Atoms[carbon].HasPartialCharge = true;
        molecule.Atoms[carbon].PartialCharge = 0.25;
        Residue residue = new Residue();
        residue.Id = 7;
        residue.Name = "GLY";
        residue.Type = "RESIDUE";
        residue.ChainId = "B";
        residue.RootAtomId = carbon;
        residue.AtomIds.Add(carbon);
        molecule.Residues.Add(residue);

        Mol2WriteResult written = new Mol2Writer().WriteMol2WithResult(molecule);
        Mol2ReadResult read = new Mol2Reader().ReadWithResult(written.Text);

        Assert.That(written.Success, Is.True);
        Assert.That(written.ChargeType, Is.EqualTo("USER_CHARGES"));
        Assert.That(written.Text, Does.Contain("1 CA 0.0000 0.0000 0.0000 C.3 7 GLY 0.2500"));
        Assert.That(written.Text, Does.Contain("7 GLY 1 RESIDUE 0 B **** 0 ROOT"));
        Assert.That(read.Success, Is.True);
        Assert.That(read.Molecule.Atoms[0].AtomType, Is.EqualTo("C.3"));
        Assert.That(read.Molecule.Atoms[0].PartialCharge, Is.EqualTo(0.25).Within(0.0001));
        Assert.That(read.Residues[0].AtomIds, Is.EqualTo(new int[] { 0 }));
    }

    static Molecule BuildAcetamideMolecule()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int methyl = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C1"));
        int carbonyl = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C2"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O1"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", "N1"));
        builder.AddBond(methyl, carbonyl, new BondSpec());
        builder.AddBond(carbonyl, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbonyl, nitrogen, new BondSpec());
        Molecule molecule = builder.Build();
        molecule.Metadata.AddName("Acetamide");
        molecule.Metadata.SetStringValue("MOL2_MOLECULE_TYPE", "SMALL");
        molecule.Metadata.SetStringValue("MOL2_CHARGE_TYPE", "USER_CHARGES");
        SetAtomMol2(molecule, 0, "C.3", "1", "LIG1", "-0.12");
        SetAtomMol2(molecule, 1, "C.2", "1", "LIG1", "0.55");
        SetAtomMol2(molecule, 2, "O.2", "1", "LIG1", "-0.50");
        SetAtomMol2(molecule, 3, "N.am", "1", "LIG1", "0.07");
        molecule.Metadata.AddVendorField("MOL2_SUBSTRUCTURE_1_NAME", "LIG1");
        molecule.Metadata.AddVendorField("MOL2_SUBSTRUCTURE_1_CHAIN", "A");
        molecule.Bonds[2].QueryValue = "am";
        SetCoordinates(molecule.Atoms[0], 0.0, 0.0, 0.0);
        SetCoordinates(molecule.Atoms[1], 1.5, 0.0, 0.0);
        SetCoordinates(molecule.Atoms[2], 2.25, 1.25, 0.0);
        SetCoordinates(molecule.Atoms[3], 2.25, -1.25, 0.0);
        return molecule;
    }

    static string BuildTypedRoundTripMol2()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "@<TRIPOS>MOLECULE",
                "Typed fragment",
                "3 2 2 0 0",
                "BIOPOLYMER",
                "USER_CHARGES",
                "round-trip comment",
                "@<TRIPOS>ATOM",
                "10 N1 0.0 0.0 0.0 N.am 101 ALA -0.31",
                "20 C1 1.4 0.0 0.0 C.2 101 ALA 0.51",
                "30 C2 2.8 0.0 0.0 C.ar 202 RING -0.20",
                "@<TRIPOS>BOND",
                "1 10 20 am",
                "2 20 30 ar",
                "@<TRIPOS>SUBSTRUCTURE",
                "101 ALA 10 RESIDUE 0 A **** 0 ROOT",
                "202 RING 30 GROUP 0 **** **** 0 ROOT",
                ""
            });
    }

    static void SetAtomMol2(Molecule molecule, int atomId, string atomType, string residueId, string residueName, string charge)
    {
        molecule.Metadata.AddVendorField("MOL2_ATOM_" + atomId.ToString() + "_TYPE", atomType);
        molecule.Metadata.AddVendorField("MOL2_ATOM_" + atomId.ToString() + "_RESIDUE_ID", residueId);
        molecule.Metadata.AddVendorField("MOL2_ATOM_" + atomId.ToString() + "_RESIDUE_NAME", residueName);
        molecule.Metadata.AddVendorField("MOL2_ATOM_" + atomId.ToString() + "_CHARGE", charge);
    }

    static void SetCoordinates(Atom atom, double x, double y, double z)
    {
        atom.HasCoordinates = true;
        atom.X = x;
        atom.Y = y;
        atom.Z = z;
    }
}
