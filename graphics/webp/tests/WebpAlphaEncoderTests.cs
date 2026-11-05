using webp;

namespace webp.Tests;

[TestFixture]
public class WebpAlphaEncoderTests
{
    static WebpPixelBuffer ObservedLosslessBuffer = new WebpPixelBuffer();
    static byte[] LosslessStream = Array.Empty<byte>();

    [Test]
    public void EncodeUncompressed_WritesNoneFilterHeaderAndRawAlphaBytes()
    {
        byte[] alpha = new byte[] { 7, 8, 9, 10 };

        byte[] payload = WebpAlphaEncoder.EncodeUncompressed(alpha, 2, 2);
        alpha[0] = 99;
        WebpAlphaDecodeResult decoded = WebpAlphaDecoder.Decode(payload, 2, 2, WebpStrictness.Strict);

        Assert.That(payload, Is.EqualTo(new byte[] { 0, 7, 8, 9, 10 }));
        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Alpha, Is.EqualTo(new byte[] { 7, 8, 9, 10 }));
    }

    [Test]
    public void EncodeUncompressed_AppliesHorizontalPredictorFilter()
    {
        byte[] alpha = new byte[] { 10, 15, 20, 30, 40, 55 };

        byte[] payload = WebpAlphaEncoder.EncodeUncompressed(alpha, 3, 2, WebpConstants.AlphaFilterHorizontal);

        Assert.That(payload[0], Is.EqualTo(0x04));
        Assert.That(payload, Is.EqualTo(new byte[] { 0x04, 10, 5, 5, 20, 10, 15 }));
    }

    [Test]
    public void EncodeUncompressed_RoundTripsAllAlphaFilters()
    {
        byte[] alpha = new byte[] { 10, 20, 35, 30, 50, 90, 80, 110, 160 };
        byte[] filters = new byte[]
        {
            WebpConstants.AlphaFilterNone,
            WebpConstants.AlphaFilterHorizontal,
            WebpConstants.AlphaFilterVertical,
            WebpConstants.AlphaFilterGradient
        };

        int index = 0;
        while (index < filters.Length)
        {
            byte[] payload = WebpAlphaEncoder.EncodeUncompressed(alpha, 3, 3, filters[index]);
            WebpAlphaDecodeResult decoded = WebpAlphaDecoder.Decode(payload, 3, 3, WebpStrictness.Strict);

            Assert.That(decoded.IsValid, Is.True);
            Assert.That(decoded.Header.CompressionMethod, Is.EqualTo(WebpConstants.AlphaCompressionNone));
            Assert.That(decoded.Header.FilterMethod, Is.EqualTo(filters[index]));
            Assert.That(decoded.Alpha, Is.EqualTo(alpha));
            index = index + 1;
        }
    }

    [Test]
    public void EncodeCompressed_PacksFilteredAlphaIntoGreenChannelAndWritesLosslessHeader()
    {
        ObservedLosslessBuffer = new WebpPixelBuffer();
        LosslessStream = new byte[] { 0xAA, 0xBB, 0xCC };
        byte[] alpha = new byte[] { 10, 15, 20, 40 };

        byte[] payload = WebpAlphaEncoder.EncodeCompressed(alpha, 2, 2, WebpConstants.AlphaFilterHorizontal, EncodeLosslessForTest);
        alpha[0] = 99;

        Assert.That(payload, Is.EqualTo(new byte[] { 0x05, 0xAA, 0xBB, 0xCC }));
        Assert.That(ObservedLosslessBuffer.Width, Is.EqualTo(2));
        Assert.That(ObservedLosslessBuffer.Height, Is.EqualTo(2));
        Assert.That(ObservedLosslessBuffer.Rgba[0], Is.EqualTo(0));
        Assert.That(ObservedLosslessBuffer.Rgba[1], Is.EqualTo(10));
        Assert.That(ObservedLosslessBuffer.Rgba[5], Is.EqualTo(5));
        Assert.That(ObservedLosslessBuffer.Rgba[9], Is.EqualTo(10));
        Assert.That(ObservedLosslessBuffer.Rgba[13], Is.EqualTo(20));
        Assert.That(ObservedLosslessBuffer.Rgba[15], Is.EqualTo(255));
    }

    [Test]
    public void EncodeCompressed_OutputRoundTripsThroughCompressedDecoderBridge()
    {
        ObservedLosslessBuffer = new WebpPixelBuffer();
        LosslessStream = new byte[] { 1, 2, 3 };
        byte[] alpha = new byte[] { 10, 20, 35, 30, 50, 90, 80, 110, 160 };

        byte[] payload = WebpAlphaEncoder.EncodeCompressed(alpha, 3, 3, WebpConstants.AlphaFilterGradient, EncodeLosslessForTest);
        WebpAlphaDecodeResult decoded = WebpAlphaDecoder.Decode(payload, 3, 3, WebpStrictness.Strict, DecodeLosslessForTest);

        Assert.That(payload[0], Is.EqualTo(0x0D));
        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Alpha, Is.EqualTo(alpha));
    }

    [Test]
    public void EncodeCompressed_OutputRoundTripsThroughRealLosslessAlphaStream()
    {
        byte[] alpha = new byte[] { 0, 17, 34, 65, 98, 131, 200, 230, 255 };

        byte[] payload = WebpAlphaEncoder.EncodeCompressed(alpha, 3, 3, WebpConstants.AlphaFilterVertical, WebpVp8LEncoder.Encode);
        WebpAlphaDecodeResult decoded = WebpAlphaDecoder.Decode(payload, 3, 3, WebpStrictness.Strict, DecodeLosslessPayload);

        Assert.That(payload[0], Is.EqualTo(0x09));
        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Alpha, Is.EqualTo(alpha));
    }

    [Test]
    public void EncodeCompressed_RejectsEmptyLosslessStream()
    {
        LosslessStream = Array.Empty<byte>();

        Assert.Throws<ArgumentException>(() => WebpAlphaEncoder.EncodeCompressed(new byte[] { 1 }, 1, 1, EncodeLosslessForTest));
    }

    [Test]
    public void CreateHeader_WritesCompressionAndFilterBitsOnly()
    {
        byte header = WebpAlphaEncoder.CreateHeader(WebpConstants.AlphaCompressionLossless, WebpConstants.AlphaFilterGradient);

        Assert.That(header, Is.EqualTo(0x0D));
        Assert.That(WebpAlphaDecoder.ParseHeader(header).CompressionMethod, Is.EqualTo(WebpConstants.AlphaCompressionLossless));
        Assert.That(WebpAlphaDecoder.ParseHeader(header).FilterMethod, Is.EqualTo(WebpConstants.AlphaFilterGradient));
        Assert.That(WebpAlphaDecoder.ParseHeader(header).ReservedBits, Is.EqualTo(0));
    }

    [Test]
    public void EncodeUncompressed_RejectsInvalidAlphaPlaneAndFilter()
    {
        Assert.Throws<ArgumentException>(() => WebpAlphaEncoder.EncodeUncompressed(new byte[] { 1, 2, 3 }, 2, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpAlphaEncoder.EncodeUncompressed(new byte[] { 1 }, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpAlphaEncoder.EncodeUncompressed(new byte[] { 1 }, 1, 1, 4));
        Assert.Throws<ArgumentException>(() => WebpAlphaEncoder.EncodeCompressed(new byte[] { 1, 2, 3 }, 2, 2, EncodeLosslessForTest));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpAlphaEncoder.EncodeCompressed(new byte[] { 1 }, 1, 1, 4, EncodeLosslessForTest));
    }

    [Test]
    public void CreateHeader_RejectsInvalidCompressionAndFilter()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpAlphaEncoder.CreateHeader(2, WebpConstants.AlphaFilterNone));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpAlphaEncoder.CreateHeader(WebpConstants.AlphaCompressionNone, 4));
    }

    static byte[] EncodeLosslessForTest(WebpPixelBuffer buffer)
    {
        ObservedLosslessBuffer = WebpPixelBufferHelper.Copy(buffer);
        return WebpUnknownChunk.CopyBytes(LosslessStream);
    }

    static WebpPixelBuffer DecodeLosslessForTest(byte[] imageStream, int width, int height)
    {
        Assert.That(imageStream, Is.EqualTo(LosslessStream));
        Assert.That(width, Is.EqualTo(ObservedLosslessBuffer.Width));
        Assert.That(height, Is.EqualTo(ObservedLosslessBuffer.Height));
        return WebpPixelBufferHelper.Copy(ObservedLosslessBuffer);
    }

    static WebpPixelBuffer DecodeLosslessPayload(byte[] imageStream, int width, int height)
    {
        WebpVp8LDecodeResult decoded = WebpVp8LDecoder.Decode(imageStream, WebpStrictness.Strict);

        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.PixelBuffer, Is.Not.Null);
        Assert.That(decoded.PixelBuffer!.Width, Is.EqualTo(width));
        Assert.That(decoded.PixelBuffer.Height, Is.EqualTo(height));
        return decoded.PixelBuffer;
    }
}
