using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class FrameControlTests
{
    [Test]
    public void Parse_ReadsFrameControlUsingOffset()
    {
        byte[] payload = CreatePayload();
        byte[] data = Wrap(payload);

        PngFrameControl control = PngFrameControlParser.Parse(data, 1, payload.Length);

        AssertControl(control);
    }

    [Test]
    public void Parse_AllowsSequenceZeroAndZeroDelayDenominator()
    {
        PngFrameControl control = PngFrameControlParser.Parse(CreateZeroAllowedPayload());

        Assert.That(control.SequenceNumber, Is.EqualTo(0));
        Assert.That(control.DelayDenominator, Is.EqualTo(0));
    }

    [Test]
    public void Parse_ReadsChunkPayload()
    {
        PngChunk chunk = CreateChunk(PngConstants.ChunkFrameControl, CreatePayload());

        PngFrameControl control = PngFrameControlParser.Parse(chunk);

        AssertControl(control);
    }

    [Test]
    public void WriteData_WritesFrameControl()
    {
        byte[] data = PngFrameControlWriter.WriteData(CreateControl());

        Assert.That(data, Is.EqualTo(CreatePayload()));
    }

    [Test]
    public void WriteData_WritesAtOffset()
    {
        byte[] payload = CreatePayload();
        byte[] data = new byte[payload.Length + 2];
        data[0] = 99;
        data[data.Length - 1] = 88;

        PngFrameControlWriter.WriteData(data, 1, CreateControl());

        Assert.That(data, Is.EqualTo(Wrap(payload)));
    }

    [Test]
    public void WriteChunk_CreatesKnownFrameControlChunk()
    {
        PngChunk chunk = PngFrameControlWriter.WriteChunk(CreateControl());

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkFrameControl));
        Assert.That(chunk.Data, Is.EqualTo(CreatePayload()));
        Assert.That(chunk.Length, Is.EqualTo(26));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.False);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.False);
    }

    [Test]
    public void WriteChunk_RoundTripsThroughRawChunkWriter()
    {
        PngChunk chunk = PngFrameControlWriter.WriteChunk(CreateControl());

        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngFrameControl control = PngFrameControlParser.Parse(readChunk);

        AssertControl(control);
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkFrameControl, readChunk.Data)));
    }

    [Test]
    public void Parser_RejectsInvalidInputs()
    {
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = CreatePayload();
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkFrameControl;
        nullData.Data = null!;
        PngChunk wrongType = CreateChunk(PngConstants.ChunkGamma, CreatePayload());

        Assert.Throws<ArgumentNullException>(() => PngFrameControlParser.Parse((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngFrameControlParser.Parse(null!, 0, 26));
        Assert.Throws<ArgumentNullException>(() => PngFrameControlParser.Parse((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngFrameControlParser.Parse(nullType));
        Assert.Throws<ArgumentNullException>(() => PngFrameControlParser.Parse(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFrameControlParser.Parse(new byte[26], -1, 26));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFrameControlParser.Parse(new byte[26], 27, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFrameControlParser.Parse(new byte[26], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFrameControlParser.Parse(new byte[26], 1, 26));
        Assert.Throws<InvalidDataException>(() => PngFrameControlParser.Parse(new byte[25]));
        Assert.Throws<InvalidDataException>(() => PngFrameControlParser.Parse(new byte[27]));
        Assert.Throws<InvalidDataException>(() => PngFrameControlParser.Parse(CreatePayloadWithWidth(0)));
        Assert.Throws<InvalidDataException>(() => PngFrameControlParser.Parse(CreatePayloadWithHeight(0)));
        Assert.Throws<InvalidDataException>(() => PngFrameControlParser.Parse(CreatePayloadWithDisposeOperation(3)));
        Assert.Throws<InvalidDataException>(() => PngFrameControlParser.Parse(CreatePayloadWithBlendOperation(2)));
        Assert.Throws<InvalidDataException>(() => PngFrameControlParser.Parse(wrongType));
    }

    [Test]
    public void Writer_RejectsInvalidInputs()
    {
        PngFrameControl control = CreateControl();
        PngFrameControl zeroWidth = CreateControl();
        zeroWidth.Width = 0;
        PngFrameControl zeroHeight = CreateControl();
        zeroHeight.Height = 0;
        PngFrameControl invalidDispose = CreateControl();
        invalidDispose.DisposeOperation = (PngApngDisposeOperation)3;
        PngFrameControl invalidBlend = CreateControl();
        invalidBlend.BlendOperation = (PngApngBlendOperation)2;

        Assert.Throws<ArgumentNullException>(() => PngFrameControlWriter.WriteData(null!));
        Assert.Throws<ArgumentNullException>(() => PngFrameControlWriter.WriteData(null!, 0, control));
        Assert.Throws<ArgumentNullException>(() => PngFrameControlWriter.WriteData(new byte[26], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngFrameControlWriter.WriteChunk(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFrameControlWriter.WriteData(new byte[26], -1, control));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFrameControlWriter.WriteData(new byte[26], 1, control));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFrameControlWriter.WriteData(zeroWidth));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFrameControlWriter.WriteData(zeroHeight));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFrameControlWriter.WriteData(invalidDispose));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFrameControlWriter.WriteData(invalidBlend));
    }

    static PngFrameControl CreateControl()
    {
        PngFrameControl control = new PngFrameControl();
        control.SequenceNumber = 7;
        control.Width = 640;
        control.Height = 480;
        control.XOffset = 10;
        control.YOffset = 20;
        control.DelayNumerator = 1;
        control.DelayDenominator = 24;
        control.DisposeOperation = PngApngDisposeOperation.Previous;
        control.BlendOperation = PngApngBlendOperation.Over;
        return control;
    }

    static byte[] CreatePayload()
    {
        return new byte[]
        {
            0x00, 0x00, 0x00, 0x07,
            0x00, 0x00, 0x02, 0x80,
            0x00, 0x00, 0x01, 0xE0,
            0x00, 0x00, 0x00, 0x0A,
            0x00, 0x00, 0x00, 0x14,
            0x00, 0x01,
            0x00, 0x18,
            0x02,
            0x01
        };
    }

    static byte[] CreateZeroAllowedPayload()
    {
        return new byte[]
        {
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00,
            0x00
        };
    }

    static byte[] CreatePayloadWithWidth(uint width)
    {
        byte[] payload = CreatePayload();
        PngBinaryWriter.WriteUInt32(payload, 4, width);
        return payload;
    }

    static byte[] CreatePayloadWithHeight(uint height)
    {
        byte[] payload = CreatePayload();
        PngBinaryWriter.WriteUInt32(payload, 8, height);
        return payload;
    }

    static byte[] CreatePayloadWithDisposeOperation(byte disposeOperation)
    {
        byte[] payload = CreatePayload();
        payload[24] = disposeOperation;
        return payload;
    }

    static byte[] CreatePayloadWithBlendOperation(byte blendOperation)
    {
        byte[] payload = CreatePayload();
        payload[25] = blendOperation;
        return payload;
    }

    static byte[] Wrap(byte[] payload)
    {
        byte[] data = new byte[payload.Length + 2];
        data[0] = 99;
        data[data.Length - 1] = 88;
        CopyBytes(payload, 0, data, 1, payload.Length);
        return data;
    }

    static void AssertControl(PngFrameControl control)
    {
        Assert.That(control.SequenceNumber, Is.EqualTo(7));
        Assert.That(control.Width, Is.EqualTo(640));
        Assert.That(control.Height, Is.EqualTo(480));
        Assert.That(control.XOffset, Is.EqualTo(10));
        Assert.That(control.YOffset, Is.EqualTo(20));
        Assert.That(control.DelayNumerator, Is.EqualTo(1));
        Assert.That(control.DelayDenominator, Is.EqualTo(24));
        Assert.That(control.DisposeOperation, Is.EqualTo(PngApngDisposeOperation.Previous));
        Assert.That(control.BlendOperation, Is.EqualTo(PngApngBlendOperation.Over));
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
