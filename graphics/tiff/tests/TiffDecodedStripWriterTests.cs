using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffDecodedStripWriterTests
{
    [Test]
    public void WriteStrip_EncodesNoCompressionAndWritesRawBytes()
    {
        TiffImageInfo info = CreateInfo(3, 2, 2, TiffCompression.None);
        byte[] destination = new byte[40];
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 6 };

        TiffDecodedStripWriteResult result = TiffDecodedStripWriter.WriteStrip(info, offsets, byteCounts, destination, 0, new byte[] { 1, 2, 3, 4, 5, 6 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.StripIndex, Is.EqualTo(0));
        Assert.That(result.ExpectedDecodedByteCount, Is.EqualTo(6));
        Assert.That(result.Compression, Is.EqualTo(TiffCompression.None));
        Assert.That(result.BytesWritten, Is.EqualTo(6));
        Assert.That(destination[20], Is.EqualTo(1));
        Assert.That(destination[25], Is.EqualTo(6));
    }

    [Test]
    public void WriteStrip_FromDirectoryUsesCodecDispatcher()
    {
        TiffByteOrder byteOrder = TiffByteOrder.LittleEndian;
        byte[] destination = CreateFileBytes(40);
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateShortEntry(TiffTagId.ImageWidth, 3, byteOrder),
            CreateShortEntry(TiffTagId.ImageLength, 2, byteOrder),
            CreateShortEntry(TiffTagId.BitsPerSample, 8, byteOrder),
            CreateShortEntry(TiffTagId.PhotometricInterpretation, (ushort)TiffPhotometric.BlackIsZero, byteOrder),
            CreateShortEntry(TiffTagId.RowsPerStrip, 2, byteOrder),
            CreateShortEntry(TiffTagId.StripOffsets, 20, byteOrder),
            CreateShortEntry(TiffTagId.StripByteCounts, 6, byteOrder)
        };
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, entries);

        TiffDecodedStripWriteResult result = TiffDecodedStripWriter.WriteStrip(directory, destination, byteOrder, 0, new byte[] { 9, 8, 7, 6, 5, 4 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.ExpectedDecodedByteCount, Is.EqualTo(6));
        Assert.That(result.BytesWritten, Is.EqualTo(6));
        Assert.That(destination[20], Is.EqualTo(9));
        Assert.That(destination[25], Is.EqualTo(4));
        Assert.That(destination[26], Is.EqualTo(26));
    }

    [Test]
    public void WriteStrip_ReportsUnsupportedCompressionFromDispatcher()
    {
        TiffImageInfo info = CreateInfo(3, 2, 2, TiffCompression.Jpeg);
        byte[] destination = new byte[40];
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 6 };

        TiffDecodedStripWriteResult result = TiffDecodedStripWriter.WriteStrip(info, offsets, byteCounts, destination, 0, new byte[] { 1, 2, 3, 4, 5, 6 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(destination[20], Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedCompression), Is.True);
    }

    [Test]
    public void WriteStrip_EncodesPackBitsAndWritesRawBytes()
    {
        TiffImageInfo info = CreateInfo(6, 1, 1, TiffCompression.PackBits);
        byte[] destination = new byte[40];
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 7 };

        TiffDecodedStripWriteResult result = TiffDecodedStripWriter.WriteStrip(info, offsets, byteCounts, destination, 0, new byte[] { 10, 11, 7, 7, 7, 12 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.StripIndex, Is.EqualTo(0));
        Assert.That(result.ExpectedDecodedByteCount, Is.EqualTo(6));
        Assert.That(result.Compression, Is.EqualTo(TiffCompression.PackBits));
        Assert.That(result.BytesWritten, Is.EqualTo(7));
        Assert.That(destination[20], Is.EqualTo(1));
        Assert.That(destination[21], Is.EqualTo(10));
        Assert.That(destination[22], Is.EqualTo(11));
        Assert.That(destination[23], Is.EqualTo(254));
        Assert.That(destination[24], Is.EqualTo(7));
        Assert.That(destination[25], Is.EqualTo(0));
        Assert.That(destination[26], Is.EqualTo(12));
        Assert.That(destination[27], Is.EqualTo(0));
    }

    [Test]
    public void WriteStrip_EncodesDeflateAndWritesRawBytes()
    {
        TiffImageInfo info = CreateInfo(17, 1, 1, TiffCompression.Deflate);
        byte[] decodedBytes = new byte[] { 104, 101, 108, 108, 111, 32, 104, 101, 108, 108, 111, 32, 104, 101, 108, 108, 111 };
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 17, new TiffLimits());
        TiffCodecResult expectedEncoded = TiffDeflateCodec.Encode(context, decodedBytes);
        byte[] destination = new byte[100];
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { (ulong)expectedEncoded.Bytes.Length };

        TiffDecodedStripWriteResult result = TiffDecodedStripWriter.WriteStrip(info, offsets, byteCounts, destination, 0, decodedBytes);
        byte[] writtenBytes = ExtractBytes(destination, 20, expectedEncoded.Bytes.Length);
        TiffCodecResult decodedResult = TiffDeflateCodec.Decode(context, writtenBytes);

        Assert.That(expectedEncoded.Success, Is.True);
        Assert.That(result.Success, Is.True);
        Assert.That(result.ExpectedDecodedByteCount, Is.EqualTo(17));
        Assert.That(result.Compression, Is.EqualTo(TiffCompression.Deflate));
        Assert.That(result.BytesWritten, Is.EqualTo((ulong)expectedEncoded.Bytes.Length));
        Assert.That(writtenBytes, Is.EqualTo(expectedEncoded.Bytes));
        Assert.That(decodedResult.Success, Is.True);
        Assert.That(decodedResult.Bytes, Is.EqualTo(decodedBytes));
    }

    [Test]
    public void WriteStrip_EncodesLzwAndWritesRawBytes()
    {
        TiffImageInfo info = CreateInfo(7, 1, 1, TiffCompression.Lzw);
        byte[] decodedBytes = new byte[] { 97, 98, 97, 98, 97, 98, 97 };
        byte[] expectedEncodedBytes = new byte[] { 128, 24, 76, 80, 40, 36, 4 };
        byte[] destination = new byte[60];
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 7 };
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 7, new TiffLimits());

        TiffDecodedStripWriteResult result = TiffDecodedStripWriter.WriteStrip(info, offsets, byteCounts, destination, 0, decodedBytes);
        byte[] writtenBytes = ExtractBytes(destination, 20, expectedEncodedBytes.Length);
        TiffCodecResult decodedResult = TiffLzwCodec.Decode(context, writtenBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ExpectedDecodedByteCount, Is.EqualTo(7));
        Assert.That(result.Compression, Is.EqualTo(TiffCompression.Lzw));
        Assert.That(result.BytesWritten, Is.EqualTo(7));
        Assert.That(writtenBytes, Is.EqualTo(expectedEncodedBytes));
        Assert.That(decodedResult.Success, Is.True);
        Assert.That(decodedResult.Bytes, Is.EqualTo(decodedBytes));
    }

    [Test]
    public void WriteStrip_RejectsDecodedByteCountMismatch()
    {
        TiffImageInfo info = CreateInfo(3, 2, 2, TiffCompression.None);
        byte[] destination = new byte[40];
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 6 };

        TiffDecodedStripWriteResult result = TiffDecodedStripWriter.WriteStrip(info, offsets, byteCounts, destination, 0, new byte[] { 1, 2, 3, 4, 5 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(destination[20], Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void WriteStrip_EnforcesDecodedExpansionLimit()
    {
        TiffImageInfo info = CreateInfo(3, 2, 2, TiffCompression.None);
        byte[] destination = new byte[40];
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 6 };
        TiffLimits limits = new TiffLimits();
        limits.MaximumDecodedBytes = 5;

        TiffDecodedStripWriteResult result = TiffDecodedStripWriter.WriteStrip(info, offsets, byteCounts, destination, 0, new byte[] { 1, 2, 3, 4, 5, 6 }, limits);

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.CodecExpansionLimitExceeded), Is.True);
    }

    [Test]
    public void WriteStrip_PropagatesRawWriteDiagnostics()
    {
        TiffImageInfo info = CreateInfo(3, 2, 2, TiffCompression.None);
        byte[] destination = new byte[40];
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 5 };

        TiffDecodedStripWriteResult result = TiffDecodedStripWriter.WriteStrip(info, offsets, byteCounts, destination, 0, new byte[] { 1, 2, 3, 4, 5, 6 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    static TiffImageInfo CreateInfo(ulong width, ulong length, ulong rowsPerStrip, TiffCompression compression)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.RowsPerStrip = rowsPerStrip;
        info.SamplesPerPixel = 1;
        info.BitsPerSample = new int[] { 8 };
        info.Compression = compression;
        return info;
    }

    static byte[] CreateFileBytes(int length)
    {
        byte[] bytes = new byte[length];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)index;
            index = index + 1;
        }

        return bytes;
    }

    static byte[] ExtractBytes(byte[] source, int sourceIndex, int length)
    {
        byte[] bytes = new byte[length];
        int index = 0;
        while (index < length)
        {
            bytes[index] = source[sourceIndex + index];
            index = index + 1;
        }

        return bytes;
    }

    static TiffIfdEntry CreateShortEntry(int tagId, ushort value, TiffByteOrder byteOrder)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = TiffDataType.Short;
        entry.Count = 1;
        entry.RawCount = 1;
        entry.ValueByteCount = 2;
        entry.ValueSlotByteCount = 4;
        entry.ValueOffsetBytes = new byte[4];
        TiffBinaryWriter.WriteUInt16(entry.ValueOffsetBytes.AsSpan(0, 2), value, byteOrder);
        return entry;
    }
}
