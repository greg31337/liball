using System.IO;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvReaderHeaderTests
{
    [Test]
    public void ReadHeader_ReadsHeaderRowAndAttachesItToFollowingRows()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HeaderOptions.HasHeader = true;
        CsvReader reader = CsvReader.Create(new StringReader("Name,Age\nAda,37"), options);

        Assert.That(reader.ReadHeader(), Is.True);
        Assert.That(reader.GetHeaders().Names, Is.EqualTo(new string[] { "Name", "Age" }));
        Assert.That(reader.GetCurrentRow().Header, Is.SameAs(reader.GetHeaders()));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "Ada", "37" }));
        Assert.That(reader.GetCurrentRow().Header, Is.SameAs(reader.GetHeaders()));
        Assert.That(reader.GetFieldByName("Age"), Is.EqualTo("37"));
    }

    [Test]
    public void Read_GeneratesColumnNamesWhenNoHeaderExists()
    {
        CsvReader reader = CsvReader.Create(new StringReader("Ada,37"), CsvReaderOptions.CreateDefault());

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetHeaders().Names, Is.EqualTo(new string[] { "Column1", "Column2" }));
        Assert.That(reader.GetCurrentRow().Header, Is.SameAs(reader.GetHeaders()));
        Assert.That(reader.GetFieldByName("Column1"), Is.EqualTo("Ada"));
        Assert.That(reader.GetFieldByName("Column2"), Is.EqualTo("37"));
    }

    [Test]
    public void Read_UsesConfiguredGeneratedHeaderPrefix()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HeaderOptions.GeneratedHeaderPrefix = "Field";
        CsvReader reader = CsvReader.Create(new StringReader("a,b,c"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetHeaders().Names, Is.EqualTo(new string[] { "Field1", "Field2", "Field3" }));
    }

    [Test]
    public void Read_DoesNotGenerateHeadersWhenDisabled()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HeaderOptions.GenerateHeaders = false;
        CsvReader reader = CsvReader.Create(new StringReader("a,b"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetHeaders().Names, Is.Empty);
        Assert.That(reader.GetCurrentRow().Header.Names, Is.Empty);
    }

    [Test]
    public void ReadHeader_UsesNormalizedCaseInsensitiveLookupWhenConfigured()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HeaderOptions.HasHeader = true;
        options.HeaderOptions.NormalizeHeaderNames = true;
        options.HeaderOptions.CaseSensitive = false;
        CsvReader reader = CsvReader.Create(new StringReader(" Name ,AGE\nAda,37"), options);

        Assert.That(reader.ReadHeader(), Is.True);
        Assert.That(reader.GetHeaders().Names, Is.EqualTo(new string[] { "Name", "AGE" }));
        Assert.That(reader.GetHeaderIndex(" name "), Is.EqualTo(0));
        Assert.That(reader.GetHeaderIndex("age"), Is.EqualTo(1));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetFieldByName(" NAME "), Is.EqualTo("Ada"));
        Assert.That(reader.GetFieldByName("age"), Is.EqualTo("37"));
    }

    [Test]
    public void ReadHeader_DefaultLookupIsCaseSensitive()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HeaderOptions.HasHeader = true;
        CsvReader reader = CsvReader.Create(new StringReader("Name,Age\nAda,37"), options);

        Assert.That(reader.ReadHeader(), Is.True);
        Assert.That(reader.GetHeaderIndex("Name"), Is.EqualTo(0));
        Assert.That(reader.GetHeaderIndex("name"), Is.EqualTo(-1));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetFieldByName("Name"), Is.EqualTo("Ada"));
        Assert.That(reader.GetFieldByName("name"), Is.EqualTo(""));
    }
}
