using System;

namespace rtp;

internal static class RtcpExtendedReportMetricsWireCodec
{
    const int ReceiverReferenceTimeDataSize = 8;
    const int DelayReportDataSize = 12;
    const int StatisticsSummaryDataSize = 36;
    const int VoiceOverIpMetricsDataSize = 32;
    const byte UnavailableQualityMetric = 127;

    internal static string? GetMetricsBlockValidationError(
        RtcpExtendedReportBlock block)
    {
        if (block.Type == RtcpExtendedReportBlockType.ReceiverReferenceTime)
        {
            return null;
        }

        if (block.Type ==
            RtcpExtendedReportBlockType.DelaySinceLastReceiverReport)
        {
            if (block.DelayReports == null)
            {
                return "The RTCP XR DLRR sub-block array is required.";
            }

            if (block.DelayReports.Length == 0)
            {
                return "An RTCP XR DLRR block must contain at least one sub-block.";
            }

            if ((long)block.DelayReports.Length * 3 > ushort.MaxValue)
            {
                return "The RTCP XR DLRR block exceeds its wire length limit.";
            }

            int delayIndex = 0;
            while (delayIndex < block.DelayReports.Length)
            {
                if (block.DelayReports[delayIndex] == null)
                {
                    return "An RTCP XR DLRR sub-block is required.";
                }

                delayIndex = delayIndex + 1;
            }

            return null;
        }

        if (block.Type == RtcpExtendedReportBlockType.StatisticsSummary)
        {
            return GetStatisticsSummaryValidationError(block);
        }

        return GetVoiceOverIpMetricsValidationError(block);
    }

    internal static string? GetStatisticsSummaryValidationError(
        RtcpExtendedReportBlock block)
    {
        if (block.TimeToLiveOrHopLimitMode > 2)
        {
            return "The RTCP XR TTL or Hop Limit mode cannot use its reserved value.";
        }

        if (RtcpExtendedReportPacketByPacketWireCodec.GetReportedPacketCount(
            block.BeginSequenceNumber,
            block.EndSequenceNumber,
            0) < 0)
        {
            return "An RTCP XR statistics-summary sequence range cannot cover 65534 or more sequence numbers.";
        }

        if (!block.ReportsLoss && block.LostPackets != 0)
        {
            return "An unreported RTCP XR loss field must be zero.";
        }

        if (!block.ReportsDuplicates && block.DuplicatePackets != 0)
        {
            return "An unreported RTCP XR duplicate field must be zero.";
        }

        if (!block.ReportsJitter &&
            (block.MinimumJitter != 0 || block.MaximumJitter != 0 ||
            block.MeanJitter != 0 || block.JitterDeviation != 0))
        {
            return "Unreported RTCP XR jitter fields must be zero.";
        }

        if (block.TimeToLiveOrHopLimitMode == 0 &&
            (block.MinimumTimeToLiveOrHopLimit != 0 ||
            block.MaximumTimeToLiveOrHopLimit != 0 ||
            block.MeanTimeToLiveOrHopLimit != 0 ||
            block.TimeToLiveOrHopLimitDeviation != 0))
        {
            return "Unreported RTCP XR TTL or Hop Limit fields must be zero.";
        }

        return null;
    }

    internal static string? GetVoiceOverIpMetricsValidationError(
        RtcpExtendedReportBlock block)
    {
        if (block.MinimumGapThreshold == 0)
        {
            return "The RTCP XR VoIP minimum gap threshold must be nonzero.";
        }

        if (!IsValidRFactor(block.RFactor) ||
            !IsValidRFactor(block.ExternalRFactor))
        {
            return "An RTCP XR VoIP R factor must be between 0 and 100 or unavailable.";
        }

        if (!IsValidMeanOpinionScore(
            block.MeanOpinionScoreListeningQuality) ||
            !IsValidMeanOpinionScore(
                block.MeanOpinionScoreConversationalQuality))
        {
            return "An RTCP XR VoIP mean opinion score must be between 10 and 50 or unavailable.";
        }

        int jitterBufferMode = (block.ReceiverConfiguration >> 4) & 3;
        if (jitterBufferMode == 1)
        {
            return "The RTCP XR VoIP receiver configuration uses a reserved jitter-buffer mode.";
        }

        if (jitterBufferMode == 2 &&
            block.JitterBufferAbsoluteMaximumDelay !=
            block.JitterBufferMaximumDelay)
        {
            return "A fixed RTCP XR jitter buffer must report equal maximum and absolute-maximum delays.";
        }

        return null;
    }

