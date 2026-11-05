using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPublicTranscoderTests
{
    [Test]
    public void SetDelay_UpdatesAllFrameDelaysAndDuration()
    {
        GifAnimation animation = CreateRgbaAnimation();

        GifAnimation result = Gif.SetDelay(animation, 25);

        Assert.That(result, Is.SameAs(animation));
        Assert.That(animation.Frames[0].DelayMilliseconds, Is.EqualTo(25));
        Assert.That(animation.Frames[1].DelayMilliseconds, Is.EqualTo(25));
        Assert.That(animation.TotalDurationMilliseconds, Is.EqualTo(50));
    }

    [Test]
    public void SetDelays_UpdatesPerFrameDelaysAndDuration()
    {
        GifAnimation animation = CreateRgbaAnimation();

        Gif.SetDelays(animation, new int[] { 30, 40 });

        Assert.That(animation.Frames[0].DelayMilliseconds, Is.EqualTo(30));
        Assert.That(animation.Frames[1].DelayMilliseconds, Is.EqualTo(40));
        Assert.That(animation.TotalDurationMilliseconds, Is.EqualTo(70));
    }

    [Test]
    public void SetDisposal_UpdatesAllFrameDisposalMethods()
    {
        GifAnimation animation = CreateRgbaAnimation();

        Gif.SetDisposal(animation, GifDisposalMethod.RestoreToBackground);

        Assert.That(animation.Frames[0].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToBackground));
        Assert.That(animation.Frames[1].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToBackground));
    }

    [Test]
    public void SetDisposals_UpdatesPerFrameDisposalMethods()
    {
        GifAnimation animation = CreateRgbaAnimation();

        Gif.SetDisposals(animation, new GifDisposalMethod[]
        {
            GifDisposalMethod.DoNotDispose,
            GifDisposalMethod.RestoreToPrevious
        });

        Assert.That(animation.Frames[0].DisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(animation.Frames[1].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
    }

    [Test]
    public void SetLoopCount_UpdatesAnimationLoopCount()
    {
        GifAnimation animation = CreateRgbaAnimation();

        Gif.SetLoopCount(animation, 3);

        Assert.That(animation.LoopCount, Is.EqualTo(3));
    }

    [Test]
    public void SetLoopCount_UpdatesDocumentLoopCountAndReplacesExistingLoopExtension()
    {
        GifDocument document = CreateLoopDocument(2);

        Gif.SetLoopCount(document, 5);
        GifDocument parsed = Gif.Decode(Gif.Encode(document));

        Assert.That(document.LoopCount, Is.EqualTo(5));
        Assert.That(document.ApplicationExtensions, Is.Empty);
        Assert.That(parsed.LoopCount, Is.EqualTo(5));
        Assert.That(parsed.ApplicationExtensions.Length, Is.EqualTo(1));
    }

    [Test]
    public void SetComments_ReplacesDocumentComments()
    {
        GifDocument document = Gif.CreateStill(CreateIndexedBitmap(0));

        Gif.SetComments(document, new string[] { "first", "second" });
        GifDocument parsed = Gif.Decode(Gif.Encode(document));

        Assert.That(document.Comments.Length, Is.EqualTo(2));
        Assert.That(document.Comments[0].Text, Is.EqualTo("first"));
        Assert.That(document.Comments[1].Text, Is.EqualTo("second"));
        Assert.That(parsed.Comments.Length, Is.EqualTo(2));
        Assert.That(parsed.Comments[0].Text, Is.EqualTo("first"));
        Assert.That(parsed.Comments[1].Text, Is.EqualTo("second"));
    }

    [Test]
    public void SetPalette_AppliesGlobalPaletteAndClearsLocalTables()
    {
        GifDocument document = Gif.CreateAnimation(new GifBitmap[] { CreateIndexedBitmap(0), CreateIndexedBitmap(1) });
        GifPalette palette = CreateAlternatePalette();

        Gif.SetPalette(document, palette);

        Assert.That(document.LogicalScreen.HasGlobalColorTable, Is.True);
        Assert.That(document.GlobalColorTable, Is.SameAs(palette));
        Assert.That(document.Frames[0].HasLocalColorTable, Is.False);
        Assert.That(document.Frames[1].HasLocalColorTable, Is.False);
    }

    [Test]
    public void SetPalettes_AppliesLocalPalettesAndClearsGlobalTable()
    {
        GifDocument document = Gif.CreateAnimation(new GifBitmap[] { CreateIndexedBitmap(0), CreateIndexedBitmap(1) });
        GifPalette first = CreatePalette();
        GifPalette second = CreateAlternatePalette();

        Gif.SetPalettes(document, new GifPalette[] { first, second });

        Assert.That(document.LogicalScreen.HasGlobalColorTable, Is.False);
        Assert.That(document.GlobalColorTable.IsEmpty(), Is.True);
        Assert.That(document.Frames[0].HasLocalColorTable, Is.True);
        Assert.That(document.Frames[0].LocalColorTable, Is.SameAs(first));
        Assert.That(document.Frames[1].HasLocalColorTable, Is.True);
        Assert.That(document.Frames[1].LocalColorTable, Is.SameAs(second));
    }

    [Test]
    public void Optimize_UsesRequestedOptimizationMode()
    {
        GifAnimation animation = CreateDuplicateRgbaAnimation();
        GifEncodeOptions options = new GifEncodeOptions();
        options.OptimizationMode = GifOptimizationMode.RemoveDuplicateFrames;

        GifAnimation optimized = Gif.Optimize(animation, options);

        Assert.That(optimized.Frames.Length, Is.EqualTo(1));
        Assert.That(optimized.Frames[0].DelayMilliseconds, Is.EqualTo(30));
    }

    [Test]
    public void Optimize_CombinesIndependentOptimizationSteps()
    {
        GifAnimation animation = new GifAnimation();
        animation.Width = 2;
        animation.Height = 1;
        animation.TotalDurationMilliseconds = 999;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new GifBitmap(2, 1, new byte[] { 0, 0, 0, 255, 0, 0, 0, 255 }), 10, 0),
            CreateFrame(new GifBitmap(2, 1, new byte[] { 0, 0, 0, 255, 0, 0, 0, 255 }), 20, 1),
            CreateFrame(new GifBitmap(2, 1, new byte[] { 0, 0, 0, 255, 5, 5, 5, 255 }), 0, 2),
            CreateFrame(new GifBitmap(2, 1, new byte[] { 0, 0, 0, 255, 9, 9, 9, 255 }), 30, 3)
        };
        GifEncodeOptions options = new GifEncodeOptions();
        options.RemoveZeroDelayFrames = true;
        options.RemoveDuplicateFrames = true;
        options.NormalizeFrameDelays = true;
        options.MinimumFrameDelayMilliseconds = 25;
        options.DeconstructFrames = true;
        options.RecalculateTimeline = true;

        GifAnimation optimized = Gif.Optimize(animation, options);

        Assert.That(optimized.Frames.Length, Is.EqualTo(2));
        Assert.That(optimized.Frames[0].FrameIndex, Is.EqualTo(0));
        Assert.That(optimized.Frames[0].DelayMilliseconds, Is.EqualTo(30));
        Assert.That(optimized.Frames[1].FrameIndex, Is.EqualTo(3));
        Assert.That(optimized.Frames[1].DelayMilliseconds, Is.EqualTo(30));
        Assert.That(optimized.Frames[1].SourceLeft, Is.EqualTo(1));
        Assert.That(optimized.Frames[1].SourceWidth, Is.EqualTo(1));
        Assert.That(optimized.Frames[1].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 9, 9, 9, 255 }));
        Assert.That(optimized.TotalDurationMilliseconds, Is.EqualTo(60));
        Assert.That(animation.Frames.Length, Is.EqualTo(4));
        Assert.That(animation.Frames[0].DelayMilliseconds, Is.EqualTo(10));
        Assert.That(animation.TotalDurationMilliseconds, Is.EqualTo(999));
    }

    [Test]
    public void SetInvalidDelayLeavesAnimationUnchanged()
    {
        GifAnimation animation = CreateRgbaAnimation();

        Gif.SetDelay(animation, -1);

        Assert.That(animation.Frames[0].DelayMilliseconds, Is.EqualTo(10));
        Assert.That(animation.Frames[1].DelayMilliseconds, Is.EqualTo(20));
    }

    static GifDocument CreateLoopDocument(int loopCount)
    {
        GifEncodeOptions options = new GifEncodeOptions();
        options.LoopCount = loopCount;
        GifEncodeResult encoded = Gif.EncodeIndexedAnimation(CreateIndexedAnimation(), options);
        return encoded.Document;
    }

    static GifAnimation CreateIndexedAnimation()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(CreateIndexedBitmap(0), 10, 0),
            CreateFrame(CreateIndexedBitmap(1), 20, 1)
        };
        return animation;
    }

    static GifAnimation CreateRgbaAnimation()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new GifBitmap(1, 1, new byte[] { 1, 2, 3, 255 }), 10, 0),
            CreateFrame(new GifBitmap(1, 1, new byte[] { 4, 5, 6, 255 }), 20, 1)
        };
        return animation;
    }

    static GifAnimation CreateDuplicateRgbaAnimation()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new GifBitmap(1, 1, new byte[] { 1, 2, 3, 255 }), 10, 0),
            CreateFrame(new GifBitmap(1, 1, new byte[] { 1, 2, 3, 255 }), 20, 1)
        };
        return animation;
    }

    static GifAnimationFrame CreateFrame(GifBitmap bitmap, int delayMilliseconds, int frameIndex)
    {
        GifAnimationFrame frame = new GifAnimationFrame();
        frame.Bitmap = bitmap;
        frame.DelayMilliseconds = delayMilliseconds;
        frame.SourceLeft = 0;
        frame.SourceTop = 0;
        frame.SourceWidth = bitmap.Width;
        frame.SourceHeight = bitmap.Height;
        frame.FrameIndex = frameIndex;
        return frame;
    }

    static GifBitmap CreateIndexedBitmap(int index)
    {
        return GifBitmap.CreateIndexed(1, 1, new byte[] { (byte)index }, CreatePalette());
    }

    static GifPalette CreatePalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
    }

    static GifPalette CreateAlternatePalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(255, 0, 0),
            new GifColor(0, 0, 255)
        });
    }
}
