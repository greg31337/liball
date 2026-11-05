using webp;

namespace webp.Tests;

[TestFixture]
public class WebpPublicModelInvariantTests
{
    [Test]
    public void DecodeRejectsInvalidEnumsAndFrameSelectionsBeforeProcessingPixels()
    {
        WebpCodec codec = new WebpCodec();
        byte[] data = codec.Encode(NewPixels(1, 1), new WebpEncodeOptions());
        WebpDecodeOptions strictness = new WebpDecodeOptions();
        strictness.Strictness = (WebpStrictness)99;
        WebpDecodeOptions unknownPolicy = new WebpDecodeOptions();
        unknownPolicy.UnknownChunkPolicy = (WebpUnknownChunkPolicy)99;
        WebpDecodeOptions animationMode = new WebpDecodeOptions();
        animationMode.AnimationDecodeMode = (WebpAnimationDecodeMode)99;
        WebpDecodeOptions negativeFrame = new WebpDecodeOptions();
        negativeFrame.FrameIndex = -2;
        WebpDecodeOptions missingStillFrame = new WebpDecodeOptions();
        missingStillFrame.FrameIndex = 1;
        WebpDecodeOptions incompatible = new WebpDecodeOptions();
        incompatible.FrameIndex = 0;
        incompatible.AnimationDecodeMode = WebpAnimationDecodeMode.CompositedFrames;

        Assert.Throws<ArgumentOutOfRangeException>(() => codec.Decode(data, strictness));
        Assert.Throws<ArgumentOutOfRangeException>(() => codec.Read(data, strictness));
        Assert.Throws<ArgumentOutOfRangeException>(() => codec.Validate(data, strictness));
        Assert.Throws<ArgumentOutOfRangeException>(() => codec.Decode(data, unknownPolicy));
        Assert.Throws<ArgumentOutOfRangeException>(() => codec.Decode(data, animationMode));
        Assert.Throws<ArgumentOutOfRangeException>(() => codec.Decode(data, negativeFrame));
        Assert.Throws<ArgumentOutOfRangeException>(() => codec.Decode(data, missingStillFrame));
        Assert.Throws<ArgumentException>(() => codec.Decode(data, incompatible));
    }

    [Test]
    public void EncodeRejectsInvalidGlobalSettingsEvenWhenSelectedModeWouldNotUseThem()
    {
        WebpCodec codec = new WebpCodec();
        WebpPixelBuffer pixels = NewPixels(1, 1);
        WebpEncodeOptions quality = NewLosslessOptions();
        quality.Quality = 101;
        WebpEncodeOptions alphaCompression = NewLosslessOptions();
        alphaCompression.AlphaCompression = (WebpAlphaCompressionMethod)99;
        WebpEncodeOptions alphaFilter = NewLosslessOptions();
        alphaFilter.AlphaFilter = (WebpAlphaFilterMode)99;
        WebpEncodeOptions nanPsnr = NewLosslessOptions();
        nanPsnr.TargetPsnr = double.NaN;
        WebpEncodeOptions competingTargets = NewLosslessOptions();
        competingTargets.TargetSize = 100;
        competingTargets.TargetPsnr = 30.0;

        Assert.Throws<ArgumentOutOfRangeException>(() => codec.Encode(pixels, quality));
        Assert.Throws<ArgumentOutOfRangeException>(() => codec.Encode(pixels, alphaCompression));
        Assert.Throws<ArgumentOutOfRangeException>(() => codec.Encode(pixels, alphaFilter));
        Assert.Throws<ArgumentOutOfRangeException>(() => codec.Encode(pixels, nanPsnr));
        Assert.Throws<ArgumentException>(() => codec.Encode(pixels, competingTargets));
    }