    internal static string? GetMetricsBlockDecodeError(
        byte wireType,
        ReadOnlySpan<byte> data)
    {
        if (wireType == RtcpExtendedReportBlockTypes.ReceiverReferenceTime &&
            data.Length != ReceiverReferenceTimeDataSize)
        {
            return "An RTCP XR receiver-reference-time block must have block length 2.";
        }

        if (wireType ==
            RtcpExtendedReportBlockTypes.DelaySinceLastReceiverReport &&
            (data.Length == 0 || data.Length % DelayReportDataSize != 0))
        {
            return "An RTCP XR DLRR block must contain one or more three-word sub-blocks.";
        }

        if (wireType == RtcpExtendedReportBlockTypes.StatisticsSummary &&
            data.Length != StatisticsSummaryDataSize)
        {
            return "An RTCP XR statistics-summary block must have block length 9.";
        }

        if (wireType == RtcpExtendedReportBlockTypes.StatisticsSummary &&
            RtcpExtendedReportPacketByPacketWireCodec.GetReportedPacketCount(
                NetworkBinaryReader.ReadUInt16(data, 4),
                NetworkBinaryReader.ReadUInt16(data, 6),
                0) < 0)
        {
            return "An RTCP XR statistics-summary sequence range cannot cover 65534 or more sequence numbers.";
        }

        if (wireType == RtcpExtendedReportBlockTypes.VoiceOverIpMetrics &&
            data.Length != VoiceOverIpMetricsDataSize)
        {
            return "An RTCP XR VoIP metrics block must have block length 8.";
        }

        if (wireType == RtcpExtendedReportBlockTypes.VoiceOverIpMetrics)
        {
            if (NetworkBinaryReader.ReadByte(data, 19) == 0)
            {
                return "The RTCP XR VoIP minimum gap threshold must be nonzero.";
            }

            int jitterBufferMode =
                (NetworkBinaryReader.ReadByte(data, 24) >> 4) & 3;
            if (jitterBufferMode == 1)
            {
                return "The RTCP XR VoIP receiver configuration uses a reserved jitter-buffer mode.";
            }

            if (jitterBufferMode == 2 &&
                NetworkBinaryReader.ReadUInt16(data, 30) !=
                NetworkBinaryReader.ReadUInt16(data, 28))
            {
                return "A fixed RTCP XR jitter buffer must report equal maximum and absolute-maximum delays.";
            }
        }

        return null;
    }

    internal static bool ShouldIgnoreStatisticsSummary(
        byte typeSpecific,
        ReadOnlySpan<byte> data)
    {
        int timeToLiveMode = (typeSpecific >> 3) & 3;
        if (timeToLiveMode == 3)
        {
            return true;
        }

        if ((typeSpecific & 128) == 0 &&
            NetworkBinaryReader.ReadUInt32(data, 8) != 0)
        {
            return true;
        }

        if ((typeSpecific & 64) == 0 &&
            NetworkBinaryReader.ReadUInt32(data, 12) != 0)
        {
            return true;
        }

        if ((typeSpecific & 32) == 0 &&
            (NetworkBinaryReader.ReadUInt32(data, 16) != 0 ||
            NetworkBinaryReader.ReadUInt32(data, 20) != 0 ||
            NetworkBinaryReader.ReadUInt32(data, 24) != 0 ||
            NetworkBinaryReader.ReadUInt32(data, 28) != 0))
        {
            return true;
        }

        if (timeToLiveMode == 0 &&
            (NetworkBinaryReader.ReadByte(data, 32) != 0 ||
            NetworkBinaryReader.ReadByte(data, 33) != 0 ||
            NetworkBinaryReader.ReadByte(data, 34) != 0 ||
            NetworkBinaryReader.ReadByte(data, 35) != 0))
        {
            return true;
        }

        return false;
    }

    internal static int CalculateMetricsBlockDataSize(
        RtcpExtendedReportBlock block)
    {
        if (block.Type == RtcpExtendedReportBlockType.ReceiverReferenceTime)
        {
            return ReceiverReferenceTimeDataSize;
        }

        if (block.Type ==
            RtcpExtendedReportBlockType.DelaySinceLastReceiverReport)
        {
            return block.DelayReports.Length * DelayReportDataSize;
        }

        if (block.Type == RtcpExtendedReportBlockType.StatisticsSummary)
        {
            return StatisticsSummaryDataSize;
        }

        return VoiceOverIpMetricsDataSize;
    }

