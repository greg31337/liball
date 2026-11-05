namespace jpeg.Tests;

internal class ExifTiffReaderTests
{
    [Test]
    public void Reader_ParsesLittleEndianTiffHeaderAndIfdEntries()
    {
        byte[] tiff = CreateLittleEndianTiff();
        global::jpeg.JpegExifTiffReader reader = new global::jpeg.JpegExifTiffReader();

        global::jpeg.JpegExifTiffHeader header = reader.ParseHeader(tiff);
        global::jpeg.JpegExifIfdEntry first = reader.ReadIfdEntry(tiff, header.FirstIfdOffset, 0, header.ByteOrder);
        global::jpeg.JpegExifIfdEntry second = reader.ReadIfdEntry(tiff, header.FirstIfdOffset, 1, header.ByteOrder);
        long nextIfdOffset = reader.ReadNextIfdOffset(tiff, header.FirstIfdOffset, header.ByteOrder);

        Assert.That(header.IsPresent, Is.True);
        Assert.That(header.IsLittleEndian, Is.True);
        Assert.That(header.IsBigEndian, Is.False);
        Assert.That(header.Magic, Is.EqualTo(42));
        Assert.That(header.FirstIfdOffset, Is.EqualTo(8));
        Assert.That(reader.ReadUInt16(tiff, 10, header.ByteOrder), Is.EqualTo(0x010F));
        Assert.That(reader.ReadInt16(new byte[] { 0x00, 0x80 }, 0, header.ByteOrder), Is.EqualTo(-32768));
        Assert.That(reader.ReadUInt32(tiff, 14, header.ByteOrder), Is.EqualTo(6));
        Assert.That(reader.ReadUInt32(tiff, 18, header.ByteOrder), Is.EqualTo(40));
        Assert.That(reader.ReadInt32(new byte[] { 0, 0, 0, 0x80 }, 0, header.ByteOrder), Is.EqualTo(-2147483648L));
        Assert.That(first.EntryOffset, Is.EqualTo(10));
        Assert.That(first.Tag, Is.EqualTo(0x010F));
        Assert.That(first.Type, Is.EqualTo(2));
        Assert.That(first.Count, Is.EqualTo(6));
        Assert.That(first.ValueOffset, Is.EqualTo(40));
        Assert.That(second.Tag, Is.EqualTo(0x0112));
        Assert.That(second.Type, Is.EqualTo(3));
        Assert.That(second.Count, Is.EqualTo(1));
        Assert.That(second.ValueOffset, Is.EqualTo(1));
        Assert.That(nextIfdOffset, Is.EqualTo(0));
    }

    [Test]
    public void Reader_ParsesBigEndianTiffHeaderAndIfdEntries()
    {
        byte[] tiff = CreateBigEndianTiff();
        global::jpeg.JpegExifTiffReader reader = new global::jpeg.JpegExifTiffReader();

        global::jpeg.JpegExifTiffHeader header = reader.ParseHeader(tiff);
        global::jpeg.JpegExifIfdEntry entry = reader.ReadIfdEntry(tiff, header.FirstIfdOffset, 0, header.ByteOrder);

        Assert.That(header.IsPresent, Is.True);
        Assert.That(header.IsBigEndian, Is.True);
        Assert.That(header.IsLittleEndian, Is.False);
        Assert.That(header.Magic, Is.EqualTo(42));
        Assert.That(header.FirstIfdOffset, Is.EqualTo(8));
        Assert.That(reader.ReadUInt16(tiff, 10, header.ByteOrder), Is.EqualTo(0x0100));
        Assert.That(reader.ReadInt16(new byte[] { 0x80, 0x00 }, 0, header.ByteOrder), Is.EqualTo(-32768));
        Assert.That(reader.ReadUInt32(tiff, 14, header.ByteOrder), Is.EqualTo(1));
        Assert.That(reader.ReadUInt32(tiff, 18, header.ByteOrder), Is.EqualTo(640));
        Assert.That(reader.ReadInt32(new byte[] { 0x80, 0, 0, 0 }, 0, header.ByteOrder), Is.EqualTo(-2147483648L));
        Assert.That(entry.Tag, Is.EqualTo(0x0100));
        Assert.That(entry.Type, Is.EqualTo(4));
        Assert.That(entry.Count, Is.EqualTo(1));
        Assert.That(entry.ValueOffset, Is.EqualTo(640));
    }

    [Test]
    public void Reader_RejectsMalformedTiffData()
    {
        global::jpeg.JpegExifTiffReader reader = new global::jpeg.JpegExifTiffReader();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                reader.ParseHeader(null!);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                reader.ParseHeader(new byte[] { 1, 2, 3 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                reader.ParseHeader(new byte[] { 0x4E, 0x4E, 0, 42, 0, 0, 0, 0 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                reader.ParseHeader(new byte[] { 0x4D, 0x4D, 0, 41, 0, 0, 0, 0 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                reader.ParseHeader(new byte[] { 0x4D, 0x4D, 0, 42, 0, 0, 0, 12 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                reader.ReadUInt16(new byte[] { 1, 2 }, 0, global::jpeg.JpegTiffByteOrder.Unknown);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                reader.ReadIfdEntry(CreateLittleEndianTiff(), 8, -1, global::jpeg.JpegTiffByteOrder.LittleEndian);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                reader.ReadIfdEntry(CreateLittleEndianTiff(), 8, 2, global::jpeg.JpegTiffByteOrder.LittleEndian);
            });
    }

    public static byte[] CreateLittleEndianTiff()
    {
        byte[] tiff = new byte[44];
        tiff[0] = 0x49;
        tiff[1] = 0x49;
        tiff[2] = 42;
        tiff[3] = 0;
        tiff[4] = 8;
        tiff[8] = 2;
        tiff[10] = 0x0F;
        tiff[11] = 0x01;
        tiff[12] = 2;
        tiff[14] = 6;
        tiff[18] = 40;
        tiff[22] = 0x12;
        tiff[23] = 0x01;
        tiff[24] = 3;
        tiff[26] = 1;
        tiff[30] = 1;
        tiff[40] = 0x43;
        tiff[41] = 0x41;
        tiff[42] = 0x4D;
        tiff[43] = 0;
        return tiff;
    }

    public static byte[] CreateBigEndianTiff()
    {
        byte[] tiff = new byte[26];
        tiff[0] = 0x4D;
        tiff[1] = 0x4D;
        tiff[3] = 42;
        tiff[7] = 8;
        tiff[9] = 1;
        tiff[10] = 0x01;
        tiff[13] = 4;
        tiff[17] = 1;
        tiff[20] = 2;
        tiff[21] = 0x80;
        return tiff;
    }
}
