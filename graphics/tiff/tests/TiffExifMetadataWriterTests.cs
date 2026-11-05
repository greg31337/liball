using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffExifMetadataWriterTests
{
    const int ExposureTime = 33434;
    const int IsoSpeedRatings = 34855;
    const int ExifVersion = 36864;
    const int DateTimeOriginal = 36867;
    const int BrightnessValue = 37379;
    const int PixelXDimension = 40962;

    [Test]
    public void WriteClassicExif_WritesParentPointerAndRoundTripsTypedValues()
    {
        TiffIfdEntry[] parentEntries = new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 100) };
        TiffTagValue[] exifValues = CreateExifValues();
        int byteCount = TiffExifMetadataWriter.CalculateClassicWriteByteCount(parentEntries, exifValues);
        byte[] fileBytes = new byte[8 + byteCount];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffExifWriteResult writeResult = TiffCodec.WriteClassicExif(fileBytes, parentEntries, exifValues, TiffByteOrder.LittleEndian, 8, 0);
        TiffHeaderReadResult header = TiffHeader.ReadClassic(fileBytes);
        TiffDirectoryReadResult directories = TiffDirectoryReader.Read(header.Header, fileBytes);
        TiffExifReadResult readResult = TiffCodec.ReadExif(directories.Directories[0], fileBytes, header.Header);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(writeResult.ExifOffset, Is.GreaterThan(8UL));
        Assert.That(directories.Success, Is.True);
        Assert.That(directories.Directories[0].FindEntry(TiffTagId.ExifIfd)!.ValueOffset, Is.EqualTo(writeResult.ExifOffset));
        Assert.That(readResult.Success, Is.True);
        Assert.That(readResult.Metadata.HasExif, Is.True);
        Assert.That(readResult.Metadata.GetAscii(DateTimeOriginal), Is.EqualTo("2026:08:06 12:34:56"));
        Assert.That(readResult.Metadata.GetUnsignedScalar(IsoSpeedRatings), Is.EqualTo(200));
        Assert.That(readResult.Metadata.GetUnsignedScalar(PixelXDimension), Is.EqualTo(4000));
        Assert.That(readResult.Metadata.GetRationalScalar(ExposureTime).Numerator, Is.EqualTo(1));
        Assert.That(readResult.Metadata.GetRationalScalar(ExposureTime).Denominator, Is.EqualTo(125));
        Assert.That(readResult.Metadata.FindValue(ExifVersion)!.RawBytes, Is.EqualTo(new byte[] { 48, 50, 51, 49 }));
        Assert.That(readResult.Metadata.FindValue(BrightnessValue)!.SignedValues, Is.EqualTo(new long[] { -2 }));
    }

    [Test]
    public void WriteClassicExif_ReplacesExistingParentExifPointer()
    {
        TiffIfdEntry[] parentEntries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ExifIfd, TiffDataType.Long, 1, 999),
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 100)
        };
        TiffTagValue[] exifValues = new TiffTagValue[] { TiffTagValue.CreateAscii(DateTimeOriginal, "2026:08:06 12:34:56") };
        byte[] fileBytes = new byte[8 + TiffExifMetadataWriter.CalculateClassicWriteByteCount(parentEntries, exifValues)];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffExifWriteResult writeResult = TiffCodec.WriteClassicExif(fileBytes, parentEntries, exifValues, TiffByteOrder.LittleEndian, 8, 0);
        TiffDirectory parent = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8).Directories[0];

        Assert.That(writeResult.Success, Is.True);
        Assert.That(parent.EntryCount(), Is.EqualTo(2));
        Assert.That(parent.FindEntry(TiffTagId.ExifIfd)!.ValueOffset, Is.EqualTo(writeResult.ExifOffset));
    }

    [Test]
    public void WriteClassicExif_ReportsShortDestination()
    {
        TiffIfdEntry[] parentEntries = new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 100) };
        TiffTagValue[] exifValues = CreateExifValues();
        byte[] fileBytes = new byte[16];

        TiffExifWriteResult result = TiffCodec.WriteClassicExif(fileBytes, parentEntries, exifValues, TiffByteOrder.LittleEndian, 8, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void EncodeValue_ReportsRawByteCountMismatchThroughDirectoryValidation()
    {
        TiffIfdEntry[] parentEntries = new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 100) };
        TiffTagValue raw = TiffTagValue.CreateRaw(ExifVersion, TiffDataType.Undefined, 4, new byte[] { 1, 2, 3 });
        TiffTagValue[] exifValues = new TiffTagValue[] { raw };
        byte[] fileBytes = new byte[128];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffExifWriteResult result = TiffCodec.WriteClassicExif(fileBytes, parentEntries, exifValues, TiffByteOrder.LittleEndian, 8, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void SignedByteValues_RoundTripAcrossHighBitBytePatterns()
    {
        long[] expected = new long[] { -128, -2, -1, 0, 127 };
        byte[] bytes = TiffExifMetadataWriter.EncodeSignedValues(expected, TiffDataType.SByte, TiffByteOrder.LittleEndian);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        long[] actual = TiffExifMetadataReader.DecodeSignedValues(bytes, TiffDataType.SByte, expected.Length, TiffByteOrder.LittleEndian, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(bytes, Is.EqualTo(new byte[] { 128, 254, 255, 0, 127 }));
        Assert.That(actual, Is.EqualTo(expected));
    }

    static TiffTagValue[] CreateExifValues()
    {
        return new TiffTagValue[]
        {
            TiffTagValue.CreateRationalScalar(ExposureTime, TiffDataType.Rational, 1, 125),
            TiffTagValue.CreateUnsignedScalar(IsoSpeedRatings, TiffDataType.Short, 200),
            TiffTagValue.CreateRaw(ExifVersion, TiffDataType.Undefined, 4, new byte[] { 48, 50, 51, 49 }),
            TiffTagValue.CreateAscii(DateTimeOriginal, "2026:08:06 12:34:56"),
            TiffTagValue.CreateSignedScalar(BrightnessValue, TiffDataType.SLong, -2),
            TiffTagValue.CreateUnsignedScalar(PixelXDimension, TiffDataType.Long, 4000)
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
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(dataType, count);
        entry.ValueSlotByteCount = 4;
        return entry;
    }
}
