using System.IO;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvDialectTests
{
    [Test]
    public void CreateRfc4180_ReturnsStrictCommaDialect()
    {
        CsvDialect dialect = CsvDialect.CreateRfc4180();

        Assert.That(dialect.Delimiter, Is.EqualTo(','));
        Assert.That(dialect.Quote, Is.EqualTo('"'));
        Assert.That(dialect.Escape, Is.EqualTo('"'));
        Assert.That(dialect.UseDoubleQuoteEscaping, Is.True);
        Assert.That(dialect.LineTerminator, Is.EqualTo("\r\n"));
        Assert.That(dialect.NewLineMode, Is.EqualTo(CsvNewLineMode.Auto));
        Assert.That(dialect.SkipInitialSpace, Is.False);
        Assert.That(dialect.AllowRelaxedQuotes, Is.False);
        Assert.That(dialect.HasComment, Is.False);
    }

    [Test]
    public void CreateDefault_MatchesRfc4180()
    {
        CsvDialect defaultDialect = CsvDialect.CreateDefault();
        CsvDialect rfcDialect = CsvDialect.CreateRfc4180();

        Assert.That(defaultDialect.Delimiter, Is.EqualTo(rfcDialect.Delimiter));
        Assert.That(defaultDialect.Quote, Is.EqualTo(rfcDialect.Quote));
        Assert.That(defaultDialect.Escape, Is.EqualTo(rfcDialect.Escape));
        Assert.That(defaultDialect.LineTerminator, Is.EqualTo(rfcDialect.LineTerminator));
        Assert.That(defaultDialect.AllowRelaxedQuotes, Is.EqualTo(rfcDialect.AllowRelaxedQuotes));
    }

    [Test]
    public void CreateTsv_ReturnsTabDelimitedDialect()
    {
        CsvDialect dialect = CsvDialect.CreateTsv();

        Assert.That(dialect.Delimiter, Is.EqualTo('\t'));
        Assert.That(dialect.Quote, Is.EqualTo('"'));
        Assert.That(dialect.Escape, Is.EqualTo('"'));
        Assert.That(dialect.LineTerminator, Is.EqualTo("\n"));
        Assert.That(dialect.UseDoubleQuoteEscaping, Is.True);
    }

    [Test]
    public void CreateExcel_ReturnsRelaxedCommaDialect()
    {
        CsvDialect dialect = CsvDialect.CreateExcel();

        Assert.That(dialect.Delimiter, Is.EqualTo(','));
        Assert.That(dialect.Quote, Is.EqualTo('"'));
        Assert.That(dialect.Escape, Is.EqualTo('"'));
        Assert.That(dialect.LineTerminator, Is.EqualTo("\r\n"));
        Assert.That(dialect.AllowRelaxedQuotes, Is.True);
    }

    [Test]
    public void FactoryMethods_ReturnIndependentInstances()
    {
        CsvDialect first = CsvDialect.CreateDefault();
        CsvDialect second = CsvDialect.CreateDefault();

        first.Delimiter = ';';

        Assert.That(second.Delimiter, Is.EqualTo(','));
    }

    [Test]
    public void Clone_CopiesDialectFieldsToIndependentInstance()
    {
        CsvDialect original = CsvDialect.CreateExcel();
        original.Delimiter = '|';
        original.HasComment = true;
        original.Comment = '#';

        CsvDialect clone = original.Clone();
        original.Delimiter = ',';
        original.HasComment = false;

        Assert.That(clone.Delimiter, Is.EqualTo('|'));
        Assert.That(clone.HasComment, Is.True);
        Assert.That(clone.Comment, Is.EqualTo('#'));
        Assert.That(clone.AllowRelaxedQuotes, Is.True);
    }

    [Test]
    public void Read_TsvDialectParsesTabDelimitedRows()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.Dialect = CsvDialect.CreateTsv();
        CsvReader reader = CsvReader.Create(new StringReader("Name\tNote\nAda\t\"uses\ttab\"\n"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "Name", "Note" }));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "Ada", "uses\ttab" }));

        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void Read_SemicolonDialectParsesSemicolonDelimitedRows()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.Dialect.Delimiter = ';';
        CsvReader reader = CsvReader.Create(new StringReader("a;b;\"c;d\"\n"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "a", "b", "c;d" }));
    }

    [Test]
    public void Read_PipeDialectParsesPipeDelimitedRows()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.Dialect.Delimiter = '|';
        CsvReader reader = CsvReader.Create(new StringReader("a|b|c\r\n1|2|3"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "a", "b", "c" }));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "1", "2", "3" }));
    }

    [Test]
    public void Read_AlternateQuoteParsesSingleQuotedFields()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.Dialect.Quote = '\'';
        options.Dialect.Escape = '\'';
        options.Dialect.UseDoubleQuoteEscaping = true;
        CsvReader reader = CsvReader.Create(new StringReader("'a,b','it''s ok'\n"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "a,b", "it's ok" }));
    }

    [Test]
    public void Read_BackslashEscapeParsesEscapedQuoteInsideQuotedField()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.Dialect.Escape = '\\';
        options.Dialect.UseDoubleQuoteEscaping = false;
        CsvReader reader = CsvReader.Create(new StringReader("\"say \\\"hi\\\"\",done\n"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "say \"hi\"", "done" }));
        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void Read_AutoNewlineModeAcceptsCrLfLfAndCrTerminators()
    {
        CsvReader reader = CsvReader.Create(new StringReader("a\r\nb\nc\rd"), CsvReaderOptions.CreateDefault());

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetField(0), Is.EqualTo("a"));
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetField(0), Is.EqualTo("b"));
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetField(0), Is.EqualTo("c"));
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetField(0), Is.EqualTo("d"));
        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void Read_BlankAndCommentOptionsSkipIgnoredLines()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.SkipBlankLines = true;
        options.AllowComments = true;
        options.Dialect.HasComment = true;
        options.Dialect.Comment = '!';
        CsvReader reader = CsvReader.Create(new StringReader("\n! comment\none,two\r\n\r\n! skip\r\nthree,four"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "one", "two" }));
        Assert.That(reader.GetCurrentRow().Line, Is.EqualTo(3));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "three", "four" }));
        Assert.That(reader.GetCurrentRow().Line, Is.EqualTo(6));

        Assert.That(reader.Read(), Is.False);
    }
}
