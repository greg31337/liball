using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffTileRegionReaderTests
{
    [Test]
    public void ReadRegion_ReadsContiguousWindowAcrossTiles()
    {
        TiffImageInfo info = CreateTileInfo(5, 4, 1, new int[] { 8 }, 3, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(80);
        ulong[] offsets = new ulong[] { 20, 26, 32, 38 };
        ulong[] byteCounts = new ulong[] { 6, 6, 6, 6 };

        TiffRegionReadResult result = TiffTileRegionReader.ReadRegion(info, offsets, byteCounts, fileBytes, 2, 1, 3, 2);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Column, Is.EqualTo(2));
        Assert.That(result.Row, Is.EqualTo(1));
        Assert.That(result.Width, Is.EqualTo(3));
        Assert.That(result.Length, Is.EqualTo(2));
        Assert.That(result.RowByteCount, Is.EqualTo(3));
        Assert.That(result.TotalByteCount, Is.EqualTo(6));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 25, 29, 30, 34, 38, 39 }));
    }

    [Test]
    public void ReadRegion_FromDirectoryUsesDecodedTileDispatcher()
    {
        TiffByteOrder byteOrder = TiffByteOrder.LittleEndian;
        byte[] fileBytes = CreateFileBytes(48);
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateShortEntry(TiffTagId.ImageWidth, 4, byteOrder),
            CreateShortEntry(TiffTagId.ImageLength, 2, byteOrder),
            CreateShortEntry(TiffTagId.BitsPerSample, 8, byteOrder),
            CreateShortEntry(TiffTagId.PhotometricInterpretation, (ushort)TiffPhotometric.BlackIsZero, byteOrder),
            CreateShortEntry(TiffTagId.TileWidth, 2, byteOrder),
            CreateShortEntry(TiffTagId.TileLength, 2, byteOrder),
            CreateShortArrayEntry(TiffTagId.TileOffsets, new ushort[] { 20, 24 }, byteOrder),
            CreateShortArrayEntry(TiffTagId.TileByteCounts, new ushort[] { 4, 4 }, byteOrder)
        };
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, entries);

        TiffRegionReadResult result = TiffTileRegionReader.ReadRegion(directory, fileBytes, byteOrder, 1, 0, 3, 2);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RowByteCount, Is.EqualTo(3));
        Assert.That(result.TotalByteCount, Is.EqualTo(6));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 21, 24, 25, 23, 26, 27 }));
    }

    [Test]
    public void ReadRegion_MapsSeparateSamplePlaneTiles()
    {
        TiffImageInfo info = CreateTileInfo(4, 4, 2, new int[] { 8, 8 }, 2, 2, TiffPlanarConfiguration.Separate, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(80);
        ulong[] offsets = new ulong[] { 20, 24, 28, 32, 36, 40, 44, 48 };
        ulong[] byteCounts = new ulong[] { 4, 4, 4, 4, 4, 4, 4, 4 };

        TiffRegionReadResult result = TiffTileRegionReader.ReadRegion(info, offsets, byteCounts, fileBytes, 1, 3, 1, 1, 1);

        Assert.That(result.Success, Is.True);
        Assert.That(result.SampleIndex, Is.EqualTo(1));
        Assert.That(result.RowByteCount, Is.EqualTo(1));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 47 }));
    }

    [Test]
    public void ReadRegion_ReportsRegionOutsideImage()
    {
        TiffImageInfo info = CreateTileInfo(5, 4, 1, new int[] { 8 }, 3, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(80);
        ulong[] offsets = new ulong[] { 20, 26, 32, 38 };
        ulong[] byteCounts = new ulong[] { 6, 6, 6, 6 };

        TiffRegionReadResult result = TiffTileRegionReader.ReadRegion(info, offsets, byteCounts, fileBytes, 4, 0, 2, 1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
    }

    [Test]
    public void ReadRegion_RejectsSubBytePixels()
    {
        TiffImageInfo info = CreateTileInfo(8, 1, 1, new int[] { 1 }, 8, 1, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(32);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 1 };

        TiffRegionReadResult result = TiffTileRegionReader.ReadRegion(info, offsets, byteCounts, fileBytes, 0, 0, 8, 1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void ReadRegion_RespectsAllocationLimit()
    {
        TiffImageInfo info = CreateTileInfo(5, 4, 1, new int[] { 8 }, 3, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(80);
        ulong[] offsets = new ulong[] { 20, 26, 32, 38 };
        ulong[] byteCounts = new ulong[] { 6, 6, 6, 6 };
        TiffLimits limits = new TiffLimits();
        limits.MaximumAllocationBytes = 5;

        TiffRegionReadResult result = TiffTileRegionReader.ReadRegion(info, offsets, byteCounts, fileBytes, 2, 1, 3, 2, 0, limits);

        Assert.That(result.Success, Is.False);
        Assert.That(result.TotalByteCount, Is.EqualTo(6));
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.AllocationLimitExceeded), Is.True);
    }

    [Test]
    public void ReadRegion_PropagatesUnsupportedCompression()
    {
        TiffImageInfo info = CreateTileInfo(3, 2, 1, new int[] { 8 }, 3, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.Jpeg);
        byte[] fileBytes = CreateFileBytes(48);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 3 };

        TiffRegionReadResult result = TiffTileRegionReader.ReadRegion(info, offsets, byteCounts, fileBytes, 0, 0, 2, 1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedCompression), Is.True);
    }

    [Test]
    public void ReadRegion_RejectsStrippedImageInfo()
    {
        TiffImageInfo info = CreateTileInfo(3, 2, 1, new int[] { 8 }, 3, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        info.IsTiled = false;
        byte[] fileBytes = CreateFileBytes(48);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 6 };

        TiffRegionReadResult result = TiffTileRegionReader.ReadRegion(info, offsets, byteCounts, fileBytes, 0, 0, 2, 1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    static TiffImageInfo CreateTileInfo(ulong width, ulong length, int samplesPerPixel, int[] bitsPerSample, ulong tileWidth, ulong tileLength, TiffPlanarConfiguration planarConfiguration, TiffCompression compression)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.SamplesPerPixel = samplesPerPixel;
        info.BitsPerSample = bitsPerSample;
        info.IsTiled = true;
        info.TileWidth = tileWidth;
        info.TileLength = tileLength;
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
