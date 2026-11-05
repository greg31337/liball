using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ContentLightLevelInformationTests
{
    [Test]
    public void Parse_ReadsBigEndianValuesUsingOffset()
    {
        PngContentLightLevelInformation lightLevels = PngContentLightLevelInformationParser.Parse(new byte[]
        {
            99,
            0x00, 0x98, 0x96, 0x80,
            0x00, 0x26, 0x25, 0xA0,
            88
        }, 1, 8);

        AssertLightLevels(lightLevels);
    }

    [Test]
    public void Parse_ReadsChunkPayload()
    {
        PngChunk chunk = CreateChunk(PngConstants.ChunkContentLightLevelInformation, CreatePayload());

        PngContentLightLevelInformation lightLevels = PngContentLightLevelInformationParser.Parse(chunk);

        AssertLightLevels(lightLevels);
    }

    [Test]
    public void WriteData_WritesBigEndianValues()
    {
        PngContentLightLevelInformation lightLevels = CreateLightLevels();

        byte[] data = PngContentLightLevelInformationWriter.WriteData(lightLevels);

        Assert.That(data, Is.EqualTo(CreatePayload()));
    }

    [Test]
    public void WriteData_WritesAtOffset()
    {
        PngContentLightLevelInformation lightLevels = CreateLightLevels();
        byte[] data = new byte[] { 99, 99, 99, 99, 99, 99, 99, 99, 99, 88 };

        PngContentLightLevelInformationWriter.WriteData(data, 1, lightLevels);

        Assert.That(data, Is.EqualTo(new byte[]
        {
            99,
            0x00, 0x98, 0x96, 0x80,
            0x00, 0x26, 0x25, 0xA0,
            88
        }));
    }

    [Test]
    public void WriteChunk_CreatesKnownClliChunk()
    {
        PngContentLightLevelInformation lightLevels = CreateLightLevels();

        PngChunk chunk = PngContentLightLevelInformationWriter.WriteChunk(lightLevels);

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkContentLightLevelInformation));
        Assert.That(chunk.Data, Is.EqualTo(CreatePayload()));
        Assert.That(chunk.Length, Is.EqualTo(8));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.False);
    }

    [Test]
    public void WriteChunk_RoundTripsThroughRawChunkWriter()
    {
        PngChunk chunk = PngContentLightLevelInformationWriter.WriteChunk(CreateLightLevels());

        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngContentLightLevelInformation lightLevels = PngContentLightLevelInformationParser.Parse(readChunk);

        AssertLightLevels(lightLevels);
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkContentLightLevelInformation, readChunk.Data)));
    }

    [Test]
    public void Parser_RejectsInvalidInputs()
    {
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = CreatePayload();
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkContentLightLevelInformation;
        nullData.Data = null!;
        PngChunk wrongType = CreateChunk(PngConstants.ChunkGamma, CreatePayload());

        Assert.Throws<ArgumentNullException>(() => PngContentLightLevelInformationParser.Parse((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngContentLightLevelInformationParser.Parse(null!, 0, 8));
        Assert.Throws<ArgumentNullException>(() => PngContentLightLevelInformationParser.Parse((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngContentLightLevelInformationParser.Parse(nullType));
        Assert.Throws<ArgumentNullException>(() => PngContentLightLevelInformationParser.Parse(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngContentLightLevelInformationParser.Parse(new byte[8], -1, 8));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngContentLightLevelInformationParser.Parse(new byte[8], 9, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngContentLightLevelInformationParser.Parse(new byte[8], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngContentLightLevelInformationParser.Parse(new byte[8], 1, 8));
        Assert.Throws<InvalidDataException>(() => PngContentLightLevelInformationParser.Parse(new byte[7]));
        Assert.Throws<InvalidDataException>(() => PngContentLightLevelInformationParser.Parse(new byte[9]));
        Assert.Throws<InvalidDataException>(() => PngContentLightLevelInformationParser.Parse(wrongType));
    }

    [Test]
    public void Writer_RejectsInvalidInputs()
    {
        PngContentLightLevelInformation lightLevels = CreateLightLevels();

        Assert.Throws<ArgumentNullException>(() => PngContentLightLevelInformationWriter.WriteData(null!));
        Assert.Throws<ArgumentNullException>(() => PngContentLightLevelInformationWriter.WriteData(null!, 0, lightLevels));
        Assert.Throws<ArgumentNullException>(() => PngContentLightLevelInformationWriter.WriteData(new byte[8], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngContentLightLevelInformationWriter.WriteChunk(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngContentLightLevelInformationWriter.WriteData(new byte[8], -1, lightLevels));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngContentLightLevelInformationWriter.WriteData(new byte[8], 1, lightLevels));
    }

    static void AssertLightLevels(PngContentLightLevelInformation lightLevels)
    {
        Assert.That(lightLevels.MaximumContentLightLevel, Is.EqualTo(10000000));
        Assert.That(lightLevels.MaximumFrameAverageLightLevel, Is.EqualTo(2500000));
    }

    static PngContentLightLevelInformation CreateLightLevels()
    {
        PngContentLightLevelInformation lightLevels = new PngContentLightLevelInformation();
        lightLevels.MaximumContentLightLevel = 10000000;
        lightLevels.MaximumFrameAverageLightLevel = 2500000;
        return lightLevels;
    }

    static byte[] CreatePayload()
    {
        return new byte[]
        {
            0x00, 0x98, 0x96, 0x80,
            0x00, 0x26, 0x25, 0xA0
        };
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
