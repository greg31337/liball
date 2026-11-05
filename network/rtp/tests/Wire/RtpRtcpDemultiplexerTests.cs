using rtp;

namespace rtp.Tests.Wire;

public class RtpRtcpDemultiplexerTests
{
    [Test]
    public void EveryRtpMarkerPayloadCombinationUsesTheMuxPartition()
    {
        RtpRtcpDemultiplexer demultiplexer = new RtpRtcpDemultiplexer();

        Assert.Multiple(() =>
        {
            int markerValue = 0;
            while (markerValue < 2)
            {
                bool marker = markerValue == 1;
                int payloadTypeValue = 0;
                while (payloadTypeValue <=
                    RtpProtocolConstants.MaximumPayloadType)
                {
                    byte payloadType = (byte)payloadTypeValue;
                    RtpDatagramClassification classification =
                        demultiplexer.ClassifyDatagram(
                            CreateRtpDatagram(payloadType, marker));
                    bool permitted = payloadType <
                        RtpProtocolConstants
                            .MultiplexingForbiddenPayloadTypeMinimum ||
                        payloadType >
                        RtpProtocolConstants
                            .MultiplexingForbiddenPayloadTypeMaximum;
                    if (permitted)
                    {
                        Assert.That(
                            classification.Kind,
                            Is.EqualTo(RtpPacketKind.Rtp),
                            $"marker={marker}, payloadType={payloadType}");
                        Assert.That(
                            classification.PayloadType,
                            Is.EqualTo(payloadType),
                            $"marker={marker}, payloadType={payloadType}");
                    }
                    else
                    {
                        Assert.That(
                            classification.Kind,
                            Is.Not.EqualTo(RtpPacketKind.Rtp),
                            $"marker={marker}, payloadType={payloadType}");
                        if (classification.Kind == RtpPacketKind.Unknown)
                        {
                            Assert.That(
                                classification.ErrorMessage,
                                Is.Not.Null,
                                $"marker={marker}, payloadType={payloadType}");
                        }
                        else
                        {
                            Assert.That(
                                classification.ControlPacketType,
                                Is.Not.EqualTo(RtcpPacketType.Unknown),
                                $"marker={marker}, payloadType={payloadType}");
                        }
                    }

                    payloadTypeValue = payloadTypeValue + 1;
                }

                markerValue = markerValue + 1;
            }
        });
    }

    [TestCase((byte)0, false)]
    [TestCase((byte)63, false)]
    [TestCase((byte)96, false)]
    [TestCase((byte)127, false)]
    [TestCase((byte)0, true)]
    [TestCase((byte)63, true)]
    [TestCase((byte)96, true)]
    [TestCase((byte)127, true)]
    public void ClassifiesLegalRtpPayloadTypes(byte payloadType, bool marker)
    {
        IRtpRtcpDemultiplexer demultiplexer = new RtpRtcpDemultiplexer();
        byte[] datagram = CreateRtpDatagram(payloadType, marker);

        RtpDatagramClassification classification =
            demultiplexer.ClassifyDatagram(datagram);

        Assert.Multiple(() =>
        {
            Assert.That(classification.Kind, Is.EqualTo(RtpPacketKind.Rtp));
            Assert.That(classification.PayloadType, Is.EqualTo(payloadType));
            Assert.That(classification.ControlPacketType,
                Is.EqualTo(RtcpPacketType.Unknown));
            Assert.That(classification.ErrorMessage, Is.Null);
        });
    }

