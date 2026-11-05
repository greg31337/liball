using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class Mol2ReaderTests
{
    [Test]
    public void ReadWithResult_ReadsMoleculeAtomsBondsChargesTypesAndResidues()
    {
        Mol2Reader reader = new Mol2Reader();

        Mol2ReadResult result = reader.ReadWithResult(BuildAcetamideMol2());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(0));
        Assert.That(result.Molecule.Metadata.GetPrimaryName(), Is.EqualTo("Acetamide"));
        Assert.That(result.Molecule.Metadata.GetStringValue("MOL2_MOLECULE_TYPE"), Is.EqualTo("SMALL"));
        Assert.That(result.Molecule.Metadata.GetStringValue("MOL2_CHARGE_TYPE"), Is.EqualTo("USER_CHARGES"));
        Assert.That(result.Molecule.Atoms.Count, Is.EqualTo(4));
        Assert.That(result.Molecule.Bonds.Count, Is.EqualTo(3));
        Assert.That(result.Molecule.Atoms[0].AtomicNumber, Is.EqualTo(6));
        Assert.That(result.Molecule.Atoms[0].Label, Is.EqualTo("C1"));
        Assert.That(result.Molecule.Atoms[0].Alias, Is.EqualTo("C1"));
        Assert.That(result.Molecule.Atoms[0].QueryValue, Is.EqualTo("C.3"));
        Assert.That(result.Molecule.Atoms[3].QueryValue, Is.EqualTo("N.am"));
        Assert.That(result.Molecule.Atoms[2].Y, Is.EqualTo(1.25).Within(0.0001));
        Assert.That(result.Molecule.Metadata.GetVendorField("MOL2_ATOM_0_CHARGE"), Is.EqualTo("-0.12"));
        Assert.That(result.Molecule.Metadata.GetVendorField("MOL2_ATOM_3_RESIDUE_NAME"), Is.EqualTo("LIG1"));
        Assert.That(result.Molecule.Bonds[1].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(result.Molecule.Bonds[2].Order, Is.EqualTo(BondOrder.Single));
        Assert.That(result.Molecule.Bonds[2].QueryValue, Is.EqualTo("am"));
        Assert.That(result.Residues.Count, Is.EqualTo(1));
        Assert.That(result.Residues[0].Id, Is.EqualTo(1));
        Assert.That(result.Residues[0].Name, Is.EqualTo("LIG1"));
        Assert.That(result.Residues[0].RootAtomId, Is.EqualTo(0));
        Assert.That(result.Residues[0].ChainId, Is.EqualTo("A"));
        Assert.That(result.Residues[0].AtomIds, Is.EqualTo(new int[] { 0, 1, 2, 3 }));
    }

    [Test]
    public void Read_ReadsMoleculeWithoutResultWrapper()
    {
        Mol2Reader reader = new Mol2Reader();

        Molecule molecule = reader.Read(BuildAcetamideMol2());

        Assert.That(molecule.Atoms.Count, Is.EqualTo(4));
        Assert.That(molecule.Bonds[1].Order, Is.EqualTo(BondOrder.Double));
    }

    [Test]
    public void ReadWithResult_MapsAromaticAndHalogenTypes()
    {
        Mol2Reader reader = new Mol2Reader();

        Mol2ReadResult result = reader.ReadWithResult(BuildAromaticHalogenMol2());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Molecule.Atoms[0].AtomicNumber, Is.EqualTo(6));
        Assert.That(result.Molecule.Atoms[1].AtomicNumber, Is.EqualTo(17));
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Aromatic));
        Assert.That(result.Molecule.Bonds[0].IsAromatic, Is.True);
    }

    [Test]
    public void ReadWithResult_ResolvesNonSequentialAtomIdsAcrossMultipleResidues()
    {
        Mol2Reader reader = new Mol2Reader();

        Mol2ReadResult result = reader.ReadWithResult(BuildTwoResidueMol2());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(0));
        Assert.That(result.Molecule.Metadata.GetPrimaryName(), Is.EqualTo("Dipeptide fragment"));
        Assert.That(result.Molecule.Metadata.Comments, Has.Count.EqualTo(1));
        Assert.That(result.Molecule.Metadata.Comments[0], Is.EqualTo("nonsequential atom ids with two residues"));
        Assert.That(result.Molecule.Atoms.Count, Is.EqualTo(6));
        Assert.That(result.Molecule.Bonds.Count, Is.EqualTo(5));
        Assert.That(result.Molecule.Atoms[0].AtomicNumber, Is.EqualTo(7));
        Assert.That(result.Molecule.Atoms[3].AtomicNumber, Is.EqualTo(8));
        Assert.That(result.Molecule.Atoms[5].Z, Is.EqualTo(0.8).Within(0.0001));
        Assert.That(result.Molecule.Metadata.GetVendorField("MOL2_ATOM_4_RESIDUE_ID"), Is.EqualTo("202"));
        Assert.That(result.Molecule.Metadata.GetVendorField("MOL2_ATOM_5_CHARGE"), Is.EqualTo("0.03"));
        Assert.That(result.Molecule.Bonds[3].BeginAtomId, Is.EqualTo(2));
        Assert.That(result.Molecule.Bonds[3].EndAtomId, Is.EqualTo(4));
        Assert.That(result.Molecule.Bonds[3].Order, Is.EqualTo(BondOrder.Single));
        Assert.That(result.Molecule.Bonds[3].QueryValue, Is.EqualTo("am"));
        Assert.That(result.Residues.Count, Is.EqualTo(2));
        Assert.That(result.Residues[0].Id, Is.EqualTo(101));
        Assert.That(result.Residues[0].Name, Is.EqualTo("ALA"));
        Assert.That(result.Residues[0].RootAtomId, Is.EqualTo(0));
        Assert.That(result.Residues[0].ChainId, Is.EqualTo("A"));
        Assert.That(result.Residues[0].AtomIds, Is.EqualTo(new int[] { 0, 1, 2, 3 }));
        Assert.That(result.Residues[1].Id, Is.EqualTo(202));
        Assert.That(result.Residues[1].Name, Is.EqualTo("GLY"));
        Assert.That(result.Residues[1].RootAtomId, Is.EqualTo(4));
        Assert.That(result.Residues[1].ChainId, Is.EqualTo("B"));
        Assert.That(result.Residues[1].AtomIds, Is.EqualTo(new int[] { 4, 5 }));
    }

    [Test]
    public void ReadWithResult_ReturnsDiagnosticForMissingMoleculeSection()
    {
        Mol2Reader reader = new Mol2Reader();

        Mol2ReadResult result = reader.ReadWithResult("@<TRIPOS>ATOM\n1 C1 0 0 0 C.3\n");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("MOL2_MOLECULE_MISSING"));
    }

    [Test]
    public void ReadWithResult_ReturnsDiagnosticForInvalidBondAtom()
    {
        Mol2Reader reader = new Mol2Reader();

        Mol2ReadResult result = reader.ReadWithResult(BuildInvalidBondMol2());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("MOL2_INVALID_BOND_ATOM"));
    }

    [Test]
    public void ReadWithResult_ReturnsDiagnosticForInvalidAtomCoordinate()
    {
        Mol2Reader reader = new Mol2Reader();

        Mol2ReadResult result = reader.ReadWithResult(BuildInvalidAtomCoordinateMol2());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("MOL2_INVALID_FLOAT"));
        Assert.That(result.Report.Get(0).Location.LineNumber, Is.EqualTo(7));
        Assert.That(result.Report.Get(0).Location.Context, Does.Contain("not-a-number"));
    }

    [Test]
    public void ReadWithResult_ReturnsDiagnosticForInvalidSubstructureRootAtom()
    {
        Mol2Reader reader = new Mol2Reader();

        Mol2ReadResult result = reader.ReadWithResult(BuildInvalidSubstructureRootMol2());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("MOL2_INVALID_INTEGER"));
        Assert.That(result.Report.Get(0).Location.LineNumber, Is.EqualTo(12));
        Assert.That(result.Report.Get(0).Location.Context, Does.Contain("ROOT_ATOM"));
    }

    static string BuildAcetamideMol2()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "@<TRIPOS>MOLECULE",
                "Acetamide",
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

    static string BuildAromaticHalogenMol2()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "@<TRIPOS>MOLECULE",
                "Aryl chloride",
                "2 1 0 0 0",
                "SMALL",
                "NO_CHARGES",
                "@<TRIPOS>ATOM",
                "1 CA 0.0 0.0 0.0 C.ar",
                "2 CL1 1.5 0.0 0.0 Cl",
                "@<TRIPOS>BOND",
                "1 1 2 ar",
                ""
            });
    }

    static string BuildTwoResidueMol2()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "@<TRIPOS>MOLECULE",
                "Dipeptide fragment",
                "6 5 2 0 0",
                "SMALL",
                "USER_CHARGES",
                "nonsequential atom ids with two residues",
                "@<TRIPOS>ATOM",
                "10 N -1.200 0.000 0.000 N.am 101 ALA -0.31",
                "20 CA 0.000 0.000 0.200 C.3 101 ALA 0.12",
                "30 C 1.400 0.100 0.000 C.2 101 ALA 0.51",
                "40 O 2.100 1.100 -0.100 O.2 101 ALA -0.51",
                "110 N2 1.900 -1.000 0.100 N.am 202 GLY -0.28",
                "130 CA2 3.200 -1.100 0.800 C.3 202 GLY 0.03",
                "@<TRIPOS>BOND",
                "1 10 20 1",
                "2 20 30 1",
                "3 30 40 2",
                "4 30 110 am",
                "5 110 130 1",
                "@<TRIPOS>SUBSTRUCTURE",
                "101 ALA 10 RESIDUE 0 A **** 0 ROOT",
                "202 GLY 110 RESIDUE 0 B **** 0 ROOT",
                ""
            });
    }

    static string BuildInvalidAtomCoordinateMol2()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "@<TRIPOS>MOLECULE",
                "Invalid atom coordinate",
                "1 0 0 0 0",
                "SMALL",
                "USER_CHARGES",
                "@<TRIPOS>ATOM",
                "1 C1 not-a-number 0.0 0.0 C.3 1 LIG1 0.0",
                ""
            });
    }

    static string BuildInvalidSubstructureRootMol2()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "@<TRIPOS>MOLECULE",
                "Invalid substructure root",
                "2 1 1 0 0",
                "SMALL",
                "NO_CHARGES",
                "@<TRIPOS>ATOM",
                "10 C1 0.0 0.0 0.0 C.3 1 LIG1",
                "20 O1 1.2 0.0 0.0 O.2 1 LIG1",
                "@<TRIPOS>BOND",
                "1 10 20 1",
                "@<TRIPOS>SUBSTRUCTURE",
                "1 LIG1 ROOT_ATOM GROUP 0 A **** 0 ROOT",
                ""
            });
    }

    static string BuildInvalidBondMol2()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "@<TRIPOS>MOLECULE",
                "Invalid bond",
                "1 1 0 0 0",
                "SMALL",
                "NO_CHARGES",
                "@<TRIPOS>ATOM",
                "1 C1 0.0 0.0 0.0 C.3",
                "@<TRIPOS>BOND",
                "1 1 2 1",
                ""
            });
    }
}
