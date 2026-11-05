using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class DuplicateDetectorStreamTests
{
    [Test]
    public void AddString_ParsesFormatOptionsAndDetectsDuplicate()
    {
        DuplicateDetector detector = new DuplicateDetector();
        FormatOptions options = new FormatOptions();
        options.KindHint = FormatKind.Smiles;

        DuplicateResult first = detector.AddString("C", options);
        DuplicateResult second = detector.AddString("C", options);

        Assert.That(first.Success, Is.True);
        Assert.That(second.Success, Is.True);
        Assert.That(first.IsDuplicate, Is.False);
        Assert.That(second.IsDuplicate, Is.True);
        Assert.That(second.FirstIndex, Is.EqualTo(0));
        Assert.That(detector.GetEntryCount(), Is.EqualTo(2));
    }

    [Test]
    public void AddSdfRecords_DetectsDuplicatesAndPreservesRecordIndex()
    {
        DuplicateDetector detector = new DuplicateDetector();
        SdfReader reader = new SdfReader();
        List<SdfRecord> records = reader.ReadRecords(BuildTwoRecordSdf("First", "Second"));

        List<DuplicateResult> results = detector.AddSdfRecords(records);

        Assert.That(results.Count, Is.EqualTo(2));
        Assert.That(results[0].Success, Is.True);
        Assert.That(results[0].IsDuplicate, Is.False);
        Assert.That(results[0].RecordIndex, Is.EqualTo(0));
        Assert.That(results[1].Success, Is.True);
        Assert.That(results[1].IsDuplicate, Is.True);
        Assert.That(results[1].FirstIndex, Is.EqualTo(0));
        Assert.That(results[1].RecordIndex, Is.EqualTo(1));
    }

    [Test]
    public void AddSdfText_ReturnsFailureForInvalidRecordsWithoutAddingThem()
    {
        DuplicateDetector detector = new DuplicateDetector();
        string text = BuildRecord("Good") + "$$$$\n" + "bad\n$$$$\n";

        List<DuplicateResult> results = detector.AddSdfText(text);

        Assert.That(results.Count, Is.EqualTo(2));
        Assert.That(results[0].Success, Is.True);
        Assert.That(results[1].Success, Is.False);
        Assert.That(results[1].Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(detector.GetEntryCount(), Is.EqualTo(1));
    }

    [Test]
    public void AddMany_ReadsMoleculeStreamReaderAndDetectsDuplicates()
    {
        DuplicateDetector detector = new DuplicateDetector();
        FormatOptions options = new FormatOptions();
        options.KindHint = FormatKind.Sdf;
        MoleculeStreamReader reader = MoleculeStreamReader.Create(new StringReader(BuildTwoRecordSdf("First", "Second")), options);

        List<DuplicateResult> results = detector.AddManyFromReader(reader);

        Assert.That(results.Count, Is.EqualTo(2));
        Assert.That(results[0].Success, Is.True);
        Assert.That(results[1].Success, Is.True);
        Assert.That(results[1].IsDuplicate, Is.True);
        Assert.That(results[1].RecordIndex, Is.EqualTo(1));
        Assert.That(detector.GetDuplicateGroups().Count, Is.EqualTo(1));
    }

    [Test]
    public void AddSdfRecord_ReportsNullRecordWithoutAddingEntry()
    {
        DuplicateDetector detector = new DuplicateDetector();

        DuplicateResult result = detector.AddSdfRecord(null);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("DUPLICATE_DETECTOR_NULL_SDF_RECORD"));
        Assert.That(detector.GetEntryCount(), Is.EqualTo(0));
    }

    [Test]
    public void CollectionAndStreamEntryPoints_ReturnEmptyForNullInputs()
    {
        DuplicateDetector detector = new DuplicateDetector();

        List<DuplicateResult> molecules = detector.AddMany(null);
        List<DuplicateResult> reader = detector.AddManyFromReader(null);
        List<DuplicateResult> records = detector.AddSdfRecords(null);
        List<DuplicateResult> text = detector.AddSdfText(null);

        Assert.That(molecules, Is.Empty);
        Assert.That(reader, Is.Empty);
        Assert.That(records, Is.Empty);
        Assert.That(text, Is.Empty);
        Assert.That(detector.GetEntryCount(), Is.Zero);
    }

    [Test]
    public void AddSdfRecord_AddsDedicatedDiagnosticWhenInvalidRecordHasNoReport()
    {
        DuplicateDetector detector = new DuplicateDetector();
        SdfRecord record = new SdfRecord();
        record.Index = 12;
        record.Success = false;

        DuplicateResult result = detector.AddSdfRecord(record);

        Assert.That(result.Success, Is.False);
        Assert.That(result.RecordIndex, Is.EqualTo(12));
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("DUPLICATE_DETECTOR_INVALID_SDF_RECORD"));
        Assert.That(detector.GetEntryCount(), Is.Zero);
    }

    static string BuildTwoRecordSdf(string firstName, string secondName)
    {
        return BuildRecord(firstName) + "$$$$\n" + BuildRecord(secondName) + "$$$$\n";
    }

    static string BuildRecord(string name)
    {
        SdfWriter writer = new SdfWriter();
        string sdf = writer.Write(BuildMolecule(name));
        return sdf.Substring(0, sdf.Length - 5);
    }

    static Molecule BuildMolecule(string name)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        Molecule molecule = builder.Build();
        molecule.Metadata.AddName(name);
        return molecule;
    }
}
