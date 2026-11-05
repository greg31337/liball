using rtp;

namespace rtp.Tests.Wire;

public class RtcpPacketCodecTests
{
    [Test]
    public void UnknownPacketDecodesAndRoundTripsLosslessly()
    {
        IRtcpPacketCodec codec = CreateCodec(true, true, 65535);
        byte[] source = new byte[]
        {
            0x89, 222, 0, 2,
            1, 2, 3, 4,
            5, 6, 7, 8,
            99
        };
        byte[] expected = source[..12];

        RtcpCodecResult result = codec.TryDecodeRtcpPacket(source);
        source[4] = 0;
        byte[] encoded = codec.EncodeRtcpPacketToArray(result.Packet!);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ConsumedCount, Is.EqualTo(12));
            Assert.That(result.Packet?.Type, Is.EqualTo(RtcpPacketType.Unknown));
            Assert.That(result.Packet?.Header.Version, Is.EqualTo(2));
            Assert.That(result.Packet?.Header.Count, Is.EqualTo(9));
            Assert.That(result.Packet?.Header.PacketType, Is.EqualTo(222));
            Assert.That(result.Packet?.Header.LengthInWordsMinusOne, Is.EqualTo(2));
            Assert.That(result.Packet?.UnknownPacket?.PacketType, Is.EqualTo(222));
            Assert.That(result.Packet?.UnknownPacket?.Count, Is.EqualTo(9));
            Assert.That(result.Packet?.UnknownPacket?.Data,
                Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
            Assert.That(encoded, Is.EqualTo(expected));
        });
    }

    [Test]
    public void PaddingCanBePreservedOrNormalizedWithoutChangingItsCount()
    {
        byte[] source = new byte[]
        {
            0xA3, 214, 0, 2,
            10, 11, 12, 13,
            0xAA, 0xBB, 0xCC, 4
        };
        IRtcpPacketCodec preservingCodec = CreateCodec(true, true, 65535);
        IRtcpPacketCodec normalizingCodec = CreateCodec(true, false, 65535);

        RtcpPacket preserved = preservingCodec.DecodeRtcpPacket(source);
        RtcpPacket normalized = normalizingCodec.DecodeRtcpPacket(source);
        byte[] preservedWire = preservingCodec.EncodeRtcpPacketToArray(preserved);
        byte[] normalizedWire = normalizingCodec.EncodeRtcpPacketToArray(preserved);

        Assert.Multiple(() =>
        {
            Assert.That(preserved.Header.HasPadding, Is.True);
            Assert.That(preserved.UnknownPacket?.Data,
                Is.EqualTo(new byte[] { 10, 11, 12, 13 }));
            Assert.That(preserved.Padding,
                Is.EqualTo(new byte[] { 0xAA, 0xBB, 0xCC, 4 }));
            Assert.That(normalized.Padding, Is.EqualTo(new byte[] { 0, 0, 0, 4 }));
            Assert.That(preservedWire, Is.EqualTo(source));
            Assert.That(normalizedWire,
                Is.EqualTo(new byte[]
                {
                    0xA3, 214, 0, 2,
                    10, 11, 12, 13,
                    0, 0, 0, 4
                }));
        });
    }

    [Test]
    public void EncodeCalculatesHeaderLengthAndLeavesExtraCapacityUntouched()
    {
        IRtcpPacketCodec codec = CreateCodec(true, true, 65535);
        RtcpPacket packet = CreateUnknownPacket(220, 31, new byte[] { 1, 2, 3, 4 });
        packet.Header.PacketType = 1;
        packet.Header.Count = 1;
        packet.Header.LengthInWordsMinusOne = 99;
        byte[] destination = new byte[]
        {
            77, 77, 77, 77, 77, 77, 77, 77, 77
        };

        int size = codec.GetRtcpPacketEncodedSize(packet);
        RtcpCodecResult result = codec.TryEncodeRtcpPacket(packet, destination);

        Assert.Multiple(() =>
        {
            Assert.That(size, Is.EqualTo(8));
            Assert.That(result.Success, Is.True);
            Assert.That(result.WrittenCount, Is.EqualTo(8));
            Assert.That(destination[..8],
                Is.EqualTo(new byte[] { 0x9F, 220, 0, 1, 1, 2, 3, 4 }));
            Assert.That(destination[8], Is.EqualTo(77));
        });
    }

    [Test]
    public void CapacityAndValidationFailuresDoNotPartiallyWrite()
    {
        IRtcpPacketCodec codec = CreateCodec(true, true, 65535);
        RtcpPacket packet = CreateUnknownPacket(220, 1, new byte[] { 1, 2, 3, 4 });
        byte[] destination = new byte[] { 77, 77, 77, 77, 77, 77, 77 };
        RtcpPacket invalidCount = CreateUnknownPacket(220, 32, new byte[0]);

        RtcpCodecResult capacityResult = codec.TryEncodeRtcpPacket(packet, destination);
        RtcpCodecResult validationResult = codec.TryEncodeRtcpPacket(
            invalidCount,
            destination);

        Assert.Multiple(() =>
        {
            Assert.That(capacityResult.Success, Is.False);
            Assert.That(validationResult.Success, Is.False);
            Assert.That(destination,
                Is.EqualTo(new byte[] { 77, 77, 77, 77, 77, 77, 77 }));
            Assert.Throws<RtpBufferCapacityException>(() =>
                codec.EncodeRtcpPacket(packet, destination));
            Assert.Throws<RtcpPacketFormatException>(() =>
                codec.GetRtcpPacketEncodedSize(invalidCount));
        });
    }

    [Test]
    public void DecoderRejectsMalformedCommonHeadersLengthsAndPadding()
    {
        IRtcpPacketCodec codec = CreateCodec(true, true, 65535);

        Assert.Multiple(() =>
        {
            Assert.That(codec.TryDecodeRtcpPacket(new byte[] { 0x80, 220, 0 }).Success,
                Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(
                new byte[] { 0x40, 220, 0, 0 }).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(
                new byte[] { 0x80, 100, 0, 0 }).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(
                new byte[] { 0x80, 220, 0, 1, 1, 2, 3, 4 }).Success, Is.True);
            Assert.That(codec.TryDecodeRtcpPacket(
                new byte[] { 0x80, 220, 0, 2, 1, 2, 3, 4 }).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(
                new byte[] { 0xA0, 220, 0, 1, 1, 2, 3, 0 }).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(
                new byte[] { 0xA0, 220, 0, 1, 1, 2, 3, 5 }).Success, Is.False);
            Assert.Throws<RtcpPacketFormatException>(() =>
                codec.DecodeRtcpPacket(new byte[] { 0x80, 220, 0, 2 }));
        });
    }

    [Test]
    public void TolerantCodecPreservesPacketTypesOutsideTheStrictRange()
    {
        IRtcpPacketCodec strictCodec = CreateCodec(true, true, 65535);
        IRtcpPacketCodec tolerantCodec = CreateCodec(false, true, 65535);
        byte[] source = new byte[] { 0x85, 150, 0, 0 };

        RtcpCodecResult strictResult = strictCodec.TryDecodeRtcpPacket(source);
        RtcpPacket tolerantPacket = tolerantCodec.DecodeRtcpPacket(source);

        Assert.Multiple(() =>
        {
            Assert.That(strictResult.Success, Is.False);
            Assert.That(tolerantPacket.Type, Is.EqualTo(RtcpPacketType.Unknown));
            Assert.That(tolerantPacket.UnknownPacket?.PacketType, Is.EqualTo(150));
            Assert.That(tolerantPacket.UnknownPacket?.Count, Is.EqualTo(5));
            Assert.That(tolerantCodec.EncodeRtcpPacketToArray(tolerantPacket),
                Is.EqualTo(source));
        });
    }

    [Test]
    public void OpaquePacketsComposeIntoAnOwnedCompoundSequence()
    {
        IRtcpPacketCodec codec = CreateCodec(false, true, 65535);
        RtcpCompoundPacket compound = new RtcpCompoundPacket();
        compound.Packets = new RtcpPacket[]
        {
            CreateUnknownPacket(220, 1, new byte[] { 1, 2, 3, 4 }),
            CreateUnknownPacket(221, 2, new byte[0])
        };

        byte[] encoded = codec.EncodeRtcpCompoundPacketToArray(compound);
        RtcpCodecResult result = codec.TryDecodeRtcpCompoundPacket(encoded);
        encoded[4] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ConsumedCount, Is.EqualTo(12));
            Assert.That(result.CompoundPacket?.Packets.Length, Is.EqualTo(2));
            Assert.That(result.CompoundPacket?.Packets[0].UnknownPacket?.Data,
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(result.CompoundPacket?.Packets[1].UnknownPacket?.PacketType,
                Is.EqualTo(221));
        });
    }

    [Test]
    public void CodecConfigurationEnforcesRtcpWireLimits()
    {
        RtpSessionConfiguration tooSmall = new RtpSessionConfiguration();
        tooSmall.MaximumRtcpCompoundPacketSize = 3;
        RtpSessionConfiguration tooLarge = new RtpSessionConfiguration();
        tooLarge.MaximumRtcpCompoundPacketSize =
            RtcpProtocolConstants.MaximumPacketSize + 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                new RtcpPacketCodec(null!));
            Assert.Throws<RtpConfigurationException>(() =>
                new RtcpPacketCodec(tooSmall));
            Assert.Throws<RtpConfigurationException>(() =>
                new RtcpPacketCodec(tooLarge));
        });
    }

    static IRtcpPacketCodec CreateCodec(
        bool strictValidation,
        bool preservePadding,
        int maximumPacketSize)
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.StrictValidation = strictValidation;
        configuration.PreservePadding = preservePadding;
        configuration.MaximumRtcpCompoundPacketSize = maximumPacketSize;
        return new RtcpPacketCodec(configuration);
    }

    static RtcpPacket CreateUnknownPacket(
        byte packetType,
        byte count,
        byte[] data)
    {
        RtcpUnknownPacket unknown = new RtcpUnknownPacket();
        unknown.PacketType = packetType;
        unknown.Count = count;
        unknown.Data = data;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketWireCodec.GetPacketType(packetType);
        packet.UnknownPacket = unknown;
        return packet;
    }
}
