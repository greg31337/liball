using webp;

namespace webp.Tests;

[TestFixture]
public class WebpCodecSpanTests
{
    [Test]
    public void DecodeIdentifyAndValidateSpanEntryPointsHonorTheSuppliedSlice()
    {
        WebpCodec codec = new WebpCodec();
        IWebpDecoder decoder = codec;
        IWebpInfoReader infoReader = codec;
        IWebpValidator validator = codec;
        WebpPixelBuffer source = NewBuffer(2, 1, 10, 20, 30, 255);
        WebpEncodeOptions encodeOptions = new WebpEncodeOptions();
        encodeOptions.Mode = WebpEncodeMode.Lossless;
        byte[] encoded = codec.Encode(source, encodeOptions);
        byte[] wrapped = Wrap(encoded);
        ReadOnlySpan<byte> data = wrapped.AsSpan(3, encoded.Length);
        WebpDecodeOptions decodeOptions = new WebpDecodeOptions();

        WebpDecodeResult decoded = decoder.DecodeSpan(data, decodeOptions);
        WebpInfo info = infoReader.ReadSpan(data, decodeOptions);
        WebpValidationResult validation = validator.ValidateSpan(data, decodeOptions);

        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(source.Rgba));
        Assert.That(info.Width, Is.EqualTo(2));
        Assert.That(info.Height, Is.EqualTo(1));
        Assert.That(info.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(validation.IsValid, Is.True);
    }

    [Test]
    public void MetadataSpanEntryPointsReadReplaceSetAndStripPayloads()
    {
        WebpCodec codec = new WebpCodec();
        IWebpMux mux = codec;
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossless;
        byte[] original = codec.Encode(NewBuffer(1, 1, 1, 2, 3, 255), options);
        WebpMetadata replacement = new WebpMetadata();
        replacement.IccProfile = new byte[] { 1 };
        replacement.Exif = new byte[] { 2 };
        replacement.Xmp = new byte[] { 3 };

        byte[] withMetadata = mux.ReplaceMetadataSpan(original.AsSpan(), replacement);
        WebpMetadata extracted = mux.GetMetadataSpan(withMetadata.AsSpan());
        byte[] withIcc = mux.SetIccProfileSpan(withMetadata.AsSpan(), new byte[] { 4, 5 });
        byte[] withExif = mux.SetExifSpan(withIcc.AsSpan(), new byte[] { 6, 7 });
        byte[] withXmp = mux.SetXmpSpan(withExif.AsSpan(), new byte[] { 8, 9 });
        WebpMetadata changed = mux.GetMetadataSpan(withXmp.AsSpan());
        byte[] stripped = mux.StripMetadataSpan(withXmp.AsSpan());
        WebpMetadata empty = mux.GetMetadataSpan(stripped.AsSpan());

        Assert.That(extracted.IccProfile, Is.EqualTo(new byte[] { 1 }));
        Assert.That(extracted.Exif, Is.EqualTo(new byte[] { 2 }));
        Assert.That(extracted.Xmp, Is.EqualTo(new byte[] { 3 }));
        Assert.That(changed.IccProfile, Is.EqualTo(new byte[] { 4, 5 }));
        Assert.That(changed.Exif, Is.EqualTo(new byte[] { 6, 7 }));
        Assert.That(changed.Xmp, Is.EqualTo(new byte[] { 8, 9 }));
        Assert.That(empty.IccProfile, Is.Null);
        Assert.That(empty.Exif, Is.Null);
        Assert.That(empty.Xmp, Is.Null);
    }

