using System.IO;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvRejectSinkTests
{
    [Test]
    public void Handle_WritesConfiguredSelfContainedRejectCsvAndSupportsLifecycle()
    {
        TrackingTextWriter output = new TrackingTextWriter();
        CsvWriterOptions options = new CsvWriterOptions();
        options.Dialect.Delimiter = ';';
        options.LineTerminator = "\n";
        options.WriteHeader = true;
        options.FlushAfterRow = true;
        options.LeaveOpen = true;
        ICsvRejectSink sink = new CsvRejectSink(output, options);
        CsvRow row = new CsvRow();
        row.RawText = "bad,-1\n";
        CsvDiagnostic diagnostic = CreateDiagnostic("Processing", "InvalidField", "Bad; field", 2, 2, 4, 7, "partial");

        sink.WriteReject(row, diagnostic);
        sink.Flush();
        sink.Close();
        output.Write("tail");

        Assert.That(output.ToString(), Is.EqualTo("Category;Code;Message;Row;Column;Line;CharacterOffset;RawInput\nProcessing;InvalidField;\"Bad; field\";2;2;4;7;\"bad,-1\n\"\ntail"));
        Assert.That(output.FlushCount, Is.EqualTo(4));
    }

    [Test]
    public void Sink_ReceivesParserAndValidationRejectsWithoutSourceDependencies()
    {
        StringWriter rejectOutput = new StringWriter();
        CsvWriterOptions sinkOptions = new CsvWriterOptions();
        sinkOptions.WriteHeader = true;
        sinkOptions.LineTerminator = "\n";
        CsvRejectSink sink = new CsvRejectSink(rejectOutput, sinkOptions);

        CsvReaderOptions malformedOptions = new CsvReaderOptions();
        malformedOptions.ErrorMode = CsvErrorMode.SkipBadRow;
        malformedOptions.RejectHandler = sink;
        malformedOptions.HasRejectHandler = true;
        CsvReader malformedReader = new CsvReader(new StringReader("\"bad\"x,1\nok,2\n"), malformedOptions);

        Assert.That(malformedReader.Read(), Is.True);
        Assert.That(malformedReader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "ok", "2" }));
        Assert.That(malformedReader.Read(), Is.False);
        Assert.That(malformedReader.GetRejects(), Is.Empty);

        CsvPipeline pipeline = new CsvPipeline();
        pipeline.Validator = new CsvRowValidator("InvalidRow", "The row is invalid.", IsValidRow, CreateValidationDiagnostic);
        pipeline.HasValidator = true;
        pipeline.RejectHandler = sink;
        pipeline.HasRejectHandler = true;
        CsvReader validationReader = new CsvReader(new StringReader("bad,3\ngood,4\n"), new CsvReaderOptions());
        StringWriter acceptedOutput = new StringWriter();
        CsvWriter acceptedWriter = new CsvWriter(acceptedOutput, new CsvWriterOptions());
        ICsvProcessor processor = new CsvProcessorService();

        long count = processor.Process(validationReader, acceptedWriter, pipeline);
        sink.Flush();

        Assert.That(count, Is.EqualTo(1));
        Assert.That(acceptedOutput.ToString(), Is.EqualTo("good,4\r\n"));

        CsvReaderOptions resultOptions = new CsvReaderOptions();
        resultOptions.HeaderOptions.HasHeader = true;
        CsvReader resultReader = new CsvReader(new StringReader(rejectOutput.ToString()), resultOptions);
        Assert.That(resultReader.ReadHeader(), Is.True);

        Assert.That(resultReader.Read(), Is.True);
        Assert.That(resultReader.GetFieldByName("Category"), Is.EqualTo("Parser"));
        Assert.That(resultReader.GetFieldByName("Code"), Is.EqualTo("MalformedQuote"));
        Assert.That(resultReader.GetFieldByName("Row"), Is.EqualTo("1"));
        Assert.That(resultReader.GetFieldByName("RawInput"), Is.EqualTo("\"bad\"x,1\n"));

        Assert.That(resultReader.Read(), Is.True);
        Assert.That(resultReader.GetFieldByName("Category"), Is.EqualTo("Processing"));
        Assert.That(resultReader.GetFieldByName("Code"), Is.EqualTo("InvalidRow"));
        Assert.That(resultReader.GetFieldByName("Row"), Is.EqualTo("1"));
        Assert.That(resultReader.GetFieldByName("RawInput"), Is.EqualTo("bad,3\n"));
        Assert.That(resultReader.Read(), Is.False);
    }

    [Test]
    public void Handle_UsesDiagnosticRawContextWhenRowRawInputIsUnavailable()
    {
        StringWriter output = new StringWriter();
        CsvRejectSink sink = new CsvRejectSink(output, null);
        CsvRow row = new CsvRow();
        CsvDiagnostic diagnostic = CreateDiagnostic("Processing", "Invalid", "Invalid row.", 1, 0, 1, 0, "fallback raw");

        sink.Handle(row, diagnostic);

        Assert.That(output.ToString(), Is.EqualTo("Category,Code,Message,Row,Column,Line,CharacterOffset,RawInput\r\nProcessing,Invalid,Invalid row.,1,0,1,0,fallback raw\r\n"));
    }

    static CsvDiagnostic CreateDiagnostic(string category, string code, string message, long row, int column, long line, long characterOffset, string rawContext)
    {
        CsvDiagnostic diagnostic = new CsvDiagnostic();
        diagnostic.Category = category;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Row = row;
        diagnostic.Column = column;
        diagnostic.Line = line;
        diagnostic.CharacterOffset = characterOffset;
        diagnostic.RawContext = rawContext;
        return diagnostic;
    }

    static bool IsValidRow(CsvRow row)
    {
        return row.Fields[0] != "bad";
    }

    static CsvDiagnostic CreateValidationDiagnostic(CsvRow row)
    {
        return CreateDiagnostic("Processing", "InvalidRow", "The row is invalid.", row.RowNumber, 0, row.Line, row.CharacterOffset, row.RawText);
    }
}
