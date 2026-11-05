using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffValidatorTests
{
    [Test]
    public void Validate_ReturnsSuccessForCompleteFile()
    {
        byte[] fileBytes = CreateValidFileBytes();

        TiffValidationResult result = TiffValidator.Validate(fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Header.FileKind, Is.EqualTo(TiffFileKind.Classic));
        Assert.That(result.ImageCount, Is.EqualTo(1));
        Assert.That(result.Diagnostics.Count(), Is.EqualTo(0));
    }

    [Test]
    public void Validate_ReportsInvalidHeader()
    {
        TiffValidationResult result = TiffValidator.Validate(new byte[] { 0, 0, 0, 0 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ImageCount, Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.TruncatedStream), Is.True);
    }

    [Test]
    public void Validate_ReportsMissingRequiredTags()
    {
        TiffIfdEntry[][] directories = new TiffIfdEntry[][]
        {
            new TiffIfdEntry[]
            {
                CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 2),
                CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 2),
                CreateEntry(TiffTagId.BitsPerSample, TiffDataType.Short, 1, 8),
                CreateEntry(TiffTagId.PhotometricInterpretation, TiffDataType.Short, 1, (ulong)TiffPhotometric.BlackIsZero),
                CreateEntry(TiffTagId.StripOffsets, TiffDataType.Long, 1, 96),
                CreateEntry(TiffTagId.RowsPerStrip, TiffDataType.Long, 1, 2)
            }
        };
        byte[] fileBytes = CreateClassicDirectoryFile(directories, 128);

        TiffValidationResult result = TiffValidator.Validate(fileBytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.MissingRequiredTag), Is.True);
    }

    [Test]
    public void Validate_ReportsImageDataRangeOutsideFile()
    {
        TiffIfdEntry[][] directories = new TiffIfdEntry[][]
        {
            new TiffIfdEntry[]
            {
                CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 2),
                CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 2),
                CreateEntry(TiffTagId.BitsPerSample, TiffDataType.Short, 1, 8),
                CreateEntry(TiffTagId.PhotometricInterpretation, TiffDataType.Short, 1, (ulong)TiffPhotometric.BlackIsZero),
                CreateEntry(TiffTagId.RowsPerStrip, TiffDataType.Long, 1, 2),
                CreateEntry(TiffTagId.StripOffsets, TiffDataType.Long, 1, 100),
                CreateEntry(TiffTagId.StripByteCounts, TiffDataType.Long, 1, 4)
            }
        };
        byte[] fileBytes = CreateClassicDirectoryFile(directories, 98);

        TiffValidationResult result = TiffValidator.Validate(fileBytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void Validate_ReportsOverlappingImageDataRanges()
    {
        byte[] fileBytes = CreateOverlappingStripFileBytes();

        TiffValidationResult result = TiffValidator.Validate(fileBytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void Validate_PathAndStreamOverloads()
    {
        string path = CreateTempPath();
        try
        {
            File.WriteAllBytes(path, CreateValidFileBytes());
            MemoryStream stream = new MemoryStream(File.ReadAllBytes(path));
            TiffReadOptions options = new TiffReadOptions();
            options.LeaveOpen = true;

            TiffValidationResult pathResult = TiffValidator.Validate(path);
            TiffValidationResult streamResult = TiffValidator.Validate(stream, options);

            Assert.That(pathResult.Success, Is.True);
            Assert.That(streamResult.Success, Is.True);
            Assert.That(stream.CanRead, Is.True);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Test]
    public void Validate_StreamClosesByDefaultAfterValidation()
    {
        MemoryStream stream = new MemoryStream(CreateValidFileBytes());

        TiffValidationResult result = TiffValidator.Validate(stream);

        Assert.That(result.Success, Is.True);
        Assert.That(stream.CanRead, Is.False);
    }

    static byte[] CreateValidFileBytes()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripValues(2, 2)));
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 1, 2, 3, 4 });
        writer.Flush();
        return writer.GetFileBytes();
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

    static TiffTagValue[] CreateStripValues(ulong width, ulong length)
    {
        return new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, width),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, length),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, length)
        };
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

    static string CreateTempPath()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".tif");
        DeleteIfExists(path);
        return path;
    }

    static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
