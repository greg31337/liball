using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8KeyFrameEncoderTests
{
    [Test]
    public void Encode_WritesKeyFrameHeaderAndFixedQuantizer()
    {
        WebpPixelBuffer buffer = NewBuffer(4, 3);

        byte[] payload = WebpVp8KeyFrameEncoder.Encode(buffer, 27);
        WebpVp8FrameHeaderDataParseResult header = WebpVp8FrameHeaderDataParser.Parse(payload, WebpStrictness.Strict);

        Assert.That(header.IsValid, Is.True);
        Assert.That(header.FrameHeader.IsKeyFrame, Is.True);
        Assert.That(header.FrameHeader.Width, Is.EqualTo(4));
        Assert.That(header.FrameHeader.Height, Is.EqualTo(3));
        Assert.That(header.HeaderData.Quantizer.BaseQIndex, Is.EqualTo(27));
        Assert.That(header.HeaderData.TokenPartitions.PartitionCount, Is.EqualTo(1));
    }

    [Test]
    public void Encode_OutputDecodesThroughVp8Decoder()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);

        byte[] payload = WebpVp8KeyFrameEncoder.Encode(buffer, 10);
        WebpVp8DecodeResult decoded = WebpVp8Decoder.Decode(payload, WebpStrictness.Strict);

        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.PixelBuffer.Width, Is.EqualTo(1));
        Assert.That(decoded.PixelBuffer.Height, Is.EqualTo(1));
        Assert.That(decoded.PixelBuffer.Rgba[3], Is.EqualTo(255));
    }

    [Test]
    public void Encode_SmokeDecodesSupportedSizesAndQuantizers()
    {
        AssertVp8EncodeDecode(NewBuffer(1, 1), 0);
        AssertVp8EncodeDecode(NewBuffer(8, 7), 32);
        AssertVp8EncodeDecode(NewBuffer(16, 16), 127);
    }

    [Test]
    public void EncodeDecode_HighLevelLossyWebpRoundTripsThroughDecoder()
    {
        WebpPixelBuffer buffer = NewBuffer(4, 3);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossy;
        options.Quality = 55;

        byte[] data = WebpEncoder.Encode(buffer, options);
        WebpBasicContainerParseResult parsed = WebpBasicContainerParser.Parse(data, WebpStrictness.Strict);
        WebpDecodeResult decoded = WebpDecoder.Decode(data);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.ContainerKind, Is.EqualTo(WebpContainerKind.SimpleLossy));
        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Image.CanvasWidth, Is.EqualTo(4));
        Assert.That(decoded.Image.CanvasHeight, Is.EqualTo(3));
        Assert.That(decoded.Image.Frames, Has.Count.EqualTo(1));
        Assert.That(decoded.Image.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(decoded.Image.Frames[0].Pixels, Is.Not.Null);
        AssertNeutralDecodedFrame(decoded.Image.Frames[0].Pixels!, 4, 3);
    }

    [Test]
    public void Encode_IsDeterministicForSameInput()
    {
        WebpPixelBuffer buffer = NewBuffer(2, 2);

        byte[] first = WebpVp8KeyFrameEncoder.Encode(buffer, 20);
        byte[] second = WebpVp8KeyFrameEncoder.Encode(buffer, 20);

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void Encode_RejectsInvalidInputs()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        WebpPixelBuffer large = NewBuffer(17, 1);

        Assert.Throws<ArgumentNullException>(() => WebpVp8KeyFrameEncoder.Encode(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8KeyFrameEncoder.Encode(buffer, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8KeyFrameEncoder.Encode(buffer, 128));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8KeyFrameEncoder.Encode(large, 10));
    }

    [Test]
    public void RawBoolWriter_WritesBitsMostSignificantFirstWithPadding()
    {
        WebpVp8RawBoolWriter writer = new WebpVp8RawBoolWriter();
        writer.WriteLiteral(0b10110001, 8);

        byte[] data = writer.ToBytes();

        Assert.That(data[0], Is.EqualTo(0xB1));
        Assert.That(data.Length, Is.GreaterThan(2));
    }

    static void AssertVp8EncodeDecode(WebpPixelBuffer buffer, int quantizer)
    {
        byte[] payload = WebpVp8KeyFrameEncoder.Encode(buffer, quantizer);
        WebpVp8FrameHeaderDataParseResult header = WebpVp8FrameHeaderDataParser.Parse(payload, WebpStrictness.Strict);
        WebpVp8DecodeResult decoded = WebpVp8Decoder.Decode(payload, WebpStrictness.Strict);

        Assert.That(header.IsValid, Is.True);
        Assert.That(header.FrameHeader.Width, Is.EqualTo(buffer.Width));
        Assert.That(header.FrameHeader.Height, Is.EqualTo(buffer.Height));
        Assert.That(header.HeaderData.Quantizer.BaseQIndex, Is.EqualTo(quantizer));
        Assert.That(decoded.IsValid, Is.True);
        AssertNeutralDecodedFrame(decoded.PixelBuffer, buffer.Width, buffer.Height);
    }

    static void AssertNeutralDecodedFrame(WebpPixelBuffer pixels, int width, int height)
    {
        Assert.That(pixels.Width, Is.EqualTo(width));
        Assert.That(pixels.Height, Is.EqualTo(height));
        Assert.That(pixels.Rgba[0], Is.EqualTo(WebpVp8YuvConverter.YuvToR(127, 128)));
        Assert.That(pixels.Rgba[1], Is.EqualTo(WebpVp8YuvConverter.YuvToG(127, 128, 128)));
        Assert.That(pixels.Rgba[2], Is.EqualTo(WebpVp8YuvConverter.YuvToB(127, 128)));
        Assert.That(pixels.Rgba[3], Is.EqualTo(255));
        Assert.That(pixels.Rgba[pixels.Rgba.Length - 1], Is.EqualTo(255));
    }

    static WebpPixelBuffer NewBuffer(int width, int height)
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(width, height);
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                int offset = (y * buffer.Stride) + (x * 4);
                buffer.Rgba[offset] = (byte)(10 + x);
                buffer.Rgba[offset + 1] = (byte)(20 + y);
                buffer.Rgba[offset + 2] = 30;
                buffer.Rgba[offset + 3] = 255;
                x = x + 1;
            }

            y = y + 1;
        }

        return buffer;
    }
}
