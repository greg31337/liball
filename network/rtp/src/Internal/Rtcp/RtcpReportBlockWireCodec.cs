using System;

namespace rtp;

internal static class RtcpReportBlockWireCodec
{
    internal static int GetSenderInformationEncodedSize(RtcpSenderInformation information)
    {
        if (information == null)
        {
            throw new RtcpPacketFormatException("RTCP sender information is required.");
        }

        return RtcpProtocolConstants.SenderInformationSize;
    }

    internal static int EncodeSenderInformation(
        RtcpSenderInformation information,
        Span<byte> destination)
    {
        int encodedSize = GetSenderInformationEncodedSize(information);
        if (destination.Length < encodedSize)
        {
            throw new RtpBufferCapacityException(
                "The destination is too small for RTCP sender information.");
        }

        NetworkBinaryWriter.WriteUInt64(destination, 0, information.NtpTimestamp);
        NetworkBinaryWriter.WriteUInt32(destination, 8, information.RtpTimestamp);
        NetworkBinaryWriter.WriteUInt32(destination, 12, information.PacketCount);
        NetworkBinaryWriter.WriteUInt32(destination, 16, information.OctetCount);
        return encodedSize;
    }

    internal static RtcpSenderInformation DecodeSenderInformation(
        ReadOnlySpan<byte> source)
    {
        if (source.Length != RtcpProtocolConstants.SenderInformationSize)
        {
            throw new RtcpPacketFormatException(
                "RTCP sender information must contain exactly 20 bytes.");
        }

        RtcpSenderInformation information = new RtcpSenderInformation();
        information.NtpTimestamp = NetworkBinaryReader.ReadUInt64(source, 0);
        information.RtpTimestamp = NetworkBinaryReader.ReadUInt32(source, 8);
        information.PacketCount = NetworkBinaryReader.ReadUInt32(source, 12);
        information.OctetCount = NetworkBinaryReader.ReadUInt32(source, 16);
        return information;
    }

    internal static int GetReceptionReportEncodedSize(RtcpReceptionReport report)
    {
        string? validationError = GetReceptionReportValidationError(report);
        if (validationError != null)
        {
            throw new RtcpPacketFormatException(validationError);
        }

        return RtcpProtocolConstants.ReceptionReportSize;
    }

    internal static int EncodeReceptionReport(
        RtcpReceptionReport report,
        Span<byte> destination)
    {
        int encodedSize = GetReceptionReportEncodedSize(report);
        if (destination.Length < encodedSize)
        {
            throw new RtpBufferCapacityException(
                "The destination is too small for an RTCP reception report.");
        }

        EncodeValidatedReceptionReport(report, destination);
        return encodedSize;
    }

    internal static RtcpReceptionReport DecodeReceptionReport(ReadOnlySpan<byte> source)
    {
        if (source.Length != RtcpProtocolConstants.ReceptionReportSize)
        {
            throw new RtcpPacketFormatException(
                "An RTCP reception report must contain exactly 24 bytes.");
        }

        RtcpReceptionReport report = new RtcpReceptionReport();
        report.SourceIdentifier = NetworkBinaryReader.ReadUInt32(source, 0);
        report.FractionLost = NetworkBinaryReader.ReadByte(source, 4);
        report.CumulativePacketsLost = NetworkBinaryReader.ReadInt24(source, 5);
        report.ExtendedHighestSequenceNumber = NetworkBinaryReader.ReadUInt32(source, 8);
        report.InterarrivalJitter = NetworkBinaryReader.ReadUInt32(source, 12);
        report.LastSenderReport = NetworkBinaryReader.ReadUInt32(source, 16);
        report.DelaySinceLastSenderReport = NetworkBinaryReader.ReadUInt32(source, 20);
        return report;
    }

    internal static int GetReceptionReportsEncodedSize(RtcpReceptionReport[] reports)
    {
        string? validationError = GetReceptionReportsValidationError(reports);
        if (validationError != null)
        {
            throw new RtcpPacketFormatException(validationError);
        }

        return reports.Length * RtcpProtocolConstants.ReceptionReportSize;
    }