    [TestCase((byte)192, RtcpPacketType.HistoricFullIntraRequest)]
    [TestCase((byte)193, RtcpPacketType.HistoricNegativeAcknowledgement)]
    [TestCase((byte)194, RtcpPacketType.SmpteTimeCode)]
    [TestCase((byte)195, RtcpPacketType.ExtendedInterArrivalJitter)]
    [TestCase((byte)200, RtcpPacketType.SenderReport)]
    [TestCase((byte)201, RtcpPacketType.ReceiverReport)]
    [TestCase((byte)202, RtcpPacketType.SourceDescription)]
    [TestCase((byte)203, RtcpPacketType.Goodbye)]
    [TestCase((byte)204, RtcpPacketType.ApplicationDefined)]
    [TestCase((byte)205, RtcpPacketType.TransportFeedback)]
    [TestCase((byte)206, RtcpPacketType.PayloadFeedback)]
    [TestCase((byte)207, RtcpPacketType.ExtendedReport)]
    [TestCase((byte)208, RtcpPacketType.AudioVideoBridging)]
    [TestCase((byte)209, RtcpPacketType.ReceiverSummaryInformation)]
    [TestCase((byte)210, RtcpPacketType.PortMappingToken)]
    [TestCase((byte)211, RtcpPacketType.InterDestinationMediaSynchronization)]
    [TestCase((byte)212, RtcpPacketType.ReportingGroupSources)]
    [TestCase((byte)213, RtcpPacketType.SplicingNotification)]
    public void ClassifiesEveryKnownRtcpPacketType(
        byte packetType,
        RtcpPacketType expectedType)
    {
        RtpRtcpDemultiplexer demultiplexer = new RtpRtcpDemultiplexer();

        RtpDatagramClassification classification =
            demultiplexer.ClassifyDatagram(CreateRtcpDatagram(packetType));

        Assert.Multiple(() =>
        {
            Assert.That(classification.Kind, Is.EqualTo(RtpPacketKind.Rtcp));
            Assert.That(classification.ControlPacketType,
                Is.EqualTo(expectedType));
            Assert.That(classification.PayloadType, Is.Zero);
            Assert.That(classification.ErrorMessage, Is.Null);
        });
    }

    [Test]
    public void EveryRtcpTruncationAndCollisionAvoidsRtpFallback()
    {
        RtpRtcpDemultiplexer demultiplexer = new RtpRtcpDemultiplexer();
        byte[] packetTypes = new byte[]
        {
            192, 193, 194, 195,
            200, 201, 202, 203, 204, 205, 206,
            207, 208, 209, 210, 211, 212, 213
        };

        Assert.Multiple(() =>
        {
            int typeIndex = 0;
            while (typeIndex < packetTypes.Length)
            {
                byte packetType = packetTypes[typeIndex];
                byte[] complete = CreateRtcpDatagram(packetType);
                int prefixLength = 0;
                while (prefixLength < complete.Length)
                {
                    byte[] truncated = new byte[prefixLength];
                    Array.Copy(
                        complete,
                        0,
                        truncated,
                        0,
                        prefixLength);
                    RtpDatagramClassification truncation =
                        demultiplexer.ClassifyDatagram(truncated);
                    Assert.That(
                        truncation.Kind,
                        Is.EqualTo(RtpPacketKind.Unknown),
                        $"packetType={packetType}, length={prefixLength}");
                    Assert.That(
                        truncation.ErrorMessage,
                        Is.Not.Null,
                        $"packetType={packetType}, length={prefixLength}");
                    prefixLength = prefixLength + 1;
                }

                byte collidingPayloadType = (byte)(packetType &
                    RtpProtocolConstants.PayloadTypeMask);
                RtpDatagramClassification collision =
                    demultiplexer.ClassifyDatagram(
                        CreateRtpDatagram(collidingPayloadType, true));
                Assert.That(
                    collision.Kind,
                    Is.Not.EqualTo(RtpPacketKind.Rtp),
                    $"RTCP-shaped RTP discriminator={packetType}");
                if (collision.Kind == RtpPacketKind.Unknown)
                {
                    Assert.That(
                        collision.ErrorMessage,
                        Is.Not.Null,
                        $"RTCP-shaped RTP discriminator={packetType}");
                }
                else
                {
                    Assert.That(
                        collision.ControlPacketType,
                        Is.Not.EqualTo(RtcpPacketType.Unknown),
                        $"RTCP-shaped RTP discriminator={packetType}");
                }
                typeIndex = typeIndex + 1;
            }
        });
    }

    [TestCase((byte)64)]
    [TestCase((byte)72)]
    [TestCase((byte)80)]
    [TestCase((byte)95)]
    public void RejectsMuxForbiddenUnmarkedRtpPayloadTypes(byte payloadType)
    {
        RtpRtcpDemultiplexer demultiplexer = new RtpRtcpDemultiplexer();

        RtpDatagramClassification classification =
            demultiplexer.ClassifyDatagram(
                CreateRtpDatagram(payloadType, false));

        Assert.Multiple(() =>
        {
            Assert.That(classification.Kind,
                Is.EqualTo(RtpPacketKind.Unknown));
            Assert.That(classification.ErrorMessage,
                Does.Contain("forbidden"));
        });
    }

