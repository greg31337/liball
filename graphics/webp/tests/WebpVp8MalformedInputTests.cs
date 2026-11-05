using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8MalformedInputTests
{
    [Test]
    public void Decode_ReportsInterframeAndInvalidSignatureDiagnostics()
    {
        byte[] interframe = NewVp8Payload(16, 16, NewBaseHeaderPartition(0), Array.Empty<byte>());
        interframe[0] = (byte)(interframe[0] | 1);
        byte[] badSignature = NewVp8Payload(16, 16, NewBaseHeaderPartition(0), Array.Empty<byte>());
        badSignature[3] = 0;

        WebpVp8DecodeResult interframeResult = WebpVp8Decoder.Decode(interframe, WebpStrictness.Strict);
        WebpVp8DecodeResult signatureResult = WebpVp8Decoder.Decode(badSignature, WebpStrictness.Strict);

        Assert.That(interframeResult.IsValid, Is.False);
        Assert.That(interframeResult.Diagnostics[0].Message, Does.Contain("key frame"));
        Assert.That(signatureResult.IsValid, Is.False);
        Assert.That(signatureResult.Diagnostics[0].Message, Does.Contain("signature"));
    }

    [Test]
    public void Decode_ReportsFirstPartitionLengthBeyondPayload()
    {
        byte[] payload = NewVp8PayloadWithPartitionSize(16, 16, new byte[] { 0, 0 }, Array.Empty<byte>(), 8);

        WebpVp8DecodeResult result = WebpVp8Decoder.Decode(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("truncated"));
    }

    [Test]
    public void Decode_ReportsTruncatedTokenPartitionSizeHeaders()
    {
        byte[] payload = NewVp8Payload(16, 16, NewBaseHeaderPartition(3), new byte[] { 1, 2, 3 });

        WebpVp8DecodeResult result = WebpVp8Decoder.Decode(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("token partition"));
    }

    [Test]
    public void Decode_ReportsTokenPartitionLengthBeyondRemainingData()
    {
        byte[] tokenData = new byte[] { 10, 0, 0, 1 };
        byte[] payload = NewVp8Payload(16, 16, NewBaseHeaderPartition(1), tokenData);

        WebpVp8DecodeResult result = WebpVp8Decoder.Decode(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("partition length"));
    }

    [Test]
    public void Decode_ReportsTruncatedBooleanModeData()
    {
        byte[] payload = NewVp8Payload(16, 16, NewEntropyOnlyPartition(), Array.Empty<byte>());

        WebpVp8DecodeResult result = WebpVp8Decoder.Decode(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("truncated"));
    }

    static byte[] NewBaseHeaderPartition(int log2TokenPartitions)
    {
        Vp8HeaderBitWriter writer = new Vp8HeaderBitWriter();
        WriteFrameHeaderData(writer, log2TokenPartitions);
        return writer.ToBytes();
    }

    static byte[] NewEntropyOnlyPartition()
    {
        WebpVp8BoolWriter writer = new WebpVp8BoolWriter();
        WriteFrameHeaderData(writer, 0);
        writer.WriteBit(0);
        int index = 0;
        while (index < WebpVp8ProbabilityTables.CoefficientUpdate.Length)
        {
            writer.WriteBool(WebpVp8ProbabilityTables.CoefficientUpdate[index], 0);
            index = index + 1;
        }

        writer.WriteBit(1);
        writer.WriteLiteral(128, 8);
        byte[] complete = writer.ToBytes();
        byte[] truncated = new byte[complete.Length - 1];
        Array.Copy(complete, truncated, truncated.Length);
        return truncated;
    }

    static void WriteFrameHeaderData(WebpVp8BoolWriter writer, int log2TokenPartitions)
    {
        writer.WriteLiteral(0, 2);
        writer.WriteBit(0);
        writer.WriteBit(1);
        writer.WriteLiteral(0, 6);
        writer.WriteLiteral(0, 3);
        writer.WriteBit(0);
        writer.WriteLiteral(log2TokenPartitions, 2);
        writer.WriteLiteral(0, 7);
        writer.WriteBit(0);
        writer.WriteBit(0);
        writer.WriteBit(0);
        writer.WriteBit(0);
        writer.WriteBit(0);
    }

    static void WriteFrameHeaderData(Vp8HeaderBitWriter writer, int log2TokenPartitions)
    {
        writer.WriteLiteral(0, 2);
        writer.WriteBit(0);
        writer.WriteBit(1);
        writer.WriteLiteral(0, 6);
        writer.WriteLiteral(0, 3);
        writer.WriteBit(0);
        writer.WriteLiteral(log2TokenPartitions, 2);
        writer.WriteLiteral(0, 7);
        writer.WriteBit(0);
        writer.WriteBit(0);
        writer.WriteBit(0);
        writer.WriteBit(0);
        writer.WriteBit(0);
    }

    static byte[] NewVp8Payload(int width, int height, byte[] firstPartition, byte[] tokenData)
    {
        return NewVp8PayloadWithPartitionSize(width, height, firstPartition, tokenData, firstPartition.Length);
    }

    static byte[] NewVp8PayloadWithPartitionSize(int width, int height, byte[] firstPartition, byte[] tokenData, int firstPartitionSize)
    {
        byte[] payload = new byte[WebpConstants.Vp8LossyKeyFrameHeaderSize + firstPartition.Length + tokenData.Length];
        uint frameTag = 0x10U | ((uint)firstPartitionSize << 5);
        payload[0] = (byte)(frameTag & 255);
        payload[1] = (byte)((frameTag >> 8) & 255);
        payload[2] = (byte)((frameTag >> 16) & 255);
        payload[3] = WebpConstants.Vp8LossyKeyFrameSignature0;
        payload[4] = WebpConstants.Vp8LossyKeyFrameSignature1;
        payload[5] = WebpConstants.Vp8LossyKeyFrameSignature2;
        WebpEndianWriter.WriteUInt16(payload, 6, (ushort)width);
        WebpEndianWriter.WriteUInt16(payload, 8, (ushort)height);
        Array.Copy(firstPartition, 0, payload, WebpConstants.Vp8LossyKeyFrameHeaderSize, firstPartition.Length);
        Array.Copy(tokenData, 0, payload, WebpConstants.Vp8LossyKeyFrameHeaderSize + firstPartition.Length, tokenData.Length);
        return payload;
    }
}
