using System.IO;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvProcessorTests
{
    [Test]
    public void Process_StreamsRowsFromReaderToWriter()
    {
        CsvReader reader = CsvReader.Create(new StringReader("a,b\n\"c,d\",e\n"), CsvReaderOptions.CreateDefault());
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, CsvWriterOptions.CreateDefault());

        long count = CsvProcessor.Process(reader, writer);

        Assert.That(count, Is.EqualTo(2));
        Assert.That(output.ToString(), Is.EqualTo("a,b\r\n\"c,d\",e\r\n"));
    }

    [Test]
    public void Process_WritesReaderHeaderWhenWriterOptionRequestsHeader()
    {
        CsvReaderOptions readerOptions = CsvReaderOptions.CreateDefault();
        readerOptions.HeaderOptions.HasHeader = true;
        CsvReader reader = CsvReader.Create(new StringReader("Name,Age\nAda,42\n"), readerOptions);
        reader.ReadHeader();
        CsvWriterOptions writerOptions = CsvWriterOptions.CreateDefault();
        writerOptions.WriteHeader = true;
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, writerOptions);

        long count = CsvProcessor.Process(reader, writer, CsvPipeline.CreateDefault());

        Assert.That(count, Is.EqualTo(1));
        Assert.That(output.ToString(), Is.EqualTo("Name,Age\r\nAda,42\r\n"));
    }

    [Test]
    public void Process_UpdatesPipelineRowCounters()
    {
        CsvReader reader = CsvReader.Create(new StringReader("1\n2\n3\n"), CsvReaderOptions.CreateDefault());
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, CsvWriterOptions.CreateDefault());
        CsvPipeline pipeline = CsvPipeline.CreateDefault();

        long count = CsvProcessor.Process(reader, writer, pipeline);

        Assert.That(count, Is.EqualTo(3));
        Assert.That(pipeline.RowsRead, Is.EqualTo(3));
        Assert.That(pipeline.RowsWritten, Is.EqualTo(3));
        Assert.That(output.ToString(), Is.EqualTo("1\r\n2\r\n3\r\n"));
    }

    [Test]
    public void Process_SkipsRowsRejectedByFilterCallback()
    {
        CsvReader reader = CsvReader.Create(new StringReader("keep\nskip\nkeep\n"), CsvReaderOptions.CreateDefault());
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, CsvWriterOptions.CreateDefault());
        CsvPipeline pipeline = CsvPipeline.CreateDefault();
        pipeline.HasFilter = true;
        pipeline.Filter = RejectSkipRowsFilter.Create();

        long count = CsvProcessor.Process(reader, writer, pipeline);

        Assert.That(count, Is.EqualTo(2));
        Assert.That(pipeline.RowsRead, Is.EqualTo(3));
        Assert.That(pipeline.RowsWritten, Is.EqualTo(2));
        Assert.That(pipeline.RowsFiltered, Is.EqualTo(1));
        Assert.That(output.ToString(), Is.EqualTo("keep\r\nkeep\r\n"));
    }

    [Test]
    public void Process_IgnoresFilterWhenHasFilterIsFalse()
    {
        CsvReader reader = CsvReader.Create(new StringReader("keep\nskip\n"), CsvReaderOptions.CreateDefault());
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, CsvWriterOptions.CreateDefault());
        CsvPipeline pipeline = CsvPipeline.CreateDefault();
        pipeline.Filter = RejectSkipRowsFilter.Create();

        long count = CsvProcessor.Process(reader, writer, pipeline);

        Assert.That(count, Is.EqualTo(2));
        Assert.That(pipeline.RowsFiltered, Is.EqualTo(0));
        Assert.That(output.ToString(), Is.EqualTo("keep\r\nskip\r\n"));
    }

    [Test]
    public void Process_TransformsRowsBeforeWriting()
    {
        CsvReader reader = CsvReader.Create(new StringReader("ada\n"), CsvReaderOptions.CreateDefault());
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, CsvWriterOptions.CreateDefault());
        CsvPipeline pipeline = CsvPipeline.CreateDefault();
        pipeline.HasTransformer = true;
        pipeline.Transformer = UppercaseFirstFieldTransformer.Create();

        long count = CsvProcessor.Process(reader, writer, pipeline);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(pipeline.RowsTransformed, Is.EqualTo(1));
        Assert.That(output.ToString(), Is.EqualTo("ADA\r\n"));
    }

    [Test]
    public void Process_TransformerCanEnrichRowsWithComputedFields()
    {
        CsvReader reader = CsvReader.Create(new StringReader("Ada,Lovelace\nGrace,Hopper\n"), CsvReaderOptions.CreateDefault());
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, CsvWriterOptions.CreateDefault());
        CsvPipeline pipeline = CsvPipeline.CreateDefault();
        pipeline.HasTransformer = true;
        pipeline.Transformer = FullNameTransformer.Create();

        long count = CsvProcessor.Process(reader, writer, pipeline);

        Assert.That(count, Is.EqualTo(2));
        Assert.That(pipeline.RowsTransformed, Is.EqualTo(2));
        Assert.That(output.ToString(), Is.EqualTo("Ada,Lovelace,Ada Lovelace\r\nGrace,Hopper,Grace Hopper\r\n"));
    }

    [Test]
    public void Process_RejectsRowsRejectedByValidatorCallback()
    {
        CsvReader reader = CsvReader.Create(new StringReader("ok\nbad\nok\n"), CsvReaderOptions.CreateDefault());
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, CsvWriterOptions.CreateDefault());
        CsvPipeline pipeline = CsvPipeline.CreateDefault();
        pipeline.HasValidator = true;
        pipeline.Validator = RejectBadRowsValidator.Create();

        long count = CsvProcessor.Process(reader, writer, pipeline);

        Assert.That(count, Is.EqualTo(2));
        Assert.That(pipeline.RowsRead, Is.EqualTo(3));
        Assert.That(pipeline.RowsWritten, Is.EqualTo(2));
        Assert.That(pipeline.RowsRejected, Is.EqualTo(1));
        Assert.That(pipeline.Rejects.Count, Is.EqualTo(1));
        Assert.That(pipeline.Rejects[0].GetField(0), Is.EqualTo("bad"));
        Assert.That(pipeline.RejectDiagnostics.Count(), Is.EqualTo(1));
        Assert.That(pipeline.RejectDiagnostics.Get(0).Code, Is.EqualTo("BadRow"));
        Assert.That(output.ToString(), Is.EqualTo("ok\r\nok\r\n"));
    }

    [Test]
    public void Process_CallsRejectHandlerForRejectedRows()
    {
        CsvReader reader = CsvReader.Create(new StringReader("bad\n"), CsvReaderOptions.CreateDefault());
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, CsvWriterOptions.CreateDefault());
        CountingRejectHandler rejectHandler = new CountingRejectHandler();
        CsvPipeline pipeline = CsvPipeline.CreateDefault();
        pipeline.HasValidator = true;
        pipeline.Validator = RejectBadRowsValidator.Create();
        pipeline.HasRejectHandler = true;
        pipeline.RejectHandler = rejectHandler.CreateHandler();

        long count = CsvProcessor.Process(reader, writer, pipeline);

        Assert.That(count, Is.EqualTo(0));
        Assert.That(rejectHandler.Count, Is.EqualTo(1));
        Assert.That(rejectHandler.LastCode, Is.EqualTo("BadRow"));
        Assert.That(rejectHandler.LastValue, Is.EqualTo("bad"));
        Assert.That(output.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void Process_AppliesPipelineRowSkipAndLimit()
    {
        CsvReader reader = CsvReader.Create(new StringReader("1\n2\n3\n4\n"), CsvReaderOptions.CreateDefault());
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, CsvWriterOptions.CreateDefault());
        CsvPipeline pipeline = CsvPipeline.CreateDefault();
        pipeline.SkipRows = 1;
        pipeline.MaxRows = 2;

        long count = CsvProcessor.Process(reader, writer, pipeline);

        Assert.That(count, Is.EqualTo(2));
        Assert.That(pipeline.RowsRead, Is.EqualTo(3));
        Assert.That(pipeline.RowsSkipped, Is.EqualTo(1));
        Assert.That(pipeline.RowsWritten, Is.EqualTo(2));
        Assert.That(output.ToString(), Is.EqualTo("2\r\n3\r\n"));
    }

    [Test]
    public void Process_CallsProgressHandlerForSkippedFilteredRejectedAndWrittenRows()
    {
        CsvReader reader = CsvReader.Create(new StringReader("skip\nfilter\nbad\nok\n"), CsvReaderOptions.CreateDefault());
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, CsvWriterOptions.CreateDefault());
        CountingProgressHandler progressHandler = new CountingProgressHandler();
        CsvPipeline pipeline = CsvPipeline.CreateDefault();
        pipeline.SkipRows = 1;
        pipeline.HasFilter = true;
        pipeline.Filter = RejectFilterRowsFilter.Create();
        pipeline.HasValidator = true;
        pipeline.Validator = RejectBadRowsValidator.Create();
        pipeline.HasProgressHandler = true;
        pipeline.ProgressHandler = progressHandler.CreateHandler();

        long count = CsvProcessor.Process(reader, writer, pipeline);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(progressHandler.Count, Is.EqualTo(4));
        Assert.That(progressHandler.LastValue, Is.EqualTo("ok"));
        Assert.That(progressHandler.LastRowsRead, Is.EqualTo(4));
        Assert.That(progressHandler.LastRowsSkipped, Is.EqualTo(1));
        Assert.That(progressHandler.LastRowsFiltered, Is.EqualTo(1));
        Assert.That(progressHandler.LastRowsRejected, Is.EqualTo(1));
        Assert.That(progressHandler.LastRowsWritten, Is.EqualTo(1));
        Assert.That(output.ToString(), Is.EqualTo("ok\r\n"));
    }

    [Test]
    public void Process_ValidatesTransformedRowsBeforeWriting()
    {
        CsvReader reader = CsvReader.Create(new StringReader("bad\n"), CsvReaderOptions.CreateDefault());
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, CsvWriterOptions.CreateDefault());
        CsvPipeline pipeline = CsvPipeline.CreateDefault();
        pipeline.HasTransformer = true;
        pipeline.Transformer = UppercaseFirstFieldTransformer.Create();
        pipeline.HasValidator = true;
        pipeline.Validator = RejectBadRowsValidator.Create();

        long count = CsvProcessor.Process(reader, writer, pipeline);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(pipeline.RowsRejected, Is.EqualTo(0));
        Assert.That(output.ToString(), Is.EqualTo("BAD\r\n"));
    }

    [Test]
    public void Process_UsesOriginalRowWhenTransformerReturnsNull()
    {
        CsvReader reader = CsvReader.Create(new StringReader("value\n"), CsvReaderOptions.CreateDefault());
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, CsvWriterOptions.CreateDefault());
        CsvPipeline pipeline = CsvPipeline.CreateDefault();
        pipeline.HasTransformer = true;
        pipeline.Transformer = NullReturningTransformer.Create();

        long count = CsvProcessor.Process(reader, writer, pipeline);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(pipeline.RowsTransformed, Is.EqualTo(1));
        Assert.That(output.ToString(), Is.EqualTo("value\r\n"));
    }

    [Test]
    public void LoadTable_ReadsHeaderAndStopsAtMaxRowsWithoutOverReading()
    {
        CsvReaderOptions readerOptions = CsvReaderOptions.CreateDefault();
        readerOptions.HeaderOptions.HasHeader = true;
        CsvReader reader = CsvReader.Create(new StringReader("Id,Name\n1,Ada\n2,Grace\n3,Linus\n"), readerOptions);
        CsvTableOptions tableOptions = CsvTableOptions.CreateDefault();
        tableOptions.MaxRows = 2;

        CsvTable table = CsvProcessor.LoadTable(reader, tableOptions);

        Assert.That(table.Header.Names, Is.EqualTo(new string[] { "Id", "Name" }));
        Assert.That(table.Schema.Count(), Is.EqualTo(2));
        Assert.That(table.CountRows(), Is.EqualTo(2));
        Assert.That(table.GetRow(0).GetFieldByName("Name"), Is.EqualTo("Ada"));
        Assert.That(table.GetRow(1).GetFieldByName("Name"), Is.EqualTo("Grace"));
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetField(0), Is.EqualTo("3"));
    }

    [Test]
    public void LoadTable_CanExcludeHeaderAndLoadRowsAsData()
    {
        CsvReader reader = CsvReader.Create(new StringReader("A,B\n1,2\n"), CsvReaderOptions.CreateDefault());
        CsvTableOptions tableOptions = CsvTableOptions.CreateDefault();
        tableOptions.IncludeHeader = false;

        CsvTable table = CsvProcessor.LoadTable(reader, tableOptions);

        Assert.That(table.Header.Names, Is.Empty);
        Assert.That(table.CountRows(), Is.EqualTo(2));
        Assert.That(table.GetRow(0).Fields, Is.EqualTo(new string[] { "A", "B" }));
    }

    [Test]
    public void LoadTable_ClonesRowsByDefault()
    {
        CsvReader reader = CsvReader.Create(new StringReader("1\n"), CsvReaderOptions.CreateDefault());

        CsvTable table = CsvProcessor.LoadTable(reader, CsvTableOptions.CreateDefault());
        reader.GetCurrentRow().SetField(0, "changed");

        Assert.That(table.GetRow(0).GetField(0), Is.EqualTo("1"));
    }

    [Test]
    public void LoadTable_CapturesGeneratedHeadersWhenIncluded()
    {
        CsvReader reader = CsvReader.Create(new StringReader("1,2\n"), CsvReaderOptions.CreateDefault());

        CsvTable table = CsvProcessor.LoadTable(reader, CsvTableOptions.CreateDefault());

        Assert.That(table.Header.Names, Is.EqualTo(new string[] { "Column1", "Column2" }));
        Assert.That(table.GetRow(0).GetFieldByName("Column2"), Is.EqualTo("2"));
    }
}