    internal static void DecodeMetricsBlock(
        RtcpExtendedReportBlock block,
        ReadOnlySpan<byte> data)
    {
        if (block.Type == RtcpExtendedReportBlockType.ReceiverReferenceTime)
        {
            block.ReceiverReferenceTime = NetworkBinaryReader.ReadUInt64(data, 0);
            return;
        }

        if (block.Type ==
            RtcpExtendedReportBlockType.DelaySinceLastReceiverReport)
        {
            DecodeDelayReports(block, data);
            return;
        }

        if (block.Type == RtcpExtendedReportBlockType.StatisticsSummary)
        {
            DecodeStatisticsSummary(block, data);
            return;
        }

        DecodeVoiceOverIpMetrics(block, data);
    }

    internal static void DecodeDelayReports(
        RtcpExtendedReportBlock block,
        ReadOnlySpan<byte> data)
    {
        int delayReportCount = data.Length / DelayReportDataSize;
        block.DelayReports =
            new RtcpDelaySinceLastReceiverReport[delayReportCount];
        int delayIndex = 0;
        while (delayIndex < delayReportCount)
        {
            int offset = delayIndex * DelayReportDataSize;
            RtcpDelaySinceLastReceiverReport delay =
                new RtcpDelaySinceLastReceiverReport();
            delay.ReceiverSourceIdentifier = NetworkBinaryReader.ReadUInt32(
                data,
                offset);
            delay.LastReceiverReport = NetworkBinaryReader.ReadUInt32(
                data,
                offset + 4);
            delay.DelaySinceLastReceiverReport = NetworkBinaryReader.ReadUInt32(
                data,
                offset + 8);
            block.DelayReports[delayIndex] = delay;
            delayIndex = delayIndex + 1;
        }
    }

    internal static void DecodeStatisticsSummary(
        RtcpExtendedReportBlock block,
        ReadOnlySpan<byte> data)
    {
        block.ReportsLoss = (block.TypeSpecific & 128) != 0;
        block.ReportsDuplicates = (block.TypeSpecific & 64) != 0;
        block.ReportsJitter = (block.TypeSpecific & 32) != 0;
        block.TimeToLiveOrHopLimitMode =
            (byte)((block.TypeSpecific >> 3) & 3);
        block.SourceIdentifier = NetworkBinaryReader.ReadUInt32(data, 0);
        block.BeginSequenceNumber = NetworkBinaryReader.ReadUInt16(data, 4);
        block.EndSequenceNumber = NetworkBinaryReader.ReadUInt16(data, 6);
        if (block.ReportsLoss)
        {
            block.LostPackets = NetworkBinaryReader.ReadUInt32(data, 8);
        }

        if (block.ReportsDuplicates)
        {
            block.DuplicatePackets = NetworkBinaryReader.ReadUInt32(data, 12);
        }

        if (block.ReportsJitter)
        {
            block.MinimumJitter = NetworkBinaryReader.ReadUInt32(data, 16);
            block.MaximumJitter = NetworkBinaryReader.ReadUInt32(data, 20);
            block.MeanJitter = NetworkBinaryReader.ReadUInt32(data, 24);
            block.JitterDeviation = NetworkBinaryReader.ReadUInt32(data, 28);
        }

        if (block.TimeToLiveOrHopLimitMode != 0)
        {
            block.MinimumTimeToLiveOrHopLimit =
                NetworkBinaryReader.ReadByte(data, 32);
            block.MaximumTimeToLiveOrHopLimit =
                NetworkBinaryReader.ReadByte(data, 33);
            block.MeanTimeToLiveOrHopLimit =
                NetworkBinaryReader.ReadByte(data, 34);
            block.TimeToLiveOrHopLimitDeviation =
                NetworkBinaryReader.ReadByte(data, 35);
        }
    }

