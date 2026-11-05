using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class SdfWriterTests
{
    [Test]
    public void Write_EmitsMolfileDataFieldsAndRecordSeparator()
    {
        SdfWriter writer = new SdfWriter();

        string sdf = writer.Write(BuildMolecule("First molecule", "123"));

        Assert.That(sdf, Does.StartWith("First molecule\n"));
        Assert.That(sdf, Does.Contain("M  END\n"));
        Assert.That(sdf, Does.Contain(">  <PUBCHEM_ID>\n123\n\n"));
        Assert.That(sdf, Does.Contain(">  <NOTES>\nline one\nline two\n\n"));
        Assert.That(sdf, Does.EndWith("$$$$\n"));
    }

    [Test]
    public void WriteMolecules_RoundTripsThroughSdfReader()
    {
        SdfWriter writer = new SdfWriter();
        SdfReader reader = new SdfReader();
        List<Molecule> molecules = new List<Molecule>();
        molecules.Add(BuildMolecule("First molecule", "123"));
        molecules.Add(BuildMolecule("Second molecule", "456"));

        string sdf = writer.WriteMolecules(molecules);
        List<SdfRecord> records = reader.ReadRecords(sdf);

        Assert.That(records.Count, Is.EqualTo(2));
        Assert.That(records[0].Success, Is.True);
        Assert.That(records[1].Success, Is.True);
        Assert.That(records[0].HasRecordSeparator, Is.True);
        Assert.That(records[1].HasRecordSeparator, Is.True);
        Assert.That(records[0].Molecule.Metadata.GetPrimaryName(), Is.EqualTo("First molecule"));
        Assert.That(records[0].Molecule.Metadata.GetSdfField("PUBCHEM_ID"), Is.EqualTo("123"));
        Assert.That(records[0].Molecule.Metadata.GetSdfField("NOTES"), Is.EqualTo("line one\nline two"));
        Assert.That(records[1].Molecule.Metadata.GetSdfField("PUBCHEM_ID"), Is.EqualTo("456"));
    }

    [Test]
    public void WriteRecord_StreamsRecordsToTextWriter()
    {
        SdfWriter writer = new SdfWriter();
        SdfReader reader = new SdfReader();
        StringWriter textWriter = new StringWriter();

        writer.WriteMoleculeRecord(textWriter, BuildMolecule("First molecule", "123"));
        writer.WriteMoleculeRecord(textWriter, BuildMolecule("Second molecule", "456"));

        List<SdfRecord> records = reader.ReadRecords(textWriter.ToString());
        Assert.That(records.Count, Is.EqualTo(2));
        Assert.That(records[1].Molecule.Metadata.GetPrimaryName(), Is.EqualTo("Second molecule"));
    }

    [Test]
    public void WriteRecords_WritesSdfRecordMolecules()
    {
        SdfWriter writer = new SdfWriter();
        SdfRecord record = new SdfRecord();
        record.Molecule = BuildMolecule("Record molecule", "789");
        List<SdfRecord> records = new List<SdfRecord>();
        records.Add(record);

        string sdf = writer.WriteRecords(records);

        Assert.That(sdf, Does.Contain("Record molecule"));
        Assert.That(sdf, Does.Contain(">  <PUBCHEM_ID>\n789\n\n"));
    }

    [Test]
    public void Write_NormalizesFieldNamesAndNullMolecules()
    {
        SdfWriter writer = new SdfWriter();
        Molecule molecule = BuildMolecule("Normalized", "1");
        molecule.Metadata.AddSdfField("BAD<KEY>\n", "value");

        string sdf = writer.Write(molecule);

        Assert.That(writer.Write(null), Is.EqualTo(""));
        Assert.That(sdf, Does.Contain(">  <BAD_KEY_ >\nvalue\n\n"));
    }

    [Test]
    public void WriteSdfWithOptions_ReportsNullCollectionAndNullRecord()
    {
        SdfWriter writer = new SdfWriter();
        SdfWriteResult nullCollection = writer.WriteSdfWithOptions(null, null);
        List<SdfRecord> records = new List<SdfRecord>();
        records.Add(null!);

        SdfWriteResult nullRecord = writer.WriteSdfWithOptions(records, null);

        Assert.That(nullCollection.Success, Is.False);
        Assert.That(nullCollection.Report.Get(0).Code, Is.EqualTo("SDF_NULL_RECORDS"));
        Assert.That(nullRecord.Success, Is.False);
        Assert.That(nullRecord.WrittenCount, Is.Zero);
        Assert.That(nullRecord.Report.Get(0).Code, Is.EqualTo("SDF_NULL_RECORD"));
    }

    [Test]
    public void StreamingWriterEntryPoints_IgnoreNullEndpointsAndValues()
    {
        SdfWriter writer = new SdfWriter();
        StringWriter output = new StringWriter();

        Assert.DoesNotThrow(() => writer.WriteRecordsToWriter(null, null));
        Assert.DoesNotThrow(() => writer.WriteRecord(output, null));
        Assert.DoesNotThrow(() => writer.WriteMoleculeRecord(output, null));
        Assert.That(output.ToString(), Is.Empty);
    }

    static Molecule BuildMolecule(string name, string id)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        Molecule molecule = builder.Build();
        molecule.Metadata.AddName(name);
        molecule.Metadata.AddSdfField("PUBCHEM_ID", id);
        molecule.Metadata.AddSdfField("NOTES", "line one\nline two");
        molecule.Atoms[carbon].HasCoordinates = true;
        molecule.Atoms[carbon].X = 1.0;
        return molecule;
    }
}
