using rtp;

namespace rtp.Tests.Wire;

public class RtcpSenderReceiverReportCodecTests
{
    [Test]
    public void SenderReportMatchesItsWireLayoutAndRoundTripsEverySection()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        byte[] source = new byte[]
        {
            0x81, 200, 0, 13,
            0xA1, 0xA2, 0xA3, 0xA4,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x11, 0x12, 0x13, 0x14,
            0x21, 0x22, 0x23, 0x24,
            0x31, 0x32, 0x33, 0x34,
            0x41, 0x42, 0x43, 0x44,
            0x80, 0xFF, 0xFF, 0xFE,
            0x51, 0x52, 0x53, 0x54,
            0x61, 0x62, 0x63, 0x64,
            0x71, 0x72, 0x73, 0x74,
            0x81, 0x82, 0x83, 0x84,
            0x91, 0x92, 0x93, 0x94
        };
        byte[] expected = (byte[])source.Clone();

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        source[52] = 0;
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(packet.Type, Is.EqualTo(RtcpPacketType.SenderReport));
            Assert.That(packet.UnknownPacket, Is.Null);
            Assert.That(packet.Header.Count, Is.EqualTo(1));
            Assert.That(packet.Header.PacketType, Is.EqualTo(200));
            Assert.That(packet.Header.LengthInWordsMinusOne, Is.EqualTo(13));
            Assert.That(packet.SenderReport?.SenderSynchronizationSource,
                Is.EqualTo(0xA1A2A3A4U));
            Assert.That(packet.SenderReport?.SenderInformation.NtpTimestamp,
                Is.EqualTo(0x0102030405060708UL));
            Assert.That(packet.SenderReport?.SenderInformation.RtpTimestamp,
                Is.EqualTo(0x11121314U));
            Assert.That(packet.SenderReport?.SenderInformation.PacketCount,
                Is.EqualTo(0x21222324U));
            Assert.That(packet.SenderReport?.SenderInformation.OctetCount,
                Is.EqualTo(0x31323334U));
            Assert.That(packet.SenderReport?.ReceptionReports.Length, Is.EqualTo(1));
            Assert.That(packet.SenderReport?.ReceptionReports[0].SourceIdentifier,
                Is.EqualTo(0x41424344U));
            Assert.That(packet.SenderReport?.ReceptionReports[0].FractionLost,
                Is.EqualTo(0x80));
            Assert.That(packet.SenderReport?.ReceptionReports[0].CumulativePacketsLost,
                Is.EqualTo(-2));
            Assert.That(packet.SenderReport?.ReceptionReports[0].ExtendedHighestSequenceNumber,
                Is.EqualTo(0x51525354U));
            Assert.That(packet.SenderReport?.ReceptionReports[0].InterarrivalJitter,
                Is.EqualTo(0x61626364U));
            Assert.That(packet.SenderReport?.ReceptionReports[0].LastSenderReport,
                Is.EqualTo(0x71727374U));
            Assert.That(packet.SenderReport?.ReceptionReports[0].DelaySinceLastSenderReport,
                Is.EqualTo(0x81828384U));
            Assert.That(packet.SenderReport?.ProfileExtension,
                Is.EqualTo(new byte[] { 0x91, 0x92, 0x93, 0x94 }));
            Assert.That(encoded, Is.EqualTo(expected));
        });
    }

    [Test]
    public void MinimumReceiverReportDerivesItsHeaderFromTheTypedBody()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.ReceiverReport;
        packet.ReceiverReport = new RtcpReceiverReport();
        packet.ReceiverReport.SenderSynchronizationSource = 0x01020304U;
        packet.Header.Count = 31;
        packet.Header.PacketType = 222;
        packet.Header.LengthInWordsMinusOne = 99;

        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
        RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(codec.GetRtcpPacketEncodedSize(packet), Is.EqualTo(8));
            Assert.That(encoded,
                Is.EqualTo(new byte[] { 0x80, 201, 0, 1, 1, 2, 3, 4 }));
            Assert.That(decoded.Type, Is.EqualTo(RtcpPacketType.ReceiverReport));
            Assert.That(decoded.ReceiverReport?.SenderSynchronizationSource,
                Is.EqualTo(0x01020304U));
            Assert.That(decoded.ReceiverReport?.ReceptionReports, Is.Empty);
            Assert.That(decoded.ReceiverReport?.ProfileExtension, Is.Empty);
            Assert.That(decoded.UnknownPacket, Is.Null);
        });
    }

    [Test]
    public void ReceiverReportSupportsTheThirtyOneBlockBoundary()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.ReceiverReport;
        packet.ReceiverReport = new RtcpReceiverReport();
        packet.ReceiverReport.SenderSynchronizationSource = 7U;
        packet.ReceiverReport.ReceptionReports = new RtcpReceptionReport[31];
        int reportIndex = 0;
        while (reportIndex < packet.ReceiverReport.ReceptionReports.Length)
        {
            RtcpReceptionReport report = new RtcpReceptionReport();
            report.SourceIdentifier = (uint)(reportIndex + 1);
            report.CumulativePacketsLost = reportIndex - 15;
            packet.ReceiverReport.ReceptionReports[reportIndex] = report;
            reportIndex = reportIndex + 1;
        }

        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
        RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Length, Is.EqualTo(752));
            Assert.That(encoded[..4], Is.EqualTo(new byte[] { 0x9F, 201, 0, 187 }));
            Assert.That(decoded.Header.Count, Is.EqualTo(31));
            Assert.That(decoded.ReceiverReport?.ReceptionReports.Length, Is.EqualTo(31));
            Assert.That(decoded.ReceiverReport?.ReceptionReports[0].SourceIdentifier,
                Is.EqualTo(1U));
            Assert.That(decoded.ReceiverReport?.ReceptionReports[30].SourceIdentifier,
                Is.EqualTo(31U));
            Assert.That(decoded.ReceiverReport?.ReceptionReports[30].CumulativePacketsLost,
                Is.EqualTo(15));
        });
    }

    [Test]
    public void TypedReportPaddingIsKeptSeparateFromTheProfileExtension()
    {
        byte[] source = new byte[]
        {
            0xA0, 201, 0, 3,
            1, 2, 3, 4,
            0xAA, 0xBB, 0xCC, 0xDD,
            0x11, 0x22, 0x33, 4
        };
        IRtcpPacketCodec preservingCodec = CreateCodec(true);
        IRtcpPacketCodec normalizingCodec = CreateCodec(false);

        RtcpPacket preserved = preservingCodec.DecodeRtcpPacket(source);
        RtcpPacket normalized = normalizingCodec.DecodeRtcpPacket(source);

        Assert.Multiple(() =>
        {
            Assert.That(preserved.ReceiverReport?.ProfileExtension,
                Is.EqualTo(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }));
            Assert.That(preserved.Padding,
                Is.EqualTo(new byte[] { 0x11, 0x22, 0x33, 4 }));
            Assert.That(normalized.Padding, Is.EqualTo(new byte[] { 0, 0, 0, 4 }));
            Assert.That(preservingCodec.EncodeRtcpPacketToArray(preserved),
                Is.EqualTo(source));
            Assert.That(normalizingCodec.EncodeRtcpPacketToArray(preserved),
                Is.EqualTo(new byte[]
                {
                    0xA0, 201, 0, 3,
                    1, 2, 3, 4,
                    0xAA, 0xBB, 0xCC, 0xDD,
                    0, 0, 0, 4
                }));
        });
    }

    [Test]
    public void DecoderRejectsLengthsThatCannotContainTheDeclaredReports()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        byte[] shortSenderReport = new byte[28];
        shortSenderReport[0] = 0x81;
        shortSenderReport[1] = 200;
        shortSenderReport[3] = 6;
        byte[] shortReceiverReport = new byte[8];
        shortReceiverReport[0] = 0x81;
        shortReceiverReport[1] = 201;
        shortReceiverReport[3] = 1;
        byte[] misalignedExtension = new byte[32];
        misalignedExtension[0] = 0xA0;
        misalignedExtension[1] = 200;
        misalignedExtension[3] = 7;
        misalignedExtension[31] = 1;

        Assert.Multiple(() =>
        {
            Assert.That(codec.TryDecodeRtcpPacket(shortSenderReport).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(shortReceiverReport).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(misalignedExtension).Success, Is.False);
            Assert.Throws<RtcpPacketFormatException>(() =>
                codec.DecodeRtcpPacket(shortSenderReport));
        });
    }

    [Test]
    public void InvalidTypedReportsFailWithoutPartiallyWriting()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        RtcpPacket missingBody = new RtcpPacket();
        missingBody.Type = RtcpPacketType.SenderReport;
        RtcpPacket misalignedExtension = new RtcpPacket();
        misalignedExtension.Type = RtcpPacketType.SenderReport;
        misalignedExtension.SenderReport = new RtcpSenderReport();
        misalignedExtension.SenderReport.ProfileExtension = new byte[] { 1, 2 };
        RtcpPacket tooManyReports = new RtcpPacket();
        tooManyReports.Type = RtcpPacketType.ReceiverReport;
        tooManyReports.ReceiverReport = new RtcpReceiverReport();
        tooManyReports.ReceiverReport.ReceptionReports = new RtcpReceptionReport[32];
        byte[] destination = new byte[64];
        int destinationIndex = 0;
        while (destinationIndex < destination.Length)
        {
            destination[destinationIndex] = 77;
            destinationIndex = destinationIndex + 1;
        }

        RtcpCodecResult missingResult = codec.TryEncodeRtcpPacket(
            missingBody,
            destination);
        RtcpCodecResult alignmentResult = codec.TryEncodeRtcpPacket(
            misalignedExtension,
            destination);
        RtcpCodecResult countResult = codec.TryEncodeRtcpPacket(
            tooManyReports,
            destination);

        Assert.Multiple(() =>
        {
            Assert.That(missingResult.Success, Is.False);
            Assert.That(alignmentResult.Success, Is.False);
            Assert.That(countResult.Success, Is.False);
            Assert.That(destination, Has.All.EqualTo(77));
            Assert.Throws<RtcpPacketFormatException>(() =>
                codec.GetRtcpPacketEncodedSize(misalignedExtension));
        });
    }

    static IRtcpPacketCodec CreateCodec(bool preservePadding)
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.StrictValidation = true;
        configuration.PreservePadding = preservePadding;
        configuration.MaximumRtcpCompoundPacketSize = 65535;
        return new RtcpPacketCodec(configuration);
    }
}
