using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class BackgroundColorTests
{
    [Test]
    public void ParseIndexed_ReadsPaletteIndexUsingOffset()
    {
        PngBackgroundColor background = PngBackgroundColorParser.ParseIndexed(new byte[] { 99, 7, 88 }, 1, 1);

        Assert.That(background.PaletteIndex, Is.EqualTo(7));
        Assert.That(background.Gray, Is.EqualTo(0));
        Assert.That(background.Red, Is.EqualTo(0));
        Assert.That(background.Green, Is.EqualTo(0));
        Assert.That(background.Blue, Is.EqualTo(0));
    }

    [Test]
    public void ParseGrayscale_ReadsBigEndianGraySample()
    {
        PngBackgroundColor background = PngBackgroundColorParser.ParseGrayscale(new byte[] { 0x12, 0x34 });

        Assert.That(background.Gray, Is.EqualTo(0x1234));
    }

    [Test]
    public void ParseTruecolor_ReadsChunkPayload()
    {
        PngChunk chunk = CreateChunk(PngConstants.ChunkBackgroundColor, new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC });

        PngBackgroundColor background = PngBackgroundColorParser.ParseTruecolor(chunk);

        Assert.That(background.Red, Is.EqualTo(0x1234));
        Assert.That(background.Green, Is.EqualTo(0x5678));
        Assert.That(background.Blue, Is.EqualTo(0x9ABC));
    }

    [Test]
    public void WriteData_WritesEachPayloadShape()
    {
        PngBackgroundColor background = CreateBackground();

        Assert.That(PngBackgroundColorWriter.WriteIndexedData(background), Is.EqualTo(new byte[] { 7 }));
        Assert.That(PngBackgroundColorWriter.WriteGrayscaleData(background), Is.EqualTo(new byte[] { 0x12, 0x34 }));
        Assert.That(PngBackgroundColorWriter.WriteTruecolorData(background), Is.EqualTo(new byte[] { 0x22, 0x33, 0x44, 0x55, 0x66, 0x77 }));
    }

    [Test]
    public void WriteData_WritesAtOffset()
    {
        PngBackgroundColor background = CreateBackground();
        byte[] data = new byte[] { 99, 99, 99, 99, 99, 99, 99, 99 };

        PngBackgroundColorWriter.WriteTruecolorData(data, 1, background);

        Assert.That(data, Is.EqualTo(new byte[] { 99, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 99 }));
    }

    [Test]
    public void WriteChunk_CreatesKnownBkgdChunk()
    {
        PngBackgroundColor background = CreateBackground();

        PngChunk chunk = PngBackgroundColorWriter.WriteTruecolorChunk(background);

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkBackgroundColor));
        Assert.That(chunk.Data, Is.EqualTo(new byte[] { 0x22, 0x33, 0x44, 0x55, 0x66, 0x77 }));
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
        PngChunk chunk = PngBackgroundColorWriter.WriteGrayscaleChunk(CreateBackground());

        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngBackgroundColor background = PngBackgroundColorParser.ParseGrayscale(readChunk);

        Assert.That(background.Gray, Is.EqualTo(0x1234));
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkBackgroundColor, readChunk.Data)));
    }

    [Test]
    public void Parser_RejectsInvalidInputs()
    {
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = new byte[] { 7 };
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkBackgroundColor;
        nullData.Data = null!;
        PngChunk wrongType = CreateChunk(PngConstants.ChunkTransparency, new byte[] { 7 });

        Assert.Throws<ArgumentNullException>(() => PngBackgroundColorParser.ParseIndexed((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngBackgroundColorParser.ParseIndexed(null!, 0, 1));
        Assert.Throws<ArgumentNullException>(() => PngBackgroundColorParser.ParseIndexed((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngBackgroundColorParser.ParseIndexed(nullType));
        Assert.Throws<ArgumentNullException>(() => PngBackgroundColorParser.ParseIndexed(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngBackgroundColorParser.ParseIndexed(new byte[1], -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngBackgroundColorParser.ParseIndexed(new byte[1], 2, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngBackgroundColorParser.ParseIndexed(new byte[1], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngBackgroundColorParser.ParseIndexed(new byte[1], 1, 1));
        Assert.Throws<InvalidDataException>(() => PngBackgroundColorParser.ParseIndexed(new byte[0]));
        Assert.Throws<InvalidDataException>(() => PngBackgroundColorParser.ParseIndexed(new byte[] { 1, 2 }));
        Assert.Throws<InvalidDataException>(() => PngBackgroundColorParser.ParseGrayscale(new byte[] { 1 }));
        Assert.Throws<InvalidDataException>(() => PngBackgroundColorParser.ParseTruecolor(new byte[] { 1, 2, 3, 4, 5 }));
        Assert.Throws<InvalidDataException>(() => PngBackgroundColorParser.ParseIndexed(wrongType));
    }

    [Test]
    public void Writer_RejectsInvalidInputs()
    {
        PngBackgroundColor background = CreateBackground();

        Assert.Throws<ArgumentNullException>(() => PngBackgroundColorWriter.WriteIndexedData(null!));
        Assert.Throws<ArgumentNullException>(() => PngBackgroundColorWriter.WriteIndexedData(null!, 0, background));
        Assert.Throws<ArgumentNullException>(() => PngBackgroundColorWriter.WriteIndexedData(new byte[1], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngBackgroundColorWriter.WriteTruecolorChunk(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngBackgroundColorWriter.WriteTruecolorData(new byte[6], -1, background));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngBackgroundColorWriter.WriteTruecolorData(new byte[6], 1, background));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngBackgroundColorWriter.WriteGrayscaleData(new byte[2], 1, background));
    }

    static PngBackgroundColor CreateBackground()
    {
        PngBackgroundColor background = new PngBackgroundColor();
        background.PaletteIndex = 7;
        background.Gray = 0x1234;
        background.Red = 0x2233;
        background.Green = 0x4455;
        background.Blue = 0x6677;
        return background;
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
