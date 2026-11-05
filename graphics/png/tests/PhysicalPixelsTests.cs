using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PhysicalPixelsTests
{
    [Test]
    public void Parse_ReadsBigEndianPhysicalPixelValuesUsingOffset()
    {
        PngPhysicalPixels physicalPixels = PngPhysicalPixelsParser.Parse(new byte[] { 99, 0x00, 0x00, 0x0B, 0x13, 0x00, 0x00, 0x0B, 0x13, 1, 88 }, 1, 9);

        AssertPhysicalPixels(physicalPixels);
    }

    [Test]
    public void Parse_AcceptsKnownUnitSpecifiers()
    {
        Assert.That(PngPhysicalPixelsParser.Parse(new byte[] { 0, 0, 0, 1, 0, 0, 0, 1, 0 }).UnitSpecifier, Is.EqualTo(PngPhysicalPixelsParser.UnitUnknown));
        Assert.That(PngPhysicalPixelsParser.Parse(new byte[] { 0, 0, 0, 1, 0, 0, 0, 1, 1 }).UnitSpecifier, Is.EqualTo(PngPhysicalPixelsParser.UnitMetre));
    }

    [Test]
    public void Parse_ReadsChunkPayload()
    {
        PngChunk chunk = CreateChunk(PngConstants.ChunkPhysicalPixels, CreatePayload());

        PngPhysicalPixels physicalPixels = PngPhysicalPixelsParser.Parse(chunk);

        AssertPhysicalPixels(physicalPixels);
    }

    [Test]
    public void WriteData_WritesBigEndianPhysicalPixelValues()
    {
        PngPhysicalPixels physicalPixels = CreatePhysicalPixels();

        byte[] data = PngPhysicalPixelsWriter.WriteData(physicalPixels);

        Assert.That(data, Is.EqualTo(CreatePayload()));
    }

    [Test]
    public void WriteData_WritesAtOffset()
    {
        PngPhysicalPixels physicalPixels = CreatePhysicalPixels();
        byte[] data = new byte[] { 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 88 };

        PngPhysicalPixelsWriter.WriteData(data, 1, physicalPixels);

        Assert.That(data, Is.EqualTo(new byte[] { 99, 0x00, 0x00, 0x0B, 0x13, 0x00, 0x00, 0x0B, 0x13, 1, 88 }));
    }

    [Test]
    public void WriteChunk_CreatesKnownPhysChunk()
    {
        PngPhysicalPixels physicalPixels = CreatePhysicalPixels();

        PngChunk chunk = PngPhysicalPixelsWriter.WriteChunk(physicalPixels);

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkPhysicalPixels));
        Assert.That(chunk.Data, Is.EqualTo(CreatePayload()));
        Assert.That(chunk.Length, Is.EqualTo(9));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.False);
    }

    [Test]
    public void WriteChunk_RoundTripsThroughRawChunkWriter()
    {
        PngChunk chunk = PngPhysicalPixelsWriter.WriteChunk(CreatePhysicalPixels());

        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngPhysicalPixels physicalPixels = PngPhysicalPixelsParser.Parse(readChunk);

        AssertPhysicalPixels(physicalPixels);
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkPhysicalPixels, readChunk.Data)));
    }

    [Test]
    public void Parser_RejectsInvalidInputs()
    {
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = CreatePayload();
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkPhysicalPixels;
        nullData.Data = null!;
        PngChunk wrongType = CreateChunk(PngConstants.ChunkGamma, CreatePayload());

        Assert.Throws<ArgumentNullException>(() => PngPhysicalPixelsParser.Parse((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngPhysicalPixelsParser.Parse(null!, 0, 9));
        Assert.Throws<ArgumentNullException>(() => PngPhysicalPixelsParser.Parse((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngPhysicalPixelsParser.Parse(nullType));
        Assert.Throws<ArgumentNullException>(() => PngPhysicalPixelsParser.Parse(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPhysicalPixelsParser.Parse(new byte[9], -1, 9));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPhysicalPixelsParser.Parse(new byte[9], 10, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPhysicalPixelsParser.Parse(new byte[9], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPhysicalPixelsParser.Parse(new byte[9], 1, 9));
        Assert.Throws<InvalidDataException>(() => PngPhysicalPixelsParser.Parse(new byte[8]));
        Assert.Throws<InvalidDataException>(() => PngPhysicalPixelsParser.Parse(new byte[10]));
        Assert.Throws<InvalidDataException>(() => PngPhysicalPixelsParser.Parse(new byte[] { 0, 0, 0, 1, 0, 0, 0, 1, 2 }));
        Assert.Throws<InvalidDataException>(() => PngPhysicalPixelsParser.Parse(wrongType));
    }

    [Test]
    public void Writer_RejectsInvalidInputs()
    {
        PngPhysicalPixels physicalPixels = CreatePhysicalPixels();
        PngPhysicalPixels invalidUnit = CreatePhysicalPixels();
        invalidUnit.UnitSpecifier = 2;

        Assert.Throws<ArgumentNullException>(() => PngPhysicalPixelsWriter.WriteData(null!));
        Assert.Throws<ArgumentNullException>(() => PngPhysicalPixelsWriter.WriteData(null!, 0, physicalPixels));
        Assert.Throws<ArgumentNullException>(() => PngPhysicalPixelsWriter.WriteData(new byte[9], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngPhysicalPixelsWriter.WriteChunk(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPhysicalPixelsWriter.WriteData(new byte[9], -1, physicalPixels));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPhysicalPixelsWriter.WriteData(new byte[9], 1, physicalPixels));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPhysicalPixelsWriter.WriteData(invalidUnit));
    }

    static PngPhysicalPixels CreatePhysicalPixels()
    {
        PngPhysicalPixels physicalPixels = new PngPhysicalPixels();
        physicalPixels.PixelsPerUnitX = 2835;
        physicalPixels.PixelsPerUnitY = 2835;
        physicalPixels.UnitSpecifier = PngPhysicalPixelsParser.UnitMetre;
        return physicalPixels;
    }

    static byte[] CreatePayload()
    {
        return new byte[] { 0x00, 0x00, 0x0B, 0x13, 0x00, 0x00, 0x0B, 0x13, 1 };
    }

    static void AssertPhysicalPixels(PngPhysicalPixels physicalPixels)
    {
        Assert.That(physicalPixels.PixelsPerUnitX, Is.EqualTo(2835));
        Assert.That(physicalPixels.PixelsPerUnitY, Is.EqualTo(2835));
        Assert.That(physicalPixels.UnitSpecifier, Is.EqualTo(PngPhysicalPixelsParser.UnitMetre));
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
