using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifDitherTests
{
    [Test]
    public void MapOrderedBayer_AppliesFourByFourThresholdPattern()
    {
        GifBitmap bitmap = new GifBitmap(4, 4, new byte[]
        {
            128, 128, 128, 255,
            128, 128, 128, 255,
            128, 128, 128, 255,
            128, 128, 128, 255,
            128, 128, 128, 255,
            128, 128, 128, 255,
            128, 128, 128, 255,
            128, 128, 128, 255,
            128, 128, 128, 255,
            128, 128, 128, 255,
            128, 128, 128, 255,
            128, 128, 128, 255,
            128, 128, 128, 255,
            128, 128, 128, 255,
            128, 128, 128, 255,
            128, 128, 128, 255
        });

        GifIndexedColorMappingResult result = new GifDither().MapOrderedBayer(bitmap, CreateBlackWhitePalette());

        Assert.That(result.Success, Is.True);
        Assert.That(result.IndexedPixels, Is.EqualTo(new byte[]
        {
            0, 1, 0, 1,
            1, 0, 1, 0,
            0, 1, 0, 1,
            1, 0, 1, 0
        }));
    }

    [Test]
    public void MapOrderedBayer_MapsTransparentPixelsDirectly()
    {
        GifBitmap bitmap = new GifBitmap(2, 1, new byte[]
        {
            255, 0, 0, 0,
            128, 128, 128, 255
        });
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.TransparentAlphaThreshold = 0;

        GifIndexedColorMappingResult result = new GifDither().MapOrderedBayerWithOptions(bitmap, palette, options, 0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IndexedPixels, Is.EqualTo(new byte[] { 0, 2 }));
        Assert.That(result.TransparentPixelCount, Is.EqualTo(1));
    }

    [Test]
    public void Map_DispatchesOrderedBayerUsingPaletteBuildResult()
    {
        GifBitmap bitmap = new GifBitmap(2, 1, new byte[]
        {
            128, 128, 128, 255,
            128, 128, 128, 255
        });
        GifPaletteBuildResult paletteResult = new GifPaletteBuildResult();
        paletteResult.Success = true;
        paletteResult.Palette = CreateBlackWhitePalette();
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.DitherMode = GifDitherMode.OrderedBayer;

        GifIndexedColorMappingResult result = new GifDither().MapWithOptions(bitmap, paletteResult, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IndexedPixels, Is.EqualTo(new byte[] { 0, 1 }));
    }

    [Test]
    public void MapOrderedBayer_RejectsInvalidTransparentIndex()
    {
        GifBitmap bitmap = new GifBitmap(1, 1, new byte[] { 1, 2, 3, 255 });

        GifIndexedColorMappingResult result = new GifDither().MapOrderedBayerWithOptions(bitmap, CreateBlackWhitePalette(), new GifQuantizeOptions(), 2);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidPaletteIndex));
    }

    [Test]
    public void MapFloydSteinberg_DiffusesErrorAcrossRow()
    {
        GifBitmap bitmap = new GifBitmap(3, 1, new byte[]
        {
            120, 120, 120, 255,
            120, 120, 120, 255,
            120, 120, 120, 255
        });
        GifPalette palette = CreateBlackWhitePalette();

        GifIndexedColorMappingResult result = new GifDither().MapFloydSteinberg(bitmap, palette);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IndexedPixels, Is.EqualTo(new byte[] { 0, 1, 0 }));
    }

    [Test]
    public void MapFloydSteinberg_DiffusesErrorAcrossRows()
    {
        GifBitmap bitmap = new GifBitmap(2, 2, new byte[]
        {
            128, 128, 128, 255,
            128, 128, 128, 255,
            128, 128, 128, 255,
            128, 128, 128, 255
        });
        GifPalette palette = CreateBlackWhitePalette();

        GifIndexedColorMappingResult result = new GifDither().MapFloydSteinberg(bitmap, palette);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IndexedPixels, Is.EqualTo(new byte[] { 1, 0, 0, 1 }));
    }

    [Test]
    public void MapFloydSteinberg_MapsTransparentPixelsWithoutDiffusingTheirRgb()
    {
        GifBitmap bitmap = new GifBitmap(3, 1, new byte[]
        {
            255, 0, 0, 0,
            120, 120, 120, 255,
            120, 120, 120, 255
        });
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.TransparentAlphaThreshold = 0;

        GifIndexedColorMappingResult result = new GifDither().MapFloydSteinbergWithOptions(bitmap, palette, options, 0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IndexedPixels, Is.EqualTo(new byte[] { 0, 1, 2 }));
        Assert.That(result.TransparentPixelCount, Is.EqualTo(1));
    }

    [Test]
    public void Map_DispatchesFloydSteinbergUsingPaletteBuildResult()
    {
        GifBitmap bitmap = new GifBitmap(3, 1, new byte[]
        {
            120, 120, 120, 255,
            120, 120, 120, 255,
            120, 120, 120, 255
        });
        GifPaletteBuildResult paletteResult = new GifPaletteBuildResult();
        paletteResult.Success = true;
        paletteResult.Palette = CreateBlackWhitePalette();
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.DitherMode = GifDitherMode.FloydSteinberg;

        GifIndexedColorMappingResult result = new GifDither().MapWithOptions(bitmap, paletteResult, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IndexedPixels, Is.EqualTo(new byte[] { 0, 1, 0 }));
    }

    [Test]
    public void MapFloydSteinberg_RejectsInvalidTransparentIndex()
    {
        GifBitmap bitmap = new GifBitmap(1, 1, new byte[] { 1, 2, 3, 255 });

        GifIndexedColorMappingResult result = new GifDither().MapFloydSteinbergWithOptions(bitmap, CreateBlackWhitePalette(), new GifQuantizeOptions(), 2);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidPaletteIndex));
    }

    [Test]
    public void MapNoDither_MapsOpaquePixelsToNearestPaletteColor()
    {
        GifBitmap bitmap = new GifBitmap(4, 1, new byte[]
        {
            0, 0, 0, 255,
            220, 220, 220, 255,
            10, 10, 10, 255,
            255, 255, 255, 255
        });
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });

        GifIndexedColorMappingResult result = new GifDither().MapNoDither(bitmap, palette);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IndexedPixels, Is.EqualTo(new byte[] { 0, 1, 0, 1 }));
        Assert.That(result.TransparentPixelCount, Is.EqualTo(0));
    }

    [Test]
    public void MapNoDither_MapsTransparentPixelsToTransparentIndexAndExcludesItForOpaquePixels()
    {
        GifBitmap bitmap = new GifBitmap(3, 1, new byte[]
        {
            255, 0, 0, 0,
            0, 0, 0, 255,
            255, 255, 255, 255
        });
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.TransparentAlphaThreshold = 0;

        GifIndexedColorMappingResult result = new GifDither().MapNoDitherWithOptions(bitmap, palette, options, 0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IndexedPixels, Is.EqualTo(new byte[] { 0, 1, 2 }));
        Assert.That(result.TransparentPixelCount, Is.EqualTo(1));
    }

    [Test]
    public void MapNoDither_PopulatesIndexedBitmap()
    {
        GifBitmap bitmap = new GifBitmap(2, 1, new byte[]
        {
            255, 0, 0, 255,
            0, 0, 255, 255
        });
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(255, 0, 0),
            new GifColor(0, 0, 255)
        });

        GifIndexedColorMappingResult result = new GifDither().MapNoDither(bitmap, palette);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bitmap.Width, Is.EqualTo(2));
        Assert.That(result.Bitmap.Height, Is.EqualTo(1));
        Assert.That(result.Bitmap.UsesIndexedPixels, Is.True);
        Assert.That(result.Bitmap.IndexedPixels, Is.SameAs(result.IndexedPixels));
        Assert.That(result.Bitmap.Palette, Is.SameAs(palette));
    }

    [Test]
    public void Map_DispatchesNoDitherUsingPaletteBuildResult()
    {
        GifBitmap bitmap = new GifBitmap(2, 1, new byte[]
        {
            0, 0, 0, 255,
            255, 255, 255, 255
        });
        GifPaletteBuildResult paletteResult = new GifPaletteBuildResult();
        paletteResult.Success = true;
        paletteResult.Palette = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.DitherMode = GifDitherMode.None;

        GifIndexedColorMappingResult result = new GifDither().MapWithOptions(bitmap, paletteResult, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IndexedPixels, Is.EqualTo(new byte[] { 0, 1 }));
    }

    [Test]
    public void MapNoDither_RejectsInvalidBitmapPaletteOrTransparentIndex()
    {
        GifDither dither = new GifDither();
        GifBitmap invalidBitmap = new GifBitmap(1, 1, new byte[] { 1, 2, 3 });
        GifBitmap validBitmap = new GifBitmap(1, 1, new byte[] { 1, 2, 3, 255 });
        GifPalette validPalette = new GifPalette(new GifColor[]
        {
            new GifColor(1, 2, 3),
            new GifColor(4, 5, 6)
        });

        GifIndexedColorMappingResult badBitmap = dither.MapNoDither(invalidBitmap, validPalette);
        GifIndexedColorMappingResult badPalette = dither.MapNoDither(validBitmap, new GifPalette(null));
        GifIndexedColorMappingResult badTransparentIndex = dither.MapNoDitherWithOptions(validBitmap, validPalette, new GifQuantizeOptions(), 2);

        Assert.That(badBitmap.Success, Is.False);
        Assert.That(badBitmap.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
        Assert.That(badPalette.Success, Is.False);
        Assert.That(badPalette.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(badTransparentIndex.Success, Is.False);
        Assert.That(badTransparentIndex.Error.Code, Is.EqualTo(GifErrorCode.InvalidPaletteIndex));
    }

    static GifPalette CreateBlackWhitePalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
    }
}