public class RejectSkipRowsFilter
{
    public static CsvRowFilter Create()
    {
        CsvRowFilter filter = CsvRowFilter.CreateDefault();
        filter.HasIncludeCallback = true;
        filter.IncludeCallback = Include;
        return filter;
    }

    public static bool Include(CsvRow row)
    {
        if (row.GetField(0) == "skip")
        {
            return false;
        }

        return true;
    }
}

public class RejectFilterRowsFilter
{
    public static CsvRowFilter Create()
    {
        CsvRowFilter filter = CsvRowFilter.CreateDefault();
        filter.HasIncludeCallback = true;
        filter.IncludeCallback = Include;
        return filter;
    }

    public static bool Include(CsvRow row)
    {
        if (row.GetField(0) == "filter")
        {
            return false;
        }

        return true;
    }
}

public class UppercaseFirstFieldTransformer
{
    public static CsvRowTransformer Create()
    {
        CsvRowTransformer transformer = CsvRowTransformer.CreateDefault();
        transformer.HasTransformCallback = true;
        transformer.TransformCallback = Transform;
        return transformer;
    }

    public static CsvRow Transform(CsvRow row)
    {
        CsvRow transformed = row.Clone();
        string? value = transformed.GetField(0);
        if (value != null)
        {
            transformed.SetField(0, value.ToUpperInvariant());
        }
        return transformed;
    }
}

