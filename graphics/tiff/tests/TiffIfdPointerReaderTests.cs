using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffIfdPointerReaderTests
{
    [Test]
    public void Read_ParsesClassicExifGpsAndNestedInteroperabilityPointers()
    {
        byte[] bytes = new byte[160];
        WriteClassicParentWithTwoPointers(bytes, 8, 48, 80);
        WriteClassicParentWithOnePointer(bytes, 48, TiffTagId.InteroperabilityIfd, 112);
        WriteClassicDirectory(bytes, 80, TiffTagId.ImageWidth, 0);
        WriteClassicDirectory(bytes, 112, TiffTagId.ImageLength, 0);
        TiffDirectory parent = TiffDirectoryReader.ReadClassic(bytes, TiffByteOrder.LittleEndian, 8).Directories[0];

        TiffIfdPointerReadResult result = TiffIfdPointerReader.Read(parent, bytes, TiffByteOrder.LittleEndian, TiffFileKind.Classic);

        Assert.That(result.Success, Is.True);
        Assert.That(result.PointerCount(), Is.EqualTo(3));
        Assert.That(result.Pointers[0].TagId, Is.EqualTo(TiffTagId.ExifIfd));
        Assert.That(result.Pointers[0].Offset, Is.EqualTo(48));
        Assert.That(result.Pointers[0].Depth, Is.EqualTo(1));
        Assert.That(result.Pointers[1].TagId, Is.EqualTo(TiffTagId.InteroperabilityIfd));
        Assert.That(result.Pointers[1].Offset, Is.EqualTo(112));
        Assert.That(result.Pointers[1].Depth, Is.EqualTo(2));
        Assert.That(result.Pointers[1].Directories[0].Entries[0].TagId, Is.EqualTo(TiffTagId.ImageLength));
        Assert.That(result.Pointers[2].TagId, Is.EqualTo(TiffTagId.GpsIfd));
        Assert.That(result.Pointers[2].Offset, Is.EqualTo(80));
    }

    [Test]
    public void Read_ParsesBigTiffLong8ExifPointer()
    {
        byte[] bytes = new byte[128];
        WriteBigTiffParentWithOnePointer(bytes, 16, TiffTagId.ExifIfd, 64);
        WriteBigTiffDirectory(bytes, 64, TiffTagId.ImageWidth, 0);
        TiffDirectory parent = TiffDirectoryReader.ReadBigTiff(bytes, TiffByteOrder.LittleEndian, 16).Directories[0];

        TiffIfdPointerReadResult result = TiffIfdPointerReader.Read(parent, bytes, TiffByteOrder.LittleEndian, TiffFileKind.BigTiff);

        Assert.That(result.Success, Is.True);
        Assert.That(result.PointerCount(), Is.EqualTo(1));
        Assert.That(result.Pointers[0].TagId, Is.EqualTo(TiffTagId.ExifIfd));
        Assert.That(result.Pointers[0].Offset, Is.EqualTo(64));
        Assert.That(result.Pointers[0].Directories[0].Entries[0].TagId, Is.EqualTo(TiffTagId.ImageWidth));
    }

    [Test]
    public void Read_ReturnsEmptyResultWhenNoPointerTagsExist()
    {
        byte[] bytes = new byte[64];
        WriteClassicDirectory(bytes, 8, TiffTagId.ImageWidth, 0);
        TiffDirectory parent = TiffDirectoryReader.ReadClassic(bytes, TiffByteOrder.LittleEndian, 8).Directories[0];

        TiffIfdPointerReadResult result = TiffIfdPointerReader.Read(parent, bytes, TiffByteOrder.LittleEndian, TiffFileKind.Classic);

        Assert.That(result.Success, Is.True);
        Assert.That(result.PointerCount(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.Count(), Is.EqualTo(0));
    }

    [Test]
    public void Read_ReportsInvalidPointerTagType()
    {
        TiffDirectory parent = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreatePointerEntry(TiffTagId.ExifIfd, TiffDataType.Short, 1, 32, 4) });

        TiffIfdPointerReadResult result = TiffIfdPointerReader.Read(parent, new byte[64], TiffByteOrder.LittleEndian, TiffFileKind.Classic);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagType), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.ExifIfd));
    }

    [Test]
    public void Read_ReportsPointerTagCountOtherThanOne()
    {
        TiffDirectory parent = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreatePointerEntry(TiffTagId.GpsIfd, TiffDataType.Long, 2, 32, 4) });

        TiffIfdPointerReadResult result = TiffIfdPointerReader.Read(parent, new byte[64], TiffByteOrder.LittleEndian, TiffFileKind.Classic);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagCount), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.GpsIfd));
    }

    [Test]
    public void Read_ReportsPointerCycleToAncestorOffset()
    {
        byte[] bytes = new byte[64];
        WriteClassicParentWithOnePointer(bytes, 8, TiffTagId.ExifIfd, 8);
        TiffDirectory parent = TiffDirectoryReader.ReadClassic(bytes, TiffByteOrder.LittleEndian, 8).Directories[0];

        TiffIfdPointerReadResult result = TiffIfdPointerReader.Read(parent, bytes, TiffByteOrder.LittleEndian, TiffFileKind.Classic);

        Assert.That(result.Success, Is.False);
        Assert.That(result.PointerCount(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.CyclicIfdChain), Is.True);
    }

    [Test]
    public void Read_ReportsDepthLimitForNestedPointer()
    {
        byte[] bytes = new byte[128];
        WriteClassicParentWithOnePointer(bytes, 8, TiffTagId.ExifIfd, 48);
        WriteClassicParentWithOnePointer(bytes, 48, TiffTagId.InteroperabilityIfd, 80);
        WriteClassicDirectory(bytes, 80, TiffTagId.ImageWidth, 0);
        TiffDirectory parent = TiffDirectoryReader.ReadClassic(bytes, TiffByteOrder.LittleEndian, 8).Directories[0];
        TiffReadOptions options = new TiffReadOptions();
        options.Limits.MaximumSubIfdDepth = 1;

        TiffIfdPointerReadResult result = TiffIfdPointerReader.Read(parent, bytes, TiffByteOrder.LittleEndian, TiffFileKind.Classic, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.PointerCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.IfdCountLimitExceeded), Is.True);
    }

    [Test]
    public void Helpers_ExposePointerTagIdsAndCopyResults()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 32, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth) });
        TiffIfdPointer pointer = TiffIfdPointer.Create(0, 8, TiffTagId.ExifIfd, 1, 32, new TiffDirectory[] { directory });
        TiffIfdPointerReadResult result = TiffIfdPointerReadResult.Create(new TiffIfdPointer[] { pointer }, new TiffDiagnostics());
        directory.Entries[0].TagId = TiffTagId.ImageLength;

        Assert.That(TiffIfdPointerReader.GetPointerTagIds(), Is.EqualTo(new int[] { TiffTagId.ExifIfd, TiffTagId.GpsIfd, TiffTagId.InteroperabilityIfd }));
        Assert.That(TiffIfdPointerReader.IsPointerTagId(TiffTagId.ExifIfd), Is.True);
        Assert.That(TiffIfdPointerReader.IsPointerTagId(TiffTagId.SubIFDs), Is.False);
        Assert.That(result.Pointers[0].Directories[0].Entries[0].TagId, Is.EqualTo(TiffTagId.ImageWidth));
    }

    static TiffIfdEntry CreatePointerEntry(int tagId, TiffDataType dataType, long count, ulong valueOffset, int slotByteCount)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = dataType;
        entry.Count = count;
        entry.RawCount = (ulong)count;
        entry.ValueOffset = valueOffset;
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(dataType, count);
        entry.ValueSlotByteCount = slotByteCount;
        entry.ValueOffsetBytes = new byte[slotByteCount];
        return entry;
    }

    static TiffIfdEntry CreateEntry(int tagId)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = TiffDataType.Long;
        entry.Count = 1;
        entry.RawCount = 1;
        entry.ValueByteCount = 4;
        entry.ValueSlotByteCount = 4;
        return entry;
    }

    static void WriteClassicParentWithOnePointer(byte[] bytes, int offset, int pointerTagId, uint pointerOffset)
    {
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), 1, TiffByteOrder.LittleEndian);
        WriteClassicPointerEntry(bytes, offset + 2, pointerTagId, pointerOffset);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 14, 4), 0, TiffByteOrder.LittleEndian);
    }

    static void WriteClassicParentWithTwoPointers(byte[] bytes, int offset, uint exifOffset, uint gpsOffset)
    {
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), 2, TiffByteOrder.LittleEndian);
        WriteClassicPointerEntry(bytes, offset + 2, TiffTagId.ExifIfd, exifOffset);
        WriteClassicPointerEntry(bytes, offset + 14, TiffTagId.GpsIfd, gpsOffset);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 26, 4), 0, TiffByteOrder.LittleEndian);
    }

    static void WriteClassicPointerEntry(byte[] bytes, int offset, int tagId, uint pointerOffset)
    {
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), (ushort)tagId, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 2, 2), (ushort)TiffDataType.Long, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 4, 4), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 8, 4), pointerOffset, TiffByteOrder.LittleEndian);
    }

    static void WriteClassicDirectory(byte[] bytes, int offset, int tagId, uint nextOffset)
    {
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 2, 2), (ushort)tagId, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 4, 2), (ushort)TiffDataType.Long, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 6, 4), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 10, 4), 100, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 14, 4), nextOffset, TiffByteOrder.LittleEndian);
    }

    static void WriteBigTiffParentWithOnePointer(byte[] bytes, int offset, int pointerTagId, ulong pointerOffset)
    {
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(offset, 8), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 8, 2), (ushort)pointerTagId, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 10, 2), (ushort)TiffDataType.Long8, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(offset + 12, 8), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(offset + 20, 8), pointerOffset, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(offset + 28, 8), 0, TiffByteOrder.LittleEndian);
    }

    static void WriteBigTiffDirectory(byte[] bytes, int offset, int tagId, ulong nextOffset)
    {
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(offset, 8), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 8, 2), (ushort)tagId, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 10, 2), (ushort)TiffDataType.Long, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(offset + 12, 8), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(offset + 20, 8), 100, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(offset + 28, 8), nextOffset, TiffByteOrder.LittleEndian);
    }
}
