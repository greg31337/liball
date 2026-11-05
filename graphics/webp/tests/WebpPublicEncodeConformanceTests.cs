using webp;

namespace webp.Tests;

[TestFixture]
public class WebpPublicEncodeConformanceTests
{
    [Test]
    public void Encode_PublicModesProduceDecodableExpectedBitstreams()
    {
        WebpCodec codec = new WebpCodec();
        WebpPixelBuffer source = NewBuffer(1, 1);
        SetPixel(source, 0, 0, 123, 45, 67, 255);
        WebpEncodeOptions automaticOptions = new WebpEncodeOptions();
        WebpEncodeOptions lossyOptions = NewLossyOptions();
        WebpEncodeOptions losslessOptions = new WebpEncodeOptions();
        losslessOptions.Mode = WebpEncodeMode.Lossless;
        WebpEncodeOptions nearLosslessOptions = new WebpEncodeOptions();
        nearLosslessOptions.Mode = WebpEncodeMode.NearLossless;
        nearLosslessOptions.NearLosslessQuality = 50;

        WebpDecodeResult automatic = RoundTrip(codec, source, automaticOptions);
        WebpDecodeResult lossy = RoundTrip(codec, source, lossyOptions);
        WebpDecodeResult lossless = RoundTrip(codec, source, losslessOptions);
        WebpDecodeResult nearLossless = RoundTrip(codec, source, nearLosslessOptions);

        Assert.That(automatic.Image.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(automatic.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(source.Rgba));
        Assert.That(lossy.Image.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(lossless.Image.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(lossless.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(source.Rgba));
        Assert.That(nearLossless.Image.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(nearLossless.Image.Frames[0].Pixels!.Rgba[0], Is.EqualTo(120));
    }

    [Test]
    public void Encode_PublicLossyQualityAndFilterOptionsReachTheVp8Header()
    {
        WebpCodec codec = new WebpCodec();
        WebpPixelBuffer source = NewOpaqueSample();
        WebpEncodeOptions manual = NewLossyOptions();
        manual.Quality = 100;
        manual.FilterStrength = 100;
        manual.FilterSharpness = 7;
        WebpEncodeOptions automatic = NewLossyOptions();
        automatic.Quality = 10;
        automatic.Method = 6;
        automatic.FilterStrength = 0;
        automatic.AutoFilter = true;
        automatic.SpatialNoiseShaping = 100;

        WebpVp8FrameHeaderData manualHeader = EncodeAndReadVp8Header(codec, source, manual);
        WebpVp8FrameHeaderData automaticHeader = EncodeAndReadVp8Header(codec, source, automatic);

        Assert.That(manualHeader.Quantizer.BaseQIndex, Is.EqualTo(0));
        Assert.That(manualHeader.LoopFilter.Level, Is.EqualTo(63));
        Assert.That(manualHeader.LoopFilter.Sharpness, Is.EqualTo(7));
        Assert.That(automaticHeader.Quantizer.BaseQIndex, Is.EqualTo(114));
        Assert.That(automaticHeader.LoopFilter.Level, Is.EqualTo(40));
    }

    [TestCase(WebpLossyPreset.Default, 38, 0)]
    [TestCase(WebpLossyPreset.Picture, 22, 4)]
    [TestCase(WebpLossyPreset.Photo, 19, 3)]
    [TestCase(WebpLossyPreset.Drawing, 6, 6)]
    [TestCase(WebpLossyPreset.Icon, 0, 0)]
    [TestCase(WebpLossyPreset.Text, 0, 0)]
    public void Encode_PublicLossyPresetsApplyTheirFilterProfile(WebpLossyPreset preset, int level, int sharpness)
    {
        WebpEncodeOptions options = NewLossyOptions();
        options.Preset = preset;

        WebpVp8FrameHeaderData header = EncodeAndReadVp8Header(new WebpCodec(), NewOpaqueSample(), options);

        Assert.That(header.LoopFilter.Level, Is.EqualTo(level));
        Assert.That(header.LoopFilter.Sharpness, Is.EqualTo(sharpness));
    }

    [TestCase(WebpLossyPreprocessing.None)]
    [TestCase(WebpLossyPreprocessing.SegmentSmooth)]
    [TestCase(WebpLossyPreprocessing.Dithering)]
    [TestCase(WebpLossyPreprocessing.SegmentSmoothAndDithering)]
    public void Encode_PublicPreprocessingModesProduceValidRoundTripsWithoutMutatingInput(WebpLossyPreprocessing preprocessing)
    {
        WebpCodec codec = new WebpCodec();
        WebpPixelBuffer source = NewOpaqueSample();
        byte[] original = WebpUnknownChunk.CopyBytes(source.Rgba);
        WebpEncodeOptions options = NewLossyOptions();
        options.Preprocessing = preprocessing;

        WebpDecodeResult decoded = RoundTrip(codec, source, options);

        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Image.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(source.Rgba, Is.EqualTo(original));
    }

    [Test]
    public void Encode_PublicTargetSizeAndTargetPsnrUseTheirPlannedPasses()
    {
        WebpCodec codec = new WebpCodec();
        WebpPixelBuffer source = NewOpaqueSample();
        WebpEncodeOptions sizeOptions = NewLossyOptions();
        sizeOptions.Quality = 80;
        sizeOptions.TargetSize = 1;
        sizeOptions.PassCount = 3;
        WebpEncodeOptions psnrOptions = NewLossyOptions();
        psnrOptions.TargetPsnr = 42.0;
        psnrOptions.PassCount = 3;

        byte[] sizeData = codec.Encode(source, sizeOptions);
        byte[] psnrData = codec.Encode(source, psnrOptions);
        WebpVp8FrameHeaderData sizeHeader = ReadVp8Header(sizeData);
        WebpVp8FrameHeaderData psnrHeader = ReadVp8Header(psnrData);

        Assert.That(sizeHeader.Quantizer.BaseQIndex, Is.EqualTo(127));
        Assert.That(psnrHeader.Quantizer.BaseQIndex, Is.EqualTo(27));
        Assert.That(codec.Decode(sizeData, new WebpDecodeOptions()).IsValid, Is.True);
        Assert.That(codec.Decode(psnrData, new WebpDecodeOptions()).IsValid, Is.True);
    }

    [Test]
    public void Encode_PublicAlphaOptionsControlQuantizationFilteringAndCompression()
    {
        WebpCodec codec = new WebpCodec();
        WebpPixelBuffer source = NewBuffer(2, 1);
        SetPixel(source, 0, 0, 20, 40, 60, 7);
        SetPixel(source, 1, 0, 20, 40, 60, 250);
        WebpEncodeOptions quantizedOptions = NewLossyOptions();
        quantizedOptions.LosslessAlpha = false;
        quantizedOptions.AlphaQuality = 0;
        quantizedOptions.AlphaCompression = WebpAlphaCompressionMethod.None;
        quantizedOptions.AlphaFilter = WebpAlphaFilterMode.None;
        WebpEncodeOptions compressedOptions = NewLossyOptions();
        compressedOptions.ExactTransparentRgb = true;
        compressedOptions.LosslessAlpha = true;
        compressedOptions.AlphaCompression = WebpAlphaCompressionMethod.Lossless;
        compressedOptions.AlphaFilter = WebpAlphaFilterMode.Best;

        byte[] quantizedData = codec.Encode(source, quantizedOptions);
        byte[] compressedData = codec.Encode(source, compressedOptions);
        WebpExtendedContainerParseResult quantized = WebpExtendedContainerParser.Parse(quantizedData, WebpStrictness.Strict);
        WebpExtendedContainerParseResult compressed = WebpExtendedContainerParser.Parse(compressedData, WebpStrictness.Strict);
        WebpDecodeResult quantizedRoundTrip = codec.Decode(quantizedData, new WebpDecodeOptions());
        WebpDecodeResult compressedRoundTrip = codec.Decode(compressedData, new WebpDecodeOptions());

        Assert.That(quantized.AlphaData![0] & WebpConstants.AlphaCompressionMask, Is.EqualTo(WebpConstants.AlphaCompressionNone));
        Assert.That(compressed.AlphaData![0] & WebpConstants.AlphaCompressionMask, Is.EqualTo(WebpConstants.AlphaCompressionLossless));
        Assert.That(ReadAlpha(quantizedRoundTrip.Image.Frames[0].Pixels!), Is.EqualTo(new byte[] { 0, 255 }));
        Assert.That(ReadAlpha(compressedRoundTrip.Image.Frames[0].Pixels!), Is.EqualTo(new byte[] { 7, 250 }));
    }

    [TestCase(WebpMetadataCopyPolicy.None, false, false, false, false)]
    [TestCase(WebpMetadataCopyPolicy.All, true, true, true, true)]
    [TestCase(WebpMetadataCopyPolicy.Icc, true, false, false, true)]
    [TestCase(WebpMetadataCopyPolicy.Exif, false, true, false, true)]
    [TestCase(WebpMetadataCopyPolicy.Xmp, false, false, true, true)]
    [TestCase(WebpMetadataCopyPolicy.Selected, true, true, true, true)]
    public void EncodeImage_PublicStillMetadataPoliciesSelectPayloadsAndContainerShape(
        WebpMetadataCopyPolicy policy,
        bool hasIcc,
        bool hasExif,
        bool hasXmp,
        bool extended)
    {
        WebpCodec codec = new WebpCodec();
        WebpImage image = NewStillImage();
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossless;
        options.MetadataCopyPolicy = policy;

        byte[] data = codec.EncodeImage(image, options);
        WebpDecodeResult decoded = codec.Decode(data, new WebpDecodeOptions());

        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Image.Metadata.IccProfile != null, Is.EqualTo(hasIcc));
        Assert.That(decoded.Image.Metadata.Exif != null, Is.EqualTo(hasExif));
        Assert.That(decoded.Image.Metadata.Xmp != null, Is.EqualTo(hasXmp));
        Assert.That(WebpInfoReader.HasVp8X(data), Is.EqualTo(extended));
        Assert.That(decoded.Image.UnknownChunks, Has.Count.EqualTo(1));
        Assert.That(decoded.Image.UnknownChunks[0].Data, Is.EqualTo(new byte[] { 9 }));
        AssertStrictlyValid(codec, data);
    }

    [Test]
    public void EncodeImage_PublicUnknownChunkPolicyCanStripStillPayloads()
    {
        WebpCodec codec = new WebpCodec();
        WebpImage image = NewStillImage();
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossless;
        options.MetadataCopyPolicy = WebpMetadataCopyPolicy.None;
        options.UnknownChunkPolicy = WebpUnknownChunkPolicy.Strip;

        byte[] data = codec.EncodeImage(image, options);
        WebpDecodeResult decoded = codec.Decode(data, new WebpDecodeOptions());

        Assert.That(WebpInfoReader.HasVp8X(data), Is.False);
        Assert.That(decoded.Image.UnknownChunks, Is.Empty);
        Assert.That(decoded.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(image.Frames[0].Pixels!.Rgba));
    }

    [Test]
    public void EncodeImage_PublicMetadataPreservesAnExtendedLossyAlphaStill()
    {
        WebpCodec codec = new WebpCodec();
        WebpImage image = NewStillImage();
        SetPixel(image.Frames[0].Pixels!, 0, 0, 10, 20, 30, 64);
        WebpEncodeOptions options = NewLossyOptions();
        options.AlphaCompression = WebpAlphaCompressionMethod.None;
        options.AlphaFilter = WebpAlphaFilterMode.None;

        byte[] data = codec.EncodeImage(image, options);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);
        WebpDecodeResult decoded = codec.Decode(data, new WebpDecodeOptions());

        Assert.That(parsed.Vp8X.HasAlpha, Is.True);
        Assert.That(parsed.Vp8X.HasIccProfile, Is.True);
        Assert.That(parsed.Vp8X.HasExif, Is.True);
        Assert.That(parsed.Vp8X.HasXmp, Is.True);
        Assert.That(parsed.AlphaData, Is.Not.Null);
        Assert.That(decoded.Image.Frames[0].Pixels!.Rgba[3], Is.EqualTo(64));
        AssertStrictlyValid(codec, data);
    }

    [TestCase(WebpEncodeMode.Automatic)]
    [TestCase(WebpEncodeMode.Lossy)]
    [TestCase(WebpEncodeMode.Lossless)]
    [TestCase(WebpEncodeMode.NearLossless)]
    public void Encode_PublicDeterministicOptionProducesStableRoundTrips(WebpEncodeMode mode)
    {
        WebpCodec codec = new WebpCodec();
        WebpPixelBuffer source = NewOpaqueSample();
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = mode;
        options.Deterministic = true;
        if (mode == WebpEncodeMode.NearLossless)
        {
            options.NearLosslessQuality = 60;
        }

        byte[] first = codec.Encode(source, options);
        byte[] second = codec.Encode(source, options);

        Assert.That(second, Is.EqualTo(first));
        Assert.That(codec.Decode(first, new WebpDecodeOptions()).IsValid, Is.True);
    }

    [Test]
    public void Encode_PublicSecurityOptionsRejectPixelOutputAndRetainedPayloadOverages()
    {
        WebpCodec codec = new WebpCodec();
        WebpPixelBuffer twoPixels = NewBuffer(2, 1);
        WebpEncodeOptions pixelOptions = new WebpEncodeOptions();
        pixelOptions.Limits.MaxCanvasPixels = 1;
        WebpEncodeOptions outputOptions = new WebpEncodeOptions();
        outputOptions.Limits.MaxRiffSize = 20;
        WebpImage image = NewStillImage();
        WebpEncodeOptions metadataOptions = new WebpEncodeOptions();
        metadataOptions.Limits.MaxMetadataBytes = 2;
        WebpEncodeOptions unknownOptions = new WebpEncodeOptions();
        unknownOptions.Limits.MaxUnknownChunkBytes = 1;

        Assert.Throws<ArgumentException>(() => codec.Encode(twoPixels, pixelOptions));
        Assert.Throws<ArgumentException>(() => codec.Encode(NewOpaqueSample(), outputOptions));
        Assert.Throws<ArgumentException>(() => codec.EncodeImage(image, metadataOptions));
        image.UnknownChunks[0] = new WebpUnknownChunk("test", new byte[] { 9, 10 });
        Assert.Throws<ArgumentException>(() => codec.EncodeImage(image, unknownOptions));
    }

    static WebpDecodeResult RoundTrip(WebpCodec codec, WebpPixelBuffer source, WebpEncodeOptions options)
    {
        byte[] data = codec.Encode(source, options);
        WebpDecodeResult result = codec.Decode(data, new WebpDecodeOptions());
        Assert.That(result.IsValid, Is.True);
        return result;
    }

    static WebpVp8FrameHeaderData EncodeAndReadVp8Header(WebpCodec codec, WebpPixelBuffer source, WebpEncodeOptions options)
    {
        return ReadVp8Header(codec.Encode(source, options));
    }

    static WebpVp8FrameHeaderData ReadVp8Header(byte[] data)
    {
        WebpBasicContainerParseResult basic = WebpBasicContainerParser.Parse(data, WebpStrictness.Strict);
        Assert.That(basic.IsValid, Is.True);
        WebpVp8FrameHeaderDataParseResult parsed = WebpVp8FrameHeaderDataParser.Parse(basic.ImagePayload, WebpStrictness.Strict);
        Assert.That(parsed.IsValid, Is.True);
        return parsed.HeaderData;
    }

    static WebpEncodeOptions NewLossyOptions()
    {
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossy;
        return options;
    }

    static WebpPixelBuffer NewOpaqueSample()
    {
        WebpPixelBuffer source = NewBuffer(2, 2);
        SetPixel(source, 0, 0, 10, 20, 30, 255);
        SetPixel(source, 1, 0, 70, 80, 90, 255);
        SetPixel(source, 0, 1, 30, 60, 90, 255);
        SetPixel(source, 1, 1, 120, 90, 60, 255);
        return source;
    }

    static WebpImage NewStillImage()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 1;
        image.CanvasHeight = 1;
        image.Metadata.IccProfile = new byte[] { 1 };
        image.Metadata.Exif = new byte[] { 2 };
        image.Metadata.Xmp = new byte[] { 3 };
        image.UnknownChunks.Add(new WebpUnknownChunk("test", new byte[] { 9 }));
        WebpFrame frame = new WebpFrame();
        frame.Width = 1;
        frame.Height = 1;
        frame.Pixels = NewBuffer(1, 1);
        SetPixel(frame.Pixels, 0, 0, 10, 20, 30, 255);
        image.Frames.Add(frame);
        return image;
    }

    static WebpPixelBuffer NewBuffer(int width, int height)
    {
        return WebpPixelBufferHelper.AllocateRgba(width, height);
    }

    static void SetPixel(WebpPixelBuffer buffer, int x, int y, byte red, byte green, byte blue, byte alpha)
    {
        int offset = (y * buffer.Stride) + (x * 4);
        buffer.Rgba[offset] = red;
        buffer.Rgba[offset + 1] = green;
        buffer.Rgba[offset + 2] = blue;
        buffer.Rgba[offset + 3] = alpha;
    }

    static byte[] ReadAlpha(WebpPixelBuffer buffer)
    {
        byte[] alpha = new byte[buffer.Width * buffer.Height];
        int y = 0;
        while (y < buffer.Height)
        {
            int x = 0;
            while (x < buffer.Width)
            {
                alpha[(y * buffer.Width) + x] = buffer.Rgba[(y * buffer.Stride) + (x * 4) + 3];
                x = x + 1;
            }

            y = y + 1;
        }

        return alpha;
    }

    static void AssertStrictlyValid(WebpCodec codec, byte[] data)
    {
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.Strictness = WebpStrictness.Strict;
        Assert.That(codec.Validate(data, options).IsValid, Is.True);
    }
}
