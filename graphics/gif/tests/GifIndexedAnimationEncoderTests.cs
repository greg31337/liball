using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifIndexedAnimationEncoderTests
{
    [Test]
    public void EncodeIndexedAnimation_WritesMultipleFramesAndRoundTripsPixels()
    {
        GifPalette palette = CreatePalette();
        GifAnimation animation = new GifAnimation();
        animation.Width = 2;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(GifBitmap.CreateIndexed(2, 1, new byte[] { 0, 1 }, palette)),
            CreateFrame(GifBitmap.CreateIndexed(2, 1, new byte[] { 1, 0 }, palette))
        };

        GifEncodeResult encoded = Gif.EncodeIndexedAnimation(animation);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);
        GifRawFrameDecodeResult first = new GifRawFrameDecoder().DecodeFrame(parsed.Document.Frames[0]);
        GifRawFrameDecodeResult second = new GifRawFrameDecoder().DecodeFrame(parsed.Document.Frames[1]);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.HasTrailer, Is.True);
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif87a));
        Assert.That(parsed.Document.LogicalScreen.Width, Is.EqualTo(2));
        Assert.That(parsed.Document.LogicalScreen.Height, Is.EqualTo(1));
        Assert.That(parsed.Document.GlobalColorTable.Count, Is.EqualTo(2));
        Assert.That(parsed.Document.Frames.Length, Is.EqualTo(2));
        Assert.That(first.Success, Is.True);
        Assert.That(second.Success, Is.True);
        Assert.That(first.IndexedPixels, Is.EqualTo(new byte[] { 0, 1 }));
        Assert.That(second.IndexedPixels, Is.EqualTo(new byte[] { 1, 0 }));
        Assert.That(encoded.Document.EncodeStatistics.FramesWritten, Is.EqualTo(2));
        Assert.That(encoded.Document.EncodeStatistics.IndexedPixelCount, Is.EqualTo(4));
    }

    [Test]
    public void EncodeIndexedAnimation_FromBitmapArrayUsesFirstFrameSize()
    {
        GifPalette palette = CreatePalette();
        GifBitmap[] bitmaps = new GifBitmap[]
        {
            GifBitmap.CreateIndexed(2, 1, new byte[] { 0, 1 }, palette),
            GifBitmap.CreateIndexed(2, 1, new byte[] { 1, 1 }, palette)
        };

        GifEncodeResult encoded = Gif.EncodeIndexedAnimation(bitmaps);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.LogicalScreen.Width, Is.EqualTo(2));
        Assert.That(parsed.Document.LogicalScreen.Height, Is.EqualTo(1));
        Assert.That(parsed.Document.Frames.Length, Is.EqualTo(2));
    }

    [Test]
    public void EncodeIndexedAnimation_GlobalModeRejectsDifferentSourcePalettes()
    {
        GifPalette firstPalette = CreatePalette();
        GifPalette secondPalette = new GifPalette(new GifColor[]
        {
            new GifColor(255, 255, 255),
            new GifColor(0, 0, 0)
        });
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, firstPalette)),
            CreateFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, secondPalette))
        };

        GifEncodeOptions options = new GifEncodeOptions();
        options.ColorTableMode = GifColorTableMode.Global;

        GifEncodeResult encoded = Gif.EncodeIndexedAnimation(animation, options);

        Assert.That(encoded.Success, Is.False);
        Assert.That(encoded.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(encoded.Error.Position.FrameIndex, Is.EqualTo(1));
        Assert.That(encoded.Data, Is.Empty);
    }

    [Test]
    public void EncodeIndexedAnimation_RejectsFrameOutsideLogicalScreen()
    {
        GifPalette palette = CreatePalette();
        GifAnimation animation = new GifAnimation();
        animation.Width = 2;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(GifBitmap.CreateIndexed(2, 1, new byte[] { 0, 1 }, palette)),
            CreateFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 1 }, palette), 2, 0)
        };

        GifEncodeResult encoded = Gif.EncodeIndexedAnimation(animation);

        Assert.That(encoded.Success, Is.False);
        Assert.That(encoded.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(encoded.Error.Position.FrameIndex, Is.EqualTo(1));
        Assert.That(encoded.Data, Is.Empty);
    }

    static GifPalette CreatePalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
    }

    static GifAnimationFrame CreateFrame(GifBitmap bitmap)
    {
        return CreateFrame(bitmap, 0, 0);
    }

    static GifAnimationFrame CreateFrame(GifBitmap bitmap, int left, int top)
    {
        GifAnimationFrame frame = new GifAnimationFrame();
        frame.Bitmap = bitmap;
        frame.SourceLeft = left;
        frame.SourceTop = top;
        frame.SourceWidth = bitmap.Width;
        frame.SourceHeight = bitmap.Height;
        return frame;
    }
}
