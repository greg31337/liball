using rtp;

namespace rtp.Tests.Wire;

public class RtcpReportBlockWireCodecTests
{
    [Test]
    public void SenderInformationMatchesTheRfc3550FieldLayout()
    {
        RtcpSenderInformation information = new RtcpSenderInformation();
        information.NtpTimestamp = 0x0123456789ABCDEFUL;
        information.RtpTimestamp = 0x10203040U;
        information.PacketCount = 0x50607080U;
        information.OctetCount = 0x90A0B0C0U;
        byte[] expected = new byte[]
        {
            0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF,
            0x10, 0x20, 0x30, 0x40,
            0x50, 0x60, 0x70, 0x80,
            0x90, 0xA0, 0xB0, 0xC0
        };
        byte[] destination = new byte[expected.Length];

        int written = RtcpReportBlockWireCodec.EncodeSenderInformation(
            information,
            destination);
        RtcpSenderInformation decoded =
            RtcpReportBlockWireCodec.DecodeSenderInformation(destination);

        Assert.Multiple(() =>
        {
            Assert.That(RtcpReportBlockWireCodec.GetSenderInformationEncodedSize(
                information), Is.EqualTo(20));
            Assert.That(written, Is.EqualTo(20));
            Assert.That(destination, Is.EqualTo(expected));
            Assert.That(decoded.NtpTimestamp, Is.EqualTo(0x0123456789ABCDEFUL));
            Assert.That(decoded.RtpTimestamp, Is.EqualTo(0x10203040U));
            Assert.That(decoded.PacketCount, Is.EqualTo(0x50607080U));
            Assert.That(decoded.OctetCount, Is.EqualTo(0x90A0B0C0U));
        });
    }

