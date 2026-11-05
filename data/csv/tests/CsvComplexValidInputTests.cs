using System.IO;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvComplexValidInputTests
{
    [Test]
    public void Read_ParsesCommentedDatasetWithHeaderNullsAndEmbeddedRecords()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HeaderOptions.HasHeader = true;
        options.AllowComments = true;
        options.Dialect.HasComment = true;
        options.Dialect.Comment = '#';
        options.SkipBlankLines = true;
        options.SkipInitialSpace = true;
        options.TrimMode = CsvTrimMode.Unquoted;
        options.HasNullString = true;
        options.NullString = "NULL";
        CsvReader reader = CsvReader.Create(new StringReader("# exported\r\n\r\nId, Description, Amount, Note\r\n1, \"Widget, large\", 10, NULL\r\n2,\"multi\r\nline \"\"memo\"\"\",  20 , \" NULL \"\r\n# ignored\r\n3,,0,\"ends with space \""), options);

        Assert.That(reader.ReadHeader(), Is.True);
        Assert.That(reader.GetHeaders().Names, Is.EqualTo(new string[] { "Id", "Description", "Amount", "Note" }));
        Assert.That(reader.GetCurrentRow().Line, Is.EqualTo(3));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string?[] { "1", "Widget, large", "10", null }));
        Assert.That(reader.GetFieldByName("Note"), Is.Null);
        Assert.That(reader.GetCurrentRow().RowNumber, Is.EqualTo(2));
        Assert.That(reader.GetCurrentRow().Line, Is.EqualTo(4));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetFieldByName("Description"), Is.EqualTo("multi\r\nline \"memo\""));
        Assert.That(reader.GetFieldByName("Amount"), Is.EqualTo("20"));
        Assert.That(reader.GetFieldByName("Note"), Is.EqualTo(" NULL "));
        Assert.That(reader.GetCurrentRow().Line, Is.EqualTo(5));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "3", "", "0", "ends with space " }));
        Assert.That(reader.GetCurrentRow().RowNumber, Is.EqualTo(4));
        Assert.That(reader.GetCurrentRow().Line, Is.EqualTo(8));

        Assert.That(reader.Read(), Is.False);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(0));
    }

    [Test]
    public void Read_CustomDialectParsesEscapedQuotesDelimitersAndComments()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HeaderOptions.HasHeader = true;
        options.AllowComments = true;
        options.Dialect.HasComment = true;
        options.Dialect.Comment = '!';
        options.Dialect.Delimiter = '|';
        options.Dialect.Quote = '\'';
        options.Dialect.Escape = '\\';
        options.Dialect.UseDoubleQuoteEscaping = false;
        CsvReader reader = CsvReader.Create(new StringReader("! generated\n'Id'|'Text'|'Path'\n'1'|'it\\'s | ok'|'c:\\\\temp'\n'2'||'plain'"), options);

        Assert.That(reader.ReadHeader(), Is.True);
        Assert.That(reader.GetHeaders().Names, Is.EqualTo(new string[] { "Id", "Text", "Path" }));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "1", "it's | ok", "c:\\temp" }));
        Assert.That(reader.GetFieldByName("Text"), Is.EqualTo("it's | ok"));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "2", "", "plain" }));

        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void Write_ThenReadRoundTripsComplexValidRowsWithNullsCommentsAndNewLines()
    {
        CsvWriterOptions writerOptions = CsvWriterOptions.CreateDefault();
        writerOptions.HasNullString = true;
        writerOptions.NullString = "NULL";
        writerOptions.NormalizeNewLines = true;
        writerOptions.LineTerminator = "\n";
        writerOptions.Dialect.HasComment = true;
        writerOptions.Dialect.Comment = '#';
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, writerOptions);
        string? nullField = null;

        writer.WriteRow(CsvRow.Create(new string?[] { "#not-comment", "a,b", "line\r\nbreak", "say \"hi\"", nullField, " trailing " }, 1));
        writer.WriteRow(CsvRow.Create(new string[] { "plain", "", "last" }, 2));

        Assert.That(output.ToString(), Is.EqualTo("\"#not-comment\",\"a,b\",\"line\nbreak\",\"say \"\"hi\"\"\",NULL,\" trailing \"\nplain,,last\n"));

        CsvReaderOptions readerOptions = CsvReaderOptions.CreateDefault();
        readerOptions.HasNullString = true;
        readerOptions.NullString = "NULL";
        CsvReader reader = CsvReader.Create(new StringReader(output.ToString()), readerOptions);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string?[] { "#not-comment", "a,b", "line\nbreak", "say \"hi\"", null, " trailing " }));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "plain", "", "last" }));

        Assert.That(reader.Read(), Is.False);
    }
}
