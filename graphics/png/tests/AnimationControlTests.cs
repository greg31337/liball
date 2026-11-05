using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class AnimationControlTests
{
    [Test]
    public void Parse_ReadsAnimationControlUsingOffset()
    {
        PngAnimationControl control = PngAnimationControlParser.Parse(new byte[] { 99, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 88 }, 1, 8);

        Assert.That(control.FrameCount, Is.EqualTo(2));
        Assert.That(control.PlayCount, Is.EqualTo(0));
    }

    [Test]
    public void Parse_ReadsFinitePlayCount()
    {
        PngAnimationControl control = PngAnimationControlParser.Parse(CreatePayload());

        AssertControl(control);
    }

    [Test]
    public void Parse_ReadsChunkPayload()
    {
        PngChunk chunk = CreateChunk(PngConstants.ChunkAnimationControl, CreatePayload());

        PngAnimationControl control = PngAnimationControlParser.Parse(chunk);

        AssertControl(control);
    }

    [Test]
    public void WriteData_WritesAnimationControl()
    {
        byte[] data = PngAnimationControlWriter.WriteData(CreateControl());

        Assert.That(data, Is.EqualTo(CreatePayload()));
    }

    [Test]
    public void WriteData_WritesAtOffset()
    {
        byte[] data = new byte[] { 99, 99, 99, 99, 99, 99, 99, 99, 99, 88 };

        PngAnimationControlWriter.WriteData(data, 1, CreateControl());

        Assert.That(data, Is.EqualTo(new byte[] { 99, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x05, 88 }));
    }

    [Test]
    public void WriteChunk_CreatesKnownAnimationControlChunk()
    {
        PngChunk chunk = PngAnimationControlWriter.WriteChunk(CreateControl());

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkAnimationControl));
        Assert.That(chunk.Data, Is.EqualTo(CreatePayload()));
        Assert.That(chunk.Length, Is.EqualTo(8));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.False);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.False);
    }

    [Test]
    public void WriteChunk_RoundTripsThroughRawChunkWriter()
    {
        PngChunk chunk = PngAnimationControlWriter.WriteChunk(CreateControl());

        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(chunk);
        PngChunk readChunk = PngRawChunkReader.Read(rawChunk, 0);
        PngAnimationControl control = PngAnimationControlParser.Parse(readChunk);

        AssertControl(control);
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkAnimationControl, readChunk.Data)));
    }

    [Test]
    public void Parser_RejectsInvalidInputs()
    {
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = CreatePayload();
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkAnimationControl;
        nullData.Data = null!;
        PngChunk wrongType = CreateChunk(PngConstants.ChunkGamma, CreatePayload());

        Assert.Throws<ArgumentNullException>(() => PngAnimationControlParser.Parse((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngAnimationControlParser.Parse(null!, 0, 8));
        Assert.Throws<ArgumentNullException>(() => PngAnimationControlParser.Parse((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngAnimationControlParser.Parse(nullType));
        Assert.Throws<ArgumentNullException>(() => PngAnimationControlParser.Parse(nullData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAnimationControlParser.Parse(new byte[8], -1, 8));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAnimationControlParser.Parse(new byte[8], 9, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAnimationControlParser.Parse(new byte[8], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAnimationControlParser.Parse(new byte[8], 1, 8));
        Assert.Throws<InvalidDataException>(() => PngAnimationControlParser.Parse(new byte[7]));
        Assert.Throws<InvalidDataException>(() => PngAnimationControlParser.Parse(new byte[9]));
        Assert.Throws<InvalidDataException>(() => PngAnimationControlParser.Parse(new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }));
        Assert.Throws<InvalidDataException>(() => PngAnimationControlParser.Parse(wrongType));
    }

    [Test]
    public void Writer_RejectsInvalidInputs()
    {
        PngAnimationControl control = CreateControl();
        PngAnimationControl zeroFrames = CreateControl();
        zeroFrames.FrameCount = 0;

        Assert.Throws<ArgumentNullException>(() => PngAnimationControlWriter.WriteData(null!));
        Assert.Throws<ArgumentNullException>(() => PngAnimationControlWriter.WriteData(null!, 0, control));
        Assert.Throws<ArgumentNullException>(() => PngAnimationControlWriter.WriteData(new byte[8], 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngAnimationControlWriter.WriteChunk(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAnimationControlWriter.WriteData(new byte[8], -1, control));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAnimationControlWriter.WriteData(new byte[8], 1, control));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAnimationControlWriter.WriteData(zeroFrames));
    }

    static PngAnimationControl CreateControl()
    {
        PngAnimationControl control = new PngAnimationControl();
        control.FrameCount = 3;
        control.PlayCount = 5;
        return control;
    }

    static byte[] CreatePayload()
    {
        return new byte[] { 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x05 };
    }

    static void AssertControl(PngAnimationControl control)
    {
        Assert.That(control.FrameCount, Is.EqualTo(3));
        Assert.That(control.PlayCount, Is.EqualTo(5));
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
