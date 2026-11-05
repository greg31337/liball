using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifIndexedToRgbaConverterTests
{
    [Test]
    public void Convert_MapsIndexedPixelsToOpaqueRgbaBytes()
    {
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(1, 2, 3),
            new GifColor(10, 20, 30),
            new GifColor(100, 110, 120),
            new GifColor(200, 210, 220)
        });
        byte[] indexedPixels = new byte[] { 0, 2, 1, 3 };
        GifIndexedToRgbaConverter converter = new GifIndexedToRgbaConverter();

        GifRgbaConversionResult result = converter.ConvertIndexedPixels(indexedPixels, 2, 2, palette);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RgbaPixels, Is.EqualTo(new byte[]
        {
            1, 2, 3, 255,
            100, 110, 120, 255,
            10, 20, 30, 255,
            200, 210, 220, 255
        }));
        Assert.That(result.Bitmap.Width, Is.EqualTo(2));
        Assert.That(result.Bitmap.Height, Is.EqualTo(2));
        Assert.That(result.Bitmap.Stride, Is.EqualTo(8));
        Assert.That(result.Bitmap.RgbaPixels, Is.SameAs(result.RgbaPixels));
        Assert.That(result.Bitmap.UsesIndexedPixels, Is.False);
    }

    [Test]
    public void Convert_FrameUsesFrameDimensionsAndIndexedPixels()
    {
        GifFrame frame = new GifFrame(0, 0, 2, 1);
        frame.IndexedPixels = new byte[] { 1, 0 };
        GifPalette palette = CreatePalette();
        GifIndexedToRgbaConverter converter = new GifIndexedToRgbaConverter();

        GifRgbaConversionResult result = converter.ConvertFrame(frame, palette);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RgbaPixels, Is.EqualTo(new byte[] { 10, 20, 30, 255, 1, 2, 3, 255 }));
    }

    [Test]
    public void Convert_FrameAppliesTransparentColorIndexToAlpha()
    {
        GifFrame frame = new GifFrame(0, 0, 3, 1);
        frame.IndexedPixels = new byte[] { 0, 1, 0 };
        frame.HasTransparency = true;
        frame.TransparentColorIndex = 0;
        GifIndexedToRgbaConverter converter = new GifIndexedToRgbaConverter();

        GifRgbaConversionResult result = converter.ConvertFrame(frame, CreatePalette());

        Assert.That(result.Success, Is.True);
        Assert.That(result.RgbaPixels, Is.EqualTo(new byte[]
        {
            1, 2, 3, 0,
            10, 20, 30, 255,
            1, 2, 3, 0
        }));
    }

    [Test]
    public void Convert_DirectOverloadCanApplyTransparency()
    {
        GifIndexedToRgbaConverter converter = new GifIndexedToRgbaConverter();

        GifRgbaConversionResult result = converter.ConvertIndexedPixelsWithTransparency(new byte[] { 1, 0 }, 2, 1, CreatePalette(), true, 1);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RgbaPixels, Is.EqualTo(new byte[] { 10, 20, 30, 0, 1, 2, 3, 255 }));
    }

    [Test]
    public void Convert_DocumentResolvesActiveColorTable()
    {
        GifDocument document = new GifDocument();
        document.GlobalColorTable = CreatePalette();
        document.Frames = new GifFrame[] { new GifFrame(0, 0, 1, 1), new GifFrame(0, 0, 1, 1) };
        document.Frames[0].IndexedPixels = new byte[] { 0 };
        document.Frames[1].IndexedPixels = new byte[] { 1 };
        GifPalette local = new GifPalette(new GifColor[] { new GifColor(80, 90, 100), new GifColor(110, 120, 130) });
        document.Frames[1].HasLocalColorTable = true;
        document.Frames[1].LocalColorTable = local;
        GifIndexedToRgbaConverter converter = new GifIndexedToRgbaConverter();

        GifRgbaConversionResult first = converter.ConvertDocumentFrame(document, 0, new GifDecodeOptions());
        GifRgbaConversionResult second = converter.ConvertDocumentFrame(document, 1, new GifDecodeOptions());

        Assert.That(first.Success, Is.True);
        Assert.That(first.RgbaPixels, Is.EqualTo(new byte[] { 1, 2, 3, 255 }));
        Assert.That(second.Success, Is.True);
        Assert.That(second.RgbaPixels, Is.EqualTo(new byte[] { 110, 120, 130, 255 }));
    }

    [Test]
    public void Convert_DocumentCanUseDefaultGlobalColorTable()
    {
        GifDocument document = new GifDocument();
        document.Frames = new GifFrame[] { new GifFrame(0, 0, 1, 1) };
        document.Frames[0].IndexedPixels = new byte[] { 1 };
        GifDecodeOptions options = new GifDecodeOptions();
        options.DefaultGlobalColorTable = new byte[] { 1, 2, 3, 4, 5, 6 };
        GifIndexedToRgbaConverter converter = new GifIndexedToRgbaConverter();

        GifRgbaConversionResult result = converter.ConvertDocumentFrame(document, 0, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RgbaPixels, Is.EqualTo(new byte[] { 4, 5, 6, 255 }));
    }

    [Test]
    public void Convert_RejectsIndexedPixelOutsidePalette()
    {
        GifIndexedToRgbaConverter converter = new GifIndexedToRgbaConverter();

        GifRgbaConversionResult result = converter.ConvertIndexedPixels(new byte[] { 0, 2 }, 2, 1, CreatePalette());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidPaletteIndex));
        Assert.That(result.RgbaPixels, Is.Empty);
    }

    [Test]
    public void Convert_RejectsInvalidDimensionsPixelCountAndPalette()
    {
        GifIndexedToRgbaConverter converter = new GifIndexedToRgbaConverter();
        GifPalette invalidPalette = new GifPalette(new GifColor[] { new GifColor(1, 2, 3) });

        GifRgbaConversionResult invalidDimensions = converter.ConvertIndexedPixels(new byte[] { 0 }, 0, 1, CreatePalette());
        GifRgbaConversionResult countMismatch = converter.ConvertIndexedPixels(new byte[] { 0 }, 2, 1, CreatePalette());
        GifRgbaConversionResult invalidPaletteResult = converter.ConvertIndexedPixels(new byte[] { 0 }, 1, 1, invalidPalette);

        Assert.That(invalidDimensions.Success, Is.False);
        Assert.That(invalidDimensions.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(countMismatch.Success, Is.False);
        Assert.That(countMismatch.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
        Assert.That(invalidPaletteResult.Success, Is.False);
        Assert.That(invalidPaletteResult.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
    }

    [Test]
    public void Convert_RejectsOversizedRgbaAllocation()
    {
        GifIndexedToRgbaConverter converter = new GifIndexedToRgbaConverter();

        GifRgbaConversionResult result = converter.ConvertIndexedPixels(new byte[0], 32768, 32768, CreatePalette());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MemoryLimitExceeded));
    }

    [Test]
    public void ConvertDocumentFrame_RejectsConfiguredMemoryLimitBeforeAllocation()
    {
        GifDocument document = new GifDocument();
        document.LogicalScreen = new GifLogicalScreen(1, 1);
        document.GlobalColorTable = CreatePalette();
        GifFrame frame = new GifFrame(0, 0, 1, 1);
        frame.IndexedPixels = new byte[] { 0 };
        document.Frames = new GifFrame[] { frame };
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxMemoryBytes = 3;
        GifIndexedToRgbaConverter converter = new GifIndexedToRgbaConverter();

        GifRgbaConversionResult result = converter.ConvertDocumentFrame(document, 0, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MemoryLimitExceeded));
        Assert.That(result.RgbaPixels, Is.Empty);
    }

    [Test]
    public void Convert_RejectsTransparentIndexOutsidePalette()
    {
        GifFrame frame = new GifFrame(0, 0, 1, 1);
        frame.IndexedPixels = new byte[] { 0 };
        frame.HasTransparency = true;
        frame.TransparentColorIndex = 2;
        GifIndexedToRgbaConverter converter = new GifIndexedToRgbaConverter();

        GifRgbaConversionResult result = converter.ConvertFrame(frame, CreatePalette());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidPaletteIndex));
    }

    [Test]
    public void ApplyTransparency_ValidatesBufferLengths()
    {
        GifIndexedToRgbaConverter converter = new GifIndexedToRgbaConverter();

        GifRgbaConversionResult missing = converter.ApplyTransparency(null!, new byte[] { 0 }, 0, 2);
        GifRgbaConversionResult mismatch = converter.ApplyTransparency(new byte[] { 1, 2, 3, 255 }, new byte[] { 0, 1 }, 0, 2);

        Assert.That(missing.Success, Is.False);
        Assert.That(missing.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
        Assert.That(mismatch.Success, Is.False);
        Assert.That(mismatch.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
    }

    [Test]
    public void ApplyTransparency_MutatesOnlyMatchingAlphaBytes()
    {
        GifIndexedToRgbaConverter converter = new GifIndexedToRgbaConverter();
        byte[] rgbaPixels = new byte[] { 1, 2, 3, 255, 10, 20, 30, 255 };

        GifRgbaConversionResult result = converter.ApplyTransparency(rgbaPixels, new byte[] { 0, 1 }, 1, 2);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RgbaPixels, Is.SameAs(rgbaPixels));
        Assert.That(result.RgbaPixels, Is.EqualTo(new byte[] { 1, 2, 3, 255, 10, 20, 30, 0 }));
    }

    static GifPalette CreatePalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(1, 2, 3),
            new GifColor(10, 20, 30)
        });
    }
}
