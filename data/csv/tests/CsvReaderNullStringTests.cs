using System.IO;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvReaderNullStringTests
{
    [Test]
    public void Read_DefaultModeKeepsEmptyAndNullLikeTextAsStrings()
    {
        CsvReader reader = CsvReader.Create(new StringReader(",NULL,\"NULL\""), CsvReaderOptions.CreateDefault());

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields[0], Is.EqualTo(""));
        Assert.That(reader.GetCurrentRow().Fields[1], Is.EqualTo("NULL"));
        Assert.That(reader.GetCurrentRow().Fields[2], Is.EqualTo("NULL"));
    }

    [Test]
    public void Read_ConfiguredNullStringConvertsMatchingFieldsToNull()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HasNullString = true;
        options.NullString = "NULL";
        CsvReader reader = CsvReader.Create(new StringReader(",NULL,\"NULL\""), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields[0], Is.EqualTo(""));
        Assert.That(reader.GetCurrentRow().Fields[1], Is.Null);
        Assert.That(reader.GetCurrentRow().Fields[2], Is.Null);
        Assert.That(reader.GetField(1), Is.Null);
    }

    [Test]
    public void Read_TrimModeAppliesBeforeNullStringRecognition()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.TrimMode = CsvTrimMode.Both;
        options.HasNullString = true;
        options.NullString = "NULL";
        CsvReader reader = CsvReader.Create(new StringReader(" NULL , value "), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields[0], Is.Null);
        Assert.That(reader.GetCurrentRow().Fields[1], Is.EqualTo("value"));
    }

    [Test]
    public void Read_EmptyNullStringOnlyConvertsEmptyWhenEnabled()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HasNullString = true;
        options.NullString = "";
        CsvReader reader = CsvReader.Create(new StringReader(","), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields[0], Is.Null);
        Assert.That(reader.GetCurrentRow().Fields[1], Is.Null);
    }

    [Test]
    public void ReadHeader_TreatsNullHeaderFieldsAsMissingHeaders()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HeaderOptions.HasHeader = true;
        options.HasNullString = true;
        options.NullString = "NULL";
        CsvReader reader = CsvReader.Create(new StringReader("A,NULL\n1,2"), options);

        Assert.That(reader.ReadHeader(), Is.True);
        Assert.That(reader.GetHeaders().Names, Is.EqualTo(new string[] { "A", "Column2" }));
    }
}
