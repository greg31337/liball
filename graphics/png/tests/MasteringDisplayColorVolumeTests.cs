using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class MasteringDisplayColorVolumeTests
{
    [Test]
    public void Parse_ReadsBigEndianValuesUsingOffset()
    {
        byte[] data = new byte[]
        {
            99,
            0x11, 0x11, 0x22, 0x22,
            0x33, 0x33, 0x44, 0x44,
            0x55, 0x55, 0x66, 0x66,
            0x77, 0x77, 0x88, 0x88,
            0x01, 0x02, 0x03, 0x04,
            0x05, 0x06, 0x07, 0x08,
            88
        };

        PngMasteringDisplayColorVolume volume = PngMasteringDisplayColorVolumeParser.Parse(data, 1, 24);

        AssertVolume(volume);
    }

    [Test]
    public void Parse_ReadsChunkPayload()
    {
        PngChunk chunk = CreateChunk(PngConstants.ChunkMasteringDisplayColorVolume, CreatePayload());

        PngMasteringDisplayColorVolume volume = PngMasteringDisplayColorVolumeParser.Parse(chunk);

        AssertVolume(volume);
    }

    [Test]
    public void WriteData_WritesBigEndianValues()
    {
        PngMasteringDisplayColorVolume volume = CreateVolume();

        byte[] data = PngMasteringDisplayColorVolumeWriter.WriteData(volume);

        Assert.That(data, Is.EqualTo(CreatePayload()));
    }

    [Test]
    public void WriteData_WritesAtOffset()
    {
        PngMasteringDisplayColorVolume volume = CreateVolume();
        byte[] data = new byte[]
        {
            99,
            99, 99, 99, 99,
            99, 99, 99, 99,
            99, 99, 99, 99,
            99, 99, 99, 99,
            99, 99, 99, 99,
            99, 99, 99, 99,
            88
        };

        PngMasteringDisplayColorVolumeWriter.WriteData(data, 1, volume);

        Assert.That(data, Is.EqualTo(new byte[]
        {
            99,
            0x11, 0x11, 0x22, 0x22,
            0x33, 0x33, 0x44, 0x44,
            0x55, 0x55, 0x66, 0x66,
            0x77, 0x77, 0x88, 0x88,
            0x01, 0x02, 0x03, 0x04,
            0x05, 0x06, 0x07, 0x08,
            88
        }));
    }

    [Test]
    public void WriteChunk_CreatesKnownMdcvChunk()
    {
        PngMasteringDisplayColorVolume volume = CreateVolume();

        PngChunk chunk = PngMasteringDisplayColorVolumeWriter.WriteChunk(volume);

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkMasteringDisplayColorVolume));
        Assert.That(chunk.Data, Is.EqualTo(CreatePayload()));
        Assert.That(chunk.Length, Is.EqualTo(24));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.False);
    }

    [Test]
    public void WriteChunk_RoundTripsThroughRawChunkWriter()
    {
        PngChunk chunk = PngMasteringDisplayColorVolumeWriter.WriteChunk(CreateVolume());

        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngMasteringDisplayColorVolume volume = PngMasteringDisplayColorVolumeParser.Parse(readChunk);

        AssertVolume(volume);
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkMasteringDisplayColorVolume, readChunk.Data)));
    }

    [Test]
    public void Parser_RejectsInvalidInputs()
    {
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = CreatePayload();
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkMasteringDisplayColorVolume;
        nullData.Data = null!;
        PngChunk wrongType = CreateChunk(PngConstants.ChunkGamma, CreatePayload());

        Assert.Throws<ArgumentNullException>(() => PngMasteringDisplayColorVolumeParser.Parse((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngMasteringDisplayColorVolumeParser.Parse(null!, 0, 24));
        Assert.Throws<ArgumentNullException>(() => PngMasteringDisplayColorVolumeParser.Parse((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngMasteringDisplayColorVolumeParser.Parse(nullType));
        Assert.Throws<ArgumentNullException>(() => PngMasteringDisplayColorVolumeParser.Parse(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngMasteringDisplayColorVolumeParser.Parse(new byte[24], -1, 24));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngMasteringDisplayColorVolumeParser.Parse(new byte[24], 25, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngMasteringDisplayColorVolumeParser.Parse(new byte[24], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngMasteringDisplayColorVolumeParser.Parse(new byte[24], 1, 24));
        Assert.Throws<InvalidDataException>(() => PngMasteringDisplayColorVolumeParser.Parse(new byte[23]));
        Assert.Throws<InvalidDataException>(() => PngMasteringDisplayColorVolumeParser.Parse(new byte[25]));
        Assert.Throws<InvalidDataException>(() => PngMasteringDisplayColorVolumeParser.Parse(wrongType));
    }

    [Test]
    public void Writer_RejectsInvalidInputs()
    {
        PngMasteringDisplayColorVolume volume = CreateVolume();

        Assert.Throws<ArgumentNullException>(() => PngMasteringDisplayColorVolumeWriter.WriteData(null!));
        Assert.Throws<ArgumentNullException>(() => PngMasteringDisplayColorVolumeWriter.WriteData(null!, 0, volume));
        Assert.Throws<ArgumentNullException>(() => PngMasteringDisplayColorVolumeWriter.WriteData(new byte[24], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngMasteringDisplayColorVolumeWriter.WriteChunk(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngMasteringDisplayColorVolumeWriter.WriteData(new byte[24], -1, volume));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngMasteringDisplayColorVolumeWriter.WriteData(new byte[24], 1, volume));
    }

    static void AssertVolume(PngMasteringDisplayColorVolume volume)
    {
        Assert.That(volume.RedX, Is.EqualTo(0x1111));
        Assert.That(volume.RedY, Is.EqualTo(0x2222));
        Assert.That(volume.GreenX, Is.EqualTo(0x3333));
        Assert.That(volume.GreenY, Is.EqualTo(0x4444));
        Assert.That(volume.BlueX, Is.EqualTo(0x5555));
        Assert.That(volume.BlueY, Is.EqualTo(0x6666));
        Assert.That(volume.WhitePointX, Is.EqualTo(0x7777));
        Assert.That(volume.WhitePointY, Is.EqualTo(0x8888));
        Assert.That(volume.MaximumLuminance, Is.EqualTo(0x01020304));
        Assert.That(volume.MinimumLuminance, Is.EqualTo(0x05060708));
    }

    static PngMasteringDisplayColorVolume CreateVolume()
    {
        PngMasteringDisplayColorVolume volume = new PngMasteringDisplayColorVolume();
        volume.RedX = 0x1111;
        volume.RedY = 0x2222;
        volume.GreenX = 0x3333;
        volume.GreenY = 0x4444;
        volume.BlueX = 0x5555;
        volume.BlueY = 0x6666;
        volume.WhitePointX = 0x7777;
        volume.WhitePointY = 0x8888;
        volume.MaximumLuminance = 0x01020304;
        volume.MinimumLuminance = 0x05060708;
        return volume;
    }

    static byte[] CreatePayload()
    {
        return new byte[]
        {
            0x11, 0x11, 0x22, 0x22,
            0x33, 0x33, 0x44, 0x44,
            0x55, 0x55, 0x66, 0x66,
            0x77, 0x77, 0x88, 0x88,
            0x01, 0x02, 0x03, 0x04,
            0x05, 0x06, 0x07, 0x08
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
