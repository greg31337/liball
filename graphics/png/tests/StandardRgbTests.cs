using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class StandardRgbTests
{
    [Test]
    public void Parse_ReadsRenderingIntentUsingOffset()
    {
        PngStandardRgb standardRgb = PngStandardRgbParser.Parse(new byte[] { 99, 2, 88 }, 1, 1);

        Assert.That(standardRgb.RenderingIntent, Is.EqualTo(2));
    }

    [Test]
    public void Parse_AcceptsAllDefinedRenderingIntents()
    {
        Assert.That(PngStandardRgbParser.Parse(new byte[] { 0 }).RenderingIntent, Is.EqualTo(0));
        Assert.That(PngStandardRgbParser.Parse(new byte[] { 1 }).RenderingIntent, Is.EqualTo(1));
        Assert.That(PngStandardRgbParser.Parse(new byte[] { 2 }).RenderingIntent, Is.EqualTo(2));
        Assert.That(PngStandardRgbParser.Parse(new byte[] { 3 }).RenderingIntent, Is.EqualTo(3));
    }

    [Test]
    public void Parse_ReadsChunkPayload()
    {
        PngChunk chunk = CreateChunk(PngConstants.ChunkStandardRgb, new byte[] { 1 });

        PngStandardRgb standardRgb = PngStandardRgbParser.Parse(chunk);

        Assert.That(standardRgb.RenderingIntent, Is.EqualTo(1));
    }

    [Test]
    public void WriteData_WritesRenderingIntent()
    {
        PngStandardRgb standardRgb = CreateStandardRgb();

        byte[] data = PngStandardRgbWriter.WriteData(standardRgb);

        Assert.That(data, Is.EqualTo(new byte[] { 2 }));
    }

    [Test]
    public void WriteData_WritesAtOffset()
    {
        PngStandardRgb standardRgb = CreateStandardRgb();
        byte[] data = new byte[] { 99, 99, 99 };

        PngStandardRgbWriter.WriteData(data, 1, standardRgb);

        Assert.That(data, Is.EqualTo(new byte[] { 99, 2, 99 }));
    }

    [Test]
    public void WriteChunk_CreatesKnownSrgbChunk()
    {
        PngStandardRgb standardRgb = CreateStandardRgb();

        PngChunk chunk = PngStandardRgbWriter.WriteChunk(standardRgb);

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkStandardRgb));
        Assert.That(chunk.Data, Is.EqualTo(new byte[] { 2 }));
        Assert.That(chunk.Length, Is.EqualTo(1));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.False);
    }

    [Test]
    public void WriteChunk_RoundTripsThroughRawChunkWriter()
    {
        PngChunk chunk = PngStandardRgbWriter.WriteChunk(CreateStandardRgb());

        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngStandardRgb standardRgb = PngStandardRgbParser.Parse(readChunk);

        Assert.That(standardRgb.RenderingIntent, Is.EqualTo(2));
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkStandardRgb, readChunk.Data)));
    }

    [Test]
    public void Parser_RejectsInvalidInputs()
    {
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = new byte[] { 0 };
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkStandardRgb;
        nullData.Data = null!;
        PngChunk wrongType = CreateChunk(PngConstants.ChunkGamma, new byte[] { 0 });

        Assert.Throws<ArgumentNullException>(() => PngStandardRgbParser.Parse((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngStandardRgbParser.Parse(null!, 0, 1));
        Assert.Throws<ArgumentNullException>(() => PngStandardRgbParser.Parse((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngStandardRgbParser.Parse(nullType));
        Assert.Throws<ArgumentNullException>(() => PngStandardRgbParser.Parse(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngStandardRgbParser.Parse(new byte[1], -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngStandardRgbParser.Parse(new byte[1], 2, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngStandardRgbParser.Parse(new byte[1], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngStandardRgbParser.Parse(new byte[1], 1, 1));
        Assert.Throws<InvalidDataException>(() => PngStandardRgbParser.Parse(new byte[0]));
        Assert.Throws<InvalidDataException>(() => PngStandardRgbParser.Parse(new byte[] { 1, 2 }));
        Assert.Throws<InvalidDataException>(() => PngStandardRgbParser.Parse(new byte[] { 4 }));
        Assert.Throws<InvalidDataException>(() => PngStandardRgbParser.Parse(wrongType));
    }

    [Test]
    public void Writer_RejectsInvalidInputs()
    {
        PngStandardRgb standardRgb = CreateStandardRgb();
        PngStandardRgb invalidIntent = CreateStandardRgb();
        invalidIntent.RenderingIntent = 4;

        Assert.Throws<ArgumentNullException>(() => PngStandardRgbWriter.WriteData(null!));
        Assert.Throws<ArgumentNullException>(() => PngStandardRgbWriter.WriteData(null!, 0, standardRgb));
        Assert.Throws<ArgumentNullException>(() => PngStandardRgbWriter.WriteData(new byte[1], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngStandardRgbWriter.WriteChunk(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngStandardRgbWriter.WriteData(new byte[1], -1, standardRgb));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngStandardRgbWriter.WriteData(new byte[1], 1, standardRgb));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngStandardRgbWriter.WriteData(invalidIntent));
    }

    static PngStandardRgb CreateStandardRgb()
    {
        PngStandardRgb standardRgb = new PngStandardRgb();
        standardRgb.RenderingIntent = 2;
        return standardRgb;
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
