using webp;

namespace webp.Tests;

[TestFixture]
public class WebpPublicDecodeConformanceTests
{
    [Test]
    public void Decode_PublicApiProducesExactRgbaForVp8AndVp8LFixtures()
    {
        WebpCodec codec = new WebpCodec();
        byte[] vp8Payload = ReadFixtureHex("Vp8", "skipped-residual-16x16.vp8.hex");
        byte[] vp8Data = WebpContainerWriter.WriteBasicStill(vp8Payload, WebpBitstreamKind.Lossy);
        byte[] vp8LPayload = ReadFixtureHex("Vp8L", "literal-rgba-alpha-2x2.vp8l.hex");
        byte[] vp8LData = WebpContainerWriter.WriteBasicStill(vp8LPayload, WebpBitstreamKind.Lossless);

        WebpDecodeResult vp8 = codec.Decode(vp8Data, new WebpDecodeOptions());
        WebpDecodeResult vp8L = codec.Decode(vp8LData, new WebpDecodeOptions());

        Assert.That(vp8.IsValid, Is.True);
        Assert.That(vp8.Image.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        AssertSolidPixels(
            vp8.Image.Frames[0].Pixels!,
            WebpVp8YuvConverter.YuvToR(127, 128),
            WebpVp8YuvConverter.YuvToG(127, 128, 128),
            WebpVp8YuvConverter.YuvToB(127, 128),
            255);
        Assert.That(vp8L.IsValid, Is.True);
        Assert.That(vp8L.Image.HasAlpha, Is.True);
        Assert.That(vp8L.Image.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(vp8L.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(new byte[]
        {
            10, 20, 30, 255,
            40, 20, 30, 255,
            10, 60, 30, 128,
            40, 60, 90, 128
        }));
    }

    [TestCase(WebpAlphaCompressionMethod.None, 0)]
    [TestCase(WebpAlphaCompressionMethod.Lossless, 1)]
    public void Decode_PublicApiAppliesUncompressedAndCompressedAlpha(WebpAlphaCompressionMethod compression, int expectedMethod)
    {
        WebpCodec codec = new WebpCodec();
        WebpPixelBuffer source = NewBuffer(2, 2);
        SetPixel(source, 0, 0, 20, 40, 60, 0);
        SetPixel(source, 1, 0, 20, 40, 60, 64);
        SetPixel(source, 0, 1, 20, 40, 60, 128);
        SetPixel(source, 1, 1, 20, 40, 60, 255);
        WebpEncodeOptions encodeOptions = new WebpEncodeOptions();
        encodeOptions.Mode = WebpEncodeMode.Lossy;
        encodeOptions.AlphaCompression = compression;
        encodeOptions.AlphaFilter = WebpAlphaFilterMode.Best;
        byte[] data = codec.Encode(source, encodeOptions);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);

        WebpDecodeResult result = codec.Decode(data, new WebpDecodeOptions());

        Assert.That(parsed.AlphaData, Is.Not.Null);
        Assert.That(parsed.AlphaData![0] & WebpConstants.AlphaCompressionMask, Is.EqualTo(expectedMethod));
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Image.HasAlpha, Is.True);
        Assert.That(ReadAlpha(result.Image.Frames[0].Pixels!), Is.EqualTo(new byte[] { 0, 64, 128, 255 }));
    }

