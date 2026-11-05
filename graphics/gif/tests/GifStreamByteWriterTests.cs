using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifStreamByteWriterTests
{
    [Test]
    public void WriteByte_WritesSingleByteAndAdvancesOffset()
    {
        MemoryStream stream = new MemoryStream();
        GifStreamByteWriter writer = new GifStreamByteWriter(stream);

        bool success = writer.WriteByte(0x7F);

        Assert.That(success, Is.True);
        Assert.That(writer.ByteOffset, Is.EqualTo(1));
        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 0x7F }));
        Assert.That(writer.Failed, Is.False);
    }

    [Test]
    public void WriteByte_RejectsOutOfRangeValuesWithoutWriting()
    {
        MemoryStream stream = new MemoryStream();
        GifStreamByteWriter writer = new GifStreamByteWriter(stream);

        Assert.That(writer.WriteByte(-1), Is.False);
        Assert.That(writer.WriteByte(256), Is.False);
        Assert.That(writer.ByteOffset, Is.EqualTo(0));
        Assert.That(stream.ToArray(), Is.Empty);
    }

    [Test]
    public void Write_WritesSegmentAndAdvancesOffset()
    {
        MemoryStream stream = new MemoryStream();
        GifStreamByteWriter writer = new GifStreamByteWriter(stream);
        byte[] data = new byte[] { 1, 2, 3, 4 };

        bool success = writer.WriteRange(data, 1, 2);

        Assert.That(success, Is.True);
        Assert.That(writer.ByteOffset, Is.EqualTo(2));
        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 2, 3 }));
    }

    [Test]
    public void Write_RejectsInvalidArgumentsWithoutWriting()
    {
        MemoryStream stream = new MemoryStream();
        GifStreamByteWriter writer = new GifStreamByteWriter(stream);
        byte[] data = new byte[] { 1, 2 };

        Assert.That(writer.Write(null!), Is.False);
        Assert.That(writer.WriteRange(data, -1, 1), Is.False);
        Assert.That(writer.WriteRange(data, 0, -1), Is.False);
        Assert.That(writer.WriteRange(data, 2, 1), Is.False);
        Assert.That(writer.ByteOffset, Is.EqualTo(0));
        Assert.That(stream.ToArray(), Is.Empty);
    }

    [Test]
    public void WriteUInt16LittleEndian_WritesLowByteFirst()
    {
        MemoryStream stream = new MemoryStream();
        GifStreamByteWriter writer = new GifStreamByteWriter(stream);

        bool success = writer.WriteUInt16LittleEndian(0xBEEF);

        Assert.That(success, Is.True);
        Assert.That(writer.ByteOffset, Is.EqualTo(2));
        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 0xEF, 0xBE }));
    }

    [Test]
    public void WriteUInt16LittleEndian_RejectsInvalidValues()
    {
        MemoryStream stream = new MemoryStream();
        GifStreamByteWriter writer = new GifStreamByteWriter(stream);

        Assert.That(writer.WriteUInt16LittleEndian(-1), Is.False);
        Assert.That(writer.WriteUInt16LittleEndian(65536), Is.False);
        Assert.That(writer.ByteOffset, Is.EqualTo(0));
        Assert.That(stream.ToArray(), Is.Empty);
    }

    [Test]
    public void NullOutput_FailsCleanlyWithoutAdvancing()
    {
        GifStreamByteWriter writer = new GifStreamByteWriter(null!);

        bool success = writer.WriteByte(1);

        Assert.That(success, Is.False);
        Assert.That(writer.Failed, Is.True);
        Assert.That(writer.ByteOffset, Is.EqualTo(0));
    }

    [Test]
    public void NonWritableOutput_FailsBeforeWriting()
    {
        MemoryStream stream = new MemoryStream(new byte[2], false);
        GifStreamByteWriter writer = new GifStreamByteWriter(stream);

        bool success = writer.Write(new byte[] { 1 });

        Assert.That(success, Is.False);
        Assert.That(writer.Failed, Is.True);
        Assert.That(writer.ByteOffset, Is.EqualTo(0));
    }

    [Test]
    public void Flush_ReturnsTrueForWritableStream()
    {
        MemoryStream stream = new MemoryStream();
        GifStreamByteWriter writer = new GifStreamByteWriter(stream);

        Assert.That(writer.Flush(), Is.True);
        Assert.That(writer.Failed, Is.False);
    }
}
