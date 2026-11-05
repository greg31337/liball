using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifQuantizerTests
{
    [Test]
    public void WebSafePaletteGenerator_GeneratesStandardCubeAndPadsToGifSize()
    {
        GifPaletteBuildResult result = new GifWebSafePaletteGenerator().Generate();

        Assert.That(result.Success, Is.True);
        Assert.That(result.Exact, Is.False);
        Assert.That(result.UniqueColorCount, Is.EqualTo(216));
        Assert.That(result.Palette.Count, Is.EqualTo(256));
        AssertColor(result.Palette.Colors[0], 0, 0, 0);
        AssertColor(result.Palette.Colors[1], 0, 0, 51);
        AssertColor(result.Palette.Colors[5], 0, 0, 255);
        AssertColor(result.Palette.Colors[6], 0, 51, 0);
        AssertColor(result.Palette.Colors[215], 255, 255, 255);
        AssertColor(result.Palette.Colors[255], 255, 255, 255);
    }

    [Test]
    public void WebSafePaletteGenerator_UsesConfiguredMaximumWhenBelowCubeSize()
    {
        GifPaletteBuildResult result = new GifWebSafePaletteGenerator().GenerateColorCount(8);

        Assert.That(result.Success, Is.True);
        Assert.That(result.UniqueColorCount, Is.EqualTo(8));
        Assert.That(result.Palette.Count, Is.EqualTo(8));
        AssertColor(result.Palette.Colors[0], 0, 0, 0);
        AssertColor(result.Palette.Colors[5], 0, 0, 255);
        AssertColor(result.Palette.Colors[6], 0, 51, 0);
        AssertColor(result.Palette.Colors[7], 0, 51, 51);
    }

    [Test]
    public void WebSafeQuantizer_ReservesTransparencyAndUsesRemainingSlots()
    {
        byte[] rgbaPixels = new byte[]
        {
            255, 0, 0, 0,
            10, 20, 30, 255,
            40, 50, 60, 255
        };
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 4;
        options.QuantizerMode = GifQuantizerMode.WebSafe;
        options.TransparentAlphaThreshold = 0;

        GifPaletteBuildResult result = new GifWebSafePaletteGenerator().QuantizeRgbaPixelsWithOptions(rgbaPixels, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.HasTransparency, Is.True);
        Assert.That(result.TransparentColorIndex, Is.EqualTo(0));
        Assert.That(result.TransparentPixelCount, Is.EqualTo(1));
        Assert.That(result.UniqueColorCount, Is.EqualTo(4));
        Assert.That(result.Palette.Count, Is.EqualTo(4));
        AssertColor(result.Palette.Colors[0], 0, 0, 0);
        AssertColor(result.Palette.Colors[1], 0, 0, 0);
        AssertColor(result.Palette.Colors[2], 0, 0, 51);
        AssertColor(result.Palette.Colors[3], 0, 0, 102);
    }

    [Test]
    public void GifQuantizer_DispatchesWebSafeMode()
    {
        byte[] rgbaPixels = new byte[]
        {
            255, 0, 0, 255,
            0, 0, 255, 255
        };
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 2;
        options.QuantizerMode = GifQuantizerMode.WebSafe;

        GifPaletteBuildResult result = new GifQuantizer().QuantizeRgbaPixelsWithOptions(rgbaPixels, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Palette.Count, Is.EqualTo(2));
        AssertColor(result.Palette.Colors[0], 0, 0, 0);
        AssertColor(result.Palette.Colors[1], 0, 0, 51);
    }

    [Test]
    public void WebSafePaletteGenerator_RejectsInvalidOptionsOrRgbaLength()
    {
        GifWebSafePaletteGenerator generator = new GifWebSafePaletteGenerator();
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 0;

        GifPaletteBuildResult badOptions = generator.GenerateWithOptions(options);
        options.MaxColors = 2;
        GifPaletteBuildResult badLength = generator.QuantizeRgbaPixelsWithOptions(new byte[] { 1, 2, 3 }, options);

        Assert.That(badOptions.Success, Is.False);
        Assert.That(badOptions.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(badLength.Success, Is.False);
        Assert.That(badLength.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
    }

    [Test]
    public void GrayscalePaletteGenerator_GeneratesEvenlySpacedLegalPalette()
    {
        GifPaletteBuildResult result = new GifGrayscalePaletteGenerator().GenerateColorCount(4);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Exact, Is.False);
        Assert.That(result.HasTransparency, Is.False);
        Assert.That(result.UniqueColorCount, Is.EqualTo(4));
        Assert.That(result.Palette.Count, Is.EqualTo(4));
        AssertColor(result.Palette.Colors[0], 0, 0, 0);
        AssertColor(result.Palette.Colors[1], 85, 85, 85);
        AssertColor(result.Palette.Colors[2], 170, 170, 170);
        AssertColor(result.Palette.Colors[3], 255, 255, 255);
    }

    [Test]
    public void GrayscalePaletteGenerator_PadsNonPowerOfTwoPalette()
    {
        GifPaletteBuildResult result = new GifGrayscalePaletteGenerator().GenerateColorCount(3);

        Assert.That(result.Success, Is.True);
        Assert.That(result.UniqueColorCount, Is.EqualTo(3));
        Assert.That(result.Palette.Count, Is.EqualTo(4));
        AssertColor(result.Palette.Colors[0], 0, 0, 0);
        AssertColor(result.Palette.Colors[1], 128, 128, 128);
        AssertColor(result.Palette.Colors[2], 255, 255, 255);
        AssertColor(result.Palette.Colors[3], 255, 255, 255);
    }

    [Test]
    public void GrayscaleQuantizer_ReservesTransparencyAndUsesRemainingSlots()
    {
        byte[] rgbaPixels = new byte[]
        {
            255, 0, 0, 0,
            10, 20, 30, 255,
            40, 50, 60, 255
        };
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 4;
        options.QuantizerMode = GifQuantizerMode.Grayscale;
        options.TransparentAlphaThreshold = 0;

        GifPaletteBuildResult result = new GifGrayscalePaletteGenerator().QuantizeRgbaPixelsWithOptions(rgbaPixels, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.HasTransparency, Is.True);
        Assert.That(result.TransparentColorIndex, Is.EqualTo(0));
        Assert.That(result.TransparentPixelCount, Is.EqualTo(1));
        Assert.That(result.UniqueColorCount, Is.EqualTo(4));
        Assert.That(result.Palette.Count, Is.EqualTo(4));
        AssertColor(result.Palette.Colors[0], 0, 0, 0);
        AssertColor(result.Palette.Colors[1], 0, 0, 0);
        AssertColor(result.Palette.Colors[2], 128, 128, 128);
        AssertColor(result.Palette.Colors[3], 255, 255, 255);
    }

    [Test]
    public void GifQuantizer_DispatchesGrayscaleMode()
    {
        byte[] rgbaPixels = new byte[]
        {
            255, 0, 0, 255,
            0, 0, 255, 255
        };
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 2;
        options.QuantizerMode = GifQuantizerMode.Grayscale;

        GifPaletteBuildResult result = new GifQuantizer().QuantizeRgbaPixelsWithOptions(rgbaPixels, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Palette.Count, Is.EqualTo(2));
        AssertColor(result.Palette.Colors[0], 0, 0, 0);
        AssertColor(result.Palette.Colors[1], 255, 255, 255);
    }

    [Test]
    public void GrayscalePaletteGenerator_RejectsInvalidOptionsOrRgbaLength()
    {
        GifGrayscalePaletteGenerator generator = new GifGrayscalePaletteGenerator();
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 0;

        GifPaletteBuildResult badOptions = generator.GenerateWithOptions(options);
        options.MaxColors = 2;
        GifPaletteBuildResult badLength = generator.QuantizeRgbaPixelsWithOptions(new byte[] { 1, 2, 3 }, options);

        Assert.That(badOptions.Success, Is.False);
        Assert.That(badOptions.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(badLength.Success, Is.False);
        Assert.That(badLength.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
    }

    [Test]
    public void OctreeQuantizer_ReducesOneDimensionalColorsToConfiguredMaximum()
    {
        byte[] rgbaPixels = new byte[]
        {
            0, 0, 0, 255,
            85, 0, 0, 255,
            170, 0, 0, 255,
            255, 0, 0, 255
        };
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 2;
        options.QuantizerMode = GifQuantizerMode.Octree;

        GifPaletteBuildResult result = new GifOctreeQuantizer().QuantizeRgbaPixelsWithOptions(rgbaPixels, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Exact, Is.False);
        Assert.That(result.UniqueColorCount, Is.EqualTo(4));
        Assert.That(result.Palette.Count, Is.EqualTo(2));
        AssertColor(result.Palette.Colors[0], 43, 0, 0);
        AssertColor(result.Palette.Colors[1], 213, 0, 0);
    }

    [Test]
    public void OctreeQuantizer_ReservesTransparencyAndExcludesTransparentRgb()
    {
        byte[] rgbaPixels = new byte[]
        {
            255, 0, 0, 0,
            0, 0, 0, 255,
            255, 255, 255, 255
        };
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 2;
        options.QuantizerMode = GifQuantizerMode.Octree;
        options.TransparentAlphaThreshold = 0;

        GifPaletteBuildResult result = new GifOctreeQuantizer().QuantizeRgbaPixelsWithOptions(rgbaPixels, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Exact, Is.False);
        Assert.That(result.HasTransparency, Is.True);
        Assert.That(result.TransparentColorIndex, Is.EqualTo(0));
        Assert.That(result.TransparentPixelCount, Is.EqualTo(1));
        Assert.That(result.UniqueColorCount, Is.EqualTo(3));
        Assert.That(result.Palette.Count, Is.EqualTo(2));
        AssertColor(result.Palette.Colors[0], 0, 0, 0);
        AssertColor(result.Palette.Colors[1], 128, 128, 128);
    }

    [Test]
    public void OctreeQuantizer_ReturnsExactCompactedPaletteWhenWithinLimit()
    {
        byte[] rgbaPixels = new byte[]
        {
            255, 0, 0, 255,
            0, 255, 0, 255,
            0, 0, 255, 255
        };
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 4;
        options.QuantizerMode = GifQuantizerMode.Octree;

        GifPaletteBuildResult result = new GifOctreeQuantizer().QuantizeRgbaPixelsWithOptions(rgbaPixels, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Exact, Is.True);
        Assert.That(result.UniqueColorCount, Is.EqualTo(3));
        Assert.That(result.Palette.Count, Is.EqualTo(4));
        AssertColor(result.Palette.Colors[0], 255, 0, 0);
        AssertColor(result.Palette.Colors[1], 0, 255, 0);
        AssertColor(result.Palette.Colors[2], 0, 0, 255);
        AssertColor(result.Palette.Colors[3], 0, 0, 255);
    }

    [Test]
    public void GifQuantizer_DispatchesOctreeMode()
    {
        byte[] rgbaPixels = new byte[]
        {
            0, 0, 0, 255,
            85, 0, 0, 255,
            170, 0, 0, 255,
            255, 0, 0, 255
        };
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 2;
        options.QuantizerMode = GifQuantizerMode.Octree;

        GifPaletteBuildResult result = new GifQuantizer().QuantizeRgbaPixelsWithOptions(rgbaPixels, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Exact, Is.False);
        Assert.That(result.Palette.Count, Is.EqualTo(2));
        AssertColor(result.Palette.Colors[0], 43, 0, 0);
        AssertColor(result.Palette.Colors[1], 213, 0, 0);
    }

    [Test]
    public void OctreeQuantizer_RejectsInvalidSamplingStepOrRgbaLength()
    {
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.QuantizerMode = GifQuantizerMode.Octree;
        options.SamplingStep = 0;

        GifOctreeQuantizer quantizer = new GifOctreeQuantizer();
        GifPaletteBuildResult badStep = quantizer.QuantizeRgbaPixelsWithOptions(new byte[] { 1, 2, 3, 255 }, options);
        options.SamplingStep = 1;
        GifPaletteBuildResult badLength = quantizer.QuantizeRgbaPixelsWithOptions(new byte[] { 1, 2, 3 }, options);

        Assert.That(badStep.Success, Is.False);
        Assert.That(badStep.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(badLength.Success, Is.False);
        Assert.That(badLength.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
    }

    [Test]
    public void MedianCutQuantizer_ReducesOneDimensionalColorsToConfiguredMaximum()
    {
        byte[] rgbaPixels = new byte[]
        {
            0, 0, 0, 255,
            85, 0, 0, 255,
            170, 0, 0, 255,
            255, 0, 0, 255
        };
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 2;
        options.QuantizerMode = GifQuantizerMode.MedianCut;

        GifPaletteBuildResult result = new GifMedianCutQuantizer().QuantizeRgbaPixelsWithOptions(rgbaPixels, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Exact, Is.False);
        Assert.That(result.UniqueColorCount, Is.EqualTo(4));
        Assert.That(result.Palette.Count, Is.EqualTo(2));
        AssertColor(result.Palette.Colors[0], 43, 0, 0);
        AssertColor(result.Palette.Colors[1], 213, 0, 0);
    }

    [Test]
    public void MedianCutQuantizer_ReservesTransparencyAndExcludesTransparentRgb()
    {
        byte[] rgbaPixels = new byte[]
        {
            255, 0, 0, 0,
            0, 0, 0, 255,
            255, 255, 255, 255
        };
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 2;
        options.QuantizerMode = GifQuantizerMode.MedianCut;
        options.TransparentAlphaThreshold = 0;

        GifPaletteBuildResult result = new GifMedianCutQuantizer().QuantizeRgbaPixelsWithOptions(rgbaPixels, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Exact, Is.False);
        Assert.That(result.HasTransparency, Is.True);
        Assert.That(result.TransparentColorIndex, Is.EqualTo(0));
        Assert.That(result.TransparentPixelCount, Is.EqualTo(1));
        Assert.That(result.UniqueColorCount, Is.EqualTo(3));
        Assert.That(result.Palette.Count, Is.EqualTo(2));
        AssertColor(result.Palette.Colors[0], 0, 0, 0);
        AssertColor(result.Palette.Colors[1], 128, 128, 128);
    }

    [Test]
    public void MedianCutQuantizer_ReturnsExactCompactedPaletteWhenWithinLimit()
    {
        byte[] rgbaPixels = new byte[]
        {
            255, 0, 0, 255,
            0, 255, 0, 255,
            0, 0, 255, 255
        };
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 4;
        options.QuantizerMode = GifQuantizerMode.MedianCut;

        GifPaletteBuildResult result = new GifMedianCutQuantizer().QuantizeRgbaPixelsWithOptions(rgbaPixels, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Exact, Is.True);
        Assert.That(result.UniqueColorCount, Is.EqualTo(3));
        Assert.That(result.Palette.Count, Is.EqualTo(4));
        AssertColor(result.Palette.Colors[0], 255, 0, 0);
        AssertColor(result.Palette.Colors[1], 0, 255, 0);
        AssertColor(result.Palette.Colors[2], 0, 0, 255);
        AssertColor(result.Palette.Colors[3], 0, 0, 255);
    }

    [Test]
    public void GifQuantizer_DispatchesMedianCutMode()
    {
        byte[] rgbaPixels = new byte[]
        {
            0, 0, 0, 255,
            85, 0, 0, 255,
            170, 0, 0, 255,
            255, 0, 0, 255
        };
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 2;
        options.QuantizerMode = GifQuantizerMode.MedianCut;

        GifPaletteBuildResult result = new GifQuantizer().QuantizeRgbaPixelsWithOptions(rgbaPixels, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Exact, Is.False);
        Assert.That(result.Palette.Count, Is.EqualTo(2));
        AssertColor(result.Palette.Colors[0], 43, 0, 0);
        AssertColor(result.Palette.Colors[1], 213, 0, 0);
    }

    [Test]
    public void MedianCutQuantizer_RejectsInvalidSamplingStepOrRgbaLength()
    {
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.QuantizerMode = GifQuantizerMode.MedianCut;
        options.SamplingStep = 0;

        GifMedianCutQuantizer quantizer = new GifMedianCutQuantizer();
        GifPaletteBuildResult badStep = quantizer.QuantizeRgbaPixelsWithOptions(new byte[] { 1, 2, 3, 255 }, options);
        options.SamplingStep = 1;
        GifPaletteBuildResult badLength = quantizer.QuantizeRgbaPixelsWithOptions(new byte[] { 1, 2, 3 }, options);

        Assert.That(badStep.Success, Is.False);
        Assert.That(badStep.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(badLength.Success, Is.False);
        Assert.That(badLength.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
    }

    static void AssertColor(GifColor color, int red, int green, int blue)
    {
        Assert.That(color.Red, Is.EqualTo((byte)red));
        Assert.That(color.Green, Is.EqualTo((byte)green));
        Assert.That(color.Blue, Is.EqualTo((byte)blue));
    }
}
