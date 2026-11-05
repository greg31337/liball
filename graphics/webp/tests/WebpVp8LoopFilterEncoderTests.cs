using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8LoopFilterEncoderTests
{
    [Test]
    public void SelectKeyFrameParameters_ScalesLevelFromQuantizer()
    {
        WebpVp8LoopFilterHeader low = WebpVp8LoopFilterEncoder.SelectKeyFrameParameters(0);
        WebpVp8LoopFilterHeader middle = WebpVp8LoopFilterEncoder.SelectKeyFrameParameters(64, 3);
        WebpVp8LoopFilterHeader high = WebpVp8LoopFilterEncoder.SelectKeyFrameParameters(127);

        Assert.That(low.UseSimpleFilter, Is.True);
        Assert.That(low.Level, Is.EqualTo(0));
        Assert.That(low.Sharpness, Is.EqualTo(0));
        Assert.That(low.DeltaEnabled, Is.False);
        Assert.That(middle.Level, Is.EqualTo(32));
        Assert.That(middle.Sharpness, Is.EqualTo(3));
        Assert.That(high.Level, Is.EqualTo(63));
    }

    [Test]
    public void Write_RoundTripsSimpleFilterHeaderThroughFrameHeaderParser()
    {
        WebpVp8LoopFilterHeader header = WebpVp8LoopFilterEncoder.SelectKeyFrameParameters(42, 5);
        byte[] payload = NewPayloadWithLoopFilter(header);

        WebpVp8FrameHeaderDataParseResult result = WebpVp8FrameHeaderDataParser.Parse(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HeaderData.LoopFilter.UseSimpleFilter, Is.True);
        Assert.That(result.HeaderData.LoopFilter.Level, Is.EqualTo(21));
        Assert.That(result.HeaderData.LoopFilter.Sharpness, Is.EqualTo(5));
        Assert.That(result.HeaderData.LoopFilter.DeltaEnabled, Is.False);
    }

    [Test]
    public void Write_RoundTripsNormalFilterDeltaUpdates()
    {
        WebpVp8LoopFilterHeader header = new WebpVp8LoopFilterHeader();
        header.UseSimpleFilter = false;
        header.Level = 31;
        header.Sharpness = 2;
        header.DeltaEnabled = true;
        header.DeltaUpdate = true;
        header.ReferenceDeltaUpdates = new bool[] { true, false, true, true };
        header.ReferenceDeltas = new int[] { 3, 0, -7, 12 };
        header.ModeDeltaUpdates = new bool[] { false, true, true, false };
        header.ModeDeltas = new int[] { 0, -5, 6, 0 };
        byte[] payload = NewPayloadWithLoopFilter(header);

        WebpVp8FrameHeaderDataParseResult result = WebpVp8FrameHeaderDataParser.Parse(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HeaderData.LoopFilter.UseSimpleFilter, Is.False);
        Assert.That(result.HeaderData.LoopFilter.Level, Is.EqualTo(31));
        Assert.That(result.HeaderData.LoopFilter.Sharpness, Is.EqualTo(2));
        Assert.That(result.HeaderData.LoopFilter.DeltaEnabled, Is.True);
        Assert.That(result.HeaderData.LoopFilter.DeltaUpdate, Is.True);
        Assert.That(result.HeaderData.LoopFilter.ReferenceDeltaUpdates, Is.EqualTo(header.ReferenceDeltaUpdates));
        Assert.That(result.HeaderData.LoopFilter.ReferenceDeltas, Is.EqualTo(header.ReferenceDeltas));
        Assert.That(result.HeaderData.LoopFilter.ModeDeltaUpdates, Is.EqualTo(header.ModeDeltaUpdates));
        Assert.That(result.HeaderData.LoopFilter.ModeDeltas, Is.EqualTo(header.ModeDeltas));
    }

    [Test]
    public void KeyFrameEncoder_WritesSelectedLoopFilterLevel()
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(1, 1);
        buffer.Rgba[3] = 255;

        byte[] payload = WebpVp8KeyFrameEncoder.Encode(buffer, 64);
        WebpVp8FrameHeaderDataParseResult result = WebpVp8FrameHeaderDataParser.Parse(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HeaderData.LoopFilter.UseSimpleFilter, Is.True);
        Assert.That(result.HeaderData.LoopFilter.Level, Is.EqualTo(32));
        Assert.That(result.HeaderData.LoopFilter.Sharpness, Is.EqualTo(0));
    }

    [Test]
    public void LoopFilterEncoder_RejectsInvalidInputs()
    {
        WebpVp8BoolWriter writer = new WebpVp8BoolWriter();
        WebpVp8LoopFilterHeader header = WebpVp8LoopFilterEncoder.SelectKeyFrameParameters(10);

        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LoopFilterEncoder.SelectKeyFrameParameters(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LoopFilterEncoder.SelectKeyFrameParameters(128));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LoopFilterEncoder.SelectKeyFrameParameters(10, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LoopFilterEncoder.SelectKeyFrameParameters(10, 8));
        Assert.Throws<ArgumentNullException>(() => WebpVp8LoopFilterEncoder.Write(null!, header));
        Assert.Throws<ArgumentNullException>(() => WebpVp8LoopFilterEncoder.Write(writer, null!));

        header.Level = 64;
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LoopFilterEncoder.Write(writer, header));
        header.Level = 1;
        header.ReferenceDeltaUpdates = new bool[3];
        Assert.Throws<ArgumentException>(() => WebpVp8LoopFilterEncoder.Write(writer, header));
        header.ReferenceDeltaUpdates = new bool[4];
        header.DeltaEnabled = true;
        header.DeltaUpdate = true;
        header.ReferenceDeltaUpdates[0] = true;
        header.ReferenceDeltas[0] = 64;
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LoopFilterEncoder.Write(writer, header));
    }

    static byte[] NewPayloadWithLoopFilter(WebpVp8LoopFilterHeader header)
    {
        WebpVp8BoolWriter writer = new WebpVp8BoolWriter();
        writer.WriteLiteral(0, 2);
        writer.WriteBit(0);
        WebpVp8LoopFilterEncoder.Write(writer, header);
        writer.WriteLiteral(0, 2);
        writer.WriteLiteral(23, 7);
        int index = 0;
        while (index < 5)
        {
            writer.WriteBit(0);
            index = index + 1;
        }

        byte[] firstPartition = writer.ToBytes();
        byte[] payload = new byte[WebpConstants.Vp8LossyKeyFrameHeaderSize + firstPartition.Length];
        uint frameTag = 0x10U | ((uint)firstPartition.Length << 5);
        payload[0] = (byte)(frameTag & 255);
        payload[1] = (byte)((frameTag >> 8) & 255);
        payload[2] = (byte)((frameTag >> 16) & 255);
        payload[3] = WebpConstants.Vp8LossyKeyFrameSignature0;
        payload[4] = WebpConstants.Vp8LossyKeyFrameSignature1;
        payload[5] = WebpConstants.Vp8LossyKeyFrameSignature2;
        WebpEndianWriter.WriteUInt16(payload, 6, 16);
        WebpEndianWriter.WriteUInt16(payload, 8, 16);
        Array.Copy(firstPartition, 0, payload, WebpConstants.Vp8LossyKeyFrameHeaderSize, firstPartition.Length);
        return payload;
    }
}
