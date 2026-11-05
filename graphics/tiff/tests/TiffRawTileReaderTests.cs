using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffRawTileReaderTests
{
    [Test]
    public void ReadTile_ReadsRequestedRawBytes()
    {
        TiffImageInfo info = CreateTileInfo(25, 12, 10, 5);
        byte[] fileBytes = CreateFileBytes(128);
        ulong[] offsets = new ulong[] { 20, 30, 40, 50, 0, 60, 70, 80, 90 };
        ulong[] byteCounts = new ulong[] { 4, 3, 2, 1, 0, 3, 2, 1, 5 };

        TiffRawTileReadResult result = TiffRawTileReader.ReadTile(info, offsets, byteCounts, fileBytes, 2);

        Assert.That(result.Success, Is.True);
        Assert.That(result.TileIndex, Is.EqualTo(2));
        Assert.That(result.Range.Offset, Is.EqualTo(40));
        Assert.That(result.Range.ByteCount, Is.EqualTo(2));
        Assert.That(result.Range.EndOffset, Is.EqualTo(42));
        Assert.That(result.IsSparse, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 40, 41 }));
    }

    [Test]
    public void ReadTile_ReturnsEmptyBytesForSparseTile()
    {
        TiffImageInfo info = CreateTileInfo(25, 12, 10, 5);
        byte[] fileBytes = CreateFileBytes(128);
        ulong[] offsets = new ulong[] { 20, 30, 40, 50, 0, 60, 70, 80, 90 };
        ulong[] byteCounts = new ulong[] { 4, 3, 2, 1, 0, 3, 2, 1, 5 };

        TiffRawTileReadResult result = TiffRawTileReader.ReadTile(info, offsets, byteCounts, fileBytes, 4);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IsSparse, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
    }

    [Test]
    public void ReadTile_ReportsOutOfRangeTileIndex()
    {
        TiffImageInfo info = CreateTileInfo(25, 12, 10, 5);
        byte[] fileBytes = CreateFileBytes(128);
        ulong[] offsets = new ulong[] { 20, 30, 40, 50, 0, 60, 70, 80, 90 };
        ulong[] byteCounts = new ulong[] { 4, 3, 2, 1, 0, 3, 2, 1, 5 };

        TiffRawTileReadResult result = TiffRawTileReader.ReadTile(info, offsets, byteCounts, fileBytes, 9);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
    }

    [Test]
    public void ReadTile_ReportsByteRangeOutsideFile()
    {
        TiffImageInfo info = CreateTileInfo(10, 10, 10, 10);
        byte[] fileBytes = CreateFileBytes(50);
        ulong[] offsets = new ulong[] { 48 };
        ulong[] byteCounts = new ulong[] { 4 };

        TiffRawTileReadResult result = TiffRawTileReader.ReadTile(info, offsets, byteCounts, fileBytes, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void ReadTile_RespectsAllocationLimit()
    {
        TiffImageInfo info = CreateTileInfo(10, 10, 10, 10);
        byte[] fileBytes = CreateFileBytes(64);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 6 };
        TiffLimits limits = new TiffLimits();
        limits.MaximumAllocationBytes = 5;

        TiffRawTileReadResult result = TiffRawTileReader.ReadTile(info, offsets, byteCounts, fileBytes, 0, limits);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.AllocationLimitExceeded), Is.True);
    }

    [Test]
    public void ReadTile_FromDirectoryLoadsTileOffsetAndByteCountTags()
    {
        TiffByteOrder byteOrder = TiffByteOrder.LittleEndian;
        byte[] fileBytes = CreateFileBytes(32);
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateShortEntry(TiffTagId.ImageWidth, 2, byteOrder),
            CreateShortEntry(TiffTagId.ImageLength, 1, byteOrder),
            CreateShortEntry(TiffTagId.PhotometricInterpretation, (ushort)TiffPhotometric.BlackIsZero, byteOrder),
            CreateShortEntry(TiffTagId.TileWidth, 1, byteOrder),
            CreateShortEntry(TiffTagId.TileLength, 1, byteOrder),
            CreateShortArrayEntry(TiffTagId.TileOffsets, new ushort[] { 20, 24 }, byteOrder),
            CreateShortArrayEntry(TiffTagId.TileByteCounts, new ushort[] { 3, 2 }, byteOrder)
        };
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, entries);

        TiffRawTileReadResult result = TiffRawTileReader.ReadTile(directory, fileBytes, byteOrder, 1);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Range.Offset, Is.EqualTo(24));
        Assert.That(result.Range.ByteCount, Is.EqualTo(2));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 24, 25 }));
    }

    [Test]
    public void ReadTile_FromDirectoryReportsMissingTileByteCounts()
    {
        TiffByteOrder byteOrder = TiffByteOrder.LittleEndian;
        byte[] fileBytes = CreateFileBytes(32);
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateShortEntry(TiffTagId.ImageWidth, 2, byteOrder),
            CreateShortEntry(TiffTagId.ImageLength, 1, byteOrder),
            CreateShortEntry(TiffTagId.PhotometricInterpretation, (ushort)TiffPhotometric.BlackIsZero, byteOrder),
            CreateShortEntry(TiffTagId.TileWidth, 1, byteOrder),
            CreateShortEntry(TiffTagId.TileLength, 1, byteOrder),
            CreateShortArrayEntry(TiffTagId.TileOffsets, new ushort[] { 20, 24 }, byteOrder)
        };
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, entries);

        TiffRawTileReadResult result = TiffRawTileReader.ReadTile(directory, fileBytes, byteOrder, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.MissingRequiredTag), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.TileByteCounts));
    }

    static TiffImageInfo CreateTileInfo(ulong width, ulong length, ulong tileWidth, ulong tileLength)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.IsTiled = true;
        info.TileWidth = tileWidth;
        info.TileLength = tileLength;
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
