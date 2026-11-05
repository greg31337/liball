using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifRgbaQuantizationControlTests
{
    [Test]
    public void CallerPaletteCanPreserveOrSortEntryOrder()
    {
        IGifEncoder encoder = new GifCodec();
        GifBitmap bitmap = new GifBitmap(3, 1, new byte[]
        {
            255, 0, 0, 255,
            0, 0, 255, 255,
            0, 255, 0, 255
        });
        GifQuantizeOptions preserveOptions = CreateCallerPaletteOptions();
        GifQuantizeOptions sortOptions = CreateCallerPaletteOptions();
        sortOptions.PreservePaletteOrder = false;

        GifEncodeResult preserved = encoder.EncodeRgbaStill(bitmap, preserveOptions, null);
        GifEncodeResult sorted = encoder.EncodeRgbaStill(bitmap, sortOptions, null);

        Assert.That(preserved.Success, Is.True);
        AssertPalette(preserved.Document.GlobalColorTable, new byte[]
        {
            0, 0, 255,
            255, 0, 0,
            0, 255, 0,
            0, 0, 0
        });
        Assert.That(preserved.Document.Frames[0].IndexedPixels, Is.EqualTo(new byte[] { 1, 0, 2 }));
        Assert.That(sorted.Success, Is.True);
        AssertPalette(sorted.Document.GlobalColorTable, new byte[]
        {
            0, 0, 0,
            0, 0, 255,
            0, 255, 0,
            255, 0, 0
        });
        Assert.That(sorted.Document.Frames[0].IndexedPixels, Is.EqualTo(new byte[] { 3, 1, 2 }));
    }

    [Test]
    public void ExactTransparentColorMatchesAllRgbaChannelsAndUsesConfiguredPaletteColor()
    {
        IGifEncoder encoder = new GifCodec();
        GifBitmap bitmap = new GifBitmap(3, 1, new byte[]
        {
            10, 20, 30, 40,
            10, 20, 30, 41,
            200, 100, 50, 255
        });
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 4;
        options.TransparentColorMode = GifTransparentColorMode.ExactColor;
        options.ExactTransparentColor = new GifRgbaColor(10, 20, 30, 40);

        GifEncodeResult encoded = encoder.EncodeRgbaStill(bitmap, options, null);
        GifFrame frame = encoded.Document.Frames[0];

        Assert.That(encoded.Success, Is.True);
        Assert.That(frame.HasTransparency, Is.True);
        Assert.That(frame.TransparentColorIndex, Is.EqualTo(0));
        Assert.That(frame.IndexedPixels[0], Is.EqualTo(0));
        Assert.That(frame.IndexedPixels[1], Is.Not.EqualTo(0));
        Assert.That(encoded.Document.GlobalColorTable.Colors[0].Red, Is.EqualTo(10));
        Assert.That(encoded.Document.GlobalColorTable.Colors[0].Green, Is.EqualTo(20));
        Assert.That(encoded.Document.GlobalColorTable.Colors[0].Blue, Is.EqualTo(30));
    }

    [Test]
    public void CallerPaletteSupportsExactTransparencyAndSharedAnimationMapping()
    {
        IGifEncoder encoder = new GifCodec();
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.QuantizerMode = GifQuantizerMode.CallerPalette;
        options.CallerPalette = new GifPalette(new GifColor[]
        {
            new GifColor(255, 0, 0),
            new GifColor(10, 20, 30),
            new GifColor(0, 0, 255),
            new GifColor(0, 255, 0)
        });
        options.TransparentColorMode = GifTransparentColorMode.ExactColor;
        options.ExactTransparentColor = new GifRgbaColor(10, 20, 30, 40);
        GifAnimation animation = new GifAnimation();
        animation.Width = 2;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 10, 20, 30, 40, 255, 0, 0, 255 }),
            CreateFrame(new byte[] { 0, 0, 255, 255, 0, 255, 0, 255 })
        };
        GifEncodeOptions encodeOptions = new GifEncodeOptions();
        encodeOptions.ColorTableMode = GifColorTableMode.Global;

        GifEncodeResult encoded = encoder.EncodeRgbaAnimation(animation, options, encodeOptions);

        Assert.That(encoded.Success, Is.True);
        Assert.That(encoded.Document.GlobalColorTable.Count, Is.EqualTo(4));
        Assert.That(encoded.Document.Frames[0].HasTransparency, Is.True);
        Assert.That(encoded.Document.Frames[0].TransparentColorIndex, Is.EqualTo(1));
        Assert.That(encoded.Document.Frames[0].IndexedPixels, Is.EqualTo(new byte[] { 1, 0 }));
        Assert.That(encoded.Document.Frames[1].HasTransparency, Is.False);
        Assert.That(encoded.Document.Frames[1].IndexedPixels, Is.EqualTo(new byte[] { 2, 3 }));
    }

    [Test]
    public void IncompatiblePaletteAndTransparencySettingsReturnStructuredFailures()
    {
        IGifEncoder encoder = new GifCodec();
        GifBitmap transparent = new GifBitmap(1, 1, new byte[] { 1, 2, 3, 0 });
        GifQuantizeOptions paletteWithoutMode = new GifQuantizeOptions();
        paletteWithoutMode.CallerPalette = CreatePalette();
        GifQuantizeOptions modeWithoutPalette = new GifQuantizeOptions();
        modeWithoutPalette.QuantizerMode = GifQuantizerMode.CallerPalette;
        GifQuantizeOptions missingTransparentIndex = CreateCallerPaletteOptions();
        missingTransparentIndex.TransparentColorMode = GifTransparentColorMode.AlphaThreshold;
        GifQuantizeOptions preserveTransparency = new GifQuantizeOptions();
        preserveTransparency.TransparentColorMode = GifTransparentColorMode.Preserve;
        GifQuantizeOptions missingExactColor = CreateCallerPaletteOptions();
        missingExactColor.TransparentColorMode = GifTransparentColorMode.ExactColor;
        missingExactColor.ExactTransparentColor = new GifRgbaColor(9, 8, 7, 0);

        GifEncodeResult paletteModeFailure = encoder.EncodeRgbaStill(transparent, paletteWithoutMode, null);
        GifEncodeResult missingPaletteFailure = encoder.EncodeRgbaStill(transparent, modeWithoutPalette, null);
        GifEncodeResult transparentIndexFailure = encoder.EncodeRgbaStill(transparent, missingTransparentIndex, null);
        GifEncodeResult preserveFailure = encoder.EncodeRgbaStill(transparent, preserveTransparency, null);
        GifEncodeResult exactColorFailure = encoder.EncodeRgbaStill(new GifBitmap(1, 1, new byte[] { 9, 8, 7, 0 }), missingExactColor, null);

        Assert.That(paletteModeFailure.Success, Is.False);
        Assert.That(paletteModeFailure.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(missingPaletteFailure.Success, Is.False);
        Assert.That(missingPaletteFailure.Error.Code, Is.EqualTo(GifErrorCode.MissingColorTable));
        Assert.That(transparentIndexFailure.Success, Is.False);
        Assert.That(transparentIndexFailure.Error.Code, Is.EqualTo(GifErrorCode.InvalidPaletteIndex));
        Assert.That(preserveFailure.Success, Is.False);
        Assert.That(preserveFailure.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(exactColorFailure.Success, Is.False);
        Assert.That(exactColorFailure.Error.Code, Is.EqualTo(GifErrorCode.InvalidPaletteIndex));
    }

    static GifQuantizeOptions CreateCallerPaletteOptions()
    {
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.QuantizerMode = GifQuantizerMode.CallerPalette;
        options.CallerPalette = CreatePalette();
        options.TransparentColorMode = GifTransparentColorMode.None;
        return options;
    }

    static GifPalette CreatePalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 255),
            new GifColor(255, 0, 0),
            new GifColor(0, 255, 0),
            new GifColor(0, 0, 0)
        });
    }

    static GifAnimationFrame CreateFrame(byte[] rgbaPixels)
    {
        GifAnimationFrame frame = new GifAnimationFrame();
        frame.Bitmap = new GifBitmap(2, 1, rgbaPixels);
        frame.SourceWidth = 2;
        frame.SourceHeight = 1;
        frame.DisposalMethod = GifDisposalMethod.DoNotDispose;
        return frame;
    }

    static void AssertPalette(GifPalette palette, byte[] rgb)
    {
        Assert.That(palette.Count * 3, Is.EqualTo(rgb.Length));
        int index = 0;
        while (index < palette.Count)
        {
            int offset = index * 3;
            Assert.That(palette.Colors[index].Red, Is.EqualTo(rgb[offset]));
            Assert.That(palette.Colors[index].Green, Is.EqualTo(rgb[offset + 1]));
            Assert.That(palette.Colors[index].Blue, Is.EqualTo(rgb[offset + 2]));
            index++;
        }
    }
}
