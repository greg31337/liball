using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPublicCoalesceTests
{
    [Test]
    public void Coalesce_FromDocumentReturnsCoalescedAnimation()
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

        GifAnimation animation = Gif.Coalesce(document);

        Assert.That(animation.Width, Is.EqualTo(3));
        Assert.That(animation.Height, Is.EqualTo(1));
        Assert.That(animation.LoopCount, Is.EqualTo(2));
        Assert.That(animation.TotalDurationMilliseconds, Is.EqualTo(120));
        Assert.That(animation.Frames.Length, Is.EqualTo(2));
        Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 255, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255 }));
        Assert.That(animation.Frames[1].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 0, 255 }));
    }

    [Test]
    public void Coalesce_HonorsDecodeOptions()
    {
        GifDocument document = CreateDocument(1, 1);
        GifFrame frame = CreateFrame(0, 0, 1, 1, new byte[] { 1 });
        frame.DelayHundredths = 0;
        frame.DisposalMethod = GifDisposalMethod.DoNotDispose;
        document.Frames = new GifFrame[] { frame };
        GifDecodeOptions options = new GifDecodeOptions();
        options.MinimumFrameDelayMilliseconds = 25;

        GifAnimation animation = Gif.Coalesce(document, options);

        Assert.That(animation.Frames.Length, Is.EqualTo(1));
        Assert.That(animation.Frames[0].DelayMilliseconds, Is.EqualTo(25));
        Assert.That(animation.TotalDurationMilliseconds, Is.EqualTo(25));
        Assert.That(animation.Warnings.Length, Is.EqualTo(1));
        Assert.That(animation.Warnings[0].Code, Is.EqualTo(GifWarningCode.ZeroDelayNormalized));
    }

    [Test]
    public void Coalesce_NullDocumentReturnsEmptyAnimation()
    {
        GifAnimation animation = Gif.Coalesce(null);

        Assert.That(animation.Width, Is.EqualTo(0));
        Assert.That(animation.Height, Is.EqualTo(0));
        Assert.That(animation.Frames, Is.Empty);
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
