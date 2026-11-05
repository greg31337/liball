using System.IO;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvReaderRelaxedQuoteTests
{
    [Test]
    public void Read_RelaxedModeTreatsQuoteInsideUnquotedFieldAsLiteral()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.ErrorMode = CsvErrorMode.Relaxed;
        CsvReader reader = CsvReader.Create(new StringReader("a\"b,c"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "a\"b", "c" }));
        Assert.That(reader.HasParseError, Is.False);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(0));
    }

    [Test]
    public void Read_RelaxedModeAppendsTextAfterClosingQuote()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.ErrorMode = CsvErrorMode.Relaxed;
        CsvReader reader = CsvReader.Create(new StringReader("\"a\"b,c"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "ab", "c" }));
        Assert.That(reader.HasParseError, Is.False);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(0));
    }

    [Test]
    public void Read_RelaxedModeCompletesUnterminatedQuotedFieldAtEnd()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.ErrorMode = CsvErrorMode.Relaxed;
        CsvReader reader = CsvReader.Create(new StringReader("\"a,b"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "a,b" }));
        Assert.That(reader.HasParseError, Is.False);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(0));
    }

    [Test]
    public void Read_ExcelDialectAllowsRelaxedQuotesWithStrictErrorMode()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.Dialect = CsvDialect.CreateExcel();
        options.ErrorMode = CsvErrorMode.Strict;
        CsvReader reader = CsvReader.Create(new StringReader("a\"b,c"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "a\"b", "c" }));
        Assert.That(reader.HasParseError, Is.False);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(0));
    }

    [Test]
    public void Read_ExcelDialectStillAppliesStrictLengthLimits()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.Dialect = CsvDialect.CreateExcel();
        options.ErrorMode = CsvErrorMode.Strict;
        options.MaxFieldLength = 1;
        CsvReader reader = CsvReader.Create(new StringReader("ab"), options);

        Assert.That(reader.Read(), Is.False);
        Assert.That(reader.HasParseError, Is.True);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Code, Is.EqualTo("MaxFieldLength"));
    }
}