    [TestCase((byte)196)]
    [TestCase((byte)199)]
    [TestCase((byte)214)]
    [TestCase((byte)223)]
    public void UnassignedRtcpDiscriminatorsRemainUnknown(byte packetType)
    {
        RtpRtcpDemultiplexer demultiplexer = new RtpRtcpDemultiplexer();

        RtpDatagramClassification classification =
            demultiplexer.ClassifyDatagram(CreateRtcpDatagram(packetType));

        Assert.Multiple(() =>
        {
            Assert.That(classification.Kind,
                Is.EqualTo(RtpPacketKind.Unknown));
            Assert.That(classification.ErrorMessage,
                Does.Contain("not an assigned RTCP"));
        });
    }

    [Test]
    public void MalformedOrAmbiguousDatagramsRemainUnknown()
    {
        RtpRtcpDemultiplexer demultiplexer = new RtpRtcpDemultiplexer();
        byte[] wrongVersion = CreateRtpDatagram(96, false);
        wrongVersion[0] = 64;
        byte[] truncatedCsrc = CreateRtpDatagram(96, false);
        truncatedCsrc[0] = 129;
        byte[] truncatedExtension = CreateRtpDatagram(96, false);
        truncatedExtension[0] = 144;
        byte[] invalidPadding = CreateRtpDatagram(96, false);
        invalidPadding[0] = 160;
        byte[] truncatedRtcp = CreateRtcpDatagram(201);
        truncatedRtcp[2] = 0;
        truncatedRtcp[3] = 2;
        byte[] rtpShapedControlConflict = CreateRtpDatagram(72, true);

        RtpDatagramClassification[] classifications =
            new RtpDatagramClassification[]
            {
                demultiplexer.ClassifyDatagram(Array.Empty<byte>()),
                demultiplexer.ClassifyDatagram(new byte[] { 128 }),
                demultiplexer.ClassifyDatagram(wrongVersion),
                demultiplexer.ClassifyDatagram(truncatedCsrc),
                demultiplexer.ClassifyDatagram(truncatedExtension),
                demultiplexer.ClassifyDatagram(invalidPadding),
                demultiplexer.ClassifyDatagram(truncatedRtcp),
                demultiplexer.ClassifyDatagram(rtpShapedControlConflict)
            };

        Assert.Multiple(() =>
        {
            Assert.That(classifications,
                Has.All.Matches<RtpDatagramClassification>(
                    value => value.Kind == RtpPacketKind.Unknown));
            Assert.That(classifications,
                Has.All.Matches<RtpDatagramClassification>(
                    value => value.ErrorMessage != null));
        });
    }

    [Test]
    public void ValidRtcpDiscriminatorDoesNotFallBackToRtpWhenMalformed()
    {
        RtpRtcpDemultiplexer demultiplexer = new RtpRtcpDemultiplexer();
        byte[] datagram = CreateRtpDatagram(73, true);

        RtpDatagramClassification classification =
            demultiplexer.ClassifyDatagram(datagram);

        Assert.That(classification.Kind, Is.EqualTo(RtpPacketKind.Unknown));
    }

    static byte[] CreateRtpDatagram(byte payloadType, bool marker)
    {
        byte[] datagram = new byte[RtpProtocolConstants.FixedHeaderSize];
        datagram[0] = 128;
        datagram[1] = payloadType;
        if (marker)
        {
            datagram[1] = (byte)(datagram[1] |
                RtpProtocolConstants.MarkerMask);
        }

        return datagram;
    }

    static byte[] CreateRtcpDatagram(byte packetType)
    {
        int packetSize = 4;
        if (packetType == RtcpProtocolConstants.SenderReportPacketType)
        {
            packetSize = RtcpProtocolConstants.SenderReportMinimumSize;
        }
        else if (packetType == RtcpProtocolConstants.ReceiverReportPacketType)
        {
            packetSize = RtcpProtocolConstants.ReceiverReportMinimumSize;
        }
        else if (packetType ==
            RtcpProtocolConstants.ApplicationDefinedPacketType ||
            packetType == RtcpProtocolConstants.TransportFeedbackPacketType ||
            packetType == RtcpProtocolConstants.PayloadFeedbackPacketType)
        {
            packetSize = RtcpProtocolConstants.FeedbackMinimumSize;
        }
        else if (packetType == RtcpProtocolConstants.ExtendedReportPacketType)
        {
            packetSize = RtcpProtocolConstants.ExtendedReportMinimumSize;
        }

        byte[] datagram = new byte[packetSize];
        datagram[0] = 128;
        datagram[1] = packetType;
        int lengthInWordsMinusOne = packetSize / 4 - 1;
        datagram[2] = (byte)(lengthInWordsMinusOne >> 8);
        datagram[3] = (byte)lengthInWordsMinusOne;
        return datagram;
    }
}
