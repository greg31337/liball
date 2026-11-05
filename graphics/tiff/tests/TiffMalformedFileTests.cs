using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffMalformedFileTests
{
    [Test]
    public void Validate_ReportsInvalidHeaderVersion()
    {
        byte[] fileBytes = new byte[] { 0x49, 0x49, 0x63, 0x00, 0x08, 0x00, 0x00, 0x00 };

        TiffValidationResult result = TiffValidator.Validate(fileBytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ImageCount, Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTiffVersion), Is.True);
    }

    [Test]
    public void Validate_ReportsDirectoryOffsetOutsideFile()
    {
        byte[] fileBytes = new byte[16];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 32);

        TiffValidationResult result = TiffValidator.Validate(fileBytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ImageCount, Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
    }

    [Test]
    public void Validate_ReportsTruncatedDirectoryEntries()
    {
        byte[] fileBytes = new byte[16];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        TiffBinaryWriter.WriteUInt16(fileBytes.AsSpan(8, 2), 1, TiffByteOrder.LittleEndian);

        TiffValidationResult result = TiffValidator.Validate(fileBytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ImageCount, Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.TruncatedStream), Is.True);
    }

    [Test]
    public void Validate_ReportsIfdChainCycle()
    {
        byte[] fileBytes = new byte[16];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        TiffBinaryWriter.WriteUInt16(fileBytes.AsSpan(8, 2), 0, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(fileBytes.AsSpan(10, 4), 8, TiffByteOrder.LittleEndian);

        TiffValidationResult result = TiffValidator.Validate(fileBytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ImageCount, Is.EqualTo(1));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.CyclicIfdChain), Is.True);
    }

    [Test]
    public void Validate_ReportsInvalidStripOffsetAndByteCountCounts()
    {
        TiffIfdEntry[][] directories = new TiffIfdEntry[][]
        {
            new TiffIfdEntry[]
            {
                CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 2),
                CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 4),
                CreateEntry(TiffTagId.BitsPerSample, TiffDataType.Short, 1, 8),
                CreateEntry(TiffTagId.PhotometricInterpretation, TiffDataType.Short, 1, (ulong)TiffPhotometric.BlackIsZero),
                CreateEntry(TiffTagId.RowsPerStrip, TiffDataType.Long, 1, 2),
                CreateEntry(TiffTagId.StripOffsets, TiffDataType.Long, 1, 80),
                CreateEntry(TiffTagId.StripByteCounts, TiffDataType.Long, 1, 4)
            }
        };
        byte[] fileBytes = CreateClassicDirectoryFile(directories, 100);

        TiffValidationResult result = TiffValidator.Validate(fileBytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ImageCount, Is.EqualTo(1));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagCount), Is.True);
    }

    [Test]
    public void Validate_ReportsOverlappingStripDataBlocks()
    {
        byte[] fileBytes = CreateOverlappingStripFileBytes();

        TiffValidationResult result = TiffValidator.Validate(fileBytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ImageCount, Is.EqualTo(1));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void Validate_ReportsTruncatedExternalTagValue()
    {
        TiffIfdEntry[][] directories = new TiffIfdEntry[][]
        {
            new TiffIfdEntry[]
            {
                CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 2),
                CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 2),
                CreateEntry(TiffTagId.SamplesPerPixel, TiffDataType.Short, 1, 3),
                CreateEntry(TiffTagId.BitsPerSample, TiffDataType.Short, 3, 200),
                CreateEntry(TiffTagId.PhotometricInterpretation, TiffDataType.Short, 1, (ulong)TiffPhotometric.Rgb),
                CreateEntry(TiffTagId.RowsPerStrip, TiffDataType.Long, 1, 2),
                CreateEntry(TiffTagId.StripOffsets, TiffDataType.Long, 1, 96),
                CreateEntry(TiffTagId.StripByteCounts, TiffDataType.Long, 1, 12)
            }
        };
        byte[] fileBytes = CreateClassicDirectoryFile(directories, 128);

        TiffValidationResult result = TiffValidator.Validate(fileBytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ImageCount, Is.EqualTo(1));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    static byte[] CreateOverlappingStripFileBytes()
    {
        TiffTagValue[] values = new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, 4),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedArray(TiffTagId.StripOffsets, TiffDataType.Long, new ulong[] { 120, 122 }),
            TiffTagValue.CreateUnsignedArray(TiffTagId.StripByteCounts, TiffDataType.Long, new ulong[] { 4, 4 })
        };
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffIfdEntry[] entries = TiffExifMetadataWriter.CreateEntries(values);
        byte[][] valueBytes = TiffExifMetadataWriter.EncodeValues(values, TiffByteOrder.LittleEndian, diagnostics);
        int layoutByteCount = TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, valueBytes);
        byte[] fileBytes = new byte[130];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        TiffDirectoryWriter.WriteClassicWithValueBytes(fileBytes.AsSpan(8, layoutByteCount), entries, valueBytes, TiffByteOrder.LittleEndian, 8, 0);
        return fileBytes;
    }

    static byte[] CreateClassicDirectoryFile(TiffIfdEntry[][] directories, int byteCount)
    {
        byte[] fileBytes = new byte[byteCount];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        TiffDirectoryWriter.WriteClassicChain(fileBytes, directories, TiffByteOrder.LittleEndian, 8);
        return fileBytes;
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
