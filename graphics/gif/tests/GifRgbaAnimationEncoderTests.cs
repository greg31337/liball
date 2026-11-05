using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifRgbaAnimationEncoderTests
{
    [Test]
    public void EncodeRgbaAnimation_QuantizesGlobalPaletteAndRoundTripsFrames()
    {
        byte[] firstPixels = new byte[]
        {
            255, 0, 0, 255,
            0, 0, 255, 255
        };
        byte[] secondPixels = new byte[]
        {
            0, 255, 0, 255,
            255, 0, 0, 255
        };
        GifAnimation animation = new GifAnimation();
        animation.Width = 2;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new GifBitmap(2, 1, firstPixels)),
            CreateFrame(new GifBitmap(2, 1, secondPixels))
        };

        GifEncodeResult encoded = Gif.EncodeRgbaAnimation(animation);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);
        GifRawFrameDecodeResult first = new GifRawFrameDecoder().DecodeFrame(parsed.Document.Frames[0]);
        GifRawFrameDecodeResult second = new GifRawFrameDecoder().DecodeFrame(parsed.Document.Frames[1]);
        GifRgbaConversionResult firstRgba = new GifIndexedToRgbaConverter().ConvertIndexedPixels(first.IndexedPixels, 2, 1, parsed.Document.GlobalColorTable);
        GifRgbaConversionResult secondRgba = new GifIndexedToRgbaConverter().ConvertIndexedPixels(second.IndexedPixels, 2, 1, parsed.Document.GlobalColorTable);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif87a));
        Assert.That(parsed.Document.LogicalScreen.Width, Is.EqualTo(2));
        Assert.That(parsed.Document.LogicalScreen.Height, Is.EqualTo(1));
        Assert.That(parsed.Document.GlobalColorTable.Count, Is.EqualTo(4));
        Assert.That(parsed.Document.Frames.Length, Is.EqualTo(2));
        Assert.That(first.Success, Is.True);
        Assert.That(second.Success, Is.True);
        Assert.That(firstRgba.Success, Is.True);
        Assert.That(secondRgba.Success, Is.True);
        Assert.That(firstRgba.RgbaPixels, Is.EqualTo(firstPixels));
        Assert.That(secondRgba.RgbaPixels, Is.EqualTo(secondPixels));
        Assert.That(encoded.Document.EncodeStatistics.FramesWritten, Is.EqualTo(2));
        Assert.That(encoded.Document.EncodeStatistics.IndexedPixelCount, Is.EqualTo(4));
    }

    [Test]
    public void EncodeRgbaAnimation_FromBitmapArrayUsesFirstFrameSize()
    {
        GifBitmap[] bitmaps = new GifBitmap[]
        {
            new GifBitmap(1, 1, new byte[] { 255, 0, 0, 255 }),
            new GifBitmap(1, 1, new byte[] { 0, 0, 255, 255 })
        };

        GifEncodeResult encoded = Gif.EncodeRgbaAnimation(bitmaps);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.LogicalScreen.Width, Is.EqualTo(1));
        Assert.That(parsed.Document.LogicalScreen.Height, Is.EqualTo(1));
        Assert.That(parsed.Document.Frames.Length, Is.EqualTo(2));
    }

    [Test]
    public void EncodeRgbaAnimation_UsesQuantizerAndDitherOptionsAcrossFrames()
    {
        GifAnimation animation = new GifAnimation();
        animation.Width = 2;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new GifBitmap(2, 1, new byte[]
            {
                0, 0, 0, 255,
                255, 255, 255, 255
            })),
            CreateFrame(new GifBitmap(2, 1, new byte[]
            {
                120, 120, 120, 255,
                120, 120, 120, 255
            }))
        };
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 2;
        options.QuantizerMode = GifQuantizerMode.Grayscale;
        options.DitherMode = GifDitherMode.None;

        GifEncodeResult encoded = Gif.EncodeRgbaAnimation(animation, options);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);
        GifRawFrameDecodeResult first = new GifRawFrameDecoder().DecodeFrame(parsed.Document.Frames[0]);
        GifRawFrameDecodeResult second = new GifRawFrameDecoder().DecodeFrame(parsed.Document.Frames[1]);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.GlobalColorTable.Count, Is.EqualTo(2));
        Assert.That(first.Success, Is.True);
        Assert.That(second.Success, Is.True);
        Assert.That(first.IndexedPixels, Is.EqualTo(new byte[] { 0, 1 }));
        Assert.That(second.IndexedPixels, Is.EqualTo(new byte[] { 0, 0 }));
    }

    [Test]
    public void EncodeRgbaAnimation_RejectsInvalidFrameRgbaByteCount()
    {
        GifAnimation animation = new GifAnimation();
        animation.Width = 2;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new GifBitmap(2, 1, new byte[] { 255, 0, 0, 255 }))
        };

        GifEncodeResult encoded = Gif.EncodeRgbaAnimation(animation);

        Assert.That(encoded.Success, Is.False);
        Assert.That(encoded.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
        Assert.That(encoded.Error.Position.FrameIndex, Is.EqualTo(0));
        Assert.That(encoded.Data, Is.Empty);
    }

    [Test]
    public void EncodeRgbaAnimation_RejectsFrameOutsideLogicalScreen()
    {
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new GifBitmap(1, 1, new byte[] { 255, 0, 0, 255 }), 1, 0)
        };

        GifEncodeResult encoded = Gif.EncodeRgbaAnimation(animation);

        Assert.That(encoded.Success, Is.False);
        Assert.That(encoded.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(encoded.Error.Position.FrameIndex, Is.EqualTo(0));
        Assert.That(encoded.Data, Is.Empty);
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
