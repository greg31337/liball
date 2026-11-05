using webp;

namespace webp.Tests;

[TestFixture]
public class WebpSecurityLimitsTests
{
    [Test]
    public void Decode_RejectsInputAboveMaxRiffSize()
    {
        byte[] data = NewLosslessContainer(1, 1);
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.Limits.MaxRiffSize = data.Length - 1;

        WebpDecodeResult result = WebpDecoder.Decode(data, options);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics, Has.Some.Matches<WebpDiagnostic>(diagnostic => diagnostic.Message.Contains("RIFF size")));
    }

    [Test]
    public void Decode_RejectsImageDimensionsAboveLimitsBeforePixelDecode()
    {
        byte[] data = NewLosslessContainer(2, 1);
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.Limits.MaxCanvasWidth = 1;

        WebpDecodeResult result = WebpDecoder.Decode(data, options);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Image.Frames, Is.Empty);
        Assert.That(result.Diagnostics, Has.Some.Matches<WebpDiagnostic>(diagnostic => diagnostic.Message.Contains("dimensions")));
    }

    [Test]
    public void Decode_RejectsAnimationAboveFrameLimit()
    {
        byte[] data = NewAnimatedContainer();
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.Limits.MaxFrameCount = 1;

        WebpDecodeResult result = WebpDecoder.Decode(data, options);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics, Has.Some.Matches<WebpDiagnostic>(diagnostic => diagnostic.Message.Contains("frame count")));
    }

    [Test]
    public void Decode_RejectsAnimationAboveChunkLimit()
    {
        byte[] data = NewAnimatedContainer();
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.Limits.MaxChunkCount = 5;

        WebpDecodeResult result = WebpDecoder.Decode(data, options);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics, Has.Some.Matches<WebpDiagnostic>(diagnostic => diagnostic.Message.Contains("chunk count")));
    }

    [Test]
    public void Decode_RejectsAnimationAboveTotalPixelLimit()
    {
        byte[] data = NewAnimatedContainer();
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.Limits.MaxTotalPixels = 1;

        WebpDecodeResult result = WebpDecoder.Decode(data, options);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics, Has.Some.Matches<WebpDiagnostic>(diagnostic => diagnostic.Message.Contains("total pixel count")));
    }

    [Test]
    public void Decode_RejectsCompressedAlphaAboveRecursionLimit()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        SetPixel(buffer, 0, 0, 10, 20, 30, 64);
        WebpEncodeOptions encodeOptions = new WebpEncodeOptions();
        encodeOptions.Mode = WebpEncodeMode.Lossy;
        byte[] data = WebpLossyAlphaEncoder.EncodeStill(buffer, encodeOptions);
        WebpDecodeOptions decodeOptions = new WebpDecodeOptions();
        decodeOptions.Limits.MaxRecursionDepth = 1;

        WebpDecodeResult result = WebpDecoder.Decode(data, decodeOptions);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics, Has.Some.Matches<WebpDiagnostic>(diagnostic => diagnostic.Message.Contains("recursion")));
    }

    [Test]
    public void Encode_RejectsPixelBufferAboveLimits()
    {
        WebpPixelBuffer buffer = NewBuffer(2, 1);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Limits.MaxCanvasWidth = 1;

        Assert.Throws<ArgumentException>(() => WebpEncoder.Encode(buffer, options));
    }

    [Test]
    public void Encode_RejectsAnimatedImageAboveFrameLimit()
    {
        WebpImage image = NewAnimatedImage();
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Limits.MaxFrameCount = 1;

        Assert.Throws<ArgumentException>(() => WebpEncoder.Encode(image, options));
    }

    [Test]
    public void Encode_RejectsCompressedAlphaAboveRecursionLimit()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        SetPixel(buffer, 0, 0, 10, 20, 30, 64);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossy;
        options.Limits.MaxRecursionDepth = 1;

        Assert.Throws<ArgumentException>(() => WebpEncoder.Encode(buffer, options));
    }

    [Test]
    public void PublicDecodeReadAndValidate_RejectMetadataAboveDedicatedLimit()
    {
        byte[] data = NewMetadataContainer(new byte[] { 1, 2, 3 });
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.Limits.MaxMetadataBytes = 2;
        WebpCodec codec = new WebpCodec();

        WebpDecodeResult decoded = codec.Decode(data, options);
        WebpInfo info = codec.Read(data, options);
        WebpValidationResult validation = codec.Validate(data, options);

        Assert.That(decoded.IsValid, Is.False);
        Assert.That(decoded.Diagnostics, Has.Some.Matches<WebpDiagnostic>(diagnostic => diagnostic.Message.Contains("metadata payload")));
        Assert.That(info.Diagnostics, Has.Some.Matches<WebpDiagnostic>(diagnostic => diagnostic.Message.Contains("metadata payload")));
        Assert.That(validation.IsValid, Is.False);
        Assert.That(validation.Diagnostics, Has.Some.Matches<WebpDiagnostic>(diagnostic => diagnostic.Message.Contains("metadata payload")));
    }

    [Test]
    public void PublicDecode_RejectsTopLevelAndFrameUnknownBytesInAggregate()
    {
        WebpImage image = NewAnimatedImage();
        image.UnknownChunks.Add(new WebpUnknownChunk("top1", new byte[] { 1, 2 }));
        image.Frames[0].UnknownChunks.Add(new WebpUnknownChunk("frmx", new byte[] { 3, 4 }));
        byte[] data = WebpContainerWriter.WriteAnimated(image);
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.Limits.MaxUnknownChunkBytes = 3;

        WebpDecodeResult result = new WebpCodec().Decode(data, options);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics, Has.Some.Matches<WebpDiagnostic>(diagnostic => diagnostic.Message.Contains("unknown chunk payload")));
    }

    [Test]
    public void PublicDecode_RejectsAggregatePixelOutputBeforeDecode()
    {
        byte[] data = NewLosslessContainer(1, 1);
        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.Limits.MaxAggregateAllocationBytes = riff.Chunks[0].PayloadLength;

        WebpDecodeResult result = new WebpCodec().Decode(data, options);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Image.Frames, Is.Empty);
        Assert.That(result.Diagnostics, Has.Some.Matches<WebpDiagnostic>(diagnostic => diagnostic.Message.Contains("aggregate allocation")));
    }

    [Test]
    public void PublicEncode_EnforcesRetainedMetadataAndUnknownChunkLimits()
    {
        WebpImage metadataImage = NewAnimatedImage();
        metadataImage.Metadata.Exif = new byte[] { 1, 2 };
        WebpEncodeOptions metadataOptions = new WebpEncodeOptions();
        metadataOptions.Limits.MaxMetadataBytes = 1;
        WebpImage unknownImage = NewAnimatedImage();
        unknownImage.UnknownChunks.Add(new WebpUnknownChunk("test", new byte[] { 1, 2 }));
        WebpEncodeOptions unknownOptions = new WebpEncodeOptions();
        unknownOptions.Limits.MaxUnknownChunkBytes = 1;
        IWebpEncoder encoder = new WebpCodec();

        Assert.Throws<ArgumentException>(() => encoder.EncodeImage(metadataImage, metadataOptions));
        Assert.Throws<ArgumentException>(() => encoder.EncodeImage(unknownImage, unknownOptions));
    }

    [Test]
    public void PublicEncode_RejectsAggregateOutputLimit()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Limits.MaxAggregateAllocationBytes = 3;
        IWebpEncoder encoder = new WebpCodec();

        Assert.Throws<ArgumentException>(() => encoder.Encode(buffer, options));
    }

    [Test]
    public void PublicStreamReader_RejectsInputAllocationBeforeReading()
    {
        byte[] data = NewLosslessContainer(1, 1);
        MemoryStream stream = new MemoryStream(data);
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.Limits.MaxAggregateAllocationBytes = data.Length - 1;
        IWebpInfoReader reader = new WebpCodec();

        Assert.Throws<ArgumentException>(() => reader.ReadStream(stream, options));
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void PublicOperations_RejectNonPositiveNewLimits()
    {
        byte[] data = NewLosslessContainer(1, 1);
        WebpDecodeOptions decodeOptions = new WebpDecodeOptions();
        decodeOptions.Limits.MaxMetadataBytes = 0;
        WebpEncodeOptions encodeOptions = new WebpEncodeOptions();
        encodeOptions.Limits.MaxUnknownChunkBytes = 0;
        WebpCodec codec = new WebpCodec();

        Assert.Throws<ArgumentOutOfRangeException>(() => codec.Validate(data, decodeOptions));
        Assert.Throws<ArgumentOutOfRangeException>(() => codec.Encode(NewBuffer(1, 1), encodeOptions));
    }

    static byte[] NewMetadataContainer(byte[] exif)
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        byte[] payload = WebpVp8LEncoder.Encode(buffer);
        WebpMetadata metadata = new WebpMetadata();
        metadata.Exif = exif;
        return WebpContainerWriter.WriteExtendedStill(payload, WebpBitstreamKind.Lossless, metadata, null);
    }

    static byte[] NewLosslessContainer(int width, int height)
    {
        WebpPixelBuffer buffer = NewBuffer(width, height);
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                SetPixel(buffer, x, y, (byte)(10 + x), (byte)(20 + y), 30, 255);
                x = x + 1;
            }

            y = y + 1;
        }

        return WebpContainerWriter.WriteBasicStill(WebpVp8LEncoder.Encode(buffer), WebpBitstreamKind.Lossless);
    }

    static byte[] NewAnimatedContainer()
    {
        return WebpContainerWriter.WriteAnimated(NewAnimatedImage());
    }

    static WebpImage NewAnimatedImage()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 4;
        image.CanvasHeight = 1;
        image.HasAnimation = true;
        image.Frames.Add(NewFrame(0, 10));
        image.Frames.Add(NewFrame(2, 20));
        return image;
    }

    static WebpFrame NewFrame(int x, int duration)
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        SetPixel(buffer, 0, 0, (byte)(10 + duration), 20, 30, 255);
        WebpFrame frame = new WebpFrame();
        frame.X = x;
        frame.Width = 1;
        frame.Height = 1;
        frame.DurationMilliseconds = duration;
        frame.BitstreamKind = WebpBitstreamKind.Lossless;
        frame.EncodedBitstream = WebpVp8LEncoder.Encode(buffer);
        return frame;
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
}
