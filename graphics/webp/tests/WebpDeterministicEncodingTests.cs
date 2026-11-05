using webp;

namespace webp.Tests;

[TestFixture]
public class WebpDeterministicEncodingTests
{
    [Test]
    public void Encode_DeterministicStillModesProduceIdenticalBytes()
    {
        WebpPixelBuffer opaque = NewPixels(false);
        WebpPixelBuffer alpha = NewPixels(true);
        WebpEncodeOptions lossy = NewOptions(WebpEncodeMode.Lossy);
        lossy.Quality = 37;
        WebpEncodeOptions lossless = NewOptions(WebpEncodeMode.Lossless);
        WebpEncodeOptions nearLossless = NewOptions(WebpEncodeMode.NearLossless);
        nearLossless.NearLosslessQuality = 45;
        WebpEncodeOptions alphaOptions = NewOptions(WebpEncodeMode.Lossy);
        alphaOptions.Quality = 62;
        alphaOptions.AlphaCompression = WebpAlphaCompressionMethod.Lossless;
        alphaOptions.AlphaFilter = WebpAlphaFilterMode.Best;

        byte[] lossyData = AssertStableStill(opaque, lossy);
        byte[] losslessData = AssertStableStill(opaque, lossless);
        byte[] nearLosslessData = AssertStableStill(opaque, nearLossless);
        byte[] alphaData = AssertStableStill(alpha, alphaOptions);

        Assert.That(WebpBasicContainerParser.Parse(lossyData, WebpStrictness.Strict).ContainerKind, Is.EqualTo(WebpContainerKind.SimpleLossy));
        Assert.That(WebpBasicContainerParser.Parse(losslessData, WebpStrictness.Strict).ContainerKind, Is.EqualTo(WebpContainerKind.SimpleLossless));
        Assert.That(WebpBasicContainerParser.Parse(nearLosslessData, WebpStrictness.Strict).ContainerKind, Is.EqualTo(WebpContainerKind.SimpleLossless));
        WebpExtendedContainerParseResult parsedAlpha = WebpExtendedContainerParser.Parse(alphaData, WebpStrictness.Strict);
        Assert.That(parsedAlpha.AlphaData, Is.Not.Null);
        Assert.That(parsedAlpha.AlphaData![0] & WebpConstants.AlphaCompressionMask, Is.EqualTo(WebpConstants.AlphaCompressionLossless));
    }

    [Test]
    public void EncodeImage_DeterministicMixedAnimationMatchesArrayStreamAndFileOutputs()
    {
        WebpImage image = NewMixedAnimation();
        WebpEncodeOptions options = NewOptions(WebpEncodeMode.Lossless);
        WebpCodec codec = new WebpCodec();
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "deterministic-" + Guid.NewGuid().ToString("N") + ".webp");

