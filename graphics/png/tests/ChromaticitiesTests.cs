using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ChromaticitiesTests
{
    [Test]
    public void Parse_ReadsBigEndianChromaticityValuesUsingOffset()
    {
        byte[] data = new byte[34];
        data[0] = 99;
        data[33] = 88;
        PngChromaticitiesWriter.WriteData(data, 1, CreateChromaticities());

        PngChromaticities chromaticities = PngChromaticitiesParser.Parse(data, 1, 32);

        AssertChromaticities(chromaticities);
    }

    [Test]
    public void Parse_ReadsChunkPayload()
    {
        PngChunk chunk = CreateChunk(PngConstants.ChunkChromaticities, PngChromaticitiesWriter.WriteData(CreateChromaticities()));

        PngChromaticities chromaticities = PngChromaticitiesParser.Parse(chunk);

        AssertChromaticities(chromaticities);
    }

    [Test]
    public void WriteData_WritesBigEndianChromaticityValues()
    {
        byte[] data = PngChromaticitiesWriter.WriteData(CreateChromaticities());

        Assert.That(data, Is.EqualTo(new byte[]
        {
            0x00, 0x00, 0x7A, 0x26,
            0x00, 0x00, 0x80, 0x84,
            0x00, 0x00, 0xFA, 0x00,
            0x00, 0x00, 0x80, 0xE8,
            0x00, 0x00, 0x75, 0x30,
            0x00, 0x00, 0xEA, 0x60,
            0x00, 0x00, 0x3A, 0x98,
            0x00, 0x00, 0x17, 0x70
        }));
    }

    [Test]
    public void WriteData_WritesAtOffset()
    {
        byte[] data = new byte[34];
        data[0] = 99;
        data[33] = 88;

        PngChromaticitiesWriter.WriteData(data, 1, CreateChromaticities());

        Assert.That(data[0], Is.EqualTo(99));
        Assert.That(data[1], Is.EqualTo(0));
        Assert.That(data[2], Is.EqualTo(0));
        Assert.That(data[3], Is.EqualTo(0x7A));
        Assert.That(data[4], Is.EqualTo(0x26));
        Assert.That(data[33], Is.EqualTo(88));
    }

    [Test]
    public void WriteChunk_CreatesKnownChrmChunk()
    {
        PngChunk chunk = PngChromaticitiesWriter.WriteChunk(CreateChromaticities());

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkChromaticities));
        Assert.That(chunk.Data.Length, Is.EqualTo(32));
        Assert.That(chunk.Length, Is.EqualTo(32));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.False);
    }

    [Test]
    public void WriteChunk_RoundTripsThroughRawChunkWriter()
    {
        PngChunk chunk = PngChromaticitiesWriter.WriteChunk(CreateChromaticities());

        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngChromaticities chromaticities = PngChromaticitiesParser.Parse(readChunk);

        AssertChromaticities(chromaticities);
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkChromaticities, readChunk.Data)));
    }

    [Test]
    public void Parser_RejectsInvalidInputs()
    {
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = new byte[32];
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkChromaticities;
        nullData.Data = null!;
        PngChunk wrongType = CreateChunk(PngConstants.ChunkGamma, new byte[32]);

        Assert.Throws<ArgumentNullException>(() => PngChromaticitiesParser.Parse((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngChromaticitiesParser.Parse(null!, 0, 32));
        Assert.Throws<ArgumentNullException>(() => PngChromaticitiesParser.Parse((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngChromaticitiesParser.Parse(nullType));
        Assert.Throws<ArgumentNullException>(() => PngChromaticitiesParser.Parse(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngChromaticitiesParser.Parse(new byte[32], -1, 32));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngChromaticitiesParser.Parse(new byte[32], 33, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngChromaticitiesParser.Parse(new byte[32], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngChromaticitiesParser.Parse(new byte[32], 1, 32));
        Assert.Throws<InvalidDataException>(() => PngChromaticitiesParser.Parse(new byte[31]));
        Assert.Throws<InvalidDataException>(() => PngChromaticitiesParser.Parse(new byte[33]));
        Assert.Throws<InvalidDataException>(() => PngChromaticitiesParser.Parse(wrongType));
    }

    [Test]
    public void Writer_RejectsInvalidInputs()
    {
        PngChromaticities chromaticities = CreateChromaticities();

        Assert.Throws<ArgumentNullException>(() => PngChromaticitiesWriter.WriteData(null!));
        Assert.Throws<ArgumentNullException>(() => PngChromaticitiesWriter.WriteData(null!, 0, chromaticities));
        Assert.Throws<ArgumentNullException>(() => PngChromaticitiesWriter.WriteData(new byte[32], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngChromaticitiesWriter.WriteChunk(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngChromaticitiesWriter.WriteData(new byte[32], -1, chromaticities));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngChromaticitiesWriter.WriteData(new byte[32], 1, chromaticities));
    }

    static void AssertChromaticities(PngChromaticities chromaticities)
    {
        Assert.That(chromaticities.WhitePointX, Is.EqualTo(31270));
        Assert.That(chromaticities.WhitePointY, Is.EqualTo(32900));
        Assert.That(chromaticities.RedX, Is.EqualTo(64000));
        Assert.That(chromaticities.RedY, Is.EqualTo(33000));
        Assert.That(chromaticities.GreenX, Is.EqualTo(30000));
        Assert.That(chromaticities.GreenY, Is.EqualTo(60000));
        Assert.That(chromaticities.BlueX, Is.EqualTo(15000));
        Assert.That(chromaticities.BlueY, Is.EqualTo(6000));
    }

    static PngChromaticities CreateChromaticities()
    {
        PngChromaticities chromaticities = new PngChromaticities();
        chromaticities.WhitePointX = 31270;
        chromaticities.WhitePointY = 32900;
        chromaticities.RedX = 64000;
        chromaticities.RedY = 33000;
        chromaticities.GreenX = 30000;
        chromaticities.GreenY = 60000;
        chromaticities.BlueX = 15000;
        chromaticities.BlueY = 6000;
        return chromaticities;
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