    [Test]
    public void AnimationSpanEntryPointsExtractFramesAndRewriteGlobalFields()
    {
        WebpCodec codec = new WebpCodec();
        IWebpMux mux = codec;
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossless;
        byte[] encoded = codec.EncodeImage(NewAnimation(), options);
        byte[] wrapped = Wrap(encoded);
        ReadOnlySpan<byte> data = wrapped.AsSpan(3, encoded.Length);

        List<WebpFrame> frames = mux.ExtractFramesSpan(data);
        WebpFrame second = mux.ExtractFrameSpan(data, 1);
        byte[] loopChanged = mux.SetAnimationLoopCountSpan(data, 7);
        byte[] backgroundChanged = mux.SetAnimationBackgroundColorSpan(data, 0xAABBCCDDU);
        byte[] bothChanged = mux.SetAnimationLoopAndBackgroundSpan(data, 0, 0x01020304U);
        WebpExtendedContainerParseResult loopResult = WebpExtendedContainerParser.Parse(loopChanged, WebpStrictness.Strict);
        WebpExtendedContainerParseResult backgroundResult = WebpExtendedContainerParser.Parse(backgroundChanged, WebpStrictness.Strict);
        WebpExtendedContainerParseResult bothResult = WebpExtendedContainerParser.Parse(bothChanged, WebpStrictness.Strict);

        Assert.That(frames, Has.Count.EqualTo(2));
        Assert.That(second.X, Is.EqualTo(2));
        Assert.That(second.DurationMilliseconds, Is.EqualTo(40));
        Assert.That(second.EncodedBitstream, Is.Not.Null);
        Assert.That(loopResult.AnimationLoopCount, Is.EqualTo(7));
        Assert.That(loopResult.AnimationBackgroundColorBgra, Is.EqualTo(0xFF010203U));
        Assert.That(backgroundResult.AnimationLoopCount, Is.EqualTo(4));
        Assert.That(backgroundResult.AnimationBackgroundColorBgra, Is.EqualTo(0xAABBCCDDU));
        Assert.That(bothResult.AnimationLoopCount, Is.EqualTo(0));
        Assert.That(bothResult.AnimationBackgroundColorBgra, Is.EqualTo(0x01020304U));
        Assert.That(bothResult.Frames, Has.Count.EqualTo(2));
    }

    static byte[] Wrap(byte[] data)
    {
        byte[] wrapped = new byte[data.Length + 6];
        wrapped[0] = 90;
        wrapped[1] = 91;
        wrapped[2] = 92;
        Array.Copy(data, 0, wrapped, 3, data.Length);
        wrapped[wrapped.Length - 3] = 93;
        wrapped[wrapped.Length - 2] = 94;
        wrapped[wrapped.Length - 1] = 95;
        return wrapped;
    }

    static WebpImage NewAnimation()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 3;
        image.CanvasHeight = 1;
        image.HasAnimation = true;
        image.LoopCount = 4;
        image.BackgroundColorBgra = 0xFF010203U;
        image.Frames.Add(NewFrame(0, 25, NewBuffer(1, 1, 255, 0, 0, 255)));
        image.Frames.Add(NewFrame(2, 40, NewBuffer(1, 1, 0, 0, 255, 255)));
        return image;
    }

    static WebpFrame NewFrame(int x, int duration, WebpPixelBuffer pixels)
    {
        WebpFrame frame = new WebpFrame();
        frame.X = x;
        frame.Y = 0;
        frame.Width = 1;
        frame.Height = 1;
        frame.DurationMilliseconds = duration;
        frame.BlendMode = WebpFrameBlendMode.Replace;
        frame.DisposeMode = WebpFrameDisposeMode.DoNotDispose;
        frame.BitstreamKind = WebpBitstreamKind.Lossless;
        frame.Pixels = pixels;
        return frame;
    }

    static WebpPixelBuffer NewBuffer(int width, int height, byte red, byte green, byte blue, byte alpha)
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(width, height);
        int offset = 0;
        while (offset < buffer.Rgba.Length)
        {
            buffer.Rgba[offset] = red;
            buffer.Rgba[offset + 1] = green;
            buffer.Rgba[offset + 2] = blue;
            buffer.Rgba[offset + 3] = alpha;
            offset = offset + 4;
        }

        return buffer;
    }
}
