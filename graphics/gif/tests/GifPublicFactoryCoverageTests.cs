using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPublicFactoryCoverageTests
{
    [Test]
    public void PublicIndexedBitmapFactoryPreservesDimensionsBuffersAndPalette()
    {
        IGifFactory factory = new GifCodec();
        GifPalette palette = CreatePalette();
        byte[] pixels = new byte[] { 0, 1, 2, 3, 3, 2 };

        GifBitmap bitmap = factory.CreateIndexedBitmap(3, 2, pixels, palette);
        GifBitmap normalized = factory.CreateIndexedBitmap(4, 5, null, null);

        Assert.That(bitmap.Width, Is.EqualTo(3));
        Assert.That(bitmap.Height, Is.EqualTo(2));
        Assert.That(bitmap.Stride, Is.EqualTo(3));
        Assert.That(bitmap.UsesIndexedPixels, Is.True);
        Assert.That(bitmap.IndexedPixels, Is.SameAs(pixels));
        Assert.That(bitmap.Palette, Is.SameAs(palette));
        Assert.That(bitmap.RgbaPixels, Is.Empty);
        Assert.That(normalized.Width, Is.EqualTo(4));
        Assert.That(normalized.Height, Is.EqualTo(5));
        Assert.That(normalized.Stride, Is.EqualTo(4));
        Assert.That(normalized.IndexedPixels, Is.Empty);
        Assert.That(normalized.Palette.IsEmpty(), Is.True);
    }

    [Test]
    public void PublicStillFactoryCreatesIndexedAndRgbaDocumentsWithRequestedPalettePlacement()
    {
        GifCodec codec = new GifCodec();
        IGifFactory factory = codec;
        IGifEncoder encoder = codec;
        IGifDecoder decoder = codec;
        GifBitmap indexed = factory.CreateIndexedBitmap(2, 2, new byte[] { 0, 1, 2, 3 }, CreatePalette());
        GifEncodeOptions localOptions = new GifEncodeOptions();
        localOptions.ColorTableMode = GifColorTableMode.Local;
        GifBitmap rgba = new GifBitmap(2, 1, new byte[]
        {
            255, 0, 0, 255,
            0, 0, 255, 255
        });

        GifDocument indexedDocument = factory.CreateStill(indexed, localOptions);
        GifDocument rgbaDocument = factory.CreateStill(rgba, null);
        GifDocument indexedDecoded = decoder.DecodeFrameBytes(encoder.Encode(indexedDocument, null), 0, null);
        GifAnimation rgbaDecoded = decoder.DecodeAnimationBytes(encoder.Encode(rgbaDocument, null), null);

        Assert.That(indexedDocument.LogicalScreen.Width, Is.EqualTo(2));
        Assert.That(indexedDocument.LogicalScreen.Height, Is.EqualTo(2));
        Assert.That(indexedDocument.LogicalScreen.HasGlobalColorTable, Is.False);
        Assert.That(indexedDocument.GlobalColorTable.IsEmpty(), Is.True);
        Assert.That(indexedDocument.Frames, Has.Length.EqualTo(1));
        AssertFrameRectangle(indexedDocument.Frames[0], 0, 0, 2, 2);
        Assert.That(indexedDocument.Frames[0].HasLocalColorTable, Is.True);
        Assert.That(indexedDocument.Frames[0].LocalColorTable.Count, Is.EqualTo(4));
        Assert.That(indexedDecoded.Frames[0].IndexedPixels, Is.EqualTo(new byte[] { 0, 1, 2, 3 }));
        Assert.That(rgbaDocument.LogicalScreen.Width, Is.EqualTo(2));
        Assert.That(rgbaDocument.LogicalScreen.Height, Is.EqualTo(1));
        Assert.That(rgbaDocument.GlobalColorTable.Count, Is.EqualTo(2));
        Assert.That(rgbaDecoded.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(rgba.RgbaPixels));
    }

    [Test]
    public void PublicAnimationFactoryAppliesDimensionsOffsetsRectanglesAndPlaybackOptions()
    {
        IGifFactory factory = new GifCodec();
        GifAnimationOptions options = new GifAnimationOptions();
        options.DelaysMilliseconds = new int[] { 50, 70, 90 };
        options.DisposalMethods = new GifDisposalMethod[]
        {
            GifDisposalMethod.DoNotDispose,
            GifDisposalMethod.RestoreToBackground,
            GifDisposalMethod.RestoreToPrevious
        };
        options.LoopCount = 4;
        options.FrameLefts = new int[] { 0, 2, 1 };
        options.FrameTops = new int[] { 0, 1, 3 };
        options.UseTransparency = true;
        options.TransparentColorIndex = 0;
        GifBitmap[] frames = new GifBitmap[]
        {
            CreateIndexedBitmap(factory, 2, 2, new byte[] { 0, 1, 2, 3 }, CreatePalette()),
            CreateIndexedBitmap(factory, 1, 2, new byte[] { 1, 2 }, CreatePalette()),
            CreateIndexedBitmap(factory, 2, 1, new byte[] { 3, 1 }, CreatePalette())
        };

        GifDocument document = factory.CreateAnimation(frames, options, null);

        Assert.That(document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(document.LogicalScreen.Width, Is.EqualTo(3));
        Assert.That(document.LogicalScreen.Height, Is.EqualTo(4));
        Assert.That(document.LoopCount, Is.EqualTo(4));
        Assert.That(document.Frames, Has.Length.EqualTo(3));
        AssertFrameRectangle(document.Frames[0], 0, 0, 2, 2);
        AssertFrameRectangle(document.Frames[1], 2, 1, 1, 2);
        AssertFrameRectangle(document.Frames[2], 1, 3, 2, 1);
        Assert.That(document.Frames[0].DelayHundredths, Is.EqualTo(5));
        Assert.That(document.Frames[1].DelayHundredths, Is.EqualTo(7));
        Assert.That(document.Frames[2].DelayHundredths, Is.EqualTo(9));
        Assert.That(document.Frames[0].DisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(document.Frames[1].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToBackground));
        Assert.That(document.Frames[2].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
        Assert.That(document.Frames[0].HasTransparency, Is.True);
        Assert.That(document.Frames[1].HasTransparency, Is.True);
        Assert.That(document.Frames[2].HasTransparency, Is.True);
        Assert.That(document.Frames[0].TransparentColorIndex, Is.EqualTo(0));
        Assert.That(document.Frames[1].TransparentColorIndex, Is.EqualTo(0));
        Assert.That(document.Frames[2].TransparentColorIndex, Is.EqualTo(0));
    }

    [Test]
    public void PublicAnimationFactoryUsesDefaultsForMissingOptionArrayEntries()
    {
        IGifFactory factory = new GifCodec();
        GifAnimationOptions options = new GifAnimationOptions();
        options.DelaysMilliseconds = new int[] { 20 };
        options.DefaultDelayMilliseconds = 90;
        options.DisposalMethods = new GifDisposalMethod[] { GifDisposalMethod.RestoreToBackground };
        options.DefaultDisposalMethod = GifDisposalMethod.RestoreToPrevious;
        options.FrameLefts = new int[] { 0, 1 };
        options.FrameTops = new int[] { 1 };
        GifBitmap[] frames = new GifBitmap[]
        {
            CreateIndexedBitmap(factory, 1, 1, new byte[] { 0 }, CreatePalette()),
            CreateIndexedBitmap(factory, 1, 1, new byte[] { 1 }, CreatePalette()),
            CreateIndexedBitmap(factory, 1, 1, new byte[] { 2 }, CreatePalette())
        };

        GifDocument document = factory.CreateAnimation(frames, options, null);

        Assert.That(document.LogicalScreen.Width, Is.EqualTo(2));
        Assert.That(document.LogicalScreen.Height, Is.EqualTo(2));
        Assert.That(document.Frames[0].DelayHundredths, Is.EqualTo(2));
        Assert.That(document.Frames[1].DelayHundredths, Is.EqualTo(9));
        Assert.That(document.Frames[2].DelayHundredths, Is.EqualTo(9));
        Assert.That(document.Frames[0].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToBackground));
        Assert.That(document.Frames[1].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
        Assert.That(document.Frames[2].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
        AssertFrameRectangle(document.Frames[0], 0, 1, 1, 1);
        AssertFrameRectangle(document.Frames[1], 1, 0, 1, 1);
        AssertFrameRectangle(document.Frames[2], 0, 0, 1, 1);
    }

    [Test]
    public void PublicAnimationFactorySelectsGlobalLocalAndAutomaticPalettes()
    {
        IGifFactory factory = new GifCodec();
        GifPalette firstPalette = CreatePalette();
        GifPalette secondPalette = new GifPalette(new GifColor[]
        {
            new GifColor(255, 255, 255),
            new GifColor(255, 255, 0),
            new GifColor(255, 0, 255),
            new GifColor(0, 255, 255)
        });
        GifBitmap[] matchingFrames = new GifBitmap[]
        {
            CreateIndexedBitmap(factory, 1, 1, new byte[] { 0 }, firstPalette),
            CreateIndexedBitmap(factory, 1, 1, new byte[] { 1 }, firstPalette)
        };
        GifBitmap[] differingFrames = new GifBitmap[]
        {
            CreateIndexedBitmap(factory, 1, 1, new byte[] { 0 }, firstPalette),
            CreateIndexedBitmap(factory, 1, 1, new byte[] { 1 }, secondPalette)
        };
        GifEncodeOptions globalOptions = new GifEncodeOptions();
        globalOptions.ColorTableMode = GifColorTableMode.Global;
        GifEncodeOptions localOptions = new GifEncodeOptions();
        localOptions.ColorTableMode = GifColorTableMode.Local;
        GifEncodeOptions automaticOptions = new GifEncodeOptions();
        automaticOptions.ColorTableMode = GifColorTableMode.Auto;

        GifDocument global = factory.CreateAnimation(matchingFrames, null, globalOptions);
        GifDocument local = factory.CreateAnimation(matchingFrames, null, localOptions);
        GifDocument automaticGlobal = factory.CreateAnimation(matchingFrames, null, automaticOptions);
        GifDocument automaticLocal = factory.CreateAnimation(differingFrames, null, automaticOptions);

        Assert.That(global.LogicalScreen.HasGlobalColorTable, Is.True);
        Assert.That(global.Frames[0].HasLocalColorTable, Is.False);
        Assert.That(local.LogicalScreen.HasGlobalColorTable, Is.False);
        Assert.That(local.Frames[0].HasLocalColorTable, Is.True);
        Assert.That(local.Frames[1].HasLocalColorTable, Is.True);
        Assert.That(automaticGlobal.LogicalScreen.HasGlobalColorTable, Is.True);
        Assert.That(automaticGlobal.Frames[0].HasLocalColorTable, Is.False);
        Assert.That(automaticLocal.LogicalScreen.HasGlobalColorTable, Is.False);
        Assert.That(automaticLocal.Frames[0].HasLocalColorTable, Is.True);
        Assert.That(automaticLocal.Frames[1].HasLocalColorTable, Is.True);
        Assert.That(automaticLocal.Frames[1].LocalColorTable.Colors[1].Red, Is.EqualTo(255));
        Assert.That(automaticLocal.Frames[1].LocalColorTable.Colors[1].Green, Is.EqualTo(255));
    }

    [Test]
    public void PublicAnimationFactoryCreatesRgbaAnimationWithPlaybackMetadata()
    {
        GifCodec codec = new GifCodec();
        IGifFactory factory = codec;
        IGifEncoder encoder = codec;
        IGifDecoder decoder = codec;
        GifAnimationOptions options = new GifAnimationOptions();
        options.DelaysMilliseconds = new int[] { 30, 40 };
        options.DisposalMethods = new GifDisposalMethod[]
        {
            GifDisposalMethod.DoNotDispose,
            GifDisposalMethod.RestoreToPrevious
        };
        options.LoopCount = 2;
        GifBitmap[] frames = new GifBitmap[]
        {
            new GifBitmap(1, 1, new byte[] { 255, 0, 0, 255 }),
            new GifBitmap(1, 1, new byte[] { 0, 0, 255, 255 })
        };

        GifDocument document = factory.CreateAnimation(frames, options, null);
        GifAnimation decoded = decoder.DecodeAnimationBytes(encoder.Encode(document, null), null);

        Assert.That(document.Frames, Has.Length.EqualTo(2));
        Assert.That(document.LoopCount, Is.EqualTo(2));
        Assert.That(document.Frames[0].DelayHundredths, Is.EqualTo(3));
        Assert.That(document.Frames[1].DelayHundredths, Is.EqualTo(4));
        Assert.That(document.Frames[1].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
        Assert.That(decoded.TotalDurationMilliseconds, Is.EqualTo(70));
        Assert.That(decoded.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(frames[0].RgbaPixels));
        Assert.That(decoded.Frames[1].Bitmap.RgbaPixels, Is.EqualTo(frames[1].RgbaPixels));
    }

    [Test]
    public void PublicFactoryNormalizesNullInputsAndRejectsInvalidFrameSetsAtomically()
    {
        IGifFactory factory = new GifCodec();
        GifDocument nullStill = factory.CreateStill(null, null);
        GifDocument nullFrames = factory.CreateAnimation(null, null, null);
        GifDocument emptyFrames = factory.CreateAnimation(new GifBitmap[0], null, null);
        GifDocument missingFrame = factory.CreateAnimation(new GifBitmap[] { null! }, null, null);
        GifDocument mixedFrames = factory.CreateAnimation(new GifBitmap[]
        {
            CreateIndexedBitmap(factory, 1, 1, new byte[] { 0 }, CreatePalette()),
            new GifBitmap(1, 1, new byte[] { 255, 0, 0, 255 })
        }, null, null);

        AssertEmptyDocument(nullStill);
        AssertEmptyDocument(nullFrames);
        AssertEmptyDocument(emptyFrames);
        AssertEmptyDocument(missingFrame);
        AssertEmptyDocument(mixedFrames);
    }

    [Test]
    public void PublicFactoryRejectsInvalidPaletteIndexesAndTransparencyAtomically()
    {
        IGifFactory factory = new GifCodec();
        GifPalette palette = CreatePalette();
        GifBitmap invalid = CreateIndexedBitmap(factory, 1, 1, new byte[] { 4 }, palette);
        GifDocument invalidStill = factory.CreateStill(invalid, null);
        GifDocument invalidAnimation = factory.CreateAnimation(new GifBitmap[]
        {
            CreateIndexedBitmap(factory, 1, 1, new byte[] { 0 }, palette),
            invalid
        }, null, null);
        GifAnimationOptions transparencyOptions = new GifAnimationOptions();
        transparencyOptions.UseTransparency = true;
        transparencyOptions.TransparentColorIndex = 4;
        GifDocument invalidTransparency = factory.CreateAnimation(new GifBitmap[]
        {
            CreateIndexedBitmap(factory, 1, 1, new byte[] { 0 }, palette)
        }, transparencyOptions, null);

        AssertEmptyDocument(invalidStill);
        AssertEmptyDocument(invalidAnimation);
        AssertEmptyDocument(invalidTransparency);
    }

    static GifBitmap CreateIndexedBitmap(IGifFactory factory, int width, int height, byte[] pixels, GifPalette palette)
    {
        return factory.CreateIndexedBitmap(width, height, pixels, palette);
    }

    static GifPalette CreatePalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 0, 0),
            new GifColor(0, 255, 0),
            new GifColor(0, 0, 255)
        });
    }

    static void AssertFrameRectangle(GifFrame frame, int left, int top, int width, int height)
    {
        Assert.That(frame.Left, Is.EqualTo(left));
        Assert.That(frame.Top, Is.EqualTo(top));
        Assert.That(frame.Width, Is.EqualTo(width));
        Assert.That(frame.Height, Is.EqualTo(height));
    }

    static void AssertEmptyDocument(GifDocument document)
    {
        Assert.That(document.Version, Is.EqualTo(GifVersion.Unknown));
        Assert.That(document.LogicalScreen.Width, Is.EqualTo(0));
        Assert.That(document.LogicalScreen.Height, Is.EqualTo(0));
        Assert.That(document.Frames, Is.Empty);
    }
}
