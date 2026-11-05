using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PngValidatorCoverageTests
{
    [Test]
    public void ValidateStrict_ReportsTruncatedChunkFramingWithoutThrowing()
    {
        byte[] png = Join(CreateSignature(), new byte[] { 0, 0, 0, 13, 73, 72, 68, 82, 0 });

        PngValidationResult result = PngValidator.ValidateStrict(png);

        Assert.That(HasError(result, "PNG_CHUNK_FRAMING_TRUNCATED"), Is.True);
    }

    [Test]
    public void ValidateStrict_ReportsReservedChunkTypeBit()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(PngConstants.ColorTypeTruecolorAlpha, PngConstants.BitDepth8)),
            CreateChunkBytes("abcD", new byte[0]),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateRgbaIdat(new byte[] { 0, 1, 2, 3, 4 })),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngValidationResult result = PngValidator.ValidateStrict(png);

        Assert.That(HasError(result, "PNG_CHUNK_TYPE_RESERVED_BIT"), Is.True);
    }

    [Test]
    public void ValidatePolicies_ClassifyMalformedAncillaryPayload()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(PngConstants.ColorTypeTruecolorAlpha, PngConstants.BitDepth8)),
            CreateChunkBytes(PngConstants.ChunkPhysicalPixels, new byte[] { 1 }),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateRgbaIdat(new byte[] { 0, 1, 2, 3, 4 })),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngValidationResult strict = PngValidator.ValidateStrict(png);
        PngValidationResult tolerant = PngValidator.ValidateTolerantMetadata(png);
        PngValidationResult repair = PngValidator.ValidateTolerantRepair(png);
        PngValidationResult inspection = PngValidator.InspectBestEffort(png);

        Assert.That(HasError(strict, "PNG_PHYS_LENGTH_INVALID"), Is.True);
        Assert.That(HasWarning(tolerant, "PNG_PHYS_LENGTH_INVALID"), Is.True);
        Assert.That(tolerant.IsValid, Is.True);
        Assert.That(HasError(repair, "PNG_PHYS_LENGTH_INVALID"), Is.True);
        Assert.That(HasWarning(inspection, "PNG_PHYS_LENGTH_INVALID"), Is.True);
        Assert.That(inspection.IsValid, Is.True);
    }

    [Test]
    public void ValidateStrict_ReportsZlibScanlineAndFilterFailures()
    {
        byte[] invalidZlib = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(PngConstants.ColorTypeTruecolorAlpha, PngConstants.BitDepth8)),
            CreateChunkBytes(PngConstants.ChunkIDAT, new byte[] { 1, 2, 3 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        byte[] wrongSize = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(PngConstants.ColorTypeTruecolorAlpha, PngConstants.BitDepth8)),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateRgbaIdat(new byte[] { 0, 1 })),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        byte[] badFilter = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(PngConstants.ColorTypeTruecolorAlpha, PngConstants.BitDepth8)),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateRgbaIdat(new byte[] { 5, 1, 2, 3, 4 })),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        byte[] trailingZlibData = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(PngConstants.ColorTypeTruecolorAlpha, PngConstants.BitDepth8)),
            CreateChunkBytes(PngConstants.ChunkIDAT, Join(CreateRgbaIdat(new byte[] { 0, 1, 2, 3, 4 }), new byte[] { 0 })),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        Assert.That(HasError(PngValidator.ValidateStrict(invalidZlib), "PNG_IDAT_ZLIB_INVALID"), Is.True);
        Assert.That(HasError(PngValidator.ValidateStrict(wrongSize), "PNG_SCANLINE_DECOMPRESSED_BYTE_COUNT_MISMATCH"), Is.True);
        Assert.That(HasError(PngValidator.ValidateStrict(badFilter), "PNG_SCANLINE_FILTER_TYPE_INVALID"), Is.True);
        Assert.That(HasError(PngValidator.ValidateStrict(trailingZlibData), "PNG_IDAT_ZLIB_INVALID"), Is.True);
    }

    [Test]
    public void ValidateStrict_ReportsPaletteIndexOutsidePlte()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(PngConstants.ColorTypeIndexedColor, PngConstants.BitDepth8)),
            CreateChunkBytes(PngConstants.ChunkPLTE, new byte[] { 10, 20, 30 }),
            CreateChunkBytes(PngConstants.ChunkIDAT, PngZlibCompressor.Compress(new byte[] { 0, 1 })),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngValidationResult result = PngValidator.ValidateStrict(png);

        Assert.That(HasError(result, "PNG_PALETTE_INDEX_OUT_OF_RANGE"), Is.True);
    }

    [Test]
    public void ValidateStrict_ReportsColorTextAndApngPayloadRules()
    {
        byte[] malformedText = new byte[] { 75, 0, 0, 0, 0, 0, 255 };
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(PngConstants.ColorTypeTruecolorAlpha, PngConstants.BitDepth8)),
            CreateChunkBytes(PngConstants.ChunkStandardRgb, new byte[] { 0 }),
            CreateChunkBytes(PngConstants.ChunkIccProfile, CreateIccProfileData()),
            CreateChunkBytes(PngConstants.ChunkInternationalText, malformedText),
            CreateChunkBytes(PngConstants.ChunkFrameData, new byte[] { 0, 0, 0, 0 }),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateRgbaIdat(new byte[] { 0, 1, 2, 3, 4 })),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngValidationResult result = PngValidator.ValidateStrict(png);

        Assert.That(HasError(result, "PNG_COLOR_METADATA_CONFLICT"), Is.True);
        Assert.That(HasError(result, "PNG_ITXT_TEXT_ENCODING_INVALID"), Is.True);
        Assert.That(HasError(result, "PNG_APNG_CONTROL_MISSING"), Is.True);
    }

    [Test]
    public void ValidatePolicies_ReportOrIgnoreTrailingDataAsConfigured()
    {
        byte[] png = Join(CreateValidPng(), new byte[] { 9, 8, 7 });
        PngDecoderOptions ignored = new PngDecoderOptions();
        ignored.IgnoreTrailingBytes = true;

        PngValidationResult strict = PngValidator.ValidateStrict(png);
        PngValidationResult tolerant = PngValidator.ValidateTolerantMetadata(png);
        PngValidationResult repair = PngValidator.ValidateTolerantRepair(png);
        PngValidationResult inspection = PngValidator.InspectBestEffort(png);
        PngValidationResult ignoredResult = PngValidator.Validate(png, 0, ignored);

        Assert.That(HasError(strict, "PNG_TRAILING_DATA"), Is.True);
        Assert.That(HasError(tolerant, "PNG_TRAILING_DATA"), Is.True);
        Assert.That(HasError(repair, "PNG_TRAILING_DATA"), Is.True);
        Assert.That(HasWarning(inspection, "PNG_TRAILING_DATA"), Is.True);
        Assert.That(ignoredResult.IsValid, Is.True);
    }

    static byte[] CreateValidPng()
    {
        return CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(PngConstants.ColorTypeTruecolorAlpha, PngConstants.BitDepth8)),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateRgbaIdat(new byte[] { 0, 1, 2, 3, 4 })),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
    }

    static byte[] CreateIccProfileData()
    {
        byte[] compressed = PngZlibCompressor.Compress(new byte[] { 1, 2, 3, 4 });
        return Join(new byte[] { 80, 0, 0 }, compressed);
    }

    static byte[] CreateRgbaIdat(byte[] scanlines)
    {
        return PngZlibCompressor.Compress(scanlines);
    }

    static byte[] CreateIhdrData(byte colorType, byte bitDepth)
    {
        byte[] data = new byte[13];
        PngBinaryWriter.WriteUInt32(data, 0, 1);
        PngBinaryWriter.WriteUInt32(data, 4, 1);
        data[8] = bitDepth;
        data[9] = colorType;
        data[10] = PngConstants.CompressionMethodDeflate;
        data[11] = PngConstants.FilterMethodAdaptive;
        data[12] = PngConstants.InterlaceMethodNone;
        return data;
    }

    static byte[] CreateChunkBytes(string chunkType, byte[] chunkData)
    {
        byte[] result = new byte[chunkData.Length + 12];
        PngBinaryWriter.WriteUInt32(result, 0, (uint)chunkData.Length);
        result[4] = (byte)chunkType[0];
        result[5] = (byte)chunkType[1];
        result[6] = (byte)chunkType[2];
        result[7] = (byte)chunkType[3];
        int index = 0;
        while (index < chunkData.Length)
        {
            result[8 + index] = chunkData[index];
            index += 1;
        }

        PngBinaryWriter.WriteUInt32(result, 8 + chunkData.Length, PngCrc32.CalculateChunkCrc(chunkType, chunkData));
        return result;
    }

    static byte[] CreatePng(params byte[][] chunks)
    {
        byte[][] arrays = new byte[chunks.Length + 1][];
        arrays[0] = CreateSignature();
        int index = 0;
        while (index < chunks.Length)
        {
            arrays[index + 1] = chunks[index];
            index += 1;
        }

        return Join(arrays);
    }

    static byte[] CreateSignature()
    {
        return new byte[]
        {
            PngConstants.SignatureByte0,
            PngConstants.SignatureByte1,
            PngConstants.SignatureByte2,
            PngConstants.SignatureByte3,
            PngConstants.SignatureByte4,
            PngConstants.SignatureByte5,
            PngConstants.SignatureByte6,
            PngConstants.SignatureByte7
        };
    }

    static byte[] Join(params byte[][] arrays)
    {
        int length = 0;
        int index = 0;
        while (index < arrays.Length)
        {
            length += arrays[index].Length;
            index += 1;
        }

        byte[] result = new byte[length];
        int offset = 0;
        index = 0;
        while (index < arrays.Length)
        {
            arrays[index].CopyTo(result, offset);
            offset += arrays[index].Length;
            index += 1;
        }

        return result;
    }

    static bool HasError(PngValidationResult result, string code)
    {
        int index = 0;
        while (index < result.Errors.Length)
        {
            if (result.Errors[index].Code == code)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static bool HasWarning(PngValidationResult result, string code)
    {
        int index = 0;
        while (index < result.Warnings.Length)
        {
            if (result.Warnings[index].Code == code)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }
}
