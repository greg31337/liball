using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8FrameHeaderDataParserTests
{
    [Test]
    public void Parse_ReadsDisabledSegmentationLoopFilterSingleTokenPartitionAndQuantizer()
    {
        Vp8HeaderBitWriter writer = new Vp8HeaderBitWriter();
        writer.WriteLiteral(0, 2);
        writer.WriteBit(0);
        writer.WriteBit(1);
        writer.WriteLiteral(22, 6);
        writer.WriteLiteral(5, 3);
        writer.WriteBit(0);
        writer.WriteLiteral(0, 2);
        writer.WriteLiteral(63, 7);
        writer.WriteBit(0);
        writer.WriteBit(0);
        writer.WriteBit(0);
        writer.WriteBit(0);
        writer.WriteBit(0);
        byte[] payload = NewVp8Payload(writer.ToBytes(), new byte[] { 1, 2, 3 });

        WebpVp8FrameHeaderDataParseResult result = WebpVp8FrameHeaderDataParser.Parse(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HeaderData.ColorSpace, Is.EqualTo(0));
        Assert.That(result.HeaderData.PixelClampingType, Is.EqualTo(0));
        Assert.That(result.HeaderData.Segmentation.Enabled, Is.False);
        Assert.That(result.HeaderData.LoopFilter.UseSimpleFilter, Is.True);
        Assert.That(result.HeaderData.LoopFilter.Level, Is.EqualTo(22));
        Assert.That(result.HeaderData.LoopFilter.Sharpness, Is.EqualTo(5));
        Assert.That(result.HeaderData.LoopFilter.DeltaEnabled, Is.False);
        Assert.That(result.HeaderData.TokenPartitions.PartitionCount, Is.EqualTo(1));
        Assert.That(result.HeaderData.TokenPartitions.PartitionSizes, Is.EqualTo(new int[] { 3 }));
        Assert.That(result.HeaderData.TokenPartitions.PartitionOffsets, Is.EqualTo(new int[] { WebpConstants.Vp8LossyKeyFrameHeaderSize + writer.ToBytes().Length }));
        Assert.That(result.HeaderData.Quantizer.BaseQIndex, Is.EqualTo(63));
        Assert.That(result.HeaderData.Quantizer.DeltaUpdate, Is.False);
    }

    [Test]
    public void Parse_ReadsSegmentationLoopDeltasTokenPartitionSizesAndQuantizerDeltas()
    {
        Vp8HeaderBitWriter writer = new Vp8HeaderBitWriter();
        writer.WriteLiteral(0, 2);
        writer.WriteBit(1);
        writer.WriteBit(1);
        writer.WriteBit(1);
        writer.WriteBit(1);
        writer.WriteMaybeSigned(4, 7);
        writer.WriteMaybeSigned(-5, 7);
        writer.WriteMaybeSigned(0, 7);
        writer.WriteMaybeSigned(7, 7);
        writer.WriteMaybeSigned(-8, 6);
        writer.WriteMaybeSigned(0, 6);
        writer.WriteMaybeSigned(3, 6);
        writer.WriteMaybeSigned(-1, 6);
        writer.WriteBit(1);
        writer.WriteLiteral(11, 8);
        writer.WriteBit(0);
        writer.WriteBit(1);
        writer.WriteLiteral(200, 8);
        writer.WriteBit(0);
        writer.WriteLiteral(31, 6);
        writer.WriteLiteral(2, 3);
        writer.WriteBit(1);
        writer.WriteBit(1);
        writer.WriteMaybeSigned(1, 6);
        writer.WriteMaybeSigned(-2, 6);
        writer.WriteMaybeSigned(0, 6);
        writer.WriteMaybeSigned(3, 6);
        writer.WriteMaybeSigned(-4, 6);
        writer.WriteMaybeSigned(5, 6);
        writer.WriteMaybeSigned(0, 6);
        writer.WriteMaybeSigned(0, 6);
        writer.WriteLiteral(2, 2);
        writer.WriteLiteral(91, 7);
        writer.WriteMaybeSigned(-3, 4);
        writer.WriteMaybeSigned(0, 4);
        writer.WriteMaybeSigned(6, 4);
        writer.WriteMaybeSigned(-7, 4);
        writer.WriteMaybeSigned(1, 4);
        byte[] firstPartition = writer.ToBytes();
        byte[] tokenData = NewTokenData(new int[] { 1, 2, 3, 4 });
        byte[] payload = NewVp8Payload(firstPartition, tokenData);

        WebpVp8FrameHeaderDataParseResult result = WebpVp8FrameHeaderDataParser.Parse(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HeaderData.Segmentation.Enabled, Is.True);
        Assert.That(result.HeaderData.Segmentation.UpdateMap, Is.True);
        Assert.That(result.HeaderData.Segmentation.UpdateData, Is.True);
        Assert.That(result.HeaderData.Segmentation.AbsoluteSegmentFeatureMode, Is.True);
        Assert.That(result.HeaderData.Segmentation.QuantizerDeltas, Is.EqualTo(new int[] { 4, -5, 0, 7 }));
        Assert.That(result.HeaderData.Segmentation.LoopFilterDeltas, Is.EqualTo(new int[] { -8, 0, 3, -1 }));
        Assert.That(result.HeaderData.Segmentation.SegmentTreeProbabilities, Is.EqualTo(new byte[] { 11, 255, 200 }));
        Assert.That(result.HeaderData.LoopFilter.UseSimpleFilter, Is.False);
        Assert.That(result.HeaderData.LoopFilter.Level, Is.EqualTo(31));
        Assert.That(result.HeaderData.LoopFilter.Sharpness, Is.EqualTo(2));
        Assert.That(result.HeaderData.LoopFilter.DeltaEnabled, Is.True);
        Assert.That(result.HeaderData.LoopFilter.DeltaUpdate, Is.True);
        Assert.That(result.HeaderData.LoopFilter.ReferenceDeltaUpdates, Is.EqualTo(new bool[] { true, true, false, true }));
        Assert.That(result.HeaderData.LoopFilter.ReferenceDeltas, Is.EqualTo(new int[] { 1, -2, 0, 3 }));
        Assert.That(result.HeaderData.LoopFilter.ModeDeltaUpdates, Is.EqualTo(new bool[] { true, true, false, false }));
        Assert.That(result.HeaderData.LoopFilter.ModeDeltas, Is.EqualTo(new int[] { -4, 5, 0, 0 }));
        Assert.That(result.HeaderData.TokenPartitions.Log2PartitionCount, Is.EqualTo(2));
        Assert.That(result.HeaderData.TokenPartitions.PartitionCount, Is.EqualTo(4));
        Assert.That(result.HeaderData.TokenPartitions.PartitionSizes, Is.EqualTo(new int[] { 1, 2, 3, 4 }));
        Assert.That(result.HeaderData.TokenPartitions.PartitionOffsets, Is.EqualTo(NewPartitionOffsets(firstPartition.Length, new int[] { 1, 2, 3, 4 })));
        Assert.That(result.HeaderData.Quantizer.BaseQIndex, Is.EqualTo(91));
        Assert.That(result.HeaderData.Quantizer.YDcDeltaPresent, Is.True);
        Assert.That(result.HeaderData.Quantizer.YDcDelta, Is.EqualTo(-3));
        Assert.That(result.HeaderData.Quantizer.Y2DcDeltaPresent, Is.False);
        Assert.That(result.HeaderData.Quantizer.Y2AcDelta, Is.EqualTo(6));
        Assert.That(result.HeaderData.Quantizer.UvDcDelta, Is.EqualTo(-7));
        Assert.That(result.HeaderData.Quantizer.UvAcDelta, Is.EqualTo(1));
        Assert.That(result.HeaderData.Quantizer.DeltaUpdate, Is.True);
    }

    [Test]
    public void Parse_ReportsTruncatedFirstPartitionAndTokenPartitionHeaders()
    {
        byte[] firstPartitionTooShort = NewVp8PayloadWithPartitionSize(new byte[] { 0, 0 }, Array.Empty<byte>(), 8);
        Vp8HeaderBitWriter writer = new Vp8HeaderBitWriter();
        writer.WriteLiteral(0, 2);
        writer.WriteBit(0);
        writer.WriteBit(0);
        writer.WriteLiteral(0, 6);
        writer.WriteLiteral(0, 3);
        writer.WriteBit(0);
        writer.WriteLiteral(3, 2);
        writer.WriteLiteral(0, 7);
        writer.WriteBit(0);
        writer.WriteBit(0);
        writer.WriteBit(0);
        writer.WriteBit(0);
        writer.WriteBit(0);
        byte[] missingTokenHeaders = NewVp8Payload(writer.ToBytes(), new byte[] { 1, 2, 3 });

        WebpVp8FrameHeaderDataParseResult firstPartitionResult = WebpVp8FrameHeaderDataParser.Parse(firstPartitionTooShort, WebpStrictness.Strict);
        WebpVp8FrameHeaderDataParseResult tokenResult = WebpVp8FrameHeaderDataParser.Parse(missingTokenHeaders, WebpStrictness.Strict);

        Assert.That(firstPartitionResult.IsValid, Is.False);
        Assert.That(firstPartitionResult.Diagnostics[0].Message, Does.Contain("truncated"));
        Assert.That(tokenResult.IsValid, Is.False);
        Assert.That(tokenResult.Diagnostics[0].Message, Does.Contain("token partition"));
    }

    [Test]
    public void Parse_ReturnsFrameHeaderDiagnostics()
    {
        byte[] payload = NewVp8Payload(new byte[] { 0, 0, 0 }, Array.Empty<byte>());
        payload[3] = 0;

        WebpVp8FrameHeaderDataParseResult result = WebpVp8FrameHeaderDataParser.Parse(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("signature"));
    }

    static byte[] NewVp8Payload(byte[] firstPartition, byte[] tokenData)
    {
        return NewVp8PayloadWithPartitionSize(firstPartition, tokenData, firstPartition.Length);
    }

    static byte[] NewVp8PayloadWithPartitionSize(byte[] firstPartition, byte[] tokenData, int firstPartitionSize)
    {
        byte[] payload = new byte[WebpConstants.Vp8LossyKeyFrameHeaderSize + firstPartition.Length + tokenData.Length];
        uint frameTag = 0x10U | ((uint)firstPartitionSize << 5);
        payload[0] = (byte)(frameTag & 255);
        payload[1] = (byte)((frameTag >> 8) & 255);
        payload[2] = (byte)((frameTag >> 16) & 255);
        payload[3] = WebpConstants.Vp8LossyKeyFrameSignature0;
        payload[4] = WebpConstants.Vp8LossyKeyFrameSignature1;
        payload[5] = WebpConstants.Vp8LossyKeyFrameSignature2;
        WebpEndianWriter.WriteUInt16(payload, 6, 16);
        WebpEndianWriter.WriteUInt16(payload, 8, 16);
        Array.Copy(firstPartition, 0, payload, WebpConstants.Vp8LossyKeyFrameHeaderSize, firstPartition.Length);
        Array.Copy(tokenData, 0, payload, WebpConstants.Vp8LossyKeyFrameHeaderSize + firstPartition.Length, tokenData.Length);
        return payload;
    }

    static byte[] NewTokenData(int[] partitionSizes)
    {
        int headerLength = (partitionSizes.Length - 1) * 3;
        int totalLength = headerLength;
        int index = 0;
        while (index < partitionSizes.Length)
        {
            totalLength = totalLength + partitionSizes[index];
            index = index + 1;
        }

        byte[] data = new byte[totalLength];
        index = 0;
        while (index < partitionSizes.Length - 1)
        {
            int size = partitionSizes[index];
            int offset = index * 3;
            data[offset] = (byte)(size & 255);
            data[offset + 1] = (byte)((size >> 8) & 255);
            data[offset + 2] = (byte)((size >> 16) & 255);
            index = index + 1;
        }

        return data;
    }

    static int[] NewPartitionOffsets(int firstPartitionLength, int[] partitionSizes)
    {
        int[] offsets = new int[partitionSizes.Length];
        int current = WebpConstants.Vp8LossyKeyFrameHeaderSize + firstPartitionLength + ((partitionSizes.Length - 1) * 3);
        int index = 0;
        while (index < partitionSizes.Length)
        {
            offsets[index] = current;
            current = current + partitionSizes[index];
            index = index + 1;
        }

        return offsets;
    }
}

public class Vp8HeaderBitWriter
{
    public List<int> Bits;

    public Vp8HeaderBitWriter()
    {
        Bits = new List<int>();
    }

    public void WriteBit(int bit)
    {
        Bits.Add(bit == 0 ? 0 : 1);
    }

    public void WriteLiteral(int value, int bitCount)
    {
        int index = bitCount - 1;
        while (index >= 0)
        {
            WriteBit((value >> index) & 1);
            index = index - 1;
        }
    }

    public void WriteMaybeSigned(int value, int bitCount)
    {
        if (value == 0)
        {
            WriteBit(0);
            return;
        }

        WriteBit(1);
        WriteLiteral(Math.Abs(value), bitCount);
        WriteBit(value < 0 ? 1 : 0);
    }

    public byte[] ToBytes()
    {
        int byteCount = (Bits.Count + 7) / 8;
        if (byteCount < 2)
        {
            byteCount = 2;
        }

        byte[] data = new byte[byteCount];
        int index = 0;
        while (index < Bits.Count)
        {
            if (Bits[index] != 0)
            {
                data[index / 8] = (byte)(data[index / 8] | (1 << (7 - (index & 7))));
            }

            index = index + 1;
        }

        return data;
    }
}