    [Test]
    public void EncodeRejectsInvalidPixelDimensionsStridesAndFrameShapes()
    {
        WebpCodec codec = new WebpCodec();
        WebpPixelBuffer negativeWidth = NewPixels(1, 1);
        negativeWidth.Width = -1;
        WebpPixelBuffer shortStride = NewPixels(2, 1);
        shortStride.Stride = 7;
        WebpImage incompleteCanvas = NewStillImage();
        incompleteCanvas.CanvasWidth = 1;
        WebpImage mismatchedFrame = NewStillImage();
        mismatchedFrame.Frames[0].Width = 2;
        mismatchedFrame.Frames[0].Height = 1;
        WebpImage missingSource = new WebpImage();
        missingSource.Frames.Add(new WebpFrame());

        Assert.Throws<ArgumentOutOfRangeException>(() => codec.Encode(negativeWidth, new WebpEncodeOptions()));
        Assert.Throws<ArgumentException>(() => codec.Encode(shortStride, new WebpEncodeOptions()));
        Assert.Throws<ArgumentException>(() => codec.EncodeImage(incompleteCanvas, new WebpEncodeOptions()));
        Assert.Throws<ArgumentException>(() => codec.EncodeImage(mismatchedFrame, new WebpEncodeOptions()));
        Assert.Throws<ArgumentException>(() => codec.EncodeImage(missingSource, new WebpEncodeOptions()));
    }

    [Test]
    public void EncodeRejectsInvalidAnimationGeometryAndEnums()
    {
        WebpCodec codec = new WebpCodec();
        WebpImage oddOffset = NewAnimationImage();
        oddOffset.Frames[0].X = 1;
        WebpImage outsideCanvas = NewAnimationImage();
        outsideCanvas.Frames[0].X = 2;
        WebpImage duration = NewAnimationImage();
        duration.Frames[0].DurationMilliseconds = 16777216;
        WebpImage blend = NewAnimationImage();
        blend.Frames[0].BlendMode = (WebpFrameBlendMode)99;
        WebpImage dispose = NewAnimationImage();
        dispose.Frames[0].DisposeMode = (WebpFrameDisposeMode)99;
        WebpImage bitstream = NewAnimationImage();
        bitstream.Frames[0].BitstreamKind = (WebpBitstreamKind)99;

        Assert.Throws<ArgumentException>(() => codec.EncodeImage(oddOffset, new WebpEncodeOptions()));
        Assert.Throws<ArgumentException>(() => codec.EncodeImage(outsideCanvas, new WebpEncodeOptions()));
        Assert.Throws<ArgumentOutOfRangeException>(() => codec.EncodeImage(duration, new WebpEncodeOptions()));
        Assert.Throws<ArgumentOutOfRangeException>(() => codec.EncodeImage(blend, new WebpEncodeOptions()));
        Assert.Throws<ArgumentOutOfRangeException>(() => codec.EncodeImage(dispose, new WebpEncodeOptions()));
        Assert.Throws<ArgumentOutOfRangeException>(() => codec.EncodeImage(bitstream, new WebpEncodeOptions()));
    }

    [Test]
    public void EncodeRejectsInvalidPerFrameSettingsForPixelBackedFrames()
    {
        WebpCodec codec = new WebpCodec();
        WebpImage image = NewAnimationImage();
        WebpFrameEncodeOptions frameOptions = new WebpFrameEncodeOptions();
        frameOptions.Quality = -1;
        image.Frames[0].EncodeOptions = frameOptions;

        Assert.Throws<ArgumentOutOfRangeException>(() => codec.EncodeImage(image, new WebpEncodeOptions()));
    }

