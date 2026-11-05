using System.Text;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class V3000ReaderTests
{
    [Test]
    public void Read_ReturnsMoleculeWithoutResultWrapper()
    {
        V3000Reader reader = new V3000Reader();

        Molecule molecule = reader.Read(BuildAnnotatedV3000Molfile());

        Assert.That(molecule.Atoms, Has.Count.EqualTo(4));
        Assert.That(molecule.Bonds, Has.Count.EqualTo(3));
    }

    [Test]
    public void ReadWithResult_ReadsAtomsBondsPropertiesCollectionsAndEnhancedStereo()
    {
        V3000Reader reader = new V3000Reader();

        MolfileReadResult result = reader.ReadWithResult(BuildAnnotatedV3000Molfile());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(0));
        Assert.That(result.Molecule.Metadata.GetPrimaryName(), Is.EqualTo("V3000 annotated"));
        Assert.That(result.Molecule.Atoms.Count, Is.EqualTo(4));
        Assert.That(result.Molecule.Bonds.Count, Is.EqualTo(3));
        Assert.That(result.Molecule.Atoms[0].AtomicNumber, Is.EqualTo(6));
        Assert.That(result.Molecule.Atoms[0].IsotopeMassNumber, Is.EqualTo(13));
        Assert.That(result.Molecule.Atoms[0].AtomMapNumber, Is.EqualTo(7));
        Assert.That(result.Molecule.Atoms[2].FormalCharge, Is.EqualTo(-1));
        Assert.That(result.Molecule.Atoms[3].FormalCharge, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[3].RadicalElectronCount, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[3].X, Is.EqualTo(2.25).Within(0.0001));
        Assert.That(result.Molecule.Bonds[1].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(result.Molecule.Bonds[2].Stereo, Is.EqualTo(BondStereo.Wedge));
        Assert.That(result.Molecule.StereoRecords.Count, Is.EqualTo(1));
        Assert.That(result.Molecule.StereoRecords[0].CenterAtomId, Is.EqualTo(3));
        Assert.That(result.Molecule.StereoRecords[0].Chirality, Is.EqualTo(Chirality.Clockwise));
        Assert.That(result.Molecule.StereoGroups.Groups.Count, Is.EqualTo(1));
        Assert.That(result.Molecule.StereoGroups.Groups[0].Type, Is.EqualTo("or"));
        Assert.That(result.Molecule.StereoGroups.Groups[0].Label, Is.EqualTo("MDLV30/STEREL1"));
        Assert.That(result.Molecule.StereoGroups.Groups[0].StereoRecordIds, Is.EqualTo(new int[] { 0 }));
    }

    [Test]
    public void MolfileReader_DelegatesV3000InputToV3000Reader()
    {
        MolfileReader reader = new MolfileReader();

        MolfileReadResult result = reader.ReadWithResult(BuildAnnotatedV3000Molfile());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Molecule.Atoms.Count, Is.EqualTo(4));
        Assert.That(result.Molecule.StereoGroups.Groups.Count, Is.EqualTo(1));
    }

    [Test]
    public void ReadWithResult_AllowsMoreThanV2000AtomLimit()
    {
        V3000Reader reader = new V3000Reader();

        MolfileReadResult result = reader.ReadWithResult(BuildLargeV3000Molfile(1001));

        Assert.That(result.Success, Is.True);
        Assert.That(result.Molecule.Atoms.Count, Is.EqualTo(1001));
        Assert.That(result.Molecule.Atoms[1000].AtomicNumber, Is.EqualTo(6));
    }

    [Test]
    public void ReadWithResult_ResolvesNonSequentialIdsAndLessCommonBondTypes()
    {
        V3000Reader reader = new V3000Reader();

        MolfileReadResult result = reader.ReadWithResult(BuildMappedBondTypesV3000Molfile());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(0));
        Assert.That(result.Molecule.Metadata.GetPrimaryName(), Is.EqualTo("V3000 mapped bond types"));
        Assert.That(result.Molecule.Atoms.Count, Is.EqualTo(5));
        Assert.That(result.Molecule.Bonds.Count, Is.EqualTo(4));
        Assert.That(result.Molecule.Atoms[0].AtomicNumber, Is.EqualTo(6));
        Assert.That(result.Molecule.Atoms[1].AtomicNumber, Is.EqualTo(7));
        Assert.That(result.Molecule.Atoms[4].AtomicNumber, Is.EqualTo(17));
        Assert.That(result.Molecule.Atoms[4].AtomMapNumber, Is.EqualTo(42));
        Assert.That(result.Molecule.Atoms[3].Y, Is.EqualTo(1.75).Within(0.0001));
        Assert.That(result.Molecule.Bonds[0].BeginAtomId, Is.EqualTo(0));
        Assert.That(result.Molecule.Bonds[0].EndAtomId, Is.EqualTo(1));
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Dative));
        Assert.That(result.Molecule.Bonds[0].IsDative, Is.True);
        Assert.That(result.Molecule.Bonds[1].Order, Is.EqualTo(BondOrder.Triple));
        Assert.That(result.Molecule.Bonds[2].Order, Is.EqualTo(BondOrder.Aromatic));
        Assert.That(result.Molecule.Bonds[2].IsAromatic, Is.True);
        Assert.That(result.Molecule.Bonds[3].Stereo, Is.EqualTo(BondStereo.Hash));
        Assert.That(result.Molecule.Bonds[3].Direction, Is.EqualTo(BondDirection.Down));
    }

    [Test]
    public void ReadWithResult_ReturnsDiagnosticForMissingCounts()
    {
        V3000Reader reader = new V3000Reader();

        MolfileReadResult result = reader.ReadWithResult(BuildMissingCountsV3000Molfile());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("MOLFILE_V3000_COUNTS_MISSING"));
    }

    [Test]
    public void ReadWithResult_ReturnsDiagnosticForInvalidBondAtom()
    {
        V3000Reader reader = new V3000Reader();

        MolfileReadResult result = reader.ReadWithResult(BuildInvalidBondV3000Molfile());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("MOLFILE_INVALID_BOND_ATOM"));
    }

    [Test]
    public void ReadWithResult_ReturnsDiagnosticForInvalidCountsValue()
    {
        V3000Reader reader = new V3000Reader();

        MolfileReadResult result = reader.ReadWithResult(BuildInvalidCountsV3000Molfile());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("MOLFILE_INVALID_INTEGER"));
        Assert.That(result.Report.Get(0).Location.LineNumber, Is.EqualTo(6));
        Assert.That(result.Report.Get(0).Location.Context, Does.Contain("COUNTS one 0"));
    }

    [Test]
    public void ReadWithResult_RejectsNegativeCounts()
    {
        V3000Reader reader = new V3000Reader();
        string text = BuildInvalidCountsV3000Molfile().Replace("COUNTS one 0", "COUNTS -1 0");

        MolfileReadResult result = reader.ReadWithResult(text);

        Assert.That(result.Success, Is.False);
        Assert.That(result.DeclaredAtomCount, Is.EqualTo(-1));
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("MOLFILE_INVALID_COUNTS"));
        Assert.That(result.Report.Get(0).Location.LineNumber, Is.EqualTo(6));
    }

    [Test]
    public void ReadWithResult_ReturnsDiagnosticForInvalidAtomCoordinate()
    {
        V3000Reader reader = new V3000Reader();

        MolfileReadResult result = reader.ReadWithResult(BuildInvalidAtomCoordinateV3000Molfile());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("MOLFILE_INVALID_FLOAT"));
        Assert.That(result.Report.Get(0).Location.LineNumber, Is.EqualTo(8));
        Assert.That(result.Report.Get(0).Location.Context, Does.Contain("not-a-number"));
    }

    [Test]
    public void ReadWithResult_ReturnsDiagnosticForIncompleteAtomLine()
    {
        V3000Reader reader = new V3000Reader();

        MolfileReadResult result = reader.ReadWithResult(BuildIncompleteAtomLineV3000Molfile());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("MOLFILE_V3000_INVALID_ATOM"));
        Assert.That(result.Report.Get(0).Location.LineNumber, Is.EqualTo(8));
        Assert.That(result.Report.Get(0).Location.Context, Does.Contain("M  V30 1 C 0.0"));
    }

    static string BuildAnnotatedV3000Molfile()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "V3000 annotated",
                "  molecule tests",
                "collections and properties",
                "  0  0  0  0  0  0            999 V3000",
                "M  V30 BEGIN CTAB",
                "M  V30 COUNTS 4 3 0 0 0",
                "M  V30 BEGIN ATOM",
                "M  V30 1 C 0.0 0.0 0.0 7 MASS=13",
                "M  V30 2 C 1.5 0.0 0.0 0",
                "M  V30 3 O 2.25 1.25 0.0 0 CHG=-1",
                "M  V30 4 N 2.25 -1.25 0.0 0 CHG=1 RAD=2 CFG=1",
                "M  V30 END ATOM",
                "M  V30 BEGIN BOND",
                "M  V30 1 1 1 2",
                "M  V30 2 2 2 3",
                "M  V30 3 1 2 4 CFG=1",
                "M  V30 END BOND",
                "M  V30 BEGIN COLLECTION",
                "M  V30 MDLV30/STEREL1 ATOMS=(1 -",
                "M  V30 4)",
                "M  V30 END COLLECTION",
                "M  V30 END CTAB",
                "M  END"
            });
    }

    static string BuildMappedBondTypesV3000Molfile()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "V3000 mapped bond types",
                "  molecule tests",
                "nonsequential ids and bond mapping",
                "  0  0  0  0  0  0            999 V3000",
                "M  V30 BEGIN CTAB",
                "M  V30 COUNTS 5 4 0 0 0",
                "M  V30 BEGIN ATOM",
                "M  V30 10 C 0.0 0.0 0.0 0",
                "M  V30 30 N 1.4 0.0 0.0 0",
                "M  V30 20 O 2.8 0.0 0.0 0 CHG=-1",
                "M  V30 80 C 1.4 1.75 0.25 0",
                "M  V30 60 Cl 2.8 1.75 0.25 0 AAMAP=42",
                "M  V30 END ATOM",
                "M  V30 BEGIN BOND",
                "M  V30 101 9 10 30",
                "M  V30 205 3 30 20",
                "M  V30 150 4 30 80",
                "M  V30 175 1 80 60 CFG=3",
                "M  V30 END BOND",
                "M  V30 END CTAB",
                "M  END"
            });
    }

    static string BuildInvalidCountsV3000Molfile()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "Invalid counts",
                "",
                "",
                "  0  0  0  0  0  0            999 V3000",
                "M  V30 BEGIN CTAB",
                "M  V30 COUNTS one 0 0 0 0",
                "M  V30 BEGIN ATOM",
                "M  V30 END ATOM",
                "M  V30 END CTAB",
                "M  END"
            });
    }

    static string BuildInvalidAtomCoordinateV3000Molfile()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "Invalid atom coordinate",
                "",
                "",
                "  0  0  0  0  0  0            999 V3000",
                "M  V30 BEGIN CTAB",
                "M  V30 COUNTS 1 0 0 0 0",
                "M  V30 BEGIN ATOM",
                "M  V30 1 C not-a-number 0.0 0.0 0",
                "M  V30 END ATOM",
                "M  V30 END CTAB",
                "M  END"
            });
    }

    static string BuildIncompleteAtomLineV3000Molfile()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "Incomplete atom line",
                "",
                "",
                "  0  0  0  0  0  0            999 V3000",
                "M  V30 BEGIN CTAB",
                "M  V30 COUNTS 1 0 0 0 0",
                "M  V30 BEGIN ATOM",
                "M  V30 1 C 0.0",
                "M  V30 END ATOM",
                "M  V30 END CTAB",
                "M  END"
            });
    }

    static string BuildLargeV3000Molfile(int atomCount)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("Large V3000\n");
        builder.Append("\n");
        builder.Append("\n");
        builder.Append("  0  0  0  0  0  0            999 V3000\n");
        builder.Append("M  V30 BEGIN CTAB\n");
        builder.Append("M  V30 COUNTS ");
        builder.Append(atomCount);
        builder.Append(" 0 0 0 0\n");
        builder.Append("M  V30 BEGIN ATOM\n");
        int index = 1;
        while (index <= atomCount)
        {
            builder.Append("M  V30 ");
            builder.Append(index);
            builder.Append(" C ");
            builder.Append(index);
            builder.Append(".0 0.0 0.0 0\n");
            index = index + 1;
        }

        builder.Append("M  V30 END ATOM\n");
        builder.Append("M  V30 END CTAB\n");
        builder.Append("M  END\n");
        return builder.ToString();
    }

    static string BuildMissingCountsV3000Molfile()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "Missing counts",
                "",
                "",
                "  0  0  0  0  0  0            999 V3000",
                "M  V30 BEGIN CTAB",
                "M  V30 BEGIN ATOM",
                "M  V30 END ATOM",
                "M  V30 END CTAB",
                "M  END"
            });
    }

    static string BuildInvalidBondV3000Molfile()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "Invalid bond",
                "",
                "",
                "  0  0  0  0  0  0            999 V3000",
                "M  V30 BEGIN CTAB",
                "M  V30 COUNTS 1 1 0 0 0",
                "M  V30 BEGIN ATOM",
                "M  V30 1 C 0.0 0.0 0.0 0",
                "M  V30 END ATOM",
                "M  V30 BEGIN BOND",
                "M  V30 1 1 1 2",
                "M  V30 END BOND",
                "M  V30 END CTAB",
                "M  END"
            });
    }
}
