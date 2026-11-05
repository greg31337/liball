using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class BatchConversionTests
{
    [Test]
    public void MoleculeStreamReader_ReadsSdfRecordsFromTextReader()
    {
        FormatOptions options = new FormatOptions();
        options.KindHint = FormatKind.Sdf;
        MoleculeStreamReader reader = MoleculeStreamReader.Create(new StringReader(BuildTwoRecordSdf()), options);

        MoleculeStreamReadResult first = reader.ReadNext();
        MoleculeStreamReadResult second = reader.ReadNext();
        MoleculeStreamReadResult end = reader.ReadNext();

        Assert.That(first.EndOfStream, Is.False);
        Assert.That(second.EndOfStream, Is.False);
        Assert.That(end.EndOfStream, Is.True);
        Assert.That(first.Molecule.Metadata.GetPrimaryName(), Is.EqualTo("First"));
        Assert.That(second.Molecule.Metadata.GetPrimaryName(), Is.EqualTo("Second"));
    }

    [Test]
    public void MoleculeStreamReader_ReadsSingleSmilesRecordAndThenEnds()
    {
        FormatOptions options = new FormatOptions();
        options.KindHint = FormatKind.Smiles;
        MoleculeStreamReader reader = MoleculeStreamReader.Create(new StringReader("CO"), options);

        MoleculeStreamReadResult first = reader.ReadNext();
        MoleculeStreamReadResult end = reader.ReadNext();

        Assert.That(first.Success, Is.True);
        Assert.That(first.EndOfStream, Is.False);
        Assert.That(first.Molecule.GetAtomCount(), Is.EqualTo(2));
        Assert.That(first.Molecule.GetBondCount(), Is.EqualTo(1));
        Assert.That(first.Molecule.Atoms[0].AtomicNumber, Is.EqualTo(6));
        Assert.That(first.Molecule.Atoms[1].AtomicNumber, Is.EqualTo(8));
        Assert.That(end.EndOfStream, Is.True);
    }

    [Test]
    public void MoleculeStreamReader_UsesFileNameDetectionForSdfStreams()
    {
        FormatOptions options = new FormatOptions();
        options.FileName = "records.sdf";
        MoleculeStreamReader reader = MoleculeStreamReader.Create(new StringReader(BuildTwoRecordSdf()), options);

        List<Molecule> molecules = reader.ReadAll();

        Assert.That(molecules.Count, Is.EqualTo(2));
        Assert.That(molecules[0].Metadata.GetPrimaryName(), Is.EqualTo("First"));
        Assert.That(molecules[1].Metadata.GetPrimaryName(), Is.EqualTo("Second"));
    }

    [Test]
    public void MoleculeStreamWriter_WritesSdfRecordsToTextWriter()
    {
        FormatOptions options = new FormatOptions();
        options.KindHint = FormatKind.Sdf;
        StringWriter textWriter = new StringWriter();
        MoleculeStreamWriter writer = MoleculeStreamWriter.Create(textWriter, options);
        SdfReader reader = new SdfReader();

        writer.Write(BuildMolecule("First"));
        writer.Write(BuildMolecule("Second"));
        List<SdfRecord> records = reader.ReadRecords(textWriter.ToString());

        Assert.That(writer.WrittenCount, Is.EqualTo(2));
        Assert.That(records.Count, Is.EqualTo(2));
        Assert.That(records[0].Molecule.Metadata.GetPrimaryName(), Is.EqualTo("First"));
        Assert.That(records[1].Molecule.Metadata.GetPrimaryName(), Is.EqualTo("Second"));
    }

    [Test]
    public void MoleculeStreamWriter_DefaultsToSmilesAndOpenResetsState()
    {
        FormatOptions defaultOptions = new FormatOptions();
        StringWriter smilesText = new StringWriter();
        MoleculeStreamWriter writer = MoleculeStreamWriter.Create(smilesText, defaultOptions);

        writer.Write(BuildCarbonOxygenMolecule("Carbon oxygen"));

        Assert.That(writer.WrittenCount, Is.EqualTo(1));
        Assert.That(smilesText.ToString(), Is.EqualTo("CO\n"));

        FormatOptions sdfOptions = new FormatOptions();
        sdfOptions.KindHint = FormatKind.Sdf;
        StringWriter sdfText = new StringWriter();
        writer.Open(sdfText, sdfOptions);

        Assert.That(writer.WrittenCount, Is.EqualTo(0));

        writer.Write(BuildMolecule("Reopened"));
        List<SdfRecord> records = new SdfReader().ReadRecords(sdfText.ToString());

        Assert.That(writer.WrittenCount, Is.EqualTo(1));
        Assert.That(records.Count, Is.EqualTo(1));
        Assert.That(records[0].Molecule.Metadata.GetPrimaryName(), Is.EqualTo("Reopened"));
        Assert.That(smilesText.ToString(), Is.EqualTo("CO\n"));
    }

    [Test]
    public void MoleculeFormats_OpenReaderAndOpenWriterCreateStreams()
    {
        MoleculeFormats formats = new MoleculeFormats();
        FormatOptions input = new FormatOptions();
        input.KindHint = FormatKind.Sdf;
        FormatOptions output = new FormatOptions();
        output.KindHint = FormatKind.Smiles;
        StringWriter textWriter = new StringWriter();

        MoleculeStreamReader reader = formats.OpenReader(new StringReader(BuildTwoRecordSdf()), input);
        MoleculeStreamWriter writer = formats.OpenWriter(textWriter, output);
        writer.Write(reader.ReadNext().Molecule);

        Assert.That(textWriter.ToString(), Is.EqualTo("C\n"));
    }

    [Test]
    public void PublicStreamObjects_HandleInitialAndNullEndpointsDeterministically()
    {
        MoleculeStreamReader reader = new MoleculeStreamReader();
        MoleculeStreamReadResult initial = reader.ReadNext();

        reader.Open(null, null);
        MoleculeStreamReadResult empty = reader.ReadNext();
        MoleculeStreamReadResult end = reader.ReadNext();

        MoleculeStreamWriter writer = new MoleculeStreamWriter();
        writer.Open(null, null);
        Assert.DoesNotThrow(() => writer.Write(null));

        Assert.That(initial.EndOfStream, Is.True);
        Assert.That(empty.Success, Is.True);
        Assert.That(empty.Molecule.GetAtomCount(), Is.Zero);
        Assert.That(end.EndOfStream, Is.True);
        Assert.That(writer.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void BatchConverter_ConvertsSdfStreamToSmilesLines()
    {
        BatchConverter converter = new BatchConverter();
        FormatOptions input = new FormatOptions();
        input.KindHint = FormatKind.Sdf;
        FormatOptions output = new FormatOptions();
        output.KindHint = FormatKind.Smiles;
        StringWriter writer = new StringWriter();

        ConversionReport report = converter.Convert(new StringReader(BuildTwoRecordSdf()), writer, input, output);

        Assert.That(report.Success, Is.True);
        Assert.That(report.ReadCount, Is.EqualTo(2));
        Assert.That(report.WrittenCount, Is.EqualTo(2));
        Assert.That(writer.ToString(), Is.EqualTo("C\nC\n"));
    }

    [Test]
    public void BatchConverter_KeepsPerRecordErrorsAndSkipsInvalidRecords()
    {
        BatchConverter converter = new BatchConverter();
        FormatOptions input = new FormatOptions();
        input.KindHint = FormatKind.Sdf;
        FormatOptions output = new FormatOptions();
        output.KindHint = FormatKind.Smiles;
        StringWriter writer = new StringWriter();
        string text = BuildRecord("Good") + "$$$$\n" + "bad\n$$$$\n";

        ConversionReport report = converter.Convert(new StringReader(text), writer, input, output);

        Assert.That(report.Success, Is.False);
        Assert.That(report.ReadCount, Is.EqualTo(2));
        Assert.That(report.WrittenCount, Is.EqualTo(1));
        Assert.That(report.ErrorCount, Is.EqualTo(1));
        Assert.That(writer.ToString(), Is.EqualTo("C\n"));
    }

    [Test]
    public void BatchConverter_UsesInjectedFormatInterfaces()
    {
        MoleculeFormats formats = new MoleculeFormats();
        TrackingReader reader = new TrackingReader();
        TrackingWriter writer = new TrackingWriter();
        formats.RegisterReader(FormatKind.Smiles, reader);
        formats.RegisterWriter(FormatKind.Smiles, writer);
        BatchConverter converter = BatchConverter.Create(formats);
        FormatOptions input = new FormatOptions();
        input.KindHint = FormatKind.Smiles;
        FormatOptions output = new FormatOptions();
        output.KindHint = FormatKind.Smiles;
        StringWriter outputText = new StringWriter();

        ConversionReport report = converter.Convert(new StringReader("source"), outputText, input, output);

        Assert.That(report.Success, Is.True);
        Assert.That(report.ReadCount, Is.EqualTo(1));
        Assert.That(report.WrittenCount, Is.EqualTo(1));
        Assert.That(reader.ReadCount, Is.EqualTo(1));
        Assert.That(writer.WriteCount, Is.EqualTo(1));
        Assert.That(outputText.ToString(), Is.EqualTo("injected\n"));
    }

    [Test]
    public void BatchConverter_FiltersSuccessfulRecordsAndCountsEveryOutcome()
    {
        BatchConverter converter = new BatchConverter();
        FormatOptions input = new FormatOptions();
        input.KindHint = FormatKind.Sdf;
        FormatOptions output = new FormatOptions();
        output.KindHint = FormatKind.Smiles;
        NameBatchFilter filter = new NameBatchFilter();
        ProgressCollector progress = new ProgressCollector();
        BatchConversionOptions conversionOptions = new BatchConversionOptions();
        conversionOptions.Filter = filter;
        conversionOptions.Progress = progress;
        StringWriter writer = new StringWriter();
        string text = BuildRecord("Keep") + "> BAD_HEADER\nvalue\n\n$$$$\n"
            + BuildRecord("Skip") + "$$$$\n"
            + "bad\n$$$$\n";

        ConversionReport report = converter.ConvertWithOptions(new StringReader(text), writer, input, output, conversionOptions);

        Assert.That(report.Success, Is.False);
        Assert.That(report.ReadCount, Is.EqualTo(3));
        Assert.That(report.WrittenCount, Is.EqualTo(1));
        Assert.That(report.SkippedCount, Is.EqualTo(1));
        Assert.That(report.WarningCount, Is.EqualTo(1));
        Assert.That(report.FailureCount, Is.EqualTo(1));
        Assert.That(report.ErrorCount, Is.EqualTo(1));
        Assert.That(writer.ToString(), Is.EqualTo("C\n"));
        Assert.That(filter.Indices, Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(progress.Snapshots.Count, Is.EqualTo(4));
        AssertProgress(progress.Snapshots[0], 1, 1, 0, 1, 0, false);
        AssertProgress(progress.Snapshots[1], 2, 1, 1, 1, 0, false);
        AssertProgress(progress.Snapshots[2], 3, 1, 1, 1, 1, false);
        AssertProgress(progress.Snapshots[3], 3, 1, 1, 1, 1, true);
    }

    [Test]
    public void BatchConverter_ReportsCompletionForEmptyInput()
    {
        BatchConverter converter = new BatchConverter();
        FormatOptions input = new FormatOptions();
        input.KindHint = FormatKind.Sdf;
        FormatOptions output = new FormatOptions();
        output.KindHint = FormatKind.Smiles;
        ProgressCollector progress = new ProgressCollector();
        BatchConversionOptions conversionOptions = new BatchConversionOptions();
        conversionOptions.Progress = progress;

        ConversionReport report = converter.ConvertWithOptions(new StringReader(""), new StringWriter(), input, output, conversionOptions);

        Assert.That(report.Success, Is.True);
        Assert.That(progress.Snapshots.Count, Is.EqualTo(1));
        AssertProgress(progress.Snapshots[0], 0, 0, 0, 0, 0, true);
    }

    static string BuildTwoRecordSdf()
    {
        return BuildRecord("First") + "$$$$\n" + BuildRecord("Second") + "$$$$\n";
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

    static Molecule BuildCarbonOxygenMolecule(string name)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(carbon, oxygen, new BondSpec());
        Molecule molecule = builder.Build();
        molecule.Metadata.AddName(name);
        return molecule;
    }

    static void AssertProgress(BatchConversionProgress progress, int read, int written, int skipped, int warnings, int failures, bool completed)
    {
        Assert.That(progress.ReadCount, Is.EqualTo(read));
        Assert.That(progress.WrittenCount, Is.EqualTo(written));
        Assert.That(progress.SkippedCount, Is.EqualTo(skipped));
        Assert.That(progress.WarningCount, Is.EqualTo(warnings));
        Assert.That(progress.FailureCount, Is.EqualTo(failures));
        Assert.That(progress.Completed, Is.EqualTo(completed));
    }

    class NameBatchFilter : IBatchConversionFilter
    {
        public List<int> Indices = new List<int>();

        public bool Include(BatchConversionRecord record)
        {
            Indices.Add(record.Index);
            return record.Molecule.Metadata.GetPrimaryName() != "Skip";
        }
    }

    class ProgressCollector : IBatchConversionProgress
    {
        public List<BatchConversionProgress> Snapshots = new List<BatchConversionProgress>();

        public void Report(BatchConversionProgress progress)
        {
            Snapshots.Add(progress);
        }
    }

    class TrackingReader : IMoleculeReader
    {
        public int ReadCount;

        public Molecule Read(string? text)
        {
            ReadCount = ReadCount + 1;
            return BuildMolecule("Injected");
        }
    }

    class TrackingWriter : IMoleculeWriter
    {
        public int WriteCount;

        public string Write(Molecule? molecule)
        {
            WriteCount = WriteCount + 1;
            return "injected";
        }
    }
}