    [Test]
    public void EncodeRejectsNullModelMembersAndInvalidUnknownChunkFourCcValues()
    {
        WebpCodec codec = new WebpCodec();
        WebpImage nullMetadata = NewStillImage();
        nullMetadata.Metadata = null!;
        WebpImage nullFrameChunks = NewStillImage();
        nullFrameChunks.Frames[0].UnknownChunks = null!;
        WebpImage shortFourCc = NewStillImage();
        WebpUnknownChunk shortChunk = new WebpUnknownChunk("test", new byte[] { 1 });
        shortChunk.FourCc = "abc";
        shortFourCc.UnknownChunks.Add(shortChunk);
        WebpImage knownFourCc = NewStillImage();
        WebpUnknownChunk knownChunk = new WebpUnknownChunk("test", new byte[] { 1 });
        knownChunk.FourCc = "VP8L";
        knownFourCc.UnknownChunks.Add(knownChunk);
        WebpImage nullPayload = NewStillImage();
        WebpUnknownChunk unknown = new WebpUnknownChunk("test", new byte[] { 1 });
        unknown.Data = null!;
        nullPayload.UnknownChunks.Add(unknown);
        WebpEncodeOptions strip = new WebpEncodeOptions();
        strip.UnknownChunkPolicy = WebpUnknownChunkPolicy.Strip;

        Assert.Throws<ArgumentNullException>(() => codec.EncodeImage(nullMetadata, new WebpEncodeOptions()));
        Assert.Throws<ArgumentNullException>(() => codec.EncodeImage(nullFrameChunks, new WebpEncodeOptions()));
        Assert.Throws<ArgumentException>(() => new WebpUnknownChunk("abc", new byte[] { 1 }));
        Assert.Throws<ArgumentException>(() => new WebpUnknownChunk("VP8L", new byte[] { 1 }));
        Assert.Throws<ArgumentException>(() => codec.EncodeImage(shortFourCc, strip));
        Assert.Throws<ArgumentException>(() => codec.EncodeImage(knownFourCc, new WebpEncodeOptions()));
        Assert.Throws<ArgumentNullException>(() => codec.EncodeImage(nullPayload, new WebpEncodeOptions()));
    }

    [Test]
    public void MetadataOperationsRejectEmptyAndOversizedPayloads()
    {
        WebpCodec codec = new WebpCodec();
        WebpImage emptyMetadata = NewStillImage();
        emptyMetadata.Metadata.Exif = Array.Empty<byte>();
        WebpImage oversizedMetadata = NewStillImage();
        oversizedMetadata.Metadata.IccProfile = new byte[] { 1, 2 };
        WebpEncodeOptions limited = new WebpEncodeOptions();
        limited.Limits.MaxMetadataBytes = 1;
        byte[] data = codec.Encode(NewPixels(1, 1), new WebpEncodeOptions());
        WebpMetadata replacement = new WebpMetadata();
        replacement.Xmp = Array.Empty<byte>();

        Assert.Throws<ArgumentException>(() => codec.EncodeImage(emptyMetadata, new WebpEncodeOptions()));
        Assert.Throws<ArgumentException>(() => codec.EncodeImage(oversizedMetadata, limited));
        Assert.Throws<ArgumentException>(() => codec.ReplaceMetadata(data, replacement));
    }

    static WebpEncodeOptions NewLosslessOptions()
    {
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossless;
        return options;
    }

    static WebpImage NewStillImage()
    {
        WebpImage image = new WebpImage();
        WebpFrame frame = new WebpFrame();
        frame.Width = 1;
        frame.Height = 1;
        frame.Pixels = NewPixels(1, 1);
        image.Frames.Add(frame);
        return image;
    }

    static WebpImage NewAnimationImage()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 2;
        image.CanvasHeight = 2;
        image.HasAnimation = true;
        WebpFrame frame = new WebpFrame();
        frame.Width = 1;
        frame.Height = 1;
        frame.DurationMilliseconds = 10;
        frame.Pixels = NewPixels(1, 1);
        image.Frames.Add(frame);
        return image;
    }

    static WebpPixelBuffer NewPixels(int width, int height)
    {
        WebpPixelBuffer pixels = new WebpPixelBuffer();
        pixels.Width = width;
        pixels.Height = height;
        pixels.Stride = width * 4;
        pixels.Rgba = new byte[pixels.Stride * height];
        int index = 3;
        while (index < pixels.Rgba.Length)
        {
            pixels.Rgba[index] = 255;
            index = index + 4;
        }

        return pixels;
    }
}