    internal static int EncodeReceptionReports(
        RtcpReceptionReport[] reports,
        Span<byte> destination)
    {
        int encodedSize = GetReceptionReportsEncodedSize(reports);
        if (destination.Length < encodedSize)
        {
            throw new RtpBufferCapacityException(
                "The destination is too small for the RTCP reception reports.");
        }

        int reportIndex = 0;
        while (reportIndex < reports.Length)
        {
            int offset = reportIndex * RtcpProtocolConstants.ReceptionReportSize;
            EncodeValidatedReceptionReport(
                reports[reportIndex],
                destination.Slice(offset, RtcpProtocolConstants.ReceptionReportSize));
            reportIndex = reportIndex + 1;
        }

        return encodedSize;
    }

    internal static RtcpReceptionReport[] DecodeReceptionReports(
        ReadOnlySpan<byte> source,
        int reportCount)
    {
        if (reportCount < 0 ||
            reportCount > RtcpProtocolConstants.MaximumReceptionReportCount)
        {
            throw new RtcpPacketFormatException(
                "An RTCP packet may contain between 0 and 31 reception reports.");
        }

        int expectedSize = reportCount * RtcpProtocolConstants.ReceptionReportSize;
        if (source.Length != expectedSize)
        {
            throw new RtcpPacketFormatException(
                "The RTCP reception-report bytes do not match the report count.");
        }

        RtcpReceptionReport[] reports = new RtcpReceptionReport[reportCount];
        int reportIndex = 0;
        while (reportIndex < reportCount)
        {
            int offset = reportIndex * RtcpProtocolConstants.ReceptionReportSize;
            reports[reportIndex] = DecodeReceptionReport(
                source.Slice(offset, RtcpProtocolConstants.ReceptionReportSize));
            reportIndex = reportIndex + 1;
        }

        return reports;
    }

    internal static string? GetReceptionReportValidationError(RtcpReceptionReport report)
    {
        if (report == null)
        {
            return "An RTCP reception report is required.";
        }

        if (report.CumulativePacketsLost < RtcpProtocolConstants.CumulativeLossMinimum ||
            report.CumulativePacketsLost > RtcpProtocolConstants.CumulativeLossMaximum)
        {
            return "Cumulative packet loss must fit in a signed 24-bit field.";
        }

        return null;
    }

    internal static string? GetReceptionReportsValidationError(
        RtcpReceptionReport[] reports)
    {
        if (reports == null)
        {
            return "The RTCP reception-report array is required.";
        }

        if (reports.Length > RtcpProtocolConstants.MaximumReceptionReportCount)
        {
            return "An RTCP packet cannot contain more than 31 reception reports.";
        }

        int reportIndex = 0;
        while (reportIndex < reports.Length)
        {
            string? reportError = GetReceptionReportValidationError(reports[reportIndex]);
            if (reportError != null)
            {
                return reportError;
            }

            reportIndex = reportIndex + 1;
        }

        return null;
    }

    internal static void EncodeValidatedReceptionReport(
        RtcpReceptionReport report,
        Span<byte> destination)
    {
        NetworkBinaryWriter.WriteUInt32(destination, 0, report.SourceIdentifier);
        NetworkBinaryWriter.WriteByte(destination, 4, report.FractionLost);
        NetworkBinaryWriter.WriteInt24(destination, 5, report.CumulativePacketsLost);
        NetworkBinaryWriter.WriteUInt32(
            destination,
            8,
            report.ExtendedHighestSequenceNumber);
        NetworkBinaryWriter.WriteUInt32(destination, 12, report.InterarrivalJitter);
        NetworkBinaryWriter.WriteUInt32(destination, 16, report.LastSenderReport);
        NetworkBinaryWriter.WriteUInt32(
            destination,
            20,
            report.DelaySinceLastSenderReport);
    }
}
