using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifFrameTransparencyEncodeTests
{
    [Test]
    public void EncodeIndexedStill_WritesTransparentIndexAsGraphicControlExtension()
    {
        GifPalette palette = CreatePalette();
        GifBitmap bitmap = GifBitmap.CreateIndexed(2, 1, new byte[] { 0, 1 }, palette);

        GifEncodeResult encoded = Gif.EncodeIndexedStill(bitmap, true, 0);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);
        GifRawFrameDecodeResult decoded = new GifRawFrameDecoder().DecodeFrame(parsed.Document.Frames[0]);
        GifRgbaConversionResult rgba = ConvertParsedFrame(parsed.Document, decoded.IndexedPixels, 0);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(decoded.Success, Is.True);
        Assert.That(rgba.Success, Is.True);
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(parsed.ExtensionCount, Is.EqualTo(1));
        Assert.That(parsed.Document.Frames[0].HasTransparency, Is.True);
        Assert.That(parsed.Document.Frames[0].TransparentColorIndex, Is.EqualTo(0));
        Assert.That(rgba.RgbaPixels[3], Is.EqualTo(0));
        Assert.That(rgba.RgbaPixels[7], Is.EqualTo(255));
    }

    [Test]
    public void EncodeIndexedAnimation_WritesTransparentIndexAsGraphicControlExtension()
    {
        GifPalette palette = CreatePalette();
        GifAnimationFrame frame = CreateIndexedFrame(GifBitmap.CreateIndexed(2, 1, new byte[] { 0, 1 }, palette));
        frame.HasTransparency = true;
        frame.TransparentColorIndex = 0;
        GifAnimation animation = new GifAnimation();
        animation.Width = 2;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[] { frame };

        GifEncodeResult encoded = Gif.EncodeIndexedAnimation(animation);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);
        GifRawFrameDecodeResult decoded = new GifRawFrameDecoder().DecodeFrame(parsed.Document.Frames[0]);
        GifRgbaConversionResult rgba = ConvertParsedFrame(parsed.Document, decoded.IndexedPixels, 0);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(decoded.Success, Is.True);
        Assert.That(rgba.Success, Is.True);
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(parsed.ExtensionCount, Is.EqualTo(1));
        Assert.That(parsed.Document.Frames[0].HasTransparency, Is.True);
        Assert.That(parsed.Document.Frames[0].TransparentColorIndex, Is.EqualTo(0));
        Assert.That(parsed.Document.Frames[0].GraphicControl.HasTransparency, Is.True);
        Assert.That(parsed.Document.Frames[0].GraphicControl.TransparentColorIndex, Is.EqualTo(0));
        Assert.That(rgba.RgbaPixels[3], Is.EqualTo(0));
        Assert.That(rgba.RgbaPixels[7], Is.EqualTo(255));
    }

    [Test]
    public void EncodeRgbaStill_WritesAlphaTransparency()
    {
        byte[] pixels = new byte[]
        {
            255, 0, 0, 0,
            0, 0, 255, 255
        };
        GifBitmap bitmap = new GifBitmap(2, 1, pixels);

        GifEncodeResult encoded = Gif.EncodeRgbaStill(bitmap);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);
        GifRawFrameDecodeResult decoded = new GifRawFrameDecoder().DecodeFrame(parsed.Document.Frames[0]);
        GifRgbaConversionResult rgba = ConvertParsedFrame(parsed.Document, decoded.IndexedPixels, 0);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(decoded.Success, Is.True);
        Assert.That(rgba.Success, Is.True);
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(parsed.Document.Frames[0].HasTransparency, Is.True);
        Assert.That(parsed.Document.Frames[0].TransparentColorIndex, Is.EqualTo(0));
        Assert.That(rgba.RgbaPixels[3], Is.EqualTo(0));
        Assert.That(rgba.RgbaPixels[7], Is.EqualTo(255));
    }

    [Test]
    public void EncodeRgbaAnimation_PreservesTransparencyThroughQuantization()
    {
        GifAnimation animation = new GifAnimation();
        animation.Width = 2;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateRgbaFrame(new GifBitmap(2, 1, new byte[]
            {
                1, 2, 3, 0,
                20, 30, 40, 255
            })),
            CreateRgbaFrame(new GifBitmap(2, 1, new byte[]
            {
                50, 60, 70, 255,
                9, 8, 7, 0
            }))
        };

        GifEncodeResult encoded = Gif.EncodeRgbaAnimation(animation);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);
        GifRawFrameDecodeResult firstDecoded = new GifRawFrameDecoder().DecodeFrame(parsed.Document.Frames[0]);
        GifRawFrameDecodeResult secondDecoded = new GifRawFrameDecoder().DecodeFrame(parsed.Document.Frames[1]);
        GifRgbaConversionResult firstRgba = ConvertParsedFrame(parsed.Document, firstDecoded.IndexedPixels, 0);
        GifRgbaConversionResult secondRgba = ConvertParsedFrame(parsed.Document, secondDecoded.IndexedPixels, 1);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(firstDecoded.Success, Is.True);
        Assert.That(secondDecoded.Success, Is.True);
        Assert.That(firstRgba.Success, Is.True);
        Assert.That(secondRgba.Success, Is.True);
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(parsed.Document.Frames[0].HasTransparency, Is.True);
        Assert.That(parsed.Document.Frames[1].HasTransparency, Is.True);
        Assert.That(parsed.Document.Frames[0].TransparentColorIndex, Is.EqualTo(0));
        Assert.That(parsed.Document.Frames[1].TransparentColorIndex, Is.EqualTo(0));
        Assert.That(firstRgba.RgbaPixels[3], Is.EqualTo(0));
        Assert.That(firstRgba.RgbaPixels[7], Is.EqualTo(255));
        Assert.That(secondRgba.RgbaPixels[3], Is.EqualTo(255));
        Assert.That(secondRgba.RgbaPixels[7], Is.EqualTo(0));
    }

    [Test]
    public void EncodeIndexedAnimation_RejectsInvalidTransparentIndex()
    {
        GifPalette palette = CreatePalette();
        GifAnimationFrame frame = CreateIndexedFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, palette));
        frame.HasTransparency = true;
        frame.TransparentColorIndex = 2;
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[] { frame };

        GifEncodeResult encoded = Gif.EncodeIndexedAnimation(animation);

        Assert.That(encoded.Success, Is.False);
        Assert.That(encoded.Error.Code, Is.EqualTo(GifErrorCode.InvalidPaletteIndex));
        Assert.That(encoded.Error.Position.FrameIndex, Is.EqualTo(0));
        Assert.That(encoded.Data, Is.Empty);
    }

    static GifRgbaConversionResult ConvertParsedFrame(GifDocument document, byte[] indexedPixels, int frameIndex)
    {
        GifFrame frame = document.Frames[frameIndex];
        GifPalette palette = ResolvePalette(document, frameIndex);
        GifIndexedToRgbaConverter converter = new GifIndexedToRgbaConverter();
        return converter.ConvertIndexedPixelsWithTransparency(indexedPixels, frame.Width, frame.Height, palette, frame.HasTransparency, frame.TransparentColorIndex);
    }

    static GifPalette ResolvePalette(GifDocument document, int frameIndex)
    {
        GifFrame frame = document.Frames[frameIndex];
        if (frame.HasLocalColorTable)
        {
            return frame.LocalColorTable;
        }

        return document.GlobalColorTable;
    }

    static GifPalette CreatePalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
    }

    static GifAnimationFrame CreateIndexedFrame(GifBitmap bitmap)
    {
        return CreateFrame(bitmap);
    }

    static GifAnimationFrame CreateRgbaFrame(GifBitmap bitmap)
    {
        return CreateFrame(bitmap);
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
