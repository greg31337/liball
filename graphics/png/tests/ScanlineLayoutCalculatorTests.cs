using System.Reflection;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ScanlineLayoutCalculatorTests
{
    [Test]
    public void PngScanlineLayout_UsesInternalFieldsOnly()
    {
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        Assert.That(typeof(PngScanlineLayout).IsNotPublic, Is.True);
        Assert.That(typeof(PngScanlineLayout).GetProperties(flags), Is.Empty);
        Assert.That(typeof(PngScanlineLayout).GetEvents(flags), Is.Empty);
        Assert.That(typeof(PngScanlineLayout).GetMethods(flags), Is.Empty);
        Assert.That(typeof(PngScanlineLayout).GetFields(flags), Has.Length.EqualTo(12));
    }

    [TestCase(PngConstants.ColorTypeGrayscale, PngConstants.BitDepth1, 1, 1, 2)]
    [TestCase(PngConstants.ColorTypeGrayscale, PngConstants.BitDepth2, 2, 1, 3)]
    [TestCase(PngConstants.ColorTypeGrayscale, PngConstants.BitDepth4, 4, 1, 5)]
    [TestCase(PngConstants.ColorTypeGrayscale, PngConstants.BitDepth8, 8, 1, 9)]
    [TestCase(PngConstants.ColorTypeGrayscale, PngConstants.BitDepth16, 16, 2, 18)]
    [TestCase(PngConstants.ColorTypeTruecolor, PngConstants.BitDepth8, 24, 3, 27)]
    [TestCase(PngConstants.ColorTypeTruecolor, PngConstants.BitDepth16, 48, 6, 54)]
    [TestCase(PngConstants.ColorTypeIndexedColor, PngConstants.BitDepth1, 1, 1, 2)]
    [TestCase(PngConstants.ColorTypeIndexedColor, PngConstants.BitDepth2, 2, 1, 3)]
    [TestCase(PngConstants.ColorTypeIndexedColor, PngConstants.BitDepth4, 4, 1, 5)]
    [TestCase(PngConstants.ColorTypeIndexedColor, PngConstants.BitDepth8, 8, 1, 9)]
    [TestCase(PngConstants.ColorTypeGrayscaleAlpha, PngConstants.BitDepth8, 16, 2, 18)]
    [TestCase(PngConstants.ColorTypeGrayscaleAlpha, PngConstants.BitDepth16, 32, 4, 36)]
    [TestCase(PngConstants.ColorTypeTruecolorAlpha, PngConstants.BitDepth8, 32, 4, 36)]
    [TestCase(PngConstants.ColorTypeTruecolorAlpha, PngConstants.BitDepth16, 64, 8, 72)]
    public void Calculate_CoversEveryValidColorTypeAndBitDepth(int colorType, int bitDepth, int bitsPerPixel, int filterBytesPerPixel, int rowDataBytes)
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(9, 2, (PngColorType)colorType, (PngBitDepth)bitDepth);

        Assert.That(layout.Width, Is.EqualTo(9));
        Assert.That(layout.Height, Is.EqualTo(2));
        Assert.That(layout.ColorType, Is.EqualTo((PngColorType)colorType));
        Assert.That(layout.BitDepth, Is.EqualTo((PngBitDepth)bitDepth));
        Assert.That(layout.BitsPerSample, Is.EqualTo(bitDepth));
        Assert.That(layout.BitsPerPixel, Is.EqualTo(bitsPerPixel));
        Assert.That(layout.FilterBytesPerPixel, Is.EqualTo(filterBytesPerPixel));
        Assert.That(layout.RowDataBytes, Is.EqualTo(rowDataBytes));
        Assert.That(layout.FilteredRowBytes, Is.EqualTo(rowDataBytes + 1));
        Assert.That(layout.ImageDataBytes, Is.EqualTo(rowDataBytes * 2));
        Assert.That(layout.FilteredImageBytes, Is.EqualTo((rowDataBytes + 1) * 2));
    }

    [Test]
    public void Calculate_FromIhdrUsesIhdrFields()
    {
        PngIhdr ihdr = new PngIhdr();
        ihdr.Width = 5;
        ihdr.Height = 3;
        ihdr.ColorType = PngColorType.TruecolorAlpha;
        ihdr.BitDepth = PngBitDepth.Eight;
        ihdr.CompressionMethod = PngCompressionMethod.Deflate;
        ihdr.FilterMethod = PngFilterMethod.Adaptive;
        ihdr.InterlaceMethod = PngInterlaceMethod.Adam7;

        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(ihdr);

        Assert.That(layout.Width, Is.EqualTo(5));
        Assert.That(layout.Height, Is.EqualTo(3));
        Assert.That(layout.SamplesPerPixel, Is.EqualTo(4));
        Assert.That(layout.BitsPerPixel, Is.EqualTo(32));
        Assert.That(layout.RowDataBytes, Is.EqualTo(20));
        Assert.That(layout.FilteredImageBytes, Is.EqualTo(63));
    }

    [Test]
    public void Calculate_ReportsExpectedSamplesPerPixel()
    {
        Assert.That(PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.Grayscale, PngBitDepth.Eight).SamplesPerPixel, Is.EqualTo(1));
        Assert.That(PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.IndexedColor, PngBitDepth.Eight).SamplesPerPixel, Is.EqualTo(1));
        Assert.That(PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.Truecolor, PngBitDepth.Eight).SamplesPerPixel, Is.EqualTo(3));
        Assert.That(PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.GrayscaleAlpha, PngBitDepth.Eight).SamplesPerPixel, Is.EqualTo(2));
        Assert.That(PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.TruecolorAlpha, PngBitDepth.Eight).SamplesPerPixel, Is.EqualTo(4));
    }

    [Test]
    public void Calculate_RejectsInvalidDimensionsAndCombinations()
    {
        Assert.Throws<ArgumentNullException>(() => PngScanlineLayoutCalculator.Calculate(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngScanlineLayoutCalculator.Calculate(0, 1, PngColorType.Grayscale, PngBitDepth.Eight));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngScanlineLayoutCalculator.Calculate(1, 0, PngColorType.Grayscale, PngBitDepth.Eight));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.Truecolor, PngBitDepth.Four));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.IndexedColor, PngBitDepth.Sixteen));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngScanlineLayoutCalculator.Calculate(1, 1, (PngColorType)99, PngBitDepth.Eight));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.Grayscale, (PngBitDepth)99));
    }

    [Test]
    public void Calculate_RejectsLongOverflow()
    {
        Assert.Throws<OverflowException>(() => PngScanlineLayoutCalculator.Calculate(int.MaxValue, int.MaxValue, PngColorType.TruecolorAlpha, PngBitDepth.Sixteen));
    }
}