    [Test]
    public void ReceptionReportRoundTripsEveryFieldAndNegativeLoss()
    {
        RtcpReceptionReport report = CreateReport(-8388608, 0x80);
        byte[] expected = new byte[]
        {
            0x01, 0x23, 0x45, 0x67,
            0x80, 0x80, 0x00, 0x00,
            0x89, 0xAB, 0xCD, 0xEF,
            0x10, 0x20, 0x30, 0x40,
            0x50, 0x60, 0x70, 0x80,
            0x90, 0xA0, 0xB0, 0xC0
        };
        byte[] destination = new byte[expected.Length];

        int written = RtcpReportBlockWireCodec.EncodeReceptionReport(
            report,
            destination);
        RtcpReceptionReport decoded =
            RtcpReportBlockWireCodec.DecodeReceptionReport(destination);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(24));
            Assert.That(destination, Is.EqualTo(expected));
            Assert.That(decoded.SourceIdentifier, Is.EqualTo(0x01234567U));
            Assert.That(decoded.FractionLost, Is.EqualTo(0x80));
            Assert.That(decoded.CumulativePacketsLost, Is.EqualTo(-8388608));
            Assert.That(decoded.ExtendedHighestSequenceNumber, Is.EqualTo(0x89ABCDEFU));
            Assert.That(decoded.InterarrivalJitter, Is.EqualTo(0x10203040U));
            Assert.That(decoded.LastSenderReport, Is.EqualTo(0x50607080U));
            Assert.That(decoded.DelaySinceLastSenderReport, Is.EqualTo(0x90A0B0C0U));
        });
    }

    [Test]
    public void ReceptionReportSupportsPositiveSigned24Boundary()
    {
        RtcpReceptionReport report = CreateReport(8388607, 255);
        byte[] destination = new byte[24];

        RtcpReportBlockWireCodec.EncodeReceptionReport(report, destination);
        RtcpReceptionReport decoded =
            RtcpReportBlockWireCodec.DecodeReceptionReport(destination);

        Assert.Multiple(() =>
        {
            Assert.That(destination[4], Is.EqualTo(255));
            Assert.That(destination[5..8], Is.EqualTo(new byte[] { 0x7F, 0xFF, 0xFF }));
            Assert.That(decoded.CumulativePacketsLost, Is.EqualTo(8388607));
        });
    }

    [Test]
    public void ReceptionReportArraySupportsTheThirtyOneBlockBoundary()
    {
        RtcpReceptionReport[] reports = new RtcpReceptionReport[31];
        int reportIndex = 0;
        while (reportIndex < reports.Length)
        {
            reports[reportIndex] = CreateReport(reportIndex - 15, (byte)reportIndex);
            reports[reportIndex].SourceIdentifier = (uint)(reportIndex + 1);
            reportIndex = reportIndex + 1;
        }

        int encodedSize = RtcpReportBlockWireCodec.GetReceptionReportsEncodedSize(reports);
        byte[] destination = new byte[encodedSize];
        int written = RtcpReportBlockWireCodec.EncodeReceptionReports(reports, destination);
        RtcpReceptionReport[] decoded =
            RtcpReportBlockWireCodec.DecodeReceptionReports(destination, 31);

        Assert.Multiple(() =>
        {
            Assert.That(encodedSize, Is.EqualTo(744));
            Assert.That(written, Is.EqualTo(744));
            Assert.That(decoded.Length, Is.EqualTo(31));
            Assert.That(decoded[0].SourceIdentifier, Is.EqualTo(1));
            Assert.That(decoded[0].CumulativePacketsLost, Is.EqualTo(-15));
            Assert.That(decoded[30].SourceIdentifier, Is.EqualTo(31));
            Assert.That(decoded[30].CumulativePacketsLost, Is.EqualTo(15));
        });
    }

    [Test]
    public void InvalidReportArrayFailsBeforeWriting()
    {
        RtcpReceptionReport[] tooMany = new RtcpReceptionReport[32];
        int reportIndex = 0;
        while (reportIndex < tooMany.Length)
        {
            tooMany[reportIndex] = CreateReport(0, 0);
            reportIndex = reportIndex + 1;
        }

        RtcpReceptionReport[] invalidLoss =
            new RtcpReceptionReport[] { CreateReport(8388608, 0) };
        byte[] destination = new byte[768];
        int destinationIndex = 0;
        while (destinationIndex < destination.Length)
        {
            destination[destinationIndex] = 77;
            destinationIndex = destinationIndex + 1;
        }

        Assert.Throws<RtcpPacketFormatException>(() =>
            RtcpReportBlockWireCodec.EncodeReceptionReports(tooMany, destination));
        Assert.Throws<RtcpPacketFormatException>(() =>
            RtcpReportBlockWireCodec.EncodeReceptionReports(invalidLoss, destination));

        Assert.Multiple(() =>
        {
            Assert.That(destination, Has.All.EqualTo(77));
            Assert.Throws<RtcpPacketFormatException>(() =>
                RtcpReportBlockWireCodec.GetReceptionReportEncodedSize(null!));
            Assert.Throws<RtcpPacketFormatException>(() =>
                RtcpReportBlockWireCodec.DecodeReceptionReports(new byte[24], 32));
            Assert.Throws<RtcpPacketFormatException>(() =>
                RtcpReportBlockWireCodec.DecodeReceptionReports(new byte[23], 1));
        });
    }

    [Test]
    public void FixedWidthCapacityAndLengthFailuresAreExplicit()
    {
        RtcpSenderInformation information = new RtcpSenderInformation();
        RtcpReceptionReport report = CreateReport(0, 0);

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpBufferCapacityException>(() =>
                RtcpReportBlockWireCodec.EncodeSenderInformation(
                    information,
                    new byte[19]));
            Assert.Throws<RtpBufferCapacityException>(() =>
                RtcpReportBlockWireCodec.EncodeReceptionReport(report, new byte[23]));
            Assert.Throws<RtpBufferCapacityException>(() =>
                RtcpReportBlockWireCodec.EncodeReceptionReports(
                    new RtcpReceptionReport[] { report },
                    new byte[23]));
            Assert.Throws<RtcpPacketFormatException>(() =>
                RtcpReportBlockWireCodec.DecodeSenderInformation(new byte[19]));
            Assert.Throws<RtcpPacketFormatException>(() =>
                RtcpReportBlockWireCodec.DecodeReceptionReport(new byte[25]));
        });
    }

    static RtcpReceptionReport CreateReport(int cumulativeLoss, byte fractionLost)
    {
        RtcpReceptionReport report = new RtcpReceptionReport();
        report.SourceIdentifier = 0x01234567U;
        report.FractionLost = fractionLost;
        report.CumulativePacketsLost = cumulativeLoss;
        report.ExtendedHighestSequenceNumber = 0x89ABCDEFU;
        report.InterarrivalJitter = 0x10203040U;
        report.LastSenderReport = 0x50607080U;
        report.DelaySinceLastSenderReport = 0x90A0B0C0U;
        return report;
    }
}
