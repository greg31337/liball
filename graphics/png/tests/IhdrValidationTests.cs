using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class IhdrValidationTests
{
    [TestCase(PngConstants.ColorTypeGrayscale, PngConstants.BitDepth1, PngPixelFormat.Grayscale1)]
    [TestCase(PngConstants.ColorTypeGrayscale, PngConstants.BitDepth2, PngPixelFormat.Grayscale2)]
    [TestCase(PngConstants.ColorTypeGrayscale, PngConstants.BitDepth4, PngPixelFormat.Grayscale4)]
    [TestCase(PngConstants.ColorTypeGrayscale, PngConstants.BitDepth8, PngPixelFormat.Grayscale8)]
    [TestCase(PngConstants.ColorTypeGrayscale, PngConstants.BitDepth16, PngPixelFormat.Grayscale16)]
    [TestCase(PngConstants.ColorTypeTruecolor, PngConstants.BitDepth8, PngPixelFormat.Rgb8)]
    [TestCase(PngConstants.ColorTypeTruecolor, PngConstants.BitDepth16, PngPixelFormat.Rgb16)]
    [TestCase(PngConstants.ColorTypeIndexedColor, PngConstants.BitDepth1, PngPixelFormat.Indexed1)]
    [TestCase(PngConstants.ColorTypeIndexedColor, PngConstants.BitDepth2, PngPixelFormat.Indexed2)]
    [TestCase(PngConstants.ColorTypeIndexedColor, PngConstants.BitDepth4, PngPixelFormat.Indexed4)]
    [TestCase(PngConstants.ColorTypeIndexedColor, PngConstants.BitDepth8, PngPixelFormat.Indexed8)]
    [TestCase(PngConstants.ColorTypeGrayscaleAlpha, PngConstants.BitDepth8, PngPixelFormat.GrayscaleAlpha8)]
    [TestCase(PngConstants.ColorTypeGrayscaleAlpha, PngConstants.BitDepth16, PngPixelFormat.GrayscaleAlpha16)]
    [TestCase(PngConstants.ColorTypeTruecolorAlpha, PngConstants.BitDepth8, PngPixelFormat.Rgba8)]
    [TestCase(PngConstants.ColorTypeTruecolorAlpha, PngConstants.BitDepth16, PngPixelFormat.Rgba16)]
    public void Validate_AcceptsAllowedColorTypeAndBitDepthCombinations(int colorType, int bitDepth, PngPixelFormat pixelFormat)
    {
        PngIhdr ihdr = CreateIhdr(8, 9, bitDepth, colorType);

        PngValidationResult result = PngIhdrValidator.Validate(ihdr);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Info.Width, Is.EqualTo(8));
        Assert.That(result.Info.Height, Is.EqualTo(9));
        Assert.That(result.Info.ColorType, Is.EqualTo((PngColorType)colorType));
        Assert.That(result.Info.BitDepth, Is.EqualTo((PngBitDepth)bitDepth));
        Assert.That(result.Info.PixelFormat, Is.EqualTo(pixelFormat));
        Assert.That(PngIhdrValidator.IsValid(ihdr), Is.True);
    }

    [Test]
    public void Validate_RejectsInvalidDimensionsColorFieldsAndMethods()
    {
        PngIhdr ihdr = CreateIhdr(0, -3, 3, 1);
        ihdr.CompressionMethod = (PngCompressionMethod)1;
        ihdr.FilterMethod = (PngFilterMethod)1;
        ihdr.InterlaceMethod = (PngInterlaceMethod)2;

        PngValidationResult result = PngIhdrValidator.Validate(ihdr);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Length.EqualTo(7));
        Assert.That(HasError(result, "PNG_IHDR_WIDTH_INVALID"), Is.True);
        Assert.That(HasError(result, "PNG_IHDR_HEIGHT_INVALID"), Is.True);
        Assert.That(HasError(result, "PNG_IHDR_BIT_DEPTH_INVALID"), Is.True);
        Assert.That(HasError(result, "PNG_IHDR_COLOR_TYPE_INVALID"), Is.True);
        Assert.That(HasError(result, "PNG_IHDR_COMPRESSION_METHOD_INVALID"), Is.True);
        Assert.That(HasError(result, "PNG_IHDR_FILTER_METHOD_INVALID"), Is.True);
        Assert.That(HasError(result, "PNG_IHDR_INTERLACE_METHOD_INVALID"), Is.True);
        Assert.That(PngIhdrValidator.IsValid(ihdr), Is.False);
    }

    [TestCase(PngConstants.ColorTypeTruecolor, PngConstants.BitDepth4)]
    [TestCase(PngConstants.ColorTypeIndexedColor, PngConstants.BitDepth16)]
    [TestCase(PngConstants.ColorTypeGrayscaleAlpha, PngConstants.BitDepth4)]
    [TestCase(PngConstants.ColorTypeTruecolorAlpha, PngConstants.BitDepth1)]
    public void Validate_RejectsBitDepthsNotAllowedForColorType(int colorType, int bitDepth)
    {
        PngIhdr ihdr = CreateIhdr(1, 1, bitDepth, colorType);

        PngValidationResult result = PngIhdrValidator.Validate(ihdr);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Length.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_IHDR_BIT_DEPTH_COLOR_TYPE_INVALID"));
    }

    [Test]
    public void Validate_WithLimitsRejectsConfiguredDimensionAndPixelLimits()
    {
        PngIhdr ihdr = CreateIhdr(11, 12, PngConstants.BitDepth8, PngConstants.ColorTypeTruecolor);
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxWidth = 10;
        limits.MaxHeight = 10;
        limits.MaxPixelCount = 20;

        PngValidationResult result = PngIhdrValidator.Validate(ihdr, limits);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Length.EqualTo(3));
        Assert.That(HasError(result, "PNG_IHDR_WIDTH_LIMIT_EXCEEDED"), Is.True);
        Assert.That(HasError(result, "PNG_IHDR_HEIGHT_LIMIT_EXCEEDED"), Is.True);
        Assert.That(HasError(result, "PNG_IHDR_PIXEL_COUNT_LIMIT_EXCEEDED"), Is.True);
        Assert.That(PngIhdrValidator.IsValid(ihdr, limits), Is.False);
    }

    [Test]
    public void Validate_ChunkReportsWrongTypeLengthAndOffsets()
    {
        PngChunk wrongType = CreateChunk(PngConstants.ChunkIEND, new byte[13], 77);
        PngChunk shortIhdr = CreateChunk(PngConstants.ChunkIHDR, new byte[12], 88);
        PngChunk validIhdr = CreateChunk(PngConstants.ChunkIHDR, CreateIhdrData(2, 3, PngConstants.BitDepth8, PngConstants.ColorTypeTruecolor), 99);

        PngValidationResult wrongTypeResult = PngIhdrValidator.Validate(wrongType);
        PngValidationResult shortResult = PngIhdrValidator.Validate(shortIhdr);
        PngValidationResult validResult = PngIhdrValidator.Validate(validIhdr);

        Assert.That(wrongTypeResult.IsValid, Is.False);
        Assert.That(wrongTypeResult.Errors[0].Code, Is.EqualTo("PNG_IHDR_CHUNK_TYPE_INVALID"));
        Assert.That(wrongTypeResult.Errors[0].ChunkType, Is.EqualTo(PngConstants.ChunkIEND));
        Assert.That(wrongTypeResult.Errors[0].Offset, Is.EqualTo(77));
        Assert.That(shortResult.IsValid, Is.False);
        Assert.That(shortResult.Errors[0].Code, Is.EqualTo("PNG_IHDR_LENGTH_INVALID"));
        Assert.That(shortResult.Errors[0].Offset, Is.EqualTo(88));
        Assert.That(validResult.IsValid, Is.True);
        Assert.That(validResult.Info.Width, Is.EqualTo(2));
        Assert.That(validResult.Info.Height, Is.EqualTo(3));
    }

    [Test]
    public void Validate_RejectsNullInputs()
    {
        PngIhdr ihdr = CreateIhdr(1, 1, PngConstants.BitDepth8, PngConstants.ColorTypeTruecolor);
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = new byte[13];
        PngChunk nullData = new PngChunk();
        nullData.Type = PngConstants.ChunkIHDR;
        nullData.Data = null!;

        Assert.Throws<ArgumentNullException>(() => PngIhdrValidator.Validate((PngIhdr)null!));
        Assert.Throws<ArgumentNullException>(() => PngIhdrValidator.Validate(ihdr, null!));
        Assert.Throws<ArgumentNullException>(() => PngIhdrValidator.Validate((PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngIhdrValidator.Validate(nullType));
        Assert.Throws<ArgumentNullException>(() => PngIhdrValidator.Validate(nullData));
        Assert.Throws<ArgumentNullException>(() => PngIhdrValidator.Validate(CreateChunk(PngConstants.ChunkIHDR, new byte[13], 0), null!));
        Assert.Throws<ArgumentNullException>(() => PngIhdrValidator.IsValid(null!));
        Assert.Throws<ArgumentNullException>(() => PngIhdrValidator.IsValid(ihdr, null!));
    }

    [Test]
    public void Identify_ReportsInvalidIhdrFields()
    {
        byte[] ihdrData = CreateIhdrData(0, 1, PngConstants.BitDepth16, PngConstants.ColorTypeIndexedColor);
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, ihdrData),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngIdentifyResult result = PngIdentifier.Identify(png);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasError(result, "PNG_IHDR_WIDTH_INVALID"), Is.True);
        Assert.That(HasError(result, "PNG_IHDR_BIT_DEPTH_COLOR_TYPE_INVALID"), Is.True);
    }

    static PngIhdr CreateIhdr(int width, int height, int bitDepth, int colorType)
    {
        PngIhdr ihdr = new PngIhdr();
        ihdr.Width = width;
        ihdr.Height = height;
        ihdr.BitDepth = (PngBitDepth)bitDepth;
        ihdr.ColorType = (PngColorType)colorType;
        ihdr.CompressionMethod = PngCompressionMethod.Deflate;
        ihdr.FilterMethod = PngFilterMethod.Adaptive;
        ihdr.InterlaceMethod = PngInterlaceMethod.None;
        return ihdr;
    }

    static PngChunk CreateChunk(string chunkType, byte[] data, long offset)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = chunkType;
        chunk.Data = data;
        chunk.Offset = offset;
        chunk.Length = (uint)data.Length;
        return chunk;
    }

    static byte[] CreateIhdrData(int width, int height, int bitDepth, int colorType)
    {
        byte[] data = new byte[13];
        PngBinaryWriter.WriteUInt32(data, 0, (uint)width);
        PngBinaryWriter.WriteUInt32(data, 4, (uint)height);
        data[8] = (byte)bitDepth;
        data[9] = (byte)colorType;
        data[10] = PngConstants.CompressionMethodDeflate;
        data[11] = PngConstants.FilterMethodAdaptive;
        data[12] = PngConstants.InterlaceMethodNone;
        return data;
    }

    static byte[] CreateChunkBytes(string chunkType, byte[] chunkData)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = chunkType;
        chunk.Data = chunkData;
        return PngRawChunkWriter.WriteToBytes(chunk);
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

    static bool HasError(PngIdentifyResult result, string code)
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
}
