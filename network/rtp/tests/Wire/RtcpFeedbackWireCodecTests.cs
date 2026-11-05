using rtp;

namespace rtp.Tests.Wire;

public class RtcpFeedbackWireCodecTests
{
    [Test]
    public void TransportFeedbackCommonFieldsDecodeAndEncodeLosslessly()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x8A, 205, 0, 3,
            0x01, 0x02, 0x03, 0x04,
            0x05, 0x06, 0x07, 0x08,
            0x11, 0x22, 0x33, 0x44
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(packet.Type, Is.EqualTo(RtcpPacketType.TransportFeedback));
            Assert.That(packet.UnknownPacket, Is.Null);
            Assert.That(packet.Feedback?.PacketType,
                Is.EqualTo(RtcpFeedbackPacketType.TransportLayer));
            Assert.That(packet.Feedback?.Format, Is.EqualTo(10));
            Assert.That(packet.Feedback?.TransportFormat,
                Is.EqualTo(RtcpTransportFeedbackFormat.DelayBudgetInformation));
            Assert.That(packet.Feedback?.PayloadFormat,
                Is.EqualTo(RtcpPayloadFeedbackFormat.Unknown));
            Assert.That(packet.Feedback?.SenderSourceIdentifier,
                Is.EqualTo(0x01020304U));
            Assert.That(packet.Feedback?.MediaSourceIdentifier,
                Is.EqualTo(0x05060708U));
            Assert.That(packet.Feedback?.RawFeedbackControlInformation,
                Is.EqualTo(new byte[] { 0x11, 0x22, 0x33, 0x44 }));
            Assert.That(encoded, Is.EqualTo(source));
        });
    }

    [Test]
    public void UnknownPayloadFormatPreservesUnalignedFciAndRtcpPadding()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0xAE, 206, 0, 3,
            0x01, 0x02, 0x03, 0x04,
            0x05, 0x06, 0x07, 0x08,
            0xAB, 0x99, 0x88, 3
        };

        RtcpCodecResult result = codec.TryDecodeRtcpPacket(source);
        source[12] = 0;
        byte[] encoded = codec.EncodeRtcpPacketToArray(result.Packet!);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ConsumedCount, Is.EqualTo(16));
            Assert.That(result.Packet?.Type,
                Is.EqualTo(RtcpPacketType.PayloadFeedback));
            Assert.That(result.Packet?.Feedback?.PacketType,
                Is.EqualTo(RtcpFeedbackPacketType.PayloadSpecific));
            Assert.That(result.Packet?.Feedback?.Format, Is.EqualTo(14));
            Assert.That(result.Packet?.Feedback?.PayloadFormat,
                Is.EqualTo(RtcpPayloadFeedbackFormat.Unknown));
            Assert.That(result.Packet?.Feedback?.RawFeedbackControlInformation,
                Is.EqualTo(new byte[] { 0xAB }));
            Assert.That(result.Packet?.Padding,
                Is.EqualTo(new byte[] { 0x99, 0x88, 3 }));
            Assert.That(encoded,
                Is.EqualTo(new byte[]
                {
                    0xAE, 206, 0, 3,
                    0x01, 0x02, 0x03, 0x04,
                    0x05, 0x06, 0x07, 0x08,
                    0xAB, 0x99, 0x88, 3
                }));
        });
    }

    [Test]
    public void TypedFormatConstructsTheCorrectFeedbackHeader()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        feedback.PayloadFormat = RtcpPayloadFeedbackFormat.ApplicationFeedback;
        feedback.SenderSourceIdentifier = 0x10203040U;
        feedback.MediaSourceIdentifier = 0x50607080U;
        feedback.RawFeedbackControlInformation = new byte[] { 82, 69, 77, 66 };
        feedback.ApplicationFeedback = new RtcpApplicationFeedback();
        feedback.ApplicationFeedback.Data = new byte[] { 82, 69, 77, 66 };
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.PayloadFeedback;
        packet.Feedback = feedback;

        int size = codec.GetRtcpPacketEncodedSize(packet);
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
        RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(size, Is.EqualTo(16));
            Assert.That(encoded,
                Is.EqualTo(new byte[]
                {
                    0x8F, 206, 0, 3,
                    0x10, 0x20, 0x30, 0x40,
                    0x50, 0x60, 0x70, 0x80,
                    82, 69, 77, 66
                }));
            Assert.That(decoded.Feedback?.Format, Is.EqualTo(15));
            Assert.That(decoded.Feedback?.PayloadFormat,
                Is.EqualTo(RtcpPayloadFeedbackFormat.ApplicationFeedback));
        });
    }

    [Test]
    public void FeedbackValidationAndCapacityFailuresAreAtomic()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.TransportLayer;
        feedback.Format = 32;
        feedback.RawFeedbackControlInformation = new byte[4];
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.TransportFeedback;
        packet.Feedback = feedback;
        byte[] destination = new byte[15];
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = 77;
            index = index + 1;
        }

        RtcpCodecResult invalidFormat = codec.TryEncodeRtcpPacket(packet, destination);
        feedback.Format = 30;
        RtcpCodecResult insufficientCapacity = codec.TryEncodeRtcpPacket(
            packet,
            destination);

        Assert.Multiple(() =>
        {
            Assert.That(invalidFormat.Success, Is.False);
            Assert.That(invalidFormat.WrittenCount, Is.Zero);
            Assert.That(insufficientCapacity.Success, Is.False);
            Assert.That(insufficientCapacity.WrittenCount, Is.Zero);
            Assert.That(destination, Has.All.EqualTo(77));
            Assert.Throws<RtpBufferCapacityException>(() =>
                codec.EncodeRtcpPacket(packet, destination));
            Assert.That(destination, Has.All.EqualTo(77));
        });
    }

    [Test]
    public void FeedbackDecoderRejectsBodiesMissingEitherSourceIdentifier()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] shortTransportFeedback = new byte[]
        {
            0x81, 205, 0, 1,
            0, 0, 0, 1
        };
        byte[] shortPayloadFeedback = new byte[]
        {
            0x81, 206, 0, 1,
            0, 0, 0, 1
        };

        Assert.Multiple(() =>
        {
            Assert.That(codec.TryDecodeRtcpPacket(shortTransportFeedback).Success,
                Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(shortPayloadFeedback).Success,
                Is.False);
            Assert.Throws<RtcpPacketFormatException>(() =>
                codec.DecodeRtcpPacket(shortTransportFeedback));
        });
    }

    [Test]
    public void FeedbackBodyMustMatchItsOuterPacketCategory()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.TransportLayer;
        feedback.Format = 30;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.PayloadFeedback;
        packet.Feedback = feedback;

        RtcpCodecResult result = codec.TryEncodeRtcpPacket(packet, new byte[12]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("does not match"));
            Assert.Throws<RtcpPacketFormatException>(() =>
                codec.GetRtcpPacketEncodedSize(packet));
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
