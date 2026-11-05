using rtp;

namespace rtp.Tests.Wire;

public class RtcpReferencePictureAndApplicationFeedbackCodecTests
{
    [Test]
    public void ReferencePictureSelectionRoundTripsNativeBitsAndPadding()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x83, 206, 0, 3,
            0x01, 0x02, 0x03, 0x04,
            0x05, 0x06, 0x07, 0x08,
            3, 96, 0xAA, 0xA8
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        RtcpReferencePictureSelectionIndication indication =
            packet.Feedback!.ReferencePictureSelectionIndication!;
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(packet.Feedback?.PayloadFormat,
                Is.EqualTo(
                    RtcpPayloadFeedbackFormat.ReferencePictureSelectionIndication));
            Assert.That(indication.PaddingBitCount, Is.EqualTo(3));
            Assert.That(indication.PayloadType, Is.EqualTo(96));
            Assert.That(indication.BitLength, Is.EqualTo(13));
            Assert.That(indication.NativeBitString,
                Is.EqualTo(new byte[] { 0xAA, 0xA8 }));
            Assert.That(encoded, Is.EqualTo(source));
        });
    }

    [Test]
    public void ReferencePictureSelectionIgnoresReservedBitAndWritesAllPaddingBits()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] sourceWithReservedBit = new byte[]
        {
            0x83, 206, 0, 3,
            0, 0, 0, 1,
            0, 0, 0, 2,
            3, 0xE0, 0xAA, 0xA8
        };
        RtcpPacket decoded = codec.DecodeRtcpPacket(sourceWithReservedBit);
        byte[] canonical = codec.EncodeRtcpPacketToArray(decoded);
        RtcpPacket oneBitPacket = CreateReferencePicturePacket(
            15,
            97,
            1,
            new byte[] { 0x80 });
        byte[] destination = new byte[16];
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = 77;
            index = index + 1;
        }

        int written = codec.EncodeRtcpPacket(oneBitPacket, destination);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Feedback?.ReferencePictureSelectionIndication?.
                PayloadType, Is.EqualTo(96));
            Assert.That(decoded.Feedback?.RawFeedbackControlInformation?[1],
                Is.EqualTo(0xE0));
            Assert.That(canonical,
                Is.EqualTo(new byte[]
                {
                    0x83, 206, 0, 3,
                    0, 0, 0, 1,
                    0, 0, 0, 2,
                    3, 96, 0xAA, 0xA8
                }));
            Assert.That(written, Is.EqualTo(16));
            Assert.That(destination,
                Is.EqualTo(new byte[]
                {
                    0x83, 206, 0, 3,
                    0, 0, 0, 1,
                    0, 0, 0, 2,
                    15, 97, 0x80, 0
                }));
        });
    }

    [Test]
    public void ReferencePictureSelectionRejectsMalformedWireAndModelValuesAtomically()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] emptyFci = new byte[]
        {
            0x83, 206, 0, 2,
            0, 0, 0, 1,
            0, 0, 0, 2
        };
        byte[] partialFci = new byte[]
        {
            0xA3, 206, 0, 3,
            0, 0, 0, 1,
            0, 0, 0, 2,
            3, 96, 0, 2
        };
        byte[] excessivePadding = new byte[]
        {
            0x83, 206, 0, 3,
            0, 0, 0, 1,
            0, 0, 0, 2,
            32, 96, 0, 0
        };
        byte[] nonzeroPadding = new byte[]
        {
            0x83, 206, 0, 3,
            0, 0, 0, 1,
            0, 0, 0, 2,
            3, 96, 0xAA, 0xAB
        };
        RtcpPacket[] invalidPackets = new RtcpPacket[]
        {
            CreateReferencePicturePacket(2, 96, 13,
                new byte[] { 0xAA, 0xA8 }),
            CreateReferencePicturePacket(3, 128, 13,
                new byte[] { 0xAA, 0xA8 }),
            CreateReferencePicturePacket(3, 96, -1, []),
            CreateReferencePicturePacket(3, 96, 13,
                new byte[] { 0xAA }),
            CreateReferencePicturePacket(3, 96, 13,
                new byte[] { 0xAA, 0xAF })
        };
        byte[] destination = new byte[16];
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
            Assert.That(codec.TryDecodeRtcpPacket(excessivePadding).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(nonzeroPadding).Success, Is.False);
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
    public void ApplicationFeedbackRoundTripsOpaqueAlignedData()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x8F, 206, 0, 4,
            0x01, 0x02, 0x03, 0x04,
            0x05, 0x06, 0x07, 0x08,
            0x52, 0x45, 0x4D, 0x42,
            0x00, 0xFF, 0x7E, 0x81
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        source[12] = 0;
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(packet.Feedback?.PayloadFormat,
                Is.EqualTo(RtcpPayloadFeedbackFormat.ApplicationFeedback));
            Assert.That(packet.Feedback?.ApplicationFeedback?.Data,
                Is.EqualTo(new byte[]
                {
                    0x52, 0x45, 0x4D, 0x42,
                    0x00, 0xFF, 0x7E, 0x81
                }));
            Assert.That(encoded,
                Is.EqualTo(new byte[]
                {
                    0x8F, 206, 0, 4,
                    0x01, 0x02, 0x03, 0x04,
                    0x05, 0x06, 0x07, 0x08,
                    0x52, 0x45, 0x4D, 0x42,
                    0x00, 0xFF, 0x7E, 0x81
                }));
        });
    }

    [Test]
    public void TypedApplicationFeedbackOverridesRawDataAndRejectsMisalignment()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] misalignedWireData = new byte[]
        {
            0xAF, 206, 0, 3,
            0, 0, 0, 1,
            0, 0, 0, 2,
            1, 2, 3, 1
        };
        RtcpPacket packet = CreateApplicationFeedbackPacket(
            new byte[] { 1, 2, 3, 4 });
        packet.Feedback!.RawFeedbackControlInformation =
            new byte[] { 9, 9, 9, 9 };
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
        RtcpPacket invalidPacket = CreateApplicationFeedbackPacket(
            new byte[] { 1, 2, 3 });
        byte[] destination = new byte[16];
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = 77;
            index = index + 1;
        }

        RtcpCodecResult result = codec.TryEncodeRtcpPacket(
            invalidPacket,
            destination);

        Assert.Multiple(() =>
        {
            Assert.That(codec.TryDecodeRtcpPacket(misalignedWireData).Success,
                Is.False);
            Assert.That(encoded,
                Is.EqualTo(new byte[]
                {
                    0x8F, 206, 0, 3,
                    0, 0, 0, 1,
                    0, 0, 0, 2,
                    1, 2, 3, 4
                }));
            Assert.That(result.Success, Is.False);
            Assert.That(result.WrittenCount, Is.Zero);
            Assert.That(destination, Has.All.EqualTo(77));
        });
    }

    static RtcpPacket CreateReferencePicturePacket(
        byte paddingBitCount,
        byte payloadType,
        int bitLength,
        byte[] nativeBitString)
    {
        RtcpReferencePictureSelectionIndication indication =
            new RtcpReferencePictureSelectionIndication();
        indication.PaddingBitCount = paddingBitCount;
        indication.PayloadType = payloadType;
        indication.BitLength = bitLength;
        indication.NativeBitString = nativeBitString;
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        feedback.PayloadFormat =
            RtcpPayloadFeedbackFormat.ReferencePictureSelectionIndication;
        feedback.SenderSourceIdentifier = 1U;
        feedback.MediaSourceIdentifier = 2U;
        feedback.ReferencePictureSelectionIndication = indication;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.PayloadFeedback;
        packet.Feedback = feedback;
        return packet;
    }

    static RtcpPacket CreateApplicationFeedbackPacket(byte[] data)
    {
        RtcpApplicationFeedback applicationFeedback =
            new RtcpApplicationFeedback();
        applicationFeedback.Data = data;
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        feedback.PayloadFormat = RtcpPayloadFeedbackFormat.ApplicationFeedback;
        feedback.SenderSourceIdentifier = 1U;
        feedback.MediaSourceIdentifier = 2U;
        feedback.ApplicationFeedback = applicationFeedback;
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