    internal static void DecodeVoiceOverIpMetrics(
        RtcpExtendedReportBlock block,
        ReadOnlySpan<byte> data)
    {
        block.SourceIdentifier = NetworkBinaryReader.ReadUInt32(data, 0);
        block.LossRate = NetworkBinaryReader.ReadByte(data, 4);
        block.DiscardRate = NetworkBinaryReader.ReadByte(data, 5);
        block.BurstDensity = NetworkBinaryReader.ReadByte(data, 6);
        block.GapDensity = NetworkBinaryReader.ReadByte(data, 7);
        block.BurstDuration = NetworkBinaryReader.ReadUInt16(data, 8);
        block.GapDuration = NetworkBinaryReader.ReadUInt16(data, 10);
        block.RoundTripDelay = NetworkBinaryReader.ReadUInt16(data, 12);
        block.EndSystemDelay = NetworkBinaryReader.ReadUInt16(data, 14);
        block.SignalLevel = NetworkBinaryReader.ReadByte(data, 16);
        block.NoiseLevel = NetworkBinaryReader.ReadByte(data, 17);
        block.ResidualEchoReturnLoss = NetworkBinaryReader.ReadByte(data, 18);
        block.MinimumGapThreshold = NetworkBinaryReader.ReadByte(data, 19);
        block.RFactor = GetReceivedRFactor(
            NetworkBinaryReader.ReadByte(data, 20));
        block.ExternalRFactor = GetReceivedRFactor(
            NetworkBinaryReader.ReadByte(data, 21));
        block.MeanOpinionScoreListeningQuality = GetReceivedMeanOpinionScore(
            NetworkBinaryReader.ReadByte(data, 22));
        block.MeanOpinionScoreConversationalQuality =
            GetReceivedMeanOpinionScore(
                NetworkBinaryReader.ReadByte(data, 23));
        block.ReceiverConfiguration = NetworkBinaryReader.ReadByte(data, 24);
        block.JitterBufferNominalDelay = NetworkBinaryReader.ReadUInt16(data, 26);
        block.JitterBufferMaximumDelay = NetworkBinaryReader.ReadUInt16(data, 28);
        block.JitterBufferAbsoluteMaximumDelay =
            NetworkBinaryReader.ReadUInt16(data, 30);
    }

    internal static byte GetMetricsBlockTypeSpecific(
        RtcpExtendedReportBlock block)
    {
        if (block.Type != RtcpExtendedReportBlockType.StatisticsSummary)
        {
            return 0;
        }

        byte typeSpecific = 0;
        if (block.ReportsLoss)
        {
            typeSpecific = (byte)(typeSpecific | 128);
        }

        if (block.ReportsDuplicates)
        {
            typeSpecific = (byte)(typeSpecific | 64);
        }

        if (block.ReportsJitter)
        {
            typeSpecific = (byte)(typeSpecific | 32);
        }

        return (byte)(typeSpecific | block.TimeToLiveOrHopLimitMode << 3);
    }

    internal static void EncodeMetricsBlockData(
        RtcpExtendedReportBlock block,
        Span<byte> destination)
    {
        if (block.Type == RtcpExtendedReportBlockType.ReceiverReferenceTime)
        {
            NetworkBinaryWriter.WriteUInt64(
                destination,
                0,
                block.ReceiverReferenceTime);
            return;
        }

        if (block.Type ==
            RtcpExtendedReportBlockType.DelaySinceLastReceiverReport)
        {
            EncodeDelayReports(block, destination);
            return;
        }

        if (block.Type == RtcpExtendedReportBlockType.StatisticsSummary)
        {
            EncodeStatisticsSummary(block, destination);
            return;
        }

        EncodeVoiceOverIpMetrics(block, destination);
    }

    internal static void EncodeDelayReports(
        RtcpExtendedReportBlock block,
        Span<byte> destination)
    {
        int delayIndex = 0;
        while (delayIndex < block.DelayReports.Length)
        {
            int offset = delayIndex * DelayReportDataSize;
            RtcpDelaySinceLastReceiverReport delay =
                block.DelayReports[delayIndex];
            NetworkBinaryWriter.WriteUInt32(
                destination,
                offset,
                delay.ReceiverSourceIdentifier);
            NetworkBinaryWriter.WriteUInt32(
                destination,
                offset + 4,
                delay.LastReceiverReport);
            NetworkBinaryWriter.WriteUInt32(
                destination,
                offset + 8,
                delay.DelaySinceLastReceiverReport);
            delayIndex = delayIndex + 1;
        }
    }

