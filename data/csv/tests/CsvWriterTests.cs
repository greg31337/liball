using System;
using System.IO;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvWriterTests
{
    [Test]
    public void WriteField_UsesMinimalQuotingAndEscapesQuotes()
    {
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, CsvWriterOptions.CreateDefault());

        writer.WriteField("alpha");
        writer.WriteField("a,b");
        writer.WriteField("say \"hi\"");
        writer.WriteField("line\nbreak");
        writer.NextRecord();

        Assert.That(output.ToString(), Is.EqualTo("alpha,\"a,b\",\"say \"\"hi\"\"\",\"line\nbreak\"\r\n"));
    }

    [Test]
    public void WriteRow_QuotesLeadingTrailingSpaceAndFirstFieldComments()
    {
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();
        options.Dialect.HasComment = true;
        options.Dialect.Comment = '#';
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, options);
        CsvRow row = CsvRow.Create(new string[] { "#comment", " leading", "trailing ", "" }, 1);

        writer.WriteRow(row);

        Assert.That(output.ToString(), Is.EqualTo("\"#comment\",\" leading\",\"trailing \",\r\n"));
    }

    [Test]
    public void WriteRow_UsesCustomDelimiterQuoteAndEscape()
    {
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();
        options.Dialect.Delimiter = '|';
        options.Dialect.Quote = '\'';
        options.Dialect.Escape = '\'';
        options.Dialect.UseDoubleQuoteEscaping = true;
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, options);
        CsvRow row = CsvRow.Create(new string[] { "a|b", "can't" }, 1);

        writer.WriteRow(row);

        Assert.That(output.ToString(), Is.EqualTo("'a|b'|'can''t'\r\n"));
    }

    [Test]
    public void WriteRow_UsesConfiguredEscapeWhenDoubleQuoteEscapingDisabled()
    {
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();
        options.Dialect.Escape = '\\';
        options.Dialect.UseDoubleQuoteEscaping = false;
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, options);

        writer.WriteRow(CsvRow.Create(new string[] { "say \"hi\"" }, 1));

        Assert.That(output.ToString(), Is.EqualTo("\"say \\\"hi\\\"\"\r\n"));
    }

    [Test]
    public void WriteRow_QuoteAllQuotesEveryField()
    {
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();
        options.QuoteMode = CsvQuoteMode.All;
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, options);
        CsvRow row = CsvRow.Create(new string[] { "alpha", "", "a,b" }, 1);

        writer.WriteRow(row);

        Assert.That(output.ToString(), Is.EqualTo("\"alpha\",\"\",\"a,b\"\r\n"));
    }

    [Test]
    public void WriteField_QuoteNonNullQuotesOnlyNonNullValues()
    {
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();
        options.QuoteMode = CsvQuoteMode.NonNull;
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, options);
        string? nullField = null;

        writer.WriteField("alpha");
        writer.WriteField(nullField);
        writer.WriteField("");
        writer.NextRecord();

        Assert.That(output.ToString(), Is.EqualTo("\"alpha\",,\"\"\r\n"));
    }

    [Test]
    public void WriteRow_QuoteNoneEscapesSpecialCharactersWithoutQuoting()
    {
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();
        options.QuoteMode = CsvQuoteMode.None;
        options.Dialect.Escape = '\\';
        options.Dialect.UseDoubleQuoteEscaping = false;
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, options);
        CsvRow row = CsvRow.Create(new string[] { "a,b", "say \"hi\"", "line\nbreak", "back\\slash" }, 1);

        writer.WriteRow(row);

        Assert.That(output.ToString(), Is.EqualTo("a\\,b,say \\\"hi\\\",line\\nbreak,back\\\\slash\r\n"));
    }

    [Test]
    public void WriteRow_QuoteNoneEscapesCarriageReturnAndLineFeedSeparately()
    {
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();
        options.QuoteMode = CsvQuoteMode.None;
        options.Dialect.Escape = '\\';
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, options);

        writer.WriteRow(CsvRow.Create(new string[] { "a\r\nb" }, 1));

        Assert.That(output.ToString(), Is.EqualTo("a\\r\\nb\r\n"));
    }

    [Test]
    public void WriteRow_UsesConfiguredLineTerminator()
    {
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();
        options.LineTerminator = "\n";
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, options);

        writer.WriteRow(CsvRow.Create(new string[] { "a" }, 1));
        writer.WriteRow(CsvRow.Create(new string[] { "b" }, 2));

        Assert.That(output.ToString(), Is.EqualTo("a\nb\n"));
    }

    [Test]
    public void WriteRow_UsesDialectLineTerminatorWhenOptionIsEmpty()
    {
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();
        options.LineTerminator = "";
        options.Dialect.LineTerminator = "\n";
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, options);

        writer.WriteRow(CsvRow.Create(new string[] { "a" }, 1));

        Assert.That(output.ToString(), Is.EqualTo("a\n"));
    }

    [Test]
    public void WriteField_NormalizesEmbeddedNewLinesWhenEnabled()
    {
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();
        options.NormalizeNewLines = true;
        options.LineTerminator = "\r\n";
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, options);

        writer.WriteField("a\nb\rc\r\nd");
        writer.NextRecord();

        Assert.That(output.ToString(), Is.EqualTo("\"a\r\nb\r\nc\r\nd\"\r\n"));
    }

    [Test]
    public void WriteField_NormalizesEmbeddedNewLinesToCustomTerminator()
    {
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();
        options.NormalizeNewLines = true;
        options.LineTerminator = "\n";
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, options);

        writer.WriteField("a\rb\r\nc");
        writer.NextRecord();

        Assert.That(output.ToString(), Is.EqualTo("\"a\nb\nc\"\n"));
    }

    [Test]
    public void WriteHeader_WritesEscapedHeaderRecord()
    {
        CsvHeaderOptions headerOptions = CsvHeaderOptions.CreateDefault();
        CsvHeader header = CsvHeader.CreateFromRow(CsvRow.Create(new string[] { "Id", "Display,Name", "Note" }, 1), headerOptions);
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, CsvWriterOptions.CreateDefault());

        writer.WriteHeader(header);

        Assert.That(output.ToString(), Is.EqualTo("Id,\"Display,Name\",Note\r\n"));
    }

    [Test]
    public void WriteTable_UsesConfiguredHeaderAndFieldPolicies()
    {
        TrackingTextWriter output = new TrackingTextWriter();
        CsvWriterOptions options = new CsvWriterOptions();
        options.Dialect.Delimiter = ';';
        options.QuoteMode = CsvQuoteMode.All;
        options.HasNullString = true;
        options.NullString = "NULL";
        options.InjectionMode = CsvInjectionMode.Escape;
        options.LineTerminator = "\n";
        options.NormalizeNewLines = true;
        options.WriteHeader = true;
        options.FlushAfterRow = true;
        ICsvTableWriter writer = new CsvWriter(output, options);
        CsvTable table = new CsvTable();
        table.Header.Names = new string[] { "Name", "Formula", "Note" };
        CsvRow first = new CsvRow();
        first.Fields = new string?[] { "Ada", "=1+1", null };
        table.Rows.Add(first);
        CsvRow second = new CsvRow();
        second.Fields = new string[] { "Grace", "safe", "line\r\nbreak" };
        table.Rows.Add(second);

        writer.WriteTable(table);

        Assert.That(output.ToString(), Is.EqualTo("\"Name\";\"Formula\";\"Note\"\n\"Ada\";\"'=1+1\";\"NULL\"\n\"Grace\";\"safe\";\"line\nbreak\"\n"));
        Assert.That(output.FlushCount, Is.EqualTo(3));
    }

    [Test]
    public void WriteTable_OmitsDisabledOrUnavailableHeaders()
    {
        CsvTable table = new CsvTable();
        table.Header.Names = new string[] { "Value" };
        CsvRow row = new CsvRow();
        row.Fields = new string[] { "data" };
        table.Rows.Add(row);
        CsvWriterOptions withoutHeaderOptions = new CsvWriterOptions();
        withoutHeaderOptions.WriteHeader = false;
        StringWriter withoutHeaderOutput = new StringWriter();
        ICsvTableWriter withoutHeader = new CsvWriter(withoutHeaderOutput, withoutHeaderOptions);

        withoutHeader.WriteTable(table);

        CsvTable headerlessTable = new CsvTable();
        CsvRow headerlessRow = new CsvRow();
        headerlessRow.Fields = new string[] { "other" };
        headerlessTable.Rows.Add(headerlessRow);
        CsvWriterOptions unavailableHeaderOptions = new CsvWriterOptions();
        unavailableHeaderOptions.WriteHeader = true;
        StringWriter unavailableHeaderOutput = new StringWriter();
        ICsvTableWriter unavailableHeader = new CsvWriter(unavailableHeaderOutput, unavailableHeaderOptions);

        unavailableHeader.WriteTable(headerlessTable);

        Assert.That(withoutHeaderOutput.ToString(), Is.EqualTo("data\r\n"));
        Assert.That(unavailableHeaderOutput.ToString(), Is.EqualTo("other\r\n"));
    }

    [Test]
    public void WriteComment_UsesConfiguredMarkerAndLineTerminator()
    {
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();
        options.Dialect.HasComment = true;
        options.Dialect.Comment = '!';
        options.LineTerminator = "\n";
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, options);

        writer.WriteComment("exported");

        Assert.That(output.ToString(), Is.EqualTo("! exported\n"));
    }

    [Test]
    public void WriteComment_PrefixesEachInputLine()
    {
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, CsvWriterOptions.CreateDefault());

        writer.WriteComment("one\ntwo\r\nthree");

        Assert.That(output.ToString(), Is.EqualTo("# one\r\n# two\r\n# three\r\n"));
    }

    [Test]
    public void WriteComment_StartsNewRecordAfterPartialRowAndHandlesNullText()
    {
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, CsvWriterOptions.CreateDefault());
        string? nullComment = null;

        writer.WriteField("value");
        writer.WriteComment(nullComment);

        Assert.That(output.ToString(), Is.EqualTo("value\r\n#\r\n"));
    }

    [Test]
    public void WriteField_WritesConfiguredNullStringOnlyForNullValues()
    {
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();
        options.HasNullString = true;
        options.NullString = "NULL";
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, options);
        string? nullField = null;

        writer.WriteField(nullField);
        writer.WriteField("");
        writer.NextRecord();

        Assert.That(output.ToString(), Is.EqualTo("NULL,\r\n"));
    }

    [Test]
    public void WriteField_NullStringNullFallsBackToEmptyField()
    {
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();
        options.HasNullString = true;
        string? nullString = null;
        options.NullString = nullString;
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, options);
        string? nullField = null;

        writer.WriteField(nullField);
        writer.NextRecord();

        Assert.That(output.ToString(), Is.EqualTo("\r\n"));
    }

    [Test]
    public void WriteField_QuotesNullStringWhenMinimalQuotingRequiresIt()
    {
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();
        options.HasNullString = true;
        options.NullString = "missing,value";
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, options);
        string? nullField = null;

        writer.WriteField(nullField);
        writer.NextRecord();

        Assert.That(output.ToString(), Is.EqualTo("\"missing,value\"\r\n"));
    }

    [Test]
    public void WriteField_NullStringRespectsQuoteAllAndQuoteNonNullModes()
    {
        CsvWriterOptions quoteAllOptions = CsvWriterOptions.CreateDefault();
        quoteAllOptions.HasNullString = true;
        quoteAllOptions.NullString = "NULL";
        quoteAllOptions.QuoteMode = CsvQuoteMode.All;
        StringWriter quoteAllOutput = new StringWriter();
        CsvWriter quoteAllWriter = CsvWriter.Create(quoteAllOutput, quoteAllOptions);
        string? nullField = null;

        quoteAllWriter.WriteField(nullField);
        quoteAllWriter.NextRecord();

        CsvWriterOptions nonNullOptions = CsvWriterOptions.CreateDefault();
        nonNullOptions.HasNullString = true;
        nonNullOptions.NullString = "NULL";
        nonNullOptions.QuoteMode = CsvQuoteMode.NonNull;
        StringWriter nonNullOutput = new StringWriter();
        CsvWriter nonNullWriter = CsvWriter.Create(nonNullOutput, nonNullOptions);

        nonNullWriter.WriteField(nullField);
        nonNullWriter.NextRecord();

        Assert.That(quoteAllOutput.ToString(), Is.EqualTo("\"NULL\"\r\n"));
        Assert.That(nonNullOutput.ToString(), Is.EqualTo("NULL\r\n"));
    }

    [Test]
    public void WriteField_InjectionNoneLeavesFormulaTextUnchanged()
    {
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();
        options.InjectionMode = CsvInjectionMode.None;
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, options);

        writer.WriteField("=1+1");
        writer.NextRecord();

        Assert.That(output.ToString(), Is.EqualTo("=1+1\r\n"));
    }

    [Test]
    public void WriteField_InjectionStripRemovesDangerousPrefix()
    {
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();
        options.InjectionMode = CsvInjectionMode.Strip;
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, options);

        writer.WriteField("=1+1");
        writer.WriteField(" @cmd");
        writer.WriteField("-10");
        writer.WriteField("+SUM");
        writer.NextRecord();

        Assert.That(output.ToString(), Is.EqualTo("1+1,cmd,10,SUM\r\n"));
    }

    [Test]
    public void WriteField_InjectionEscapePrefixesApostrophe()
    {
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();
        options.InjectionMode = CsvInjectionMode.Escape;
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, options);

        writer.WriteField("=1+1");
        writer.NextRecord();

        Assert.That(output.ToString(), Is.EqualTo("'=1+1\r\n"));
    }

    [Test]
    public void WriteField_InjectionEscapeDetectsDangerAfterLeadingSpaces()
    {
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();
        options.InjectionMode = CsvInjectionMode.Escape;
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, options);

        writer.WriteField(" +SUM(A1:A2)");
        writer.NextRecord();

        Assert.That(output.ToString(), Is.EqualTo("' +SUM(A1:A2)\r\n"));
    }

    [Test]
    public void WriteField_InjectionErrorThrowsBeforeWriting()
    {
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();
        options.InjectionMode = CsvInjectionMode.Error;
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, options);

        Assert.Throws<InvalidOperationException>(() => writer.WriteField("@cmd"));
        Assert.That(output.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CreateAppend_SetsAppendModeAndWritesAtCurrentPosition()
    {
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();
        StringWriter output = new StringWriter();
        output.Write("existing\r\n");

        CsvWriter writer = CsvWriter.CreateAppend(output, options);
        writer.WriteRow(CsvRow.Create(new string[] { "next" }, 2));

        Assert.That(writer.Options.Append, Is.True);
        Assert.That(output.ToString(), Is.EqualTo("existing\r\nnext\r\n"));
    }

    [Test]
    public void FlushAndFlushAfterRowFlushUnderlyingWriter()
    {
        TrackingTextWriter output = new TrackingTextWriter();
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();
        CsvWriter writer = CsvWriter.Create(output, options);

        writer.Flush();

        options.FlushAfterRow = true;
        writer.WriteRow(CsvRow.Create(new string[] { "a" }, 1));

        Assert.That(output.FlushCount, Is.EqualTo(2));
    }

    [Test]
    public void Close_ClosesUnderlyingWriterByDefault()
    {
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, CsvWriterOptions.CreateDefault());

        writer.Close();

        Assert.That(writer.IsClosed, Is.True);
        Assert.Throws<ObjectDisposedException>(() => output.Write("tail"));
    }

    [Test]
    public void Close_LeavesUnderlyingWriterOpenWhenConfigured()
    {
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();
        options.LeaveOpen = true;
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, options);

        writer.Close();
        output.Write("tail");

        Assert.That(writer.IsClosed, Is.True);
        Assert.That(output.ToString(), Is.EqualTo("tail"));
    }
}

public class TrackingTextWriter : StringWriter
{
    public int FlushCount;

    public TrackingTextWriter()
    {
        FlushCount = 0;
    }

    public override void Flush()
    {
        FlushCount = FlushCount + 1;
        base.Flush();
    }
}
