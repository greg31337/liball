using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffDirectoryWriterTests
{
    [Test]
    public void WriteClassic_SortsEntriesByTagIdAndWritesLittleEndianBytes()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 3),
            CreateEntry(TiffTagId.BitsPerSample, TiffDataType.Short, 1, 8),
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 4)
        };
        byte[] bytes = new byte[TiffDirectoryWriter.CalculateClassicDirectoryByteCount(entries.Length)];

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteClassic(bytes, entries, TiffByteOrder.LittleEndian, 0);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(bytes, Is.EqualTo(new byte[]
        {
            0x03, 0x00,
            0x00, 0x01, 0x04, 0x00, 0x01, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
            0x01, 0x01, 0x04, 0x00, 0x01, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00,
            0x02, 0x01, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
        }));
    }

    [Test]
    public void WriteClassic_WritesBigEndianEntriesAndNextOffset()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.StripOffsets, TiffDataType.Long, 1, 0x01020304),
            CreateEntry(TiffTagId.BitsPerSample, TiffDataType.Short, 3, 0x26)
        };
        byte[] bytes = new byte[TiffDirectoryWriter.CalculateClassicDirectoryByteCount(entries.Length)];

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteClassic(bytes, entries, TiffByteOrder.BigEndian, 0x11223344);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(bytes, Is.EqualTo(new byte[]
        {
            0x00, 0x02,
            0x01, 0x02, 0x00, 0x03, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x26,
            0x01, 0x11, 0x00, 0x04, 0x00, 0x00, 0x00, 0x01, 0x01, 0x02, 0x03, 0x04,
            0x11, 0x22, 0x33, 0x44
        }));
    }

    [Test]
    public void WriteClassic_RoundTripsThroughDirectoryReader()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 3),
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 4)
        };
        byte[] fileBytes = new byte[64];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteClassic(fileBytes.AsSpan(8), entries, TiffByteOrder.LittleEndian, 0);
        TiffDirectoryReadResult result = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(result.Success, Is.True);
        Assert.That(result.DirectoryCount(), Is.EqualTo(1));
        Assert.That(result.Directories[0].EntryCount(), Is.EqualTo(2));
        Assert.That(result.Directories[0].Entries[0].TagId, Is.EqualTo(TiffTagId.ImageWidth));
        Assert.That(result.Directories[0].Entries[1].TagId, Is.EqualTo(TiffTagId.ImageLength));
    }

    [Test]
    public void WriteBigTiff_SortsEntriesByTagIdAndWritesLittleEndianBytes()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.StripOffsets, TiffDataType.Long8, 1, 0x1122334455667788),
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 1024),
            CreateEntry(TiffTagId.BitsPerSample, TiffDataType.Short, 3, 0x0000000800080008)
        };
        byte[] bytes = new byte[TiffDirectoryWriter.CalculateBigTiffDirectoryByteCount(entries.Length)];

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteBigTiff(bytes, entries, TiffByteOrder.LittleEndian, 0);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(bytes, Is.EqualTo(new byte[]
        {
            0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x01, 0x04, 0x00,
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x02, 0x01, 0x03, 0x00,
            0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x08, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x00,
            0x11, 0x01, 0x10, 0x00,
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        }));
    }

    [Test]
    public void WriteBigTiff_WritesBigEndianEntriesAndNextOffset()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.StripOffsets, TiffDataType.Long8, 1, 0x1122334455667788),
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 1024)
        };
        byte[] bytes = new byte[TiffDirectoryWriter.CalculateBigTiffDirectoryByteCount(entries.Length)];

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteBigTiff(bytes, entries, TiffByteOrder.BigEndian, 0x0102030405060708);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(bytes, Is.EqualTo(new byte[]
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02,
            0x01, 0x00, 0x00, 0x04,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00,
            0x01, 0x11, 0x00, 0x10,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
            0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
        }));
    }

    [Test]
    public void WriteBigTiff_RoundTripsThroughDirectoryReader()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 3),
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 4)
        };
        byte[] fileBytes = new byte[96];
        TiffHeader.WriteBigTiff(fileBytes.AsSpan(0, 16), TiffByteOrder.LittleEndian, 16);

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteBigTiff(fileBytes.AsSpan(16), entries, TiffByteOrder.LittleEndian, 0);
        TiffDirectoryReadResult result = TiffDirectoryReader.ReadBigTiff(fileBytes, TiffByteOrder.LittleEndian, 16);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(result.Success, Is.True);
        Assert.That(result.DirectoryCount(), Is.EqualTo(1));
        Assert.That(result.Directories[0].EntryCount(), Is.EqualTo(2));
        Assert.That(result.Directories[0].Entries[0].TagId, Is.EqualTo(TiffTagId.ImageWidth));
        Assert.That(result.Directories[0].Entries[1].TagId, Is.EqualTo(TiffTagId.ImageLength));
    }

    [Test]
    public void WriteClassicChain_WritesNextIfdOffsetsForMultipleDirectories()
    {
        TiffIfdEntry[][] directories = new TiffIfdEntry[][]
        {
            new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 4) },
            new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 3) }
        };
        int chainByteCount = TiffDirectoryWriter.CalculateClassicDirectoryChainByteCount(directories);
        byte[] fileBytes = new byte[8 + chainByteCount];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteClassicChain(fileBytes, directories, TiffByteOrder.LittleEndian, 8);
        TiffDirectoryReadResult result = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(chainByteCount, Is.EqualTo(36));
        Assert.That(result.Success, Is.True);
        Assert.That(result.DirectoryCount(), Is.EqualTo(2));
        Assert.That(result.Directories[0].Offset, Is.EqualTo(8));
        Assert.That(result.Directories[0].NextIfdOffset, Is.EqualTo(26));
        Assert.That(result.Directories[1].Offset, Is.EqualTo(26));
        Assert.That(result.Directories[1].NextIfdOffset, Is.EqualTo(0));
    }

    [Test]
    public void WriteBigTiffChain_AlignsNextIfdOffsetForMultipleDirectories()
    {
        TiffIfdEntry[][] directories = new TiffIfdEntry[][]
        {
            new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 4) },
            new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 3) }
        };
        int chainByteCount = TiffDirectoryWriter.CalculateBigTiffDirectoryChainByteCount(directories);
        byte[] fileBytes = new byte[16 + chainByteCount];
        TiffHeader.WriteBigTiff(fileBytes.AsSpan(0, 16), TiffByteOrder.LittleEndian, 16);

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteBigTiffChain(fileBytes, directories, TiffByteOrder.LittleEndian, 16);
        TiffDirectoryReadResult result = TiffDirectoryReader.ReadBigTiff(fileBytes, TiffByteOrder.LittleEndian, 16);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(chainByteCount, Is.EqualTo(76));
        Assert.That(result.Success, Is.True);
        Assert.That(result.DirectoryCount(), Is.EqualTo(2));
        Assert.That(result.Directories[0].NextIfdOffset, Is.EqualTo(56));
        Assert.That(result.Directories[1].Offset, Is.EqualTo(56));
        Assert.That(result.Directories[1].NextIfdOffset, Is.EqualTo(0));
        Assert.That(fileBytes[52], Is.EqualTo(0));
        Assert.That(fileBytes[55], Is.EqualTo(0));
    }

    [Test]
    public void WriteClassicWithValueBytes_WritesInlineAndExternalValuesAndPatchesOffset()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.BitsPerSample, TiffDataType.Short, 3, 0),
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 0)
        };
        byte[][] values = new byte[][]
        {
            new byte[] { 0x08, 0x00, 0x08, 0x00, 0x08, 0x00 },
            new byte[] { 0x80, 0x02, 0x00, 0x00 }
        };
        int layoutByteCount = TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, values);
        byte[] fileBytes = new byte[8 + layoutByteCount];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteClassicWithValueBytes(fileBytes.AsSpan(8, layoutByteCount), entries, values, TiffByteOrder.LittleEndian, 8, 0);
        TiffDirectoryReadResult directoryResult = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8);
        TiffTagValueLoadResult widthBytes = TiffTagValueLoader.LoadBytes(directoryResult.Directories[0].Entries[0], fileBytes);
        TiffTagValueLoadResult bitsBytes = TiffTagValueLoader.LoadBytes(directoryResult.Directories[0].Entries[1], fileBytes);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(layoutByteCount, Is.EqualTo(36));
        Assert.That(directoryResult.Success, Is.True);
        Assert.That(directoryResult.Directories[0].Entries[0].TagId, Is.EqualTo(TiffTagId.ImageWidth));
        Assert.That(directoryResult.Directories[0].Entries[0].HasExternalValue(), Is.False);
        Assert.That(widthBytes.Bytes, Is.EqualTo(new byte[] { 0x80, 0x02, 0x00, 0x00 }));
        Assert.That(directoryResult.Directories[0].Entries[1].TagId, Is.EqualTo(TiffTagId.BitsPerSample));
        Assert.That(directoryResult.Directories[0].Entries[1].ValueOffset, Is.EqualTo(38));
        Assert.That(directoryResult.Directories[0].Entries[1].HasExternalValue(), Is.True);
        Assert.That(bitsBytes.Bytes, Is.EqualTo(values[0]));
        Assert.That(entries[0].TagId, Is.EqualTo(TiffTagId.BitsPerSample));
    }

    [Test]
    public void WriteClassicWithValueBytes_AlignsMultipleExternalValues()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.BitsPerSample, TiffDataType.Short, 3, 0),
            CreateEntry(TiffTagId.NewSubfileType, TiffDataType.Byte, 5, 0)
        };
        byte[][] values = new byte[][]
        {
            new byte[] { 0x08, 0x00, 0x08, 0x00, 0x08, 0x00 },
            new byte[] { 1, 2, 3, 4, 5 }
        };
        int layoutByteCount = TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, values);
        byte[] fileBytes = new byte[8 + layoutByteCount];

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteClassicWithValueBytes(fileBytes.AsSpan(8, layoutByteCount), entries, values, TiffByteOrder.LittleEndian, 8, 0);
        TiffDirectoryReadResult directoryResult = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(layoutByteCount, Is.EqualTo(42));
        Assert.That(directoryResult.Directories[0].Entries[0].ValueOffset, Is.EqualTo(38));
        Assert.That(directoryResult.Directories[0].Entries[1].ValueOffset, Is.EqualTo(44));
        Assert.That(fileBytes[43], Is.EqualTo(0));
        Assert.That(TiffTagValueLoader.LoadBytes(directoryResult.Directories[0].Entries[0], fileBytes).Bytes, Is.EqualTo(values[1]));
        Assert.That(TiffTagValueLoader.LoadBytes(directoryResult.Directories[0].Entries[1], fileBytes).Bytes, Is.EqualTo(values[0]));
    }

    [Test]
    public void WriteBigTiffWithValueBytes_WritesInlineAndExternalValuesAndPatchesOffset()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.WhitePoint, TiffDataType.Double, 2, 0),
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 0)
        };
        byte[][] values = new byte[][]
        {
            new byte[] { 0, 0, 0, 0, 0, 0, 0xE0, 0x3F, 0, 0, 0, 0, 0, 0, 0xE8, 0x3F },
            new byte[] { 0x00, 0x04, 0x00, 0x00 }
        };
        int layoutByteCount = TiffDirectoryWriter.CalculateBigTiffDirectoryLayoutByteCount(entries, values);
        byte[] fileBytes = new byte[16 + layoutByteCount];
        TiffHeader.WriteBigTiff(fileBytes.AsSpan(0, 16), TiffByteOrder.LittleEndian, 16);

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteBigTiffWithValueBytes(fileBytes.AsSpan(16, layoutByteCount), entries, values, TiffByteOrder.LittleEndian, 16, 0);
        TiffDirectoryReadResult directoryResult = TiffDirectoryReader.ReadBigTiff(fileBytes, TiffByteOrder.LittleEndian, 16);
        TiffTagValueLoadResult widthBytes = TiffTagValueLoader.LoadBytes(directoryResult.Directories[0].Entries[0], fileBytes);
        TiffTagValueLoadResult whitePointBytes = TiffTagValueLoader.LoadBytes(directoryResult.Directories[0].Entries[1], fileBytes);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(layoutByteCount, Is.EqualTo(72));
        Assert.That(directoryResult.Success, Is.True);
        Assert.That(directoryResult.Directories[0].Entries[0].TagId, Is.EqualTo(TiffTagId.ImageWidth));
        Assert.That(widthBytes.Bytes, Is.EqualTo(new byte[] { 0x00, 0x04, 0x00, 0x00 }));
        Assert.That(directoryResult.Directories[0].Entries[1].ValueOffset, Is.EqualTo(72));
        Assert.That(directoryResult.Directories[0].Entries[1].HasExternalValue(), Is.True);
        Assert.That(whitePointBytes.Bytes, Is.EqualTo(values[0]));
    }

    [Test]
    public void WriteClassicChainWithValueBytes_LinksAfterExternalValues()
    {
        TiffIfdEntry[][] directories = new TiffIfdEntry[][]
        {
            new TiffIfdEntry[] { CreateEntry(TiffTagId.BitsPerSample, TiffDataType.Short, 3, 0) },
            new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 0) }
        };
        byte[][][] values = new byte[][][]
        {
            new byte[][]
            {
                new byte[] { 0x08, 0x00, 0x08, 0x00, 0x08, 0x00 }
            },
            new byte[][]
            {
                new byte[] { 0x80, 0x02, 0x00, 0x00 }
            }
        };
        int chainByteCount = TiffDirectoryWriter.CalculateClassicDirectoryValueChainByteCount(directories, values);
        byte[] fileBytes = new byte[8 + chainByteCount];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteClassicChainWithValueBytes(fileBytes, directories, values, TiffByteOrder.LittleEndian, 8);
        TiffDirectoryReadResult result = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8);
        TiffTagValueLoadResult bitsBytes = TiffTagValueLoader.LoadBytes(result.Directories[0].Entries[0], fileBytes);
        TiffTagValueLoadResult widthBytes = TiffTagValueLoader.LoadBytes(result.Directories[1].Entries[0], fileBytes);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(chainByteCount, Is.EqualTo(42));
        Assert.That(result.Success, Is.True);
        Assert.That(result.DirectoryCount(), Is.EqualTo(2));
        Assert.That(result.Directories[0].NextIfdOffset, Is.EqualTo(32));
        Assert.That(result.Directories[0].Entries[0].ValueOffset, Is.EqualTo(26));
        Assert.That(bitsBytes.Bytes, Is.EqualTo(values[0][0]));
        Assert.That(result.Directories[1].Offset, Is.EqualTo(32));
        Assert.That(widthBytes.Bytes, Is.EqualTo(values[1][0]));
    }

    [Test]
    public void WriteBigTiffChainWithValueBytes_LinksAfterExternalValues()
    {
        TiffIfdEntry[][] directories = new TiffIfdEntry[][]
        {
            new TiffIfdEntry[] { CreateEntry(TiffTagId.WhitePoint, TiffDataType.Double, 2, 0) },
            new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 0) }
        };
        byte[][][] values = new byte[][][]
        {
            new byte[][]
            {
                new byte[] { 0, 0, 0, 0, 0, 0, 0xE0, 0x3F, 0, 0, 0, 0, 0, 0, 0xE8, 0x3F }
            },
            new byte[][]
            {
                new byte[] { 0x00, 0x04, 0x00, 0x00 }
            }
        };
        int chainByteCount = TiffDirectoryWriter.CalculateBigTiffDirectoryValueChainByteCount(directories, values);
        byte[] fileBytes = new byte[16 + chainByteCount];
        TiffHeader.WriteBigTiff(fileBytes.AsSpan(0, 16), TiffByteOrder.LittleEndian, 16);

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteBigTiffChainWithValueBytes(fileBytes, directories, values, TiffByteOrder.LittleEndian, 16);
        TiffDirectoryReadResult result = TiffDirectoryReader.ReadBigTiff(fileBytes, TiffByteOrder.LittleEndian, 16);
        TiffTagValueLoadResult whitePointBytes = TiffTagValueLoader.LoadBytes(result.Directories[0].Entries[0], fileBytes);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(chainByteCount, Is.EqualTo(92));
        Assert.That(result.Success, Is.True);
        Assert.That(result.DirectoryCount(), Is.EqualTo(2));
        Assert.That(result.Directories[0].Entries[0].ValueOffset, Is.EqualTo(56));
        Assert.That(result.Directories[0].NextIfdOffset, Is.EqualTo(72));
        Assert.That(result.Directories[1].Offset, Is.EqualTo(72));
        Assert.That(whitePointBytes.Bytes, Is.EqualTo(values[0][0]));
    }

    [Test]
    public void WriteClassic_DoesNotMutateCallerEntryOrder()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 3),
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 4)
        };
        byte[] bytes = new byte[TiffDirectoryWriter.CalculateClassicDirectoryByteCount(entries.Length)];

        TiffDirectoryWriter.WriteClassic(bytes, entries, TiffByteOrder.LittleEndian, 0);

        Assert.That(entries[0].TagId, Is.EqualTo(TiffTagId.ImageLength));
        Assert.That(entries[1].TagId, Is.EqualTo(TiffTagId.ImageWidth));
        Assert.That(TiffDirectoryWriter.SortEntriesByTagId(entries)[0].TagId, Is.EqualTo(TiffTagId.ImageWidth));
    }

    [Test]
    public void WriteBigTiff_DoesNotMutateCallerEntryOrder()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 3),
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 4)
        };
        byte[] bytes = new byte[TiffDirectoryWriter.CalculateBigTiffDirectoryByteCount(entries.Length)];

        TiffDirectoryWriter.WriteBigTiff(bytes, entries, TiffByteOrder.LittleEndian, 0);

        Assert.That(entries[0].TagId, Is.EqualTo(TiffTagId.ImageLength));
        Assert.That(entries[1].TagId, Is.EqualTo(TiffTagId.ImageWidth));
    }

    [Test]
    public void WriteClassic_ReportsShortDestination()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 4)
        };
        byte[] bytes = new byte[8];

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteClassic(bytes, entries, TiffByteOrder.LittleEndian, 0);

        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void WriteBigTiff_ReportsShortDestination()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 4)
        };
        byte[] bytes = new byte[16];

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteBigTiff(bytes, entries, TiffByteOrder.LittleEndian, 0);

        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void WriteClassicWithValueBytes_ReportsShortDestinationForExternalPayload()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.BitsPerSample, TiffDataType.Short, 3, 0)
        };
        byte[][] values = new byte[][]
        {
            new byte[] { 0x08, 0x00, 0x08, 0x00, 0x08, 0x00 }
        };
        byte[] bytes = new byte[TiffDirectoryWriter.CalculateClassicDirectoryByteCount(entries.Length)];

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteClassicWithValueBytes(bytes, entries, values, TiffByteOrder.LittleEndian, 8, 0);

        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void WriteBigTiffWithValueBytes_ReportsValueByteCountMismatch()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 0)
        };
        byte[][] values = new byte[][]
        {
            new byte[] { 1, 2 }
        };
        byte[] bytes = new byte[TiffDirectoryWriter.CalculateBigTiffDirectoryLayoutByteCount(entries, values)];

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteBigTiffWithValueBytes(bytes, entries, values, TiffByteOrder.LittleEndian, 16, 0);

        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void WriteClassicWithValueBytes_ReportsExternalOffsetOverflow()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.NewSubfileType, TiffDataType.Byte, 5, 0)
        };
        byte[][] values = new byte[][]
        {
            new byte[] { 1, 2, 3, 4, 5 }
        };
        byte[] bytes = new byte[TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, values)];

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteClassicWithValueBytes(bytes, entries, values, TiffByteOrder.LittleEndian, uint.MaxValue, 0);

        Assert.That(diagnostics.ContainsCode(TiffErrorCode.IntegerOverflow), Is.True);
    }

    [Test]
    public void WriteClassicChain_ReportsNextOffsetOverflow()
    {
        TiffIfdEntry[][] directories = new TiffIfdEntry[][]
        {
            new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 4) },
            new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 3) }
        };
        byte[] bytes = new byte[64];

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteClassicChain(bytes, directories, TiffByteOrder.LittleEndian, uint.MaxValue - 8UL);

        Assert.That(diagnostics.ContainsCode(TiffErrorCode.IntegerOverflow), Is.True);
    }

    [Test]
    public void WriteBigTiffChain_UsesConfiguredIfdCountLimit()
    {
        TiffIfdEntry[][] directories = new TiffIfdEntry[][]
        {
            new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 4) },
            new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 3) }
        };
        TiffWriteOptions options = new TiffWriteOptions();
        options.Limits.MaximumIfdCount = 1;
        byte[] bytes = new byte[128];

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteBigTiffChain(bytes, directories, TiffByteOrder.LittleEndian, 16, options);

        Assert.That(diagnostics.ContainsCode(TiffErrorCode.IfdCountLimitExceeded), Is.True);
    }

    [Test]
    public void WriteClassic_ReportsInvalidEntryFields()
    {
        TiffIfdEntry entry = CreateEntry(70000, TiffDataType.Unknown, -1, 0x100000000);
        byte[] bytes = new byte[TiffDirectoryWriter.CalculateClassicDirectoryByteCount(1)];

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteClassic(bytes, new TiffIfdEntry[] { entry }, TiffByteOrder.Unknown, 0x100000000);

        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteOrder), Is.True);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.IntegerOverflow), Is.True);
    }

    [Test]
    public void WriteBigTiff_ReportsInvalidEntryFields()
    {
        TiffIfdEntry entry = CreateEntry(70000, TiffDataType.Unknown, -1, 0);
        byte[] bytes = new byte[TiffDirectoryWriter.CalculateBigTiffDirectoryByteCount(1)];

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteBigTiff(bytes, new TiffIfdEntry[] { entry }, TiffByteOrder.Unknown, 0);

        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteOrder), Is.True);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void WriteClassic_UsesConfiguredTagCountLimit()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 4),
            CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 3)
        };
        TiffWriteOptions options = new TiffWriteOptions();
        options.Limits.MaximumTagCountPerIfd = 1;
        byte[] bytes = new byte[TiffDirectoryWriter.CalculateClassicDirectoryByteCount(entries.Length)];

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteClassic(bytes, entries, TiffByteOrder.LittleEndian, 0, options);

        Assert.That(diagnostics.ContainsCode(TiffErrorCode.TagCountLimitExceeded), Is.True);
    }

    [Test]
    public void WriteBigTiff_UsesConfiguredTagCountLimit()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 4),
            CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 3)
        };
        TiffWriteOptions options = new TiffWriteOptions();
        options.Limits.MaximumTagCountPerIfd = 1;
        byte[] bytes = new byte[TiffDirectoryWriter.CalculateBigTiffDirectoryByteCount(entries.Length)];

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteBigTiff(bytes, entries, TiffByteOrder.LittleEndian, 0, options);

        Assert.That(diagnostics.ContainsCode(TiffErrorCode.TagCountLimitExceeded), Is.True);
    }

    static TiffIfdEntry CreateEntry(int tagId, TiffDataType dataType, long count, ulong valueOffset)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = dataType;
        entry.Count = count;
        if (count < 0)
        {
            entry.RawCount = 0;
        }
        else
        {
            entry.RawCount = (ulong)count;
        }

        entry.ValueOffset = valueOffset;
        entry.ValueOffsetBytes = new byte[4];
        entry.ValueByteCount = 0;
        entry.ValueSlotByteCount = 4;
        return entry;
    }
}