    [Test]
    public void Decode_PublicApiInfersExtendedVp8LAlphaFromTheBitstream()
    {
        WebpCodec codec = new WebpCodec();
        byte[] payload = ReadFixtureHex("Vp8L", "literal-rgba-alpha-2x2.vp8l.hex");
        byte[] data = WebpContainerWriter.WriteExtendedStill(payload, WebpBitstreamKind.Lossless, new WebpMetadata(), null);
        ClearVp8XAlphaFlag(data);

        WebpDecodeResult result = codec.Decode(data, new WebpDecodeOptions());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Image.HasAlpha, Is.True);
        Assert.That(ReadAlpha(result.Image.Frames[0].Pixels!), Is.EqualTo(new byte[] { 255, 255, 128, 128 }));
    }

    [Test]
    public void Decode_PublicApiHonorsMetadataAndUnknownChunkPolicies()
    {
        WebpCodec codec = new WebpCodec();
        WebpPixelBuffer source = NewBuffer(1, 1);
        SetPixel(source, 0, 0, 1, 2, 3, 255);
        WebpEncodeOptions encodeOptions = new WebpEncodeOptions();
        encodeOptions.Mode = WebpEncodeMode.Lossless;
        byte[] basic = codec.Encode(source, encodeOptions);
        List<WebpUnknownChunk> unknownChunks = new List<WebpUnknownChunk>();
        unknownChunks.Add(new WebpUnknownChunk("test", new byte[] { 7, 8, 9 }));
        byte[] basicWithUnknown = codec.ReplaceUnknownChunks(basic, unknownChunks, WebpUnknownChunkPlacement.AfterRecognizedChunks);
        WebpMetadata metadata = new WebpMetadata();
        metadata.IccProfile = new byte[] { 1 };
        metadata.Exif = new byte[] { 2 };
        metadata.Xmp = new byte[] { 3 };
        byte[] extended = codec.ReplaceMetadata(basicWithUnknown, metadata);
        WebpDecodeOptions stripOptions = new WebpDecodeOptions();
        stripOptions.DecodeMetadata = false;
        stripOptions.UnknownChunkPolicy = WebpUnknownChunkPolicy.Strip;

        WebpDecodeResult basicPreserved = codec.Decode(basicWithUnknown, new WebpDecodeOptions());
        WebpDecodeResult preserved = codec.Decode(extended, new WebpDecodeOptions());
        WebpDecodeResult stripped = codec.Decode(extended, stripOptions);

        Assert.That(basicPreserved.IsValid, Is.True);
        AssertUnknownChunk(basicPreserved.Image.UnknownChunks);
        Assert.That(preserved.IsValid, Is.True);
        Assert.That(preserved.Image.Metadata.IccProfile, Is.EqualTo(new byte[] { 1 }));
        Assert.That(preserved.Image.Metadata.Exif, Is.EqualTo(new byte[] { 2 }));
        Assert.That(preserved.Image.Metadata.Xmp, Is.EqualTo(new byte[] { 3 }));
        AssertUnknownChunk(preserved.Image.UnknownChunks);
        Assert.That(stripped.IsValid, Is.True);
        Assert.That(stripped.Image.Metadata.IccProfile, Is.Null);
        Assert.That(stripped.Image.Metadata.Exif, Is.Null);
        Assert.That(stripped.Image.Metadata.Xmp, Is.Null);
        Assert.That(stripped.Image.UnknownChunks, Is.Empty);
    }

    [Test]
    public void Decode_PermissiveTrailingDataRemainsValidAndReportsWarning()
    {
        WebpCodec codec = new WebpCodec();
        WebpPixelBuffer source = NewBuffer(1, 1);
        SetPixel(source, 0, 0, 1, 2, 3, 255);
        WebpEncodeOptions encodeOptions = new WebpEncodeOptions();
        encodeOptions.Mode = WebpEncodeMode.Lossless;
        byte[] encoded = codec.Encode(source, encodeOptions);
        byte[] withTrailingData = new byte[encoded.Length + 1];
        Array.Copy(encoded, withTrailingData, encoded.Length);
        withTrailingData[withTrailingData.Length - 1] = 0xA5;
        WebpDecodeOptions decodeOptions = new WebpDecodeOptions();
        decodeOptions.Strictness = WebpStrictness.Permissive;

        WebpDecodeResult result = codec.Decode(withTrailingData, decodeOptions);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Diagnostics, Has.Some.Matches<WebpDiagnostic>(diagnostic => diagnostic.Severity == WebpDiagnosticSeverity.Warning && diagnostic.Message.Contains("trailing")));
        Assert.That(result.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(source.Rgba));
    }

    [Test]
    public void Decode_PublicApiSelectsIndependentFramesAndCompositesAnimationSnapshots()
    {
        WebpCodec codec = new WebpCodec();
        byte[] data = EncodeAnimation(codec);
        WebpDecodeOptions selectedOptions = new WebpDecodeOptions();
        selectedOptions.FrameIndex = 1;
        WebpDecodeOptions firstOptions = new WebpDecodeOptions();
        firstOptions.FirstFrameOnly = true;
        firstOptions.AnimationDecodeMode = WebpAnimationDecodeMode.CompositedFrames;
        WebpDecodeOptions compositedOptions = new WebpDecodeOptions();
        compositedOptions.AnimationDecodeMode = WebpAnimationDecodeMode.CompositedFrames;

        WebpDecodeResult independent = codec.Decode(data, new WebpDecodeOptions());
        WebpDecodeResult selected = codec.Decode(data, selectedOptions);
        WebpDecodeResult first = codec.Decode(data, firstOptions);
        WebpDecodeResult composited = codec.Decode(data, compositedOptions);

        Assert.That(independent.IsValid, Is.True);
        Assert.That(independent.Image.Frames, Has.Count.EqualTo(2));
        AssertFrame(independent.Image.Frames[0], 0, 10, new byte[] { 255, 0, 0, 255 });
        AssertFrame(independent.Image.Frames[1], 2, 20, new byte[] { 0, 255, 0, 255 });
        Assert.That(selected.Image.HasAnimation, Is.False);
        Assert.That(selected.Image.Frames, Has.Count.EqualTo(1));
        AssertFrame(selected.Image.Frames[0], 2, 20, new byte[] { 0, 255, 0, 255 });
        Assert.That(first.Image.HasAnimation, Is.False);
        Assert.That(first.Image.Frames, Has.Count.EqualTo(1));
        Assert.That(first.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(new byte[]
        {
            255, 0, 0, 255,
            1, 2, 3, 255,
            1, 2, 3, 255
        }));
        Assert.That(composited.Image.Frames, Has.Count.EqualTo(2));
        Assert.That(composited.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(first.Image.Frames[0].Pixels!.Rgba));
        Assert.That(composited.Image.Frames[1].Pixels!.Rgba, Is.EqualTo(new byte[]
        {
            1, 2, 3, 255,
            1, 2, 3, 255,
            0, 255, 0, 255
        }));
    }

    [Test]
    public void Decode_PublicApiCropsAndScalesWithExactRgbaOutput()
    {
        WebpCodec codec = new WebpCodec();
        WebpPixelBuffer source = NewGridBuffer();
        WebpEncodeOptions encodeOptions = new WebpEncodeOptions();
        encodeOptions.Mode = WebpEncodeMode.Lossless;
        byte[] data = codec.Encode(source, encodeOptions);
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.CropX = 1;
        options.CropY = 0;
        options.CropWidth = 2;
        options.CropHeight = 2;
        options.TargetWidth = 4;
        options.TargetHeight = 2;
        options.AspectRatioMode = WebpAspectRatioMode.Stretch;

        WebpDecodeResult result = codec.Decode(data, options);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Image.CanvasWidth, Is.EqualTo(4));
        Assert.That(result.Image.CanvasHeight, Is.EqualTo(2));
        Assert.That(result.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(new byte[]
        {
            1, 0, 0, 255, 1, 0, 0, 255, 2, 0, 0, 255, 2, 0, 0, 255,
            11, 0, 0, 255, 11, 0, 0, 255, 12, 0, 0, 255, 12, 0, 0, 255
        }));
        Assert.That(result.Image.Transform.SourceX, Is.EqualTo(1));
        Assert.That(result.Image.Transform.SourceWidth, Is.EqualTo(2));
        Assert.That(result.Image.Transform.OutputWidth, Is.EqualTo(4));
        Assert.That(result.Image.Transform.OutputHeight, Is.EqualTo(2));
    }

    static byte[] EncodeAnimation(WebpCodec codec)
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 3;
        image.CanvasHeight = 1;
        image.HasAnimation = true;
        image.BackgroundColorBgra = 0xFF010203U;
        image.Frames.Add(NewAnimationFrame(0, 10, WebpFrameDisposeMode.DisposeToBackground, 255, 0, 0));
        image.Frames.Add(NewAnimationFrame(2, 20, WebpFrameDisposeMode.DoNotDispose, 0, 255, 0));
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossless;
        return codec.EncodeImage(image, options);
    }

    static WebpFrame NewAnimationFrame(int x, int duration, WebpFrameDisposeMode disposeMode, byte red, byte green, byte blue)
    {
        WebpPixelBuffer pixels = NewBuffer(1, 1);
        SetPixel(pixels, 0, 0, red, green, blue, 255);
        WebpFrame frame = new WebpFrame();
        frame.X = x;
        frame.Y = 0;
        frame.Width = 1;
        frame.Height = 1;
        frame.DurationMilliseconds = duration;
        frame.BlendMode = WebpFrameBlendMode.Replace;
        frame.DisposeMode = disposeMode;
        frame.BitstreamKind = WebpBitstreamKind.Lossless;
        frame.Pixels = pixels;
        return frame;
    }

    static WebpPixelBuffer NewGridBuffer()
    {
        WebpPixelBuffer buffer = NewBuffer(4, 2);
        int y = 0;
        while (y < 2)
        {
            int x = 0;
            while (x < 4)
            {
                SetPixel(buffer, x, y, (byte)(y * 10 + x), 0, 0, 255);
                x = x + 1;
            }

            y = y + 1;
        }

        return buffer;
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

    static void AssertSolidPixels(WebpPixelBuffer buffer, byte red, byte green, byte blue, byte alpha)
    {
        int y = 0;
        while (y < buffer.Height)
        {
            int x = 0;
            while (x < buffer.Width)
            {
                int offset = (y * buffer.Stride) + (x * 4);
                Assert.That(buffer.Rgba[offset], Is.EqualTo(red));
                Assert.That(buffer.Rgba[offset + 1], Is.EqualTo(green));
                Assert.That(buffer.Rgba[offset + 2], Is.EqualTo(blue));
                Assert.That(buffer.Rgba[offset + 3], Is.EqualTo(alpha));
                x = x + 1;
            }

            y = y + 1;
        }
    }

    static void AssertUnknownChunk(List<WebpUnknownChunk> chunks)
    {
        Assert.That(chunks, Has.Count.EqualTo(1));
        Assert.That(chunks[0].FourCc, Is.EqualTo("test"));
        Assert.That(chunks[0].Data, Is.EqualTo(new byte[] { 7, 8, 9 }));
    }

    static void AssertFrame(WebpFrame frame, int x, int duration, byte[] rgba)
    {
        Assert.That(frame.X, Is.EqualTo(x));
        Assert.That(frame.Y, Is.EqualTo(0));
        Assert.That(frame.Width, Is.EqualTo(1));
        Assert.That(frame.Height, Is.EqualTo(1));
        Assert.That(frame.DurationMilliseconds, Is.EqualTo(duration));
        Assert.That(frame.Pixels!.Rgba, Is.EqualTo(rgba));
    }

    static void ClearVp8XAlphaFlag(byte[] data)
    {
        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        int index = 0;
        while (index < riff.Chunks.Count)
        {
            WebpChunkInfo chunk = riff.Chunks[index];
            if (chunk.FourCc == WebpConstants.FourCcVp8XString)
            {
                data[(int)chunk.PayloadOffset] = (byte)(data[(int)chunk.PayloadOffset] & ~WebpConstants.Vp8XFeatureAlpha);
                return;
            }

            index = index + 1;
        }

        throw new InvalidOperationException("expected a VP8X chunk");
    }

    static byte[] ReadFixtureHex(string directory, string fileName)
    {
        string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", directory, fileName);
        string hex = File.ReadAllText(path).Trim();
        byte[] payload = new byte[hex.Length / 2];
        int index = 0;
        while (index < payload.Length)
        {
            payload[index] = Convert.ToByte(hex.Substring(index * 2, 2), 16);
            index = index + 1;
        }

        return payload;
    }
}
