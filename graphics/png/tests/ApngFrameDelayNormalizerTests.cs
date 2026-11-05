using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ApngFrameDelayNormalizerTests
{
    [Test]
    public void Normalize_ReplacesZeroDenominatorForFrameControl()
    {
        PngFrameControl control = CreateFrameControl();
        control.DelayDenominator = 0;

        PngFrameControl normalized = PngApngFrameDelayNormalizer.Normalize(control);

        Assert.That(normalized.DelayNumerator, Is.EqualTo(1));
        Assert.That(normalized.DelayDenominator, Is.EqualTo(PngApngFrameDelayNormalizer.DefaultDelayDenominator));
        Assert.That(control.DelayDenominator, Is.EqualTo(0));
    }

    [Test]
    public void Normalize_PreservesNonZeroDenominatorForFrameControl()
    {
        PngFrameControl control = CreateFrameControl();
        control.DelayDenominator = 24;

        PngFrameControl normalized = PngApngFrameDelayNormalizer.Normalize(control);

        Assert.That(normalized.DelayDenominator, Is.EqualTo(24));
        Assert.That(normalized.Width, Is.EqualTo(control.Width));
        Assert.That(normalized.Height, Is.EqualTo(control.Height));
        Assert.That(normalized.DisposeOperation, Is.EqualTo(control.DisposeOperation));
        Assert.That(normalized.BlendOperation, Is.EqualTo(control.BlendOperation));
    }

    [Test]
    public void Normalize_ReplacesZeroDenominatorForFrame()
    {
        PngFrame frame = CreateFrame();
        frame.DelayDenominator = 0;

        PngFrame normalized = PngApngFrameDelayNormalizer.Normalize(frame);

        Assert.That(normalized.DelayNumerator, Is.EqualTo(2));
        Assert.That(normalized.DelayDenominator, Is.EqualTo(PngApngFrameDelayNormalizer.DefaultDelayDenominator));
        Assert.That(normalized.Width, Is.EqualTo(frame.Width));
        Assert.That(normalized.Height, Is.EqualTo(frame.Height));
        Assert.That(normalized.Pixels, Is.SameAs(frame.Pixels));
        Assert.That(normalized.UsesDefaultImage, Is.EqualTo(frame.UsesDefaultImage));
        Assert.That(frame.DelayDenominator, Is.EqualTo(0));
    }

    [Test]
    public void CalculateDelayMilliseconds_UsesNormalizedDenominator()
    {
        PngFrameControl control = CreateFrameControl();
        control.DelayNumerator = 1;
        control.DelayDenominator = 0;
        PngFrame frame = CreateFrame();
        frame.DelayNumerator = 3;
        frame.DelayDenominator = 2;

        Assert.That(PngApngFrameDelayNormalizer.CalculateDelayMilliseconds(control), Is.EqualTo(10));
        Assert.That(PngApngFrameDelayNormalizer.CalculateDelayMilliseconds(frame), Is.EqualTo(1500));
        Assert.That(PngApngFrameDelayNormalizer.CalculateDelayMilliseconds(0, 0), Is.EqualTo(0));
        Assert.That(PngApngFrameDelayNormalizer.CalculateDelayMilliseconds(1, 3), Is.EqualTo(333));
    }

    [Test]
    public void NormalizeDenominator_ReplacesZeroAndPreservesPositiveValues()
    {
        Assert.That(PngApngFrameDelayNormalizer.NormalizeDenominator(0), Is.EqualTo(100));
        Assert.That(PngApngFrameDelayNormalizer.NormalizeDenominator(24), Is.EqualTo(24));
    }

    [Test]
    public void RejectsInvalidInputs()
    {
        PngFrame negativeNumerator = CreateFrame();
        negativeNumerator.DelayNumerator = -1;
        PngFrame negativeDenominator = CreateFrame();
        negativeDenominator.DelayDenominator = -1;

        Assert.Throws<ArgumentNullException>(() => PngApngFrameDelayNormalizer.Normalize((PngFrameControl)null!));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDelayNormalizer.Normalize((PngFrame)null!));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDelayNormalizer.CalculateDelayMilliseconds((PngFrameControl)null!));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDelayNormalizer.CalculateDelayMilliseconds((PngFrame)null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDelayNormalizer.Normalize(negativeNumerator));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDelayNormalizer.Normalize(negativeDenominator));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDelayNormalizer.NormalizeDenominator(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDelayNormalizer.CalculateDelayMilliseconds(-1, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDelayNormalizer.CalculateDelayMilliseconds(1, -1));
        Assert.Throws<OverflowException>(() => PngApngFrameDelayNormalizer.CalculateDelayMilliseconds(int.MaxValue, 1));
    }

    static PngFrameControl CreateFrameControl()
    {
        PngFrameControl control = new PngFrameControl();
        control.SequenceNumber = 0;
        control.Width = 10;
        control.Height = 20;
        control.XOffset = 1;
        control.YOffset = 2;
        control.DelayNumerator = 1;
        control.DelayDenominator = 24;
        control.DisposeOperation = PngApngDisposeOperation.Background;
        control.BlendOperation = PngApngBlendOperation.Over;
        return control;
    }

    static PngFrame CreateFrame()
    {
        PngFrame frame = new PngFrame();
        frame.Width = 10;
        frame.Height = 20;
        frame.XOffset = 1;
        frame.YOffset = 2;
        frame.DelayNumerator = 2;
        frame.DelayDenominator = 24;
        frame.DisposeOperation = PngApngDisposeOperation.Background;
        frame.BlendOperation = PngApngBlendOperation.Over;
        frame.Pixels = new PngPixelBuffer();
        frame.UsesDefaultImage = true;
        return frame;
    }
}
