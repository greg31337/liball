using rtp;

namespace rtp.Tests.Wire;

public class RtcpFullIntraAndRapidResynchronizationCodecTests
{
    [Test]
    public void FullIntraRequestRoundTripsMultipleTargetSequenceEntries()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x84, 206, 0, 6,
            0x01, 0x02, 0x03, 0x04,
            0, 0, 0, 0,
            0x11, 0x22, 0x33, 0x44,
            254, 0, 0, 0,
            0xAA, 0xBB, 0xCC, 0xDD,
            255, 0, 0, 0
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        RtcpFullIntraRequestEntry[] entries =
            packet.Feedback!.FullIntraRequest!.Entries;
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(packet.Feedback?.PayloadFormat,
                Is.EqualTo(RtcpPayloadFeedbackFormat.FullIntraRequest));
            Assert.That(packet.Feedback?.MediaSourceIdentifier, Is.Zero);
            Assert.That(entries.Length, Is.EqualTo(2));
            Assert.That(entries[0].SourceIdentifier, Is.EqualTo(0x11223344U));
            Assert.That(entries[0].SequenceNumber, Is.EqualTo(254));
            Assert.That(entries[1].SourceIdentifier, Is.EqualTo(0xAABBCCDDU));
            Assert.That(entries[1].SequenceNumber, Is.EqualTo(255));
            Assert.That(encoded, Is.EqualTo(source));
        });
    }

    [Test]
    public void FullIntraRequestIgnoresReservedBitsAndCanonicalizesThemToZero()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x84, 206, 0, 4,
            0, 0, 0, 1,
            0, 0, 0, 0,
            0x11, 0x22, 0x33, 0x44,
            7, 0xAA, 0xBB, 0xCC
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(packet.Feedback?.FullIntraRequest?.Entries[0].
                SequenceNumber, Is.EqualTo(7));
            Assert.That(packet.Feedback?.RawFeedbackControlInformation,
                Is.EqualTo(new byte[]
                {
                    0x11, 0x22, 0x33, 0x44,
                    7, 0xAA, 0xBB, 0xCC
                }));
            Assert.That(encoded,
                Is.EqualTo(new byte[]
                {
                    0x84, 206, 0, 4,
                    0, 0, 0, 1,
                    0, 0, 0, 0,
                    0x11, 0x22, 0x33, 0x44,
                    7, 0, 0, 0
                }));
        });
    }

    [Test]
    public void FullIntraRequestSequenceAdvancesForNewCommandsAndWraps()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpPacket packet = CreateFullIntraRequestPacket(
            new uint[] { 0x11223344U },
            new byte[] { 255 });

        byte[] firstTransmission = codec.EncodeRtcpPacketToArray(packet);
        byte[] repetition = codec.EncodeRtcpPacketToArray(packet);
        byte nextSequenceNumber =
            codec.AdvanceRtcpFullIntraRequestSequenceNumber(255);
        packet.Feedback!.FullIntraRequest!.Entries[0].SequenceNumber =
            nextSequenceNumber;
        byte[] newCommand = codec.EncodeRtcpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(repetition, Is.EqualTo(firstTransmission));
            Assert.That(codec.AdvanceRtcpFullIntraRequestSequenceNumber(254),
                Is.EqualTo(255));
            Assert.That(nextSequenceNumber, Is.Zero);
            Assert.That(newCommand[16], Is.Zero);
        });
    }

    [Test]
    public void FullIntraRequestRejectsMalformedAndDuplicateEntriesAtomically()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] emptyFci = new byte[]
        {
            0x84, 206, 0, 2,
            0, 0, 0, 1,
            0, 0, 0, 0
        };
        byte[] partialFci = new byte[]
        {
            0x84, 206, 0, 3,
            0, 0, 0, 1,
            0, 0, 0, 0,
            0x11, 0x22, 0x33, 0x44
        };
        byte[] nonzeroMediaSource = new byte[]
        {
            0x84, 206, 0, 4,
            0, 0, 0, 1,
            0, 0, 0, 2,
            0x11, 0x22, 0x33, 0x44,
            1, 0, 0, 0
        };
        byte[] duplicateTargets = new byte[]
        {
            0x84, 206, 0, 6,
            0, 0, 0, 1,
            0, 0, 0, 0,
            0x11, 0x22, 0x33, 0x44,
            1, 0, 0, 0,
            0x11, 0x22, 0x33, 0x44,
            2, 0, 0, 0
        };
        byte[] paddedFci = new byte[]
        {
            0xA4, 206, 0, 5,
            0, 0, 0, 1,
            0, 0, 0, 0,
            0x11, 0x22, 0x33, 0x44,
            1, 0, 0, 0,
            0, 0, 0, 4
        };
        RtcpPacket emptyPacket = CreateFullIntraRequestPacket([], []);
        RtcpPacket duplicatePacket = CreateFullIntraRequestPacket(
            new uint[] { 7U, 7U },
            new byte[] { 1, 2 });
        RtcpPacket invalidMediaPacket = CreateFullIntraRequestPacket(
            new uint[] { 7U },
            new byte[] { 1 });
        invalidMediaPacket.Feedback!.MediaSourceIdentifier = 9U;
        RtcpPacket paddedPacket = CreateFullIntraRequestPacket(
            new uint[] { 7U },
            new byte[] { 1 });
        paddedPacket.Header.HasPadding = true;
        paddedPacket.Padding = new byte[] { 0, 0, 0, 4 };
        RtcpPacket[] invalidPackets = new RtcpPacket[]
        {
            emptyPacket,
            duplicatePacket,
            invalidMediaPacket,
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
            Assert.That(codec.TryDecodeRtcpPacket(emptyFci).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(partialFci).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(nonzeroMediaSource).Success,
                Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(duplicateTargets).Success,
                Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(paddedFci).Success, Is.False);
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
    public void RapidResynchronizationRequestRoundTripsWithoutFci()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x85, 205, 0, 2,
            0x01, 0x02, 0x03, 0x04,
            0x05, 0x06, 0x07, 0x08
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(packet.Type, Is.EqualTo(RtcpPacketType.TransportFeedback));
            Assert.That(packet.Feedback?.TransportFormat,
                Is.EqualTo(
                    RtcpTransportFeedbackFormat.RapidResynchronizationRequest));
            Assert.That(packet.Feedback?.RapidResynchronizationRequest?.Requested,
                Is.True);
            Assert.That(packet.Feedback?.RawFeedbackControlInformation, Is.Empty);
            Assert.That(encoded, Is.EqualTo(source));
        });
    }

    [Test]
    public void RapidResynchronizationRequestRejectsFciPaddingAndInactiveRequests()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] fci = new byte[]
        {
            0x85, 205, 0, 3,
            0, 0, 0, 1,
            0, 0, 0, 2,
            0, 0, 0, 0
        };
        byte[] padded = new byte[]
        {
            0xA5, 205, 0, 3,
            0, 0, 0, 1,
            0, 0, 0, 2,
            0, 0, 0, 4
        };
        RtcpPacket inactivePacket = CreateRapidResynchronizationPacket(false);
        RtcpPacket packetWithFci = CreateRapidResynchronizationPacket(true);
        packetWithFci.Feedback!.RawFeedbackControlInformation =
            new byte[] { 0, 0, 0, 0 };
        RtcpPacket packetWithPadding = CreateRapidResynchronizationPacket(true);
        packetWithPadding.Header.HasPadding = true;
        packetWithPadding.Padding = new byte[] { 0, 0, 0, 4 };
        byte[] destination = new byte[16];
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = 77;
            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(codec.TryDecodeRtcpPacket(fci).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(padded).Success, Is.False);
            Assert.That(codec.TryEncodeRtcpPacket(inactivePacket, destination).Success,
                Is.False);
            Assert.That(codec.TryEncodeRtcpPacket(packetWithFci, destination).Success,
                Is.False);
            Assert.That(codec.TryEncodeRtcpPacket(
                packetWithPadding,
                destination).Success, Is.False);
            Assert.That(destination, Has.All.EqualTo(77));
        });
    }

    static RtcpPacket CreateFullIntraRequestPacket(
        uint[] sourceIdentifiers,
        byte[] sequenceNumbers)
    {
        RtcpFullIntraRequest fullIntraRequest = new RtcpFullIntraRequest();
        fullIntraRequest.Entries =
            new RtcpFullIntraRequestEntry[sourceIdentifiers.Length];
        int index = 0;
        while (index < sourceIdentifiers.Length)
        {
            RtcpFullIntraRequestEntry entry =
                new RtcpFullIntraRequestEntry();
            entry.SourceIdentifier = sourceIdentifiers[index];
            entry.SequenceNumber = sequenceNumbers[index];
            fullIntraRequest.Entries[index] = entry;
            index = index + 1;
        }

        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        feedback.PayloadFormat = RtcpPayloadFeedbackFormat.FullIntraRequest;
        feedback.SenderSourceIdentifier = 1U;
        feedback.MediaSourceIdentifier = 0U;
        feedback.FullIntraRequest = fullIntraRequest;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.PayloadFeedback;
        packet.Feedback = feedback;
        return packet;
    }

    static RtcpPacket CreateRapidResynchronizationPacket(bool requested)
    {
        RtcpRapidResynchronizationRequest request =
            new RtcpRapidResynchronizationRequest();
        request.Requested = requested;
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

    static IRtcpPacketCodec CreateCodec()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.StrictValidation = true;
        configuration.PreservePadding = true;
        configuration.MaximumRtcpCompoundPacketSize = 65535;
        return new RtcpPacketCodec(configuration);
    }
}
