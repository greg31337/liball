using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PaletteHistogramTests
{
    [Test]
    public void Parse_ReadsBigEndianFrequenciesUsingOffset()
    {
        byte[] data = new byte[] { 99, 0x00, 0x01, 0x12, 0x34, 0xFF, 0xFF, 88 };

        PngPaletteHistogram histogram = PngPaletteHistogramParser.Parse(data, 1, 6);

        AssertHistogram(histogram);
    }

    [Test]
    public void Parse_ReadsChunkPayload()
    {
        PngChunk chunk = CreateChunk(PngConstants.ChunkPaletteHistogram, CreatePayload());

        PngPaletteHistogram histogram = PngPaletteHistogramParser.Parse(chunk);

        AssertHistogram(histogram);
    }

    [Test]
    public void WriteData_WritesBigEndianFrequencies()
    {
        byte[] data = PngPaletteHistogramWriter.WriteData(CreateHistogram());

        Assert.That(data, Is.EqualTo(CreatePayload()));
    }

    [Test]
    public void WriteData_WritesAtOffset()
    {
        byte[] data = new byte[] { 99, 0, 0, 0, 0, 0, 0, 88 };

        PngPaletteHistogramWriter.WriteData(data, 1, CreateHistogram());

        Assert.That(data, Is.EqualTo(new byte[] { 99, 0x00, 0x01, 0x12, 0x34, 0xFF, 0xFF, 88 }));
    }

    [Test]
    public void WriteChunk_CreatesKnownHistChunk()
    {
        PngChunk chunk = PngPaletteHistogramWriter.WriteChunk(CreateHistogram());

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkPaletteHistogram));
        Assert.That(chunk.Data, Is.EqualTo(CreatePayload()));
        Assert.That(chunk.Length, Is.EqualTo(6));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.False);
    }

    [Test]
    public void WriteChunk_RoundTripsThroughRawChunkWriter()
    {
        PngChunk chunk = PngPaletteHistogramWriter.WriteChunk(CreateHistogram());

        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngPaletteHistogram histogram = PngPaletteHistogramParser.Parse(readChunk);

        AssertHistogram(histogram);
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkPaletteHistogram, readChunk.Data)));
    }

    [Test]
    public void Parser_RejectsInvalidInputs()
    {
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = CreatePayload();
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkPaletteHistogram;
        nullData.Data = null!;
        PngChunk wrongType = CreateChunk(PngConstants.ChunkGamma, CreatePayload());

        Assert.Throws<ArgumentNullException>(() => PngPaletteHistogramParser.Parse((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngPaletteHistogramParser.Parse(null!, 0, 2));
        Assert.Throws<ArgumentNullException>(() => PngPaletteHistogramParser.Parse((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngPaletteHistogramParser.Parse(nullType));
        Assert.Throws<ArgumentNullException>(() => PngPaletteHistogramParser.Parse(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteHistogramParser.Parse(new byte[2], -1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteHistogramParser.Parse(new byte[2], 3, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteHistogramParser.Parse(new byte[2], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteHistogramParser.Parse(new byte[2], 1, 2));
        Assert.Throws<InvalidDataException>(() => PngPaletteHistogramParser.Parse(new byte[0]));
        Assert.Throws<InvalidDataException>(() => PngPaletteHistogramParser.Parse(new byte[] { 1 }));
        Assert.Throws<InvalidDataException>(() => PngPaletteHistogramParser.Parse(new byte[514]));
        Assert.Throws<InvalidDataException>(() => PngPaletteHistogramParser.Parse(wrongType));
    }

    [Test]
    public void Writer_RejectsInvalidInputs()
    {
        PngPaletteHistogram histogram = CreateHistogram();
        PngPaletteHistogram nullFrequencies = new PngPaletteHistogram();
        nullFrequencies.Frequencies = null!;
        PngPaletteHistogram emptyFrequencies = CreateHistogram(new ushort[0]);
        PngPaletteHistogram tooManyFrequencies = CreateHistogram(new ushort[257]);

        Assert.Throws<ArgumentNullException>(() => PngPaletteHistogramWriter.WriteData(null!));
        Assert.Throws<ArgumentNullException>(() => PngPaletteHistogramWriter.WriteData(null!, 0, histogram));
        Assert.Throws<ArgumentNullException>(() => PngPaletteHistogramWriter.WriteData(new byte[6], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngPaletteHistogramWriter.WriteData(nullFrequencies));
        Assert.Throws<ArgumentNullException>(() => PngPaletteHistogramWriter.WriteChunk(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteHistogramWriter.WriteData(emptyFrequencies));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteHistogramWriter.WriteData(tooManyFrequencies));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteHistogramWriter.WriteData(new byte[6], -1, histogram));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteHistogramWriter.WriteData(new byte[6], 1, histogram));
    }

    static PngPaletteHistogram CreateHistogram()
    {
        return CreateHistogram(new ushort[] { 1, 0x1234, 0xFFFF });
    }

    static PngPaletteHistogram CreateHistogram(ushort[] frequencies)
    {
        PngPaletteHistogram histogram = new PngPaletteHistogram();
        histogram.Frequencies = frequencies;
        return histogram;
    }

    static byte[] CreatePayload()
    {
        return new byte[] { 0x00, 0x01, 0x12, 0x34, 0xFF, 0xFF };
    }

    static PngChunk CreateChunk(string type, byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = type;
        chunk.Data = data;
        chunk.Length = (uint)data.Length;
        chunk.IsKnown = true;
        return chunk;
    }

    static void AssertHistogram(PngPaletteHistogram histogram)
    {
        Assert.That(histogram.Frequencies, Has.Length.EqualTo(3));
        Assert.That(histogram.Frequencies[0], Is.EqualTo(1));
        Assert.That(histogram.Frequencies[1], Is.EqualTo(0x1234));
        Assert.That(histogram.Frequencies[2], Is.EqualTo(0xFFFF));
    }
}
