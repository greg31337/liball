using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class SignificantBitsTests
{
    [Test]
    public void ParseGrayscale_ReadsSingleByteUsingOffset()
    {
        PngSignificantBits bits = PngSignificantBitsParser.ParseGrayscale(new byte[] { 99, 5, 88 }, 1, 1);

        Assert.That(bits.Gray, Is.EqualTo(5));
        Assert.That(bits.Red, Is.EqualTo(0));
        Assert.That(bits.Green, Is.EqualTo(0));
        Assert.That(bits.Blue, Is.EqualTo(0));
        Assert.That(bits.Alpha, Is.EqualTo(0));
    }

    [Test]
    public void ParseGrayscaleAlpha_ReadsGrayAndAlpha()
    {
        PngSignificantBits bits = PngSignificantBitsParser.ParseGrayscaleAlpha(new byte[] { 6, 4 });

        Assert.That(bits.Gray, Is.EqualTo(6));
        Assert.That(bits.Alpha, Is.EqualTo(4));
    }

    [Test]
    public void ParseTruecolor_ReadsRgb()
    {
        PngSignificantBits bits = PngSignificantBitsParser.ParseTruecolor(new byte[] { 5, 6, 7 });

        Assert.That(bits.Red, Is.EqualTo(5));
        Assert.That(bits.Green, Is.EqualTo(6));
        Assert.That(bits.Blue, Is.EqualTo(7));
    }

    [Test]
    public void ParseIndexed_UsesRgbPayload()
    {
        PngSignificantBits bits = PngSignificantBitsParser.ParseIndexed(new byte[] { 3, 4, 5 });

        Assert.That(bits.Red, Is.EqualTo(3));
        Assert.That(bits.Green, Is.EqualTo(4));
        Assert.That(bits.Blue, Is.EqualTo(5));
    }

    [Test]
    public void ParseTruecolorAlpha_ReadsChunkPayload()
    {
        PngChunk chunk = CreateChunk(PngConstants.ChunkSignificantBits, new byte[] { 5, 6, 7, 8 });

        PngSignificantBits bits = PngSignificantBitsParser.ParseTruecolorAlpha(chunk);

        Assert.That(bits.Red, Is.EqualTo(5));
        Assert.That(bits.Green, Is.EqualTo(6));
        Assert.That(bits.Blue, Is.EqualTo(7));
        Assert.That(bits.Alpha, Is.EqualTo(8));
    }

    [Test]
    public void WriteData_WritesEachPayloadShape()
    {
        PngSignificantBits bits = CreateBits();

        Assert.That(PngSignificantBitsWriter.WriteGrayscaleData(bits), Is.EqualTo(new byte[] { 3 }));
        Assert.That(PngSignificantBitsWriter.WriteGrayscaleAlphaData(bits), Is.EqualTo(new byte[] { 3, 8 }));
        Assert.That(PngSignificantBitsWriter.WriteTruecolorData(bits), Is.EqualTo(new byte[] { 5, 6, 7 }));
        Assert.That(PngSignificantBitsWriter.WriteIndexedData(bits), Is.EqualTo(new byte[] { 5, 6, 7 }));
        Assert.That(PngSignificantBitsWriter.WriteTruecolorAlphaData(bits), Is.EqualTo(new byte[] { 5, 6, 7, 8 }));
    }

    [Test]
    public void WriteData_WritesAtOffset()
    {
        PngSignificantBits bits = CreateBits();
        byte[] data = new byte[] { 99, 99, 99, 99, 99 };

        PngSignificantBitsWriter.WriteTruecolorData(data, 1, bits);

        Assert.That(data, Is.EqualTo(new byte[] { 99, 5, 6, 7, 99 }));
    }

    [Test]
    public void WriteChunk_CreatesKnownSbitChunk()
    {
        PngSignificantBits bits = CreateBits();

        PngChunk chunk = PngSignificantBitsWriter.WriteTruecolorAlphaChunk(bits);

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkSignificantBits));
        Assert.That(chunk.Data, Is.EqualTo(new byte[] { 5, 6, 7, 8 }));
        Assert.That(chunk.Length, Is.EqualTo(4));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.False);
    }

    [Test]
    public void WriteChunk_RoundTripsThroughRawChunkWriter()
    {
        PngChunk chunk = PngSignificantBitsWriter.WriteGrayscaleAlphaChunk(CreateBits());

        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngSignificantBits bits = PngSignificantBitsParser.ParseGrayscaleAlpha(readChunk);

        Assert.That(bits.Gray, Is.EqualTo(3));
        Assert.That(bits.Alpha, Is.EqualTo(8));
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkSignificantBits, readChunk.Data)));
    }

    [Test]
    public void Parser_RejectsInvalidInputs()
    {
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = new byte[] { 5 };
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkSignificantBits;
        nullData.Data = null!;
        PngChunk wrongType = CreateChunk(PngConstants.ChunkTransparency, new byte[] { 5 });

        Assert.Throws<ArgumentNullException>(() => PngSignificantBitsParser.ParseGrayscale((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngSignificantBitsParser.ParseGrayscale(null!, 0, 1));
        Assert.Throws<ArgumentNullException>(() => PngSignificantBitsParser.ParseGrayscale((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngSignificantBitsParser.ParseGrayscale(nullType));
        Assert.Throws<ArgumentNullException>(() => PngSignificantBitsParser.ParseGrayscale(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSignificantBitsParser.ParseGrayscale(new byte[1], -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSignificantBitsParser.ParseGrayscale(new byte[1], 2, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSignificantBitsParser.ParseGrayscale(new byte[1], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSignificantBitsParser.ParseGrayscale(new byte[1], 1, 1));
        Assert.Throws<InvalidDataException>(() => PngSignificantBitsParser.ParseGrayscale(new byte[0]));
        Assert.Throws<InvalidDataException>(() => PngSignificantBitsParser.ParseGrayscale(new byte[] { 1, 2 }));
        Assert.Throws<InvalidDataException>(() => PngSignificantBitsParser.ParseTruecolor(new byte[] { 1, 2 }));
        Assert.Throws<InvalidDataException>(() => PngSignificantBitsParser.ParseTruecolorAlpha(new byte[] { 1, 2, 3 }));
        Assert.Throws<InvalidDataException>(() => PngSignificantBitsParser.ParseGrayscale(wrongType));
    }

    [Test]
    public void Writer_RejectsInvalidInputs()
    {
        PngSignificantBits bits = CreateBits();

        Assert.Throws<ArgumentNullException>(() => PngSignificantBitsWriter.WriteGrayscaleData(null!));
        Assert.Throws<ArgumentNullException>(() => PngSignificantBitsWriter.WriteGrayscaleData(null!, 0, bits));
        Assert.Throws<ArgumentNullException>(() => PngSignificantBitsWriter.WriteGrayscaleData(new byte[1], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngSignificantBitsWriter.WriteTruecolorAlphaChunk(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSignificantBitsWriter.WriteTruecolorData(new byte[3], -1, bits));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSignificantBitsWriter.WriteTruecolorData(new byte[3], 1, bits));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSignificantBitsWriter.WriteTruecolorAlphaData(new byte[4], 1, bits));
    }

    static PngSignificantBits CreateBits()
    {
        PngSignificantBits bits = new PngSignificantBits();
        bits.Gray = 3;
        bits.Red = 5;
        bits.Green = 6;
        bits.Blue = 7;
        bits.Alpha = 8;
        return bits;
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
}
