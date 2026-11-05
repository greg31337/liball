using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class MolfileSdfOptionsTests
{
    [Test]
    public void V2000Options_RoundTripAtomListsAliasesAndCommonProperties()
    {
        Molecule molecule = BuildQueryMolecule();
        MolfileWriteOptions writeOptions = new MolfileWriteOptions();
        writeOptions.Version = MolfileVersion.V2000;
        MolfileWriteResult written = new MolfileWriter().WriteMolfileWithOptions(molecule, writeOptions);

        MolfileReadResult read = new MolfileReader().ReadMolfileWithOptions(written.Text, null);

        Assert.That(written.Success, Is.True);
        Assert.That(written.Version, Is.EqualTo(MolfileVersion.V2000));
        Assert.That(written.Text, Does.Contain("M  ALS"));
        Assert.That(written.Text, Does.Contain("A    1\nquery alias\n"));
        Assert.That(written.Text, Does.Contain("M  CHG"));
        Assert.That(read.Success, Is.True);
        Assert.That(read.Version, Is.EqualTo(MolfileVersion.V2000));
        Assert.That(read.DeclaredAtomCount, Is.EqualTo(1));
        Assert.That(read.AtomListCount, Is.EqualTo(1));
        Assert.That(read.AliasCount, Is.EqualTo(1));
        Assert.That(read.PropertyRecordCount, Is.EqualTo(3));
        Assert.That(read.Molecule.Atoms[0].QueryOperation, Is.EqualTo(QueryOperation.NotInList));
        Assert.That(read.Molecule.Atoms[0].QueryValue, Is.EqualTo("C,N"));
        Assert.That(read.Molecule.Atoms[0].Alias, Is.EqualTo("query alias"));
        Assert.That(read.Molecule.Atoms[0].FormalCharge, Is.EqualTo(-1));
        Assert.That(read.Molecule.Atoms[0].IsotopeMassNumber, Is.EqualTo(15));
        Assert.That(read.Molecule.Atoms[0].RadicalElectronCount, Is.EqualTo(1));
    }

    [Test]
    public void MolfileReadOptions_CountButDoNotApplyDisabledFeatures()
    {
        MolfileWriteOptions writeOptions = new MolfileWriteOptions();
        writeOptions.Version = MolfileVersion.V2000;
        string text = new MolfileWriter().WriteMolfileWithOptions(BuildQueryMolecule(), writeOptions).Text;
        MolfileReadOptions readOptions = new MolfileReadOptions();
        readOptions.ReadAtomLists = false;
        readOptions.ReadAliases = false;
        readOptions.ReadCommonProperties = false;
        writeOptions.WriteAtomLists = false;
        writeOptions.WriteAliases = false;
        writeOptions.WriteCommonProperties = false;

        MolfileReadResult result = new MolfileReader().ReadMolfileWithOptions(text, readOptions);
        string plain = new V2000Writer().WriteV2000WithOptions(BuildQueryMolecule(), writeOptions).Text;

        Assert.That(result.Success, Is.True);
        Assert.That(result.AtomListCount, Is.EqualTo(1));
        Assert.That(result.AliasCount, Is.EqualTo(1));
        Assert.That(result.PropertyRecordCount, Is.EqualTo(3));
        Assert.That(result.Molecule.Atoms[0].QueryOperation, Is.EqualTo(QueryOperation.None));
        Assert.That(result.Molecule.Atoms[0].Alias, Is.Empty);
        Assert.That(result.Molecule.Atoms[0].FormalCharge, Is.Zero);
        Assert.That(result.Molecule.Atoms[0].IsotopeMassNumber, Is.Zero);
        Assert.That(result.Molecule.Atoms[0].RadicalElectronCount, Is.Zero);
        Assert.That(plain, Does.Not.Contain("M  ALS"));
        Assert.That(plain, Does.Not.Contain("A    1"));
        Assert.That(plain, Does.Not.Contain("M  CHG"));
        Assert.That(plain, Does.Not.Contain("M  ISO"));
        Assert.That(plain, Does.Not.Contain("M  RAD"));
    }

    [Test]
    public void V3000Options_RoundTripAtomListsAliasesAndFeaturePolicies()
    {
        MolfileWriteOptions options = new MolfileWriteOptions();
        options.Version = MolfileVersion.V3000;
        MolfileWriteResult written = new MolfileWriter().WriteMolfileWithOptions(BuildQueryMolecule(), options);

        MolfileReadResult read = new MolfileReader().ReadMolfileWithOptions(written.Text, null);
        options.WriteAtomLists = false;
        options.WriteAliases = false;
        options.WriteCommonProperties = false;
        string plain = new V3000Writer().WriteV3000WithOptions(BuildQueryMolecule(), options).Text;

        Assert.That(written.Success, Is.True);
        Assert.That(written.Version, Is.EqualTo(MolfileVersion.V3000));
        Assert.That(written.Text, Does.Contain("NOT[C,N]"));
        Assert.That(written.Text, Does.Contain("LABEL=\"query alias\""));
        Assert.That(read.Success, Is.True);
        Assert.That(read.AtomListCount, Is.EqualTo(1));
        Assert.That(read.AliasCount, Is.EqualTo(1));
        Assert.That(read.PropertyRecordCount, Is.EqualTo(3));
        Assert.That(read.Molecule.Atoms[0].QueryOperation, Is.EqualTo(QueryOperation.NotInList));
        Assert.That(read.Molecule.Atoms[0].Alias, Is.EqualTo("query alias"));
        Assert.That(plain, Does.Not.Contain("NOT["));
        Assert.That(plain, Does.Not.Contain("LABEL="));
        Assert.That(plain, Does.Not.Contain("CHG="));
        Assert.That(plain, Does.Not.Contain("MASS="));
        Assert.That(plain, Does.Not.Contain("RAD="));
    }

    [Test]
    public void V3000ReadOptions_CountAndControlEnhancedStereoCollections()
    {
        string text = "stereo\n\n\n"
            + "  0  0  0  0  0  0            999 V3000\n"
            + "M  V30 BEGIN CTAB\n"
            + "M  V30 COUNTS 1 0 0 0 1\n"
            + "M  V30 BEGIN ATOM\n"
            + "M  V30 1 C 0 0 0 0 CFG=1\n"
            + "M  V30 END ATOM\n"
            + "M  V30 BEGIN COLLECTION\n"
            + "M  V30 MDLV30/STEABS ATOMS=(1 1)\n"
            + "M  V30 END COLLECTION\n"
            + "M  V30 END CTAB\n"
            + "M  END\n";
        MolfileReadOptions ignoredOptions = new MolfileReadOptions();
        ignoredOptions.ReadEnhancedStereo = false;

        MolfileReadResult preserved = new V3000Reader().ReadV3000WithOptions(text, null);
        MolfileReadResult ignored = new V3000Reader().ReadV3000WithOptions(text, ignoredOptions);
        MolfileWriteOptions writeOptions = new MolfileWriteOptions();
        writeOptions.Version = MolfileVersion.V3000;
        writeOptions.WriteEnhancedStereo = false;
        string writtenWithoutCollections = new V3000Writer().WriteV3000WithOptions(preserved.Molecule, writeOptions).Text;

        Assert.That(preserved.Success, Is.True);
        Assert.That(preserved.StereoCollectionCount, Is.EqualTo(1));
        Assert.That(preserved.Molecule.StereoGroups.Groups, Has.Count.EqualTo(1));
        Assert.That(ignored.Success, Is.True);
        Assert.That(ignored.StereoCollectionCount, Is.EqualTo(1));
        Assert.That(ignored.Molecule.StereoGroups.Groups, Is.Empty);
        Assert.That(writtenWithoutCollections, Does.Not.Contain("BEGIN COLLECTION"));
    }

    [Test]
    public void MolfileOptions_SelectV3000ForLargeStructuresAndEnforceLimits()
    {
        Molecule molecule = BuildLargeMolecule(1000);
        MolfileWriter writer = new MolfileWriter();

        MolfileWriteResult automatic = writer.WriteMolfileWithOptions(molecule, null);
        MolfileWriteOptions forcedOptions = new MolfileWriteOptions();
        forcedOptions.Version = MolfileVersion.V2000;
        MolfileWriteResult forced = writer.WriteMolfileWithOptions(molecule, forcedOptions);
        MolfileReadOptions limitedOptions = new MolfileReadOptions();
        limitedOptions.MaximumAtomCount = 999;
        MolfileReadResult limited = new MolfileReader().ReadMolfileWithOptions(automatic.Text, limitedOptions);

        Assert.That(automatic.Success, Is.True);
        Assert.That(automatic.Version, Is.EqualTo(MolfileVersion.V3000));
        Assert.That(automatic.Text, Does.Contain("M  V30 COUNTS 1000 0"));
        Assert.That(forced.Success, Is.False);
        Assert.That(forced.Text, Is.Empty);
        Assert.That(forced.Report.Errors[0].Code, Is.EqualTo("MOLFILE_V2000_COUNT_LIMIT"));
        Assert.That(limited.Success, Is.False);
        Assert.That(limited.Version, Is.EqualTo(MolfileVersion.V3000));
        Assert.That(limited.Report.Errors[0].Code, Is.EqualTo("MOLFILE_COUNT_LIMIT"));
    }

    [Test]
    public void SdfReadOptions_ReportMixedVersionsAndDataFields()
    {
        string text = BuildMixedVersionSdf();
        SdfReader reader = new SdfReader();

        SdfReadResult allowed = reader.ReadSdfWithOptions(text, null);
        SdfReadOptions disallowedOptions = new SdfReadOptions();
        disallowedOptions.AllowMixedMolfileVersions = false;
        SdfReadResult disallowed = reader.ReadSdfWithOptions(text, disallowedOptions);

        Assert.That(allowed.Success, Is.True);
        Assert.That(allowed.ReadCount, Is.EqualTo(2));
        Assert.That(allowed.FailedCount, Is.Zero);
        Assert.That(allowed.MixedMolfileVersions, Is.True);
        Assert.That(allowed.Records[0].Version, Is.EqualTo(MolfileVersion.V2000));
        Assert.That(allowed.Records[1].Version, Is.EqualTo(MolfileVersion.V3000));
        Assert.That(allowed.Records[0].DataFieldCount, Is.EqualTo(1));
        Assert.That(allowed.Records[0].Molecule.Metadata.GetSdfField("ID"), Is.EqualTo("v2"));
        Assert.That(disallowed.Success, Is.False);
        Assert.That(disallowed.FailedCount, Is.EqualTo(1));
        Assert.That(disallowed.Records[1].Report.Errors[0].Code, Is.EqualTo("SDF_MIXED_MOLFILE_VERSIONS"));
    }

    [Test]
    public void SdfReadOptions_ControlFailedRecordRecoveryAndRetention()
    {
        string text = "bad\n$$$$\n" + BuildSdfRecord(BuildSimpleMolecule("good"), MolfileVersion.V2000, "ok");
        SdfReadOptions stopOptions = new SdfReadOptions();
        stopOptions.ContinueAfterError = false;
        stopOptions.IncludeFailedRecords = false;
        SdfReadOptions continueOptions = new SdfReadOptions();
        continueOptions.IncludeFailedRecords = false;
        continueOptions.ReadDataFields = false;

        SdfReadResult stopped = new SdfReader().ReadSdfWithOptions(text, stopOptions);
        SdfReadResult continued = new SdfReader().ReadSdfWithOptions(text, continueOptions);

        Assert.That(stopped.Success, Is.False);
        Assert.That(stopped.ReadCount, Is.EqualTo(1));
        Assert.That(stopped.FailedCount, Is.EqualTo(1));
        Assert.That(stopped.Records, Is.Empty);
        Assert.That(continued.Success, Is.False);
        Assert.That(continued.ReadCount, Is.EqualTo(2));
        Assert.That(continued.FailedCount, Is.EqualTo(1));
        Assert.That(continued.Records, Has.Count.EqualTo(1));
        Assert.That(continued.Records[0].Molecule.Metadata.GetPrimaryName(), Is.EqualTo("good"));
        Assert.That(continued.Records[0].DataFieldCount, Is.Zero);
        Assert.That(continued.Records[0].Molecule.Metadata.SdfFields, Is.Empty);
    }

    [Test]
    public void SdfWriteOptions_ControlEmbeddedVersionFieldsAndSeparators()
    {
        SdfRecord record = new SdfRecord();
        record.Molecule = BuildSimpleMolecule("configured");
        record.Molecule.Metadata.AddSdfField("ID", "42");
        List<SdfRecord> records = new List<SdfRecord>();
        records.Add(record);
        SdfWriteOptions options = new SdfWriteOptions();
        options.Molfile.Version = MolfileVersion.V3000;
        options.WriteDataFields = false;
        options.WriteRecordSeparator = false;

        SdfWriteResult result = new SdfWriter().WriteSdfWithOptions(records, options);
        SdfWriteResult failed = new SdfWriter().WriteSdfWithOptions(null, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.WrittenCount, Is.EqualTo(1));
        Assert.That(result.Text, Does.Contain("V3000"));
        Assert.That(result.Text, Does.Not.Contain(">  <ID>"));
        Assert.That(result.Text, Does.Not.Contain("$$$$"));
        Assert.That(failed.Success, Is.False);
        Assert.That(failed.Report.Errors[0].Code, Is.EqualTo("SDF_NULL_RECORDS"));
    }

    [Test]
    public void MoleculeFormats_AppliesMolfileAndSdfOptions()
    {
        MoleculeFormats formats = new MoleculeFormats();
        FormatOptions molOptions = new FormatOptions();
        molOptions.KindHint = FormatKind.MolV3000;
        molOptions.MolfileWrite.WriteAliases = false;
        FormatOptions sdfOptions = new FormatOptions();
        sdfOptions.KindHint = FormatKind.Sdf;
        sdfOptions.SdfWrite.WriteDataFields = false;
        sdfOptions.SdfWrite.WriteRecordSeparator = false;
        Molecule molecule = BuildQueryMolecule();
        molecule.Metadata.AddSdfField("ID", "value");

        string molfile = formats.WriteString(molecule, molOptions);
        string sdf = formats.WriteString(molecule, sdfOptions);

        Assert.That(molfile, Does.Contain("V3000"));
        Assert.That(molfile, Does.Not.Contain("LABEL="));
        Assert.That(sdf, Does.Contain("M  END"));
        Assert.That(sdf, Does.Not.Contain(">  <ID>"));
        Assert.That(sdf, Does.Not.Contain("$$$$"));
    }

    [Test]
    public void MoleculeFormatsAndStreamReader_ApplyMolfileAndSdfReadOptions()
    {
        MoleculeFormats formats = new MoleculeFormats();
        MolfileWriteOptions molfileWriteOptions = new MolfileWriteOptions();
        molfileWriteOptions.Version = MolfileVersion.V2000;
        string molfile = new MolfileWriter().WriteMolfileWithOptions(BuildQueryMolecule(), molfileWriteOptions).Text;
        FormatOptions molfileOptions = new FormatOptions();
        molfileOptions.KindHint = FormatKind.MolV2000;
        molfileOptions.MolfileRead.ReadAliases = false;
        string sdf = BuildSdfRecord(BuildSimpleMolecule("read options"), MolfileVersion.V2000, "field");
        FormatOptions sdfOptions = new FormatOptions();
        sdfOptions.KindHint = FormatKind.Sdf;
        sdfOptions.SdfRead.ReadDataFields = false;

        Molecule readMolfile = formats.ReadString(molfile, molfileOptions);
        Molecule readSdf = formats.ReadString(sdf, sdfOptions);
        MoleculeStreamReadResult streamedSdf = formats.OpenReader(new StringReader(sdf), sdfOptions).ReadNext();

        Assert.That(readMolfile.Atoms[0].Alias, Is.Empty);
        Assert.That(readSdf.Metadata.SdfFields, Is.Empty);
        Assert.That(streamedSdf.Success, Is.True);
        Assert.That(streamedSdf.Molecule.Metadata.SdfFields, Is.Empty);
    }

    static Molecule BuildQueryMolecule()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int atomId = builder.AddAtom(new AtomSpec(7, 15, -1, 0, 0, 1, 0, false, "query alias", "N"));
        Molecule molecule = builder.Build();
        molecule.Atoms[atomId].QueryOperation = QueryOperation.NotInList;
        molecule.Atoms[atomId].QueryValue = "C,N";
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

    static Molecule BuildSimpleMolecule(string name)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        Molecule molecule = builder.Build();
        molecule.Metadata.AddName(name);
        return molecule;
    }

    static string BuildMixedVersionSdf()
    {
        return BuildSdfRecord(BuildSimpleMolecule("v2"), MolfileVersion.V2000, "v2")
            + BuildSdfRecord(BuildSimpleMolecule("v3"), MolfileVersion.V3000, "v3");
    }

    static string BuildSdfRecord(Molecule molecule, MolfileVersion version, string id)
    {
        MolfileWriteOptions options = new MolfileWriteOptions();
        options.Version = version;
        string molfile = new MolfileWriter().WriteMolfileWithOptions(molecule, options).Text;
        return molfile + ">  <ID>\n" + id + "\n\n$$$$\n";
    }
}
