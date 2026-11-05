using System.IO;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvReaderTrimModeTests
{
    [Test]
    public void Read_TrimModeNonePreservesWhitespace()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.TrimMode = CsvTrimMode.None;
        CsvReader reader = CsvReader.Create(new StringReader(" a ,\" b \""), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { " a ", " b " }));
    }

    [Test]
    public void Read_TrimModeUnquotedTrimsOnlyUnquotedFields()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.TrimMode = CsvTrimMode.Unquoted;
        CsvReader reader = CsvReader.Create(new StringReader(" a ,\" b \""), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "a", " b " }));
    }

    [Test]
    public void Read_TrimModeQuotedAndUnquotedTrimsAllFields()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.TrimMode = CsvTrimMode.QuotedAndUnquoted;
        CsvReader reader = CsvReader.Create(new StringReader(" a ,\" b \""), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "a", "b" }));
    }

    [Test]
    public void Read_TrimModeLeftTrimsLeftSideOfAllFields()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.TrimMode = CsvTrimMode.Left;
        CsvReader reader = CsvReader.Create(new StringReader(" a ,\" b \""), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "a ", "b " }));
    }

    [Test]
    public void Read_TrimModeRightTrimsRightSideOfAllFields()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.TrimMode = CsvTrimMode.Right;
        CsvReader reader = CsvReader.Create(new StringReader(" a ,\" b \""), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { " a", " b" }));
    }

    [Test]
    public void Read_TrimModeBothTrimsBothSidesOfAllFields()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.TrimMode = CsvTrimMode.Both;
        CsvReader reader = CsvReader.Create(new StringReader(" a ,\" b \""), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "a", "b" }));
    }
}
