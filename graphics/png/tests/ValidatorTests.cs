using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ValidatorTests
{
    [Test]
    public void ValidateStrict_ReportsCrcMismatchAsFatalError()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytesWithCrc(PngConstants.ChunkIDAT, CreateValidIdatData(), 0),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngValidationResult result = PngValidator.ValidateStrict(png);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Warnings, Is.Empty);
        Assert.That(result.Errors, Has.Length.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_CHUNK_CRC_MISMATCH"));
        Assert.That(result.Errors[0].ChunkType, Is.EqualTo(PngConstants.ChunkIDAT));
        Assert.That(result.Errors[0].IsFatal, Is.True);
    }

    [Test]
    public void ValidateTolerantMetadata_DowngradesAncillaryCrcMismatchToWarning()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytesWithCrc(PngConstants.ChunkText, new byte[] { 65, 0, 66 }, 0),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngValidationResult result = PngValidator.ValidateTolerantMetadata(png);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Warnings, Has.Length.EqualTo(1));
        Assert.That(result.Warnings[0].Code, Is.EqualTo("PNG_CHUNK_CRC_MISMATCH"));
        Assert.That(result.Warnings[0].ChunkType, Is.EqualTo(PngConstants.ChunkText));
    }

    [Test]
    public void ValidateTolerantMetadata_KeepsCriticalCrcMismatchFatal()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytesWithCrc(PngConstants.ChunkIDAT, CreateValidIdatData(), 0),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngValidationResult result = PngValidator.ValidateTolerantMetadata(png);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Warnings, Is.Empty);
        Assert.That(result.Errors, Has.Length.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_CHUNK_CRC_MISMATCH"));
        Assert.That(result.Errors[0].ChunkType, Is.EqualTo(PngConstants.ChunkIDAT));
    }

    [Test]
    public void ValidateTolerantMetadata_DowngradesAncillaryOrderingAndDuplicateErrors()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytes(PngConstants.ChunkGamma, new byte[] { 0, 0, 0, 1 }),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkPhysicalPixels, new byte[] { 0, 0, 0, 1, 0, 0, 0, 1, 1 }),
            CreateChunkBytes(PngConstants.ChunkGamma, new byte[] { 0, 0, 0, 2 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngValidationResult result = PngValidator.ValidateTolerantMetadata(png);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
        Assert.That(HasWarning(result, "PNG_ANCILLARY_AFTER_IDAT", PngConstants.ChunkPhysicalPixels), Is.True);
        Assert.That(HasWarning(result, "PNG_ANCILLARY_AFTER_IDAT", PngConstants.ChunkGamma), Is.True);
        Assert.That(HasWarning(result, "PNG_CHUNK_DUPLICATE", PngConstants.ChunkGamma), Is.True);
    }

    [Test]
    public void ValidateTolerantMetadata_KeepsStructuralOrderErrorsFatal()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkPLTE, new byte[] { 1, 2, 3 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngValidationResult result = PngValidator.ValidateTolerantMetadata(png);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Warnings, Is.Empty);
        Assert.That(HasError(result, "PNG_PLTE_AFTER_IDAT", PngConstants.ChunkPLTE), Is.True);
    }

    [Test]
    public void ValidateTolerantMetadata_StreamUsesTolerantMetadataPolicy()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytesWithCrc(PngConstants.ChunkText, new byte[] { 65, 0, 66 }, 0),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        MemoryStream stream = new MemoryStream(png);

        PngValidationResult result = PngValidator.ValidateTolerantMetadata(stream);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Warnings[0].Code, Is.EqualTo("PNG_CHUNK_CRC_MISMATCH"));
    }

    [Test]
    public void InspectBestEffort_DowngradesCriticalCrcAndStructuralErrorsToWarnings()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytesWithCrc(PngConstants.ChunkIDAT, CreateValidIdatData(), 0),
            CreateChunkBytes(PngConstants.ChunkPLTE, new byte[] { 1, 2, 3 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngValidationResult result = PngValidator.InspectBestEffort(png);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
        Assert.That(HasWarning(result, "PNG_CHUNK_CRC_MISMATCH", PngConstants.ChunkIDAT), Is.True);
        Assert.That(HasWarning(result, "PNG_PLTE_AFTER_IDAT", PngConstants.ChunkPLTE), Is.True);
    }

    [Test]
    public void InspectBestEffort_DowngradesDuplicateChunksToWarnings()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytes(PngConstants.ChunkPLTE, new byte[] { 1, 2, 3 }),
            CreateChunkBytes(PngConstants.ChunkPLTE, new byte[] { 4, 5, 6 }),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngValidationResult result = PngValidator.InspectBestEffort(png);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
        Assert.That(HasWarning(result, "PNG_CHUNK_DUPLICATE", PngConstants.ChunkPLTE), Is.True);
    }

    [Test]
    public void InspectBestEffort_StreamUsesBestEffortPolicy()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytesWithCrc(PngConstants.ChunkIDAT, CreateValidIdatData(), 0),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        MemoryStream stream = new MemoryStream(png);

        PngValidationResult result = PngValidator.InspectBestEffort(stream);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
        Assert.That(HasWarning(result, "PNG_CHUNK_CRC_MISMATCH", PngConstants.ChunkIDAT), Is.True);
    }

    [Test]
    public void InspectBestEffort_InvalidSignatureReturnsWarningNotError()
    {
        byte[] data = new byte[] { 1, 2, 3 };

        PngValidationResult result = PngValidator.InspectBestEffort(data);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Warnings, Has.Length.EqualTo(1));
        Assert.That(result.Warnings[0].Code, Is.EqualTo("PNG_SIGNATURE_TOO_SHORT"));
    }

    [Test]
    public void ValidateTolerantRepair_RepairsBadAncillaryOrderingForValidation()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkPhysicalPixels, new byte[] { 0, 0, 0, 1, 0, 0, 0, 1, 1 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngValidationResult result = PngValidator.ValidateTolerantRepair(png);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void ValidateTolerantRepair_KeepsUnrepairedStructuralOrderErrorsFatal()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkPLTE, new byte[] { 1, 2, 3 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngValidationResult result = PngValidator.ValidateTolerantRepair(png);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasError(result, "PNG_PLTE_AFTER_IDAT", PngConstants.ChunkPLTE), Is.True);
    }

    [Test]
    public void ValidateTolerantRepair_StreamUsesRepairPolicy()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkPhysicalPixels, new byte[] { 0, 0, 0, 1, 0, 0, 0, 1, 1 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        MemoryStream stream = new MemoryStream(png);

        PngValidationResult result = PngValidator.ValidateTolerantRepair(stream);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void ValidateTolerantRepair_StillReportsCrcMismatch()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytesWithCrc(PngConstants.ChunkIDAT, CreateValidIdatData(), 0),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngValidationResult result = PngValidator.ValidateTolerantRepair(png);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasError(result, "PNG_CHUNK_CRC_MISMATCH", PngConstants.ChunkIDAT), Is.True);
    }

    [Test]
    public void Validate_StrictModeHonorsDisabledCrcValidation()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytesWithCrc(PngConstants.ChunkIDAT, CreateValidIdatData(), 0),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        PngDecoderOptions options = new PngDecoderOptions();
        options.ErrorPolicy = PngErrorPolicy.Strict;
        options.ValidateCrc = false;

        PngValidationResult result = PngValidator.Validate(png, 0, options);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void ValidateStrict_StreamUsesStrictCrcValidation()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytesWithCrc(PngConstants.ChunkIDAT, CreateValidIdatData(), 0),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        MemoryStream stream = new MemoryStream(png);

        PngValidationResult result = PngValidator.ValidateStrict(stream);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_CHUNK_CRC_MISMATCH"));
    }

    [Test]
    public void ValidateStrict_ReportsSignatureMismatchAsFatalError()
    {
        byte[] data = new byte[] { 1, 2, 3 };

        PngValidationResult result = PngValidator.ValidateStrict(data);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Warnings, Is.Empty);
        Assert.That(result.Errors, Has.Length.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_SIGNATURE_TOO_SHORT"));
        Assert.That(result.Errors[0].IsFatal, Is.True);
    }

    [Test]
    public void Validate_RejectsInvalidInputs()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytes(PngConstants.ChunkIDAT, new byte[] { 1 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        PngDecoderOptions nullLimits = new PngDecoderOptions();
        nullLimits.Limits = null!;

        Assert.Throws<ArgumentNullException>(() => PngValidator.ValidateStrict((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngValidator.ValidateStrict(null!, 0));
        Assert.Throws<ArgumentNullException>(() => PngValidator.ValidateTolerantMetadata((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngValidator.ValidateTolerantMetadata(null!, 0));
        Assert.Throws<ArgumentNullException>(() => PngValidator.InspectBestEffort((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngValidator.InspectBestEffort(null!, 0));
        Assert.Throws<ArgumentNullException>(() => PngValidator.ValidateTolerantRepair((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngValidator.ValidateTolerantRepair(null!, 0));
        Assert.Throws<ArgumentNullException>(() => PngValidator.Validate((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngValidator.Validate(png, 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngValidator.Validate(png, 0, nullLimits));
        Assert.Throws<ArgumentNullException>(() => PngValidator.ValidateStrict((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => PngValidator.ValidateTolerantMetadata((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => PngValidator.InspectBestEffort((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => PngValidator.ValidateTolerantRepair((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => PngValidator.Validate((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => PngValidator.Validate(new MemoryStream(png), null!));
        Assert.Throws<ArgumentNullException>(() => PngValidator.Validate(new MemoryStream(png), nullLimits));
    }

    static byte[] CreateIhdrData(int width, int height)
    {
        byte[] data = new byte[13];
        PngBinaryWriter.WriteUInt32(data, 0, (uint)width);
        PngBinaryWriter.WriteUInt32(data, 4, (uint)height);
        data[8] = PngConstants.BitDepth8;
        data[9] = PngConstants.ColorTypeTruecolorAlpha;
        data[10] = PngConstants.CompressionMethodDeflate;
        data[11] = PngConstants.FilterMethodAdaptive;
        data[12] = PngConstants.InterlaceMethodNone;
        return data;
    }

    static byte[] CreateValidIdatData()
    {
        return PngZlibCompressor.Compress(new byte[] { 0, 1, 2, 3, 4 });
    }

    static byte[] CreateChunkBytes(string chunkType, byte[] chunkData)
    {
        return CreateChunkBytesWithCrc(chunkType, chunkData, PngCrc32.CalculateChunkCrc(chunkType, chunkData));
    }

    static byte[] CreateChunkBytesWithCrc(string chunkType, byte[] chunkData, uint crc)
    {
        byte[] bytes = new byte[chunkData.Length + 12];
        PngBinaryWriter.WriteUInt32(bytes, 0, (uint)chunkData.Length);
        bytes[4] = (byte)chunkType[0];
        bytes[5] = (byte)chunkType[1];
        bytes[6] = (byte)chunkType[2];
        bytes[7] = (byte)chunkType[3];
        int index = 0;
        while (index < chunkData.Length)
        {
            bytes[8 + index] = chunkData[index];
            index += 1;
        }

        PngBinaryWriter.WriteUInt32(bytes, 8 + chunkData.Length, crc);
        return bytes;
    }

    static byte[] CreatePng(params byte[][] chunks)
    {
        int length = PngConstants.SignatureLength;
        int index = 0;
        while (index < chunks.Length)
        {
            length += chunks[index].Length;
            index += 1;
        }

        byte[] data = new byte[length];
        data[0] = PngConstants.SignatureByte0;
        data[1] = PngConstants.SignatureByte1;
        data[2] = PngConstants.SignatureByte2;
        data[3] = PngConstants.SignatureByte3;
        data[4] = PngConstants.SignatureByte4;
        data[5] = PngConstants.SignatureByte5;
        data[6] = PngConstants.SignatureByte6;
        data[7] = PngConstants.SignatureByte7;

        int offset = PngConstants.SignatureLength;
        index = 0;
        while (index < chunks.Length)
        {
            int chunkIndex = 0;
            while (chunkIndex < chunks[index].Length)
            {
                data[offset + chunkIndex] = chunks[index][chunkIndex];
                chunkIndex += 1;
            }

            offset += chunks[index].Length;
            index += 1;
        }

        return data;
    }

    static bool HasWarning(PngValidationResult result, string code, string chunkType)
    {
        int index = 0;
        while (index < result.Warnings.Length)
        {
            if (result.Warnings[index].Code == code && result.Warnings[index].ChunkType == chunkType)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static bool HasError(PngValidationResult result, string code, string chunkType)
    {
        int index = 0;
        while (index < result.Errors.Length)
        {
            if (result.Errors[index].Code == code && result.Errors[index].ChunkType == chunkType)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }
}
