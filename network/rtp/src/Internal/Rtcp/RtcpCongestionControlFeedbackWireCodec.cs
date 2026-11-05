using System;

namespace rtp;

internal static class RtcpCongestionControlFeedbackWireCodec
{
    const int ReportHeaderSize = 8;
    const int MetricSize = 2;
    const int TimestampSize = 4;
    const int MaximumReportCount = 16384;
    const byte EcnMaximum = 3;
    const ushort ArrivalTimeOffsetMaximum = 8191;

    internal static string? GetCongestionControlFeedbackValidationError(
        RtcpCongestionControlFeedback? feedback,
        uint mediaSourceIdentifier)
    {
        if (mediaSourceIdentifier != 0)
        {
            return "RFC 8888 congestion feedback does not use a common media SSRC.";
        }

        if (feedback == null)
        {
            return "The congestion-control feedback body is required.";
        }

        if (feedback.Reports == null)
        {
            return "The congestion-control report array is required.";
        }

        int reportIndex = 0;
        while (reportIndex < feedback.Reports.Length)
        {
            RtcpCongestionControlReport? report = feedback.Reports[reportIndex];
            if (report == null)
            {
                return "A congestion-control report block is required.";
            }

            string? reportError = GetCongestionControlReportValidationError(report);
            if (reportError != null)
            {
                return reportError;
            }

            reportIndex = reportIndex + 1;
        }

        return null;
    }

    internal static string? GetCongestionControlReportValidationError(
        RtcpCongestionControlReport report)
    {
        if (report.ReportCount < 0 || report.ReportCount > MaximumReportCount)
        {
            return "A congestion-control report count must be between 0 and 16384.";
        }

        if (report.Received == null || report.EcnMarks == null ||
            report.ArrivalTimeOffsets == null)
        {
            return "Congestion-control metric arrays are required.";
        }

        if (report.Received.Length != report.ReportCount ||
            report.EcnMarks.Length != report.ReportCount ||
            report.ArrivalTimeOffsets.Length != report.ReportCount)
        {
            return "Congestion-control metric arrays must match the report count.";
        }

        int metricIndex = 0;
        while (metricIndex < report.ReportCount)
        {
            if (!report.Received[metricIndex])
            {
                if (report.EcnMarks[metricIndex] != 0 ||
                    report.ArrivalTimeOffsets[metricIndex] != 0)
                {
                    return "An unreceived packet metric cannot contain ECN or arrival data.";
                }
            }
            else
            {
                if (report.EcnMarks[metricIndex] > EcnMaximum)
                {
                    return "A congestion-control ECN mark must fit two bits.";
                }

                if (report.ArrivalTimeOffsets[metricIndex] >
                    ArrivalTimeOffsetMaximum)
                {
                    return "A congestion-control arrival offset must fit thirteen bits.";
                }
            }

            metricIndex = metricIndex + 1;
        }

        return null;
    }

    internal static string? GetCongestionControlFeedbackDecodeError(
        ReadOnlySpan<byte> source)
    {
        if (source.Length < TimestampSize ||
            source.Length % RtcpProtocolConstants.LengthWordSize != 0)
        {
            return "Congestion-control feedback must end with a 32-bit report timestamp.";
        }

        int reportLimit = source.Length - TimestampSize;
        int offset = 0;
        while (offset < reportLimit)
        {
            if (reportLimit - offset < ReportHeaderSize)
            {
                return "A congestion-control report block header is truncated.";
            }

            int reportCount = NetworkBinaryReader.ReadUInt16(source, offset + 6);
            if (reportCount > MaximumReportCount)
            {
                return "A congestion-control report cannot exceed 16384 metrics.";
            }

            int metricBytes = reportCount * MetricSize;
            int metricPaddingSize = GetMetricPaddingSize(reportCount);
            int reportSize = ReportHeaderSize + metricBytes + metricPaddingSize;
            if (reportSize > reportLimit - offset)
            {
                return "A congestion-control report block is truncated.";
            }

            if (metricPaddingSize != 0 &&
                NetworkBinaryReader.ReadUInt16(
                    source,
                    offset + ReportHeaderSize + metricBytes) != 0)
            {
                return "Congestion-control metric padding must be zero.";
            }

            offset = offset + reportSize;
        }

        return null;
    }

    internal static long CalculateCongestionControlFeedbackSize(
        RtcpCongestionControlFeedback feedback)
    {
        long size = TimestampSize;
        int reportIndex = 0;
        while (reportIndex < feedback.Reports.Length)
        {
            RtcpCongestionControlReport report = feedback.Reports[reportIndex];
            size = size + ReportHeaderSize +
                (long)report.ReportCount * MetricSize +
                GetMetricPaddingSize(report.ReportCount);
            reportIndex = reportIndex + 1;
        }

        return size;
    }

