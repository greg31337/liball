using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class SdfReaderTests
{
    [Test]
    public void ReadRecords_ReadsMultipleRecordsDataFieldsAndSeparators()
    {
        SdfReader reader = new SdfReader();

        List<SdfRecord> records = reader.ReadRecords(BuildTwoRecordSdf());

        Assert.That(records.Count, Is.EqualTo(2));
        Assert.That(records[0].Index, Is.EqualTo(0));
        Assert.That(records[1].Index, Is.EqualTo(1));
        Assert.That(records[0].Success, Is.True);
        Assert.That(records[1].Success, Is.True);
        Assert.That(records[0].HasRecordSeparator, Is.True);
        Assert.That(records[1].HasRecordSeparator, Is.True);
        Assert.That(records[0].Molecule.Metadata.GetPrimaryName(), Is.EqualTo("First molecule"));
        Assert.That(records[0].Molecule.Metadata.GetSdfField("PUBCHEM_ID"), Is.EqualTo("123"));
        Assert.That(records[0].Molecule.Metadata.GetSdfField("NOTES"), Is.EqualTo("line one\nline two"));
        Assert.That(records[1].Molecule.Metadata.GetPrimaryName(), Is.EqualTo("Second molecule"));
        Assert.That(records[1].Molecule.Metadata.GetSdfField("PUBCHEM_ID"), Is.EqualTo("456"));
    }

    [Test]
    public void ReadRecord_StreamsOneRecordAtATimeFromTextReader()
    {
        SdfReader reader = new SdfReader();
        StringReader textReader = new StringReader(BuildTwoRecordSdf());

        SdfRecord first = reader.ReadRecord(textReader);
        SdfRecord second = reader.ReadRecord(textReader);
        SdfRecord end = reader.ReadRecord(textReader);

        Assert.That(first.EndOfStream, Is.False);
        Assert.That(second.EndOfStream, Is.False);
        Assert.That(end.EndOfStream, Is.True);
        Assert.That(first.Molecule.Metadata.GetPrimaryName(), Is.EqualTo("First molecule"));
        Assert.That(second.Molecule.Metadata.GetPrimaryName(), Is.EqualTo("Second molecule"));
    }

    [Test]
    public void ReadRecords_ReadsFinalRecordWithoutSeparator()
    {
        SdfReader reader = new SdfReader();

        List<SdfRecord> records = reader.ReadRecords(BuildRecord("No separator", "ID", "1"));

        Assert.That(records.Count, Is.EqualTo(1));
        Assert.That(records[0].Success, Is.True);
        Assert.That(records[0].HasRecordSeparator, Is.False);
        Assert.That(records[0].Molecule.Metadata.GetSdfField("ID"), Is.EqualTo("1"));
    }

    [Test]
    public void ReadRecords_KeepsPerRecordDiagnostics()
    {
        SdfReader reader = new SdfReader();
        string text = BuildRecord("Good", "ID", "1") + "$$$$\n" + "bad\n$$$$\n";

        List<SdfRecord> records = reader.ReadRecords(text);

        Assert.That(records.Count, Is.EqualTo(2));
        Assert.That(records[0].Success, Is.True);
        Assert.That(records[0].Report.GetErrorCount(), Is.EqualTo(0));
        Assert.That(records[1].Success, Is.False);
        Assert.That(records[1].Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(records[1].Report.Get(0).Code, Is.EqualTo("MOLFILE_COUNTS_MISSING"));
    }

    [Test]
    public void ReadRecords_WarnsForMalformedDataHeaderButKeepsMetadata()
    {
        SdfReader reader = new SdfReader();
        string text = BuildMolfile("Malformed data") + "> BAD_HEADER\nvalue\n\n$$$$\n";

        List<SdfRecord> records = reader.ReadRecords(text);

        Assert.That(records.Count, Is.EqualTo(1));
        Assert.That(records[0].Success, Is.True);
        Assert.That(records[0].Report.GetWarningCount(), Is.EqualTo(1));
        Assert.That(records[0].Report.Get(0).Code, Is.EqualTo("SDF_DATA_HEADER_INVALID"));
        Assert.That(records[0].Molecule.Metadata.SdfFields[0].Value, Is.EqualTo("value"));
    }

    [Test]
    public void ReadAll_ReturnsMoleculesFromRecords()
    {
        SdfReader reader = new SdfReader();

        List<Molecule> molecules = reader.ReadAll(BuildTwoRecordSdf());

        Assert.That(molecules.Count, Is.EqualTo(2));
        Assert.That(molecules[0].Metadata.GetPrimaryName(), Is.EqualTo("First molecule"));
        Assert.That(molecules[1].Metadata.GetPrimaryName(), Is.EqualTo("Second molecule"));
    }

    [Test]
    public void ReaderEntryPoints_NormalizeNullAndEmptyStreams()
    {
        SdfReader reader = new SdfReader();

        List<SdfRecord> fromText = reader.ReadRecords(null);
        List<SdfRecord> fromReader = reader.ReadRecordsFromReader(null);
        SdfRecord record = reader.ReadRecord(null);

        Assert.That(fromText, Is.Empty);
        Assert.That(fromReader, Is.Empty);
        Assert.That(record.EndOfStream, Is.True);
        Assert.That(record.Success, Is.False);
        Assert.That(reader.Read(null).GetAtomCount(), Is.Zero);
    }

    static string BuildTwoRecordSdf()
    {
        return BuildRecordWithTwoFields("First molecule", "PUBCHEM_ID", "123", "NOTES", "line one\nline two")
            + "$$$$\n"
            + BuildRecord("Second molecule", "PUBCHEM_ID", "456")
            + "$$$$\n";
    }

    static string BuildRecord(string name, string key, string value)
    {
        return BuildMolfile(name)
            + ">  <" + key + ">\n"
            + value + "\n"
            + "\n";
    }

    static string BuildRecordWithTwoFields(string name, string keyA, string valueA, string keyB, string valueB)
    {
        return BuildMolfile(name)
            + ">  <" + keyA + ">\n"
            + valueA + "\n"
            + "\n"
            + ">  <" + keyB + ">\n"
            + valueB + "\n"
            + "\n";
    }

    static string BuildMolfile(string name)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        Molecule molecule = builder.Build();
        molecule.Metadata.AddName(name);
        molecule.Atoms[carbon].HasCoordinates = true;
        V2000Writer writer = new V2000Writer();
        return writer.Write(molecule);
    }
}
