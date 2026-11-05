using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvWriterFileSafetyPublicTests
{
    [Test]
    public void WriteField_PublicApiAppliesEveryQuoteModeAndEscapingStyle()
    {
        StringWriter minimalOutput = new StringWriter();
        CsvWriter minimalWriter = new CsvWriter(minimalOutput, new CsvWriterOptions());
        minimalWriter.WriteField("a,b");
        minimalWriter.WriteField("say \"hi\"");
        minimalWriter.NextRecord();

        CsvWriterOptions allOptions = new CsvWriterOptions();
        allOptions.QuoteMode = CsvQuoteMode.All;
        allOptions.HasNullString = true;
        allOptions.NullString = "NULL";
        StringWriter allOutput = new StringWriter();
        CsvWriter allWriter = new CsvWriter(allOutput, allOptions);
        allWriter.WriteField("plain");
        allWriter.WriteField(null);
        allWriter.NextRecord();

        CsvWriterOptions noneOptions = new CsvWriterOptions();
        noneOptions.QuoteMode = CsvQuoteMode.None;
        noneOptions.Dialect.Escape = '\\';
        noneOptions.Dialect.UseDoubleQuoteEscaping = false;
        StringWriter noneOutput = new StringWriter();
        CsvWriter noneWriter = new CsvWriter(noneOutput, noneOptions);
        noneWriter.WriteField("a,b");
        noneWriter.WriteField("say \"hi\"");
        noneWriter.WriteField("line\r\nnext");
        noneWriter.NextRecord();

        CsvWriterOptions nonNullOptions = new CsvWriterOptions();
        nonNullOptions.QuoteMode = CsvQuoteMode.NonNull;
        nonNullOptions.HasNullString = true;
        nonNullOptions.NullString = "NULL";
        StringWriter nonNullOutput = new StringWriter();
        CsvWriter nonNullWriter = new CsvWriter(nonNullOutput, nonNullOptions);
        nonNullWriter.WriteField(null);
        nonNullWriter.WriteField("a,b");
        nonNullWriter.NextRecord();

        Assert.That(minimalOutput.ToString(), Is.EqualTo("\"a,b\",\"say \"\"hi\"\"\"\r\n"));
        Assert.That(allOutput.ToString(), Is.EqualTo("\"plain\",\"NULL\"\r\n"));
        Assert.That(noneOutput.ToString(), Is.EqualTo("a\\,b,say \\\"hi\\\",line\\r\\nnext\r\n"));
        Assert.That(nonNullOutput.ToString(), Is.EqualTo("NULL,\"a,b\"\r\n"));
    }

    [Test]
    public void WriteTable_PublicApiWritesConfiguredHeaderAndRows()
    {
        CsvTable table = new CsvTable();
        table.Header.Names = new string[] { "Name", "Note" };
        table.Rows.Add(CreateRow(new string?[] { "Ada", "uses, comma" }));
        table.Rows.Add(CreateRow(new string?[] { "Grace", null }));
        CsvWriterOptions options = new CsvWriterOptions();
        options.WriteHeader = true;
        options.HasNullString = true;
        options.NullString = "NULL";
        options.LineTerminator = "\n";
        StringWriter output = new StringWriter();
        CsvWriter writer = new CsvWriter(output, options);

        writer.WriteTable(table);

        Assert.That(writer.ShouldWriteHeader(), Is.True);
        Assert.That(output.ToString(), Is.EqualTo("Name,Note\nAda,\"uses, comma\"\nGrace,NULL\n"));
    }

    [Test]
    public void WriteField_PublicApiAppliesEveryInjectionMode()
    {
        StringWriter noneOutput = WriteInjected("＝SUM(A1:A2)", CsvInjectionMode.None);
        StringWriter stripOutput = WriteInjected("\r\n＠payload", CsvInjectionMode.Strip);
        StringWriter escapeOutput = WriteInjected("+SUM(A1:A2)", CsvInjectionMode.Escape);

        CsvWriterOptions errorOptions = new CsvWriterOptions();
        errorOptions.InjectionMode = CsvInjectionMode.Error;
        StringWriter errorOutput = new StringWriter();
        CsvWriter errorWriter = new CsvWriter(errorOutput, errorOptions);

        Assert.Throws<InvalidOperationException>(() => errorWriter.WriteField("@command"));
        Assert.That(noneOutput.ToString(), Is.EqualTo("＝SUM(A1:A2)\r\n"));
        Assert.That(stripOutput.ToString(), Is.EqualTo("payload\r\n"));
        Assert.That(escapeOutput.ToString(), Is.EqualTo("'+SUM(A1:A2)\r\n"));
        Assert.That(errorOutput.ToString(), Is.Empty);
    }

    [Test]
    public void WriteFieldAndComment_PublicApiHandlesNullsAndNormalizesNewLines()
    {
        CsvWriterOptions options = new CsvWriterOptions();
        options.HasNullString = true;
        options.NullString = "NULL";
        options.NormalizeNewLines = true;
        options.LineTerminator = "\n";
        options.Dialect.HasComment = true;
        options.Dialect.Comment = '#';
        StringWriter output = new StringWriter();
        ICsvWriterControl writer = new CsvWriter(output, options);

        writer.WriteField(null);
        writer.WriteField("line 1\r\nline 2\rline 3");
        writer.NextRecord();
        writer.WriteComment("first\r\nsecond\nthird");

        Assert.That(output.ToString(), Is.EqualTo("NULL,\"line 1\nline 2\nline 3\"\n# first\n# second\n# third\n"));
    }

    [Test]
    public void FlushAndClose_PublicApiHonorsFlushAfterRowAndLeaveOpen()
    {
        TrackingPublicTextWriter output = new TrackingPublicTextWriter();
        CsvWriterOptions options = new CsvWriterOptions();
        options.FlushAfterRow = true;
        options.LeaveOpen = true;
        ICsvWriterControl writer = new CsvWriter(output, options);

        writer.WriteField("value");
        writer.NextRecord();
        writer.Flush();
        writer.Close();
        output.Write("tail");

        Assert.That(output.FlushCount, Is.EqualTo(3));
        Assert.That(output.ToString(), Is.EqualTo("value\r\ntail"));

        StringWriter ownedOutput = new StringWriter();
        ICsvWriterControl ownedWriter = new CsvWriter(ownedOutput, new CsvWriterOptions());
        ownedWriter.Close();
        Assert.Throws<ObjectDisposedException>(() => ownedOutput.Write("closed"));
    }

    [Test]
    public void FileService_PublicApiWritesUtf8WithoutBomAndAppends()
    {
        string path = CreateTempPath();
        try
        {
            ICsvFileService files = new CsvFileService();
            CsvWriter writer = files.OpenWriterUtf8(path, null);
            writer.WriteField("café");
            writer.NextRecord();
            writer.Close();

            byte[] initialBytes = File.ReadAllBytes(path);
            Assert.That(HasUtf8Preamble(initialBytes), Is.False);

            CsvWriter appendWriter = files.OpenAppendWriterUtf8(path, null);
            appendWriter.WriteField("next");
            appendWriter.NextRecord();
            appendWriter.Close();

            CsvReader reader = files.OpenReader(path, null);
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.GetField(0), Is.EqualTo("café"));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.GetField(0), Is.EqualTo("next"));
            Assert.That(reader.Read(), Is.False);
            reader.Close();
        }
        finally
        {
            DeleteTempPath(path);
        }
    }

    [Test]
    public void FileService_PublicApiDetectsUtf8AndUtf16ByteOrderMarks()
    {
        string utf8Path = CreateTempPath();
        string utf16Path = CreateTempPath();
        try
        {
            ICsvFileService files = new CsvFileService();
            CsvWriter utf8Writer = files.OpenWriterWithEncoding(utf8Path, null, new UTF8Encoding(true), false);
            utf8Writer.WriteField("é");
            utf8Writer.NextRecord();
            utf8Writer.Close();

            CsvReader utf8Reader = files.OpenReaderAscii(utf8Path, null);
            Assert.That(utf8Reader.Read(), Is.True);
            Assert.That(utf8Reader.GetField(0), Is.EqualTo("é"));
            Assert.That(utf8Reader.GetCurrentRow().ByteOffset, Is.EqualTo(3));
            utf8Reader.Close();

            CsvWriter utf16Writer = files.OpenWriterWithEncoding(utf16Path, null, Encoding.Unicode, false);
            utf16Writer.WriteField("東京");
            utf16Writer.NextRecord();
            utf16Writer.Close();

            CsvReader utf16Reader = files.OpenReaderUtf8(utf16Path, null);
            Assert.That(utf16Reader.Read(), Is.True);
            Assert.That(utf16Reader.GetField(0), Is.EqualTo("東京"));
            Assert.That(utf16Reader.GetCurrentRow().ByteOffset, Is.EqualTo(2));
            utf16Reader.Close();
        }
        finally
        {
            DeleteTempPath(utf8Path);
            DeleteTempPath(utf16Path);
        }
    }

    [Test]
    public void FileService_PublicApiRoundTripsExplicitEncodingAndAppendOption()
    {
        string path = CreateTempPath();
        try
        {
            ICsvFileService files = new CsvFileService();
            CsvWriter writer = files.OpenWriterWithEncoding(path, null, Encoding.Latin1, false);
            writer.WriteField("café");
            writer.NextRecord();
            writer.Close();

            CsvReader reader = files.OpenReaderWithEncoding(path, null, Encoding.Latin1);
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.GetField(0), Is.EqualTo("café"));
            reader.Close();

            CsvWriterOptions appendOptions = new CsvWriterOptions();
            appendOptions.Append = true;
            CsvWriter appendWriter = files.OpenWriter(path, appendOptions);
            appendWriter.WriteField("next");
            appendWriter.NextRecord();
            appendWriter.Close();

            string text = File.ReadAllText(path, new UTF8Encoding(false));
            Assert.That(text, Does.EndWith("next\r\n"));
        }
        finally
        {
            DeleteTempPath(path);
        }
    }

    [Test]
    public void RejectSink_PublicApiWritesSelfContainedEscapedOutput()
    {
        CsvWriterOptions options = new CsvWriterOptions();
        options.WriteHeader = true;
        options.LineTerminator = "\n";
        options.LeaveOpen = true;
        StringWriter output = new StringWriter();
        ICsvRejectSink sink = new CsvRejectSink(output, options);
        CsvRow row = new CsvRow();
        row.RawText = "bad,\"input\"\n";
        CsvDiagnostic diagnostic = new CsvDiagnostic();
        diagnostic.Category = CsvDiagnostic.ParserCategory;
        diagnostic.Code = "MalformedQuote";
        diagnostic.Message = "Bad, quoted input.";
        diagnostic.Row = 2;
        diagnostic.Column = 1;
        diagnostic.Line = 3;
        diagnostic.CharacterOffset = 8;

        sink.WriteReject(row, diagnostic);
        sink.Flush();
        sink.Close();

        CsvReaderOptions readerOptions = new CsvReaderOptions();
        readerOptions.HeaderOptions.HasHeader = true;
        CsvReader reader = new CsvReader(new StringReader(output.ToString()), readerOptions);
        Assert.That(reader.ReadHeader(), Is.True);
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetFieldByName("Category"), Is.EqualTo("Parser"));
        Assert.That(reader.GetFieldByName("Code"), Is.EqualTo("MalformedQuote"));
        Assert.That(reader.GetFieldByName("Message"), Is.EqualTo("Bad, quoted input."));
        Assert.That(reader.GetFieldByName("CharacterOffset"), Is.EqualTo("8"));
        Assert.That(reader.GetFieldByName("RawInput"), Is.EqualTo("bad,\"input\"\n"));
        Assert.That(reader.Read(), Is.False);
    }

    static StringWriter WriteInjected(string value, CsvInjectionMode mode)
    {
        CsvWriterOptions options = new CsvWriterOptions();
        options.InjectionMode = mode;
        StringWriter output = new StringWriter();
        CsvWriter writer = new CsvWriter(output, options);
        writer.WriteField(value);
        writer.NextRecord();
        return output;
    }

    static CsvRow CreateRow(string?[] fields)
    {
        CsvRow row = new CsvRow();
        row.Fields = fields;
        return row;
    }

    static bool HasUtf8Preamble(byte[] bytes)
    {
        if (bytes.Length < 3)
        {
            return false;
        }

        return bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191;
    }

    static string CreateTempPath()
    {
        return Path.Combine(Path.GetTempPath(), "csv-public-" + Guid.NewGuid().ToString("N") + ".csv");
    }

    static void DeleteTempPath(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

public class TrackingPublicTextWriter : StringWriter
{
    public int FlushCount;

    public TrackingPublicTextWriter()
    {
        FlushCount = 0;
    }

    public override void Flush()
    {
        FlushCount = FlushCount + 1;
        base.Flush();
    }
}
