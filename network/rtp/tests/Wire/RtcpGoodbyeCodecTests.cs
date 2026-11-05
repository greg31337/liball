using rtp;

namespace rtp.Tests.Wire;

public class RtcpGoodbyeCodecTests
{
    [Test]
    public void MultipleSourcesAndUtf8ReasonMatchTheWireLayout()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        byte[] source = new byte[]
        {
            0x82, 203, 0, 4,
            1, 2, 3, 4,
            0xA1, 0xA2, 0xA3, 0xA4,
            4, 0xE2, 0x98, 0x83, 0x21, 0, 0, 0
        };
        byte[] expected = (byte[])source.Clone();

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        source[13] = 0;
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(packet.Type, Is.EqualTo(RtcpPacketType.Goodbye));
            Assert.That(packet.UnknownPacket, Is.Null);
            Assert.That(packet.Header.Count, Is.EqualTo(2));
            Assert.That(packet.Header.PacketType, Is.EqualTo(203));
            Assert.That(packet.Header.LengthInWordsMinusOne, Is.EqualTo(4));
            Assert.That(packet.Goodbye?.Sources,
                Is.EqualTo(new uint[] { 0x01020304U, 0xA1A2A3A4U }));
            Assert.That(packet.Goodbye?.HasReason, Is.True);
            Assert.That(packet.Goodbye?.Reason,
                Is.EqualTo(new byte[] { 0xE2, 0x98, 0x83, 0x21 }));
            Assert.That(encoded, Is.EqualTo(expected));
        });
    }

    [Test]
    public void AbsentAndEmptyReasonsRemainDistinct()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        byte[] absentWire = new byte[]
        {
            0x81, 203, 0, 1,
            1, 2, 3, 4
        };
        byte[] emptyWire = new byte[]
        {
            0x81, 203, 0, 2,
            1, 2, 3, 4,
            0, 0, 0, 0
        };
        byte[] noSourcesWire = new byte[] { 0x80, 203, 0, 0 };

        RtcpPacket absent = codec.DecodeRtcpPacket(absentWire);
        RtcpPacket empty = codec.DecodeRtcpPacket(emptyWire);
        RtcpPacket noSources = codec.DecodeRtcpPacket(noSourcesWire);

        Assert.Multiple(() =>
        {
            Assert.That(absent.Goodbye?.HasReason, Is.False);
            Assert.That(absent.Goodbye?.Reason, Is.Empty);
            Assert.That(empty.Goodbye?.HasReason, Is.True);
            Assert.That(empty.Goodbye?.Reason, Is.Empty);
            Assert.That(noSources.Goodbye?.Sources, Is.Empty);
            Assert.That(codec.EncodeRtcpPacketToArray(absent), Is.EqualTo(absentWire));
            Assert.That(codec.EncodeRtcpPacketToArray(empty), Is.EqualTo(emptyWire));
            Assert.That(codec.EncodeRtcpPacketToArray(noSources),
                Is.EqualTo(noSourcesWire));
        });
    }

    [Test]
    public void GoodbyeSupportsThirtyOneSourcesAndDerivesItsHeader()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.Goodbye;
        packet.Goodbye = new RtcpGoodbye();
        packet.Goodbye.Sources = new uint[31];
        int sourceIndex = 0;
        while (sourceIndex < packet.Goodbye.Sources.Length)
        {
            packet.Goodbye.Sources[sourceIndex] = (uint)(sourceIndex + 1);
            sourceIndex = sourceIndex + 1;
        }

        packet.Header.Count = 1;
        packet.Header.PacketType = 222;
        packet.Header.LengthInWordsMinusOne = 99;
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
        RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Length, Is.EqualTo(128));
            Assert.That(encoded[..4], Is.EqualTo(new byte[] { 0x9F, 203, 0, 31 }));
            Assert.That(decoded.Goodbye?.Sources.Length, Is.EqualTo(31));
            Assert.That(decoded.Goodbye?.Sources[30], Is.EqualTo(31U));
        });
    }

    [Test]
    public void MaximumReasonLengthIsAcceptedAndOwned()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.Goodbye;
        packet.Goodbye = new RtcpGoodbye();
        packet.Goodbye.Sources = new uint[] { 7U };
        packet.Goodbye.HasReason = true;
        packet.Goodbye.Reason = new byte[255];
        int reasonIndex = 0;
        while (reasonIndex < packet.Goodbye.Reason.Length)
        {
            packet.Goodbye.Reason[reasonIndex] = 0x61;
            reasonIndex = reasonIndex + 1;
        }

        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
        RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);
        encoded[9] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Length, Is.EqualTo(264));
            Assert.That(encoded[..4], Is.EqualTo(new byte[] { 0x81, 203, 0, 65 }));
            Assert.That(decoded.Goodbye?.Reason.Length, Is.EqualTo(255));
            Assert.That(decoded.Goodbye?.Reason[0], Is.EqualTo(0x61));
        });
    }

    [Test]
    public void RtcpPaddingIsNotDecodedAsAReason()
    {
        byte[] source = new byte[]
        {
            0xA1, 203, 0, 2,
            1, 2, 3, 4,
            0x11, 0x22, 0x33, 4
        };
        IRtcpPacketCodec preservingCodec = CreateCodec(true);
        IRtcpPacketCodec normalizingCodec = CreateCodec(false);

        RtcpPacket preserved = preservingCodec.DecodeRtcpPacket(source);
        RtcpPacket normalized = normalizingCodec.DecodeRtcpPacket(source);

        Assert.Multiple(() =>
        {
            Assert.That(preserved.Goodbye?.HasReason, Is.False);
            Assert.That(preserved.Goodbye?.Reason, Is.Empty);
            Assert.That(preserved.Padding,
                Is.EqualTo(new byte[] { 0x11, 0x22, 0x33, 4 }));
            Assert.That(normalized.Padding, Is.EqualTo(new byte[] { 0, 0, 0, 4 }));
            Assert.That(preservingCodec.EncodeRtcpPacketToArray(preserved),
                Is.EqualTo(source));
        });
    }

    [Test]
    public void DecoderRejectsInvalidCountsReasonLengthsUtf8AndPadding()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        byte[] shortSources = new byte[]
        {
            0x82, 203, 0, 1,
            0, 0, 0, 1
        };
        byte[] truncatedReason = new byte[]
        {
            0x81, 203, 0, 2,
            0, 0, 0, 1,
            5, 0x61, 0x62, 0x63
        };
        byte[] invalidPadding = new byte[]
        {
            0x81, 203, 0, 2,
            0, 0, 0, 1,
            1, 0x61, 9, 0
        };
        byte[] excessPadding = new byte[]
        {
            0x81, 203, 0, 3,
            0, 0, 0, 1,
            1, 0x61, 0, 0,
            0, 0, 0, 0
        };
        byte[] invalidUtf8 = new byte[]
        {
            0x81, 203, 0, 2,
            0, 0, 0, 1,
            2, 0xC0, 0xAF, 0
        };

        Assert.Multiple(() =>
        {
            Assert.That(codec.TryDecodeRtcpPacket(shortSources).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(truncatedReason).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(invalidPadding).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(excessPadding).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(invalidUtf8).Success, Is.False);
            Assert.Throws<RtcpPacketFormatException>(() =>
                codec.DecodeRtcpPacket(truncatedReason));
        });
    }

    [Test]
    public void InvalidGoodbyeModelsFailWithoutPartiallyWriting()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        RtcpPacket missingBody = new RtcpPacket();
        missingBody.Type = RtcpPacketType.Goodbye;
        RtcpPacket tooManySources = CreateGoodbyePacket();
        tooManySources.Goodbye!.Sources = new uint[32];
        RtcpPacket oversizedReason = CreateGoodbyePacket();
        oversizedReason.Goodbye!.HasReason = true;
        oversizedReason.Goodbye.Reason = new byte[256];
        RtcpPacket hiddenReason = CreateGoodbyePacket();
        hiddenReason.Goodbye!.Reason = new byte[] { 0x61 };
        RtcpPacket invalidUtf8 = CreateGoodbyePacket();
        invalidUtf8.Goodbye!.HasReason = true;
        invalidUtf8.Goodbye.Reason = new byte[] { 0xC0, 0xAF };
        byte[] destination = new byte[300];
        int destinationIndex = 0;
        while (destinationIndex < destination.Length)
        {
            destination[destinationIndex] = 77;
            destinationIndex = destinationIndex + 1;
        }

        RtcpCodecResult missingResult = codec.TryEncodeRtcpPacket(missingBody, destination);
        RtcpCodecResult countResult = codec.TryEncodeRtcpPacket(tooManySources, destination);
        RtcpCodecResult sizeResult = codec.TryEncodeRtcpPacket(oversizedReason, destination);
        RtcpCodecResult hiddenResult = codec.TryEncodeRtcpPacket(hiddenReason, destination);
        RtcpCodecResult utf8Result = codec.TryEncodeRtcpPacket(invalidUtf8, destination);

        Assert.Multiple(() =>
        {
            Assert.That(missingResult.Success, Is.False);
            Assert.That(countResult.Success, Is.False);
            Assert.That(sizeResult.Success, Is.False);
            Assert.That(hiddenResult.Success, Is.False);
            Assert.That(utf8Result.Success, Is.False);
            Assert.That(destination, Has.All.EqualTo(77));
            Assert.Throws<RtcpPacketFormatException>(() =>
                codec.GetRtcpPacketEncodedSize(oversizedReason));
        });
    }

    static RtcpPacket CreateGoodbyePacket()
    {
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.Goodbye;
        packet.Goodbye = new RtcpGoodbye();
        return packet;
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