        try
        {
            byte[] first = codec.EncodeImage(image, options);
            byte[] second = codec.EncodeImage(image, options);
            MemoryStream stream = new MemoryStream();
            codec.EncodeImageStream(image, stream, options);
            codec.EncodeImageFile(image, path, options);

            Assert.That(second, Is.EqualTo(first));
            Assert.That(stream.ToArray(), Is.EqualTo(first));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(first));
            WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(first, WebpStrictness.Strict);
            Assert.That(parsed.IsValid, Is.True);
            Assert.That(parsed.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
            Assert.That(parsed.Frames[1].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
            Assert.That(parsed.Frames[1].AlphaData, Is.Not.Null);
            Assert.That(parsed.Frames[2].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void ApplyDeterministicSerialization_PreservesChunkAndImagePayloadOrder()
    {
        WebpPixelBuffer pixels = NewPixels(false);
        byte[] imagePayload = WebpVp8LEncoder.Encode(pixels);
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk(WebpConstants.FourCcVp8LString, imagePayload));
        chunks.Add(new WebpRiffChunk("z001", new byte[] { 1 }));
        chunks.Add(new WebpRiffChunk("a001", new byte[] { 2, 3 }));
        byte[] source = WebpUnknownChunkMux.WritePreservingOrder(WebpConstants.FourCcWebpString, chunks);
        WebpEncodeOptions options = NewOptions(WebpEncodeMode.Lossless);

        byte[] first = WebpEncoder.ApplyDeterministicSerialization(source, options);
        byte[] second = WebpEncoder.ApplyDeterministicSerialization(source, options);
        WebpRiffParseResult riff = WebpRiffParser.Parse(first);

        Assert.That(second, Is.EqualTo(first));
        Assert.That(riff.IsValid, Is.True);
        Assert.That(riff.Chunks[0].FourCc, Is.EqualTo("VP8L"));
        Assert.That(riff.Chunks[1].FourCc, Is.EqualTo("z001"));
        Assert.That(riff.Chunks[2].FourCc, Is.EqualTo("a001"));
        Assert.That(ReadPayload(first, riff.Chunks[0]), Is.EqualTo(imagePayload));
    }

    static byte[] AssertStableStill(WebpPixelBuffer pixels, WebpEncodeOptions options)
    {
        WebpCodec codec = new WebpCodec();
        byte[] first = codec.Encode(pixels, options);
        byte[] second = codec.Encode(pixels, options);
        byte[] third = codec.Encode(pixels, options);
        Assert.That(second, Is.EqualTo(first));
        Assert.That(third, Is.EqualTo(first));
        return first;
    }

    static WebpEncodeOptions NewOptions(WebpEncodeMode mode)
    {
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = mode;
        options.Deterministic = true;
        return options;
    }

    static WebpImage NewMixedAnimation()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 4;
        image.CanvasHeight = 2;
        image.HasAnimation = true;
        image.LoopCount = 4;
        image.BackgroundColorBgra = 0xFF102030U;
        image.Metadata.Exif = new byte[] { 1, 2, 3 };
        image.UnknownChunks.Add(new WebpUnknownChunk("mix1", new byte[] { 9 }));

        WebpFrame lossless = NewFrame(0, 10, false);
        lossless.EncodeOptions = NewFrameOptions(WebpEncodeMode.Lossless);
        WebpFrame lossyAlpha = NewFrame(2, 20, true);
        lossyAlpha.EncodeOptions = NewFrameOptions(WebpEncodeMode.Lossy);
        lossyAlpha.EncodeOptions.Quality = 55;
        lossyAlpha.EncodeOptions.AlphaCompression = WebpAlphaCompressionMethod.Lossless;
        lossyAlpha.EncodeOptions.AlphaFilter = WebpAlphaFilterMode.Best;
        WebpFrame nearLossless = NewFrame(0, 30, false);
        nearLossless.EncodeOptions = NewFrameOptions(WebpEncodeMode.NearLossless);
        nearLossless.EncodeOptions.NearLosslessQuality = 50;
        image.Frames.Add(lossless);
        image.Frames.Add(lossyAlpha);
        image.Frames.Add(nearLossless);
        return image;
    }

    static WebpFrameEncodeOptions NewFrameOptions(WebpEncodeMode mode)
    {
        WebpFrameEncodeOptions options = new WebpFrameEncodeOptions();
        options.Mode = mode;
        return options;
    }

    static WebpFrame NewFrame(int x, int duration, bool alpha)
    {
        WebpFrame frame = new WebpFrame();
        frame.X = x;
        frame.Width = 2;
        frame.Height = 2;
        frame.DurationMilliseconds = duration;
        frame.Pixels = NewPixels(alpha);
        return frame;
    }

    static WebpPixelBuffer NewPixels(bool alpha)
    {
        WebpPixelBuffer buffer = new WebpPixelBuffer();
        buffer.Width = 2;
        buffer.Height = 2;
        buffer.Stride = 8;
        buffer.Rgba = new byte[]
        {
            10, 20, 30, 255,
            200, 100, 50, alpha ? (byte)80 : (byte)255,
            40, 150, 220, 255,
            230, 15, 90, alpha ? (byte)170 : (byte)255
        };
        return buffer;
    }

    static byte[] ReadPayload(byte[] data, WebpChunkInfo chunk)
    {
        return WebpEndianReader.ReadSlice(data, (int)chunk.PayloadOffset, (int)chunk.PayloadLength).ToArray();
    }
}
