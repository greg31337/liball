using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifOverflowSafetyTests
{
    [Test]
    public void RgbaBitmap_ExtremeWidthsUseInvalidStrideSentinelWithoutThrowing()
    {
        GifBitmap positive = new GifBitmap(int.MaxValue, int.MaxValue, new byte[0]);
        GifBitmap negative = new GifBitmap(int.MinValue, 1, new byte[0]);

        Assert.That(positive.Stride, Is.EqualTo(0));
        Assert.That(negative.Stride, Is.EqualTo(0));
        Assert.That(Gif.Validate(positive).Code, Is.Not.EqualTo(GifErrorCode.None));
        Assert.That(Gif.Validate(negative).Code, Is.Not.EqualTo(GifErrorCode.None));
    }

    [Test]
    public void Compositor_ExtremeCanvasDimensionsReturnMemoryDiagnostic()
    {
        GifLogicalScreen screen = new GifLogicalScreen(int.MaxValue, int.MaxValue);

        GifCompositingCanvasResult result = new GifCompositor().ValidateCanvasDimensions(screen);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MemoryLimitExceeded));
    }

    [Test]
    public void PublicAnimationConstruction_SaturatesOverflowingCanvasExtentForValidation()
    {
        GifBitmap bitmap = GifBitmap.CreateIndexed(2, 1, new byte[] { 0, 0 }, CreatePalette());
        GifAnimationOptions options = new GifAnimationOptions();
        options.FrameLefts = new int[] { int.MaxValue };

        GifAnimation animation = Gif.CreateAnimationForPublicApi(new GifBitmap[] { bitmap }, options);

        Assert.That(animation.Width, Is.EqualTo(int.MaxValue));
        Assert.That(animation.Height, Is.EqualTo(1));
    }

    [TestCase(int.MaxValue)]
    [TestCase(int.MinValue)]
    public void Compositor_ExtremeFrameOffsetsAreClippedWithoutArithmeticOverflow(int left)
    {
        GifBitmap canvas = new GifBitmap(1, 1, new byte[] { 1, 2, 3, 255 });
        GifBitmap frame = new GifBitmap(2, 1, new byte[] { 9, 8, 7, 255, 6, 5, 4, 255 });
        GifCompositor compositor = new GifCompositor();

        GifFrameOverlayResult result = compositor.OverlayFrameAtPosition(canvas, frame, left, 0);
        compositor.FillRectangle(canvas, left, 0, int.MaxValue, 1, 0, 0, 0, 0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Clipped, Is.True);
        Assert.That(canvas.RgbaPixels, Is.EqualTo(new byte[] { 1, 2, 3, 255 }));
    }

    [Test]
    public void Timeline_ExtremeRawDelayReturnsDiagnosticInsteadOfOverflowing()
    {
        GifDocument document = new GifDocument();
        GifFrame frame = new GifFrame(0, 0, 0, 0);
        frame.DelayHundredths = int.MinValue;
        document.Frames = new GifFrame[] { frame };

        GifTimelineResult result = new GifTimeline().CalculateDocument(document);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
    }

    [Test]
    public void DelayConversion_RoundsMaximumIntWithoutOverflowing()
    {
        int result = new GifFrameDelayEncoder().ConvertMillisecondsToHundredths(int.MaxValue);

        Assert.That(result, Is.EqualTo(214748365));
    }

    [Test]
    public void LzwOutputCounter_RejectsAdditionPastLongMaximum()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();
        GifLzwOutputCountResult configuration = decoder.ConfigureOutputValidation(long.MaxValue, long.MaxValue);
        GifLzwOutputCountResult maximum = decoder.RecordOutputCount(long.MaxValue);

        GifLzwOutputCountResult overflow = decoder.RecordOutputCount(1);

        Assert.That(configuration.Success, Is.True);
        Assert.That(maximum.Success, Is.True);
        Assert.That(overflow.Success, Is.False);
        Assert.That(overflow.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelLimitExceeded));
        Assert.That(overflow.OutputCount, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void LzwBitReader_ExtremeOffsetReportsNoRemainingBits()
    {
        GifLzwBitReader reader = new GifLzwBitReader(new byte[] { 0 });
        reader.ByteOffset = int.MaxValue;

        Assert.That(reader.RemainingBits(), Is.EqualTo(0));
    }

    [Test]
    public void EstimatedDecodedBytes_SaturatesInsteadOfOverflowing()
    {
        GifFrame frame = new GifFrame(0, 0, int.MaxValue, int.MaxValue);

        long result = new GifIdentifier().CalculateEstimatedDecodedBytes(new GifFrame[] { frame });

        Assert.That(result, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void GrayscaleShade_ExtremeIndexesClampToByteRange()
    {
        GifGrayscalePaletteGenerator generator = new GifGrayscalePaletteGenerator();

        Assert.That(generator.CalculateShade(int.MinValue, 256), Is.EqualTo(0));
        Assert.That(generator.CalculateShade(int.MaxValue, 256), Is.EqualTo(255));
    }

    [Test]
    public void MedianCutComparison_ExtremeOriginalIndexesDoesNotOverflow()
    {
        GifMedianCutColor first = new GifMedianCutColor(0, 0, 0, int.MinValue);
        GifMedianCutColor second = new GifMedianCutColor(0, 0, 0, int.MaxValue);

        int result = new GifMedianCutQuantizer().CompareColors(first, second, 0);

        Assert.That(result, Is.EqualTo(-1));
    }

    static GifPalette CreatePalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
    }
}
