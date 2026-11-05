using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffMalformedIfdValidationTests
{
    [Test]
    public void ReadClassic_ReportsFirstIfdOffsetOutsideFile()
    {
        byte[] bytes = new byte[16];

        TiffDirectoryReadResult result = TiffDirectoryReader.ReadClassic(bytes, TiffByteOrder.LittleEndian, 32);

        Assert.That(result.Success, Is.False);
        Assert.That(result.DirectoryCount(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
        Assert.That(result.Diagnostics.Get(0).Offset, Is.EqualTo(32));
    }

    [Test]
    public void ReadBigTiff_ReportsFirstIfdOffsetOutsideFile()
    {
        byte[] bytes = new byte[32];

        TiffDirectoryReadResult result = TiffDirectoryReader.ReadBigTiff(bytes, TiffByteOrder.LittleEndian, 64);

        Assert.That(result.Success, Is.False);
        Assert.That(result.DirectoryCount(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
        Assert.That(result.Diagnostics.Get(0).Offset, Is.EqualTo(64));
    }

    [Test]
    public void ReadClassic_ReportsTruncatedEntryArea()
    {
        byte[] bytes = new byte[16];
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(8, 2), 1, TiffByteOrder.LittleEndian);

        TiffDirectoryReadResult result = TiffDirectoryReader.ReadClassic(bytes, TiffByteOrder.LittleEndian, 8);

        Assert.That(result.Success, Is.False);
        Assert.That(result.DirectoryCount(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.TruncatedStream), Is.True);
        Assert.That(result.Diagnostics.Get(0).Offset, Is.EqualTo(8));
    }

    [Test]
    public void ReadBigTiff_ReportsTruncatedEntryArea()
    {
        byte[] bytes = new byte[32];
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(16, 8), 1, TiffByteOrder.LittleEndian);

        TiffDirectoryReadResult result = TiffDirectoryReader.ReadBigTiff(bytes, TiffByteOrder.LittleEndian, 16);

        Assert.That(result.Success, Is.False);
        Assert.That(result.DirectoryCount(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.TruncatedStream), Is.True);
        Assert.That(result.Diagnostics.Get(0).Offset, Is.EqualTo(16));
    }

    [Test]
    public void ReadClassic_PropagatesInvalidEntryDataType()
    {
        byte[] bytes = new byte[32];
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(8, 2), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(10, 2), (ushort)TiffTagId.ImageWidth, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(12, 2), 99, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(14, 4), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(18, 4), 0, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(22, 4), 0, TiffByteOrder.LittleEndian);

        TiffDirectoryReadResult result = TiffDirectoryReader.ReadClassic(bytes, TiffByteOrder.LittleEndian, 8);

        Assert.That(result.Success, Is.False);
        Assert.That(result.DirectoryCount(), Is.EqualTo(1));
        Assert.That(result.Directories[0].EntryCount(), Is.EqualTo(1));
        Assert.That(result.Directories[0].Entries[0].TagId, Is.EqualTo(TiffTagId.ImageWidth));
        Assert.That((int)result.Directories[0].Entries[0].DataType, Is.EqualTo(99));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagType), Is.True);
    }

    [Test]
    public void ReadBigTiff_PropagatesInvalidEntryDataType()
    {
        byte[] bytes = new byte[64];
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(16, 8), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(24, 2), (ushort)TiffTagId.ImageWidth, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(26, 2), 99, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(28, 8), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(36, 8), 0, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(44, 8), 0, TiffByteOrder.LittleEndian);

        TiffDirectoryReadResult result = TiffDirectoryReader.ReadBigTiff(bytes, TiffByteOrder.LittleEndian, 16);

        Assert.That(result.Success, Is.False);
        Assert.That(result.DirectoryCount(), Is.EqualTo(1));
        Assert.That(result.Directories[0].EntryCount(), Is.EqualTo(1));
        Assert.That(result.Directories[0].Entries[0].TagId, Is.EqualTo(TiffTagId.ImageWidth));
        Assert.That((int)result.Directories[0].Entries[0].DataType, Is.EqualTo(99));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagType), Is.True);
    }

    [Test]
    public void ReadBigTiff_PropagatesEntryValueByteCountOverflow()
    {
        byte[] bytes = new byte[64];
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(16, 8), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(24, 2), (ushort)TiffTagId.WhitePoint, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(26, 2), (ushort)TiffDataType.Double, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(28, 8), 0x7FFFFFFFFFFFFFFF, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(36, 8), 0, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(44, 8), 0, TiffByteOrder.LittleEndian);
        TiffReadOptions options = new TiffReadOptions();
        options.Limits.MaximumTagCountPerIfd = 1;

        TiffDirectoryReadResult result = TiffDirectoryReader.ReadBigTiff(bytes, TiffByteOrder.LittleEndian, 16, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.DirectoryCount(), Is.EqualTo(1));
        Assert.That(result.Directories[0].Entries[0].RawCount, Is.EqualTo(0x7FFFFFFFFFFFFFFFUL));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.IntegerOverflow), Is.True);
    }

    [Test]
    public void ReadClassic_ReportsCycleBeforeFollowingMalformedRepeatedDirectory()
    {
        byte[] bytes = new byte[32];
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(8, 2), 0, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(10, 4), 8, TiffByteOrder.LittleEndian);

        TiffDirectoryReadResult result = TiffDirectoryReader.ReadClassic(bytes, TiffByteOrder.LittleEndian, 8);

        Assert.That(result.Success, Is.False);
        Assert.That(result.DirectoryCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.CyclicIfdChain), Is.True);
    }
}
