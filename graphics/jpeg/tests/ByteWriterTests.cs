namespace jpeg.Tests;

internal class ByteWriterTests
{
    [Test]
    public void ExpandableArrayWriter_WritesBigEndianValuesAndGrows()
    {
        global::jpeg.JpegByteWriter writer = global::jpeg.JpegByteWriter.WithCapacity(1);

        writer.WriteUInt16(global::jpeg.JpegConstants.MarkerStartOfImage);
        writer.WriteUInt16(0x1234);
        writer.WriteInt16(-2);
        writer.WriteUInt32(0x89ABCDEFL);

        Assert.That(writer.Length, Is.EqualTo(10));
        Assert.That(writer.AbsoluteOffset, Is.EqualTo(10));
        Assert.That(
            writer.ToArray(),
            Is.EqualTo(new byte[] { 0xFF, 0xD8, 0x12, 0x34, 0xFF, 0xFE, 0x89, 0xAB, 0xCD, 0xEF }));
        Assert.That(writer.Buffer.Length, Is.GreaterThanOrEqualTo(10));
        Assert.That(writer.OutputStream, Is.Null);
    }

    [Test]
    public void StreamWriter_WritesBigEndianValuesToStream()
    {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        global::jpeg.JpegByteWriter writer = global::jpeg.JpegByteWriter.FromStream(stream);

        writer.WriteByte(0xFF);
        writer.WriteByte(0xD8);
        writer.WriteUInt16(16);
        writer.WriteBytesRange(new byte[] { 1, 2, 3, 4 }, 1, 2);

        Assert.That(writer.AbsoluteOffset, Is.EqualTo(6));
        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 0xFF, 0xD8, 0x00, 0x10, 2, 3 }));
    }

    [Test]
    public void StaticByteArrayHelpers_WriteBigEndianValuesAtOffsets()
    {
        byte[] data = new byte[8];

        global::jpeg.JpegByteWriter.WriteByte(data, 0, 0xFF);
        global::jpeg.JpegByteWriter.WriteUInt16(data, 1, 0xD800);
        global::jpeg.JpegByteWriter.WriteInt16(data, 3, -1);
        global::jpeg.JpegByteWriter.WriteUInt32(data, 4, 0x12345678L);

        Assert.That(data, Is.EqualTo(new byte[] { 0xFF, 0xD8, 0x00, 0xFF, 0x12, 0x34, 0x56, 0x78 }));
    }

    [Test]
    public void ToArray_ReturnsCopyOfOwnedBuffer()
    {
        global::jpeg.JpegByteWriter writer = new global::jpeg.JpegByteWriter();
        writer.WriteBytes(new byte[] { 1, 2, 3 });

        byte[] first = writer.ToArray();
        first[0] = 9;

        Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void Writer_RejectsInvalidValuesAndRanges()
    {
        global::jpeg.JpegByteWriter writer = new global::jpeg.JpegByteWriter();

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                writer.WriteByte(256);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                writer.WriteUInt16(65536);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                writer.WriteInt16(-32769);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                writer.WriteUInt32(4294967296L);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegByteWriter.WriteUInt16(new byte[] { 0 }, 0, 1);
            });
    }

    [Test]
    public void Writer_RejectsNonWritableStreamsAndStreamToArray()
    {
        System.IO.MemoryStream nonWritable = new System.IO.MemoryStream(new byte[] { 1 }, false);

        Assert.Throws<System.ArgumentException>(
            delegate
            {
                global::jpeg.JpegByteWriter.FromStream(nonWritable);
            });

        System.IO.MemoryStream writable = new System.IO.MemoryStream();
        global::jpeg.JpegByteWriter writer = global::jpeg.JpegByteWriter.FromStream(writable);

        Assert.Throws<System.InvalidOperationException>(
            delegate
            {
                writer.ToArray();
            });
    }
}
