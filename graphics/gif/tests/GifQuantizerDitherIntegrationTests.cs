using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifQuantizerDitherIntegrationTests
{
    [Test]
    public void ExactQuantizerAndNoDitherMapper_RoundTripThroughIndexedConversion()
    {
        GifBitmap bitmap = new GifBitmap(2, 1, new byte[]
        {
            255, 0, 0, 255,
            0, 0, 255, 255
        });
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.QuantizerMode = GifQuantizerMode.Exact;
        options.DitherMode = GifDitherMode.None;

        GifPaletteBuildResult palette = new GifQuantizer().QuantizeBitmapWithOptions(bitmap, options);
        GifIndexedColorMappingResult mapped = new GifDither().MapWithOptions(bitmap, palette, options);
        GifRgbaConversionResult converted = new GifIndexedToRgbaConverter().ConvertIndexedPixels(mapped.IndexedPixels, bitmap.Width, bitmap.Height, palette.Palette);

        Assert.That(palette.Success, Is.True);
        Assert.That(mapped.Success, Is.True);
        Assert.That(mapped.IndexedPixels, Is.EqualTo(new byte[] { 0, 1 }));
        Assert.That(converted.Success, Is.True);
        Assert.That(converted.RgbaPixels, Is.EqualTo(bitmap.RgbaPixels));
    }

    [Test]
    public void GrayscaleQuantizerAndFloydSteinbergMapper_ProduceDiffusedPattern()
    {
        GifBitmap bitmap = new GifBitmap(3, 1, new byte[]
        {
            120, 120, 120, 255,
            120, 120, 120, 255,
            120, 120, 120, 255
        });
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 2;
        options.QuantizerMode = GifQuantizerMode.Grayscale;
        options.DitherMode = GifDitherMode.FloydSteinberg;

        GifPaletteBuildResult palette = new GifQuantizer().QuantizeBitmapWithOptions(bitmap, options);
        GifIndexedColorMappingResult mapped = new GifDither().MapWithOptions(bitmap, palette, options);

        Assert.That(palette.Success, Is.True);
        Assert.That(mapped.Success, Is.True);
        Assert.That(mapped.IndexedPixels, Is.EqualTo(new byte[] { 0, 1, 0 }));
    }

    [Test]
    public void GrayscaleQuantizerAndOrderedBayerMapper_ProduceThresholdPattern()
    {
        GifBitmap bitmap = new GifBitmap(2, 1, new byte[]
        {
            128, 128, 128, 255,
            128, 128, 128, 255
        });
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 2;
        options.QuantizerMode = GifQuantizerMode.Grayscale;
        options.DitherMode = GifDitherMode.OrderedBayer;

        GifPaletteBuildResult palette = new GifQuantizer().QuantizeBitmapWithOptions(bitmap, options);
        GifIndexedColorMappingResult mapped = new GifDither().MapWithOptions(bitmap, palette, options);

        Assert.That(palette.Success, Is.True);
        Assert.That(mapped.Success, Is.True);
        Assert.That(mapped.IndexedPixels, Is.EqualTo(new byte[] { 0, 1 }));
    }

    [Test]
    public void WebSafeQuantizerAndNoDitherMapper_PreserveTransparentSlotForTransparentPixelsOnly()
    {
        GifBitmap bitmap = new GifBitmap(2, 1, new byte[]
        {
            255, 0, 0, 0,
            0, 0, 0, 255
        });
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 3;
        options.QuantizerMode = GifQuantizerMode.WebSafe;
        options.DitherMode = GifDitherMode.None;
        options.TransparentAlphaThreshold = 0;

        GifPaletteBuildResult palette = new GifQuantizer().QuantizeBitmapWithOptions(bitmap, options);
        GifIndexedColorMappingResult mapped = new GifDither().MapWithOptions(bitmap, palette, options);

        Assert.That(palette.Success, Is.True);
        Assert.That(palette.TransparentColorIndex, Is.EqualTo(0));
        Assert.That(mapped.Success, Is.True);
        Assert.That(mapped.TransparentPixelCount, Is.EqualTo(1));
        Assert.That(mapped.IndexedPixels, Is.EqualTo(new byte[] { 0, 1 }));
    }
}
