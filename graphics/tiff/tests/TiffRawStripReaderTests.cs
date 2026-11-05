using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffRawStripReaderTests
{
    [Test]
    public void ReadStrip_ReadsRequestedRawBytes()
    {
        TiffImageInfo info = CreateInfo(10, 10, 4);
        byte[] fileBytes = CreateFileBytes(64);
        TiffDiagnostics diagnostics = TiffDiagnostics.Create();
        ulong[] offsets = new ulong[] { 20, 30, 40 };
        ulong[] byteCounts = new ulong[] { 4, 3, 2 };

        TiffRawStripReadResult result = TiffRawStripReader.ReadStrip(info, offsets, byteCounts, fileBytes, 1);

        Assert.That(result.Success, Is.True);
        Assert.That(result.StripIndex, Is.EqualTo(1));
        Assert.That(result.Range.Offset, Is.EqualTo(30));
        Assert.That(result.Range.ByteCount, Is.EqualTo(3));
        Assert.That(result.Range.EndOffset, Is.EqualTo(33));
        Assert.That(result.IsSparse, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 30, 31, 32 }));
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void ReadStrip_ReturnsEmptyBytesForSparseStrip()
    {
        TiffImageInfo info = CreateInfo(10, 10, 4);
        byte[] fileBytes = CreateFileBytes(64);
        ulong[] offsets = new ulong[] { 0, 30, 40 };
        ulong[] byteCounts = new ulong[] { 4, 3, 2 };

        TiffRawStripReadResult result = TiffRawStripReader.ReadStrip(info, offsets, byteCounts, fileBytes, 0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IsSparse, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
    }

    [Test]
    public void ReadStrip_ReportsOutOfRangeStripIndex()
    {
        TiffImageInfo info = CreateInfo(10, 10, 4);
        byte[] fileBytes = CreateFileBytes(64);
        ulong[] offsets = new ulong[] { 20, 30, 40 };
        ulong[] byteCounts = new ulong[] { 4, 3, 2 };

        TiffRawStripReadResult result = TiffRawStripReader.ReadStrip(info, offsets, byteCounts, fileBytes, 3);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
    }

    [Test]
    public void ReadStrip_ReportsByteRangeOutsideFile()
    {
        TiffImageInfo info = CreateInfo(10, 10, 10);
        byte[] fileBytes = CreateFileBytes(50);
        ulong[] offsets = new ulong[] { 48 };
        ulong[] byteCounts = new ulong[] { 4 };

        TiffRawStripReadResult result = TiffRawStripReader.ReadStrip(info, offsets, byteCounts, fileBytes, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void ReadStrip_RespectsAllocationLimit()
    {
        TiffImageInfo info = CreateInfo(10, 10, 10);
        byte[] fileBytes = CreateFileBytes(64);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 6 };
        TiffLimits limits = new TiffLimits();
        limits.MaximumAllocationBytes = 5;

        TiffRawStripReadResult result = TiffRawStripReader.ReadStrip(info, offsets, byteCounts, fileBytes, 0, limits);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.AllocationLimitExceeded), Is.True);
    }

    [Test]
    public void ReadStrip_FromDirectoryLoadsStripOffsetAndByteCountTags()
    {
        TiffByteOrder byteOrder = TiffByteOrder.LittleEndian;
        byte[] fileBytes = CreateFileBytes(32);
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

        TiffRawStripReadResult result = TiffRawStripReader.ReadStrip(directory, fileBytes, byteOrder, 1);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Range.Offset, Is.EqualTo(24));
        Assert.That(result.Range.ByteCount, Is.EqualTo(2));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 24, 25 }));
    }

    [Test]
    public void ReadStrip_FromDirectoryReportsMissingStripByteCounts()
    {
        TiffByteOrder byteOrder = TiffByteOrder.LittleEndian;
        byte[] fileBytes = CreateFileBytes(32);
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateShortEntry(TiffTagId.ImageWidth, 1, byteOrder),
            CreateShortEntry(TiffTagId.ImageLength, 2, byteOrder),
            CreateShortEntry(TiffTagId.PhotometricInterpretation, (ushort)TiffPhotometric.BlackIsZero, byteOrder),
            CreateShortEntry(TiffTagId.RowsPerStrip, 1, byteOrder),
            CreateShortArrayEntry(TiffTagId.StripOffsets, new ushort[] { 20, 24 }, byteOrder)
        };
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, entries);

        TiffRawStripReadResult result = TiffRawStripReader.ReadStrip(directory, fileBytes, byteOrder, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.MissingRequiredTag), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.StripByteCounts));
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
