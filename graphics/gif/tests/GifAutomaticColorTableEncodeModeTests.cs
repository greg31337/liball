using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifAutomaticColorTableEncodeModeTests
{
    [Test]
    public void EncodeIndexedAnimation_AutoModeUsesGlobalWhenFramePalettesMatch()
    {
        GifPalette palette = CreatePalette(new GifColor(0, 0, 0), new GifColor(255, 255, 255));
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, palette)),
            CreateFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 1 }, palette))
        };

        GifEncodeResult encoded = Gif.EncodeIndexedAnimation(animation);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.LogicalScreen.HasGlobalColorTable, Is.True);
        Assert.That(parsed.Document.GlobalColorTable.Count, Is.EqualTo(2));
        Assert.That(parsed.Document.Frames[0].HasLocalColorTable, Is.False);
        Assert.That(parsed.Document.Frames[1].HasLocalColorTable, Is.False);
    }

    [Test]
    public void EncodeIndexedAnimation_AutoModeUsesLocalWhenFramePalettesDiffer()
    {
        GifPalette firstPalette = CreatePalette(new GifColor(0, 0, 0), new GifColor(255, 255, 255));
        GifPalette secondPalette = CreatePalette(new GifColor(255, 0, 0), new GifColor(0, 0, 255));
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 1 }, firstPalette)),
            CreateFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 1 }, secondPalette))
        };

        GifEncodeResult encoded = Gif.EncodeIndexedAnimation(animation);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.LogicalScreen.HasGlobalColorTable, Is.False);
        Assert.That(parsed.Document.GlobalColorTable.IsEmpty(), Is.True);
        Assert.That(parsed.Document.Frames[0].HasLocalColorTable, Is.True);
        Assert.That(parsed.Document.Frames[1].HasLocalColorTable, Is.True);
        Assert.That(parsed.Document.Frames[0].LocalColorTable.Colors[1].Red, Is.EqualTo(255));
        Assert.That(parsed.Document.Frames[1].LocalColorTable.Colors[1].Blue, Is.EqualTo(255));
    }

    [Test]
    public void EncodeRgbaAnimation_AutoModeFallsBackToLocalWhenGlobalPaletteExceedsLimit()
    {
        GifAnimation animation = new GifAnimation();
        animation.Width = 2;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new GifBitmap(2, 1, new byte[]
            {
                255, 0, 0, 255,
                0, 255, 0, 255
            })),
            CreateFrame(new GifBitmap(2, 1, new byte[]
            {
                0, 0, 255, 255,
                255, 255, 0, 255
            }))
        };
        GifQuantizeOptions quantizeOptions = new GifQuantizeOptions();
        quantizeOptions.MaxColors = 2;
        GifEncodeOptions encodeOptions = new GifEncodeOptions();
        encodeOptions.ColorTableMode = GifColorTableMode.Auto;

        GifEncodeResult encoded = Gif.EncodeRgbaAnimation(animation, quantizeOptions, encodeOptions);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.LogicalScreen.HasGlobalColorTable, Is.False);
        Assert.That(parsed.Document.GlobalColorTable.IsEmpty(), Is.True);
        Assert.That(parsed.Document.Frames[0].HasLocalColorTable, Is.True);
        Assert.That(parsed.Document.Frames[1].HasLocalColorTable, Is.True);
        Assert.That(parsed.Document.Frames[0].LocalColorTable.Count, Is.EqualTo(2));
        Assert.That(parsed.Document.Frames[1].LocalColorTable.Count, Is.EqualTo(2));
    }

    [Test]
    public void CopyEncodeOptionsWithColorTableMode_DoesNotMutateSourceOptions()
    {
        GifEncodeOptions options = new GifEncodeOptions();
        options.ColorTableMode = GifColorTableMode.Auto;
        options.Interlace = true;
        options.MaxBytes = 1234;
        options.RemoveZeroDelayFrames = true;
        options.RemoveDuplicateFrames = true;
        options.NormalizeFrameDelays = true;
        options.MinimumFrameDelayMilliseconds = 25;
        options.DeconstructFrames = true;
        options.RecalculateTimeline = false;

        GifEncodeOptions copy = new GifAutomaticColorTableEncoder().CopyEncodeOptionsWithColorTableMode(options, GifColorTableMode.Local);

        Assert.That(options.ColorTableMode, Is.EqualTo(GifColorTableMode.Auto));
        Assert.That(copy.ColorTableMode, Is.EqualTo(GifColorTableMode.Local));
        Assert.That(copy.Interlace, Is.True);
        Assert.That(copy.MaxBytes, Is.EqualTo(1234));
        Assert.That(copy.RemoveZeroDelayFrames, Is.True);
        Assert.That(copy.RemoveDuplicateFrames, Is.True);
        Assert.That(copy.NormalizeFrameDelays, Is.True);
        Assert.That(copy.MinimumFrameDelayMilliseconds, Is.EqualTo(25));
        Assert.That(copy.DeconstructFrames, Is.True);
        Assert.That(copy.RecalculateTimeline, Is.False);
    }

    static GifPalette CreatePalette(GifColor first, GifColor second)
    {
        return new GifPalette(new GifColor[] { first, second });
    }

    static GifAnimationFrame CreateFrame(GifBitmap bitmap)
    {
        GifAnimationFrame frame = new GifAnimationFrame();
        frame.Bitmap = bitmap;
        frame.SourceLeft = 0;
        frame.SourceTop = 0;
        frame.SourceWidth = bitmap.Width;
        frame.SourceHeight = bitmap.Height;
        return frame;
    }
}
