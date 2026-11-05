using System.IO;
using System.Reflection;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvReaderRfc4180Tests
{
    [Test]
    public void Read_ParsesUnquotedRows()
    {
        CsvReader reader = CsvReader.Create(new StringReader("a,b,c\r\n1,2,3"), CsvReaderOptions.CreateDefault());

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "a", "b", "c" }));
        Assert.That(reader.GetCurrentRow().RowNumber, Is.EqualTo(1));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "1", "2", "3" }));
        Assert.That(reader.GetCurrentRow().RowNumber, Is.EqualTo(2));

        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void Read_ParsesQuotedFieldsWithDelimitersNewlinesAndEscapedQuotes()
    {
        CsvReader reader = CsvReader.Create(new StringReader("\"a,b\",\"line 1\r\nline 2\",\"say \"\"hi\"\"\"\r\nz,y,x"), CsvReaderOptions.CreateDefault());

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetField(0), Is.EqualTo("a,b"));
        Assert.That(reader.GetField(1), Is.EqualTo("line 1\r\nline 2"));
        Assert.That(reader.GetField(2), Is.EqualTo("say \"hi\""));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "z", "y", "x" }));
    }

    [Test]
    public void ReadHeader_ParsesOptionalRfcHeaderRecord()
    {
        CsvReader reader = CsvReader.Create(new StringReader("Id,Name,Dept\r\n1,Ada,Eng\r\n2,Grace,Ops\r\n"), CsvReaderOptions.CreateDefault());

        Assert.That(reader.ReadHeader(), Is.True);
        Assert.That(reader.Header.Names, Is.EqualTo(new string[] { "Id", "Name", "Dept" }));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetFieldByName("Name"), Is.EqualTo("Ada"));
        Assert.That(reader.GetFieldByName("Dept"), Is.EqualTo("Eng"));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetFieldByName("Name"), Is.EqualTo("Grace"));
        Assert.That(reader.GetFieldByName("Dept"), Is.EqualTo("Ops"));

        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void Read_TreatsSpacesAsFieldData()
    {
        CsvReader reader = CsvReader.Create(new StringReader(" alpha ,\" beta \",gamma \r\n"), CsvReaderOptions.CreateDefault());

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { " alpha ", " beta ", "gamma " }));
        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void Read_ParsesCanonicalDoubledQuoteExample()
    {
        CsvReader reader = CsvReader.Create(new StringReader("\"aaa\",\"b\"\"bb\",\"ccc\"\r\n"), CsvReaderOptions.CreateDefault());

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "aaa", "b\"bb", "ccc" }));
        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void Read_CrLfAfterFinalRecordDoesNotCreateExtraRow()
    {
        CsvReader reader = CsvReader.Create(new StringReader("a,b,c\r\n"), CsvReaderOptions.CreateDefault());

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "a", "b", "c" }));
        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void WriteRow_EmitsRfcEscapingForCommasQuotesAndCrLf()
    {
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, CsvWriterOptions.CreateDefault());

        writer.WriteRow(CsvRow.Create(new string[] { "aaa", "b\"bb", "line\r\nbreak", "x,y" }, 1));

        Assert.That(output.ToString(), Is.EqualTo("aaa,\"b\"\"bb\",\"line\r\nbreak\",\"x,y\"\r\n"));
    }

    [Test]
    public void Read_PreservesEmptyFieldsAndRows()
    {
        CsvReader reader = CsvReader.Create(new StringReader(",\r\n\r\nx,,\"\""), CsvReaderOptions.CreateDefault());

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "", "" }));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "" }));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "x", "", "" }));

        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void Read_HandlesFinalRecordWithoutTrailingNewline()
    {
        CsvReader reader = CsvReader.Create(new StringReader("\"last\",field"), CsvReaderOptions.CreateDefault());

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "last", "field" }));
        Assert.That(reader.GetCurrentRow().RawText, Is.EqualTo("\"last\",field"));
        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void Read_ReturnsFalseForEmptyInput()
    {
        CsvReader reader = CsvReader.Create(new StringReader(""), CsvReaderOptions.CreateDefault());

        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void Read_TracksRowAndPhysicalPositions()
    {
        CsvReader reader = CsvReader.Create(new StringReader("a,b\r\n\"c\r\nd\",e\nz"), CsvReaderOptions.CreateDefault());

        Assert.That(reader.PhysicalLine, Is.EqualTo(1));
        Assert.That(reader.PhysicalColumn, Is.EqualTo(1));
        Assert.That(reader.CharacterOffset, Is.EqualTo(0));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().RowNumber, Is.EqualTo(1));
        Assert.That(reader.GetCurrentRow().Line, Is.EqualTo(1));
        Assert.That(reader.GetCurrentRow().Column, Is.EqualTo(1));
        Assert.That(reader.GetCurrentRow().CharacterOffset, Is.EqualTo(0));
        Assert.That(reader.CurrentFieldIndex, Is.EqualTo(1));
        Assert.That(reader.PhysicalLine, Is.EqualTo(2));
        Assert.That(reader.PhysicalColumn, Is.EqualTo(1));
        Assert.That(reader.CharacterOffset, Is.EqualTo(5));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().RowNumber, Is.EqualTo(2));
        Assert.That(reader.GetCurrentRow().Line, Is.EqualTo(2));
        Assert.That(reader.GetCurrentRow().Column, Is.EqualTo(1));
        Assert.That(reader.GetCurrentRow().CharacterOffset, Is.EqualTo(5));
        Assert.That(reader.CurrentFieldIndex, Is.EqualTo(1));
        Assert.That(reader.PhysicalLine, Is.EqualTo(4));
        Assert.That(reader.PhysicalColumn, Is.EqualTo(1));
        Assert.That(reader.CharacterOffset, Is.EqualTo(14));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().RowNumber, Is.EqualTo(3));
        Assert.That(reader.GetCurrentRow().Line, Is.EqualTo(4));
        Assert.That(reader.GetCurrentRow().Column, Is.EqualTo(1));
        Assert.That(reader.GetCurrentRow().CharacterOffset, Is.EqualTo(14));
        Assert.That(reader.CurrentFieldIndex, Is.EqualTo(0));
        Assert.That(reader.PhysicalLine, Is.EqualTo(4));
        Assert.That(reader.PhysicalColumn, Is.EqualTo(2));
        Assert.That(reader.CharacterOffset, Is.EqualTo(15));
    }

    [Test]
    public void Read_TracksCurrentFieldStartPosition()
    {
        CsvReader reader = CsvReader.Create(new StringReader("aa,bb,\"cc\""), CsvReaderOptions.CreateDefault());

        Assert.That(reader.Read(), Is.True);

        Assert.That(reader.CurrentFieldIndex, Is.EqualTo(2));
        Assert.That(reader.CurrentFieldLine, Is.EqualTo(1));
        Assert.That(reader.CurrentFieldColumn, Is.EqualTo(7));
        Assert.That(reader.CurrentFieldCharacterOffset, Is.EqualTo(6));
    }

    [Test]
    public void CsvReader_HidesMutableStateAndExposesNoProperties()
    {
        FieldInfo[] fields = typeof(CsvReader).GetFields(BindingFlags.Public | BindingFlags.Instance);
        PropertyInfo[] properties = typeof(CsvReader).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.That(fields, Is.Empty);
        Assert.That(properties, Is.Empty);
    }
}
