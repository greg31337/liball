using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffAllocationBudgetTests
{
    [Test]
    public void Validate_ReportsConfiguredImageWidthBudget()
    {
        TiffReadOptions options = new TiffReadOptions();
        options.Limits.MaximumImageWidth = 1000;
        byte[] fileBytes = CreateStripFile(1001, 1);

        TiffValidationResult result = TiffValidator.Validate(fileBytes, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.ImageDimensionLimitExceeded), Is.True);
    }

    [Test]
    public void Validate_ReportsConfiguredImagePixelBudget()
    {
        TiffReadOptions options = new TiffReadOptions();
        options.Limits.MaximumImageWidth = 1000;
        options.Limits.MaximumImageLength = 1000;
        options.Limits.MaximumImagePixels = 1000;
        byte[] fileBytes = CreateStripFile(100, 100);

        TiffValidationResult result = TiffValidator.Validate(fileBytes, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.ImageDimensionLimitExceeded), Is.True);
    }

    [Test]
    public void Validate_ReportsAllocationBudgetForHugeDeclaredTagArray()
    {
        TiffIfdEntry[][] directories = new TiffIfdEntry[][]
        {
            new TiffIfdEntry[]
            {
                CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 2),
                CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 2),
                CreateEntry(TiffTagId.BitsPerSample, TiffDataType.Short, 1073741824, 96),
                CreateEntry(TiffTagId.PhotometricInterpretation, TiffDataType.Short, 1, (ulong)TiffPhotometric.BlackIsZero),
                CreateEntry(TiffTagId.RowsPerStrip, TiffDataType.Long, 1, 2),
                CreateEntry(TiffTagId.StripOffsets, TiffDataType.Long, 1, 120),
                CreateEntry(TiffTagId.StripByteCounts, TiffDataType.Long, 1, 4)
            }
        };
        byte[] fileBytes = CreateClassicDirectoryFile(directories, 128);

        TiffValidationResult result = TiffValidator.Validate(fileBytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.AllocationLimitExceeded), Is.True);
    }

    [Test]
    public void ReadClassic_ReportsTagCountBudgetBeforeAllocatingDirectory()
    {
        byte[] fileBytes = new byte[32];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        TiffBinaryWriter.WriteUInt16(fileBytes.AsSpan(8, 2), 2, TiffByteOrder.LittleEndian);
        TiffReadOptions options = new TiffReadOptions();
        options.Limits.MaximumTagCountPerIfd = 1;

        TiffDirectoryReadResult result = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.DirectoryCount(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.TagCountLimitExceeded), Is.True);
    }

    static byte[] CreateStripFile(ulong width, ulong length)
    {
        TiffIfdEntry[][] directories = new TiffIfdEntry[][]
        {
            new TiffIfdEntry[]
            {
                CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, width),
                CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, length),
                CreateEntry(TiffTagId.BitsPerSample, TiffDataType.Short, 1, 8),
                CreateEntry(TiffTagId.PhotometricInterpretation, TiffDataType.Short, 1, (ulong)TiffPhotometric.BlackIsZero),
                CreateEntry(TiffTagId.RowsPerStrip, TiffDataType.Long, 1, length),
                CreateEntry(TiffTagId.StripOffsets, TiffDataType.Long, 1, 120),
                CreateEntry(TiffTagId.StripByteCounts, TiffDataType.Long, 1, 1)
            }
        };

        return CreateClassicDirectoryFile(directories, 128);
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
