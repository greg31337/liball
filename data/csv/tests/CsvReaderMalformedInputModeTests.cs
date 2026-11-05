using System.IO;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvReaderMalformedInputModeTests
{
    [Test]
    public void Read_StrictModeStopsAtMalformedInput()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.ErrorMode = CsvErrorMode.Strict;
        CsvReader reader = CsvReader.Create(new StringReader("\"bad\"x,1\nok,2"), options);

        Assert.That(reader.Read(), Is.False);
        Assert.That(reader.HasParseError, Is.True);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Code, Is.EqualTo("MalformedQuote"));
        Assert.That(reader.GetDiagnostics().Get(0).Row, Is.EqualTo(1));
        Assert.That(reader.GetRejects().Count, Is.EqualTo(0));
    }

    [Test]
    public void Read_RelaxedModeTreatsMalformedQuotesAsData()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.ErrorMode = CsvErrorMode.Relaxed;
        CsvReader reader = CsvReader.Create(new StringReader("\"bad\"x,1\nok,2"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "badx", "1" }));
        Assert.That(reader.HasParseError, Is.False);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(0));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "ok", "2" }));
    }

    [Test]
    public void Read_RelaxedModeDoesNotApplyConfiguredLengthLimits()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.ErrorMode = CsvErrorMode.Relaxed;
        options.MaxFieldLength = 3;
        options.MaxRowLength = 3;
        options.MaxColumnCount = 1;
        CsvReader reader = CsvReader.Create(new StringReader("abcd,too,long"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "abcd", "too", "long" }));
        Assert.That(reader.HasParseError, Is.False);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(0));
    }

    [Test]
    public void Read_SkipBadRowSkipsMalformedQuoteRowsAndContinues()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.ErrorMode = CsvErrorMode.SkipBadRow;
        CsvReader reader = CsvReader.Create(new StringReader("\"bad\"x,1\nok,2"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "ok", "2" }));
        Assert.That(reader.GetCurrentRow().RowNumber, Is.EqualTo(2));
        Assert.That(reader.HasParseError, Is.False);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Code, Is.EqualTo("MalformedQuote"));
        Assert.That(reader.GetRejects().Count, Is.EqualTo(0));
    }

    [Test]
    public void Read_SkipBadRowSkipsRowsThatExceedColumnLimits()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.ErrorMode = CsvErrorMode.SkipBadRow;
        options.MaxColumnCount = 2;
        CsvReader reader = CsvReader.Create(new StringReader("a,b,c\nok,1"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "ok", "1" }));
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Code, Is.EqualTo("MaxColumnCount"));
        Assert.That(reader.GetRejects().Count, Is.EqualTo(0));
    }

    [Test]
    public void Read_CollectRejectsStoresMalformedQuoteRowsAndContinues()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.ErrorMode = CsvErrorMode.CollectRejects;
        CsvReader reader = CsvReader.Create(new StringReader("\"bad\"x,1\nok,2"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "ok", "2" }));
        Assert.That(reader.HasParseError, Is.False);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(1));
        Assert.That(reader.GetRejects().Count, Is.EqualTo(1));
        Assert.That(reader.GetRejects()[0].RawText, Is.EqualTo("\"bad\"x,1\n"));
        Assert.That(reader.GetRejects()[0].Diagnostics.Get(0).Code, Is.EqualTo("MalformedQuote"));
    }

    [Test]
    public void Read_CollectRejectsStoresRowsThatExceedFieldLimits()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.ErrorMode = CsvErrorMode.CollectRejects;
        options.MaxFieldLength = 3;
        CsvReader reader = CsvReader.Create(new StringReader("abcd,skip\nok,1"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "ok", "1" }));
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Code, Is.EqualTo("MaxFieldLength"));
        Assert.That(reader.GetRejects().Count, Is.EqualTo(1));
        Assert.That(reader.GetRejects()[0].RawText, Is.EqualTo("abcd,skip\n"));
        Assert.That(reader.GetRejects()[0].Diagnostics.Get(0).Code, Is.EqualTo("MaxFieldLength"));
    }
}
