using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPublicRgbaEncodeCoverageTests
{
    [TestCase(GifQuantizerMode.Exact)]
    [TestCase(GifQuantizerMode.MedianCut)]
    [TestCase(GifQuantizerMode.Octree)]
    [TestCase(GifQuantizerMode.Grayscale)]
    [TestCase(GifQuantizerMode.WebSafe)]
    [TestCase(GifQuantizerMode.CallerPalette)]
    public void PublicRgbaStillSupportsEveryQuantizerMode(GifQuantizerMode mode)
    {
        IGifEncoder encoder = new GifCodec();
        IGifDecoder decoder = new GifCodec();
        GifBitmap bitmap = new GifBitmap(6, 1, CreateSixColorPixels());
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.QuantizerMode = mode;
        options.MaxColors = 4;
        options.TransparentColorMode = GifTransparentColorMode.None;
        if (mode == GifQuantizerMode.Exact)
        {
            options.MaxColors = 8;
        }
        else if (mode == GifQuantizerMode.CallerPalette)
        {
            options.CallerPalette = CreateCallerPalette();
        }

        GifEncodeResult encoded = encoder.EncodeRgbaStill(bitmap, options, null);
        GifAnimation decoded = decoder.DecodeAnimationBytes(encoded.Data, null);

        Assert.That(encoded.Success, Is.True);
        Assert.That(encoded.Document.GlobalColorTable.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(encoded.Document.GlobalColorTable.Count, Is.LessThanOrEqualTo(options.MaxColors));
        Assert.That(encoded.Document.Frames[0].IndexedPixels, Has.Length.EqualTo(6));
        Assert.That(decoded.Frames[0].Bitmap.RgbaPixels, Has.Length.EqualTo(24));
        if (mode == GifQuantizerMode.Exact)
        {
            Assert.That(decoded.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(bitmap.RgbaPixels));
        }

        if (mode == GifQuantizerMode.Grayscale)
        {
            AssertPaletteIsGrayscale(encoded.Document.GlobalColorTable);
        }

        if (mode == GifQuantizerMode.CallerPalette)
        {
            Assert.That(encoded.Document.GlobalColorTable.Colors[0].Red, Is.EqualTo(255));
            Assert.That(encoded.Document.GlobalColorTable.Colors[1].Green, Is.EqualTo(255));
            Assert.That(encoded.Document.GlobalColorTable.Colors[2].Blue, Is.EqualTo(255));
        }
    }

    [Test]
    public void PublicExactQuantizationCompactsPaletteAndReusesIndexes()
    {
        IGifEncoder encoder = new GifCodec();
        GifBitmap bitmap = new GifBitmap(6, 1, new byte[]
        {
            255, 0, 0, 255,
            0, 255, 0, 255,
            255, 0, 0, 255,
            0, 0, 255, 255,
            0, 255, 0, 255,
            255, 0, 0, 255
        });
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.QuantizerMode = GifQuantizerMode.Exact;
        options.TransparentColorMode = GifTransparentColorMode.None;

        GifEncodeResult encoded = encoder.EncodeRgbaStill(bitmap, options, null);

        Assert.That(encoded.Success, Is.True);
        Assert.That(encoded.Document.GlobalColorTable.Count, Is.EqualTo(4));
        Assert.That(encoded.Document.Frames[0].IndexedPixels, Is.EqualTo(new byte[] { 0, 1, 0, 2, 1, 0 }));
        Assert.That(encoded.Document.GlobalColorTable.Colors[3].Blue, Is.EqualTo(255));
    }

    [Test]
    public void PublicRgbaAnimationSupportsSharedGlobalAndPerFrameLocalPalettes()
    {
        IGifEncoder encoder = new GifCodec();
        IGifDecoder decoder = new GifCodec();
        GifAnimation animation = new GifAnimation();
        animation.Width = 2;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 255, 0, 0, 255, 0, 255, 0, 255 }),
            CreateFrame(new byte[] { 255, 0, 0, 255, 0, 0, 255, 255 })
        };
        GifQuantizeOptions quantizeOptions = new GifQuantizeOptions();
        quantizeOptions.MaxColors = 4;
        quantizeOptions.QuantizerMode = GifQuantizerMode.Exact;
        quantizeOptions.TransparentColorMode = GifTransparentColorMode.None;
        GifEncodeOptions globalOptions = new GifEncodeOptions();
        globalOptions.ColorTableMode = GifColorTableMode.Global;
        GifEncodeOptions localOptions = new GifEncodeOptions();
        localOptions.ColorTableMode = GifColorTableMode.Local;

        GifEncodeResult global = encoder.EncodeRgbaAnimation(animation, quantizeOptions, globalOptions);
        GifEncodeResult local = encoder.EncodeRgbaAnimation(animation, quantizeOptions, localOptions);
        GifDocument globalDocument = decoder.DecodeBytes(global.Data, null);
        GifDocument localDocument = decoder.DecodeBytes(local.Data, null);

        Assert.That(global.Success, Is.True);
        Assert.That(globalDocument.LogicalScreen.HasGlobalColorTable, Is.True);
        Assert.That(globalDocument.GlobalColorTable.Count, Is.EqualTo(4));
        Assert.That(globalDocument.Frames[0].HasLocalColorTable, Is.False);
        Assert.That(globalDocument.Frames[1].HasLocalColorTable, Is.False);
        Assert.That(global.Document.Frames[0].IndexedPixels[0], Is.EqualTo(global.Document.Frames[1].IndexedPixels[0]));
        Assert.That(local.Success, Is.True);
        Assert.That(localDocument.LogicalScreen.HasGlobalColorTable, Is.False);
        Assert.That(localDocument.Frames[0].HasLocalColorTable, Is.True);
        Assert.That(localDocument.Frames[1].HasLocalColorTable, Is.True);
        Assert.That(localDocument.Frames[0].LocalColorTable.Count, Is.EqualTo(2));
        Assert.That(localDocument.Frames[1].LocalColorTable.Count, Is.EqualTo(2));
    }

    [Test]
    public void PublicRgbaQuantizationSamplingChangesReducedPaletteInput()
    {
        IGifEncoder encoder = new GifCodec();
        GifBitmap bitmap = new GifBitmap(8, 1, new byte[]
        {
            0, 0, 0, 255,
            32, 0, 0, 255,
            64, 0, 0, 255,
            96, 0, 0, 255,
            128, 0, 0, 255,
            160, 0, 0, 255,
            192, 0, 0, 255,
            255, 0, 0, 255
        });
        GifQuantizeOptions everyPixel = new GifQuantizeOptions();
        everyPixel.QuantizerMode = GifQuantizerMode.MedianCut;
        everyPixel.MaxColors = 2;
        everyPixel.SamplingStep = 1;
        everyPixel.TransparentColorMode = GifTransparentColorMode.None;
        GifQuantizeOptions sampled = new GifQuantizeOptions();
        sampled.QuantizerMode = GifQuantizerMode.MedianCut;
        sampled.MaxColors = 2;
        sampled.SamplingStep = 3;
        sampled.TransparentColorMode = GifTransparentColorMode.None;

        GifEncodeResult complete = encoder.EncodeRgbaStill(bitmap, everyPixel, null);
        GifEncodeResult reduced = encoder.EncodeRgbaStill(bitmap, sampled, null);

        Assert.That(complete.Success, Is.True);
        Assert.That(reduced.Success, Is.True);
        Assert.That(complete.Document.GlobalColorTable.Count, Is.EqualTo(2));
        Assert.That(reduced.Document.GlobalColorTable.Count, Is.EqualTo(2));
        Assert.That(PaletteBytes(complete.Document.GlobalColorTable), Is.Not.EqualTo(PaletteBytes(reduced.Document.GlobalColorTable)));
    }

    [Test]
    public void PublicRgbaEncodingSupportsNoFloydSteinbergAndBayerDither()
    {
        IGifEncoder encoder = new GifCodec();
        GifBitmap bitmap = new GifBitmap(3, 1, new byte[]
        {
            120, 120, 120, 255,
            120, 120, 120, 255,
            120, 120, 120, 255
        });

        GifEncodeResult none = encoder.EncodeRgbaStill(bitmap, CreateGrayscaleOptions(GifDitherMode.None), null);
        GifEncodeResult floyd = encoder.EncodeRgbaStill(bitmap, CreateGrayscaleOptions(GifDitherMode.FloydSteinberg), null);
        GifEncodeResult bayer = encoder.EncodeRgbaStill(bitmap, CreateGrayscaleOptions(GifDitherMode.OrderedBayer), null);

        Assert.That(none.Success, Is.True);
        Assert.That(floyd.Success, Is.True);
        Assert.That(bayer.Success, Is.True);
        Assert.That(none.Document.Frames[0].IndexedPixels, Is.EqualTo(new byte[] { 0, 0, 0 }));
        Assert.That(floyd.Document.Frames[0].IndexedPixels, Is.EqualTo(new byte[] { 0, 1, 0 }));
        Assert.That(bayer.Document.Frames[0].IndexedPixels, Is.EqualTo(new byte[] { 0, 1, 0 }));
    }

    [Test]
    public void PublicRgbaEncodingSupportsAlphaThresholdAndExactColorTransparency()
    {
        IGifEncoder encoder = new GifCodec();
        GifBitmap bitmap = new GifBitmap(3, 1, new byte[]
        {
            10, 20, 30, 10,
            10, 20, 30, 11,
            200, 100, 50, 255
        });
        GifQuantizeOptions thresholdOptions = new GifQuantizeOptions();
        thresholdOptions.MaxColors = 4;
        thresholdOptions.TransparentColorMode = GifTransparentColorMode.AlphaThreshold;
        thresholdOptions.TransparentAlphaThreshold = 10;
        GifQuantizeOptions exactOptions = new GifQuantizeOptions();
        exactOptions.MaxColors = 4;
        exactOptions.TransparentColorMode = GifTransparentColorMode.ExactColor;
        exactOptions.ExactTransparentColor = new GifRgbaColor(10, 20, 30, 10);

        GifEncodeResult threshold = encoder.EncodeRgbaStill(bitmap, thresholdOptions, null);
        GifEncodeResult exact = encoder.EncodeRgbaStill(bitmap, exactOptions, null);

        Assert.That(threshold.Success, Is.True);
        Assert.That(threshold.Document.Frames[0].HasTransparency, Is.True);
        Assert.That(threshold.Document.Frames[0].IndexedPixels[0], Is.EqualTo(threshold.Document.Frames[0].TransparentColorIndex));
        Assert.That(threshold.Document.Frames[0].IndexedPixels[1], Is.Not.EqualTo(threshold.Document.Frames[0].TransparentColorIndex));
        Assert.That(exact.Success, Is.True);
        Assert.That(exact.Document.Frames[0].HasTransparency, Is.True);
        Assert.That(exact.Document.Frames[0].IndexedPixels[0], Is.EqualTo(exact.Document.Frames[0].TransparentColorIndex));
        Assert.That(exact.Document.Frames[0].IndexedPixels[1], Is.Not.EqualTo(exact.Document.Frames[0].TransparentColorIndex));
        GifColor exactPaletteColor = exact.Document.GlobalColorTable.Colors[exact.Document.Frames[0].TransparentColorIndex];
        Assert.That(exactPaletteColor.Red, Is.EqualTo(10));
        Assert.That(exactPaletteColor.Green, Is.EqualTo(20));
        Assert.That(exactPaletteColor.Blue, Is.EqualTo(30));
    }

    [Test]
    public void PublicCallerPaletteReportsOutOfPaletteFailures()
    {
        IGifEncoder encoder = new GifCodec();
        GifQuantizeOptions missingExactColor = new GifQuantizeOptions();
        missingExactColor.QuantizerMode = GifQuantizerMode.CallerPalette;
        missingExactColor.CallerPalette = CreateCallerPalette();
        missingExactColor.MaxColors = 4;
        missingExactColor.TransparentColorMode = GifTransparentColorMode.ExactColor;
        missingExactColor.ExactTransparentColor = new GifRgbaColor(9, 8, 7, 6);
        GifQuantizeOptions invalidIndex = new GifQuantizeOptions();
        invalidIndex.QuantizerMode = GifQuantizerMode.CallerPalette;
        invalidIndex.CallerPalette = CreateCallerPalette();
        invalidIndex.MaxColors = 4;
        invalidIndex.TransparentColorMode = GifTransparentColorMode.AlphaThreshold;
        invalidIndex.TransparentColorIndex = 4;
        GifBitmap bitmap = new GifBitmap(1, 1, new byte[] { 9, 8, 7, 6 });

        GifEncodeResult missingColor = encoder.EncodeRgbaStill(bitmap, missingExactColor, null);
        GifEncodeResult outsidePalette = encoder.EncodeRgbaStill(bitmap, invalidIndex, null);

        Assert.That(missingColor.Success, Is.False);
        Assert.That(missingColor.Error.Code, Is.EqualTo(GifErrorCode.InvalidPaletteIndex));
        Assert.That(outsidePalette.Success, Is.False);
        Assert.That(outsidePalette.Error.Code, Is.EqualTo(GifErrorCode.InvalidPaletteIndex));
    }

    [Test]
    public void PublicLocalRgbaAnimationRetainsOutOfPaletteFrameDiagnostic()
    {
        IGifEncoder encoder = new GifCodec();
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 255, 0, 0, 255 }),
            CreateFrame(new byte[] { 9, 8, 7, 6 })
        };
        GifQuantizeOptions quantizeOptions = new GifQuantizeOptions();
        quantizeOptions.QuantizerMode = GifQuantizerMode.CallerPalette;
        quantizeOptions.CallerPalette = CreateCallerPalette();
        quantizeOptions.MaxColors = 4;
        quantizeOptions.TransparentColorMode = GifTransparentColorMode.ExactColor;
        quantizeOptions.ExactTransparentColor = new GifRgbaColor(9, 8, 7, 6);
        GifEncodeOptions encodeOptions = new GifEncodeOptions();
        encodeOptions.ColorTableMode = GifColorTableMode.Local;

        GifEncodeResult encoded = encoder.EncodeRgbaAnimation(animation, quantizeOptions, encodeOptions);

        Assert.That(encoded.Success, Is.False);
        Assert.That(encoded.Error.Code, Is.EqualTo(GifErrorCode.InvalidPaletteIndex));
        Assert.That(encoded.Error.Position.FrameIndex, Is.EqualTo(1));
        Assert.That(encoded.Data, Is.Empty);
    }

    static GifAnimationFrame CreateFrame(byte[] rgbaPixels)
    {
        GifAnimationFrame frame = new GifAnimationFrame();
        frame.Bitmap = new GifBitmap(rgbaPixels.Length / 4, 1, rgbaPixels);
        frame.SourceWidth = frame.Bitmap.Width;
        frame.SourceHeight = 1;
        frame.DisposalMethod = GifDisposalMethod.DoNotDispose;
        return frame;
    }

    static GifQuantizeOptions CreateGrayscaleOptions(GifDitherMode ditherMode)
    {
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 2;
        options.QuantizerMode = GifQuantizerMode.Grayscale;
        options.DitherMode = ditherMode;
        options.TransparentColorMode = GifTransparentColorMode.None;
        return options;
    }

    static GifPalette CreateCallerPalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(255, 0, 0),
            new GifColor(0, 255, 0),
            new GifColor(0, 0, 255),
            new GifColor(0, 0, 0)
        });
    }

    static byte[] CreateSixColorPixels()
    {
        return new byte[]
        {
            255, 0, 0, 255,
            0, 255, 0, 255,
            0, 0, 255, 255,
            255, 255, 0, 255,
            255, 0, 255, 255,
            0, 255, 255, 255
        };
    }

    static void AssertPaletteIsGrayscale(GifPalette palette)
    {
        int index = 0;
        while (index < palette.Count)
        {
            Assert.That(palette.Colors[index].Red, Is.EqualTo(palette.Colors[index].Green));
            Assert.That(palette.Colors[index].Green, Is.EqualTo(palette.Colors[index].Blue));
            index = index + 1;
        }
    }

    static byte[] PaletteBytes(GifPalette palette)
    {
        byte[] bytes = new byte[palette.Count * 3];
        int index = 0;
        while (index < palette.Count)
        {
            int offset = index * 3;
            bytes[offset] = palette.Colors[index].Red;
            bytes[offset + 1] = palette.Colors[index].Green;
            bytes[offset + 2] = palette.Colors[index].Blue;
            index = index + 1;
        }

        return bytes;
    }
}
