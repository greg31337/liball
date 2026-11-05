using rtp;
using rtp.Tests.Wire.Fixtures;

namespace rtp.Tests.Wire;

public class PionWireInteroperabilityTests
{
    static readonly object[] RtpCases =
    [
        new object[] { "packet", PionWireFixtures.RtpPacket, -1 },
        new object[]
        {
            "one-byte extensions",
            PionWireFixtures.RtpOneByteHeaderExtensions,
            (int)RtpHeaderExtensionFormat.OneByte
        },
        new object[]
        {
            "two-byte extensions",
            PionWireFixtures.RtpTwoByteHeaderExtensions,
            (int)RtpHeaderExtensionFormat.TwoByte
        },
        new object[]
        {
            "raw extension",
            PionWireFixtures.RtpRawHeaderExtension,
            (int)RtpHeaderExtensionFormat.Raw
        }
    ];

    static readonly object[] CoreRtcpCases =
    [
        new object[]
        {
            "sender report",
            PionWireFixtures.RtcpSenderReport,
            RtcpPacketType.SenderReport
        },
        new object[]
        {
            "receiver report",
            PionWireFixtures.RtcpReceiverReport,
            RtcpPacketType.ReceiverReport
        },
        new object[]
        {
            "source description",
            PionWireFixtures.RtcpSourceDescription,
            RtcpPacketType.SourceDescription
        },
        new object[]
        {
            "goodbye",
            PionWireFixtures.RtcpGoodbye,
            RtcpPacketType.Goodbye
        },
        new object[]
        {
            "application defined",
            PionWireFixtures.RtcpApplicationDefined,
            RtcpPacketType.ApplicationDefined
        }
    ];

    static readonly object[] FeedbackRtcpCases =
    [
        new object[]
        {
            "generic NACK",
            PionWireFixtures.RtcpGenericNack,
            RtcpFeedbackPacketType.TransportLayer,
            (byte)1
        },
        new object[]
        {
            "rapid resynchronization request",
            PionWireFixtures.RtcpRapidResynchronizationRequest,
            RtcpFeedbackPacketType.TransportLayer,
            (byte)5
        },
        new object[]
        {
            "picture loss indication",
            PionWireFixtures.RtcpPictureLossIndication,
            RtcpFeedbackPacketType.PayloadSpecific,
            (byte)1
        },
        new object[]
        {
            "Pion transport-layer slice loss indication",
            PionWireFixtures.RtcpSliceLossIndication,
            RtcpFeedbackPacketType.TransportLayer,
            (byte)2
        },
        new object[]
        {
            "full intra request",
            PionWireFixtures.RtcpFullIntraRequest,
            RtcpFeedbackPacketType.PayloadSpecific,
            (byte)4
        },
        new object[]
        {
            "receiver estimated maximum bitrate",
            PionWireFixtures.RtcpReceiverEstimatedMaximumBitrate,
            RtcpFeedbackPacketType.PayloadSpecific,
            (byte)15
        },
        new object[]
        {
            "congestion-control feedback",
            PionWireFixtures.RtcpCongestionControlFeedback,
            RtcpFeedbackPacketType.TransportLayer,
            (byte)11
        }
    ];

