using rtp;

namespace rtp.Tests.Wire;

public class RtcpExtendedReportBoundaryCoverageTests
{
    [Test]
    public void EveryRfc3611BlockAndChunkFormRoundTripsTogether()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpExtendedReportBlock loss = CreateRunLengthBlock(
            RtcpExtendedReportBlockType.LossRunLength,
            10,
            25,
            new RtcpRunLengthChunk[] { CreateVectorChunk(0x7FFF) });
        RtcpExtendedReportBlock duplicate = CreateRunLengthBlock(
            RtcpExtendedReportBlockType.DuplicateRunLength,
            20,
            24,
            new RtcpRunLengthChunk[]
            {
                CreateRunChunk(true, 2),
                CreateRunChunk(false, 2)
            });
        RtcpExtendedReportBlock receipt = CreateReceiptTimesBlock(
            65535,
            1,
            0,
            new uint[] { 0U, uint.MaxValue });
        RtcpExtendedReportBlock reference = new RtcpExtendedReportBlock();
        reference.Type = RtcpExtendedReportBlockType.ReceiverReferenceTime;
        reference.ReceiverReferenceTime = 0x0123456789ABCDEFUL;
        RtcpExtendedReportBlock delay = CreateDelayBlock(1);
        RtcpExtendedReportBlock statistics = CreateStatisticsBlock();
        RtcpExtendedReportBlock voice = CreateVoiceMetricsBlock();
        RtcpPacket packet = CreatePacket(new RtcpExtendedReportBlock[]
        {
            loss,
            duplicate,
            receipt,
            reference,
            delay,
            statistics,
            voice
        });

        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
        RtcpExtendedReportBlock[] decoded = codec.DecodeRtcpPacket(encoded)
            .ExtendedReport!.Blocks;

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Length, Is.EqualTo(7));
            int index = 0;
            while (index < decoded.Length)
            {
                Assert.That(decoded[index].Type,
                    Is.EqualTo((RtcpExtendedReportBlockType)(index + 1)));
                index = index + 1;
            }

            Assert.That(decoded[0].RunLengthChunks.Length, Is.EqualTo(1));
            Assert.That(decoded[0].RunLengthChunks[0].IsBitVector, Is.True);
            Assert.That(decoded[0].RunLengthChunks[0].BitVector,
                Is.EqualTo(0x7FFF));
            Assert.That(decoded[1].RunLengthChunks[0].PositiveRun, Is.True);
            Assert.That(decoded[1].RunLengthChunks[1].PositiveRun, Is.False);
            Assert.That(decoded[2].ReceiptTimes,
                Is.EqualTo(new uint[] { 0U, uint.MaxValue }));
            Assert.That(decoded[3].ReceiverReferenceTime,
                Is.EqualTo(0x0123456789ABCDEFUL));
            Assert.That(decoded[4].DelayReports.Length, Is.EqualTo(1));
            Assert.That(decoded[5].ReportsLoss, Is.True);
            Assert.That(decoded[6].MinimumGapThreshold, Is.EqualTo(16));
            Assert.That(codec.EncodeRtcpPacketToArray(
                codec.DecodeRtcpPacket(encoded)), Is.EqualTo(encoded));
        });
    }

    [Test]
    public void EveryUnassignedBlockTypePreservesHeaderAndAlignedData()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpExtendedReportBlock[] blocks = new RtcpExtendedReportBlock[249];
        int blockIndex = 0;
        int wireType = 0;
        while (wireType <= 255)
        {
            if (wireType == 0 || wireType > 7)
            {
                RtcpExtendedReportBlock block = new RtcpExtendedReportBlock();
                block.Type = RtcpExtendedReportBlockType.Unknown;
                block.WireType = (byte)wireType;
                block.TypeSpecific = (byte)(wireType ^ 0xA5);
                block.Data = new byte[(wireType % 3) * 4];
                int dataIndex = 0;
                while (dataIndex < block.Data.Length)
                {
                    block.Data[dataIndex] = (byte)(wireType + dataIndex);
                    dataIndex = dataIndex + 1;
                }

                blocks[blockIndex] = block;
                blockIndex = blockIndex + 1;
            }

            wireType = wireType + 1;
        }

        byte[] encoded = codec.EncodeRtcpPacketToArray(CreatePacket(blocks));
        RtcpExtendedReportBlock[] decoded = codec.DecodeRtcpPacket(encoded)
            .ExtendedReport!.Blocks;

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Length, Is.EqualTo(249));
            int index = 0;
            while (index < blocks.Length)
            {
                Assert.That(decoded[index].Type,
                    Is.EqualTo(RtcpExtendedReportBlockType.Unknown));
                Assert.That(decoded[index].WireType,
                    Is.EqualTo(blocks[index].WireType));
                Assert.That(decoded[index].TypeSpecific,
                    Is.EqualTo(blocks[index].TypeSpecific));
                Assert.That(decoded[index].Data,
                    Is.EqualTo(blocks[index].Data));
                index = index + 1;
            }

            Assert.That(codec.EncodeRtcpPacketToArray(
                codec.DecodeRtcpPacket(encoded)), Is.EqualTo(encoded));
        });
    }

    [Test]
    public void VariableBlocksReachTheirMinimumLegalLengths()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpExtendedReportBlock unknown = new RtcpExtendedReportBlock();
        unknown.Type = RtcpExtendedReportBlockType.Unknown;
        unknown.WireType = 200;
        RtcpExtendedReportBlock loss = CreateRunLengthBlock(
            RtcpExtendedReportBlockType.LossRunLength,
            10,
            10,
            []);
        RtcpExtendedReportBlock duplicate = CreateRunLengthBlock(
            RtcpExtendedReportBlockType.DuplicateRunLength,
            20,
            20,
            []);
        RtcpExtendedReportBlock receipt = CreateReceiptTimesBlock(
            30,
            30,
            15,
            []);
        RtcpExtendedReportBlock delay = CreateDelayBlock(1);

        byte[] encoded = codec.EncodeRtcpPacketToArray(CreatePacket(
            new RtcpExtendedReportBlock[]
            {
                unknown, loss, duplicate, receipt, delay
            }));
        RtcpExtendedReportBlock[] decoded = codec.DecodeRtcpPacket(encoded)
            .ExtendedReport!.Blocks;

        Assert.Multiple(() =>
        {
            Assert.That(decoded[0].BlockLengthInWordsMinusOne, Is.Zero);
            Assert.That(decoded[1].BlockLengthInWordsMinusOne, Is.EqualTo(2));
            Assert.That(decoded[2].BlockLengthInWordsMinusOne, Is.EqualTo(2));
            Assert.That(decoded[3].BlockLengthInWordsMinusOne, Is.EqualTo(2));
            Assert.That(decoded[4].BlockLengthInWordsMinusOne, Is.EqualTo(3));
            Assert.That(decoded[1].RunLengthChunks, Is.Empty);
            Assert.That(decoded[2].RunLengthChunks, Is.Empty);
            Assert.That(decoded[3].ReceiptTimes, Is.Empty);
        });
    }

    [Test]
    public void VariableBlocksReachMaximumPacketBoundaries()
    {
        IRtcpPacketCodec codec = CreateCodec();

        RtcpExtendedReportBlock unknown = new RtcpExtendedReportBlock();
        unknown.Type = RtcpExtendedReportBlockType.Unknown;
        unknown.WireType = 200;
        unknown.Data = new byte[262132];
        unknown.Data[0] = 1;
        unknown.Data[unknown.Data.Length - 1] = 2;
        byte[] unknownWire = codec.EncodeRtcpPacketToArray(CreatePacket(
            new RtcpExtendedReportBlock[] { unknown }));
        RtcpExtendedReportBlock unknownDecoded = codec.DecodeRtcpPacket(
            unknownWire).ExtendedReport!.Blocks[0];

        uint[] receiptTimes = new uint[65531];
        receiptTimes[0] = 1U;
        receiptTimes[receiptTimes.Length - 1] = uint.MaxValue;
        RtcpExtendedReportBlock receipt = CreateReceiptTimesBlock(
            0,
            65531,
            0,
            receiptTimes);
        byte[] receiptWire = codec.EncodeRtcpPacketToArray(CreatePacket(
            new RtcpExtendedReportBlock[] { receipt }));
        RtcpExtendedReportBlock receiptDecoded = codec.DecodeRtcpPacket(
            receiptWire).ExtendedReport!.Blocks[0];

        RtcpExtendedReportBlock delay = CreateDelayBlock(21844);
        delay.DelayReports[0].ReceiverSourceIdentifier = 1U;
        delay.DelayReports[delay.DelayReports.Length - 1]
            .DelaySinceLastReceiverReport = uint.MaxValue;
        byte[] delayWire = codec.EncodeRtcpPacketToArray(CreatePacket(
            new RtcpExtendedReportBlock[] { delay }));
        RtcpExtendedReportBlock delayDecoded = codec.DecodeRtcpPacket(
            delayWire).ExtendedReport!.Blocks[0];

        RtcpRunLengthChunk[] chunks = new RtcpRunLengthChunk[65533];
        int chunkIndex = 0;
        while (chunkIndex < chunks.Length)
        {
            chunks[chunkIndex] = CreateRunChunk(chunkIndex % 2 == 0, 1);
            chunkIndex = chunkIndex + 1;
        }

        RtcpExtendedReportBlock loss = CreateRunLengthBlock(
            RtcpExtendedReportBlockType.LossRunLength,
            0,
            65533,
            chunks);
        byte[] lossWire = codec.EncodeRtcpPacketToArray(CreatePacket(
            new RtcpExtendedReportBlock[] { loss }));
        RtcpExtendedReportBlock lossDecoded = codec.DecodeRtcpPacket(lossWire)
            .ExtendedReport!.Blocks[0];

        Assert.Multiple(() =>
        {
            Assert.That(unknownWire.Length,
                Is.EqualTo(RtcpProtocolConstants.MaximumPacketSize));
            Assert.That(unknownDecoded.BlockLengthInWordsMinusOne,
                Is.EqualTo(65533));
            Assert.That(unknownDecoded.Data.Length, Is.EqualTo(262132));
            Assert.That(unknownDecoded.Data[0], Is.EqualTo(1));
            Assert.That(unknownDecoded.Data[262131], Is.EqualTo(2));

            Assert.That(receiptWire.Length,
                Is.EqualTo(RtcpProtocolConstants.MaximumPacketSize));
            Assert.That(receiptDecoded.BlockLengthInWordsMinusOne,
                Is.EqualTo(65533));
            Assert.That(receiptDecoded.ReceiptTimes.Length, Is.EqualTo(65531));
            Assert.That(receiptDecoded.ReceiptTimes[65530],
                Is.EqualTo(uint.MaxValue));

            Assert.That(delayWire.Length, Is.EqualTo(262140));
            Assert.That(delayDecoded.BlockLengthInWordsMinusOne,
                Is.EqualTo(65532));
            Assert.That(delayDecoded.DelayReports.Length, Is.EqualTo(21844));
            Assert.That(delayDecoded.DelayReports[21843]
                .DelaySinceLastReceiverReport, Is.EqualTo(uint.MaxValue));

            Assert.That(lossWire.Length, Is.EqualTo(131088));
            Assert.That(lossDecoded.BlockLengthInWordsMinusOne,
                Is.EqualTo(32769));
            Assert.That(lossDecoded.RunLengthChunks.Length, Is.EqualTo(65533));
        });
    }

    [Test]
    public void OddRunLengthChunkCountsUseOnlyATerminatingNullForAlignment()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpExtendedReportBlock one = CreateRunLengthBlock(
            RtcpExtendedReportBlockType.LossRunLength,
            0,
            1,
            new RtcpRunLengthChunk[] { CreateRunChunk(true, 1) });
        RtcpExtendedReportBlock two = CreateRunLengthBlock(
            RtcpExtendedReportBlockType.DuplicateRunLength,
            0,
            2,
            new RtcpRunLengthChunk[]
            {
                CreateRunChunk(false, 1),
                CreateRunChunk(true, 1)
            });
        RtcpExtendedReportBlock three = CreateRunLengthBlock(
            RtcpExtendedReportBlockType.LossRunLength,
            0,
            3,
            new RtcpRunLengthChunk[]
            {
                CreateRunChunk(true, 1),
                CreateRunChunk(false, 1),
                CreateRunChunk(true, 1)
            });

        byte[] encoded = codec.EncodeRtcpPacketToArray(CreatePacket(
            new RtcpExtendedReportBlock[] { one, two, three }));
        RtcpExtendedReportBlock[] decoded = codec.DecodeRtcpPacket(encoded)
            .ExtendedReport!.Blocks;

        Assert.Multiple(() =>
        {
            Assert.That(decoded[0].BlockLengthInWordsMinusOne, Is.EqualTo(3));
            Assert.That(decoded[1].BlockLengthInWordsMinusOne, Is.EqualTo(3));
            Assert.That(decoded[2].BlockLengthInWordsMinusOne, Is.EqualTo(4));
            Assert.That(decoded[0].RunLengthChunks.Length, Is.EqualTo(1));
            Assert.That(decoded[1].RunLengthChunks.Length, Is.EqualTo(2));
            Assert.That(decoded[2].RunLengthChunks.Length, Is.EqualTo(3));
            Assert.That(ReadUInt16(encoded, 22), Is.Zero);
            Assert.That(ReadUInt16(encoded, 58), Is.Zero);
        });
    }

    [Test]
    public void EveryThinningExponentPreservesItsReportedPacket()
    {
        IRtcpPacketCodec codec = CreateCodec();
        int thinning = 0;
        while (thinning <= 15)
        {
            RtcpExtendedReportBlock block = CreateRunLengthBlock(
                RtcpExtendedReportBlockType.LossRunLength,
                0,
                1,
                new RtcpRunLengthChunk[] { CreateVectorChunk(0x4000) });
            block.Thinning = (byte)thinning;
            byte[] encoded = codec.EncodeRtcpPacketToArray(CreatePacket(
                new RtcpExtendedReportBlock[] { block }));
            RtcpExtendedReportBlock decoded = codec.DecodeRtcpPacket(encoded)
                .ExtendedReport!.Blocks[0];

            Assert.Multiple(() =>
            {
                Assert.That(encoded[9], Is.EqualTo(thinning));
                Assert.That(decoded.Thinning, Is.EqualTo(thinning));
                Assert.That(decoded.RunLengthChunks[0].BitVector,
                    Is.EqualTo(0x4000));
            });

            thinning = thinning + 1;
        }
    }

    [Test]
    public void NestedBlockTruncationsAndLengthMismatchesFailAtomically()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpExtendedReportBlock unknown = new RtcpExtendedReportBlock();
        unknown.Type = RtcpExtendedReportBlockType.Unknown;
        unknown.WireType = 200;
        unknown.Data = new byte[] { 200, 0, 0, 2 };
        RtcpExtendedReportBlock reference = new RtcpExtendedReportBlock();
        reference.Type = RtcpExtendedReportBlockType.ReceiverReferenceTime;
        reference.ReceiverReferenceTime = 1UL;
        RtcpExtendedReportBlock[] blocks = new RtcpExtendedReportBlock[]
        {
            CreateRunLengthBlock(
                RtcpExtendedReportBlockType.LossRunLength,
                0,
                1,
                new RtcpRunLengthChunk[] { CreateRunChunk(true, 1) }),
            CreateRunLengthBlock(
                RtcpExtendedReportBlockType.DuplicateRunLength,
                0,
                1,
                new RtcpRunLengthChunk[] { CreateRunChunk(false, 1) }),
            CreateReceiptTimesBlock(0, 1, 0, new uint[] { 1U }),
            reference,
            CreateDelayBlock(1),
            CreateStatisticsBlock(),
            CreateVoiceMetricsBlock(),
            unknown
        };

        int index = 0;
        while (index < blocks.Length)
        {
            byte[] valid = codec.EncodeRtcpPacketToArray(CreatePacket(
                new RtcpExtendedReportBlock[] { blocks[index] }));
            byte[] truncated = valid[..^4];
            SetOuterPacketLength(truncated);
            byte[] declaredLonger = (byte[])valid.Clone();
            WriteUInt16(declaredLonger, 10,
                (ushort)(ReadUInt16(declaredLonger, 10) + 1));
            byte[] declaredShorter = (byte[])valid.Clone();
            WriteUInt16(declaredShorter, 10,
                (ushort)(ReadUInt16(declaredShorter, 10) - 1));
            byte[][] malformed = new byte[][]
            {
                truncated, declaredLonger, declaredShorter
            };

            int malformedIndex = 0;
            while (malformedIndex < malformed.Length)
            {
                RtcpCodecResult result = codec.TryDecodeRtcpPacket(
                    malformed[malformedIndex]);
                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.False,
                        $"Block={index}, malformed={malformedIndex}");
                    Assert.That(result.ConsumedCount, Is.Zero);
                });
                malformedIndex = malformedIndex + 1;
            }

            index = index + 1;
        }
    }

    static RtcpExtendedReportBlock CreateRunLengthBlock(
        RtcpExtendedReportBlockType type,
        ushort begin,
        ushort end,
        RtcpRunLengthChunk[] chunks)
    {
        RtcpExtendedReportBlock block = new RtcpExtendedReportBlock();
        block.Type = type;
        block.SourceIdentifier = 0x11223344U;
        block.BeginSequenceNumber = begin;
        block.EndSequenceNumber = end;
        block.RunLengthChunks = chunks;
        return block;
    }

    static RtcpRunLengthChunk CreateRunChunk(bool positive, int length)
    {
        RtcpRunLengthChunk chunk = new RtcpRunLengthChunk();
        chunk.PositiveRun = positive;
        chunk.RunLength = length;
        return chunk;
    }

    static RtcpRunLengthChunk CreateVectorChunk(ushort bitVector)
    {
        RtcpRunLengthChunk chunk = new RtcpRunLengthChunk();
        chunk.IsBitVector = true;
        chunk.BitVector = bitVector;
        return chunk;
    }

    static RtcpExtendedReportBlock CreateReceiptTimesBlock(
        ushort begin,
        ushort end,
        byte thinning,
        uint[] receiptTimes)
    {
        RtcpExtendedReportBlock block = new RtcpExtendedReportBlock();
        block.Type = RtcpExtendedReportBlockType.PacketReceiptTimes;
        block.SourceIdentifier = 0x22334455U;
        block.BeginSequenceNumber = begin;
        block.EndSequenceNumber = end;
        block.Thinning = thinning;
        block.ReceiptTimes = receiptTimes;
        return block;
    }

    static RtcpExtendedReportBlock CreateDelayBlock(int count)
    {
        RtcpExtendedReportBlock block = new RtcpExtendedReportBlock();
        block.Type =
            RtcpExtendedReportBlockType.DelaySinceLastReceiverReport;
        block.DelayReports = new RtcpDelaySinceLastReceiverReport[count];
        int index = 0;
        while (index < count)
        {
            block.DelayReports[index] = new RtcpDelaySinceLastReceiverReport();
            index = index + 1;
        }

        return block;
    }

    static RtcpExtendedReportBlock CreateStatisticsBlock()
    {
        RtcpExtendedReportBlock block = new RtcpExtendedReportBlock();
        block.Type = RtcpExtendedReportBlockType.StatisticsSummary;
        block.SourceIdentifier = 0x33445566U;
        block.BeginSequenceNumber = 65535;
        block.EndSequenceNumber = 1;
        block.ReportsLoss = true;
        block.ReportsDuplicates = true;
        block.ReportsJitter = true;
        block.TimeToLiveOrHopLimitMode = 2;
        block.LostPackets = uint.MaxValue;
        block.DuplicatePackets = uint.MaxValue;
        block.MinimumJitter = 1U;
        block.MaximumJitter = uint.MaxValue;
        block.MeanJitter = 2U;
        block.JitterDeviation = 3U;
        block.MinimumTimeToLiveOrHopLimit = 1;
        block.MaximumTimeToLiveOrHopLimit = byte.MaxValue;
        block.MeanTimeToLiveOrHopLimit = 2;
        block.TimeToLiveOrHopLimitDeviation = 3;
        return block;
    }

    static RtcpExtendedReportBlock CreateVoiceMetricsBlock()
    {
        RtcpExtendedReportBlock block = new RtcpExtendedReportBlock();
        block.Type = RtcpExtendedReportBlockType.VoiceOverIpMetrics;
        block.SourceIdentifier = 0x44556677U;
        block.LossRate = byte.MaxValue;
        block.DiscardRate = byte.MaxValue;
        block.BurstDensity = byte.MaxValue;
        block.GapDensity = byte.MaxValue;
        block.BurstDuration = ushort.MaxValue;
        block.GapDuration = ushort.MaxValue;
        block.RoundTripDelay = ushort.MaxValue;
        block.EndSystemDelay = ushort.MaxValue;
        block.SignalLevel = byte.MaxValue;
        block.NoiseLevel = byte.MaxValue;
        block.ResidualEchoReturnLoss = byte.MaxValue;
        block.MinimumGapThreshold = 16;
        block.RFactor = 127;
        block.ExternalRFactor = 100;
        block.MeanOpinionScoreListeningQuality = 127;
        block.MeanOpinionScoreConversationalQuality = 50;
        block.ReceiverConfiguration = 0x30;
        block.JitterBufferNominalDelay = ushort.MaxValue;
        block.JitterBufferMaximumDelay = ushort.MaxValue;
        block.JitterBufferAbsoluteMaximumDelay = ushort.MaxValue;
        return block;
    }

    static RtcpPacket CreatePacket(RtcpExtendedReportBlock[] blocks)
    {
        RtcpExtendedReport report = new RtcpExtendedReport();
        report.SenderSynchronizationSource = 0x01020304U;
        report.Blocks = blocks;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.ExtendedReport;
        packet.ExtendedReport = report;
        return packet;
    }

    static void SetOuterPacketLength(byte[] packet)
    {
        WriteUInt16(packet, 2, (ushort)(packet.Length / 4 - 1));
    }

    static ushort ReadUInt16(byte[] source, int offset)
    {
        return (ushort)((source[offset] << 8) | source[offset + 1]);
    }

    static void WriteUInt16(byte[] destination, int offset, ushort value)
    {
        destination[offset] = (byte)(value >> 8);
        destination[offset + 1] = (byte)value;
    }

    static IRtcpPacketCodec CreateCodec()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.StrictValidation = true;
        configuration.PreservePadding = true;
        configuration.MaximumRtcpCompoundPacketSize =
            RtcpProtocolConstants.MaximumPacketSize;
        return new RtcpPacketCodec(configuration);
    }
}
