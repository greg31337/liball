using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffSubIfdReaderTests
{
    [Test]
    public void Read_ParsesClassicInlineSubIfdOffset()
    {
        byte[] bytes = new byte[96];
        WriteClassicParentWithOneInlineSubIfd(bytes, 8, 32);
        WriteClassicDirectory(bytes, 32, TiffTagId.ImageWidth, 0);
        TiffDirectory parent = TiffDirectoryReader.ReadClassic(bytes, TiffByteOrder.LittleEndian, 8).Directories[0];

        TiffSubIfdReadResult result = TiffSubIfdReader.Read(parent, bytes, TiffByteOrder.LittleEndian, TiffFileKind.Classic);

        Assert.That(result.Success, Is.True);
        Assert.That(result.SubIfdCount(), Is.EqualTo(1));
        Assert.That(result.SubIfds[0].ParentOffset, Is.EqualTo(8));
        Assert.That(result.SubIfds[0].SubIfdIndex, Is.EqualTo(0));
        Assert.That(result.SubIfds[0].Depth, Is.EqualTo(1));
        Assert.That(result.SubIfds[0].Offset, Is.EqualTo(32));
        Assert.That(result.SubIfds[0].Directories.Length, Is.EqualTo(1));
        Assert.That(result.SubIfds[0].Directories[0].Entries[0].TagId, Is.EqualTo(TiffTagId.ImageWidth));
    }

    [Test]
    public void Read_ParsesClassicExternalSubIfdOffsetArray()
    {
        byte[] bytes = new byte[128];
        WriteClassicParentWithExternalSubIfds(bytes, 8, 96);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(96, 4), 32, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(100, 4), 64, TiffByteOrder.LittleEndian);
        WriteClassicDirectory(bytes, 32, TiffTagId.ImageWidth, 0);
        WriteClassicDirectory(bytes, 64, TiffTagId.ImageLength, 0);
        TiffDirectory parent = TiffDirectoryReader.ReadClassic(bytes, TiffByteOrder.LittleEndian, 8).Directories[0];

        TiffSubIfdReadResult result = TiffSubIfdReader.Read(parent, bytes, TiffByteOrder.LittleEndian, TiffFileKind.Classic);

        Assert.That(result.Success, Is.True);
        Assert.That(result.SubIfdCount(), Is.EqualTo(2));
        Assert.That(result.SubIfds[0].Offset, Is.EqualTo(32));
        Assert.That(result.SubIfds[1].Offset, Is.EqualTo(64));
        Assert.That(result.SubIfds[1].Directories[0].Entries[0].TagId, Is.EqualTo(TiffTagId.ImageLength));
    }

    [Test]
    public void Read_ParsesBigTiffInlineLong8SubIfdOffset()
    {
        byte[] bytes = new byte[128];
        WriteBigTiffParentWithOneInlineSubIfd(bytes, 16, 64);
        WriteBigTiffDirectory(bytes, 64, TiffTagId.ImageWidth, 0);
        TiffDirectory parent = TiffDirectoryReader.ReadBigTiff(bytes, TiffByteOrder.LittleEndian, 16).Directories[0];

        TiffSubIfdReadResult result = TiffSubIfdReader.Read(parent, bytes, TiffByteOrder.LittleEndian, TiffFileKind.BigTiff);

        Assert.That(result.Success, Is.True);
        Assert.That(result.SubIfdCount(), Is.EqualTo(1));
        Assert.That(result.SubIfds[0].Offset, Is.EqualTo(64));
        Assert.That(result.SubIfds[0].Directories[0].Entries[0].TagId, Is.EqualTo(TiffTagId.ImageWidth));
    }

    [Test]
    public void Read_ReturnsEmptyResultWhenParentHasNoSubIfdTag()
    {
        byte[] bytes = new byte[64];
        WriteClassicDirectory(bytes, 8, TiffTagId.ImageWidth, 0);
        TiffDirectory parent = TiffDirectoryReader.ReadClassic(bytes, TiffByteOrder.LittleEndian, 8).Directories[0];

        TiffSubIfdReadResult result = TiffSubIfdReader.Read(parent, bytes, TiffByteOrder.LittleEndian, TiffFileKind.Classic);

        Assert.That(result.Success, Is.True);
        Assert.That(result.SubIfdCount(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.Count(), Is.EqualTo(0));
    }

    [Test]
    public void Read_ReportsInvalidSubIfdTagType()
    {
        TiffDirectory parent = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateSubIfdEntry(TiffDataType.Short, 1, 32, 4) });

        TiffSubIfdReadResult result = TiffSubIfdReader.Read(parent, new byte[64], TiffByteOrder.LittleEndian, TiffFileKind.Classic);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagType), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.SubIFDs));
    }

    [Test]
    public void Read_ReportsSubIfdDepthLimit()
    {
        byte[] bytes = new byte[160];
        WriteClassicParentWithOneInlineSubIfd(bytes, 8, 32);
        WriteClassicParentWithOneInlineSubIfd(bytes, 32, 64);
        WriteClassicDirectory(bytes, 64, TiffTagId.ImageWidth, 0);
        TiffDirectory parent = TiffDirectoryReader.ReadClassic(bytes, TiffByteOrder.LittleEndian, 8).Directories[0];
        TiffReadOptions options = new TiffReadOptions();
        options.Limits.MaximumSubIfdDepth = 1;

        TiffSubIfdReadResult result = TiffSubIfdReader.Read(parent, bytes, TiffByteOrder.LittleEndian, TiffFileKind.Classic, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.SubIfdCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.IfdCountLimitExceeded), Is.True);
    }

    [Test]
    public void Read_ReportsSubIfdCycleToAncestorOffset()
    {
        byte[] bytes = new byte[64];
        WriteClassicParentWithOneInlineSubIfd(bytes, 8, 8);
        TiffDirectory parent = TiffDirectoryReader.ReadClassic(bytes, TiffByteOrder.LittleEndian, 8).Directories[0];

        TiffSubIfdReadResult result = TiffSubIfdReader.Read(parent, bytes, TiffByteOrder.LittleEndian, TiffFileKind.Classic);

        Assert.That(result.Success, Is.False);
        Assert.That(result.SubIfdCount(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.CyclicIfdChain), Is.True);
    }

    [Test]
    public void DecodeOffsets_ReadsLongAndLong8Values()
    {
        byte[] longBytes = new byte[4];
        byte[] long8Bytes = new byte[8];
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffBinaryWriter.WriteUInt32(longBytes.AsSpan(0, 4), 32, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt64(long8Bytes.AsSpan(0, 8), 64, TiffByteOrder.LittleEndian);

        ulong[] longOffsets = TiffSubIfdReader.DecodeOffsets(longBytes, TiffDataType.Ifd, 1, TiffByteOrder.LittleEndian, diagnostics);
        ulong[] long8Offsets = TiffSubIfdReader.DecodeOffsets(long8Bytes, TiffDataType.Ifd8, 1, TiffByteOrder.LittleEndian, diagnostics);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(longOffsets, Is.EqualTo(new ulong[] { 32 }));
        Assert.That(long8Offsets, Is.EqualTo(new ulong[] { 64 }));
        Assert.That(TiffSubIfdReader.GetOffsetElementSize(TiffDataType.Long), Is.EqualTo(4));
        Assert.That(TiffSubIfdReader.GetOffsetElementSize(TiffDataType.Long8), Is.EqualTo(8));
        Assert.That(TiffSubIfdReader.IsSupportedOffsetDataType(TiffDataType.Ascii), Is.False);
    }

    [Test]
    public void ResultCopiesMutableSubIfdStorage()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 32, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth) });
        TiffSubIfd subIfd = TiffSubIfd.Create(0, 8, 0, 1, 32, new TiffDirectory[] { directory });
        TiffSubIfdReadResult result = TiffSubIfdReadResult.Create(new TiffSubIfd[] { subIfd }, new TiffDiagnostics());
        directory.Entries[0].TagId = TiffTagId.ImageLength;

        Assert.That(result.SubIfds[0].Directories[0].Entries[0].TagId, Is.EqualTo(TiffTagId.ImageWidth));
    }

    static TiffIfdEntry CreateSubIfdEntry(TiffDataType dataType, long count, ulong valueOffset, int slotByteCount)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = TiffTagId.SubIFDs;
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

    static void WriteClassicParentWithOneInlineSubIfd(byte[] bytes, int offset, uint subIfdOffset)
    {
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 2, 2), (ushort)TiffTagId.SubIFDs, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 4, 2), (ushort)TiffDataType.Long, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 6, 4), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 10, 4), subIfdOffset, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 14, 4), 0, TiffByteOrder.LittleEndian);
    }

    static void WriteClassicParentWithExternalSubIfds(byte[] bytes, int offset, uint valueOffset)
    {
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 2, 2), (ushort)TiffTagId.SubIFDs, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 4, 2), (ushort)TiffDataType.Long, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 6, 4), 2, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 10, 4), valueOffset, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 14, 4), 0, TiffByteOrder.LittleEndian);
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

    static void WriteBigTiffParentWithOneInlineSubIfd(byte[] bytes, int offset, ulong subIfdOffset)
    {
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(offset, 8), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 8, 2), (ushort)TiffTagId.SubIFDs, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 10, 2), (ushort)TiffDataType.Long8, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(offset + 12, 8), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(offset + 20, 8), subIfdOffset, TiffByteOrder.LittleEndian);
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
