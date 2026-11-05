using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffRawTileWriterTests
{
    [Test]
    public void WriteTile_WritesRequestedRawBytes()
    {
        TiffImageInfo info = CreateTileInfo(25, 12, 10, 5);
        byte[] destination = new byte[128];
        ulong[] offsets = new ulong[] { 20, 30, 40, 50, 0, 60, 70, 80, 90 };
        ulong[] byteCounts = new ulong[] { 4, 3, 2, 1, 0, 3, 2, 1, 5 };

        TiffRawTileWriteResult result = TiffRawTileWriter.WriteTile(info, offsets, byteCounts, destination, 2, new byte[] { 9, 8 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.TileIndex, Is.EqualTo(2));
        Assert.That(result.BytesWritten, Is.EqualTo(2));
        Assert.That(result.Range.Offset, Is.EqualTo(40));
        Assert.That(destination[39], Is.EqualTo(0));
        Assert.That(destination[40], Is.EqualTo(9));
        Assert.That(destination[41], Is.EqualTo(8));
        Assert.That(destination[42], Is.EqualTo(0));
    }

    [Test]
    public void WriteTile_RejectsMismatchedSourceByteCount()
    {
        TiffImageInfo info = CreateTileInfo(25, 12, 10, 5);
        byte[] destination = new byte[128];
        ulong[] offsets = new ulong[] { 20, 30, 40, 50, 0, 60, 70, 80, 90 };
        ulong[] byteCounts = new ulong[] { 4, 3, 2, 1, 0, 3, 2, 1, 5 };

        TiffRawTileWriteResult result = TiffRawTileWriter.WriteTile(info, offsets, byteCounts, destination, 2, new byte[] { 9 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(destination[40], Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void WriteTile_ReportsOutOfRangeTileIndex()
    {
        TiffImageInfo info = CreateTileInfo(25, 12, 10, 5);
        byte[] destination = new byte[128];
        ulong[] offsets = new ulong[] { 20, 30, 40, 50, 0, 60, 70, 80, 90 };
        ulong[] byteCounts = new ulong[] { 4, 3, 2, 1, 0, 3, 2, 1, 5 };

        TiffRawTileWriteResult result = TiffRawTileWriter.WriteTile(info, offsets, byteCounts, destination, 9, new byte[] { 1 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
    }

    [Test]
    public void WriteTile_ReportsDestinationRangeOutsideBuffer()
    {
        TiffImageInfo info = CreateTileInfo(10, 10, 10, 10);
        byte[] destination = new byte[50];
        ulong[] offsets = new ulong[] { 48 };
        ulong[] byteCounts = new ulong[] { 4 };

        TiffRawTileWriteResult result = TiffRawTileWriter.WriteTile(info, offsets, byteCounts, destination, 0, new byte[] { 1, 2, 3, 4 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void WriteTile_AllowsEmptySparseWrite()
    {
        TiffImageInfo info = CreateTileInfo(25, 12, 10, 5);
        byte[] destination = new byte[128];
        ulong[] offsets = new ulong[] { 20, 30, 40, 50, 0, 60, 70, 80, 90 };
        ulong[] byteCounts = new ulong[] { 4, 3, 2, 1, 0, 3, 2, 1, 5 };

        TiffRawTileWriteResult result = TiffRawTileWriter.WriteTile(info, offsets, byteCounts, destination, 4, new byte[0]);

        Assert.That(result.Success, Is.True);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(result.Range.IsSparse, Is.True);
    }

    [Test]
    public void WriteTile_RejectsNonEmptySparseWrite()
    {
        TiffImageInfo info = CreateTileInfo(25, 12, 10, 5);
        byte[] destination = new byte[128];
        ulong[] offsets = new ulong[] { 20, 30, 40, 50, 0, 60, 70, 80, 90 };
        ulong[] byteCounts = new ulong[] { 4, 3, 2, 1, 0, 3, 2, 1, 5 };

        TiffRawTileWriteResult result = TiffRawTileWriter.WriteTile(info, offsets, byteCounts, destination, 4, new byte[] { 1 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void WriteTile_FromDirectoryLoadsTileOffsetAndByteCountTags()
    {
        TiffByteOrder byteOrder = TiffByteOrder.LittleEndian;
        byte[] destination = CreateFileBytes(32);
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

        TiffRawTileWriteResult result = TiffRawTileWriter.WriteTile(directory, destination, byteOrder, 1, new byte[] { 88, 89 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.BytesWritten, Is.EqualTo(2));
        Assert.That(result.Range.Offset, Is.EqualTo(24));
        Assert.That(destination[24], Is.EqualTo(88));
        Assert.That(destination[25], Is.EqualTo(89));
        Assert.That(destination[26], Is.EqualTo(26));
    }

    [Test]
    public void WriteTile_FromDirectoryReportsMissingTileOffsets()
    {
        TiffByteOrder byteOrder = TiffByteOrder.LittleEndian;
        byte[] destination = CreateFileBytes(32);
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateShortEntry(TiffTagId.ImageWidth, 2, byteOrder),
            CreateShortEntry(TiffTagId.ImageLength, 1, byteOrder),
            CreateShortEntry(TiffTagId.PhotometricInterpretation, (ushort)TiffPhotometric.BlackIsZero, byteOrder),
            CreateShortEntry(TiffTagId.TileWidth, 1, byteOrder),
            CreateShortEntry(TiffTagId.TileLength, 1, byteOrder),
            CreateShortArrayEntry(TiffTagId.TileByteCounts, new ushort[] { 3, 2 }, byteOrder)
        };
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, entries);

        TiffRawTileWriteResult result = TiffRawTileWriter.WriteTile(directory, destination, byteOrder, 0, new byte[] { 1, 2, 3 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.MissingRequiredTag), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.TileOffsets));
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