    internal static void EncodeStatisticsSummary(
        RtcpExtendedReportBlock block,
        Span<byte> destination)
    {
        NetworkBinaryWriter.WriteUInt32(destination, 0, block.SourceIdentifier);
        NetworkBinaryWriter.WriteUInt16(
            destination,
            4,
            block.BeginSequenceNumber);
        NetworkBinaryWriter.WriteUInt16(
            destination,
            6,
            block.EndSequenceNumber);
        NetworkBinaryWriter.WriteUInt32(destination, 8, block.LostPackets);
        NetworkBinaryWriter.WriteUInt32(destination, 12, block.DuplicatePackets);
        NetworkBinaryWriter.WriteUInt32(destination, 16, block.MinimumJitter);
        NetworkBinaryWriter.WriteUInt32(destination, 20, block.MaximumJitter);
        NetworkBinaryWriter.WriteUInt32(destination, 24, block.MeanJitter);
        NetworkBinaryWriter.WriteUInt32(destination, 28, block.JitterDeviation);
        NetworkBinaryWriter.WriteByte(
            destination,
            32,
            block.MinimumTimeToLiveOrHopLimit);
        NetworkBinaryWriter.WriteByte(
            destination,
            33,
            block.MaximumTimeToLiveOrHopLimit);
        NetworkBinaryWriter.WriteByte(
            destination,
            34,
            block.MeanTimeToLiveOrHopLimit);
        NetworkBinaryWriter.WriteByte(
            destination,
            35,
            block.TimeToLiveOrHopLimitDeviation);
    }

    internal static void EncodeVoiceOverIpMetrics(
        RtcpExtendedReportBlock block,
        Span<byte> destination)
    {
        NetworkBinaryWriter.WriteUInt32(destination, 0, block.SourceIdentifier);
        NetworkBinaryWriter.WriteByte(destination, 4, block.LossRate);
        NetworkBinaryWriter.WriteByte(destination, 5, block.DiscardRate);
        NetworkBinaryWriter.WriteByte(destination, 6, block.BurstDensity);
        NetworkBinaryWriter.WriteByte(destination, 7, block.GapDensity);
        NetworkBinaryWriter.WriteUInt16(destination, 8, block.BurstDuration);
        NetworkBinaryWriter.WriteUInt16(destination, 10, block.GapDuration);
        NetworkBinaryWriter.WriteUInt16(destination, 12, block.RoundTripDelay);
        NetworkBinaryWriter.WriteUInt16(destination, 14, block.EndSystemDelay);
        NetworkBinaryWriter.WriteByte(destination, 16, block.SignalLevel);
        NetworkBinaryWriter.WriteByte(destination, 17, block.NoiseLevel);
        NetworkBinaryWriter.WriteByte(
            destination,
            18,
            block.ResidualEchoReturnLoss);
        NetworkBinaryWriter.WriteByte(
            destination,
            19,
            block.MinimumGapThreshold);
        NetworkBinaryWriter.WriteByte(destination, 20, block.RFactor);
        NetworkBinaryWriter.WriteByte(destination, 21, block.ExternalRFactor);
        NetworkBinaryWriter.WriteByte(
            destination,
            22,
            block.MeanOpinionScoreListeningQuality);
        NetworkBinaryWriter.WriteByte(
            destination,
            23,
            block.MeanOpinionScoreConversationalQuality);
        NetworkBinaryWriter.WriteByte(
            destination,
            24,
            block.ReceiverConfiguration);
        NetworkBinaryWriter.WriteByte(destination, 25, 0);
        NetworkBinaryWriter.WriteUInt16(
            destination,
            26,
            block.JitterBufferNominalDelay);
        NetworkBinaryWriter.WriteUInt16(
            destination,
            28,
            block.JitterBufferMaximumDelay);
        NetworkBinaryWriter.WriteUInt16(
            destination,
            30,
            block.JitterBufferAbsoluteMaximumDelay);
    }

    internal static bool IsValidRFactor(byte value)
    {
        return value <= 100 || value == UnavailableQualityMetric;
    }

    internal static bool IsValidMeanOpinionScore(byte value)
    {
        return (value >= 10 && value <= 50) ||
            value == UnavailableQualityMetric;
    }

    internal static byte GetReceivedRFactor(byte value)
    {
        if (IsValidRFactor(value))
        {
            return value;
        }

        return UnavailableQualityMetric;
    }

    internal static byte GetReceivedMeanOpinionScore(byte value)
    {
        if (IsValidMeanOpinionScore(value))
        {
            return value;
        }

        return UnavailableQualityMetric;
    }
}
