using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifRawFrameEncoderTests
{
    [Test]
    public void EncodeIndexedPixels_CompressesAndRoundTripsThroughRawDecoder()
    {
        GifRawFrameEncoder encoder = new GifRawFrameEncoder();
        byte[] pixels = new byte[] { 0, 1, 0, 1, 0, 1, 2, 2, 2, 2, 3, 3 };

        GifRawFrameEncodeResult encoded = encoder.EncodeIndexedPixels(pixels, 2, 4, 3, false);
        GifRawFrameDecodeResult decoded = new GifRawFrameDecoder().DecodeIndexedPixels(encoded.CompressedData, encoded.LzwMinimumCodeSize, 4, 3, false);

        Assert.That(encoded.Success, Is.True);
        Assert.That(encoded.CompressedData.Length, Is.LessThan(pixels.Length));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.IndexedPixels, Is.EqualTo(pixels));
    }

    [Test]
    public void EncodeFrame_PopulatesFrameCompressedDataAndMinimumCodeSize()
    {
        GifFrame frame = new GifFrame(0, 0, 4, 1);
        frame.IndexedPixels = new byte[] { 0, 1, 2, 3 };
        GifRawFrameEncoder encoder = new GifRawFrameEncoder();

        GifRawFrameEncodeResult encoded = encoder.EncodeFrame(frame);
        GifRawFrameDecodeResult decoded = new GifRawFrameDecoder().DecodeFrame(frame);

        Assert.That(encoded.Success, Is.True);
        Assert.That(frame.LzwMinimumCodeSize, Is.EqualTo(2));
        Assert.That(frame.CompressedImageData, Is.EqualTo(encoded.CompressedData));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.IndexedPixels, Is.EqualTo(new byte[] { 0, 1, 2, 3 }));
    }

    [Test]
    public void EncodeIndexedPixels_InterlacesBeforeCompression()
    {
        GifRawFrameEncoder encoder = new GifRawFrameEncoder();
        byte[] rowMajorPixels = new byte[]
        {
            0, 0,
            1, 1,
            2, 2,
            3, 3,
            4, 4
        };

        GifRawFrameEncodeResult encoded = encoder.EncodeIndexedPixels(rowMajorPixels, 3, 2, 5, true);
        GifRawFrameDecodeResult decodedAsInterlaced = new GifRawFrameDecoder().DecodeIndexedPixels(encoded.CompressedData, 3, 2, 5, true);
        GifRawFrameDecodeResult decodedAsStored = new GifRawFrameDecoder().DecodeIndexedPixels(encoded.CompressedData, 3, 2, 5, false);

        Assert.That(encoded.Success, Is.True);
        Assert.That(decodedAsInterlaced.Success, Is.True);
        Assert.That(decodedAsInterlaced.IndexedPixels, Is.EqualTo(rowMajorPixels));
        Assert.That(decodedAsStored.Success, Is.True);
        Assert.That(decodedAsStored.IndexedPixels, Is.EqualTo(new byte[]
        {
            0, 0,
            4, 4,
            2, 2,
            1, 1,
            3, 3
        }));
    }

    [Test]
    public void EncodeIndexedPixels_RejectsPaletteIndexOutsideMinimumCodeSizeRange()
    {
        GifRawFrameEncoder encoder = new GifRawFrameEncoder();

        GifRawFrameEncodeResult result = encoder.EncodeIndexedPixels(new byte[] { 0, 4 }, 2, 2, 1, false);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidPaletteIndex));
        Assert.That(result.CompressedData, Is.Empty);
    }

    [Test]
    public void EncodeIndexedPixels_RejectsInvalidDimensionsCountAndMinimumCodeSize()
    {
        GifRawFrameEncoder encoder = new GifRawFrameEncoder();

        GifRawFrameEncodeResult invalidDimensions = encoder.EncodeIndexedPixels(new byte[] { 0 }, 2, 0, 1, false);
        GifRawFrameEncodeResult countMismatch = encoder.EncodeIndexedPixels(new byte[] { 0 }, 2, 2, 1, false);
        GifRawFrameEncodeResult invalidMinimumCodeSize = encoder.EncodeIndexedPixels(new byte[] { 0 }, 1, 1, 1, false);

        Assert.That(invalidDimensions.Success, Is.False);
        Assert.That(invalidDimensions.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(countMismatch.Success, Is.False);
        Assert.That(countMismatch.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
        Assert.That(invalidMinimumCodeSize.Success, Is.False);
        Assert.That(invalidMinimumCodeSize.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwMinimumCodeSize));
    }

    [Test]
    public void EncodeIndexedPixels_EnforcesCompressedByteLimit()
    {
        GifRawFrameEncoder encoder = new GifRawFrameEncoder();
        GifEncodeOptions options = new GifEncodeOptions();
        options.MaxBytes = 0;

        GifRawFrameEncodeResult result = encoder.EncodeIndexedPixelsWithOptions(new byte[] { 0, 1, 2, 3 }, 2, 4, 1, false, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.ByteLimitExceeded));
        Assert.That(result.CompressedData, Is.Empty);
    }

    [Test]
    public void CalculateMinimumCodeSize_UsesSmallestLegalGifLzwRootSize()
    {
        GifRawFrameEncoder encoder = new GifRawFrameEncoder();

        Assert.That(encoder.CalculateMinimumCodeSize(new byte[] { 0, 1, 3 }), Is.EqualTo(2));
        Assert.That(encoder.CalculateMinimumCodeSize(new byte[] { 4 }), Is.EqualTo(3));
        Assert.That(encoder.CalculateMinimumCodeSize(new byte[] { 255 }), Is.EqualTo(8));
    }
}
