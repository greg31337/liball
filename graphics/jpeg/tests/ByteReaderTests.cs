namespace jpeg.Tests;

internal class ByteReaderTests
{
    [Test]
    public void ByteArrayReader_ReadsBigEndianValuesAndTracksOffsets()
    {
        byte[] data = new byte[] { 0x12, 0x34, 0x80, 0x00, 0x01, 0x02, 0x03, 0x04 };
        global::jpeg.JpegByteReader reader = new global::jpeg.JpegByteReader(data);

        Assert.That(reader.ReadUInt16(), Is.EqualTo(0x1234));
        Assert.That(reader.Offset, Is.EqualTo(2));
        Assert.That(reader.AbsoluteOffset, Is.EqualTo(2));
        Assert.That(reader.ReadInt16(), Is.EqualTo(-32768));
        Assert.That(reader.ReadUInt32(), Is.EqualTo(0x01020304));
        Assert.That(reader.Remaining(), Is.EqualTo(0));
        Assert.That(reader.InputStream, Is.Null);
    }

    [Test]
    public void StreamReader_ReadsBigEndianValuesAndTracksAbsoluteOffset()
    {
        byte[] data = new byte[] { 0xFF, 0xD8, 0x00, 0x10, 0x7F, 0xFF };
        System.IO.MemoryStream stream = new System.IO.MemoryStream(data);
        global::jpeg.JpegByteReader reader = global::jpeg.JpegByteReader.FromStream(stream);

        Assert.That(reader.ReadUInt16(), Is.EqualTo(global::jpeg.JpegConstants.MarkerStartOfImage));
        Assert.That(reader.ReadUInt16(), Is.EqualTo(16));
        Assert.That(reader.ReadInt16(), Is.EqualTo(32767));
        Assert.That(reader.AbsoluteOffset, Is.EqualTo(6));
        Assert.That(reader.Remaining(), Is.EqualTo(-1));
    }

    [Test]
    public void Reader_ReadBytesAndSkipAdvanceConsistently()
    {
        byte[] data = new byte[] { 1, 2, 3, 4, 5, 6 };
        global::jpeg.JpegByteReader reader = new global::jpeg.JpegByteReader(data);

        Assert.That(reader.ReadBytes(2), Is.EqualTo(new byte[] { 1, 2 }));
        reader.Skip(2);

        Assert.That(reader.ReadByte(), Is.EqualTo(5));
        Assert.That(reader.Offset, Is.EqualTo(5));
        Assert.That(reader.AbsoluteOffset, Is.EqualTo(5));
    }

    [Test]
    public void StaticByteArrayHelpers_ReadBigEndianValuesAtOffsets()
    {
        byte[] data = new byte[] { 0x00, 0xFE, 0xDC, 0x89, 0xAB, 0xCD };

        Assert.That(global::jpeg.JpegByteReader.ReadUInt16(data, 1), Is.EqualTo(0xFEDC));
        Assert.That(global::jpeg.JpegByteReader.ReadInt16(data, 1), Is.EqualTo(-292));
        Assert.That(global::jpeg.JpegByteReader.ReadUInt32(data, 2), Is.EqualTo(0xDC89ABCD));
    }

    [Test]
    public void Reader_ThrowsOnUnexpectedEnd()
    {
        global::jpeg.JpegByteReader arrayReader = new global::jpeg.JpegByteReader(new byte[] { 0xFF });
        global::jpeg.JpegByteReader streamReader = global::jpeg.JpegByteReader.FromStream(new System.IO.MemoryStream(new byte[] { 0xFF }));

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                arrayReader.ReadUInt16();
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                streamReader.ReadUInt16();
            });
    }

    [Test]
    public void RangeValidation_RejectsInvalidByteArrayRanges()
    {
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.JpegByteReader.ReadUInt16(null!, 0);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                global::jpeg.JpegByteReader.ReadUInt16(new byte[] { 1, 2 }, -1);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegByteReader.ReadUInt16(new byte[] { 1 }, 0);
            });
    }
}
