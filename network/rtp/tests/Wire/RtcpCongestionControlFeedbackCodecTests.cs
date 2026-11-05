using rtp;

namespace rtp.Tests.Wire;

public class RtcpCongestionControlFeedbackCodecTests
{
    [Test]
    public void EmptyFeedbackRoundTripsTimestampWithoutCommonMediaSsrc()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x8B, 205, 0, 2,
            0x01, 0x02, 0x03, 0x04,
            0x89, 0xAB, 0xCD, 0xEF
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(packet.Feedback?.SenderSourceIdentifier,
                Is.EqualTo(0x01020304U));
            Assert.That(packet.Feedback?.MediaSourceIdentifier, Is.Zero);
            Assert.That(packet.Feedback?.TransportFormat,
                Is.EqualTo(RtcpTransportFeedbackFormat.CongestionControlFeedback));
            Assert.That(packet.Feedback?.CongestionControlFeedback?.Reports,
                Is.Empty);
            Assert.That(
                packet.Feedback?.CongestionControlFeedback?.ReportTimestamp,
                Is.EqualTo(0x89ABCDEFU));
            Assert.That(packet.Feedback?.RawFeedbackControlInformation,
                Is.EqualTo(new byte[] { 0x89, 0xAB, 0xCD, 0xEF }));
            Assert.That(codec.GetRtcpPacketEncodedSize(packet), Is.EqualTo(12));
            Assert.That(encoded, Is.EqualTo(source));
        });
    }

    [Test]
    public void FeedbackPreservesOuterRtcpPadding()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0xAB, 205, 0, 3,
            0, 0, 0, 1,
            1, 2, 3, 4,
            0xAA, 0xBB, 0xCC, 4
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);

        Assert.Multiple(() =>
        {
            Assert.That(packet.Header.HasPadding, Is.True);
            Assert.That(packet.Padding,
                Is.EqualTo(new byte[] { 0xAA, 0xBB, 0xCC, 4 }));
            Assert.That(
                packet.Feedback?.CongestionControlFeedback?.ReportTimestamp,
                Is.EqualTo(0x01020304U));
            Assert.That(codec.EncodeRtcpPacketToArray(packet), Is.EqualTo(source));
        });
    }

    [Test]
    public void OddMetricReportRoundTripsPaddingAndWrappedSequenceRange()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x8B, 205, 0, 6,
            0x01, 0x02, 0x03, 0x04,
            0x11, 0x22, 0x33, 0x44,
            0xFF, 0xFE, 0, 3,
            0, 0, 0xC2, 0, 0xFF, 0xFF,
            0, 0,
            0x89, 0xAB, 0xCD, 0xEF
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        RtcpCongestionControlReport report = packet.Feedback!
            .CongestionControlFeedback!.Reports[0];
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(report.SourceIdentifier, Is.EqualTo(0x11223344U));
            Assert.That(report.BeginSequenceNumber, Is.EqualTo(65534));
            Assert.That(report.ReportCount, Is.EqualTo(3));
            Assert.That(report.Received,
                Is.EqualTo(new bool[] { false, true, true }));
            Assert.That(report.EcnMarks,
                Is.EqualTo(new byte[] { 0, 2, 3 }));
            Assert.That(report.ArrivalTimeOffsets,
                Is.EqualTo(new ushort[] { 0, 512, 8191 }));
            Assert.That(codec.GetRtcpCongestionControlSequenceNumber(report, 0),
                Is.EqualTo(65534));
            Assert.That(codec.GetRtcpCongestionControlSequenceNumber(report, 1),
                Is.EqualTo(65535));
            Assert.That(codec.GetRtcpCongestionControlSequenceNumber(report, 2),
                Is.Zero);
            Assert.Throws<RtcpPacketFormatException>(() =>
                codec.GetRtcpCongestionControlSequenceNumber(report, 3));
            Assert.That(encoded, Is.EqualTo(source));
        });
    }

    [Test]
    public void MultipleReportsIncludingEmptyBlockPrecedeTimestamp()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x8B, 205, 0, 7,
            0, 0, 0, 9,
            0, 0, 0, 1, 0, 100, 0, 0,
            0, 0, 0, 2, 0, 200, 0, 2,
            0x80, 1, 0, 0,
            1, 2, 3, 4
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        RtcpCongestionControlFeedback feedback = packet.Feedback!
            .CongestionControlFeedback!;

        Assert.Multiple(() =>
        {
            Assert.That(feedback.Reports.Length, Is.EqualTo(2));
            Assert.That(feedback.Reports[0].ReportCount, Is.Zero);
            Assert.That(feedback.Reports[1].SourceIdentifier, Is.EqualTo(2));
            Assert.That(feedback.Reports[1].BeginSequenceNumber, Is.EqualTo(200));
            Assert.That(feedback.Reports[1].ReportCount, Is.EqualTo(2));
            Assert.That(feedback.Reports[1].Received,
                Is.EqualTo(new bool[] { true, false }));
            Assert.That(feedback.Reports[1].ArrivalTimeOffsets,
                Is.EqualTo(new ushort[] { 1, 0 }));
            Assert.That(feedback.ReportTimestamp, Is.EqualTo(0x01020304U));
            Assert.That(codec.EncodeRtcpPacketToArray(packet), Is.EqualTo(source));
        });
    }

    [Test]
    public void DecoderIgnoresReservedDataForAnUnreceivedPacket()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x8B, 205, 0, 5,
            0, 0, 0, 1,
            0, 0, 0, 2, 0, 10, 0, 1,
            0x7F, 0xFF, 0, 0,
            0x11, 0x22, 0x33, 0x44
        };
        byte[] canonical = (byte[])source.Clone();
        canonical[16] = 0;
        canonical[17] = 0;

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        RtcpCongestionControlReport report = packet.Feedback!
            .CongestionControlFeedback!.Reports[0];

        Assert.Multiple(() =>
        {
            Assert.That(report.Received, Is.EqualTo(new bool[] { false }));
            Assert.That(report.EcnMarks, Is.EqualTo(new byte[] { 0 }));
            Assert.That(report.ArrivalTimeOffsets,
                Is.EqualTo(new ushort[] { 0 }));
            Assert.That(packet.Feedback?.RawFeedbackControlInformation,
                Is.EqualTo(source[8..]));
            Assert.That(codec.EncodeRtcpPacketToArray(packet),
                Is.EqualTo(canonical));
        });
    }

    [Test]
    public void DecoderRejectsTruncationExcessCountsAndBadMetricPadding()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[][] malformedPackets = new byte[][]
        {
            new byte[]
            {
                0x8B, 205, 0, 1,
                0, 0, 0, 1
            },
            new byte[]
            {
                0x8B, 205, 0, 3,
                0, 0, 0, 1,
                0, 0, 0, 2,
                1, 2, 3, 4
            },
            new byte[]
            {
                0x8B, 205, 0, 4,
                0, 0, 0, 1,
                0, 0, 0, 2, 0, 0, 0x40, 1,
                1, 2, 3, 4
            },
            new byte[]
            {
                0x8B, 205, 0, 5,
                0, 0, 0, 1,
                0, 0, 0, 2, 0, 0, 0, 1,
                0x80, 1, 0, 1,
                1, 2, 3, 4
            },
            new byte[]
            {
                0x8B, 205, 0, 4,
                0, 0, 0, 1,
                0, 0, 0, 2, 0, 0, 0, 2,
                1, 2, 3, 4
            }
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
    public void EncoderRejectsInvalidModelsAtomically()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpCongestionControlReport report = CreateReport(1);
        RtcpPacket packet = CreatePacket(report);
        byte[] destination = new byte[64];
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = 77;
            index = index + 1;
        }

        packet.Feedback!.MediaSourceIdentifier = 1;
        AssertEncodingFails(codec, packet, destination);
        packet.Feedback.MediaSourceIdentifier = 0;

        report.ReportCount = -1;
        AssertEncodingFails(codec, packet, destination);
        report.ReportCount = 16385;
        AssertEncodingFails(codec, packet, destination);
        report.ReportCount = 1;

        report.Received = new bool[0];
        AssertEncodingFails(codec, packet, destination);
        report.Received = new bool[] { true };

        report.EcnMarks[0] = 4;
        AssertEncodingFails(codec, packet, destination);
        report.EcnMarks[0] = 0;

        report.ArrivalTimeOffsets[0] = 8192;
        AssertEncodingFails(codec, packet, destination);
        report.ArrivalTimeOffsets[0] = 0;

        report.Received[0] = false;
        report.EcnMarks[0] = 1;
        AssertEncodingFails(codec, packet, destination);
        report.EcnMarks[0] = 0;

        Assert.That(destination, Has.All.EqualTo(77));
    }

    static RtcpCongestionControlReport CreateReport(int count)
    {
        RtcpCongestionControlReport report = new RtcpCongestionControlReport();
        report.SourceIdentifier = 55;
        report.BeginSequenceNumber = 100;
        report.ReportCount = count;
        report.Received = new bool[count];
        report.EcnMarks = new byte[count];
        report.ArrivalTimeOffsets = new ushort[count];
        if (count != 0)
        {
            report.Received[0] = true;
        }

        return report;
    }

    static RtcpPacket CreatePacket(RtcpCongestionControlReport report)
    {
        RtcpCongestionControlFeedback congestion =
            new RtcpCongestionControlFeedback();
        congestion.Reports = new RtcpCongestionControlReport[] { report };
        congestion.ReportTimestamp = 1234;
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.TransportLayer;
        feedback.TransportFormat =
            RtcpTransportFeedbackFormat.CongestionControlFeedback;
        feedback.SenderSourceIdentifier = 44;
        feedback.CongestionControlFeedback = congestion;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.TransportFeedback;
        packet.Feedback = feedback;
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
