using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffDirectoryReaderTests
{
    [Test]
    public void ReadClassic_TraversesNextIfdChain()
    {
        byte[] bytes = new byte[64];
        WriteClassicDirectory(bytes, 8, TiffTagId.ImageWidth, 32);
        WriteClassicDirectory(bytes, 32, TiffTagId.ImageLength, 0);

        TiffDirectoryReadResult result = TiffDirectoryReader.ReadClassic(bytes, TiffByteOrder.LittleEndian, 8);

        Assert.That(result.Success, Is.True);
        Assert.That(result.DirectoryCount(), Is.EqualTo(2));
        Assert.That(result.Directories[0].ImageIndex, Is.EqualTo(0));
        Assert.That(result.Directories[0].Offset, Is.EqualTo(8));
        Assert.That(result.Directories[0].NextIfdOffset, Is.EqualTo(32));
        Assert.That(result.Directories[0].Entries[0].TagId, Is.EqualTo(TiffTagId.ImageWidth));
        Assert.That(result.Directories[1].ImageIndex, Is.EqualTo(1));
        Assert.That(result.Directories[1].Offset, Is.EqualTo(32));
        Assert.That(result.Directories[1].NextIfdOffset, Is.EqualTo(0));
        Assert.That(result.Directories[1].Entries[0].TagId, Is.EqualTo(TiffTagId.ImageLength));
    }

    [Test]
    public void ReadClassic_ZeroFirstIfdOffsetReturnsEmptyDirectoryList()
    {
        TiffDirectoryReadResult result = TiffDirectoryReader.ReadClassic(new byte[8], TiffByteOrder.LittleEndian, 0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.DirectoryCount(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.Count(), Is.EqualTo(0));
    }

    [Test]
    public void ReadClassic_ReportsCycleAndReturnsDirectoriesReadBeforeCycle()
    {
        byte[] bytes = new byte[64];
        WriteClassicDirectory(bytes, 8, TiffTagId.ImageWidth, 32);
        WriteClassicDirectory(bytes, 32, TiffTagId.ImageLength, 8);

        TiffDirectoryReadResult result = TiffDirectoryReader.ReadClassic(bytes, TiffByteOrder.LittleEndian, 8);

        Assert.That(result.Success, Is.False);
        Assert.That(result.DirectoryCount(), Is.EqualTo(2));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.CyclicIfdChain), Is.True);
        Assert.That(result.Diagnostics.Get(0).Offset, Is.EqualTo(8));
    }

    [Test]
    public void ReadClassic_ReportsTruncatedDirectory()
    {
        byte[] bytes = new byte[12];
        bytes[8] = 1;

        TiffDirectoryReadResult result = TiffDirectoryReader.ReadClassic(bytes, TiffByteOrder.LittleEndian, 8);

        Assert.That(result.Success, Is.False);
        Assert.That(result.DirectoryCount(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.TruncatedStream), Is.True);
    }

    [Test]
    public void ReadClassic_ReportsInvalidByteOrder()
    {
        TiffDirectoryReadResult result = TiffDirectoryReader.ReadClassic(new byte[16], TiffByteOrder.Unknown, 8);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteOrder), Is.True);
    }

    [Test]
    public void ReadClassic_StopsBeforeDirectoryCountExceedsConfiguredLimit()
    {
        byte[] bytes = new byte[64];
        WriteClassicDirectory(bytes, 8, TiffTagId.ImageWidth, 32);
        WriteClassicDirectory(bytes, 32, TiffTagId.ImageLength, 0);
        TiffReadOptions options = new TiffReadOptions();
        options.Limits.MaximumIfdCount = 1;

        TiffDirectoryReadResult result = TiffDirectoryReader.ReadClassic(bytes, TiffByteOrder.LittleEndian, 8, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.DirectoryCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.IfdCountLimitExceeded), Is.True);
        Assert.That(result.Diagnostics.Get(0).Offset, Is.EqualTo(32));
    }

    [Test]
    public void ReadClassic_RejectsDirectoryTagCountAboveConfiguredLimitBeforeReadingEntries()
    {
        byte[] bytes = new byte[64];
        WriteClassicDirectoryWithTwoEntries(bytes, 8, 0);
        TiffReadOptions options = new TiffReadOptions();
        options.Limits.MaximumTagCountPerIfd = 1;

        TiffDirectoryReadResult result = TiffDirectoryReader.ReadClassic(bytes, TiffByteOrder.LittleEndian, 8, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.DirectoryCount(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.TagCountLimitExceeded), Is.True);
        Assert.That(result.Diagnostics.Get(0).Offset, Is.EqualTo(8));
    }

    [Test]
    public void ReadBigTiff_TraversesNextIfdChain()
    {
        byte[] bytes = new byte[128];
        WriteBigTiffDirectory(bytes, 16, TiffTagId.ImageWidth, 64);
        WriteBigTiffDirectory(bytes, 64, TiffTagId.ImageLength, 0);

        TiffDirectoryReadResult result = TiffDirectoryReader.ReadBigTiff(bytes, TiffByteOrder.LittleEndian, 16);

        Assert.That(result.Success, Is.True);
        Assert.That(result.DirectoryCount(), Is.EqualTo(2));
        Assert.That(result.Directories[0].Offset, Is.EqualTo(16));
        Assert.That(result.Directories[0].NextIfdOffset, Is.EqualTo(64));
        Assert.That(result.Directories[0].Entries[0].TagId, Is.EqualTo(TiffTagId.ImageWidth));
        Assert.That(result.Directories[1].Offset, Is.EqualTo(64));
        Assert.That(result.Directories[1].NextIfdOffset, Is.EqualTo(0));
        Assert.That(result.Directories[1].Entries[0].TagId, Is.EqualTo(TiffTagId.ImageLength));
    }

    [Test]
    public void ReadBigTiff_ReportsCycle()
    {
        byte[] bytes = new byte[128];
        WriteBigTiffDirectory(bytes, 16, TiffTagId.ImageWidth, 64);
        WriteBigTiffDirectory(bytes, 64, TiffTagId.ImageLength, 16);

        TiffDirectoryReadResult result = TiffDirectoryReader.ReadBigTiff(bytes, TiffByteOrder.LittleEndian, 16);

        Assert.That(result.Success, Is.False);
        Assert.That(result.DirectoryCount(), Is.EqualTo(2));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.CyclicIfdChain), Is.True);
        Assert.That(result.Diagnostics.Get(0).Offset, Is.EqualTo(16));
    }

    [Test]
    public void ReadBigTiff_StopsBeforeDirectoryCountExceedsConfiguredLimit()
    {
        byte[] bytes = new byte[128];
        WriteBigTiffDirectory(bytes, 16, TiffTagId.ImageWidth, 64);
        WriteBigTiffDirectory(bytes, 64, TiffTagId.ImageLength, 0);
        TiffReadOptions options = new TiffReadOptions();
        options.Limits.MaximumIfdCount = 1;

        TiffDirectoryReadResult result = TiffDirectoryReader.ReadBigTiff(bytes, TiffByteOrder.LittleEndian, 16, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.DirectoryCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.IfdCountLimitExceeded), Is.True);
        Assert.That(result.Diagnostics.Get(0).Offset, Is.EqualTo(64));
    }

    [Test]
    public void ReadBigTiff_RejectsDirectoryTagCountAboveConfiguredLimitBeforeReadingEntries()
    {
        byte[] bytes = new byte[128];
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(16, 8), 2, TiffByteOrder.LittleEndian);
        TiffReadOptions options = new TiffReadOptions();
        options.Limits.MaximumTagCountPerIfd = 1;

        TiffDirectoryReadResult result = TiffDirectoryReader.ReadBigTiff(bytes, TiffByteOrder.LittleEndian, 16, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.DirectoryCount(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.TagCountLimitExceeded), Is.True);
        Assert.That(result.Diagnostics.Get(0).Offset, Is.EqualTo(16));
    }

    [Test]
    public void Read_UsesHeaderFileKind()
    {
        byte[] bytes = new byte[64];
        WriteClassicDirectory(bytes, 8, TiffTagId.ImageWidth, 0);
        TiffHeader header = TiffHeader.Create(TiffByteOrder.LittleEndian, TiffFileKind.Classic, 8);

        TiffDirectoryReadResult result = TiffDirectoryReader.Read(header, bytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.DirectoryCount(), Is.EqualTo(1));
        Assert.That(result.Directories[0].Entries[0].TagId, Is.EqualTo(TiffTagId.ImageWidth));
    }

    [Test]
    public void DirectoryAndResultCopyMutableStorage()
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = TiffTagId.ImageWidth;
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { entry });
        TiffDirectoryReadResult result = TiffDirectoryReadResult.Create(new TiffDirectory[] { directory }, new TiffDiagnostics());
        entry.TagId = TiffTagId.ImageLength;
        directory.Entries[0].TagId = TiffTagId.Compression;

        Assert.That(result.Directories[0].Entries[0].TagId, Is.EqualTo(TiffTagId.ImageWidth));
        Assert.That(result.Directories[0].FindEntry(TiffTagId.ImageWidth)!.TagId, Is.EqualTo(TiffTagId.ImageWidth));
        Assert.That(result.Directories[0].FindEntry(TiffTagId.ImageLength), Is.Null);
    }

    [Test]
    public void HelperMethods_TrackOffsetsAndRanges()
    {
        ulong[] offsets = TiffDirectoryReader.AppendOffset(new ulong[] { 8 }, 32);

        Assert.That(TiffDirectoryReader.ContainsOffset(offsets, 8), Is.True);
        Assert.That(TiffDirectoryReader.ContainsOffset(offsets, 16), Is.False);
        Assert.That(TiffDirectoryReader.HasRange(64, 8, 56), Is.True);
        Assert.That(TiffDirectoryReader.HasRange(64, 8, 57), Is.False);
        Assert.That(TiffDirectoryReader.IsKnownByteOrder(TiffByteOrder.BigEndian), Is.True);
        Assert.That(TiffDirectoryReader.IsKnownByteOrder(TiffByteOrder.Unknown), Is.False);
    }

    [Test]
    public void LimitHelpers_NormalizeNullOptionsAndLimits()
    {
        TiffReadOptions options = new TiffReadOptions();
        options.Limits = null!;

        TiffLimits limits = TiffDirectoryReader.GetLimits(options);

        Assert.That(TiffDirectoryReader.NormalizeOptions(null!), Is.Not.Null);
        Assert.That(limits.MaximumIfdCount, Is.EqualTo(TiffLimits.DefaultMaximumIfdCount));
        Assert.That(options.Limits, Is.SameAs(limits));
        Assert.That(TiffDirectoryReader.AllowsRawTagCount(1, null!), Is.True);
        Assert.That(TiffDirectoryReader.AllowsRawTagCount((ulong)TiffLimits.DefaultMaximumTagCountPerIfd + 1, null!), Is.False);
    }

    static void WriteClassicDirectory(byte[] bytes, int offset, int tagId, uint nextOffset)
    {
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), 1, TiffByteOrder.LittleEndian);
        WriteClassicEntry(bytes, offset + 2, tagId);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 14, 4), nextOffset, TiffByteOrder.LittleEndian);
    }

    static void WriteClassicDirectoryWithTwoEntries(byte[] bytes, int offset, uint nextOffset)
    {
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), 2, TiffByteOrder.LittleEndian);
        WriteClassicEntry(bytes, offset + 2, TiffTagId.ImageWidth);
        WriteClassicEntry(bytes, offset + 14, TiffTagId.ImageLength);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 26, 4), nextOffset, TiffByteOrder.LittleEndian);
    }

    static void WriteClassicEntry(byte[] bytes, int offset, int tagId)
    {
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), (ushort)tagId, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 2, 2), (ushort)TiffDataType.Long, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 4, 4), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 8, 4), 100, TiffByteOrder.LittleEndian);
    }

    static void WriteBigTiffDirectory(byte[] bytes, int offset, int tagId, ulong nextOffset)
    {
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(offset, 8), 1, TiffByteOrder.LittleEndian);
        WriteBigTiffEntry(bytes, offset + 8, tagId);
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(offset + 28, 8), nextOffset, TiffByteOrder.LittleEndian);
    }

    static void WriteBigTiffEntry(byte[] bytes, int offset, int tagId)
    {
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), (ushort)tagId, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 2, 2), (ushort)TiffDataType.Long, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(offset + 4, 8), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(offset + 12, 8), 100, TiffByteOrder.LittleEndian);
    }
}
