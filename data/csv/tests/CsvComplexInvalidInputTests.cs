using System.IO;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvComplexInvalidInputTests
{
    [Test]
    public void Read_CollectRejectsStoresMalformedMultilineQuotedRowAndContinuesWithHeader()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HeaderOptions.HasHeader = true;
        options.AllowComments = true;
        options.Dialect.HasComment = true;
        options.Dialect.Comment = '#';
        options.ErrorMode = CsvErrorMode.CollectRejects;
        CsvReader reader = CsvReader.Create(new StringReader("# source\r\nId,Text,Amount\r\n1,ok,10\r\n2,\"bad\r\nquote\"x,20\r\n3,after,30"), options);

        Assert.That(reader.ReadHeader(), Is.True);
        Assert.That(reader.GetHeaders().Names, Is.EqualTo(new string[] { "Id", "Text", "Amount" }));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "1", "ok", "10" }));
        Assert.That(reader.GetCurrentRow().RowNumber, Is.EqualTo(2));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "3", "after", "30" }));
        Assert.That(reader.GetCurrentRow().RowNumber, Is.EqualTo(4));
        Assert.That(reader.GetCurrentRow().Line, Is.EqualTo(6));

        Assert.That(reader.Read(), Is.False);
        Assert.That(reader.HasParseError, Is.False);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Code, Is.EqualTo("MalformedQuote"));
        Assert.That(reader.GetDiagnostics().Get(0).Row, Is.EqualTo(3));
        Assert.That(reader.GetDiagnostics().Get(0).Column, Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Line, Is.EqualTo(5));
        Assert.That(reader.GetRejects().Count, Is.EqualTo(1));
        Assert.That(reader.GetRejects()[0].Header, Is.SameAs(reader.GetHeaders()));
        Assert.That(reader.GetRejects()[0].RowNumber, Is.EqualTo(3));
        Assert.That(reader.GetRejects()[0].Line, Is.EqualTo(4));
        Assert.That(reader.GetRejects()[0].RawText, Is.EqualTo("2,\"bad\r\nquote\"x,20\r\n"));
        Assert.That(reader.GetRejects()[0].Diagnostics.Get(0).Code, Is.EqualTo("MalformedQuote"));
    }

    [Test]
    public void Read_SkipBadRowSkipsMultipleLimitFailuresAroundCommentsAndBlankLines()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.AllowComments = true;
        options.Dialect.HasComment = true;
        options.Dialect.Comment = '#';
        options.SkipBlankLines = true;
        options.ErrorMode = CsvErrorMode.SkipBadRow;
        options.MaxFieldLength = 6;
        options.MaxColumnCount = 2;
        CsvReader reader = CsvReader.Create(new StringReader("ok,1\n# skipped\nlengthy,row\n\nalso,too,many\nlast,2"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "ok", "1" }));
        Assert.That(reader.GetCurrentRow().RowNumber, Is.EqualTo(1));
        Assert.That(reader.GetCurrentRow().Line, Is.EqualTo(1));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "last", "2" }));
        Assert.That(reader.GetCurrentRow().RowNumber, Is.EqualTo(4));
        Assert.That(reader.GetCurrentRow().Line, Is.EqualTo(6));

        Assert.That(reader.Read(), Is.False);
        Assert.That(reader.HasParseError, Is.False);
        Assert.That(reader.GetRejects().Count, Is.EqualTo(0));
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(2));
        Assert.That(reader.GetDiagnostics().Get(0).Code, Is.EqualTo("MaxFieldLength"));
        Assert.That(reader.GetDiagnostics().Get(0).Row, Is.EqualTo(2));
        Assert.That(reader.GetDiagnostics().Get(0).Line, Is.EqualTo(3));
        Assert.That(reader.GetDiagnostics().Get(1).Code, Is.EqualTo("MaxColumnCount"));
        Assert.That(reader.GetDiagnostics().Get(1).Row, Is.EqualTo(3));
        Assert.That(reader.GetDiagnostics().Get(1).Line, Is.EqualTo(6));
    }

    [Test]
    public void Read_StrictModeReportsMalformedQuotedFieldAfterEmbeddedNewLineWithHeaderContext()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HeaderOptions.HasHeader = true;
        CsvReader reader = CsvReader.Create(new StringReader("Name,Note\nAda,\"line\nquote\"suffix,extra\nGrace,ok"), options);

        Assert.That(reader.ReadHeader(), Is.True);

        Assert.That(reader.Read(), Is.False);
        Assert.That(reader.HasParseError, Is.True);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Code, Is.EqualTo("MalformedQuote"));
        Assert.That(reader.GetDiagnostics().Get(0).Row, Is.EqualTo(2));
        Assert.That(reader.GetDiagnostics().Get(0).Column, Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Line, Is.EqualTo(3));
        Assert.That(reader.GetDiagnostics().Get(0).RawContext, Is.EqualTo("Ada,\"line\nquote\"s"));

        Assert.That(reader.Read(), Is.False);
    }
}
