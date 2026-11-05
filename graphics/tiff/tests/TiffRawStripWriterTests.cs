using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffRawStripWriterTests
{
    [Test]
    public void WriteStrip_WritesRequestedRawBytes()
    {
        TiffImageInfo info = CreateInfo(10, 10, 4);
        byte[] destination = new byte[64];
        ulong[] offsets = new ulong[] { 20, 30, 40 };
        ulong[] byteCounts = new ulong[] { 4, 3, 2 };

        TiffRawStripWriteResult result = TiffRawStripWriter.WriteStrip(info, offsets, byteCounts, destination, 1, new byte[] { 9, 8, 7 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.StripIndex, Is.EqualTo(1));
        Assert.That(result.BytesWritten, Is.EqualTo(3));
        Assert.That(result.Range.Offset, Is.EqualTo(30));
        Assert.That(destination[29], Is.EqualTo(0));
        Assert.That(destination[30], Is.EqualTo(9));
        Assert.That(destination[31], Is.EqualTo(8));
        Assert.That(destination[32], Is.EqualTo(7));
        Assert.That(destination[33], Is.EqualTo(0));
    }

    [Test]
    public void WriteStrip_RejectsMismatchedSourceByteCount()
    {
        TiffImageInfo info = CreateInfo(10, 10, 4);
        byte[] destination = new byte[64];
        ulong[] offsets = new ulong[] { 20, 30, 40 };
        ulong[] byteCounts = new ulong[] { 4, 3, 2 };

        TiffRawStripWriteResult result = TiffRawStripWriter.WriteStrip(info, offsets, byteCounts, destination, 1, new byte[] { 9, 8 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(destination[30], Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void WriteStrip_ReportsOutOfRangeStripIndex()
    {
        TiffImageInfo info = CreateInfo(10, 10, 4);
        byte[] destination = new byte[64];
        ulong[] offsets = new ulong[] { 20, 30, 40 };
        ulong[] byteCounts = new ulong[] { 4, 3, 2 };

        TiffRawStripWriteResult result = TiffRawStripWriter.WriteStrip(info, offsets, byteCounts, destination, 3, new byte[] { 1 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
    }

    [Test]
    public void WriteStrip_ReportsDestinationRangeOutsideBuffer()
    {
        TiffImageInfo info = CreateInfo(10, 10, 10);
        byte[] destination = new byte[50];
        ulong[] offsets = new ulong[] { 48 };
        ulong[] byteCounts = new ulong[] { 4 };

        TiffRawStripWriteResult result = TiffRawStripWriter.WriteStrip(info, offsets, byteCounts, destination, 0, new byte[] { 1, 2, 3, 4 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void WriteStrip_AllowsEmptySparseWrite()
    {
        TiffImageInfo info = CreateInfo(10, 10, 4);
        byte[] destination = new byte[64];
        ulong[] offsets = new ulong[] { 0, 30, 40 };
        ulong[] byteCounts = new ulong[] { 4, 3, 2 };

        TiffRawStripWriteResult result = TiffRawStripWriter.WriteStrip(info, offsets, byteCounts, destination, 0, new byte[0]);

        Assert.That(result.Success, Is.True);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(result.Range.IsSparse, Is.True);
    }

    [Test]
    public void WriteStrip_RejectsNonEmptySparseWrite()
    {
        TiffImageInfo info = CreateInfo(10, 10, 4);
        byte[] destination = new byte[64];
        ulong[] offsets = new ulong[] { 0, 30, 40 };
        ulong[] byteCounts = new ulong[] { 4, 3, 2 };

        TiffRawStripWriteResult result = TiffRawStripWriter.WriteStrip(info, offsets, byteCounts, destination, 0, new byte[] { 1 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void WriteStrip_FromDirectoryLoadsStripOffsetAndByteCountTags()
    {
        TiffByteOrder byteOrder = TiffByteOrder.LittleEndian;
        byte[] destination = CreateFileBytes(32);
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateShortEntry(TiffTagId.ImageWidth, 1, byteOrder),
            CreateShortEntry(TiffTagId.ImageLength, 2, byteOrder),
            CreateShortEntry(TiffTagId.PhotometricInterpretation, (ushort)TiffPhotometric.BlackIsZero, byteOrder),
            CreateShortEntry(TiffTagId.RowsPerStrip, 1, byteOrder),
            CreateShortArrayEntry(TiffTagId.StripOffsets, new ushort[] { 20, 24 }, byteOrder),
            CreateShortArrayEntry(TiffTagId.StripByteCounts, new ushort[] { 3, 2 }, byteOrder)
        };
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, entries);

        TiffRawStripWriteResult result = TiffRawStripWriter.WriteStrip(directory, destination, byteOrder, 1, new byte[] { 88, 89 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.BytesWritten, Is.EqualTo(2));
        Assert.That(result.Range.Offset, Is.EqualTo(24));
        Assert.That(destination[24], Is.EqualTo(88));
        Assert.That(destination[25], Is.EqualTo(89));
        Assert.That(destination[26], Is.EqualTo(26));
    }

    [Test]
    public void WriteStrip_FromDirectoryReportsMissingStripOffsets()
    {
        TiffByteOrder byteOrder = TiffByteOrder.LittleEndian;
        byte[] destination = CreateFileBytes(32);
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateShortEntry(TiffTagId.ImageWidth, 1, byteOrder),
            CreateShortEntry(TiffTagId.ImageLength, 2, byteOrder),
            CreateShortEntry(TiffTagId.PhotometricInterpretation, (ushort)TiffPhotometric.BlackIsZero, byteOrder),
            CreateShortEntry(TiffTagId.RowsPerStrip, 1, byteOrder),
            CreateShortArrayEntry(TiffTagId.StripByteCounts, new ushort[] { 3, 2 }, byteOrder)
        };
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, entries);

        TiffRawStripWriteResult result = TiffRawStripWriter.WriteStrip(directory, destination, byteOrder, 0, new byte[] { 1, 2, 3 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.MissingRequiredTag), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.StripOffsets));
    }

    static TiffImageInfo CreateInfo(ulong width, ulong length, ulong rowsPerStrip)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.RowsPerStrip = rowsPerStrip;
        info.SamplesPerPixel = 1;
        info.BitsPerSample = new int[] { 8 };
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

    static TiffIfdEntry CreateShortEntry(int tagId, ushort value, TiffByteOrder byteOrder)
    {
        return CreateShortArrayEntry(tagId, new ushort[] { value }, byteOrder);
    }

    static TiffIfdEntry CreateShortArrayEntry(int tagId, ushort[] values, TiffByteOrder byteOrder)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = TiffDataType.Short;
        entry.Count = values.Length;
        entry.RawCount = (ulong)values.Length;
        entry.ValueByteCount = values.Length * 2;
        entry.ValueSlotByteCount = 4;
        entry.ValueOffsetBytes = new byte[4];

        int index = 0;
        while (index < values.Length)
        {
            TiffBinaryWriter.WriteUInt16(entry.ValueOffsetBytes.AsSpan(index * 2, 2), values[index], byteOrder);
            index = index + 1;
        }

        return entry;
    }
}