public class NullReturningTransformer
{
    public static CsvRowTransformer Create()
    {
        CsvRowTransformer transformer = CsvRowTransformer.CreateDefault();
        transformer.HasTransformCallback = true;
        transformer.TransformCallback = Transform;
        return transformer;
    }

    public static CsvRow? Transform(CsvRow row)
    {
        return null;
    }
}

public class FullNameTransformer
{
    public static CsvRowTransformer Create()
    {
        CsvRowTransformer transformer = CsvRowTransformer.CreateDefault();
        transformer.HasTransformCallback = true;
        transformer.TransformCallback = Transform;
        return transformer;
    }

    public static CsvRow Transform(CsvRow row)
    {
        CsvRow transformed = row.Clone();
        string fullName = transformed.GetField(0) + " " + transformed.GetField(1);
        transformed.AddField(fullName);
        return transformed;
    }
}

public class RejectBadRowsValidator
{
    public static CsvRowValidator Create()
    {
        CsvRowValidator validator = CsvRowValidator.CreateDefault();
        validator.Code = "BadRow";
        validator.Message = "Bad rows are rejected.";
        validator.HasIsValidCallback = true;
        validator.IsValidCallback = IsValid;
        return validator;
    }

    public static bool IsValid(CsvRow row)
    {
        if (row.GetField(0) == "bad")
        {
            return false;
        }

        return true;
    }
}

