using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class SizeCalculatorTests
{
    [Test]
    public void CalculateRowBytes_HandlesPackedPngRows()
    {
        Assert.That(PngSizeCalculator.CalculateRowBytes(1, PngColorType.Grayscale, PngBitDepth.One), Is.EqualTo(1));
        Assert.That(PngSizeCalculator.CalculateRowBytes(8, PngColorType.Grayscale, PngBitDepth.One), Is.EqualTo(1));
        Assert.That(PngSizeCalculator.CalculateRowBytes(9, PngColorType.Grayscale, PngBitDepth.One), Is.EqualTo(2));

        Assert.That(PngSizeCalculator.CalculateRowBytes(3, PngColorType.IndexedColor, PngBitDepth.Two), Is.EqualTo(1));
        Assert.That(PngSizeCalculator.CalculateRowBytes(5, PngColorType.IndexedColor, PngBitDepth.Four), Is.EqualTo(3));
    }

    [Test]
    public void CalculateRowBytes_HandlesMultiSamplePngRows()
    {
        Assert.That(PngSizeCalculator.CalculateRowBytes(10, PngColorType.Truecolor, PngBitDepth.Eight), Is.EqualTo(30));
        Assert.That(PngSizeCalculator.CalculateRowBytes(10, PngColorType.TruecolorAlpha, PngBitDepth.Eight), Is.EqualTo(40));
        Assert.That(PngSizeCalculator.CalculateRowBytes(10, PngColorType.GrayscaleAlpha, PngBitDepth.Sixteen), Is.EqualTo(40));
        Assert.That(PngSizeCalculator.CalculateRowBytes(10, PngColorType.Truecolor, PngBitDepth.Sixteen), Is.EqualTo(60));
    }

    [Test]
    public void CalculateImageBytes_MultipliesRowBytesByHeight()
    {
        Assert.That(PngSizeCalculator.CalculateImageBytes(10, 3, PngColorType.TruecolorAlpha, PngBitDepth.Eight), Is.EqualTo(120));
        Assert.That(PngSizeCalculator.CalculateImageBytes(9, 4, PngColorType.Grayscale, PngBitDepth.One), Is.EqualTo(8));
    }

    [Test]
    public void CalculateFrameBytes_UsesPixelFormatLayout()
    {
        Assert.That(PngSizeCalculator.CalculateFrameBytes(10, 2, PngPixelFormat.Rgba8), Is.EqualTo(80));
        Assert.That(PngSizeCalculator.CalculateFrameBytes(10, 2, PngPixelFormat.Rgba16), Is.EqualTo(160));
        Assert.That(PngSizeCalculator.CalculateFrameBytes(9, 2, PngPixelFormat.Indexed1), Is.EqualTo(4));
        Assert.That(PngSizeCalculator.CalculateFrameBytes(5, 3, PngPixelFormat.Grayscale4), Is.EqualTo(9));
    }

    [Test]
    public void Calculators_RejectInvalidDimensions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSizeCalculator.CalculateRowBytes(0, PngColorType.Grayscale, PngBitDepth.Eight));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSizeCalculator.CalculateImageBytes(1, 0, PngColorType.Grayscale, PngBitDepth.Eight));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSizeCalculator.CalculateFrameRowBytes(0, PngPixelFormat.Rgba8));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSizeCalculator.CalculateFrameBytes(1, 0, PngPixelFormat.Rgba8));
    }

    [Test]
    public void Calculators_RejectInvalidEnumValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSizeCalculator.GetSamplesPerPixel((PngColorType)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSizeCalculator.GetBitDepthValue((PngBitDepth)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSizeCalculator.GetPixelFormatBitsPerPixel(PngPixelFormat.Unknown));
    }

    [Test]
    public void ImageAndFrameCalculators_RejectLongOverflow()
    {
        Assert.Throws<OverflowException>(() => PngSizeCalculator.CalculateImageBytes(int.MaxValue, int.MaxValue, PngColorType.TruecolorAlpha, PngBitDepth.Sixteen));
        Assert.Throws<OverflowException>(() => PngSizeCalculator.CalculateFrameBytes(int.MaxValue, int.MaxValue, PngPixelFormat.Rgba16));
    }

    [Test]
    public void CalculateChunkAllocation_RejectsLengthsThatCannotBackByteArrays()
    {
        Assert.That(PngSizeCalculator.CalculateChunkAllocation(0), Is.EqualTo(0));
        Assert.That(PngSizeCalculator.CalculateChunkAllocation(1024), Is.EqualTo(1024));
        Assert.That(PngSizeCalculator.CalculateChunkAllocation((uint)int.MaxValue), Is.EqualTo(int.MaxValue));

        Assert.Throws<OverflowException>(() => PngSizeCalculator.CalculateChunkAllocation((uint)int.MaxValue + 1u));
    }

    [Test]
    public void CalculateChunkAllocation_EnforcesPositiveCallerLimit()
    {
        Assert.That(PngSizeCalculator.CalculateChunkAllocation(10, 0), Is.EqualTo(10));
        Assert.That(PngSizeCalculator.CalculateChunkAllocation(10, 10), Is.EqualTo(10));

        Assert.Throws<OverflowException>(() => PngSizeCalculator.CalculateChunkAllocation(11, 10));
    }
}
