using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PngSecurityWorkflowLimitTests
{
    [Test]
    public void DecodeAndProgressiveEntryPoints_EnforceRowByteLimit()
    {
        byte[] png = CreatePng();
        PngDecoderOptions options = new PngDecoderOptions();
        options.Limits.MaxRowBytes = 8;
        PngCodec codec = new PngCodec();
        PngProgressiveDecoder progressive = new PngProgressiveDecoder();
        SecurityLimitSink sink = new SecurityLimitSink();

        Assert.Throws<OverflowException>(() => codec.DecodeBytes(png, options));
        Assert.Throws<OverflowException>(() => codec.DecodeSpan(png, options));
        Assert.Throws<OverflowException>(() => codec.DecodeStream(new MemoryStream(png), options));
        Assert.Throws<OverflowException>(() => progressive.DecodeProgressiveBytes(png, options, sink));
        Assert.Throws<OverflowException>(() => progressive.DecodeProgressiveSpan(png, options, sink));
        Assert.Throws<OverflowException>(() => progressive.DecodeProgressiveStream(new MemoryStream(png), options, sink));
    }

    [Test]
    public void IdentifyValidateAndRepairEntryPoints_EnforceFrameByteLimit()
    {
        byte[] png = CreatePng();
        PngDecoderOptions decoderOptions = new PngDecoderOptions();
        decoderOptions.Limits.MaxFrameBytes = 15;
        PngRepairOptions repairOptions = new PngRepairOptions();
        repairOptions.Limits.MaxFrameBytes = 15;
        PngValidationService validator = new PngValidationService();
        PngRepairService repairer = new PngRepairService();

        Assert.Throws<OverflowException>(() => Png.IdentifyBytes(png, decoderOptions));
        Assert.Throws<OverflowException>(() => Png.IdentifyStream(new MemoryStream(png), decoderOptions));
        Assert.Throws<OverflowException>(() => validator.ValidateBytesWithOptions(png, decoderOptions));
        Assert.Throws<OverflowException>(() => validator.ValidateStreamWithOptions(new MemoryStream(png), decoderOptions));
        Assert.Throws<OverflowException>(() => repairer.RepairBytes(png, repairOptions));
        Assert.Throws<OverflowException>(() => repairer.RepairStream(new MemoryStream(png), repairOptions));
    }

    [Test]
    public void EncodeAndOptimizationEntryPoints_EnforceFrameByteLimit()
    {
        PngImage image = CreateImage();
        PngEncoderOptions encoderOptions = new PngEncoderOptions();
        encoderOptions.Limits.MaxFrameBytes = 15;
        PngOptimizerOptions optimizerOptions = new PngOptimizerOptions();
        optimizerOptions.Limits.MaxFrameBytes = 15;
        PngCodec codec = new PngCodec();
        PngOptimizationService optimizer = new PngOptimizationService();

        Assert.Throws<OverflowException>(() => codec.EncodeToBytes(image, encoderOptions));
        Assert.Throws<OverflowException>(() => codec.EncodeToStream(new MemoryStream(), image, encoderOptions));
        Assert.Throws<OverflowException>(() => optimizer.RunCompressionTrialsWithOptions(image, optimizerOptions));
        Assert.Throws<OverflowException>(() => optimizer.RunFilterTrialsWithOptions(image, optimizerOptions));
    }

    [Test]
    public void InspectionAndDecodeEntryPoints_EnforceAggregateChunkLimits()
    {
        byte[] textPng = CreateTextPng();
        byte[] splitIdatPng = CreateSplitIdatPng();

        AssertInspectionLimit(textPng, CreateLimitsWithChunkCount(4));
        AssertInspectionLimit(textPng, CreateLimitsWithMetadataBytes(9));
        AssertInspectionLimit(textPng, CreateLimitsWithTextEntries(1));
        AssertInspectionLimit(textPng, CreateLimitsWithTextBytes(5));

        PngChunkListResult chunks = PngChunkListReader.Read(splitIdatPng, PngConstants.SignatureLength);
        int firstLength = FindIdatLength(chunks.Chunks, 0);
        int secondLength = FindIdatLength(chunks.Chunks, 1);
        int perChunkLimit = Math.Max(firstLength, secondLength);
        AssertInspectionLimit(splitIdatPng, CreateLimitsWithIdatBytes(perChunkLimit));
    }

    [Test]
    public void IdentifyAndValidate_EnforcePredictedDecompressedByteLimit()
    {
        byte[] png = CreatePng();
        PngDecoderOptions options = new PngDecoderOptions();
        options.Limits.MaxDecompressedBytes = 17;

        Assert.Throws<OverflowException>(() => Png.IdentifyBytes(png, options));
        Assert.Throws<OverflowException>(() => Png.ValidateBytes(png, options));
    }

    static void AssertInspectionLimit(byte[] png, PngSecurityLimits limits)
    {
        PngDecoderOptions options = new PngDecoderOptions();
        options.Limits = limits;

        Assert.Throws<OverflowException>(() => Png.IdentifyBytes(png, options));
        Assert.Throws<OverflowException>(() => Png.ValidateBytes(png, options));
        Assert.Throws<OverflowException>(() => Png.DecodeBytes(png, options));
    }

    static PngSecurityLimits CreateLimitsWithChunkCount(int value)
    {
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxChunkCount = value;
        return limits;
    }

    static PngSecurityLimits CreateLimitsWithMetadataBytes(long value)
    {
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxMetadataBytes = value;
        return limits;
    }

    static PngSecurityLimits CreateLimitsWithTextEntries(int value)
    {
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxTextEntries = value;
        return limits;
    }

    static PngSecurityLimits CreateLimitsWithTextBytes(long value)
    {
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxTextBytes = value;
        return limits;
    }

    static PngSecurityLimits CreateLimitsWithIdatBytes(long value)
    {
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxIdatBytes = value;
        return limits;
    }

    static byte[] CreatePng()
    {
        PngEncoderOptions options = new PngEncoderOptions();
        options.UseAutomaticColorType = false;
        options.UseAutomaticBitDepth = false;
        options.ColorType = PngColorType.TruecolorAlpha;
        options.BitDepth = PngBitDepth.Eight;
        return new PngCodec().EncodeToBytes(CreateImage(), options);
    }

    static PngImage CreateImage()
    {
        PngPixelBuffer pixels = new PngPixelBuffer();
        pixels.Format = PngPixelFormat.Rgba8;
        pixels.Width = 2;
        pixels.Height = 2;
        pixels.Stride = 8;
        pixels.Data = new byte[]
        {
            1, 2, 3, 255, 4, 5, 6, 255,
            7, 8, 9, 255, 10, 11, 12, 255
        };
        PngImage image = new PngImage();
        image.Pixels = pixels;
        return image;
    }

    static byte[] CreateTextPng()
    {
        PngChunkListResult source = PngChunkListReader.Read(CreatePng(), PngConstants.SignatureLength);
        PngChunk first = PngTextWriter.WriteChunk(CreateText("A", "123"));
        PngChunk second = PngTextWriter.WriteChunk(CreateText("B", "456"));
        PngChunk[] chunks = new PngChunk[] { source.Chunks[0], first, second, source.Chunks[1], source.Chunks[2] };
        return PngChunkDatastreamWriter.WriteToBytes(chunks);
    }

    static byte[] CreateSplitIdatPng()
    {
        PngChunkListResult source = PngChunkListReader.Read(CreatePng(), PngConstants.SignatureLength);
        PngChunk idat = source.Chunks[1];
        int firstLength = idat.Data.Length / 2;
        if (firstLength == 0)
        {
            firstLength = 1;
        }

        PngChunk first = CreateIdat(idat.Data, 0, firstLength);
        PngChunk second = CreateIdat(idat.Data, firstLength, idat.Data.Length - firstLength);
        PngChunk[] chunks = new PngChunk[] { source.Chunks[0], first, second, source.Chunks[2] };
        return PngChunkDatastreamWriter.WriteToBytes(chunks);
    }

    static PngChunk CreateIdat(byte[] data, int offset, int length)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkIDAT;
        chunk.Data = new byte[length];
        Array.Copy(data, offset, chunk.Data, 0, length);
        return chunk;
    }

    static PngText CreateText(string keyword, string value)
    {
        PngText text = new PngText();
        text.Keyword = keyword;
        text.Text = value;
        return text;
    }

    static int FindIdatLength(PngChunk[] chunks, int wantedIndex)
    {
        int found = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == PngConstants.ChunkIDAT)
            {
                if (found == wantedIndex)
                {
                    return chunks[index].Data.Length;
                }

                found += 1;
            }

            index += 1;
        }

        return 0;
    }
}

public class SecurityLimitSink : IPngProgressiveSink
{
    public bool WantsPreviews()
    {
        return false;
    }

    public void ReceiveRow(PngProgressiveRow row)
    {
    }

    public void ReceivePass(PngAdam7PassUpdate pass)
    {
    }

    public void ReceivePreview(PngProgressivePreview preview)
    {
    }

    public void ReceiveFrame(PngFrame frame)
    {
    }

    public void ReceiveWarning(PngWarning warning)
    {
    }
}