    [TestCaseSource(nameof(RtpCases))]
    public void PionRtpPacketsDecodeAndEncodeByteForByte(
        string name,
        byte[] fixture,
        int expectedExtensionFormat)
    {
        IRtpPacketCodec codec = CreateRtpCodec();

        RtpPacket packet = codec.DecodeRtpPacket(fixture);
        byte[] encoded = codec.EncodeRtpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(packet.Version, Is.EqualTo(2), name);
            Assert.That(encoded, Is.EqualTo(fixture), name);
            if (expectedExtensionFormat < 0)
            {
                Assert.That(packet.HeaderExtension, Is.Null, name);
            }
            else
            {
                Assert.That((int)packet.HeaderExtension!.Format,
                    Is.EqualTo(expectedExtensionFormat), name);
            }
        });
    }

    [TestCaseSource(nameof(CoreRtcpCases))]
    public void PionCoreRtcpPacketsDecodeToTypedModelsAndEncodeByteForByte(
        string name,
        byte[] fixture,
        RtcpPacketType expectedType)
    {
        IRtcpPacketCodec codec = CreateRtcpCodec();

        RtcpPacket packet = codec.DecodeRtcpPacket(fixture);
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(packet.Type, Is.EqualTo(expectedType), name);
            Assert.That(encoded, Is.EqualTo(fixture), name);
            AssertCoreBody(packet, expectedType, name);
        });
    }

    [TestCaseSource(nameof(FeedbackRtcpCases))]
    public void PionFeedbackPacketsDecodeAndEncodeByteForByte(
        string name,
        byte[] fixture,
        RtcpFeedbackPacketType expectedPacketType,
        byte expectedFormat)
    {
        IRtcpPacketCodec codec = CreateRtcpCodec();

        RtcpPacket packet = codec.DecodeRtcpPacket(fixture);
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(packet.Feedback, Is.Not.Null, name);
            Assert.That(packet.Feedback!.PacketType,
                Is.EqualTo(expectedPacketType), name);
            Assert.That(packet.Feedback.Format, Is.EqualTo(expectedFormat), name);
            Assert.That(encoded, Is.EqualTo(fixture), name);
            AssertFeedbackBody(packet.Feedback, expectedPacketType, expectedFormat, name);
        });
    }

    [Test]
    public void PionExtendedReportDecodesEveryRfc3611BlockAndEncodesByteForByte()
    {
        IRtcpPacketCodec codec = CreateRtcpCodec();

        RtcpPacket packet = codec.DecodeRtcpPacket(PionWireFixtures.RtcpExtendedReport);
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
        RtcpExtendedReportBlock[] blocks = packet.ExtendedReport!.Blocks;

        Assert.Multiple(() =>
        {
            Assert.That(packet.Type, Is.EqualTo(RtcpPacketType.ExtendedReport));
            Assert.That(blocks, Has.Length.EqualTo(7));
            int index = 0;
            while (index < blocks.Length)
            {
                Assert.That(blocks[index].Type,
                    Is.EqualTo((RtcpExtendedReportBlockType)(index + 1)));
                index = index + 1;
            }

            Assert.That(blocks[0].RunLengthChunks, Has.Length.EqualTo(1));
            Assert.That(blocks[1].RunLengthChunks, Has.Length.EqualTo(1));
            Assert.That(blocks[2].ReceiptTimes,
                Is.EqualTo(new uint[] { 0x11111111, 0x22222222, 0x33333333 }));
            Assert.That(blocks[3].ReceiverReferenceTime,
                Is.EqualTo(0x4142434445464748UL));
            Assert.That(blocks[4].DelayReports, Has.Length.EqualTo(2));
            Assert.That(blocks[5].ReportsLoss, Is.True);
            Assert.That(blocks[5].ReportsDuplicates, Is.True);
            Assert.That(blocks[5].ReportsJitter, Is.True);
            Assert.That(blocks[6].JitterBufferAbsoluteMaximumDelay,
                Is.EqualTo(30));
            Assert.That(encoded, Is.EqualTo(PionWireFixtures.RtcpExtendedReport));
        });
    }

    static void AssertCoreBody(
        RtcpPacket packet,
        RtcpPacketType expectedType,
        string name)
    {
        switch (expectedType)
        {
            case RtcpPacketType.SenderReport:
                Assert.That(packet.SenderReport, Is.Not.Null, name);
                break;
            case RtcpPacketType.ReceiverReport:
                Assert.That(packet.ReceiverReport, Is.Not.Null, name);
                break;
            case RtcpPacketType.SourceDescription:
                Assert.That(packet.SourceDescription, Is.Not.Null, name);
                break;
            case RtcpPacketType.Goodbye:
                Assert.That(packet.Goodbye, Is.Not.Null, name);
                break;
            case RtcpPacketType.ApplicationDefined:
                Assert.That(packet.ApplicationDefined, Is.Not.Null, name);
                break;
        }
    }

    static void AssertFeedbackBody(
        RtcpFeedbackPacket feedback,
        RtcpFeedbackPacketType packetType,
        byte format,
        string name)
    {
        if (packetType == RtcpFeedbackPacketType.TransportLayer)
        {
            switch (format)
            {
                case 1:
                    Assert.That(feedback.GenericNack, Is.Not.Null, name);
                    break;
                case 2:
                    Assert.That(feedback.RawFeedbackControlInformation,
                        Is.Not.Empty, name);
                    break;
                case 5:
                    Assert.That(feedback.RapidResynchronizationRequest,
                        Is.Not.Null, name);
                    break;
                case 11:
                    Assert.That(feedback.CongestionControlFeedback,
                        Is.Not.Null, name);
                    break;
            }

            return;
        }

        switch (format)
        {
            case 1:
                Assert.That(feedback.PictureLossIndication, Is.Not.Null, name);
                break;
            case 4:
                Assert.That(feedback.FullIntraRequest, Is.Not.Null, name);
                break;
            case 15:
                Assert.That(feedback.ApplicationFeedback, Is.Not.Null, name);
                break;
        }
    }

    static IRtpPacketCodec CreateRtpCodec()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.StrictValidation = true;
        configuration.PreservePadding = true;
        configuration.MaximumRtpPacketSize = 65535;
        return new RtpPacketCodec(configuration);
    }

    static IRtcpPacketCodec CreateRtcpCodec()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.StrictValidation = true;
        configuration.PreservePadding = true;
        configuration.MaximumRtcpCompoundPacketSize = 65535;
        return new RtcpPacketCodec(configuration);
    }
}
