using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffPyramidWriterTests
{
    [Test]
    public void WriteClassicPyramid_WritesReducedSubIfdLevelsAndDetects()
    {
        TiffIfdEntry[] parentEntries = CreateParentEntries(1024, 1024);
        TiffIfdEntry[][] overviewDirectories = new TiffIfdEntry[][]
        {
            CreateOverviewEntries(512, 512, 0, false),
            CreateOverviewEntries(256, 256, 0, false)
        };
        int byteCount = TiffCodec.CalculateClassicPyramidWriteByteCount(parentEntries, overviewDirectories);
        byte[] fileBytes = new byte[8 + byteCount];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffPyramidWriteResult writeResult = TiffCodec.WriteClassicPyramid(fileBytes, parentEntries, overviewDirectories, TiffByteOrder.LittleEndian, 8, 0);
        TiffDirectoryReadResult directoryResult = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8);
        TiffSubIfdReadResult subIfdResult = TiffSubIfdReader.Read(directoryResult.Directories[0], fileBytes, TiffByteOrder.LittleEndian, TiffFileKind.Classic);
        TiffPyramidDetectionResult pyramidResult = TiffCodec.DetectPyramid(directoryResult.Directories[0], subIfdResult.SubIfds, fileBytes, TiffByteOrder.LittleEndian);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(writeResult.BytesWritten, Is.EqualTo((ulong)byteCount));
        Assert.That(directoryResult.Success, Is.True);
        Assert.That(subIfdResult.Success, Is.True);
        Assert.That(subIfdResult.SubIfdCount(), Is.EqualTo(2));
        Assert.That(ReadUnsigned(subIfdResult.SubIfds[0].Directories[0], fileBytes, TiffTagId.NewSubfileType), Is.EqualTo(1));
        Assert.That(pyramidResult.Success, Is.True);
        Assert.That(pyramidResult.Pyramid.HasPyramid, Is.True);
        Assert.That(pyramidResult.Pyramid.LevelCount(), Is.EqualTo(2));
        Assert.That(pyramidResult.Pyramid.Levels[0].Width, Is.EqualTo(512));
        Assert.That(pyramidResult.Pyramid.Levels[1].Width, Is.EqualTo(256));
    }

    [Test]
    public void WriteClassicPyramid_PreservesExistingNewSubfileTypeBits()
    {
        TiffIfdEntry[] parentEntries = CreateParentEntries(1024, 1024);
        TiffIfdEntry[][] overviewDirectories = new TiffIfdEntry[][]
        {
            CreateOverviewEntries(512, 512, 2, true)
        };
        int byteCount = TiffCodec.CalculateClassicPyramidWriteByteCount(parentEntries, overviewDirectories);
        byte[] fileBytes = new byte[8 + byteCount];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffPyramidWriteResult writeResult = TiffCodec.WriteClassicPyramid(fileBytes, parentEntries, overviewDirectories, TiffByteOrder.LittleEndian, 8, 0);
        TiffDirectoryReadResult directoryResult = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8);
        TiffSubIfdReadResult subIfdResult = TiffSubIfdReader.Read(directoryResult.Directories[0], fileBytes, TiffByteOrder.LittleEndian, TiffFileKind.Classic);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(ReadUnsigned(subIfdResult.SubIfds[0].Directories[0], fileBytes, TiffTagId.NewSubfileType), Is.EqualTo(3));
    }

    [Test]
    public void WriteBigTiffPyramid_WritesLong8SubIfdOffsetsAndDetects()
    {
        TiffIfdEntry[] parentEntries = CreateParentEntries(2048, 2048);
        TiffIfdEntry[][] overviewDirectories = new TiffIfdEntry[][]
        {
            CreateOverviewEntries(1024, 1024, 0, false),
            CreateOverviewEntries(512, 512, 0, false)
        };
        int byteCount = TiffCodec.CalculateBigTiffPyramidWriteByteCount(parentEntries, overviewDirectories);
        byte[] fileBytes = new byte[16 + byteCount];
        TiffHeader.WriteBigTiff(fileBytes.AsSpan(0, 16), TiffByteOrder.LittleEndian, 16);

        TiffPyramidWriteResult writeResult = TiffCodec.WriteBigTiffPyramid(fileBytes, parentEntries, overviewDirectories, TiffByteOrder.LittleEndian, 16, 0);
        TiffDirectoryReadResult directoryResult = TiffDirectoryReader.ReadBigTiff(fileBytes, TiffByteOrder.LittleEndian, 16);
        TiffSubIfdReadResult subIfdResult = TiffSubIfdReader.Read(directoryResult.Directories[0], fileBytes, TiffByteOrder.LittleEndian, TiffFileKind.BigTiff);
        TiffPyramidDetectionResult pyramidResult = TiffCodec.DetectPyramid(directoryResult.Directories[0], subIfdResult.SubIfds, fileBytes, TiffByteOrder.LittleEndian);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(writeResult.BytesWritten, Is.EqualTo((ulong)byteCount));
        Assert.That(directoryResult.Directories[0].FindEntry(TiffTagId.SubIFDs)!.DataType, Is.EqualTo(TiffDataType.Long8));
        Assert.That(subIfdResult.Success, Is.True);
        Assert.That(subIfdResult.SubIfdCount(), Is.EqualTo(2));
        Assert.That(pyramidResult.Success, Is.True);
        Assert.That(pyramidResult.Pyramid.LevelCount(), Is.EqualTo(2));
        Assert.That(pyramidResult.Pyramid.Levels[0].Width, Is.EqualTo(1024));
    }

    [Test]
    public void WriteClassicPyramid_ReportsShortDestination()
    {
        TiffIfdEntry[] parentEntries = CreateParentEntries(1024, 1024);
        TiffIfdEntry[][] overviewDirectories = new TiffIfdEntry[][]
        {
            CreateOverviewEntries(512, 512, 0, false)
        };

        TiffPyramidWriteResult result = TiffCodec.WriteClassicPyramid(new byte[24], parentEntries, overviewDirectories, TiffByteOrder.LittleEndian, 8, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
    }

    static TiffIfdEntry[] CreateParentEntries(uint width, uint length)
    {
        return new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, width),
            CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, length)
        };
    }

    static TiffIfdEntry[] CreateOverviewEntries(uint width, uint length, uint newSubfileType, bool includeNewSubfileType)
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[2];
        if (includeNewSubfileType)
        {
            entries = new TiffIfdEntry[3];
        }

        entries[0] = CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, width);
        entries[1] = CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, length);
        if (includeNewSubfileType)
        {
            entries[2] = CreateEntry(TiffTagId.NewSubfileType, TiffDataType.Long, 1, newSubfileType);
        }

        return entries;
    }

    static ulong ReadUnsigned(TiffDirectory directory, byte[] fileBytes, int tagId)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        ulong value = TiffImageInfoExtractor.ReadOptionalUnsignedScalar(directory, fileBytes, TiffByteOrder.LittleEndian, new TiffLimits(), tagId, 0, diagnostics);
        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        return value;
    }

    static TiffIfdEntry CreateEntry(int tagId, TiffDataType dataType, long count, ulong valueOffset)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = dataType;
        entry.Count = count;
        entry.RawCount = (ulong)count;
        entry.ValueOffset = valueOffset;
        entry.ValueOffsetBytes = new byte[4];
        TiffBinaryWriter.WriteUInt32(entry.ValueOffsetBytes, (uint)valueOffset, TiffByteOrder.LittleEndian);
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(dataType, count);
        entry.ValueSlotByteCount = 4;
        return entry;
    }
}
