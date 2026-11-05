using rtp;

namespace rtp.Tests.Wire;

public class RtcpPictureAndSliceLossCodecTests
{
    [Test]
    public void PictureLossIndicationRoundTripsWithoutFci()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x81, 206, 0, 2,
            0x01, 0x02, 0x03, 0x04,
            0x05, 0x06, 0x07, 0x08
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(packet.Type, Is.EqualTo(RtcpPacketType.PayloadFeedback));
            Assert.That(packet.Feedback?.PayloadFormat,
                Is.EqualTo(RtcpPayloadFeedbackFormat.PictureLossIndication));
            Assert.That(packet.Feedback?.PictureLossIndication?.Requested, Is.True);
            Assert.That(packet.Feedback?.RawFeedbackControlInformation, Is.Empty);
            Assert.That(encoded, Is.EqualTo(source));
        });
    }

    [Test]
    public void PictureLossIndicationRejectsFciPaddingAndInactiveRequests()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] fci = new byte[]
        {
            0x81, 206, 0, 3,
            0, 0, 0, 1,
            0, 0, 0, 2,
            0, 0, 0, 0
        };
        byte[] padded = new byte[]
        {
            0xA1, 206, 0, 3,
            0, 0, 0, 1,
            0, 0, 0, 2,
            0, 0, 0, 4
        };
        RtcpPacket packet = CreatePictureLossPacket(false);
        RtcpPacket packetWithFci = CreatePictureLossPacket(true);
        packetWithFci.Feedback!.RawFeedbackControlInformation =
            new byte[] { 0, 0, 0, 0 };
        RtcpPacket packetWithPadding = CreatePictureLossPacket(true);
        packetWithPadding.Header.HasPadding = true;
        packetWithPadding.Padding = new byte[] { 0, 0, 0, 4 };

        Assert.Multiple(() =>
        {
            Assert.That(codec.TryDecodeRtcpPacket(fci).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(padded).Success, Is.False);
            Assert.That(codec.TryEncodeRtcpPacket(packet, new byte[12]).Success,
                Is.False);
            Assert.That(codec.TryEncodeRtcpPacket(packetWithFci, new byte[16]).Success,
                Is.False);
            Assert.That(codec.TryEncodeRtcpPacket(
                packetWithPadding,
                new byte[16]).Success, Is.False);
        });
    }

    [Test]
    public void SliceLossIndicationRoundTripsMinimumAndMaximumFieldValues()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x82, 206, 0, 4,
            0x01, 0x02, 0x03, 0x04,
            0x05, 0x06, 0x07, 0x08,
            0x00, 0x08, 0x00, 0x40,
            0xFF, 0xFF, 0xFF, 0xFF
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        RtcpSliceLossEntry[] entries =
            packet.Feedback!.SliceLossIndication!.Entries;
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(packet.Feedback?.PayloadFormat,
                Is.EqualTo(RtcpPayloadFeedbackFormat.SliceLossIndication));
            Assert.That(entries.Length, Is.EqualTo(2));
            Assert.That(entries[0].FirstMacroblock, Is.EqualTo(1));
            Assert.That(entries[0].MacroblockCount, Is.EqualTo(1));
            Assert.That(entries[0].PictureIdentifier, Is.Zero);
            Assert.That(entries[1].FirstMacroblock, Is.EqualTo(8191));
            Assert.That(entries[1].MacroblockCount, Is.EqualTo(8191));
            Assert.That(entries[1].PictureIdentifier, Is.EqualTo(63));
            Assert.That(encoded, Is.EqualTo(source));
        });
    }

    [Test]
    public void TypedSliceLossEntriesArePackedInsteadOfRawFci()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpPacket packet = CreateSliceLossPacket(2, 3, 4);
        packet.Feedback!.RawFeedbackControlInformation =
            new byte[] { 9, 9, 9, 9 };

        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.That(encoded,
            Is.EqualTo(new byte[]
            {
                0x82, 206, 0, 3,
                0, 0, 0, 1,
                0, 0, 0, 2,
                0x00, 0x10, 0x00, 0xC4
            }));
    }

    [Test]
    public void SliceLossIndicationRejectsMalformedFciAndOutOfRangeFieldsAtomically()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] emptyFci = new byte[]
        {
            0x82, 206, 0, 2,
            0, 0, 0, 1,
            0, 0, 0, 2
        };
        byte[] partialFci = new byte[]
        {
            0xA2, 206, 0, 3,
            0, 0, 0, 1,
            0, 0, 0, 2,
            0, 8, 0, 2
        };
        byte[] zeroFields = new byte[]
        {
            0x82, 206, 0, 3,
            0, 0, 0, 1,
            0, 0, 0, 2,
            0, 0, 0, 0
        };
        RtcpPacket[] invalidPackets = new RtcpPacket[]
        {
            CreateSliceLossPacket(0, 1, 0),
            CreateSliceLossPacket(8192, 1, 0),
            CreateSliceLossPacket(1, 0, 0),
            CreateSliceLossPacket(1, 8192, 0),
            CreateSliceLossPacket(1, 1, 64)
        };
        byte[] destination = new byte[16];
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = 77;
            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(codec.TryDecodeRtcpPacket(emptyFci).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(partialFci).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(zeroFields).Success, Is.False);
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

    static RtcpPacket CreatePictureLossPacket(bool requested)
    {
        RtcpPictureLossIndication pictureLossIndication =
            new RtcpPictureLossIndication();
        pictureLossIndication.Requested = requested;
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        feedback.PayloadFormat = RtcpPayloadFeedbackFormat.PictureLossIndication;
        feedback.SenderSourceIdentifier = 1U;
        feedback.MediaSourceIdentifier = 2U;
        feedback.PictureLossIndication = pictureLossIndication;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.PayloadFeedback;
        packet.Feedback = feedback;
        return packet;
    }

    static RtcpPacket CreateSliceLossPacket(
        int firstMacroblock,
        int macroblockCount,
        byte pictureIdentifier)
    {
        RtcpSliceLossEntry entry = new RtcpSliceLossEntry();
        entry.FirstMacroblock = firstMacroblock;
        entry.MacroblockCount = macroblockCount;
        entry.PictureIdentifier = pictureIdentifier;
        RtcpSliceLossIndication sliceLossIndication =
            new RtcpSliceLossIndication();
        sliceLossIndication.Entries = new RtcpSliceLossEntry[] { entry };
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        feedback.PayloadFormat = RtcpPayloadFeedbackFormat.SliceLossIndication;
        feedback.SenderSourceIdentifier = 1U;
        feedback.MediaSourceIdentifier = 2U;
        feedback.SliceLossIndication = sliceLossIndication;
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
