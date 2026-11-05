using System.IO;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvReaderLineHandlingTests
{
    [Test]
    public void Read_SkipBlankLinesSkipsEmptyPhysicalLines()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.SkipBlankLines = true;
        CsvReader reader = CsvReader.Create(new StringReader("\r\n\nfirst,1\r\n\r\nsecond,2"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "first", "1" }));
        Assert.That(reader.GetCurrentRow().RowNumber, Is.EqualTo(1));
        Assert.That(reader.GetCurrentRow().Line, Is.EqualTo(3));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "second", "2" }));
        Assert.That(reader.GetCurrentRow().RowNumber, Is.EqualTo(2));
        Assert.That(reader.GetCurrentRow().Line, Is.EqualTo(5));

        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void Read_DefaultModeKeepsCommentMarkerAsData()
    {
        CsvReader reader = CsvReader.Create(new StringReader("#not comment\nx"), CsvReaderOptions.CreateDefault());

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "#not comment" }));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "x" }));
    }

    [Test]
    public void Read_AllowCommentsSkipsCommentLines()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.AllowComments = true;
        options.Dialect.HasComment = true;
        options.Dialect.Comment = '#';
        CsvReader reader = CsvReader.Create(new StringReader("#comment\r\na,b\n#skip\nc,d"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "a", "b" }));
        Assert.That(reader.GetCurrentRow().RowNumber, Is.EqualTo(1));
        Assert.That(reader.GetCurrentRow().Line, Is.EqualTo(2));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "c", "d" }));
        Assert.That(reader.GetCurrentRow().RowNumber, Is.EqualTo(2));
        Assert.That(reader.GetCurrentRow().Line, Is.EqualTo(4));

        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void Read_SkipInitialSpaceSkipsSpacesAtFieldStarts()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.SkipInitialSpace = true;
        CsvReader reader = CsvReader.Create(new StringReader(" a,  \"b,c\",   d"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "a", "b,c", "d" }));
        Assert.That(reader.GetCurrentRow().RawText, Is.EqualTo(" a,  \"b,c\",   d"));
    }

    [Test]
    public void Read_DialectSkipInitialSpaceSkipsSpacesAtFieldStarts()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.Dialect.SkipInitialSpace = true;
        CsvReader reader = CsvReader.Create(new StringReader("a, b"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "a", "b" }));
    }

    [Test]
    public void Read_DefaultModeKeepsInitialSpaces()
    {
        CsvReader reader = CsvReader.Create(new StringReader(" a, b"), CsvReaderOptions.CreateDefault());

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { " a", " b" }));
    }
}
