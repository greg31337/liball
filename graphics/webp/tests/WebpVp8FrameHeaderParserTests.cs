using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8FrameHeaderParserTests
{
    [Test]
    public void Parse_ReadsFrameTagKeyFrameAndPartitionFields()
    {
        byte[] payload = NewVp8Payload(320, 240, 2, true, 1234, 0, 0);

        WebpVp8FrameHeaderParseResult result = WebpVp8FrameHeaderParser.Parse(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Header.IsKeyFrame, Is.True);
        Assert.That(result.Header.Version, Is.EqualTo(2));
        Assert.That(result.Header.ShowFrame, Is.True);
        Assert.That(result.Header.FirstPartitionSize, Is.EqualTo(1234));
        Assert.That(result.Header.HasValidKeyFrameSignature, Is.True);
    }

    [Test]
    public void Parse_ReadsDimensionsScaleColorSpaceAndClampBits()
    {
        byte[] payload = NewVp8Payload(320, 240, 0, true, 10, 2, 3);
        payload[10] = 0x03;

        WebpVp8FrameHeaderParseResult result = WebpVp8FrameHeaderParser.Parse(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Header.Width, Is.EqualTo(320));
        Assert.That(result.Header.Height, Is.EqualTo(240));
        Assert.That(result.Header.HorizontalScale, Is.EqualTo(2));
        Assert.That(result.Header.VerticalScale, Is.EqualTo(3));
        Assert.That(result.Header.ColorSpace, Is.EqualTo(1));
        Assert.That(result.Header.PixelClampingType, Is.EqualTo(1));
    }

    [Test]
    public void Parse_DefaultsColorSpaceAndClampWhenFirstPartitionByteIsUnavailable()
    {
        byte[] payload = NewVp8Payload(1, 1, 0, true, 10, 0, 0);

        WebpVp8FrameHeaderParseResult result = WebpVp8FrameHeaderParser.Parse(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Header.ColorSpace, Is.EqualTo(0));
        Assert.That(result.Header.PixelClampingType, Is.EqualTo(0));
    }

    [Test]
    public void Parse_RejectsInterframesInvalidSignatureAndZeroDimensions()
    {
        byte[] interframe = NewVp8Payload(1, 1, 0, true, 10, 0, 0);
        interframe[0] = (byte)(interframe[0] | 1);
        byte[] badSignature = NewVp8Payload(1, 1, 0, true, 10, 0, 0);
        badSignature[3] = 0;
        byte[] zeroDimensions = NewVp8Payload(1, 1, 0, true, 10, 0, 0);
        WebpEndianWriter.WriteUInt16(zeroDimensions, 6, 0);

        WebpVp8FrameHeaderParseResult interframeResult = WebpVp8FrameHeaderParser.Parse(interframe, WebpStrictness.Strict);
        WebpVp8FrameHeaderParseResult signatureResult = WebpVp8FrameHeaderParser.Parse(badSignature, WebpStrictness.Strict);
        WebpVp8FrameHeaderParseResult dimensionResult = WebpVp8FrameHeaderParser.Parse(zeroDimensions, WebpStrictness.Strict);

        Assert.That(interframeResult.IsValid, Is.False);
        Assert.That(interframeResult.Diagnostics[0].Message, Does.Contain("key frame"));
        Assert.That(signatureResult.IsValid, Is.False);
        Assert.That(signatureResult.Diagnostics[0].Message, Does.Contain("signature"));
        Assert.That(dimensionResult.IsValid, Is.False);
        Assert.That(dimensionResult.Diagnostics[0].Message, Does.Contain("dimensions"));
    }

    [Test]
    public void Parse_RejectsTruncatedPayload()
    {
        WebpVp8FrameHeaderParseResult result = WebpVp8FrameHeaderParser.Parse(new byte[] { 1, 2, 3 }, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("too short"));
    }

    static byte[] NewVp8Payload(int width, int height, int version, bool showFrame, int firstPartitionSize, int horizontalScale, int verticalScale)
    {
        byte[] payload = new byte[WebpConstants.Vp8LossyKeyFrameHeaderSize + 1];
        uint frameTag = ((uint)version << 1) | ((uint)firstPartitionSize << 5);
        if (showFrame)
        {
            frameTag = frameTag | 0x10U;
        }

        payload[0] = (byte)(frameTag & 255);
        payload[1] = (byte)((frameTag >> 8) & 255);
        payload[2] = (byte)((frameTag >> 16) & 255);
        payload[3] = WebpConstants.Vp8LossyKeyFrameSignature0;
        payload[4] = WebpConstants.Vp8LossyKeyFrameSignature1;
        payload[5] = WebpConstants.Vp8LossyKeyFrameSignature2;
        WebpEndianWriter.WriteUInt16(payload, 6, (ushort)(width | (horizontalScale << 14)));
        WebpEndianWriter.WriteUInt16(payload, 8, (ushort)(height | (verticalScale << 14)));
        return payload;
    }
}
