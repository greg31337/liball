using System.IO;
using System.Text;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvByteOffsetTests
{
    [Test]
    public void Read_StreamReaderTracksUtf8OffsetsIndependentlyFromCharacterOffsets()
    {
        CsvReader reader = CreateStreamBackedReader("😀,a\né,b\n", new UTF8Encoding(false), false);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().CharacterOffset, Is.EqualTo(0));
        Assert.That(reader.GetCurrentRow().ByteOffset, Is.EqualTo(0));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().CharacterOffset, Is.EqualTo(5));
        Assert.That(reader.GetCurrentRow().ByteOffset, Is.EqualTo(7));
    }

    [Test]
    public void Read_StreamReaderIncludesUtf8PreambleInByteOffsets()
    {
        CsvReader reader = CreateStreamBackedReader("é\nx\n", new UTF8Encoding(true), true);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().CharacterOffset, Is.EqualTo(0));
        Assert.That(reader.GetCurrentRow().ByteOffset, Is.EqualTo(3));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().CharacterOffset, Is.EqualTo(2));
        Assert.That(reader.GetCurrentRow().ByteOffset, Is.EqualTo(6));
    }

    [Test]
    public void Read_StreamReaderIncludesUtf16PreambleAndCodeUnitWidths()
    {
        CsvReader reader = CreateStreamBackedReader("é\nx\n", Encoding.Unicode, true);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().ByteOffset, Is.EqualTo(2));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().CharacterOffset, Is.EqualTo(2));
        Assert.That(reader.GetCurrentRow().ByteOffset, Is.EqualTo(6));
    }

    [Test]
    public void Read_ParserDiagnosticsExposeExactStreamByteOffset()
    {
        CsvReader reader = CreateStreamBackedReader("é\"x\n", new UTF8Encoding(false), false);

        Assert.That(reader.Read(), Is.False);
        Assert.That(reader.GetDiagnostics().Items, Has.Count.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Items[0].CharacterOffset, Is.EqualTo(2));
        Assert.That(reader.GetDiagnostics().Items[0].ByteOffset, Is.EqualTo(3));
    }

    [Test]
    public void Read_ArbitraryTextReaderReportsByteOffsetsAsUnavailable()
    {
        CsvReader rowReader = new CsvReader(new StringReader("a,b\n"), new CsvReaderOptions());

        Assert.That(rowReader.Read(), Is.True);
        Assert.That(rowReader.GetCurrentRow().CharacterOffset, Is.EqualTo(0));
        Assert.That(rowReader.GetCurrentRow().ByteOffset, Is.EqualTo(-1));

        CsvReader errorReader = new CsvReader(new StringReader("a\"b\n"), new CsvReaderOptions());

        Assert.That(errorReader.Read(), Is.False);
        Assert.That(errorReader.GetDiagnostics().Items[0].ByteOffset, Is.EqualTo(-1));
    }

    [Test]
    public void Convert_ResultDiagnosticsRetainTheSourceRowByteOffset()
    {
        CsvReader reader = CreateStreamBackedReader("ok\nbad\n", new UTF8Encoding(false), false);
        ICsvConverter converter = new CsvConverterService();

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.Read(), Is.True);
        CsvConversionResult result = converter.ConvertInt32(reader.GetCurrentRow(), 0, new CsvConversionOptions());

        Assert.That(reader.GetCurrentRow().ByteOffset, Is.EqualTo(3));
        Assert.That(result.Diagnostics.Items[0].ByteOffset, Is.EqualTo(3));
    }

    static CsvReader CreateStreamBackedReader(string text, Encoding encoding, bool includePreamble)
    {
        byte[] content = encoding.GetBytes(text);
        byte[] preamble = new byte[0];
        if (includePreamble)
        {
            preamble = encoding.GetPreamble();
        }

        byte[] bytes = new byte[preamble.Length + content.Length];
        preamble.CopyTo(bytes, 0);
        content.CopyTo(bytes, preamble.Length);
        MemoryStream stream = new MemoryStream(bytes);
        StreamReader textReader = new StreamReader(stream, new UTF8Encoding(false), true);
        CsvReader reader = new CsvReader(textReader, new CsvReaderOptions());
        return reader;
    }
}
