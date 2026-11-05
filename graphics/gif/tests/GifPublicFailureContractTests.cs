using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPublicFailureContractTests
{
    [Test]
    public void IdentifyFailureReturnsStructuredDiagnostic()
    {
        IGifDecoder decoder = new GifCodec();

        GifInfo info = decoder.IdentifyBytes(new byte[] { 1, 2, 3, 4, 5, 6 }, null);

        Assert.That(info.Success, Is.False);
        Assert.That(info.Error.Code, Is.EqualTo(GifErrorCode.InvalidSignature));
        Assert.That(info.Error.Position.ByteOffset, Is.EqualTo(6));
        Assert.That(info.Error.Position.FrameIndex, Is.EqualTo(-1));
        Assert.That(info.Error.Position.BlockName, Is.EqualTo("Header"));
    }

    [Test]
    public void IdentifyFailurePreservesMetadataFromEarlierValidBlocks()
    {
        IGifDecoder decoder = new GifCodec();

        GifInfo info = decoder.IdentifyBytes(CreateGifWithInvalidSecondFrameRectangle(), null);

        Assert.That(info.Success, Is.False);
        Assert.That(info.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(info.Width, Is.EqualTo(2));
        Assert.That(info.Height, Is.EqualTo(1));
        Assert.That(info.FrameCount, Is.EqualTo(1));
        Assert.That(info.HasAnimation, Is.False);
        Assert.That(info.HasComments, Is.True);
        Assert.That(info.EstimatedDecodedBytes, Is.EqualTo(4));
        Assert.That(info.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(info.Error.Position.FrameIndex, Is.EqualTo(1));
        Assert.That(info.Error.Position.BlockName, Is.EqualTo("ImageDescriptor"));
    }

    [Test]
    public void DecodeFailureThrowsGifExceptionWithDiagnosticContext()
    {
        IGifDecoder decoder = new GifCodec();

        GifException exception = Assert.Throws<GifException>(() => decoder.DecodeBytes(new byte[] { 1, 2, 3, 4, 5, 6 }, null));
        GifErrorDiagnostic diagnostic = exception.ToDiagnostic();

        Assert.That(exception.ErrorCode, Is.EqualTo(GifErrorCode.InvalidSignature));
        Assert.That(exception.ByteOffset, Is.EqualTo(6));
        Assert.That(exception.FrameIndex, Is.EqualTo(-1));
        Assert.That(exception.BlockName, Is.EqualTo("Header"));
        Assert.That(diagnostic.Code, Is.EqualTo(exception.ErrorCode));
        Assert.That(diagnostic.Position.ByteOffset, Is.EqualTo(exception.ByteOffset));
        Assert.That(diagnostic.Position.FrameIndex, Is.EqualTo(exception.FrameIndex));
        Assert.That(diagnostic.Position.BlockName, Is.EqualTo(exception.BlockName));
    }

    [Test]
    public void AnimationDecodeFailureIncludesFrameContext()
    {
        IGifDecoder decoder = new GifCodec();

        GifException exception = Assert.Throws<GifException>(() => decoder.DecodeAnimationBytes(CreateGifWithoutLzwEndCode(), null));

        Assert.That(exception.ErrorCode, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(exception.FrameIndex, Is.EqualTo(0));
        Assert.That(exception.BlockName, Does.StartWith("Lzw"));
    }

    [Test]
    public void TolerantAnimationDecodeReturnsOutputAndContextualWarning()
    {
        IGifDecoder decoder = new GifCodec();
        GifDecodeOptions options = new GifDecodeOptions();
        options.Strictness = GifStrictness.Tolerant;

        GifAnimation animation = decoder.DecodeAnimationBytes(CreateGifWithoutLzwEndCode(), options);

        Assert.That(animation.Frames.Length, Is.EqualTo(1));
        Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 0, 0, 0, 255 }));
        Assert.That(animation.Warnings.Length, Is.EqualTo(1));
        Assert.That(animation.Warnings[0].Code, Is.EqualTo(GifWarningCode.ToleratedMalformedBlock));
        Assert.That(animation.Warnings[0].Position.FrameIndex, Is.EqualTo(0));
        Assert.That(animation.Warnings[0].Position.BlockName, Is.EqualTo("LzwDecoder"));
    }

    [Test]
    public void CoalesceFailureThrowsGifException()
    {
        IGifDecoder decoder = new GifCodec();
        GifDocument document = new GifDocument();
        document.LogicalScreen = new GifLogicalScreen(1, 1);

        GifException exception = Assert.Throws<GifException>(() => decoder.Coalesce(document, null));

        Assert.That(exception.ErrorCode, Is.EqualTo(GifErrorCode.MissingColorTable));
        Assert.That(exception.BlockName, Is.EqualTo("Compositor"));
    }

    [Test]
    public void ByteArrayEncodeFailureThrowsButResultEncodeRetainsDiagnostic()
    {
        GifCodec codec = new GifCodec();
        IGifEncoder encoder = codec;
        GifBitmap invalidBitmap = codec.CreateIndexedBitmap(1, 1, new byte[] { 2 }, CreatePalette());

        GifException exception = Assert.Throws<GifException>(() => encoder.Encode(new GifDocument(), null));
        GifEncodeResult result = encoder.EncodeIndexedStill(invalidBitmap, null, false, -1);

        Assert.That(exception.ErrorCode, Is.Not.EqualTo(GifErrorCode.None));
        Assert.That(exception.BlockName, Is.Not.Empty);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidPaletteIndex));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("GifEncode"));
    }

    [Test]
    public void WriteValidationFailureThrowsBeforeWritingDestination()
    {
        IGifEncoder encoder = new GifCodec();
        MemoryStream destination = new MemoryStream();

        GifException exception = Assert.Throws<GifException>(() => encoder.Write(destination, new GifDocument(), null));

        Assert.That(exception.ErrorCode, Is.Not.EqualTo(GifErrorCode.None));
        Assert.That(destination.Length, Is.EqualTo(0));
    }

    [Test]
    public void UnwritableDestinationThrowsGifException()
    {
        IGifEncoder encoder = new GifCodec();
        MemoryStream destination = new MemoryStream(new byte[0], false);

        GifException exception = Assert.Throws<GifException>(() => encoder.Write(destination, null, null));

        Assert.That(exception.ErrorCode, Is.EqualTo(GifErrorCode.DestinationWriteFailed));
        Assert.That(exception.BlockName, Is.EqualTo("DestinationStream"));
    }

    [Test]
    public void ValidationFailureReturnsDiagnosticWithoutThrowing()
    {
        IGifTransformer transformer = new GifCodec();

        GifErrorDiagnostic diagnostic = transformer.ValidateDocument(new GifDocument(), null);

        Assert.That(diagnostic.Code, Is.Not.EqualTo(GifErrorCode.None));
        Assert.That(diagnostic.Position.BlockName, Is.Not.Empty);
    }

    [Test]
    public void TransformationFailureThrowsGifExceptionWithFrameContext()
    {
        IGifTransformer transformer = new GifCodec();
        GifAnimation animation = CreateAnimation();

        GifException exception = Assert.Throws<GifException>(() => transformer.SetDelays(animation, new int[] { 10, -1 }));

        Assert.That(exception.ErrorCode, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(exception.FrameIndex, Is.EqualTo(1));
        Assert.That(exception.BlockName, Is.EqualTo("GraphicControlExtension"));
        Assert.That(animation.Frames[0].DelayMilliseconds, Is.EqualTo(0));
        Assert.That(animation.Frames[1].DelayMilliseconds, Is.EqualTo(0));
    }

    [Test]
    public void OptimizationFailureThrowsGifExceptionWithFrameContext()
    {
        IGifTransformer transformer = new GifCodec();
        GifAnimation animation = CreateAnimation();
        animation.Frames[1].Bitmap = new GifBitmap(2, 1, new byte[] { 0, 0, 0, 255, 0, 0, 0, 255 });
        GifEncodeOptions options = new GifEncodeOptions();
        options.OptimizationMode = GifOptimizationMode.Deconstruct;

        GifException exception = Assert.Throws<GifException>(() => transformer.Optimize(animation, options));

        Assert.That(exception.ErrorCode, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(exception.FrameIndex, Is.EqualTo(1));
        Assert.That(exception.BlockName, Is.EqualTo("Optimizer"));
    }

    static GifPalette CreatePalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
    }

    static GifAnimation CreateAnimation()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(0),
            CreateFrame(1)
        };
        return animation;
    }

    static GifAnimationFrame CreateFrame(int frameIndex)
    {
        GifAnimationFrame frame = new GifAnimationFrame();
        frame.Bitmap = new GifBitmap(1, 1, new byte[] { 0, 0, 0, 255 });
        frame.FrameIndex = frameIndex;
        return frame;
    }

    static byte[] CreateGifWithoutLzwEndCode()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0x80, 0, 0,
            0, 0, 0,
            255, 255, 255,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 1, 0x04, 0,
            0x3B
        };
    }

    static byte[] CreateGifWithInvalidSecondFrameRectangle()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            2, 0, 1, 0, 0, 0, 0,
            0x21, 0xFE, 2, 79, 75, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 1, 77, 0,
            0x2C, 1, 0, 0, 0, 2, 0, 1, 0, 0,
            2, 1, 88, 0,
            0x3B
        };
    }
}
