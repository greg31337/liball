using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8LEncoderTests
{
    [Test]
    public void Encode_EmitsPayloadThatDecodesToOriginalPixels()
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(3, 2, 16);
        SetPixel(buffer, 0, 0, 1, 2, 3, 255);
        SetPixel(buffer, 1, 0, 4, 5, 6, 200);
        SetPixel(buffer, 2, 0, 7, 8, 9, 255);
        SetPixel(buffer, 0, 1, 10, 11, 12, 128);
        SetPixel(buffer, 1, 1, 13, 14, 15, 255);
        SetPixel(buffer, 2, 1, 250, 251, 252, 253);

        byte[] payload = WebpVp8LEncoder.Encode(buffer);
        WebpVp8LDecodeResult result = WebpVp8LDecoder.Decode(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.PixelBuffer.Width, Is.EqualTo(3));
        Assert.That(result.PixelBuffer.Height, Is.EqualTo(2));
        Assert.That(result.PixelBuffer.Rgba, Is.EqualTo(WebpPixelBufferHelper.CopyRgba(buffer.Rgba, buffer.Width, buffer.Height, buffer.Stride)));
    }

    [Test]
    public void EncodeDecode_RoundTripsRepresentativeLosslessBuffers()
    {
        AssertVp8LRoundTrip(NewPatternBuffer(1, 1, 0, false));
        AssertVp8LRoundTrip(NewPatternBuffer(3, 3, 8, true));
        AssertVp8LRoundTrip(NewPalettePatternBuffer(9, 2));
    }

    [Test]
    public void EncodeDecode_HighLevelLosslessWebpRoundTripsPixels()
    {
        WebpPixelBuffer buffer = NewPatternBuffer(4, 3, 12, true);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossless;

        byte[] data = WebpEncoder.Encode(buffer, options);
        WebpDecodeResult decoded = WebpDecoder.Decode(data);
        WebpBasicContainerParseResult parsed = WebpBasicContainerParser.Parse(data, WebpStrictness.Strict);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.ContainerKind, Is.EqualTo(WebpContainerKind.SimpleLossless));
        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Image.HasAlpha, Is.True);
        Assert.That(decoded.Image.Frames, Has.Count.EqualTo(1));
        Assert.That(decoded.Image.Frames[0].Pixels, Is.Not.Null);
        Assert.That(decoded.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(WebpPixelBufferHelper.CopyRgba(buffer.Rgba, buffer.Width, buffer.Height, buffer.Stride)));
    }

    [Test]
    public void Encode_WritesHeaderDimensionsAndAlphaHint()
    {
        WebpPixelBuffer opaque = WebpPixelBufferHelper.AllocateRgba(2, 1);
        SetPixel(opaque, 0, 0, 10, 20, 30, 255);
        SetPixel(opaque, 1, 0, 40, 50, 60, 255);
        WebpPixelBuffer alpha = WebpPixelBufferHelper.AllocateRgba(1, 1);
        SetPixel(alpha, 0, 0, 1, 2, 3, 4);

        WebpVp8LHeaderParseResult opaqueHeader = WebpVp8LBitReader.ParseHeader(WebpVp8LEncoder.Encode(opaque), WebpStrictness.Strict);
        WebpVp8LHeaderParseResult alphaHeader = WebpVp8LBitReader.ParseHeader(WebpVp8LEncoder.Encode(alpha), WebpStrictness.Strict);

        Assert.That(opaqueHeader.IsValid, Is.True);
        Assert.That(opaqueHeader.Header.Width, Is.EqualTo(2));
        Assert.That(opaqueHeader.Header.Height, Is.EqualTo(1));
        Assert.That(opaqueHeader.Header.HasAlpha, Is.False);
        Assert.That(alphaHeader.Header.HasAlpha, Is.True);
    }

    [Test]
    public void Encode_UsesSubtractGreenTransformWhenItReducesResiduals()
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(1, 1);
        SetPixel(buffer, 0, 0, 20, 10, 30, 255);

        byte[] payload = WebpVp8LEncoder.Encode(buffer);
        WebpVp8LDecodeResult result = WebpVp8LDecoder.Decode(payload, WebpStrictness.Strict);
        WebpVp8LHeaderParseResult header = WebpVp8LBitReader.ParseHeader(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.PixelBuffer.Rgba, Is.EqualTo(buffer.Rgba));
        Assert.That(header.Reader, Is.Not.Null);
        Assert.That(header.Reader!.ReadBits(1), Is.EqualTo(1));
        Assert.That(header.Reader.ReadBits(2), Is.EqualTo(WebpVp8LDecoder.SubtractGreenTransformType));
    }

    [Test]
    public void Encode_UsesPredictorTransformForRepeatedRows()
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(3, 2);
        SetPixel(buffer, 0, 0, 10, 0, 1, 255);
        SetPixel(buffer, 1, 0, 20, 0, 2, 255);
        SetPixel(buffer, 2, 0, 30, 0, 3, 255);
        SetPixel(buffer, 0, 1, 10, 0, 1, 255);
        SetPixel(buffer, 1, 1, 20, 0, 2, 255);
        SetPixel(buffer, 2, 1, 30, 0, 3, 255);

        byte[] payload = WebpVp8LEncoder.Encode(buffer);
        WebpVp8LDecodeResult result = WebpVp8LDecoder.Decode(payload, WebpStrictness.Strict);
        WebpVp8LHeaderParseResult header = WebpVp8LBitReader.ParseHeader(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.PixelBuffer.Rgba, Is.EqualTo(buffer.Rgba));
        Assert.That(header.Reader, Is.Not.Null);
        Assert.That(header.Reader!.ReadBits(1), Is.EqualTo(1));
        Assert.That(header.Reader.ReadBits(2), Is.EqualTo(WebpVp8LDecoder.PredictorTransformType));
    }

    [Test]
    public void ForwardTransforms_AreReversedByExistingInverseTransforms()
    {
        WebpVp8LColor[] pixels = new WebpVp8LColor[]
        {
            NewColor(10, 2, 12, 255),
            NewColor(20, 4, 24, 255),
            NewColor(10, 2, 12, 255),
            NewColor(20, 4, 24, 255)
        };

        WebpVp8LColor[] subtracted = WebpVp8LEncoder.ApplyForwardSubtractGreen(pixels);
        WebpVp8LColor[] restoredSubtract = WebpVp8LSubtractGreenTransformDecoder.ApplyInverse(subtracted, 2, 2);
        WebpVp8LColor[] residuals = WebpVp8LEncoder.ApplyForwardPredictor(pixels, 2, 2, 2);
        WebpVp8LPredictorTransform transform = new WebpVp8LPredictorTransform();
        transform.SizeBits = 2;
        transform.Width = 1;
        transform.Height = 1;
        transform.Modes = new byte[] { 2 };
        WebpVp8LColor[] restoredPredictor = WebpVp8LPredictorTransformDecoder.ApplyInverse(residuals, 2, 2, transform);

        Assert.That(restoredSubtract[0].ToArgb(), Is.EqualTo(pixels[0].ToArgb()));
        Assert.That(restoredSubtract[3].ToArgb(), Is.EqualTo(pixels[3].ToArgb()));
        Assert.That(restoredPredictor[0].ToArgb(), Is.EqualTo(pixels[0].ToArgb()));
        Assert.That(restoredPredictor[3].ToArgb(), Is.EqualTo(pixels[3].ToArgb()));
    }

    [Test]
    public void Encode_UsesColorIndexingForLowColorImages()
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(4, 2);
        SetPixel(buffer, 0, 0, 10, 20, 30, 255);
        SetPixel(buffer, 1, 0, 40, 50, 60, 255);
        SetPixel(buffer, 2, 0, 10, 20, 30, 255);
        SetPixel(buffer, 3, 0, 40, 50, 60, 255);
        SetPixel(buffer, 0, 1, 40, 50, 60, 255);
        SetPixel(buffer, 1, 1, 10, 20, 30, 255);
        SetPixel(buffer, 2, 1, 40, 50, 60, 255);
        SetPixel(buffer, 3, 1, 10, 20, 30, 255);

        byte[] payload = WebpVp8LEncoder.Encode(buffer);
        WebpVp8LDecodeResult result = WebpVp8LDecoder.Decode(payload, WebpStrictness.Strict);
        WebpVp8LHeaderParseResult header = WebpVp8LBitReader.ParseHeader(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.PixelBuffer.Rgba, Is.EqualTo(buffer.Rgba));
        Assert.That(header.Reader, Is.Not.Null);
        Assert.That(header.Reader!.ReadBits(1), Is.EqualTo(1));
        Assert.That(header.Reader.ReadBits(2), Is.EqualTo(WebpVp8LDecoder.ColorIndexingTransformType));
    }

    [Test]
    public void CreateColorIndexingPlan_BuildsFirstSeenPaletteAndPackedIndices()
    {
        WebpVp8LColor first = NewColor(10, 20, 30, 255);
        WebpVp8LColor second = NewColor(40, 50, 60, 255);
        WebpVp8LColor[] pixels = new WebpVp8LColor[]
        {
            first,
            second,
            first,
            second,
            second,
            first,
            second,
            first
        };

        WebpVp8LColorIndexingEncodePlan plan = WebpVp8LEncoder.CreateColorIndexingPlan(pixels, 8, 1);

        Assert.That(plan.UseColorIndexing, Is.True);
        Assert.That(plan.ColorTableSize, Is.EqualTo(2));
        Assert.That(plan.WidthBits, Is.EqualTo(3));
        Assert.That(plan.PackedWidth, Is.EqualTo(1));
        Assert.That(plan.ColorTable[0].ToArgb(), Is.EqualTo(first.ToArgb()));
        Assert.That(plan.ColorTable[1].ToArgb(), Is.EqualTo(second.ToArgb()));
        Assert.That(plan.PackedPixels[0].Green, Is.EqualTo(0x5A));
    }

    [Test]
    public void CreateColorIndexingPlan_DisablesPaletteWhenTooManyColors()
    {
        WebpVp8LColor[] pixels = new WebpVp8LColor[257];
        int index = 0;
        while (index < pixels.Length)
        {
            pixels[index] = NewColor((byte)index, (byte)(index >> 1), (byte)(index >> 2), 255);
            index = index + 1;
        }

        WebpVp8LColorIndexingEncodePlan plan = WebpVp8LEncoder.CreateColorIndexingPlan(pixels, 257, 1);

        Assert.That(plan.UseColorIndexing, Is.False);
        Assert.That(plan.ColorTableSize, Is.EqualTo(257));
    }

    [Test]
    public void Encode_IsDeterministic()
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(2, 1);
        SetPixel(buffer, 0, 0, 9, 8, 7, 6);
        SetPixel(buffer, 1, 0, 5, 4, 3, 2);

        byte[] first = WebpVp8LEncoder.Encode(buffer);
        byte[] second = WebpVp8LEncoder.Encode(buffer);

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void Encode_WithNearLosslessOptionsEncodesPreprocessedPixels()
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(1, 1);
        SetPixel(buffer, 0, 0, 13, 21, 250, 128);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.NearLossless;
        options.NearLosslessQuality = 50;

        WebpVp8LDecodeResult decoded = WebpVp8LDecoder.Decode(WebpVp8LEncoder.Encode(buffer, options), WebpStrictness.Strict);

        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.PixelBuffer.Rgba, Is.EqualTo(new byte[] { 16, 24, 248, 128 }));
    }

    [Test]
    public void Encode_WithNearLosslessQuality100PreservesPixels()
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(1, 1);
        SetPixel(buffer, 0, 0, 13, 21, 250, 128);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.NearLossless;
        options.NearLosslessQuality = 100;

        WebpVp8LDecodeResult decoded = WebpVp8LDecoder.Decode(WebpVp8LEncoder.Encode(buffer, options), WebpStrictness.Strict);

        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.PixelBuffer.Rgba, Is.EqualTo(buffer.Rgba));
    }

    [Test]
    public void Encode_WithOptionsRejectsInvalidNearLosslessQuality()
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(1, 1);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.NearLosslessQuality = 101;

        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LEncoder.Encode(buffer, options));
    }

    [Test]
    public void Encode_RejectsInvalidVp8LDimensions()
    {
        WebpPixelBuffer tooWide = new WebpPixelBuffer();
        tooWide.Width = WebpConstants.Vp8LMaxDimension + 1;
        tooWide.Height = 1;
        tooWide.Stride = WebpPixelBufferHelper.ComputeMinimumRgbaStride(tooWide.Width);
        tooWide.Rgba = new byte[tooWide.Stride];

        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LEncoder.Encode(tooWide));
    }

    static void AssertVp8LRoundTrip(WebpPixelBuffer buffer)
    {
        byte[] payload = WebpVp8LEncoder.Encode(buffer);

        WebpVp8LDecodeResult result = WebpVp8LDecoder.Decode(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.PixelBuffer.Width, Is.EqualTo(buffer.Width));
        Assert.That(result.PixelBuffer.Height, Is.EqualTo(buffer.Height));
        Assert.That(result.PixelBuffer.Rgba, Is.EqualTo(WebpPixelBufferHelper.CopyRgba(buffer.Rgba, buffer.Width, buffer.Height, buffer.Stride)));
    }

    static WebpPixelBuffer NewPatternBuffer(int width, int height, int stridePadding, bool includeAlpha)
    {
        int stride = WebpPixelBufferHelper.ComputeMinimumRgbaStride(width) + stridePadding;
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(width, height, stride);
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                byte red = (byte)((17 + x * 31 + y * 19) & 255);
                byte green = (byte)((23 + x * 11 + y * 29) & 255);
                byte blue = (byte)((31 + x * 7 + y * 37) & 255);
                byte alpha = 255;
                if (includeAlpha)
                {
                    alpha = (byte)((80 + x * 47 + y * 53) & 255);
                }

                SetPixel(buffer, x, y, red, green, blue, alpha);
                x = x + 1;
            }

            y = y + 1;
        }

        return buffer;
    }

    static WebpPixelBuffer NewPalettePatternBuffer(int width, int height)
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(width, height);
        byte[] palette = new byte[]
        {
            10, 20, 30, 255,
            40, 50, 60, 128,
            70, 80, 90, 255
        };

        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                int index = ((x + y) % 3) * 4;
                SetPixel(buffer, x, y, palette[index], palette[index + 1], palette[index + 2], palette[index + 3]);
                x = x + 1;
            }

            y = y + 1;
        }

        return buffer;
    }

    static void SetPixel(WebpPixelBuffer buffer, int x, int y, byte red, byte green, byte blue, byte alpha)
    {
        int pixel = y * buffer.Stride + x * 4;
        buffer.Rgba[pixel] = red;
        buffer.Rgba[pixel + 1] = green;
        buffer.Rgba[pixel + 2] = blue;
        buffer.Rgba[pixel + 3] = alpha;
    }

    static WebpVp8LColor NewColor(byte red, byte green, byte blue, byte alpha)
    {
        WebpVp8LColor color = new WebpVp8LColor();
        color.Red = red;
        color.Green = green;
        color.Blue = blue;
        color.Alpha = alpha;
        return color;
    }
}
