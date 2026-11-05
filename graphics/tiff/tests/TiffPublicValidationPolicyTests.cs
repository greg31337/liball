using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffPublicValidationPolicyTests
{
    [Test]
    public void UnsupportedCompression_IsErrorInStrictModeAndWarningInPermissiveMode()
    {
        byte[] bytes = CreateWriterStripFile(TiffCompression.Jpeg, TiffPhotometric.BlackIsZero, 1, 2, 2, new byte[] { 1, 2, 3, 4 });
        TiffReadOptions strictOptions = new TiffReadOptions();
        TiffReadOptions permissiveOptions = new TiffReadOptions();
        permissiveOptions.Strict = false;
        ITiffValidationService service = new TiffValidationService();

        TiffValidationResult strict = service.ValidateBytes(bytes, strictOptions);
        TiffValidationResult permissive = service.ValidateBytes(bytes, permissiveOptions);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Success, Is.False);
            Assert.That(strict.Mode, Is.EqualTo(TiffValidationMode.Strict));
            Assert.That(strict.ErrorCount, Is.EqualTo(1));
            Assert.That(strict.WarningCount, Is.EqualTo(0));
            Assert.That(strict.Diagnostics.Get(0).Code, Is.EqualTo(TiffErrorCode.UnsupportedCompression));
            Assert.That(strict.Diagnostics.Get(0).ImageIndex, Is.EqualTo(0));
            Assert.That(strict.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.Compression));
            Assert.That(strict.Diagnostics.Get(0).Offset, Is.GreaterThanOrEqualTo(8));
            Assert.That(permissive.Success, Is.True);
            Assert.That(permissive.Mode, Is.EqualTo(TiffValidationMode.Permissive));
            Assert.That(permissive.ErrorCount, Is.EqualTo(0));
            Assert.That(permissive.WarningCount, Is.EqualTo(1));
            Assert.That(permissive.Diagnostics.Get(0).Severity, Is.EqualTo(TiffDiagnosticSeverity.Warning));
            Assert.That(permissive.Diagnostics.Get(0).Code, Is.EqualTo(TiffErrorCode.UnsupportedCompression));
        });
    }

    [Test]
    public void DuplicateTags_AreLocatedAndFollowValidationMode()
    {
        byte[] bytes = CreateDuplicateWidthFile();
        TiffReadOptions strictOptions = new TiffReadOptions();
        TiffReadOptions permissiveOptions = new TiffReadOptions();
        permissiveOptions.Strict = false;
        ITiffValidationService service = new TiffValidationService();

        TiffValidationResult strict = service.ValidateBytes(bytes, strictOptions);
        TiffValidationResult permissive = service.ValidateBytes(bytes, permissiveOptions);
        TiffDiagnostic strictDuplicate = FindDiagnostic(strict.Diagnostics, TiffErrorCode.DuplicateTag);
        TiffDiagnostic permissiveDuplicate = FindDiagnostic(permissive.Diagnostics, TiffErrorCode.DuplicateTag);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Success, Is.False);
            Assert.That(strictDuplicate.Severity, Is.EqualTo(TiffDiagnosticSeverity.Error));
            Assert.That(strictDuplicate.Offset, Is.EqualTo(8));
            Assert.That(strictDuplicate.ImageIndex, Is.EqualTo(0));
            Assert.That(strictDuplicate.TagId, Is.EqualTo(TiffTagId.ImageWidth));
            Assert.That(permissive.Success, Is.True);
            Assert.That(permissiveDuplicate.Severity, Is.EqualTo(TiffDiagnosticSeverity.Warning));
            Assert.That(permissiveDuplicate.Offset, Is.EqualTo(8));
            Assert.That(permissiveDuplicate.ImageIndex, Is.EqualTo(0));
            Assert.That(permissiveDuplicate.TagId, Is.EqualTo(TiffTagId.ImageWidth));
        });
    }

    [Test]
    public void PhotometricSupportAndShape_ReportDistinctLocatedCodes()
    {
        byte[] unsupportedBytes = CreateWriterStripFile(TiffCompression.None, (TiffPhotometric)99, 1, 2, 2, new byte[] { 1, 2, 3, 4 });
        byte[] mismatchBytes = CreateWriterStripFile(TiffCompression.None, TiffPhotometric.Rgb, 1, 2, 2, new byte[] { 1, 2, 3, 4 });
        ITiffValidationService service = new TiffValidationService();

        TiffValidationResult unsupported = service.ValidateBytes(unsupportedBytes, new TiffReadOptions());
        TiffValidationResult mismatch = service.ValidateBytes(mismatchBytes, new TiffReadOptions());
        TiffDiagnostic unsupportedDiagnostic = FindDiagnostic(unsupported.Diagnostics, TiffErrorCode.UnsupportedPhotometric);
        TiffDiagnostic mismatchDiagnostic = FindDiagnostic(mismatch.Diagnostics, TiffErrorCode.PhotometricMismatch);

        Assert.Multiple(() =>
        {
            Assert.That(unsupported.Success, Is.False);
            Assert.That(unsupportedDiagnostic.ImageIndex, Is.EqualTo(0));
            Assert.That(unsupportedDiagnostic.TagId, Is.EqualTo(TiffTagId.PhotometricInterpretation));
            Assert.That(unsupportedDiagnostic.Offset, Is.GreaterThanOrEqualTo(8));
            Assert.That(mismatch.Success, Is.False);
            Assert.That(mismatchDiagnostic.ImageIndex, Is.EqualTo(0));
            Assert.That(mismatchDiagnostic.TagId, Is.EqualTo(TiffTagId.SamplesPerPixel));
            Assert.That(mismatchDiagnostic.Offset, Is.GreaterThanOrEqualTo(8));
        });

        TiffReadOptions permissiveOptions = new TiffReadOptions();
        permissiveOptions.Strict = false;
        TiffValidationResult permissiveMismatch = service.ValidateBytes(mismatchBytes, permissiveOptions);
        Assert.That(permissiveMismatch.Success, Is.True);
        Assert.That(FindDiagnostic(permissiveMismatch.Diagnostics, TiffErrorCode.PhotometricMismatch).Severity, Is.EqualTo(TiffDiagnosticSeverity.Warning));
    }

    [Test]
    public void OverlappingSegments_UseDedicatedCodeAndStructuredLocation()
    {
        TiffValidationResult result = new TiffValidationService().ValidateBytes(CreateOverlappingStripFile(), new TiffReadOptions());
        TiffDiagnostic diagnostic = FindDiagnostic(result.Diagnostics, TiffErrorCode.OverlappingDataBlock);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(diagnostic.Offset, Is.EqualTo(202));
            Assert.That(diagnostic.ImageIndex, Is.EqualTo(0));
            Assert.That(diagnostic.TagId, Is.EqualTo(TiffTagId.StripOffsets));
            Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
        });
    }

    [Test]
    public void CodecExpansionLimit_IsValidatedAtTheEncodedSegmentLocation()
    {
        byte[] bytes = CreateWriterStripFile(TiffCompression.PackBits, TiffPhotometric.BlackIsZero, 1, 100, 1, new byte[] { 157, 0 });
        TiffReadOptions options = new TiffReadOptions();
        options.Limits.MaximumCodecExpansionRatio = 10;

        TiffValidationResult result = new TiffValidationService().ValidateBytes(bytes, options);
        TiffDiagnostic diagnostic = FindDiagnostic(result.Diagnostics, TiffErrorCode.CodecExpansionLimitExceeded);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(diagnostic.Offset, Is.GreaterThan(8));
            Assert.That(diagnostic.ImageIndex, Is.EqualTo(0));
            Assert.That(diagnostic.TagId, Is.EqualTo(TiffTagId.StripOffsets));
        });
    }

    [Test]
    public void MalformedHeaderAndEntryType_ExposeByteLocations()
    {
        byte[] invalidVersion = new byte[] { 73, 73, 99, 0, 8, 0, 0, 0 };
        TiffValidationResult headerResult = new TiffValidationService().ValidateBytes(invalidVersion, new TiffReadOptions());
        TiffDiagnostic headerDiagnostic = FindDiagnostic(headerResult.Diagnostics, TiffErrorCode.InvalidTiffVersion);
        byte[] invalidType = CreateUnknownEntryTypeFile();
        TiffValidationResult entryResult = new TiffValidationService().ValidateBytes(invalidType, new TiffReadOptions());
        TiffDiagnostic entryDiagnostic = FindDiagnostic(entryResult.Diagnostics, TiffErrorCode.InvalidTagType);

        Assert.Multiple(() =>
        {
            Assert.That(headerDiagnostic.Offset, Is.EqualTo(2));
            Assert.That(headerDiagnostic.ImageIndex, Is.EqualTo(TiffDiagnostic.NoImageIndex));
            Assert.That(entryDiagnostic.Offset, Is.EqualTo(10));
            Assert.That(entryDiagnostic.ImageIndex, Is.EqualTo(0));
            Assert.That(entryDiagnostic.TagId, Is.EqualTo(TiffTagId.ImageWidth));
        });
    }

    [Test]
    public void CloudOptimizedResult_ExposesModeAndDiagnosticCounts()
    {
        byte[] bytes = CreateWriterStripFile(TiffCompression.None, TiffPhotometric.BlackIsZero, 1, 2, 2, new byte[] { 1, 2, 3, 4 });
        TiffReadOptions options = new TiffReadOptions();
        options.Strict = false;

        TiffCloudOptimizedGeoTiffValidationResult result = new TiffValidationService().ValidateCloudOptimizedBytes(bytes, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Mode, Is.EqualTo(TiffValidationMode.Permissive));
            Assert.That(result.ErrorCount, Is.EqualTo(result.Diagnostics.ErrorCount()));
            Assert.That(result.WarningCount, Is.EqualTo(result.Diagnostics.WarningCount()));
        });
    }

    static TiffDiagnostic FindDiagnostic(TiffDiagnostics diagnostics, TiffErrorCode code)
    {
        int index = 0;
        while (index < diagnostics.Count())
        {
            if (diagnostics.Get(index).Code == code)
            {
                return diagnostics.Get(index);
            }

            index = index + 1;
        }

        Assert.Fail("Expected diagnostic code " + code.ToString());
        return new TiffDiagnostic(TiffDiagnosticSeverity.Error, TiffErrorCode.UnknownError, "missing");
    }

    static byte[] CreateWriterStripFile(TiffCompression compression, TiffPhotometric photometric, int samplesPerPixel, ulong width, ulong length, byte[] rawBytes)
    {
        TiffTagValue[] values = new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, width),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, length),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.Compression, TiffDataType.Short, (ulong)compression),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)photometric),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.SamplesPerPixel, TiffDataType.Short, (ulong)samplesPerPixel),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, length)
        };
        TiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(values));
        writer.WriteRawStrip(imageIndex, 0, rawBytes);
        TiffDiagnostics flush = writer.Flush();
        Assert.That(flush.HasErrors(), Is.False);
        byte[] bytes = writer.GetFileBytes();
        writer.Close();
        return bytes;
    }

    static byte[] CreateDuplicateWidthFile()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 2),
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 2),
            CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 2),
            CreateEntry(TiffTagId.BitsPerSample, TiffDataType.Short, 1, 8),
            CreateEntry(TiffTagId.PhotometricInterpretation, TiffDataType.Short, 1, (ulong)TiffPhotometric.BlackIsZero),
            CreateEntry(TiffTagId.RowsPerStrip, TiffDataType.Long, 1, 2),
            CreateEntry(TiffTagId.StripOffsets, TiffDataType.Long, 1, 200),
            CreateEntry(TiffTagId.StripByteCounts, TiffDataType.Long, 1, 4)
        };
        return CreateClassicDirectoryFile(entries, 204);
    }

    static byte[] CreateOverlappingStripFile()
    {
        TiffTagValue[] values = new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, 4),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedArray(TiffTagId.StripOffsets, TiffDataType.Long, new ulong[] { 200, 202 }),
            TiffTagValue.CreateUnsignedArray(TiffTagId.StripByteCounts, TiffDataType.Long, new ulong[] { 4, 4 })
        };
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffIfdEntry[] entries = TiffExifMetadataWriter.CreateEntries(values);
        byte[][] valueBytes = TiffExifMetadataWriter.EncodeValues(values, TiffByteOrder.LittleEndian, diagnostics);
        int layoutByteCount = TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, valueBytes);
        byte[] bytes = new byte[210];
        TiffHeader.WriteClassic(bytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        TiffDirectoryWriter.WriteClassicWithValueBytes(bytes.AsSpan(8, layoutByteCount), entries, valueBytes, TiffByteOrder.LittleEndian, 8, 0);
        return bytes;
    }

    static byte[] CreateUnknownEntryTypeFile()
    {
        byte[] bytes = new byte[26];
        TiffHeader.WriteClassic(bytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(8, 2), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(10, 2), (ushort)TiffTagId.ImageWidth, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(12, 2), 99, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(14, 4), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(18, 4), 2, TiffByteOrder.LittleEndian);
        return bytes;
    }

    static byte[] CreateClassicDirectoryFile(TiffIfdEntry[] entries, int byteCount)
    {
        byte[] bytes = new byte[byteCount];
        TiffHeader.WriteClassic(bytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        TiffDirectoryWriter.WriteClassicChain(bytes, new TiffIfdEntry[][] { entries }, TiffByteOrder.LittleEndian, 8);
        return bytes;
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
