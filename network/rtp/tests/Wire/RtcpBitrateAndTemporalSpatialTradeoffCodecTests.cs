using rtp;

namespace rtp.Tests.Wire;

public class RtcpBitrateAndTemporalSpatialTradeoffCodecTests
{
    [Test]
    public void TemporaryMaximumBitrateRequestRoundTripsFieldBoundaries()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x83, 205, 0, 6,
            0x01, 0x02, 0x03, 0x04,
            0, 0, 0, 0,
            0x11, 0x22, 0x33, 0x44,
            0, 0, 0, 0,
            0xAA, 0xBB, 0xCC, 0xDD,
            0xFF, 0xFF, 0xFF, 0xFF
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        RtcpBitrateControl control = packet.Feedback!.BitrateControl!;
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(packet.Feedback?.TransportFormat,
                Is.EqualTo(
                    RtcpTransportFeedbackFormat.TemporaryMaximumBitrateRequest));
            Assert.That(control.IsNotification, Is.False);
            Assert.That(control.Entries.Length, Is.EqualTo(2));
            Assert.That(control.Entries[0].Exponent, Is.Zero);
            Assert.That(control.Entries[0].Mantissa, Is.Zero);
            Assert.That(control.Entries[0].Overhead, Is.Zero);
            Assert.That(control.Entries[1].SourceIdentifier,
                Is.EqualTo(0xAABBCCDDU));
            Assert.That(control.Entries[1].Exponent, Is.EqualTo(63));
            Assert.That(control.Entries[1].Mantissa, Is.EqualTo(131071U));
            Assert.That(control.Entries[1].Overhead, Is.EqualTo(511));
            Assert.That(encoded, Is.EqualTo(source));
        });
    }

    [Test]
    public void TemporaryMaximumBitrateNotificationAllowsAnEmptyBoundingSet()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] emptyNotification = new byte[]
        {
            0x84, 205, 0, 2,
            0x01, 0x02, 0x03, 0x04,
            0, 0, 0, 0
        };

        RtcpPacket decoded = codec.DecodeRtcpPacket(emptyNotification);
        byte[] roundTrip = codec.EncodeRtcpPacketToArray(decoded);
        RtcpPacket typed = CreateBitratePacket(
            true,
            new RtcpBitrateControlEntry[]
            {
                CreateBitrateEntry(7U, 5, 0x12345U, 0x101)
            });
        typed.Feedback!.RawFeedbackControlInformation =
            new byte[] { 9, 9, 9, 9, 9, 9, 9, 9 };
        byte[] encoded = codec.EncodeRtcpPacketToArray(typed);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Feedback?.BitrateControl?.IsNotification, Is.True);
            Assert.That(decoded.Feedback?.BitrateControl?.Entries, Is.Empty);
            Assert.That(roundTrip, Is.EqualTo(emptyNotification));
            Assert.That(encoded,
                Is.EqualTo(new byte[]
                {
                    0x84, 205, 0, 4,
                    0, 0, 0, 1,
                    0, 0, 0, 0,
                    0, 0, 0, 7,
                    0x16, 0x46, 0x8B, 0x01
                }));
        });
    }

    [Test]
    public void BitrateControlRejectsMalformedFciAndOutOfRangeFieldsAtomically()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] emptyRequest = new byte[]
        {
            0x83, 205, 0, 2,
            0, 0, 0, 1,
            0, 0, 0, 0
        };
        byte[] partialRequest = new byte[]
        {
            0x83, 205, 0, 3,
            0, 0, 0, 1,
            0, 0, 0, 0,
            0, 0, 0, 7
        };
        byte[] paddedRequest = new byte[]
        {
            0xA3, 205, 0, 5,
            0, 0, 0, 1,
            0, 0, 0, 0,
            0, 0, 0, 7,
            0, 0, 0, 0,
            0, 0, 0, 4
        };
        RtcpPacket wrongKind = CreateBitratePacket(true,
            new RtcpBitrateControlEntry[]
            {
                CreateBitrateEntry(7U, 1, 1U, 1)
            });
        wrongKind.Feedback!.TransportFormat =
            RtcpTransportFeedbackFormat.TemporaryMaximumBitrateRequest;
        RtcpPacket nonzeroMediaSource = CreateBitratePacket(false,
            new RtcpBitrateControlEntry[]
            {
                CreateBitrateEntry(7U, 1, 1U, 1)
            });
        nonzeroMediaSource.Feedback!.MediaSourceIdentifier = 8U;
        RtcpPacket paddedPacket = CreateBitratePacket(false,
            new RtcpBitrateControlEntry[]
            {
                CreateBitrateEntry(7U, 1, 1U, 1)
            });
        paddedPacket.Header.HasPadding = true;
        paddedPacket.Padding = new byte[] { 0, 0, 0, 4 };
        RtcpPacket[] invalidPackets = new RtcpPacket[]
        {
            CreateBitratePacket(false, []),
            CreateBitratePacket(false,
                new RtcpBitrateControlEntry[]
                {
                    CreateBitrateEntry(7U, 64, 1U, 1)
                }),
            CreateBitratePacket(false,
                new RtcpBitrateControlEntry[]
                {
                    CreateBitrateEntry(7U, 1, 131072U, 1)
                }),
            CreateBitratePacket(false,
                new RtcpBitrateControlEntry[]
                {
                    CreateBitrateEntry(7U, 1, 1U, 512)
                }),
            wrongKind,
            nonzeroMediaSource,
            paddedPacket
        };
        byte[] destination = new byte[20];
        int destinationIndex = 0;
        while (destinationIndex < destination.Length)
        {
            destination[destinationIndex] = 77;
            destinationIndex = destinationIndex + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(codec.TryDecodeRtcpPacket(emptyRequest).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(partialRequest).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(paddedRequest).Success, Is.False);
            int packetIndex = 0;
            while (packetIndex < invalidPackets.Length)
            {
                RtcpCodecResult result = codec.TryEncodeRtcpPacket(
                    invalidPackets[packetIndex],
                    destination);
                Assert.That(result.Success, Is.False);
                Assert.That(result.WrittenCount, Is.Zero);
                packetIndex = packetIndex + 1;
            }

            Assert.That(destination, Has.All.EqualTo(77));
        });
    }

    [Test]
    public void TemporalSpatialTradeoffRequestRoundTripsMultipleEntries()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x85, 206, 0, 6,
            0x01, 0x02, 0x03, 0x04,
            0, 0, 0, 0,
            0x11, 0x22, 0x33, 0x44,
            254, 0, 0, 0,
            0xAA, 0xBB, 0xCC, 0xDD,
            255, 0, 0, 31
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        RtcpTemporalSpatialTradeoff[] entries =
            packet.Feedback!.TemporalSpatialTradeoffs;
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(entries.Length, Is.EqualTo(2));
            Assert.That(entries[0].IsNotification, Is.False);
            Assert.That(entries[0].SourceIdentifier, Is.EqualTo(0x11223344U));
            Assert.That(entries[0].SequenceNumber, Is.EqualTo(254));
            Assert.That(entries[0].TradeoffIndex, Is.Zero);
            Assert.That(entries[1].SequenceNumber, Is.EqualTo(255));
            Assert.That(entries[1].TradeoffIndex, Is.EqualTo(31));
            Assert.That(encoded, Is.EqualTo(source));
        });
    }

    [Test]
    public void TemporalSpatialTradeoffNotificationIgnoresReservedBits()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x86, 206, 0, 6,
            0, 0, 0, 1,
            0, 0, 0, 0,
            0x11, 0x11, 0x11, 0x11,
            7, 0xAA, 0xBB, 0xD1,
            0x22, 0x22, 0x22, 0x22,
            8, 0xCC, 0xDD, 0xF1
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        RtcpTemporalSpatialTradeoff[] entries =
            packet.Feedback!.TemporalSpatialTradeoffs;
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(entries[0].IsNotification, Is.True);
            Assert.That(entries[0].TradeoffIndex, Is.EqualTo(17));
            Assert.That(entries[1].TradeoffIndex, Is.EqualTo(17));
            Assert.That(packet.Feedback?.RawFeedbackControlInformation,
                Is.EqualTo(new byte[]
                {
                    0x11, 0x11, 0x11, 0x11,
                    7, 0xAA, 0xBB, 0xD1,
                    0x22, 0x22, 0x22, 0x22,
                    8, 0xCC, 0xDD, 0xF1
                }));
            Assert.That(encoded,
                Is.EqualTo(new byte[]
                {
                    0x86, 206, 0, 6,
                    0, 0, 0, 1,
                    0, 0, 0, 0,
                    0x11, 0x11, 0x11, 0x11,
                    7, 0, 0, 17,
                    0x22, 0x22, 0x22, 0x22,
                    8, 0, 0, 17
                }));
        });
    }

    [Test]
    public void TemporalSpatialTradeoffRejectsInvalidEntriesAndFramingAtomically()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] emptyRequest = new byte[]
        {
            0x85, 206, 0, 2,
            0, 0, 0, 1,
            0, 0, 0, 0
        };
        byte[] partialNotification = new byte[]
        {
            0x86, 206, 0, 3,
            0, 0, 0, 1,
            0, 0, 0, 0,
            0, 0, 0, 7
        };
        byte[] inconsistentNotification = new byte[]
        {
            0x86, 206, 0, 6,
            0, 0, 0, 1,
            0, 0, 0, 0,
            0, 0, 0, 7,
            1, 0, 0, 4,
            0, 0, 0, 8,
            2, 0, 0, 5
        };
        byte[] paddedRequest = new byte[]
        {
            0xA5, 206, 0, 5,
            0, 0, 0, 1,
            0, 0, 0, 0,
            0, 0, 0, 7,
            1, 0, 0, 4,
            0, 0, 0, 4
        };
        RtcpPacket outOfRange = CreateTradeoffPacket(
            false,
            new uint[] { 7U },
            new byte[] { 1 },
            new byte[] { 32 });
        RtcpPacket wrongKind = CreateTradeoffPacket(
            true,
            new uint[] { 7U },
            new byte[] { 1 },
            new byte[] { 4 });
        wrongKind.Feedback!.PayloadFormat =
            RtcpPayloadFeedbackFormat.TemporalSpatialTradeoffRequest;
        RtcpPacket duplicate = CreateTradeoffPacket(
            false,
            new uint[] { 7U, 7U },
            new byte[] { 1, 2 },
            new byte[] { 4, 5 });
        RtcpPacket nonzeroMediaSource = CreateTradeoffPacket(
            false,
            new uint[] { 7U },
            new byte[] { 1 },
            new byte[] { 4 });
        nonzeroMediaSource.Feedback!.MediaSourceIdentifier = 8U;
        RtcpPacket paddedPacket = CreateTradeoffPacket(
            false,
            new uint[] { 7U },
            new byte[] { 1 },
            new byte[] { 4 });
        paddedPacket.Header.HasPadding = true;
        paddedPacket.Padding = new byte[] { 0, 0, 0, 4 };
        RtcpPacket[] invalidPackets = new RtcpPacket[]
        {
            outOfRange,
            wrongKind,
            duplicate,
            nonzeroMediaSource,
            paddedPacket
        };
        byte[] destination = new byte[28];
        int destinationIndex = 0;
        while (destinationIndex < destination.Length)
        {
            destination[destinationIndex] = 77;
            destinationIndex = destinationIndex + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(codec.TryDecodeRtcpPacket(emptyRequest).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(partialNotification).Success,
                Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(
                inconsistentNotification).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(paddedRequest).Success,
                Is.False);
            int packetIndex = 0;
            while (packetIndex < invalidPackets.Length)
            {
                RtcpCodecResult result = codec.TryEncodeRtcpPacket(
                    invalidPackets[packetIndex],
                    destination);
                Assert.That(result.Success, Is.False);
                Assert.That(result.WrittenCount, Is.Zero);
                packetIndex = packetIndex + 1;
            }

            Assert.That(destination, Has.All.EqualTo(77));
        });
    }

    static RtcpBitrateControlEntry CreateBitrateEntry(
        uint sourceIdentifier,
        byte exponent,
        uint mantissa,
        ushort overhead)
    {
        RtcpBitrateControlEntry entry = new RtcpBitrateControlEntry();
        entry.SourceIdentifier = sourceIdentifier;
        entry.Exponent = exponent;
        entry.Mantissa = mantissa;
        entry.Overhead = overhead;
        return entry;
    }

    static RtcpPacket CreateBitratePacket(
        bool isNotification,
        RtcpBitrateControlEntry[] entries)
    {
        RtcpBitrateControl control = new RtcpBitrateControl();
        control.IsNotification = isNotification;
        control.Entries = entries;
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.TransportLayer;
        if (isNotification)
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

    static RtcpPacket CreateTradeoffPacket(
        bool isNotification,
        uint[] sourceIdentifiers,
        byte[] sequenceNumbers,
        byte[] indices)
    {
        RtcpTemporalSpatialTradeoff[] tradeoffs =
            new RtcpTemporalSpatialTradeoff[sourceIdentifiers.Length];
        int index = 0;
        while (index < tradeoffs.Length)
        {
            RtcpTemporalSpatialTradeoff tradeoff =
                new RtcpTemporalSpatialTradeoff();
            tradeoff.IsNotification = isNotification;
            tradeoff.SourceIdentifier = sourceIdentifiers[index];
            tradeoff.SequenceNumber = sequenceNumbers[index];
            tradeoff.TradeoffIndex = indices[index];
            tradeoffs[index] = tradeoff;
            index = index + 1;
        }

        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        if (isNotification)
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
        feedback.TemporalSpatialTradeoffs = tradeoffs;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.PayloadFeedback;
        packet.Feedback = feedback;
        return packet;
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
