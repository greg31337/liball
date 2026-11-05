using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffDecodedStripReaderTests
{
    [Test]
    public void ReadStrip_DecodesNoCompressionRawBytes()
    {
        TiffImageInfo info = CreateInfo(3, 2, 2, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 6 };

        TiffDecodedStripReadResult result = TiffDecodedStripReader.ReadStrip(info, offsets, byteCounts, fileBytes, 0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.StripIndex, Is.EqualTo(0));
        Assert.That(result.ExpectedDecodedByteCount, Is.EqualTo(6));
        Assert.That(result.Compression, Is.EqualTo(TiffCompression.None));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 20, 21, 22, 23, 24, 25 }));
    }

    [Test]
    public void ReadStrip_FromDirectoryUsesCodecDispatcher()
    {
        TiffByteOrder byteOrder = TiffByteOrder.LittleEndian;
        byte[] fileBytes = CreateFileBytes(40);
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

        TiffDecodedStripReadResult result = TiffDecodedStripReader.ReadStrip(directory, fileBytes, byteOrder, 0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ExpectedDecodedByteCount, Is.EqualTo(6));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 20, 21, 22, 23, 24, 25 }));
    }

    [Test]
    public void ReadStrip_ReportsUnsupportedCompressionAndPreservesRawBytes()
    {
        TiffImageInfo info = CreateInfo(3, 2, 2, TiffCompression.Jpeg);
        byte[] fileBytes = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 2 };

        TiffDecodedStripReadResult result = TiffDecodedStripReader.ReadStrip(info, offsets, byteCounts, fileBytes, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 20, 21 }));
        Assert.That(result.ExpectedDecodedByteCount, Is.EqualTo(6));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedCompression), Is.True);
    }

    [Test]
    public void ReadStrip_DecodesDeflateCompressedBytes()
    {
        TiffImageInfo info = CreateInfo(3, 2, 2, TiffCompression.Deflate);
        byte[] fileBytes = CreateFileBytes(60);
        byte[] compressedBytes = new byte[] { 120, 156, 99, 100, 98, 102, 97, 101, 3, 0, 0, 62, 0, 22 };
        CopyBytes(compressedBytes, fileBytes, 20);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 14 };

        TiffDecodedStripReadResult result = TiffDecodedStripReader.ReadStrip(info, offsets, byteCounts, fileBytes, 0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.StripIndex, Is.EqualTo(0));
        Assert.That(result.ExpectedDecodedByteCount, Is.EqualTo(6));
        Assert.That(result.Compression, Is.EqualTo(TiffCompression.Deflate));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void ReadStrip_DecodesLzwCompressedBytes()
    {
        TiffImageInfo info = CreateInfo(3, 1, 1, TiffCompression.Lzw);
        byte[] fileBytes = CreateFileBytes(60);
        byte[] compressedBytes = new byte[] { 128, 16, 72, 68, 56, 8 };
        CopyBytes(compressedBytes, fileBytes, 20);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 6 };

        TiffDecodedStripReadResult result = TiffDecodedStripReader.ReadStrip(info, offsets, byteCounts, fileBytes, 0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ExpectedDecodedByteCount, Is.EqualTo(3));
        Assert.That(result.Compression, Is.EqualTo(TiffCompression.Lzw));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 65, 66, 67 }));
    }

    [Test]
    public void ReadStrip_EnforcesDecodedExpansionLimit()
    {
        TiffImageInfo info = CreateInfo(3, 2, 2, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 6 };
        TiffLimits limits = new TiffLimits();
        limits.MaximumDecodedBytes = 5;

        TiffDecodedStripReadResult result = TiffDecodedStripReader.ReadStrip(info, offsets, byteCounts, fileBytes, 0, limits);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.CodecExpansionLimitExceeded), Is.True);
    }

    [Test]
    public void ReadStrip_PropagatesRawReadDiagnostics()
    {
        TiffImageInfo info = CreateInfo(3, 2, 2, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(24);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 6 };

        TiffDecodedStripReadResult result = TiffDecodedStripReader.ReadStrip(info, offsets, byteCounts, fileBytes, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
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

    static void CopyBytes(byte[] source, byte[] destination, int destinationIndex)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination[destinationIndex + index] = source[index];
            index = index + 1;
        }
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
