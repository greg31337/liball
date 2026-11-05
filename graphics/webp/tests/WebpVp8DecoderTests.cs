using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8DecoderTests
{
    [Test]
    public void Decode_ParsesSkippedResidualKeyFrameAndConvertsNeutralYuvToRgba()
    {
        byte[] payload = NewSkippedResidualVp8Payload(16, 16, 1, true, true);

        WebpVp8DecodeResult result = WebpVp8Decoder.Decode(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.FrameHeader.Width, Is.EqualTo(16));
        Assert.That(result.FrameHeader.Height, Is.EqualTo(16));
        Assert.That(result.ModeResult.Macroblocks.Count, Is.EqualTo(1));
        Assert.That(result.ModeResult.Macroblocks[0].SkipCoefficients, Is.True);
        Assert.That(result.PixelBuffer.Width, Is.EqualTo(16));
        Assert.That(result.PixelBuffer.Height, Is.EqualTo(16));
        Assert.That(result.PixelBuffer.Rgba[0], Is.EqualTo(WebpVp8YuvConverter.YuvToR(127, 128)));
        Assert.That(result.PixelBuffer.Rgba[1], Is.EqualTo(WebpVp8YuvConverter.YuvToG(127, 128, 128)));
        Assert.That(result.PixelBuffer.Rgba[2], Is.EqualTo(WebpVp8YuvConverter.YuvToB(127, 128)));
        Assert.That(result.PixelBuffer.Rgba[3], Is.EqualTo(255));
    }

    [Test]
    public void Decode_RoundsOddDimensionsToMacroblockGridForModeParsing()
    {
        byte[] payload = NewSkippedResidualVp8Payload(9, 9, 1, true, true);

        WebpVp8DecodeResult result = WebpVp8Decoder.Decode(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.FrameHeader.Width, Is.EqualTo(9));
        Assert.That(result.FrameHeader.Height, Is.EqualTo(9));
        Assert.That(result.ModeResult.Macroblocks.Count, Is.EqualTo(1));
        Assert.That(result.PixelBuffer.Width, Is.EqualTo(9));
        Assert.That(result.PixelBuffer.Height, Is.EqualTo(9));
    }

    [Test]
    public void Decode_DecodesReferenceFixturePayloads()
    {
        AssertReferenceFixtureDecode("skipped-residual-16x16.vp8.hex", 16, 16, 1);
        AssertReferenceFixtureDecode("skipped-residual-odd-9x9.vp8.hex", 9, 9, 1);
    }

    [Test]
    public void Decode_RejectsFramesWithoutCoefficientSkipSupport()
    {
        byte[] payload = NewSkippedResidualVp8Payload(16, 16, 1, false, false);

        WebpVp8DecodeResult result = WebpVp8Decoder.Decode(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("coefficient-skip"));
    }

    [Test]
    public void Decode_RejectsMacroblocksWithResidualTokens()
    {
        byte[] payload = NewSkippedResidualVp8Payload(16, 16, 1, true, false);

        WebpVp8DecodeResult result = WebpVp8Decoder.Decode(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("skipped-residual"));
    }

    [Test]
    public void Decode_ReturnsFrameHeaderDiagnostics()
    {
        byte[] payload = NewSkippedResidualVp8Payload(16, 16, 1, true, true);
        payload[3] = 0;

        WebpVp8DecodeResult result = WebpVp8Decoder.Decode(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("signature"));
    }

    static void AssertReferenceFixtureDecode(string fileName, int width, int height, int macroblockCount)
    {
        byte[] payload = ReadFixtureHex(fileName);

        WebpVp8DecodeResult result = WebpVp8Decoder.Decode(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.FrameHeader.Width, Is.EqualTo(width));
        Assert.That(result.FrameHeader.Height, Is.EqualTo(height));
        Assert.That(result.ModeResult.Macroblocks, Has.Count.EqualTo(macroblockCount));
        Assert.That(result.ModeResult.Macroblocks[0].SkipCoefficients, Is.True);
        Assert.That(result.PixelBuffer.Width, Is.EqualTo(width));
        Assert.That(result.PixelBuffer.Height, Is.EqualTo(height));
        Assert.That(result.PixelBuffer.Rgba[0], Is.EqualTo(WebpVp8YuvConverter.YuvToR(127, 128)));
        Assert.That(result.PixelBuffer.Rgba[1], Is.EqualTo(WebpVp8YuvConverter.YuvToG(127, 128, 128)));
        Assert.That(result.PixelBuffer.Rgba[2], Is.EqualTo(WebpVp8YuvConverter.YuvToB(127, 128)));
        Assert.That(result.PixelBuffer.Rgba[3], Is.EqualTo(255));
        Assert.That(result.PixelBuffer.Rgba[result.PixelBuffer.Rgba.Length - 1], Is.EqualTo(255));
    }

    static byte[] ReadFixtureHex(string fileName)
    {
        string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Vp8", fileName);
        string hex = File.ReadAllText(path).Trim();
        byte[] payload = new byte[hex.Length / 2];
        int index = 0;
        while (index < payload.Length)
        {
            payload[index] = Convert.ToByte(hex.Substring(index * 2, 2), 16);
            index = index + 1;
        }

        return payload;
    }

    static byte[] NewSkippedResidualVp8Payload(int width, int height, int macroblockCount, bool coefficientSkipEnabled, bool skipMacroblocks)
    {
        WebpVp8BoolWriter writer = new WebpVp8BoolWriter();
        WriteFrameHeaderData(writer);
        writer.WriteBit(0);
        int index = 0;
        while (index < WebpVp8ProbabilityTables.CoefficientUpdate.Length)
        {
            writer.WriteBool(WebpVp8ProbabilityTables.CoefficientUpdate[index], 0);
            index = index + 1;
        }

        writer.WriteBit(coefficientSkipEnabled ? 1 : 0);
        if (coefficientSkipEnabled)
        {
            writer.WriteLiteral(128, 8);
        }

        index = 0;
        while (index < macroblockCount)
        {
            if (coefficientSkipEnabled)
            {
                writer.WriteBit(skipMacroblocks ? 1 : 0);
            }

            writer.WriteBool(145, 1);
            writer.WriteBool(156, 0);
            writer.WriteBool(163, 0);
            writer.WriteBool(142, 0);
            index = index + 1;
        }

        return NewVp8Payload(width, height, writer.ToBytes());
    }

    static void WriteFrameHeaderData(WebpVp8BoolWriter writer)
    {
        writer.WriteLiteral(0, 2);
        writer.WriteBit(0);
        writer.WriteBit(1);
        writer.WriteLiteral(0, 6);
        writer.WriteLiteral(0, 3);
        writer.WriteBit(0);
        writer.WriteLiteral(0, 2);
        writer.WriteLiteral(0, 7);
        writer.WriteBit(0);
        writer.WriteBit(0);
        writer.WriteBit(0);
        writer.WriteBit(0);
        writer.WriteBit(0);
    }

    static byte[] NewVp8Payload(int width, int height, byte[] firstPartition)
    {
        byte[] payload = new byte[WebpConstants.Vp8LossyKeyFrameHeaderSize + firstPartition.Length];
        uint frameTag = 0x10U | ((uint)firstPartition.Length << 5);
        payload[0] = (byte)(frameTag & 255);
        payload[1] = (byte)((frameTag >> 8) & 255);
        payload[2] = (byte)((frameTag >> 16) & 255);
        payload[3] = WebpConstants.Vp8LossyKeyFrameSignature0;
        payload[4] = WebpConstants.Vp8LossyKeyFrameSignature1;
        payload[5] = WebpConstants.Vp8LossyKeyFrameSignature2;
        WebpEndianWriter.WriteUInt16(payload, 6, (ushort)width);
        WebpEndianWriter.WriteUInt16(payload, 8, (ushort)height);
        Array.Copy(firstPartition, 0, payload, WebpConstants.Vp8LossyKeyFrameHeaderSize, firstPartition.Length);
        return payload;
    }

}