    internal static RtcpCongestionControlFeedback DecodeCongestionControlFeedback(
        ReadOnlySpan<byte> source)
    {
        int reportLimit = source.Length - TimestampSize;
        int reportCount = CountReportBlocks(source, reportLimit);
        RtcpCongestionControlFeedback feedback =
            new RtcpCongestionControlFeedback();
        feedback.Reports = new RtcpCongestionControlReport[reportCount];
        int offset = 0;
        int reportIndex = 0;
        while (offset < reportLimit)
        {
            RtcpCongestionControlReport report =
                DecodeCongestionControlReport(source.Slice(offset));
            feedback.Reports[reportIndex] = report;
            offset = offset + CalculateCongestionControlReportSize(report.ReportCount);
            reportIndex = reportIndex + 1;
        }

        feedback.ReportTimestamp = NetworkBinaryReader.ReadUInt32(
            source,
            reportLimit);
        return feedback;
    }

    internal static RtcpCongestionControlReport DecodeCongestionControlReport(
        ReadOnlySpan<byte> source)
    {
        RtcpCongestionControlReport report =
            new RtcpCongestionControlReport();
        report.SourceIdentifier = NetworkBinaryReader.ReadUInt32(source, 0);
        report.BeginSequenceNumber = NetworkBinaryReader.ReadUInt16(source, 4);
        report.ReportCount = NetworkBinaryReader.ReadUInt16(source, 6);
        report.Received = new bool[report.ReportCount];
        report.EcnMarks = new byte[report.ReportCount];
        report.ArrivalTimeOffsets = new ushort[report.ReportCount];
        int metricIndex = 0;
        while (metricIndex < report.ReportCount)
        {
            ushort packedMetric = NetworkBinaryReader.ReadUInt16(
                source,
                ReportHeaderSize + metricIndex * MetricSize);
            if ((packedMetric & 32768) != 0)
            {
                report.Received[metricIndex] = true;
                report.EcnMarks[metricIndex] =
                    (byte)((packedMetric >> 13) & EcnMaximum);
                report.ArrivalTimeOffsets[metricIndex] =
                    (ushort)(packedMetric & ArrivalTimeOffsetMaximum);
            }

            metricIndex = metricIndex + 1;
        }

        return report;
    }

    internal static void EncodeCongestionControlFeedback(
        RtcpCongestionControlFeedback feedback,
        Span<byte> destination)
    {
        int offset = 0;
        int reportIndex = 0;
        while (reportIndex < feedback.Reports.Length)
        {
            RtcpCongestionControlReport report = feedback.Reports[reportIndex];
            EncodeCongestionControlReport(report, destination.Slice(offset));
            offset = offset + CalculateCongestionControlReportSize(
                report.ReportCount);
            reportIndex = reportIndex + 1;
        }

        NetworkBinaryWriter.WriteUInt32(
            destination,
            offset,
            feedback.ReportTimestamp);
    }

    internal static void EncodeCongestionControlReport(
        RtcpCongestionControlReport report,
        Span<byte> destination)
    {
        NetworkBinaryWriter.WriteUInt32(
            destination,
            0,
            report.SourceIdentifier);
        NetworkBinaryWriter.WriteUInt16(
            destination,
            4,
            report.BeginSequenceNumber);
        NetworkBinaryWriter.WriteUInt16(
            destination,
            6,
            (ushort)report.ReportCount);
        int metricIndex = 0;
        while (metricIndex < report.ReportCount)
        {
            ushort packedMetric = 0;
            if (report.Received[metricIndex])
            {
                packedMetric = (ushort)(32768 |
                    report.EcnMarks[metricIndex] << 13 |
                    report.ArrivalTimeOffsets[metricIndex]);
            }

            NetworkBinaryWriter.WriteUInt16(
                destination,
                ReportHeaderSize + metricIndex * MetricSize,
                packedMetric);
            metricIndex = metricIndex + 1;
        }

        if (GetMetricPaddingSize(report.ReportCount) != 0)
        {
            NetworkBinaryWriter.WriteUInt16(
                destination,
                ReportHeaderSize + report.ReportCount * MetricSize,
                0);
        }
    }

    internal static ushort GetCongestionControlSequenceNumber(
        RtcpCongestionControlReport? report,
        int metricIndex)
    {
        if (report == null)
        {
            throw new RtcpPacketFormatException(
                "The congestion-control report block is required.");
        }

        if (metricIndex < 0 || metricIndex >= report.ReportCount)
        {
            throw new RtcpPacketFormatException(
                "The metric index is outside the congestion-control sequence range.");
        }

        return (ushort)((report.BeginSequenceNumber + metricIndex) & 65535);
    }

    internal static int CountReportBlocks(
        ReadOnlySpan<byte> source,
        int reportLimit)
    {
        int count = 0;
        int offset = 0;
        while (offset < reportLimit)
        {
            int metricCount = NetworkBinaryReader.ReadUInt16(source, offset + 6);
            offset = offset + CalculateCongestionControlReportSize(metricCount);
            count = count + 1;
        }

        return count;
    }

    internal static int CalculateCongestionControlReportSize(int reportCount)
    {
        return ReportHeaderSize + reportCount * MetricSize +
            GetMetricPaddingSize(reportCount);
    }

    internal static int GetMetricPaddingSize(int reportCount)
    {
        if (reportCount % 2 == 0)
        {
            return 0;
        }

        return MetricSize;
    }
}
