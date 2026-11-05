using System.Reflection;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class Adam7PassGeometryCalculatorTests
{
    [Test]
    public void PngAdam7PassGeometry_UsesInternalFieldsOnly()
    {
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        Assert.That(typeof(PngAdam7PassGeometry).IsNotPublic, Is.True);
        Assert.That(typeof(PngAdam7PassGeometry).GetProperties(flags), Is.Empty);
        Assert.That(typeof(PngAdam7PassGeometry).GetEvents(flags), Is.Empty);
        Assert.That(typeof(PngAdam7PassGeometry).GetMethods(flags), Is.Empty);
        Assert.That(typeof(PngAdam7PassGeometry).GetFields(flags), Has.Length.EqualTo(16));
    }

    [Test]
    public void CalculatePasses_UsesStandardAdam7GeometryForEightByEightImage()
    {
        PngAdam7PassGeometry[] passes = PngAdam7PassGeometryCalculator.CalculatePasses(8, 8, PngColorType.Truecolor, PngBitDepth.Eight);

        Assert.That(passes, Has.Length.EqualTo(PngAdam7PassGeometryCalculator.PassCount));
        AssertPass(passes[0], 1, 0, 0, 8, 8, 1, 1);
        AssertPass(passes[1], 2, 4, 0, 8, 8, 1, 1);
        AssertPass(passes[2], 3, 0, 4, 4, 8, 2, 1);
        AssertPass(passes[3], 4, 2, 0, 4, 4, 2, 2);
        AssertPass(passes[4], 5, 0, 2, 2, 4, 4, 2);
        AssertPass(passes[5], 6, 1, 0, 2, 2, 4, 4);
        AssertPass(passes[6], 7, 0, 1, 1, 2, 8, 4);
    }

    [Test]
    public void CalculatePasses_ReportsEmptyPassesForSmallImages()
    {
        PngAdam7PassGeometry[] passes = PngAdam7PassGeometryCalculator.CalculatePasses(5, 3, PngColorType.Grayscale, PngBitDepth.One);

        Assert.That(passes[0].Width, Is.EqualTo(1));
        Assert.That(passes[0].Height, Is.EqualTo(1));
        Assert.That(passes[1].Width, Is.EqualTo(1));
        Assert.That(passes[1].Height, Is.EqualTo(1));
        Assert.That(passes[2].Width, Is.EqualTo(2));
        Assert.That(passes[2].Height, Is.EqualTo(0));
        Assert.That(passes[2].IsEmpty, Is.True);
        Assert.That(passes[2].FilteredImageBytes, Is.EqualTo(0));
        Assert.That(passes[3].Width, Is.EqualTo(1));
        Assert.That(passes[3].Height, Is.EqualTo(1));
        Assert.That(passes[4].Width, Is.EqualTo(3));
        Assert.That(passes[4].Height, Is.EqualTo(1));
        Assert.That(passes[5].Width, Is.EqualTo(2));
        Assert.That(passes[5].Height, Is.EqualTo(2));
        Assert.That(passes[6].Width, Is.EqualTo(5));
        Assert.That(passes[6].Height, Is.EqualTo(1));
    }

    [Test]
    public void CalculatePass_ComputesPackedRowByteCounts()
    {
        PngAdam7PassGeometry pass = PngAdam7PassGeometryCalculator.CalculatePass(5, 3, PngColorType.Grayscale, PngBitDepth.One, 7);

        Assert.That(pass.BitsPerPixel, Is.EqualTo(1));
        Assert.That(pass.FilterBytesPerPixel, Is.EqualTo(1));
        Assert.That(pass.RowDataBytes, Is.EqualTo(1));
        Assert.That(pass.FilteredRowBytes, Is.EqualTo(2));
        Assert.That(pass.ImageDataBytes, Is.EqualTo(1));
        Assert.That(pass.FilteredImageBytes, Is.EqualTo(2));
    }

    [Test]
    public void CalculateFilteredImageBytes_SumsAllPassFilteredBytes()
    {
        PngIhdr packed = CreateIhdr(5, 3, PngColorType.Grayscale, PngBitDepth.One);
        PngIhdr truecolor = CreateIhdr(8, 8, PngColorType.Truecolor, PngBitDepth.Eight);

        Assert.That(PngAdam7PassGeometryCalculator.CalculateFilteredImageBytes(packed), Is.EqualTo(14));
        Assert.That(PngAdam7PassGeometryCalculator.CalculateFilteredImageBytes(truecolor), Is.EqualTo(207));
    }

    [Test]
    public void GetPixelCoordinates_MapPassCoordinatesToImageCoordinates()
    {
        Assert.That(PngAdam7PassGeometryCalculator.GetPixelX(6, 2), Is.EqualTo(5));
        Assert.That(PngAdam7PassGeometryCalculator.GetPixelY(7, 3), Is.EqualTo(7));
        Assert.That(PngAdam7PassGeometryCalculator.GetPixelX(2, 0), Is.EqualTo(4));
        Assert.That(PngAdam7PassGeometryCalculator.GetPixelY(5, 0), Is.EqualTo(2));
    }

    [Test]
    public void Calculators_RejectInvalidInputs()
    {
        PngIhdr ihdr = CreateIhdr(1, 1, PngColorType.Grayscale, PngBitDepth.Eight);

        Assert.Throws<ArgumentNullException>(() => PngAdam7PassGeometryCalculator.CalculatePasses(null!));
        Assert.Throws<ArgumentNullException>(() => PngAdam7PassGeometryCalculator.CalculatePass(null!, 1));
        Assert.Throws<ArgumentNullException>(() => PngAdam7PassGeometryCalculator.CalculateFilteredImageBytes(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAdam7PassGeometryCalculator.CalculatePass(1, 1, PngColorType.Grayscale, PngBitDepth.Eight, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAdam7PassGeometryCalculator.CalculatePass(1, 1, PngColorType.Grayscale, PngBitDepth.Eight, 8));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAdam7PassGeometryCalculator.CalculatePass(0, 1, PngColorType.Grayscale, PngBitDepth.Eight, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAdam7PassGeometryCalculator.CalculatePass(1, 0, PngColorType.Grayscale, PngBitDepth.Eight, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAdam7PassGeometryCalculator.CalculatePass(1, 1, PngColorType.Truecolor, PngBitDepth.Four, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAdam7PassGeometryCalculator.GetPixelX(0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAdam7PassGeometryCalculator.GetPixelY(8, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAdam7PassGeometryCalculator.GetPixelX(1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAdam7PassGeometryCalculator.GetPixelY(1, -1));
        Assert.Throws<OverflowException>(() => PngAdam7PassGeometryCalculator.GetPixelX(1, int.MaxValue));
        Assert.That(PngAdam7PassGeometryCalculator.CalculatePass(ihdr, 1).PassIndex, Is.EqualTo(1));
    }

    static void AssertPass(PngAdam7PassGeometry pass, int passIndex, int xStart, int yStart, int xStep, int yStep, int width, int height)
    {
        Assert.That(pass.PassIndex, Is.EqualTo(passIndex));
        Assert.That(pass.XStart, Is.EqualTo(xStart));
        Assert.That(pass.YStart, Is.EqualTo(yStart));
        Assert.That(pass.XStep, Is.EqualTo(xStep));
        Assert.That(pass.YStep, Is.EqualTo(yStep));
        Assert.That(pass.Width, Is.EqualTo(width));
        Assert.That(pass.Height, Is.EqualTo(height));
        Assert.That(pass.IsEmpty, Is.EqualTo(width == 0 || height == 0));
    }

    static PngIhdr CreateIhdr(int width, int height, PngColorType colorType, PngBitDepth bitDepth)
    {
        PngIhdr ihdr = new PngIhdr();
        ihdr.Width = width;
        ihdr.Height = height;
        ihdr.ColorType = colorType;
        ihdr.BitDepth = bitDepth;
        ihdr.CompressionMethod = PngCompressionMethod.Deflate;
        ihdr.FilterMethod = PngFilterMethod.Adaptive;
        ihdr.InterlaceMethod = PngInterlaceMethod.Adam7;
        return ihdr;
    }
}
