using webp;

namespace webp.Tests;

[TestFixture]
public class WebpMalformedFuzzTests
{
    [Test]
    public void GeneratedMalformedInputsReturnDiagnosticsWithoutThrowing()
    {
        WebpDecodeOptions options = NewStrictSmallLimitOptions();

        int seed = 0;
        while (seed < 64)
        {
            byte[] data = NewMalformedBytes(seed);
            WebpValidationResult validation = new WebpValidationResult();
            WebpInfo info = new WebpInfo();
            WebpDecodeResult decoded = new WebpDecodeResult();

            Assert.DoesNotThrow(() =>
            {
                validation = WebpValidator.Validate(data, options);
                info = WebpInfoReader.Read(data, options);
                decoded = WebpDecoder.Decode(data, options);
            });

            Assert.That(validation.IsValid, Is.False);
            Assert.That(validation.Diagnostics, Is.Not.Empty);
            Assert.That(info.Diagnostics, Is.Not.Empty);
            Assert.That(decoded.IsValid, Is.False);
            Assert.That(decoded.Diagnostics, Is.Not.Empty);
            seed = seed + 1;
        }
    }

    [Test]
    public void DecodeRejectsGeneratedContainersAboveStrictAllocationLimitsBeforeFramesAreAllocated()
    {
        WebpDecodeOptions options = NewStrictSmallLimitOptions();
        options.Limits.MaxRiffSize = 4096;
        options.Limits.MaxChunkCount = 16;
        byte[] lossless = NewLosslessContainer(2, 1);
        byte[] extended = NewExtendedLosslessContainer(2, 2);

        WebpDecodeResult losslessResult = WebpDecoder.Decode(lossless, options);
        WebpDecodeResult extendedResult = WebpDecoder.Decode(extended, options);

        Assert.That(losslessResult.IsValid, Is.False);
        Assert.That(losslessResult.Image.Frames, Is.Empty);
        Assert.That(losslessResult.Diagnostics, Has.Some.Matches<WebpDiagnostic>(diagnostic => diagnostic.Message.Contains("dimensions")));
        Assert.That(extendedResult.IsValid, Is.False);
        Assert.That(extendedResult.Image.Frames, Is.Empty);
        Assert.That(extendedResult.Diagnostics, Has.Some.Matches<WebpDiagnostic>(diagnostic => diagnostic.Message.Contains("dimensions")));
    }

    static WebpDecodeOptions NewStrictSmallLimitOptions()
    {
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.Strictness = WebpStrictness.Strict;
        options.Limits.MaxCanvasWidth = 1;
        options.Limits.MaxCanvasHeight = 1;
        options.Limits.MaxCanvasPixels = 1;
        options.Limits.MaxTotalPixels = 1;
        options.Limits.MaxRiffSize = 96;
        options.Limits.MaxChunkCount = 4;
        options.Limits.MaxFrameCount = 1;
        options.Limits.MaxRecursionDepth = 1;
        return options;
    }

    static byte[] NewMalformedBytes(int seed)
    {
        int length = 1 + (seed * 17 % 80);
        byte[] data = new byte[length];
        uint state = (uint)(0xA5A5A5A5U + (uint)seed * 1103515245U);
        int index = 0;
        while (index < data.Length)
        {
            state = state * 1664525U + 1013904223U;
            data[index] = (byte)(state >> 24);
            index = index + 1;
        }

        if (seed % 4 == 1 && data.Length >= WebpConstants.RiffHeaderSize)
        {
            WriteFourCc(data, 0, "RIFF");
            WebpEndianWriter.WriteUInt32(data, 4, (uint)(data.Length + 12));
            WriteFourCc(data, 8, "WEBP");
        }
        else if (seed % 4 == 2 && data.Length >= WebpConstants.RiffHeaderSize + WebpConstants.ChunkHeaderSize)
        {
            WriteFourCc(data, 0, "RIFF");
            WebpEndianWriter.WriteUInt32(data, 4, (uint)(data.Length - 8));
            WriteFourCc(data, 8, "WEBP");
            WriteFourCc(data, 12, "VP8X");
            WebpEndianWriter.WriteUInt32(data, 16, 10);
        }
        else if (seed % 4 == 3 && data.Length >= WebpConstants.RiffHeaderSize + WebpConstants.ChunkHeaderSize)
        {
            WriteFourCc(data, 0, "RIFF");
            WebpEndianWriter.WriteUInt32(data, 4, (uint)(data.Length - 8));
            WriteFourCc(data, 8, "WEBP");
            WriteFourCc(data, 12, "VP8 ");
            WebpEndianWriter.WriteUInt32(data, 16, 4);
        }

        return data;
    }

    static byte[] NewLosslessContainer(int width, int height)
    {
        WebpPixelBuffer buffer = NewBuffer(width, height);
        return WebpContainerWriter.WriteBasicStill(WebpVp8LEncoder.Encode(buffer), WebpBitstreamKind.Lossless);
    }

    static byte[] NewExtendedLosslessContainer(int width, int height)
    {
        WebpPixelBuffer buffer = NewBuffer(width, height);
        byte[] payload = WebpVp8LEncoder.Encode(buffer);
        return WebpContainerWriter.WriteExtendedStill(payload, WebpBitstreamKind.Lossless, new WebpMetadata(), null);
    }

    static WebpPixelBuffer NewBuffer(int width, int height)
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(width, height);
        int index = 0;
        while (index < buffer.Rgba.Length)
        {
            buffer.Rgba[index] = (byte)(10 + index);
            buffer.Rgba[index + 1] = 20;
            buffer.Rgba[index + 2] = 30;
            buffer.Rgba[index + 3] = 255;
            index = index + 4;
        }

        return buffer;
    }

    static void WriteFourCc(byte[] data, int offset, string fourCc)
    {
        data[offset] = (byte)fourCc[0];
        data[offset + 1] = (byte)fourCc[1];
        data[offset + 2] = (byte)fourCc[2];
        data[offset + 3] = (byte)fourCc[3];
    }
}
