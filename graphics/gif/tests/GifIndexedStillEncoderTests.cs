using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifIndexedStillEncoderTests
{
    [Test]
    public void EncodeIndexedStill_WritesCompleteGifAndRoundTripsPixels()
    {
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
        byte[] pixels = new byte[] { 0, 1, 1, 0 };
        GifBitmap bitmap = GifBitmap.CreateIndexed(2, 2, pixels, palette);

        GifEncodeResult encoded = Gif.EncodeIndexedStill(bitmap);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);
        GifFrame frame = parsed.Document.Frames[0];
        GifRawFrameDecodeResult decoded = new GifRawFrameDecoder().DecodeFrame(frame);

        Assert.That(encoded.Success, Is.True);
        Assert.That(encoded.Data[0], Is.EqualTo((byte)'G'));
        Assert.That(encoded.Data[1], Is.EqualTo((byte)'I'));
        Assert.That(encoded.Data[2], Is.EqualTo((byte)'F'));
        Assert.That(encoded.Data[3], Is.EqualTo((byte)'8'));
        Assert.That(encoded.Data[4], Is.EqualTo((byte)'7'));
        Assert.That(encoded.Data[5], Is.EqualTo((byte)'a'));
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.HasTrailer, Is.True);
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif87a));
        Assert.That(parsed.Document.LogicalScreen.Width, Is.EqualTo(2));
        Assert.That(parsed.Document.LogicalScreen.Height, Is.EqualTo(2));
        Assert.That(parsed.Document.GlobalColorTable.Count, Is.EqualTo(2));
        Assert.That(parsed.Document.Frames.Length, Is.EqualTo(1));
        Assert.That(frame.Left, Is.EqualTo(0));
        Assert.That(frame.Top, Is.EqualTo(0));
        Assert.That(frame.Width, Is.EqualTo(2));
        Assert.That(frame.Height, Is.EqualTo(2));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.IndexedPixels, Is.EqualTo(pixels));
        Assert.That(encoded.Document.EncodeStatistics.FramesWritten, Is.EqualTo(1));
        Assert.That(encoded.Document.EncodeStatistics.IndexedPixelCount, Is.EqualTo(4));
        Assert.That(encoded.Document.EncodeStatistics.BytesWritten, Is.EqualTo(encoded.Data.Length));
    }

    [Test]
    public void EncodeIndexedStill_PadsPaletteToLegalGifColorTableSize()
    {
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(10, 20, 30),
            new GifColor(40, 50, 60),
            new GifColor(70, 80, 90)
        });
        byte[] pixels = new byte[] { 0, 2 };

        GifEncodeResult encoded = Gif.EncodeIndexedStill(2, 1, pixels, palette);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);
        GifFrame frame = parsed.Document.Frames[0];
        GifRawFrameDecodeResult decoded = new GifRawFrameDecoder().DecodeFrame(frame);

        Assert.That(encoded.Success, Is.True);
        Assert.That(encoded.Document.GlobalColorTable.Count, Is.EqualTo(4));
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.GlobalColorTable.Count, Is.EqualTo(4));
        Assert.That(parsed.Document.GlobalColorTable.Colors[2].Red, Is.EqualTo(70));
        Assert.That(parsed.Document.GlobalColorTable.Colors[3].Red, Is.EqualTo(70));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.IndexedPixels, Is.EqualTo(pixels));
    }

    [Test]
    public void EncodeIndexedStill_RejectsPaletteIndexOutsideSourcePalette()
    {
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
        GifBitmap bitmap = GifBitmap.CreateIndexed(2, 1, new byte[] { 0, 2 }, palette);

        GifEncodeResult encoded = Gif.EncodeIndexedStill(bitmap);

        Assert.That(encoded.Success, Is.False);
        Assert.That(encoded.Error.Code, Is.EqualTo(GifErrorCode.InvalidPaletteIndex));
        Assert.That(encoded.Data, Is.Empty);
    }

    [Test]
    public void EncodeIndexedStill_RejectsInvalidPixelCount()
    {
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
        GifBitmap bitmap = GifBitmap.CreateIndexed(2, 2, new byte[] { 0, 1 }, palette);

        GifEncodeResult encoded = Gif.EncodeIndexedStill(bitmap);

        Assert.That(encoded.Success, Is.False);
        Assert.That(encoded.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
        Assert.That(encoded.Data, Is.Empty);
    }
}
