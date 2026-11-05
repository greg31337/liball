using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffStripRegionReaderTests
{
    [Test]
    public void ReadRegion_ReadsContiguousWindowAcrossStrips()
    {
        TiffImageInfo info = CreateInfo(5, 4, 1, new int[] { 8 }, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(64);
        ulong[] offsets = new ulong[] { 20, 30 };
        ulong[] byteCounts = new ulong[] { 10, 10 };

        TiffRegionReadResult result = TiffStripRegionReader.ReadRegion(info, offsets, byteCounts, fileBytes, 1, 1, 3, 2);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Column, Is.EqualTo(1));
        Assert.That(result.Row, Is.EqualTo(1));
        Assert.That(result.Width, Is.EqualTo(3));
        Assert.That(result.Length, Is.EqualTo(2));
        Assert.That(result.RowByteCount, Is.EqualTo(3));
        Assert.That(result.TotalByteCount, Is.EqualTo(6));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 26, 27, 28, 31, 32, 33 }));
    }

    [Test]
    public void ReadRegion_FromDirectoryUsesScanlineReader()
    {
        TiffByteOrder byteOrder = TiffByteOrder.LittleEndian;
        byte[] fileBytes = CreateFileBytes(48);
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateShortEntry(TiffTagId.ImageWidth, 5, byteOrder),
            CreateShortEntry(TiffTagId.ImageLength, 2, byteOrder),
            CreateShortEntry(TiffTagId.BitsPerSample, 8, byteOrder),
            CreateShortEntry(TiffTagId.PhotometricInterpretation, (ushort)TiffPhotometric.BlackIsZero, byteOrder),
            CreateShortEntry(TiffTagId.RowsPerStrip, 2, byteOrder),
            CreateShortEntry(TiffTagId.StripOffsets, 20, byteOrder),
            CreateShortEntry(TiffTagId.StripByteCounts, 10, byteOrder)
        };
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, entries);

        TiffRegionReadResult result = TiffStripRegionReader.ReadRegion(directory, fileBytes, byteOrder, 2, 0, 2, 2);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RowByteCount, Is.EqualTo(2));
        Assert.That(result.TotalByteCount, Is.EqualTo(4));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 22, 23, 27, 28 }));
    }

    [Test]
    public void ReadRegion_MapsSeparateSamplePlaneRows()
    {
        TiffImageInfo info = CreateInfo(4, 4, 2, new int[] { 8, 8 }, 2, TiffPlanarConfiguration.Separate, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(64);
        ulong[] offsets = new ulong[] { 20, 28, 36, 44 };
        ulong[] byteCounts = new ulong[] { 8, 8, 8, 8 };

        TiffRegionReadResult result = TiffStripRegionReader.ReadRegion(info, offsets, byteCounts, fileBytes, 1, 3, 2, 1, 1);

        Assert.That(result.Success, Is.True);
        Assert.That(result.SampleIndex, Is.EqualTo(1));
        Assert.That(result.RowByteCount, Is.EqualTo(2));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 49, 50 }));
    }

    [Test]
    public void ReadRegion_ReportsRegionOutsideImage()
    {
        TiffImageInfo info = CreateInfo(5, 2, 1, new int[] { 8 }, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(48);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 10 };

        TiffRegionReadResult result = TiffStripRegionReader.ReadRegion(info, offsets, byteCounts, fileBytes, 4, 0, 2, 1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
    }

    [Test]
    public void ReadRegion_RejectsSubBytePixels()
    {
        TiffImageInfo info = CreateInfo(8, 1, 1, new int[] { 1 }, 1, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(32);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 1 };

        TiffRegionReadResult result = TiffStripRegionReader.ReadRegion(info, offsets, byteCounts, fileBytes, 0, 0, 8, 1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void ReadRegion_RespectsAllocationLimit()
    {
        TiffImageInfo info = CreateInfo(5, 4, 1, new int[] { 8 }, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(64);
        ulong[] offsets = new ulong[] { 20, 30 };
        ulong[] byteCounts = new ulong[] { 10, 10 };
        TiffLimits limits = new TiffLimits();
        limits.MaximumAllocationBytes = 5;

        TiffRegionReadResult result = TiffStripRegionReader.ReadRegion(info, offsets, byteCounts, fileBytes, 1, 1, 3, 2, 0, limits);

        Assert.That(result.Success, Is.False);
        Assert.That(result.TotalByteCount, Is.EqualTo(6));
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.AllocationLimitExceeded), Is.True);
    }

    [Test]
    public void ReadRegion_PropagatesUnsupportedCompression()
    {
        TiffImageInfo info = CreateInfo(5, 2, 1, new int[] { 8 }, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.Jpeg);
        byte[] fileBytes = CreateFileBytes(48);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 3 };

        TiffRegionReadResult result = TiffStripRegionReader.ReadRegion(info, offsets, byteCounts, fileBytes, 0, 0, 2, 1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedCompression), Is.True);
    }

    static TiffImageInfo CreateInfo(ulong width, ulong length, int samplesPerPixel, int[] bitsPerSample, ulong rowsPerStrip, TiffPlanarConfiguration planarConfiguration, TiffCompression compression)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.SamplesPerPixel = samplesPerPixel;
        info.BitsPerSample = bitsPerSample;
        info.RowsPerStrip = rowsPerStrip;
        info.PlanarConfiguration = planarConfiguration;
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
