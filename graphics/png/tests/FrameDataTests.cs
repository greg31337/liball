using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class FrameDataTests
{
    [Test]
    public void Parse_ReadsFrameDataUsingOffset()
    {
        byte[] payload = CreatePayload();
        byte[] data = Wrap(payload);

        PngFrameData frameData = PngFrameDataParser.Parse(data, 1, payload.Length);

        AssertFrameData(frameData);
    }

    [Test]
    public void Parse_AllowsEmptyFrameData()
    {
        PngFrameData frameData = PngFrameDataParser.Parse(new byte[] { 0x00, 0x00, 0x00, 0x09 });

        Assert.That(frameData.SequenceNumber, Is.EqualTo(9));
        Assert.That(frameData.Data, Is.Empty);
    }

    [Test]
    public void Parse_ReadsChunkPayload()
    {
        PngChunk chunk = CreateChunk(PngConstants.ChunkFrameData, CreatePayload());

        PngFrameData frameData = PngFrameDataParser.Parse(chunk);

        AssertFrameData(frameData);
    }

    [Test]
    public void Parse_CopiesFrameDataBytes()
    {
        byte[] payload = CreatePayload();

        PngFrameData frameData = PngFrameDataParser.Parse(payload);
        payload[4] = 99;

        Assert.That(frameData.Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void WriteData_WritesFrameData()
    {
        byte[] data = PngFrameDataWriter.WriteData(CreateFrameData());

        Assert.That(data, Is.EqualTo(CreatePayload()));
    }

    [Test]
    public void WriteData_CopiesFrameDataBytes()
    {
        PngFrameData frameData = CreateFrameData();

        byte[] data = PngFrameDataWriter.WriteData(frameData);
        frameData.Data[0] = 99;

        Assert.That(data, Is.EqualTo(CreatePayload()));
    }

    [Test]
    public void WriteData_WritesAtOffset()
    {
        byte[] payload = CreatePayload();
        byte[] data = new byte[payload.Length + 2];
        data[0] = 99;
        data[data.Length - 1] = 88;

        PngFrameDataWriter.WriteData(data, 1, CreateFrameData());

        Assert.That(data, Is.EqualTo(Wrap(payload)));
    }

    [Test]
    public void WriteChunk_CreatesKnownFrameDataChunk()
    {
        PngChunk chunk = PngFrameDataWriter.WriteChunk(CreateFrameData());

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkFrameData));
        Assert.That(chunk.Data, Is.EqualTo(CreatePayload()));
        Assert.That(chunk.Length, Is.EqualTo(7));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.False);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.False);
    }

    [Test]
    public void WriteChunk_RoundTripsThroughRawChunkWriter()
    {
        PngChunk chunk = PngFrameDataWriter.WriteChunk(CreateFrameData());

        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngFrameData frameData = PngFrameDataParser.Parse(readChunk);

        AssertFrameData(frameData);
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkFrameData, readChunk.Data)));
    }

    [Test]
    public void Parser_RejectsInvalidInputs()
    {
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = CreatePayload();
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkFrameData;
        nullData.Data = null!;
        PngChunk wrongType = CreateChunk(PngConstants.ChunkGamma, CreatePayload());

        Assert.Throws<ArgumentNullException>(() => PngFrameDataParser.Parse((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngFrameDataParser.Parse(null!, 0, 4));
        Assert.Throws<ArgumentNullException>(() => PngFrameDataParser.Parse((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngFrameDataParser.Parse(nullType));
        Assert.Throws<ArgumentNullException>(() => PngFrameDataParser.Parse(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFrameDataParser.Parse(new byte[4], -1, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFrameDataParser.Parse(new byte[4], 5, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFrameDataParser.Parse(new byte[4], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFrameDataParser.Parse(new byte[4], 1, 4));
        Assert.Throws<InvalidDataException>(() => PngFrameDataParser.Parse(new byte[3]));
        Assert.Throws<InvalidDataException>(() => PngFrameDataParser.Parse(wrongType));
    }

    [Test]
    public void Writer_RejectsInvalidInputs()
    {
        PngFrameData frameData = CreateFrameData();
        PngFrameData nullData = CreateFrameData();
        nullData.Data = null!;

        Assert.Throws<ArgumentNullException>(() => PngFrameDataWriter.WriteData(null!));
        Assert.Throws<ArgumentNullException>(() => PngFrameDataWriter.WriteData(null!, 0, frameData));
        Assert.Throws<ArgumentNullException>(() => PngFrameDataWriter.WriteData(new byte[7], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngFrameDataWriter.WriteChunk(null!));
        Assert.Throws<ArgumentNullException>(() => PngFrameDataWriter.WriteData(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFrameDataWriter.WriteData(new byte[7], -1, frameData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFrameDataWriter.WriteData(new byte[7], 1, frameData));
    }

    static PngFrameData CreateFrameData()
    {
        PngFrameData frameData = new PngFrameData();
        frameData.SequenceNumber = 7;
        frameData.Data = new byte[] { 1, 2, 3 };
        return frameData;
    }

    static byte[] CreatePayload()
    {
        return new byte[] { 0x00, 0x00, 0x00, 0x07, 1, 2, 3 };
    }

    static byte[] Wrap(byte[] payload)
    {
        byte[] data = new byte[payload.Length + 2];
        data[0] = 99;
        data[data.Length - 1] = 88;
        CopyBytes(payload, 0, data, 1, payload.Length);
        return data;
    }

    static void AssertFrameData(PngFrameData frameData)
    {
        Assert.That(frameData.SequenceNumber, Is.EqualTo(7));
        Assert.That(frameData.Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
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

    static void CopyBytes(byte[] source, int sourceOffset, byte[] destination, int destinationOffset, int length)
    {
        int index = 0;
        while (index < length)
        {
            destination[destinationOffset + index] = source[sourceOffset + index];
            index += 1;
        }
    }
}
