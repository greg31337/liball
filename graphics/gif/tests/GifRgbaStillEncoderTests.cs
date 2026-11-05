using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifRgbaStillEncoderTests
{
    [Test]
    public void EncodeRgbaStill_QuantizesAndRoundTripsOpaquePixels()
    {
        byte[] rgbaPixels = new byte[]
        {
            255, 0, 0, 255,
            0, 0, 255, 255
        };
        GifBitmap bitmap = new GifBitmap(2, 1, rgbaPixels);

        GifEncodeResult encoded = Gif.EncodeRgbaStill(bitmap);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);
        GifFrame frame = parsed.Document.Frames[0];
        GifRawFrameDecodeResult decoded = new GifRawFrameDecoder().DecodeFrame(frame);
        GifRgbaConversionResult converted = new GifIndexedToRgbaConverter().ConvertIndexedPixels(decoded.IndexedPixels, frame.Width, frame.Height, parsed.Document.GlobalColorTable);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif87a));
        Assert.That(parsed.Document.LogicalScreen.Width, Is.EqualTo(2));
        Assert.That(parsed.Document.LogicalScreen.Height, Is.EqualTo(1));
        Assert.That(parsed.Document.GlobalColorTable.Count, Is.EqualTo(2));
        Assert.That(decoded.Success, Is.True);
        Assert.That(converted.Success, Is.True);
        Assert.That(converted.RgbaPixels, Is.EqualTo(rgbaPixels));
        Assert.That(encoded.Document.EncodeStatistics.FramesWritten, Is.EqualTo(1));
        Assert.That(encoded.Document.EncodeStatistics.IndexedPixelCount, Is.EqualTo(2));
    }

    [Test]
    public void EncodeRgbaStill_UsesQuantizerAndDitherOptions()
    {
        GifBitmap bitmap = new GifBitmap(3, 1, new byte[]
        {
            120, 120, 120, 255,
            120, 120, 120, 255,
            120, 120, 120, 255
        });
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 2;
        options.QuantizerMode = GifQuantizerMode.Grayscale;
        options.DitherMode = GifDitherMode.FloydSteinberg;

        GifEncodeResult encoded = Gif.EncodeRgbaStill(bitmap, options);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);
        GifRawFrameDecodeResult decoded = new GifRawFrameDecoder().DecodeFrame(parsed.Document.Frames[0]);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.GlobalColorTable.Count, Is.EqualTo(2));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.IndexedPixels, Is.EqualTo(new byte[] { 0, 1, 0 }));
    }

    [Test]
    public void EncodeRgbaStill_DerivesQuantizeSettingsFromEncodeOptions()
    {
        GifBitmap bitmap = new GifBitmap(2, 1, new byte[]
        {
            128, 128, 128, 255,
            128, 128, 128, 255
        });
        GifEncodeOptions options = new GifEncodeOptions();
        options.QuantizerMode = GifQuantizerMode.Grayscale;
        options.DitherMode = GifDitherMode.OrderedBayer;

        GifEncodeResult encoded = Gif.EncodeRgbaStill(bitmap, options);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);
        GifRawFrameDecodeResult decoded = new GifRawFrameDecoder().DecodeFrame(parsed.Document.Frames[0]);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.GlobalColorTable.Count, Is.EqualTo(256));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.IndexedPixels, Is.EqualTo(new byte[] { 0, 136 }));
    }

    [Test]
    public void EncodeRgbaStill_RejectsInvalidRgbaByteCount()
    {
        GifBitmap bitmap = new GifBitmap(2, 1, new byte[] { 255, 0, 0, 255 });

        GifEncodeResult encoded = Gif.EncodeRgbaStill(bitmap);

        Assert.That(encoded.Success, Is.False);
        Assert.That(encoded.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
        Assert.That(encoded.Data, Is.Empty);
    }
}
