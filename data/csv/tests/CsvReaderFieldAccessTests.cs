using System.IO;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvReaderFieldAccessTests
{
    [Test]
    public void Read_ProvidesIndexBasedFieldAccessHelpers()
    {
        CsvReader reader = CsvReader.Create(new StringReader("alpha,beta"), CsvReaderOptions.CreateDefault());

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.CountFields(), Is.EqualTo(2));
        Assert.That(reader.HasField(0), Is.True);
        Assert.That(reader.HasField(1), Is.True);
        Assert.That(reader.HasField(2), Is.False);
        Assert.That(reader.GetField(0), Is.EqualTo("alpha"));
        Assert.That(reader.GetField(1), Is.EqualTo("beta"));

        reader.SetField(1, "changed");

        Assert.That(reader.GetField(1), Is.EqualTo("changed"));
    }

    [Test]
    public void Read_ProvidesNameBasedFieldAccessHelpers()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HeaderOptions.HasHeader = true;
        CsvReader reader = CsvReader.Create(new StringReader("First,Last\nAda,Lovelace"), options);

        Assert.That(reader.ReadHeader(), Is.True);
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.HasFieldByName("First"), Is.True);
        Assert.That(reader.HasFieldByName("Missing"), Is.False);
        Assert.That(reader.GetFieldByName("First"), Is.EqualTo("Ada"));
        Assert.That(reader.GetFieldByName("Last"), Is.EqualTo("Lovelace"));

        reader.SetFieldByName("Last", "Byron");

        Assert.That(reader.GetFieldByName("Last"), Is.EqualTo("Byron"));
        Assert.That(reader.GetField(1), Is.EqualTo("Byron"));
    }

    [Test]
    public void Read_NameBasedAccessWorksWithGeneratedHeaders()
    {
        CsvReader reader = CsvReader.Create(new StringReader("x,y"), CsvReaderOptions.CreateDefault());

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.HasFieldByName("Column1"), Is.True);
        Assert.That(reader.HasFieldByName("Column3"), Is.False);
        Assert.That(reader.GetFieldByName("Column2"), Is.EqualTo("y"));
    }
}
