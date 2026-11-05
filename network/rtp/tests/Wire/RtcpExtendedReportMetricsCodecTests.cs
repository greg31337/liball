using rtp;

namespace rtp.Tests.Wire;

public class RtcpExtendedReportMetricsCodecTests
{
    [Test]
    public void ReceiverReferenceTimeAndDlrrBlocksRoundTrip()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x80, 207, 0, 11,
            1, 2, 3, 4,
            4, 0, 0, 2,
            0x01, 0x23, 0x45, 0x67,
            0x89, 0xAB, 0xCD, 0xEF,
            5, 0, 0, 6,
            0x11, 0x11, 0x11, 0x11,
            0x22, 0x33, 0x44, 0x55,
            0x66, 0x77, 0x88, 0x99,
            0xAA, 0xBB, 0xCC, 0xDD,
            1, 2, 3, 4,
            5, 6, 7, 8
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        RtcpExtendedReportBlock reference = packet.ExtendedReport!.Blocks[0];
        RtcpExtendedReportBlock delay = packet.ExtendedReport.Blocks[1];

        Assert.Multiple(() =>
        {
            Assert.That(reference.Type,
                Is.EqualTo(RtcpExtendedReportBlockType.ReceiverReferenceTime));
            Assert.That(reference.ReceiverReferenceTime,
                Is.EqualTo(0x0123456789ABCDEFUL));
            Assert.That(delay.Type,
                Is.EqualTo(
                    RtcpExtendedReportBlockType.DelaySinceLastReceiverReport));
            Assert.That(delay.DelayReports.Length, Is.EqualTo(2));
            Assert.That(delay.DelayReports[0].ReceiverSourceIdentifier,
                Is.EqualTo(0x11111111U));
            Assert.That(delay.DelayReports[0].LastReceiverReport,
                Is.EqualTo(0x22334455U));
            Assert.That(delay.DelayReports[0].DelaySinceLastReceiverReport,
                Is.EqualTo(0x66778899U));
            Assert.That(delay.DelayReports[1].ReceiverSourceIdentifier,
                Is.EqualTo(0xAABBCCDDU));
            Assert.That(delay.DelayReports[1].LastReceiverReport,
                Is.EqualTo(0x01020304U));
            Assert.That(delay.DelayReports[1].DelaySinceLastReceiverReport,
                Is.EqualTo(0x05060708U));
            Assert.That(codec.EncodeRtcpPacketToArray(packet), Is.EqualTo(source));
        });
    }

    [Test]
    public void StatisticsSummaryRoundTripsFlagsRangeAndMetrics()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x80, 207, 0, 11,
            1, 2, 3, 4,
            6, 0xF0, 0, 9,
            0x11, 0x22, 0x33, 0x44,
            0xFF, 0xFE, 0, 2,
            0, 0, 0, 1,
            0, 0, 0, 2,
            0, 0, 0, 3,
            0, 0, 0, 4,
            0, 0, 0, 5,
            0, 0, 0, 6,
            7, 8, 9, 10
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        RtcpExtendedReportBlock block = packet.ExtendedReport!.Blocks[0];

        Assert.Multiple(() =>
        {
            Assert.That(block.Type,
                Is.EqualTo(RtcpExtendedReportBlockType.StatisticsSummary));
            Assert.That(block.ReportsLoss, Is.True);
            Assert.That(block.ReportsDuplicates, Is.True);
            Assert.That(block.ReportsJitter, Is.True);
            Assert.That(block.TimeToLiveOrHopLimitMode, Is.EqualTo(2));
            Assert.That(block.SourceIdentifier, Is.EqualTo(0x11223344U));
            Assert.That(block.BeginSequenceNumber, Is.EqualTo(65534));
            Assert.That(block.EndSequenceNumber, Is.EqualTo(2));
            Assert.That(block.LostPackets, Is.EqualTo(1U));
            Assert.That(block.DuplicatePackets, Is.EqualTo(2U));
            Assert.That(block.MinimumJitter, Is.EqualTo(3U));
            Assert.That(block.MaximumJitter, Is.EqualTo(4U));
            Assert.That(block.MeanJitter, Is.EqualTo(5U));
            Assert.That(block.JitterDeviation, Is.EqualTo(6U));
            Assert.That(block.MinimumTimeToLiveOrHopLimit, Is.EqualTo(7));
            Assert.That(block.MaximumTimeToLiveOrHopLimit, Is.EqualTo(8));
            Assert.That(block.MeanTimeToLiveOrHopLimit, Is.EqualTo(9));
            Assert.That(block.TimeToLiveOrHopLimitDeviation, Is.EqualTo(10));
            Assert.That(codec.EncodeRtcpPacketToArray(packet), Is.EqualTo(source));
        });
    }

    [Test]
    public void InvalidUnreportedStatisticsAreIgnoredAndPreservedRaw()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x80, 207, 0, 11,
            1, 2, 3, 4,
            6, 0, 0, 9,
            0, 0, 0, 1,
            0, 10, 0, 11,
            0, 0, 0, 1,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        RtcpExtendedReportBlock block = packet.ExtendedReport!.Blocks[0];

        Assert.Multiple(() =>
        {
            Assert.That(block.Type,
                Is.EqualTo(RtcpExtendedReportBlockType.Unknown));
            Assert.That(block.WireType, Is.EqualTo(6));
            Assert.That(block.Data.Length, Is.EqualTo(36));
            Assert.That(codec.EncodeRtcpPacketToArray(packet), Is.EqualTo(source));
        });
    }

    [Test]
    public void VoiceOverIpMetricsRoundTripAllFields()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x80, 207, 0, 10,
            1, 2, 3, 4,
            7, 0, 0, 8,
            0x11, 0x22, 0x33, 0x44,
            1, 2, 3, 4,
            0x05, 0x06, 0x07, 0x08,
            0x09, 0x0A, 0x0B, 0x0C,
            0xED, 0xBA, 42, 16,
            94, 127, 35, 40,
            0xB5, 0, 0, 100,
            0, 200, 1, 44
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        RtcpExtendedReportBlock block = packet.ExtendedReport!.Blocks[0];

        Assert.Multiple(() =>
        {
            Assert.That(block.Type,
                Is.EqualTo(RtcpExtendedReportBlockType.VoiceOverIpMetrics));
            Assert.That(block.SourceIdentifier, Is.EqualTo(0x11223344U));
            Assert.That(block.LossRate, Is.EqualTo(1));
            Assert.That(block.DiscardRate, Is.EqualTo(2));
            Assert.That(block.BurstDensity, Is.EqualTo(3));
            Assert.That(block.GapDensity, Is.EqualTo(4));
            Assert.That(block.BurstDuration, Is.EqualTo(0x0506));
            Assert.That(block.GapDuration, Is.EqualTo(0x0708));
            Assert.That(block.RoundTripDelay, Is.EqualTo(0x090A));
            Assert.That(block.EndSystemDelay, Is.EqualTo(0x0B0C));
            Assert.That(block.SignalLevel, Is.EqualTo(0xED));
            Assert.That(block.NoiseLevel, Is.EqualTo(0xBA));
            Assert.That(block.ResidualEchoReturnLoss, Is.EqualTo(42));
            Assert.That(block.MinimumGapThreshold, Is.EqualTo(16));
            Assert.That(block.RFactor, Is.EqualTo(94));
            Assert.That(block.ExternalRFactor, Is.EqualTo(127));
            Assert.That(block.MeanOpinionScoreListeningQuality, Is.EqualTo(35));
            Assert.That(block.MeanOpinionScoreConversationalQuality,
                Is.EqualTo(40));
            Assert.That(block.ReceiverConfiguration, Is.EqualTo(0xB5));
            Assert.That(block.JitterBufferNominalDelay, Is.EqualTo(100));
            Assert.That(block.JitterBufferMaximumDelay, Is.EqualTo(200));
            Assert.That(block.JitterBufferAbsoluteMaximumDelay, Is.EqualTo(300));
            Assert.That(codec.EncodeRtcpPacketToArray(packet), Is.EqualTo(source));
        });
    }

    [Test]
    public void DecoderCanonicalizesReservedAndInvalidVoipValues()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x80, 207, 0, 10,
            1, 2, 3, 4,
            7, 0xA5, 0, 8,
            0, 0, 0, 1,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            127, 127, 127, 16,
            101, 126, 9, 51,
            0, 0xFE, 0, 0,
            0, 0, 0, 0
        };
        byte[] canonical = (byte[])source.Clone();
        canonical[9] = 0;
        canonical[32] = 127;
        canonical[33] = 127;
        canonical[34] = 127;
        canonical[35] = 127;
        canonical[37] = 0;

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        RtcpExtendedReportBlock block = packet.ExtendedReport!.Blocks[0];

        Assert.Multiple(() =>
        {
            Assert.That(block.RFactor, Is.EqualTo(127));
            Assert.That(block.ExternalRFactor, Is.EqualTo(127));
            Assert.That(block.MeanOpinionScoreListeningQuality, Is.EqualTo(127));
            Assert.That(block.MeanOpinionScoreConversationalQuality,
                Is.EqualTo(127));
            Assert.That(codec.EncodeRtcpPacketToArray(packet),
                Is.EqualTo(canonical));
        });
    }

    [Test]
    public void DecoderRejectsWrongMetricBlockLengthsAndInvalidVoipConfiguration()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] zeroGapThreshold = new byte[32];
        byte[] reservedJitterBufferMode = CreateValidVoiceData();
        reservedJitterBufferMode[24] = 0x10;
        byte[] fixedJitterBufferMismatch = CreateValidVoiceData();
        fixedJitterBufferMismatch[24] = 0x20;
        fixedJitterBufferMismatch[30] = 0;
        fixedJitterBufferMismatch[31] = 2;
        byte[] excessiveStatisticsRange = new byte[36];
        excessiveStatisticsRange[6] = 0xFF;
        excessiveStatisticsRange[7] = 0xFE;
        byte[][] malformedPackets = new byte[][]
        {
            CreateSingleBlockPacket(4, 1, new byte[4]),
            CreateSingleBlockPacket(4, 3, new byte[12]),
            CreateSingleBlockPacket(5, 0, new byte[0]),
            CreateSingleBlockPacket(5, 2, new byte[8]),
            CreateSingleBlockPacket(6, 8, new byte[32]),
            CreateSingleBlockPacket(6, 10, new byte[40]),
            CreateSingleBlockPacket(6, 9, excessiveStatisticsRange),
            CreateSingleBlockPacket(7, 7, new byte[28]),
            CreateSingleBlockPacket(7, 9, new byte[36]),
            CreateSingleBlockPacket(7, 8, zeroGapThreshold),
            CreateSingleBlockPacket(7, 8, reservedJitterBufferMode),
            CreateSingleBlockPacket(7, 8, fixedJitterBufferMismatch)
        };

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < malformedPackets.Length)
            {
                Assert.That(codec.TryDecodeRtcpPacket(malformedPackets[index]).Success,
                    Is.False,
                    $"Malformed vector {index} was accepted.");
                index = index + 1;
            }
        });
    }

    [Test]
    public void EncoderRejectsInvalidMetricModelsAtomically()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpExtendedReportBlock block = CreateValidStatisticsSummary();
        RtcpPacket packet = CreatePacket(block);
        byte[] destination = new byte[64];
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = 77;
            index = index + 1;
        }

        block.TimeToLiveOrHopLimitMode = 3;
        AssertEncodingFails(codec, packet, destination);
        block.TimeToLiveOrHopLimitMode = 0;

        block.ReportsLoss = false;
        AssertEncodingFails(codec, packet, destination);
        block.ReportsLoss = true;

        block.EndSequenceNumber = 65534;
        block.BeginSequenceNumber = 0;
        AssertEncodingFails(codec, packet, destination);

        block = new RtcpExtendedReportBlock();
        block.Type =
            RtcpExtendedReportBlockType.DelaySinceLastReceiverReport;
        packet = CreatePacket(block);
        AssertEncodingFails(codec, packet, destination);
        block.DelayReports =
            new RtcpDelaySinceLastReceiverReport[] { null! };
        AssertEncodingFails(codec, packet, destination);

        block = CreateValidVoiceMetrics();
        packet = CreatePacket(block);
        block.MinimumGapThreshold = 0;
        AssertEncodingFails(codec, packet, destination);
        block.MinimumGapThreshold = 16;
        block.RFactor = 101;
        AssertEncodingFails(codec, packet, destination);
        block.RFactor = 94;
        block.MeanOpinionScoreListeningQuality = 9;
        AssertEncodingFails(codec, packet, destination);
        block.MeanOpinionScoreListeningQuality = 35;
        block.ReceiverConfiguration = 0x10;
        AssertEncodingFails(codec, packet, destination);
        block.ReceiverConfiguration = 0x20;
        block.JitterBufferMaximumDelay = 20;
        block.JitterBufferAbsoluteMaximumDelay = 21;
        AssertEncodingFails(codec, packet, destination);

        Assert.That(destination, Has.All.EqualTo(77));
    }

    [Test]
    public void StatisticsSummaryRejectsEveryUnreportedMetricFamily()
    {
        RtcpExtendedReportBlock block = CreateValidStatisticsSummary();
        block.ReportsDuplicates = true;
        block.DuplicatePackets = 1U;
        block.ReportsJitter = true;
        block.MinimumJitter = 1U;
        block.MaximumJitter = 2U;
        block.MeanJitter = 1U;
        block.JitterDeviation = 1U;
        block.TimeToLiveOrHopLimitMode = 2;
        block.MinimumTimeToLiveOrHopLimit = 1;
        block.MaximumTimeToLiveOrHopLimit = 2;
        block.MeanTimeToLiveOrHopLimit = 1;
        block.TimeToLiveOrHopLimitDeviation = 1;

        block.ReportsLoss = false;
        Assert.That(
            RtcpExtendedReportMetricsWireCodec.GetStatisticsSummaryValidationError(block),
            Is.Not.Null);
        block.ReportsLoss = true;

        block.ReportsDuplicates = false;
        Assert.That(
            RtcpExtendedReportMetricsWireCodec.GetStatisticsSummaryValidationError(block),
            Is.Not.Null);
        block.ReportsDuplicates = true;

        block.ReportsJitter = false;
        Assert.That(
            RtcpExtendedReportMetricsWireCodec.GetStatisticsSummaryValidationError(block),
            Is.Not.Null);
        block.MinimumJitter = 0U;
        Assert.That(
            RtcpExtendedReportMetricsWireCodec.GetStatisticsSummaryValidationError(block),
            Is.Not.Null);
        block.MaximumJitter = 0U;
        Assert.That(
            RtcpExtendedReportMetricsWireCodec.GetStatisticsSummaryValidationError(block),
            Is.Not.Null);
        block.MeanJitter = 0U;
        Assert.That(
            RtcpExtendedReportMetricsWireCodec.GetStatisticsSummaryValidationError(block),
            Is.Not.Null);
        block.ReportsJitter = true;

        block.TimeToLiveOrHopLimitMode = 0;
        Assert.That(
            RtcpExtendedReportMetricsWireCodec.GetStatisticsSummaryValidationError(block),
            Is.Not.Null);
        block.MinimumTimeToLiveOrHopLimit = 0;
        Assert.That(
            RtcpExtendedReportMetricsWireCodec.GetStatisticsSummaryValidationError(block),
            Is.Not.Null);
        block.MaximumTimeToLiveOrHopLimit = 0;
        Assert.That(
            RtcpExtendedReportMetricsWireCodec.GetStatisticsSummaryValidationError(block),
            Is.Not.Null);
        block.MeanTimeToLiveOrHopLimit = 0;
        Assert.That(
            RtcpExtendedReportMetricsWireCodec.GetStatisticsSummaryValidationError(block),
            Is.Not.Null);
    }

    [Test]
    public void DelayReportValidationRejectsNullEmptyOversizedAndNullEntries()
    {
        RtcpExtendedReportBlock block = new RtcpExtendedReportBlock();
        block.Type = RtcpExtendedReportBlockType.DelaySinceLastReceiverReport;
        block.DelayReports = null!;
        Assert.That(
            RtcpExtendedReportMetricsWireCodec.GetMetricsBlockValidationError(block),
            Is.Not.Null);

        block.DelayReports = [];
        Assert.That(
            RtcpExtendedReportMetricsWireCodec.GetMetricsBlockValidationError(block),
            Is.Not.Null);

        block.DelayReports = new RtcpDelaySinceLastReceiverReport[21846];
        Assert.That(
            RtcpExtendedReportMetricsWireCodec.GetMetricsBlockValidationError(block),
            Is.Not.Null);

        block.DelayReports = new RtcpDelaySinceLastReceiverReport[] { null! };
        Assert.That(
            RtcpExtendedReportMetricsWireCodec.GetMetricsBlockValidationError(block),
            Is.Not.Null);
    }

    [Test]
    public void StatisticsSummaryDecoderIgnoresEveryContradictoryFlagAndField()
    {
        byte[] data = new byte[36];
        Assert.That(
            RtcpExtendedReportMetricsWireCodec.ShouldIgnoreStatisticsSummary(0, data),
            Is.False);
        Assert.That(
            RtcpExtendedReportMetricsWireCodec.ShouldIgnoreStatisticsSummary(24, data),
            Is.True);

        int[] contradictoryOffsets = new int[]
        {
            11,
            15,
            19,
            23,
            27,
            31,
            32,
            33,
            34,
            35
        };
        int offsetIndex = 0;
        while (offsetIndex < contradictoryOffsets.Length)
        {
            data = new byte[36];
            data[contradictoryOffsets[offsetIndex]] = 1;
            Assert.That(
                RtcpExtendedReportMetricsWireCodec.ShouldIgnoreStatisticsSummary(0, data),
                Is.True,
                $"Contradictory statistics-summary byte {contradictoryOffsets[offsetIndex]} was accepted.");
            offsetIndex = offsetIndex + 1;
        }
    }

    static byte[] CreateValidVoiceData()
    {
        byte[] data = new byte[32];
        data[19] = 16;
        data[20] = 94;
        data[21] = 94;
        data[22] = 35;
        data[23] = 35;
        return data;
    }

    static RtcpExtendedReportBlock CreateValidStatisticsSummary()
    {
        RtcpExtendedReportBlock block = new RtcpExtendedReportBlock();
        block.Type = RtcpExtendedReportBlockType.StatisticsSummary;
        block.SourceIdentifier = 1;
        block.BeginSequenceNumber = 10;
        block.EndSequenceNumber = 11;
        block.ReportsLoss = true;
        block.LostPackets = 1;
        return block;
    }

    static RtcpExtendedReportBlock CreateValidVoiceMetrics()
    {
        RtcpExtendedReportBlock block = new RtcpExtendedReportBlock();
        block.Type = RtcpExtendedReportBlockType.VoiceOverIpMetrics;
        block.MinimumGapThreshold = 16;
        block.RFactor = 94;
        block.ExternalRFactor = 94;
        block.MeanOpinionScoreListeningQuality = 35;
        block.MeanOpinionScoreConversationalQuality = 35;
        return block;
    }

    static byte[] CreateSingleBlockPacket(
        byte blockType,
        ushort blockLength,
        byte[] data)
    {
        int packetSize = 12 + data.Length;
        byte[] packet = new byte[packetSize];
        packet[0] = 0x80;
        packet[1] = 207;
        packet[2] = (byte)((packetSize / 4 - 1) >> 8);
        packet[3] = (byte)(packetSize / 4 - 1);
        packet[7] = 9;
        packet[8] = blockType;
        packet[10] = (byte)(blockLength >> 8);
        packet[11] = (byte)blockLength;
        data.CopyTo(packet, 12);
        return packet;
    }

    static RtcpPacket CreatePacket(RtcpExtendedReportBlock block)
    {
        RtcpExtendedReport report = new RtcpExtendedReport();
        report.SenderSynchronizationSource = 0x01020304U;
        report.Blocks = new RtcpExtendedReportBlock[] { block };
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.ExtendedReport;
        packet.ExtendedReport = report;
        return packet;
    }

    static void AssertEncodingFails(
        IRtcpPacketCodec codec,
        RtcpPacket packet,
        byte[] destination)
    {
        RtcpCodecResult result = codec.TryEncodeRtcpPacket(packet, destination);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.WrittenCount, Is.Zero);
            Assert.That(destination, Has.All.EqualTo(77));
        });
    }

    static IRtcpPacketCodec CreateCodec()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.StrictValidation = true;
        configuration.PreservePadding = true;
        configuration.MaximumRtcpCompoundPacketSize = 65535;
        return new RtcpPacketCodec(configuration);
    }
}
