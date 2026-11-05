using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PngTopLevelValidateTests
{
    [Test]
    public void ValidateBytes_ReturnsValidResultForPng()
    {
        byte[] encoded = Png.EncodeToBytes(CreateImage(new byte[] { 1, 2, 3, 4 }));

        PngValidationResult result = Png.ValidateBytes(encoded);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Info.Width, Is.EqualTo(1));
        Assert.That(result.Info.Height, Is.EqualTo(1));
        Assert.That(result.Info.PixelFormat, Is.EqualTo(PngPixelFormat.Rgba8));
    }

    [Test]
    public void ValidateBytes_UsesOffsetAndOptions()
    {
        byte[] encoded = Png.EncodeToBytes(CreateImage(new byte[] { 5, 6, 7, 8 }));
        byte[] wrapped = Wrap(encoded);
        PngDecoderOptions options = new PngDecoderOptions();

        PngValidationResult result = Png.ValidateBytes(wrapped, 2, options);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Info.Width, Is.EqualTo(1));
    }

    [Test]
    public void ValidateBytes_ReturnsSignatureErrorsForNonPngData()
    {
        PngValidationResult result = Png.ValidateBytes(new byte[] { 1, 2, 3 });

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_SIGNATURE_TOO_SHORT"));
    }

    [Test]
    public void ValidateBytes_RunsChunkValidators()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytes(PngConstants.ChunkIDAT, PngZlibCompressor.Compress(new byte[] { 0, 1, 2, 3, 4 })),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngValidationResult result = Png.ValidateBytes(png);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasError(result, "PNG_CHUNK_DUPLICATE"), Is.True);
    }

    [Test]
    public void ValidateBytes_AppliesStrictAndTolerantMetadataPolicies()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytesWithCrc(PngConstants.ChunkText, new byte[] { 65, 0, 66 }, 0),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngValidationResult strict = Png.ValidateBytes(png, CreateValidationOptions(PngErrorPolicy.Strict));
        PngValidationResult tolerant = Png.ValidateBytes(png, CreateValidationOptions(PngErrorPolicy.TolerantMetadata));
        PngValidationResult bestEffort = Png.ValidateBytes(png, CreateValidationOptions(PngErrorPolicy.BestEffortInspection));

        Assert.That(strict.IsValid, Is.False);
        Assert.That(HasError(strict, "PNG_CHUNK_CRC_MISMATCH"), Is.True);
        Assert.That(strict.Warnings, Is.Empty);
        Assert.That(tolerant.IsValid, Is.True);
        Assert.That(tolerant.Errors, Is.Empty);
        Assert.That(HasWarning(tolerant, "PNG_CHUNK_CRC_MISMATCH"), Is.True);
        Assert.That(bestEffort.IsValid, Is.True);
        Assert.That(bestEffort.Errors, Is.Empty);
        Assert.That(HasWarning(bestEffort, "PNG_CHUNK_CRC_MISMATCH"), Is.True);
    }

    [Test]
    public void ValidateBytes_BestEffortDowngradesStructuralErrorsThatTolerantMetadataKeepsFatal()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkPLTE, new byte[] { 1, 2, 3 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngValidationResult strict = Png.ValidateBytes(png, CreateValidationOptions(PngErrorPolicy.Strict));
        PngValidationResult tolerant = Png.ValidateBytes(png, CreateValidationOptions(PngErrorPolicy.TolerantMetadata));
        PngValidationResult bestEffort = Png.ValidateBytes(png, CreateValidationOptions(PngErrorPolicy.BestEffortInspection));

        Assert.That(strict.IsValid, Is.False);
        Assert.That(HasError(strict, "PNG_PLTE_AFTER_IDAT"), Is.True);
        Assert.That(tolerant.IsValid, Is.False);
        Assert.That(HasError(tolerant, "PNG_PLTE_AFTER_IDAT"), Is.True);
        Assert.That(tolerant.Warnings, Is.Empty);
        Assert.That(bestEffort.IsValid, Is.True);
        Assert.That(bestEffort.Errors, Is.Empty);
        Assert.That(HasWarning(bestEffort, "PNG_PLTE_AFTER_IDAT"), Is.True);
    }

    [Test]
    public void ValidateStream_ValidatesStreamBytes()
    {
        byte[] encoded = Png.EncodeToBytes(CreateImage(new byte[] { 9, 8, 7, 6 }));

        PngValidationResult result = Png.ValidateStream(new MemoryStream(encoded));

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Info.Width, Is.EqualTo(1));
        Assert.That(result.Info.Height, Is.EqualTo(1));
    }

    [Test]
    public void ValidateStream_UsesOptions()
    {
        byte[] actl = new byte[] { 0, 0, 0, 2, 0, 0, 0, 0 };
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytes(PngConstants.ChunkAnimationControl, actl),
            CreateChunkBytes(PngConstants.ChunkIDAT, PngZlibCompressor.Compress(new byte[] { 0, 1, 2, 3, 4 })),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        PngDecoderOptions options = new PngDecoderOptions();
        options.Limits.MaxAnimationFrames = 1;

        Assert.Throws<OverflowException>(() => Png.ValidateStream(new MemoryStream(png), options));
    }

    [Test]
    public void InspectBestEffort_UsesBestEffortPolicy()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytesWithCrc(PngConstants.ChunkIDAT, CreateValidIdatData(), 0),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        byte[] wrapped = Wrap(png);

        PngValidationResult byteResult = Png.InspectBestEffortBytes(wrapped, 2);
        PngValidationResult streamResult = Png.InspectBestEffortStream(new MemoryStream(png));

        Assert.That(byteResult.IsValid, Is.True);
        Assert.That(byteResult.Errors, Is.Empty);
        Assert.That(HasWarning(byteResult, "PNG_CHUNK_CRC_MISMATCH"), Is.True);
        Assert.That(streamResult.IsValid, Is.True);
        Assert.That(streamResult.Errors, Is.Empty);
        Assert.That(HasWarning(streamResult, "PNG_CHUNK_CRC_MISMATCH"), Is.True);
    }

    [Test]
    public void ValidateTolerantRepair_UsesRepairPolicy()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkPhysicalPixels, new byte[] { 0, 0, 0, 1, 0, 0, 0, 1, 1 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        byte[] wrapped = Wrap(png);

        PngValidationResult byteResult = Png.ValidateTolerantRepairBytes(wrapped, 2);
        PngValidationResult streamResult = Png.ValidateTolerantRepairStream(new MemoryStream(png));

        Assert.That(byteResult.IsValid, Is.True);
        Assert.That(byteResult.Errors, Is.Empty);
        Assert.That(streamResult.IsValid, Is.True);
        Assert.That(streamResult.Errors, Is.Empty);
    }

    [Test]
    public void Validate_RejectsInvalidInputs()
    {
        byte[] encoded = Png.EncodeToBytes(CreateImage(new byte[] { 1, 2, 3, 4 }));
        PngDecoderOptions nullLimits = new PngDecoderOptions();
        nullLimits.Limits = null!;

        Assert.Throws<ArgumentNullException>(() => Png.ValidateBytes(null!));
        Assert.Throws<ArgumentNullException>(() => Png.ValidateBytes(encoded, null!));
        Assert.Throws<ArgumentNullException>(() => Png.ValidateBytes(encoded, 0, null!));
        Assert.Throws<ArgumentNullException>(() => Png.ValidateBytes(encoded, nullLimits));
        Assert.Throws<ArgumentNullException>(() => Png.ValidateStream(null!));
        Assert.Throws<ArgumentNullException>(() => Png.ValidateStream(new MemoryStream(encoded), null!));
        Assert.Throws<ArgumentNullException>(() => Png.ValidateStream(new MemoryStream(encoded), nullLimits));
        Assert.Throws<ArgumentNullException>(() => Png.InspectBestEffortBytes(null!));
        Assert.Throws<ArgumentNullException>(() => Png.InspectBestEffortBytes(null!, 0));
        Assert.Throws<ArgumentNullException>(() => Png.InspectBestEffortStream(null!));
        Assert.Throws<ArgumentNullException>(() => Png.ValidateTolerantRepairBytes(null!));
        Assert.Throws<ArgumentNullException>(() => Png.ValidateTolerantRepairBytes(null!, 0));
        Assert.Throws<ArgumentNullException>(() => Png.ValidateTolerantRepairStream(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => Png.ValidateBytes(encoded, -1, new PngDecoderOptions()));
        Assert.Throws<ArgumentOutOfRangeException>(() => Png.InspectBestEffortBytes(encoded, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Png.ValidateTolerantRepairBytes(encoded, -1));
    }

    static PngDecoderOptions CreateValidationOptions(PngErrorPolicy errorPolicy)
    {
        PngDecoderOptions options = new PngDecoderOptions();
        options.ErrorPolicy = errorPolicy;
        options.ValidateCrc = true;
        return options;
    }

    static PngImage CreateImage(byte[] data)
    {
        PngImage image = new PngImage();
        image.Pixels = new PngPixelBuffer();
        image.Pixels.Format = PngPixelFormat.Rgba8;
        image.Pixels.Width = 1;
        image.Pixels.Height = 1;
        image.Pixels.Stride = 4;
        image.Pixels.Data = data;
        return image;
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
        PngChunk chunk = new PngChunk();
        chunk.Type = chunkType;
        chunk.Data = chunkData;
        return PngRawChunkWriter.WriteToBytes(chunk);
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
        byte[] signature = new byte[]
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

        byte[][] arrays = new byte[chunks.Length + 1][];
        arrays[0] = signature;
        int index = 0;
        while (index < chunks.Length)
        {
            arrays[index + 1] = chunks[index];
            index += 1;
        }

        return Join(arrays);
    }

    static byte[] Wrap(byte[] data)
    {
        byte[] wrapped = new byte[data.Length + 2];
        wrapped[0] = 99;
        wrapped[1] = 88;
        Array.Copy(data, 0, wrapped, 2, data.Length);
        return wrapped;
    }

    static byte[] Join(params byte[][] arrays)
    {
        int totalLength = 0;
        int index = 0;
        while (index < arrays.Length)
        {
            totalLength += arrays[index].Length;
            index += 1;
        }

        byte[] result = new byte[totalLength];
        int offset = 0;
        index = 0;
        while (index < arrays.Length)
        {
            Array.Copy(arrays[index], 0, result, offset, arrays[index].Length);
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