public class CountingRejectHandler
{
    public int Count;
    public string LastCode;
    public string? LastValue;

    public CountingRejectHandler()
    {
        Count = 0;
        LastCode = "";
        LastValue = "";
    }

    public CsvRejectHandler CreateHandler()
    {
        CsvRejectHandler handler = CsvRejectHandler.CreateDefault();
        handler.HasHandleCallback = true;
        handler.HandleCallback = Handle;
        return handler;
    }

    public void Handle(CsvRow row, CsvDiagnostic diagnostic)
    {
        Count = Count + 1;
        LastCode = diagnostic.Code;
        LastValue = row.GetField(0);
    }
}

public class CountingProgressHandler
{
    public int Count;
    public long LastRowsRead;
    public long LastRowsWritten;
    public long LastRowsSkipped;
    public long LastRowsFiltered;
    public long LastRowsRejected;
    public string? LastValue;

    public CountingProgressHandler()
    {
        Count = 0;
        LastRowsRead = 0;
        LastRowsWritten = 0;
        LastRowsSkipped = 0;
        LastRowsFiltered = 0;
        LastRowsRejected = 0;
        LastValue = "";
    }

    public CsvProgressHandler CreateHandler()
    {
        CsvProgressHandler handler = CsvProgressHandler.CreateDefault();
        handler.HasReportCallback = true;
        handler.ReportCallback = Report;
        return handler;
    }

    public void Report(CsvPipeline pipeline, CsvRow row)
    {
        Count = Count + 1;
        LastRowsRead = pipeline.RowsRead;
        LastRowsWritten = pipeline.RowsWritten;
        LastRowsSkipped = pipeline.RowsSkipped;
        LastRowsFiltered = pipeline.RowsFiltered;
        LastRowsRejected = pipeline.RowsRejected;
        LastValue = row.GetField(0);
    }
}
