using rtp;

namespace rtp.Tests.Wire;

public class RtcpLaterFeedbackBoundaryTests
{
    [Test]
    public void EverySupportedLaterFeedbackMessageRoundTripsAsTypedFeedback()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        RtcpPacket[] packets = new RtcpPacket[]
        {
            CreateBitratePacket(false, 1, 1U, 1),
            CreateBitratePacket(true, 1, 1U, 1),
            CreateFullIntraRequestPacket(1),
            CreateTradeoffPacket(false, 1, 1),
            CreateTradeoffPacket(true, 1, 1),
            CreateRapidResynchronizationPacket(),
            CreateCongestionControlPacket([], 0x01020304U)
        };
        byte[] expectedFormats = new byte[]
        {
            3, 4, 4, 5, 6, 5, 11
        };
        byte[] expectedPacketTypes = new byte[]
        {
            205, 205, 206, 206, 206, 205, 205
        };

        int index = 0;
        while (index < packets.Length)
        {
            byte[] encoded = codec.EncodeRtcpPacketToArray(packets[index]);
            RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);

            Assert.Multiple(() =>
            {
                Assert.That(encoded[0] & 31, Is.EqualTo(expectedFormats[index]));
                Assert.That(encoded[1], Is.EqualTo(expectedPacketTypes[index]));
                Assert.That(codec.EncodeRtcpPacketToArray(decoded),
                    Is.EqualTo(encoded));
                Assert.That(decoded.Feedback, Is.Not.Null);
            });

            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(codec.DecodeRtcpPacket(
                codec.EncodeRtcpPacketToArray(packets[0])).Feedback?.BitrateControl,
                Is.Not.Null);
            Assert.That(codec.DecodeRtcpPacket(
                codec.EncodeRtcpPacketToArray(packets[1])).Feedback?.BitrateControl,
                Is.Not.Null);
            Assert.That(codec.DecodeRtcpPacket(
                codec.EncodeRtcpPacketToArray(packets[2])).Feedback?.FullIntraRequest,
                Is.Not.Null);
            Assert.That(codec.DecodeRtcpPacket(
                codec.EncodeRtcpPacketToArray(packets[3])).Feedback?
                    .TemporalSpatialTradeoffs,
                Is.Not.Null);
            Assert.That(codec.DecodeRtcpPacket(
                codec.EncodeRtcpPacketToArray(packets[4])).Feedback?
                    .TemporalSpatialTradeoffs,
                Is.Not.Null);
            Assert.That(codec.DecodeRtcpPacket(
                codec.EncodeRtcpPacketToArray(packets[5])).Feedback?
                    .RapidResynchronizationRequest,
                Is.Not.Null);
            Assert.That(codec.DecodeRtcpPacket(
                codec.EncodeRtcpPacketToArray(packets[6])).Feedback?
                    .CongestionControlFeedback,
                Is.Not.Null);
        });
    }

    [Test]
    public void BitrateExponentMantissaAndOverheadCoverEveryBitAndLimit()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        int exponent = 0;
        while (exponent <= 63)
        {
            AssertBitrateFields(codec, exponent % 2 != 0, (byte)exponent,
                131071U, 511);
            exponent = exponent + 1;
        }

        AssertBitrateFields(codec, false, 0, 0U, 0);
        int mantissaBit = 0;
        while (mantissaBit < 17)
        {
            AssertBitrateFields(codec, mantissaBit % 2 != 0, 63,
                1U << mantissaBit, 511);
            mantissaBit = mantissaBit + 1;
        }

        int overheadBit = 0;
        while (overheadBit < 9)
        {
            AssertBitrateFields(codec, overheadBit % 2 == 0, 63, 131071U,
                (ushort)(1 << overheadBit));
            overheadBit = overheadBit + 1;
        }
    }

    [Test]
    public void FirAndTradeoffSequencesCoverTheFullOctetAndWrap()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        int value = 0;
        while (value <= 255)
        {
            byte sequenceNumber = (byte)value;
            byte tradeoffIndex = (byte)(value & 31);
            RtcpPacket fir = codec.DecodeRtcpPacket(
                codec.EncodeRtcpPacketToArray(
                    CreateFullIntraRequestPacket(sequenceNumber)));
            RtcpPacket request = codec.DecodeRtcpPacket(
                codec.EncodeRtcpPacketToArray(
                    CreateTradeoffPacket(false, sequenceNumber, tradeoffIndex)));
            RtcpPacket notification = codec.DecodeRtcpPacket(
                codec.EncodeRtcpPacketToArray(
                    CreateTradeoffPacket(true, sequenceNumber, tradeoffIndex)));

            Assert.Multiple(() =>
            {
                Assert.That(fir.Feedback?.FullIntraRequest?.Entries[0]
                    .SequenceNumber, Is.EqualTo(sequenceNumber));
                Assert.That(request.Feedback?.TemporalSpatialTradeoffs[0]
                    .SequenceNumber, Is.EqualTo(sequenceNumber));
                Assert.That(notification.Feedback?.TemporalSpatialTradeoffs[0]
                    .SequenceNumber, Is.EqualTo(sequenceNumber));
                Assert.That(codec.AdvanceRtcpFullIntraRequestSequenceNumber(
                    sequenceNumber), Is.EqualTo((byte)((value + 1) & 255)));
            });

            value = value + 1;
        }
    }

    [Test]
    public void CongestionReportSymbolsCoverLossEcnArrivalAndSequenceWrap()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        RtcpCongestionControlReport report = new RtcpCongestionControlReport();
        report.SourceIdentifier = 0x11223344U;
        report.BeginSequenceNumber = 65533;
        report.ReportCount = 7;
        report.Received = new bool[]
        {
            false, true, true, true, true, true, true
        };
        report.EcnMarks = new byte[] { 0, 0, 1, 2, 3, 3, 0 };
        report.ArrivalTimeOffsets = new ushort[]
        {
            0, 0, 1, 0x1FFD, 0x1FFE, 0x1FFF, 0x1FFF
        };
        ushort[] expectedSymbols = new ushort[]
        {
            0, 0x8000, 0xA001, 0xDFFD, 0xFFFE, 0xFFFF, 0x9FFF
        };
        RtcpPacket packet = CreateCongestionControlPacket(
            new RtcpCongestionControlReport[] { report },
            uint.MaxValue);

        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
        RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);
        RtcpCongestionControlReport decodedReport = decoded.Feedback!
            .CongestionControlFeedback!.Reports[0];

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < expectedSymbols.Length)
            {
                Assert.That(ReadUInt16(encoded, 16 + (index * 2)),
                    Is.EqualTo(expectedSymbols[index]));
                Assert.That(codec.GetRtcpCongestionControlSequenceNumber(
                    decodedReport,
                    index), Is.EqualTo((ushort)((65533 + index) & 65535)));
                index = index + 1;
            }

            Assert.That(encoded[30], Is.Zero);
            Assert.That(encoded[31], Is.Zero);
            Assert.That(decodedReport.Received, Is.EqualTo(report.Received));
            Assert.That(decodedReport.EcnMarks, Is.EqualTo(report.EcnMarks));
            Assert.That(decodedReport.ArrivalTimeOffsets,
                Is.EqualTo(report.ArrivalTimeOffsets));
            Assert.That(decoded.Feedback?.CongestionControlFeedback?
                .ReportTimestamp, Is.EqualTo(uint.MaxValue));
        });
    }

    [Test]
    public void CongestionTimestampAndMaximumReportCountReachWireLimits()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        uint[] timestamps = new uint[]
        {
            uint.MaxValue - 1U, uint.MaxValue, 0U, 1U
        };
        int timestampIndex = 0;
        while (timestampIndex < timestamps.Length)
        {
            RtcpPacket timestampPacket = CreateCongestionControlPacket(
                [], timestamps[timestampIndex]);
            byte[] timestampWire = codec.EncodeRtcpPacketToArray(timestampPacket);
            RtcpPacket timestampDecoded = codec.DecodeRtcpPacket(timestampWire);

            Assert.Multiple(() =>
            {
                Assert.That(ReadUInt32(timestampWire, 8),
                    Is.EqualTo(timestamps[timestampIndex]));
                Assert.That(timestampDecoded.Feedback?.CongestionControlFeedback?
                    .ReportTimestamp, Is.EqualTo(timestamps[timestampIndex]));
            });

            timestampIndex = timestampIndex + 1;
        }

        RtcpCongestionControlReport maximumReport =
            new RtcpCongestionControlReport();
        maximumReport.SourceIdentifier = 1U;
        maximumReport.BeginSequenceNumber = 65535;
        maximumReport.ReportCount = 16384;
        maximumReport.Received = new bool[16384];
        maximumReport.EcnMarks = new byte[16384];
        maximumReport.ArrivalTimeOffsets = new ushort[16384];
        RtcpPacket maximumPacket = CreateCongestionControlPacket(
            new RtcpCongestionControlReport[] { maximumReport }, 0U);

        byte[] maximumWire = codec.EncodeRtcpPacketToArray(maximumPacket);
        RtcpCongestionControlReport maximumDecoded = codec.DecodeRtcpPacket(
            maximumWire).Feedback!.CongestionControlFeedback!.Reports[0];

        Assert.Multiple(() =>
        {
            Assert.That(maximumWire.Length, Is.EqualTo(32788));
            Assert.That(maximumDecoded.ReportCount, Is.EqualTo(16384));
            Assert.That(codec.GetRtcpCongestionControlSequenceNumber(
                maximumDecoded, 0), Is.EqualTo(65535));
            Assert.That(codec.GetRtcpCongestionControlSequenceNumber(
                maximumDecoded, 1), Is.Zero);
            Assert.That(codec.GetRtcpCongestionControlSequenceNumber(
                maximumDecoded, 16383), Is.EqualTo(16382));
        });
    }

    [Test]
    public void CongestionPaddingRoundTripsAndFixedFeedbackRejectsPadding()
    {
        int paddingLength = 4;
        while (paddingLength <= 252)
        {
            IRtcpPacketCodec codec = CreateCodec(true);
            RtcpPacket packet = CreateCongestionControlPacket([], 7U);
            packet.Header.HasPadding = true;
            packet.Padding = new byte[paddingLength];
            packet.Padding[paddingLength - 1] = (byte)paddingLength;

            byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
            RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);

            Assert.Multiple(() =>
            {
                Assert.That(decoded.Header.HasPadding, Is.True);
                Assert.That(decoded.Padding, Is.EqualTo(packet.Padding));
                Assert.That(codec.EncodeRtcpPacketToArray(decoded),
                    Is.EqualTo(encoded));
            });

            paddingLength = paddingLength + 4;
        }

        RtcpPacket[] fixedFeedback = new RtcpPacket[]
        {
            CreateBitratePacket(false, 1, 1U, 1),
            CreateBitratePacket(true, 1, 1U, 1),
            CreateFullIntraRequestPacket(1),
            CreateTradeoffPacket(false, 1, 1),
            CreateTradeoffPacket(true, 1, 1),
            CreateRapidResynchronizationPacket()
        };
        IRtcpPacketCodec fixedCodec = CreateCodec(true);
        int packetIndex = 0;
        while (packetIndex < fixedFeedback.Length)
        {
            fixedFeedback[packetIndex].Header.HasPadding = true;
            fixedFeedback[packetIndex].Padding = new byte[] { 0, 0, 0, 4 };
            byte[] destination = new byte[64];
            RtcpCodecResult result = fixedCodec.TryEncodeRtcpPacket(
                fixedFeedback[packetIndex], destination);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.WrittenCount, Is.Zero);
                Assert.That(destination, Has.All.Zero);
            });

            packetIndex = packetIndex + 1;
        }
    }

    [Test]
    public void EveryLaterFeedbackDecoderRejectsAllPartialFciRemainders()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        byte[] packetTypes = new byte[] { 205, 205, 206, 206, 206, 205, 205 };
        byte[] formats = new byte[] { 3, 4, 4, 5, 6, 5, 11 };
        int messageIndex = 0;
        while (messageIndex < formats.Length)
        {
            int fciLength = 1;
            while (fciLength < 8)
            {
                byte[] malformed = CreatePaddedPartialFci(
                    packetTypes[messageIndex],
                    formats[messageIndex],
                    fciLength);
                RtcpCodecResult result = codec.TryDecodeRtcpPacket(malformed);

                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.False,
                        $"PT={packetTypes[messageIndex]}, FMT={formats[messageIndex]}, FCI={fciLength}");
                    Assert.That(result.ConsumedCount, Is.Zero);
                });

                fciLength = fciLength + 1;
            }

            messageIndex = messageIndex + 1;
        }
    }

    static void AssertBitrateFields(
        IRtcpPacketCodec codec,
        bool notification,
        byte exponent,
        uint mantissa,
        ushort overhead)
    {
        RtcpPacket packet = CreateBitratePacket(
            notification, exponent, mantissa, overhead);
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
        RtcpBitrateControlEntry decoded = codec.DecodeRtcpPacket(encoded)
            .Feedback!.BitrateControl!.Entries[0];
        uint packed = ((uint)exponent << 26) | (mantissa << 9) | overhead;

        Assert.Multiple(() =>
        {
            Assert.That(ReadUInt32(encoded, 16), Is.EqualTo(packed));
            Assert.That(decoded.Exponent, Is.EqualTo(exponent));
            Assert.That(decoded.Mantissa, Is.EqualTo(mantissa));
            Assert.That(decoded.Overhead, Is.EqualTo(overhead));
        });
    }

    static RtcpPacket CreateBitratePacket(
        bool notification,
        byte exponent,
        uint mantissa,
        ushort overhead)
    {
        RtcpBitrateControlEntry entry = new RtcpBitrateControlEntry();
        entry.SourceIdentifier = 0x11223344U;
        entry.Exponent = exponent;
        entry.Mantissa = mantissa;
        entry.Overhead = overhead;
        RtcpBitrateControl control = new RtcpBitrateControl();
        control.IsNotification = notification;
        control.Entries = new RtcpBitrateControlEntry[] { entry };
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.TransportLayer;
        if (notification)
        {
            feedback.TransportFormat =
                RtcpTransportFeedbackFormat.TemporaryMaximumBitrateNotification;
        }
        else
        {
            feedback.TransportFormat =
                RtcpTransportFeedbackFormat.TemporaryMaximumBitrateRequest;
        }

        feedback.SenderSourceIdentifier = 1U;
        feedback.MediaSourceIdentifier = 0U;
        feedback.BitrateControl = control;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.TransportFeedback;
        packet.Feedback = feedback;
        return packet;
    }

    static RtcpPacket CreateFullIntraRequestPacket(byte sequenceNumber)
    {
        RtcpFullIntraRequestEntry entry = new RtcpFullIntraRequestEntry();
        entry.SourceIdentifier = 0x11223344U;
        entry.SequenceNumber = sequenceNumber;
        RtcpFullIntraRequest request = new RtcpFullIntraRequest();
        request.Entries = new RtcpFullIntraRequestEntry[] { entry };
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        feedback.PayloadFormat = RtcpPayloadFeedbackFormat.FullIntraRequest;
        feedback.SenderSourceIdentifier = 1U;
        feedback.MediaSourceIdentifier = 0U;
        feedback.FullIntraRequest = request;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.PayloadFeedback;
        packet.Feedback = feedback;
        return packet;
    }

    static RtcpPacket CreateTradeoffPacket(
        bool notification,
        byte sequenceNumber,
        byte tradeoffIndex)
    {
        RtcpTemporalSpatialTradeoff tradeoff =
            new RtcpTemporalSpatialTradeoff();
        tradeoff.IsNotification = notification;
        tradeoff.SourceIdentifier = 0x11223344U;
        tradeoff.SequenceNumber = sequenceNumber;
        tradeoff.TradeoffIndex = tradeoffIndex;
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        if (notification)
        {
            feedback.PayloadFormat =
                RtcpPayloadFeedbackFormat.TemporalSpatialTradeoffNotification;
        }
        else
        {
            feedback.PayloadFormat =
                RtcpPayloadFeedbackFormat.TemporalSpatialTradeoffRequest;
        }

        feedback.SenderSourceIdentifier = 1U;
        feedback.MediaSourceIdentifier = 0U;
        feedback.TemporalSpatialTradeoffs =
            new RtcpTemporalSpatialTradeoff[] { tradeoff };
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.PayloadFeedback;
        packet.Feedback = feedback;
        return packet;
    }

    static RtcpPacket CreateRapidResynchronizationPacket()
    {
        RtcpRapidResynchronizationRequest request =
            new RtcpRapidResynchronizationRequest();
        request.Requested = true;
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.TransportLayer;
        feedback.TransportFormat =
            RtcpTransportFeedbackFormat.RapidResynchronizationRequest;
        feedback.SenderSourceIdentifier = 1U;
        feedback.MediaSourceIdentifier = 2U;
        feedback.RapidResynchronizationRequest = request;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.TransportFeedback;
        packet.Feedback = feedback;
        return packet;
    }

    static RtcpPacket CreateCongestionControlPacket(
        RtcpCongestionControlReport[] reports,
        uint timestamp)
    {
        RtcpCongestionControlFeedback congestion =
            new RtcpCongestionControlFeedback();
        congestion.Reports = reports;
        congestion.ReportTimestamp = timestamp;
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.TransportLayer;
        feedback.TransportFormat =
            RtcpTransportFeedbackFormat.CongestionControlFeedback;
        feedback.SenderSourceIdentifier = 1U;
        feedback.MediaSourceIdentifier = 0U;
        feedback.CongestionControlFeedback = congestion;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.TransportFeedback;
        packet.Feedback = feedback;
        return packet;
    }

    static byte[] CreatePaddedPartialFci(
        byte packetType,
        byte format,
        int fciLength)
    {
        byte[] packet = new byte[20];
        packet[0] = (byte)(0xA0 | format);
        packet[1] = packetType;
        packet[3] = 4;
        packet[7] = 1;
        int index = 0;
        while (index < fciLength)
        {
            packet[12 + index] = (byte)(index + 1);
            index = index + 1;
        }

        packet[19] = (byte)(8 - fciLength);
        return packet;
    }

    static ushort ReadUInt16(byte[] source, int offset)
    {
        return (ushort)((source[offset] << 8) | source[offset + 1]);
    }

    static uint ReadUInt32(byte[] source, int offset)
    {
        return ((uint)source[offset] << 24)
            | ((uint)source[offset + 1] << 16)
            | ((uint)source[offset + 2] << 8)
            | source[offset + 3];
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
