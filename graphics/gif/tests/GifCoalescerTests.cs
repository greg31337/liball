using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifCoalescerTests
{
    [Test]
    public void Coalesce_OverlaysSubRectangleFramesIntoFullCanvasFrames()
    {
        GifDocument document = CreateDocument(3, 1);
        GifFrame first = CreateFrame(0, 0, 3, 1, new byte[] { 1, 0, 0 });
        first.DelayHundredths = 5;
        first.DisposalMethod = GifDisposalMethod.DoNotDispose;
        GifFrame second = CreateFrame(1, 0, 1, 1, new byte[] { 2 });
        second.DelayHundredths = 7;
        second.DisposalMethod = GifDisposalMethod.DoNotDispose;
        document.Frames = new GifFrame[] { first, second };
        document.LoopCount = 2;

        GifCoalesceResult result = new GifCoalescer().Coalesce(document);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Animation.Width, Is.EqualTo(3));
        Assert.That(result.Animation.Height, Is.EqualTo(1));
        Assert.That(result.Animation.LoopCount, Is.EqualTo(2));
        Assert.That(result.Animation.TotalDurationMilliseconds, Is.EqualTo(120));
        Assert.That(result.Animation.Frames.Length, Is.EqualTo(2));
        Assert.That(result.Animation.Frames[0].DelayMilliseconds, Is.EqualTo(50));
        Assert.That(result.Animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 255, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255 }));
        Assert.That(result.Animation.Frames[1].DelayMilliseconds, Is.EqualTo(70));
        Assert.That(result.Animation.Frames[1].SourceLeft, Is.EqualTo(1));
        Assert.That(result.Animation.Frames[1].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 0, 255 }));
    }

    [Test]
    public void Coalesce_AppliesRestoreToBackgroundBeforeNextFrame()
    {
        GifDocument document = CreateDocument(2, 1);
        GifFrame first = CreateFrame(0, 0, 2, 1, new byte[] { 1, 1 });
        first.DisposalMethod = GifDisposalMethod.RestoreToBackground;
        GifFrame second = CreateFrame(1, 0, 1, 1, new byte[] { 2 });
        second.DisposalMethod = GifDisposalMethod.DoNotDispose;
        document.Frames = new GifFrame[] { first, second };

        GifCoalesceResult result = new GifCoalescer().Coalesce(document);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 255, 0, 0, 255, 255, 0, 0, 255 }));
        Assert.That(result.Animation.Frames[1].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 0, 0, 0, 255, 0, 255, 0, 255 }));
    }

    [Test]
    public void Coalesce_AppliesRestoreToPreviousBeforeNextFrame()
    {
        GifDocument document = CreateDocument(2, 1);
        GifFrame first = CreateFrame(0, 0, 2, 1, new byte[] { 1, 1 });
        first.DisposalMethod = GifDisposalMethod.DoNotDispose;
        GifFrame second = CreateFrame(1, 0, 1, 1, new byte[] { 2 });
        second.DisposalMethod = GifDisposalMethod.RestoreToPrevious;
        GifFrame third = CreateFrame(0, 0, 1, 1, new byte[] { 3 });
        third.DisposalMethod = GifDisposalMethod.DoNotDispose;
        document.Frames = new GifFrame[] { first, second, third };

        GifCoalesceResult result = new GifCoalescer().Coalesce(document);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 255, 0, 0, 255, 255, 0, 0, 255 }));
        Assert.That(result.Animation.Frames[1].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 255, 0, 0, 255, 0, 255, 0, 255 }));
        Assert.That(result.Animation.Frames[2].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 0, 0, 255, 255, 255, 0, 0, 255 }));
        Assert.That(result.Animation.Frames[1].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
    }

    [Test]
    public void Coalesce_UsesReservedDisposalFallbackAndCollectsWarning()
    {
        GifDocument document = CreateDocument(1, 1);
        GifFrame frame = CreateFrame(0, 0, 1, 1, new byte[] { 1 });
        frame.DisposalMethod = GifDisposalMethod.Reserved7;
        document.Frames = new GifFrame[] { frame };

        GifCoalesceResult result = new GifCoalescer().CoalesceWithOptions(document, new GifDecodeOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Animation.Frames[0].DisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(result.Animation.Warnings.Length, Is.EqualTo(1));
        Assert.That(result.Animation.Warnings[0].Code, Is.EqualTo(GifWarningCode.ReservedDisposalMethod));
        Assert.That(result.Warnings, Is.SameAs(result.Animation.Warnings));
    }

    [Test]
    public void Coalesce_NormalizesZeroDelayUsingDecodeOption()
    {
        GifDocument document = CreateDocument(1, 1);
        GifFrame frame = CreateFrame(0, 0, 1, 1, new byte[] { 1 });
        frame.DelayHundredths = 0;
        frame.DisposalMethod = GifDisposalMethod.DoNotDispose;
        document.Frames = new GifFrame[] { frame };
        GifDecodeOptions options = new GifDecodeOptions();
        options.MinimumFrameDelayMilliseconds = 25;

        GifCoalesceResult result = new GifCoalescer().CoalesceWithOptions(document, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Animation.Frames[0].DelayMilliseconds, Is.EqualTo(25));
        Assert.That(result.Animation.TotalDurationMilliseconds, Is.EqualTo(25));
        Assert.That(result.Animation.Warnings.Length, Is.EqualTo(1));
        Assert.That(result.Animation.Warnings[0].Code, Is.EqualTo(GifWarningCode.ZeroDelayNormalized));
    }

    [Test]
    public void Coalesce_RejectsInvalidFramePixels()
    {
        GifDocument document = CreateDocument(1, 1);
        GifFrame frame = CreateFrame(0, 0, 1, 1, new byte[] { 9 });
        document.Frames = new GifFrame[] { frame };

        GifCoalesceResult result = new GifCoalescer().Coalesce(document);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidPaletteIndex));
    }

    static GifDocument CreateDocument(int width, int height)
    {
        GifDocument document = new GifDocument();
        document.LogicalScreen = new GifLogicalScreen(width, height);
        document.LogicalScreen.HasGlobalColorTable = true;
        document.LogicalScreen.BackgroundColorIndex = 0;
        document.GlobalColorTable = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 0, 0),
            new GifColor(0, 255, 0),
            new GifColor(0, 0, 255)
        });
        return document;
    }

    static GifFrame CreateFrame(int left, int top, int width, int height, byte[] indexedPixels)
    {
        GifFrame frame = new GifFrame(left, top, width, height);
        frame.IndexedPixels = indexedPixels;
        return frame;
    }
}
