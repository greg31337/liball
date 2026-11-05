using System.IO;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvReaderRecoveryModeTests
{
    [Test]
    public void Read_SkipBadRowSkipsMalformedRowAndContinues()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.ErrorMode = CsvErrorMode.SkipBadRow;
        CsvReader reader = CsvReader.Create(new StringReader("good,1\r\nbad\"row,2\r\nnext,3"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "good", "1" }));
        Assert.That(reader.GetCurrentRow().RowNumber, Is.EqualTo(1));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "next", "3" }));
        Assert.That(reader.GetCurrentRow().RowNumber, Is.EqualTo(3));
        Assert.That(reader.HasParseError, Is.False);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Code, Is.EqualTo("MalformedQuote"));
        Assert.That(reader.GetDiagnostics().Get(0).Row, Is.EqualTo(2));
        Assert.That(reader.GetRejects().Count, Is.EqualTo(0));
        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void Read_SkipBadRowSkipsRowsThatExceedLimits()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.ErrorMode = CsvErrorMode.SkipBadRow;
        options.MaxFieldLength = 3;
        CsvReader reader = CsvReader.Create(new StringReader("abcd,skip\nok,1"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "ok", "1" }));
        Assert.That(reader.GetCurrentRow().RowNumber, Is.EqualTo(2));
        Assert.That(reader.HasParseError, Is.False);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Code, Is.EqualTo("MaxFieldLength"));
    }

    [Test]
    public void Read_CollectRejectsStoresRejectedRowsAndContinues()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.ErrorMode = CsvErrorMode.CollectRejects;
        CsvReader reader = CsvReader.Create(new StringReader("bad\"row,2\nnext,3"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "next", "3" }));
        Assert.That(reader.GetCurrentRow().RowNumber, Is.EqualTo(2));
        Assert.That(reader.HasParseError, Is.False);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(1));
        Assert.That(reader.GetRejects().Count, Is.EqualTo(1));

        CsvRow reject = reader.GetRejects()[0];
        Assert.That(reject.RowNumber, Is.EqualTo(1));
        Assert.That(reject.RawText, Is.EqualTo("bad\"row,2\n"));
        Assert.That(reject.Diagnostics.Count(), Is.EqualTo(1));
        Assert.That(reject.Diagnostics.Get(0).Code, Is.EqualTo("MalformedQuote"));
        Assert.That(reject.Diagnostics.Get(0).Row, Is.EqualTo(1));
    }

    [Test]
    public void Read_CollectRejectsStoresUnterminatedFinalRow()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.ErrorMode = CsvErrorMode.CollectRejects;
        CsvReader reader = CsvReader.Create(new StringReader("\"bad,last"), options);

        Assert.That(reader.Read(), Is.False);
        Assert.That(reader.HasParseError, Is.False);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(1));
        Assert.That(reader.GetRejects().Count, Is.EqualTo(1));
        Assert.That(reader.GetRejects()[0].RowNumber, Is.EqualTo(1));
        Assert.That(reader.GetRejects()[0].RawText, Is.EqualTo("\"bad,last"));
        Assert.That(reader.GetRejects()[0].Diagnostics.Get(0).Code, Is.EqualTo("UnterminatedQuote"));
    }
}
