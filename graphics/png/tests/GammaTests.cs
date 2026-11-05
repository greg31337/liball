using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class GammaTests
{
    [Test]
    public void Parse_ReadsBigEndianGammaValueUsingOffset()
    {
        PngGamma gamma = PngGammaParser.Parse(new byte[] { 99, 0x00, 0x00, 0xB1, 0x8F, 88 }, 1, 4);

        Assert.That(gamma.Value, Is.EqualTo(45455));
    }

    [Test]
    public void Parse_ReadsChunkPayload()
    {
        PngChunk chunk = CreateChunk(PngConstants.ChunkGamma, new byte[] { 0x00, 0x01, 0x86, 0xA0 });

        PngGamma gamma = PngGammaParser.Parse(chunk);

        Assert.That(gamma.Value, Is.EqualTo(100000));
    }

    [Test]
    public void WriteData_WritesBigEndianGammaValue()
    {
        PngGamma gamma = CreateGamma();

        byte[] data = PngGammaWriter.WriteData(gamma);

        Assert.That(data, Is.EqualTo(new byte[] { 0x00, 0x00, 0xB1, 0x8F }));
    }

    [Test]
    public void WriteData_WritesAtOffset()
    {
        PngGamma gamma = CreateGamma();
        byte[] data = new byte[] { 99, 99, 99, 99, 99, 99 };

        PngGammaWriter.WriteData(data, 1, gamma);

        Assert.That(data, Is.EqualTo(new byte[] { 99, 0x00, 0x00, 0xB1, 0x8F, 99 }));
    }

    [Test]
    public void WriteChunk_CreatesKnownGammaChunk()
    {
        PngGamma gamma = CreateGamma();

        PngChunk chunk = PngGammaWriter.WriteChunk(gamma);

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkGamma));
        Assert.That(chunk.Data, Is.EqualTo(new byte[] { 0x00, 0x00, 0xB1, 0x8F }));
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
        PngChunk chunk = PngGammaWriter.WriteChunk(CreateGamma());

        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngGamma gamma = PngGammaParser.Parse(readChunk);

        Assert.That(gamma.Value, Is.EqualTo(45455));
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkGamma, readChunk.Data)));
    }

    [Test]
    public void Parser_RejectsInvalidInputs()
    {
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = new byte[] { 0, 0, 0, 1 };
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkGamma;
        nullData.Data = null!;
        PngChunk wrongType = CreateChunk(PngConstants.ChunkTransparency, new byte[] { 0, 0, 0, 1 });

        Assert.Throws<ArgumentNullException>(() => PngGammaParser.Parse((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngGammaParser.Parse(null!, 0, 4));
        Assert.Throws<ArgumentNullException>(() => PngGammaParser.Parse((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngGammaParser.Parse(nullType));
        Assert.Throws<ArgumentNullException>(() => PngGammaParser.Parse(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaParser.Parse(new byte[4], -1, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaParser.Parse(new byte[4], 5, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaParser.Parse(new byte[4], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaParser.Parse(new byte[4], 1, 4));
        Assert.Throws<InvalidDataException>(() => PngGammaParser.Parse(new byte[3]));
        Assert.Throws<InvalidDataException>(() => PngGammaParser.Parse(new byte[5]));
        Assert.Throws<InvalidDataException>(() => PngGammaParser.Parse(wrongType));
    }

    [Test]
    public void Writer_RejectsInvalidInputs()
    {
        PngGamma gamma = CreateGamma();

        Assert.Throws<ArgumentNullException>(() => PngGammaWriter.WriteData(null!));
        Assert.Throws<ArgumentNullException>(() => PngGammaWriter.WriteData(null!, 0, gamma));
        Assert.Throws<ArgumentNullException>(() => PngGammaWriter.WriteData(new byte[4], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngGammaWriter.WriteChunk(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaWriter.WriteData(new byte[4], -1, gamma));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaWriter.WriteData(new byte[4], 1, gamma));
    }

    static PngGamma CreateGamma()
    {
        PngGamma gamma = new PngGamma();
        gamma.Value = 45455;
        return gamma;
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
