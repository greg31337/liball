using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffSubIfdWriterTests
{
    [Test]
    public void WriteClassic_WritesInlineSubIfdOffsetAndDirectory()
    {
        TiffIfdEntry[] parentEntries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 1024)
        };
        TiffIfdEntry[][][] subIfdChains = new TiffIfdEntry[][][]
        {
            new TiffIfdEntry[][]
            {
                new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 256) }
            }
        };
        int byteCount = TiffSubIfdWriter.CalculateClassicByteCount(parentEntries, subIfdChains);
        byte[] fileBytes = new byte[8 + byteCount];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffDiagnostics diagnostics = TiffSubIfdWriter.WriteClassic(fileBytes, parentEntries, subIfdChains, TiffByteOrder.LittleEndian, 8, 0);
        TiffDirectoryReadResult directoryResult = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8);
        TiffSubIfdReadResult subIfdResult = TiffSubIfdReader.Read(directoryResult.Directories[0], fileBytes, TiffByteOrder.LittleEndian, TiffFileKind.Classic);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(byteCount, Is.EqualTo(48));
        Assert.That(directoryResult.Success, Is.True);
        Assert.That(directoryResult.DirectoryCount(), Is.EqualTo(1));
        Assert.That(directoryResult.Directories[0].Entries[1].TagId, Is.EqualTo(TiffTagId.SubIFDs));
        Assert.That(directoryResult.Directories[0].Entries[1].ValueOffset, Is.EqualTo(38));
        Assert.That(subIfdResult.Success, Is.True);
        Assert.That(subIfdResult.SubIfdCount(), Is.EqualTo(1));
        Assert.That(subIfdResult.SubIfds[0].Offset, Is.EqualTo(38));
        Assert.That(subIfdResult.SubIfds[0].Directories[0].Entries[0].TagId, Is.EqualTo(TiffTagId.ImageLength));
    }

    [Test]
    public void WriteClassic_WritesExternalSubIfdOffsetArray()
    {
        TiffIfdEntry[] parentEntries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 1024)
        };
        TiffIfdEntry[][][] subIfdChains = new TiffIfdEntry[][][]
        {
            new TiffIfdEntry[][]
            {
                new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 256) }
            },
            new TiffIfdEntry[][]
            {
                new TiffIfdEntry[] { CreateEntry(TiffTagId.TileWidth, TiffDataType.Long, 1, 64) }
            }
        };
        int byteCount = TiffSubIfdWriter.CalculateClassicByteCount(parentEntries, subIfdChains);
        byte[] fileBytes = new byte[8 + byteCount];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffDiagnostics diagnostics = TiffSubIfdWriter.WriteClassic(fileBytes, parentEntries, subIfdChains, TiffByteOrder.LittleEndian, 8, 0);
        TiffDirectoryReadResult directoryResult = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8);
        TiffSubIfdReadResult subIfdResult = TiffSubIfdReader.Read(directoryResult.Directories[0], fileBytes, TiffByteOrder.LittleEndian, TiffFileKind.Classic);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(byteCount, Is.EqualTo(74));
        Assert.That(directoryResult.Directories[0].Entries[1].ValueOffset, Is.EqualTo(38));
        Assert.That(TiffBinaryReader.ReadUInt32(fileBytes.AsSpan(38, 4), TiffByteOrder.LittleEndian), Is.EqualTo(46));
        Assert.That(TiffBinaryReader.ReadUInt32(fileBytes.AsSpan(42, 4), TiffByteOrder.LittleEndian), Is.EqualTo(64));
        Assert.That(subIfdResult.Success, Is.True);
        Assert.That(subIfdResult.SubIfdCount(), Is.EqualTo(2));
        Assert.That(subIfdResult.SubIfds[0].Offset, Is.EqualTo(46));
        Assert.That(subIfdResult.SubIfds[1].Offset, Is.EqualTo(64));
        Assert.That(subIfdResult.SubIfds[1].Directories[0].Entries[0].TagId, Is.EqualTo(TiffTagId.TileWidth));
    }

    [Test]
    public void WriteBigTiff_WritesExternalLong8OffsetArray()
    {
        TiffIfdEntry[] parentEntries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 1024)
        };
        TiffIfdEntry[][][] subIfdChains = new TiffIfdEntry[][][]
        {
            new TiffIfdEntry[][]
            {
                new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 256) }
            },
            new TiffIfdEntry[][]
            {
                new TiffIfdEntry[] { CreateEntry(TiffTagId.TileWidth, TiffDataType.Long, 1, 64) }
            }
        };
        int byteCount = TiffSubIfdWriter.CalculateBigTiffByteCount(parentEntries, subIfdChains);
        byte[] fileBytes = new byte[16 + byteCount];
        TiffHeader.WriteBigTiff(fileBytes.AsSpan(0, 16), TiffByteOrder.LittleEndian, 16);

        TiffDiagnostics diagnostics = TiffSubIfdWriter.WriteBigTiff(fileBytes, parentEntries, subIfdChains, TiffByteOrder.LittleEndian, 16, 0);
        TiffDirectoryReadResult directoryResult = TiffDirectoryReader.ReadBigTiff(fileBytes, TiffByteOrder.LittleEndian, 16);
        TiffSubIfdReadResult subIfdResult = TiffSubIfdReader.Read(directoryResult.Directories[0], fileBytes, TiffByteOrder.LittleEndian, TiffFileKind.BigTiff);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(byteCount, Is.EqualTo(148));
        Assert.That(directoryResult.Directories[0].Entries[1].DataType, Is.EqualTo(TiffDataType.Long8));
        Assert.That(directoryResult.Directories[0].Entries[1].ValueOffset, Is.EqualTo(72));
        Assert.That(TiffBinaryReader.ReadUInt64(fileBytes.AsSpan(72, 8), TiffByteOrder.LittleEndian), Is.EqualTo(88));
        Assert.That(TiffBinaryReader.ReadUInt64(fileBytes.AsSpan(80, 8), TiffByteOrder.LittleEndian), Is.EqualTo(128));
        Assert.That(subIfdResult.Success, Is.True);
        Assert.That(subIfdResult.SubIfdCount(), Is.EqualTo(2));
        Assert.That(subIfdResult.SubIfds[0].Offset, Is.EqualTo(88));
        Assert.That(subIfdResult.SubIfds[1].Offset, Is.EqualTo(128));
    }

    [Test]
    public void WriteClassic_ReplacesExistingSubIfdTag()
    {
        TiffIfdEntry[] parentEntries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.SubIFDs, TiffDataType.Short, 1, 0),
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 1024)
        };
        TiffIfdEntry[][][] subIfdChains = new TiffIfdEntry[][][]
        {
            new TiffIfdEntry[][]
            {
                new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 256) }
            }
        };
        byte[] fileBytes = new byte[8 + TiffSubIfdWriter.CalculateClassicByteCount(parentEntries, subIfdChains)];

        TiffDiagnostics diagnostics = TiffSubIfdWriter.WriteClassic(fileBytes, parentEntries, subIfdChains, TiffByteOrder.LittleEndian, 8, 0);
        TiffDirectoryReadResult directoryResult = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(directoryResult.Directories[0].EntryCount(), Is.EqualTo(2));
        Assert.That(directoryResult.Directories[0].Entries[1].TagId, Is.EqualTo(TiffTagId.SubIFDs));
        Assert.That(directoryResult.Directories[0].Entries[1].DataType, Is.EqualTo(TiffDataType.Long));
    }

    [Test]
    public void WriteClassic_ReportsShortDestination()
    {
        TiffIfdEntry[] parentEntries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 1024)
        };
        TiffIfdEntry[][][] subIfdChains = new TiffIfdEntry[][][]
        {
            new TiffIfdEntry[][]
            {
                new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 256) }
            }
        };
        byte[] fileBytes = new byte[32];

        TiffDiagnostics diagnostics = TiffSubIfdWriter.WriteClassic(fileBytes, parentEntries, subIfdChains, TiffByteOrder.LittleEndian, 8, 0);

        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void WriteBigTiff_UsesConfiguredSubIfdDepthLimit()
    {
        TiffIfdEntry[] parentEntries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 1024)
        };
        TiffIfdEntry[][][] subIfdChains = new TiffIfdEntry[][][]
        {
            new TiffIfdEntry[][]
            {
                new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 256) }
            }
        };
        TiffWriteOptions options = new TiffWriteOptions();
        options.Limits.MaximumSubIfdDepth = 0;
        byte[] fileBytes = new byte[128];

        TiffDiagnostics diagnostics = TiffSubIfdWriter.WriteBigTiff(fileBytes, parentEntries, subIfdChains, TiffByteOrder.LittleEndian, 16, 0, options);

        Assert.That(diagnostics.ContainsCode(TiffErrorCode.IfdCountLimitExceeded), Is.True);
    }

    [Test]
    public void WriteClassic_ReportsOffsetOverflow()
    {
        TiffIfdEntry[] parentEntries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 1024)
        };
        TiffIfdEntry[][][] subIfdChains = new TiffIfdEntry[][][]
        {
            new TiffIfdEntry[][]
            {
                new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 256) }
            }
        };
        byte[] fileBytes = new byte[64];

        TiffDiagnostics diagnostics = TiffSubIfdWriter.WriteClassic(fileBytes, parentEntries, subIfdChains, TiffByteOrder.LittleEndian, uint.MaxValue - 8UL, 0);

        Assert.That(diagnostics.ContainsCode(TiffErrorCode.IntegerOverflow), Is.True);
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
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(dataType, count);
        entry.ValueSlotByteCount = 4;
        return entry;
    }
}
