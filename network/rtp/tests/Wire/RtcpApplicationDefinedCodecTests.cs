using rtp;

namespace rtp.Tests.Wire;

public class RtcpApplicationDefinedCodecTests
{
    [Test]
    public void MaximumSubtypeAndOpaqueDataMatchTheWireLayout()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        byte[] source = new byte[]
        {
            0x9F, 204, 0, 4,
            1, 2, 3, 4,
            0x54, 0x65, 0x53, 0x74,
            0x10, 0x20, 0x30, 0x40,
            0x50, 0x60, 0x70, 0x80
        };
        byte[] expected = (byte[])source.Clone();

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        source[12] = 0;
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(packet.Type, Is.EqualTo(RtcpPacketType.ApplicationDefined));
            Assert.That(packet.UnknownPacket, Is.Null);
            Assert.That(packet.Header.Count, Is.EqualTo(31));
            Assert.That(packet.Header.PacketType, Is.EqualTo(204));
            Assert.That(packet.Header.LengthInWordsMinusOne, Is.EqualTo(4));
            Assert.That(packet.ApplicationDefined?.Subtype, Is.EqualTo(31));
            Assert.That(packet.ApplicationDefined?.SourceIdentifier,
                Is.EqualTo(0x01020304U));
            Assert.That(packet.ApplicationDefined?.Name,
                Is.EqualTo(new byte[] { 0x54, 0x65, 0x53, 0x74 }));
            Assert.That(packet.ApplicationDefined?.Data,
                Is.EqualTo(new byte[]
                {
                    0x10, 0x20, 0x30, 0x40,
                    0x50, 0x60, 0x70, 0x80
                }));
            Assert.That(encoded, Is.EqualTo(expected));
        });
    }

    [Test]
    public void MinimumApplicationPacketDerivesItsHeaderFromTheTypedBody()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        RtcpPacket packet = CreateApplicationPacket();
        packet.ApplicationDefined!.SourceIdentifier = 0xA1A2A3A4U;
        packet.ApplicationDefined.Name = new byte[] { 0x41, 0x42, 0x43, 0x44 };
        packet.Header.Count = 17;
        packet.Header.PacketType = 222;
        packet.Header.LengthInWordsMinusOne = 99;

        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
        RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(codec.GetRtcpPacketEncodedSize(packet), Is.EqualTo(12));
            Assert.That(encoded, Is.EqualTo(new byte[]
            {
                0x80, 204, 0, 2,
                0xA1, 0xA2, 0xA3, 0xA4,
                0x41, 0x42, 0x43, 0x44
            }));
            Assert.That(decoded.ApplicationDefined?.Subtype, Is.Zero);
            Assert.That(decoded.ApplicationDefined?.Data, Is.Empty);
        });
    }

    [Test]
    public void RtcpPaddingIsKeptSeparateFromApplicationData()
    {
        byte[] source = new byte[]
        {
            0xA3, 204, 0, 4,
            1, 2, 3, 4,
            0x54, 0x45, 0x53, 0x54,
            9, 8, 7, 6,
            0x11, 0x22, 0x33, 4
        };
        IRtcpPacketCodec preservingCodec = CreateCodec(true);
        IRtcpPacketCodec normalizingCodec = CreateCodec(false);

        RtcpPacket preserved = preservingCodec.DecodeRtcpPacket(source);
        RtcpPacket normalized = normalizingCodec.DecodeRtcpPacket(source);

        Assert.Multiple(() =>
        {
            Assert.That(preserved.ApplicationDefined?.Subtype, Is.EqualTo(3));
            Assert.That(preserved.ApplicationDefined?.Data,
                Is.EqualTo(new byte[] { 9, 8, 7, 6 }));
            Assert.That(preserved.Padding,
                Is.EqualTo(new byte[] { 0x11, 0x22, 0x33, 4 }));
            Assert.That(normalized.Padding, Is.EqualTo(new byte[] { 0, 0, 0, 4 }));
            Assert.That(preservingCodec.EncodeRtcpPacketToArray(preserved),
                Is.EqualTo(source));
            Assert.That(normalizingCodec.EncodeRtcpPacketToArray(preserved),
                Is.EqualTo(new byte[]
                {
                    0xA3, 204, 0, 4,
                    1, 2, 3, 4,
                    0x54, 0x45, 0x53, 0x54,
                    9, 8, 7, 6,
                    0, 0, 0, 4
                }));
        });
    }

    [Test]
    public void DecoderRejectsShortBodiesNonAsciiNamesAndMisalignedData()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        byte[] shortBody = new byte[]
        {
            0x80, 204, 0, 1,
            0, 0, 0, 1
        };
        byte[] nonAsciiName = new byte[]
        {
            0x80, 204, 0, 2,
            0, 0, 0, 1,
            0x54, 0x45, 0xFF, 0x54
        };
        byte[] misalignedData = new byte[]
        {
            0xA0, 204, 0, 3,
            0, 0, 0, 1,
            0x54, 0x45, 0x53, 0x54,
            9,
            0, 0, 3
        };
        byte[] paddingHidesFixedFields = new byte[]
        {
            0xA0, 204, 0, 2,
            0, 0, 0, 1,
            0, 0, 0, 4
        };

        Assert.Multiple(() =>
        {
            Assert.That(codec.TryDecodeRtcpPacket(shortBody).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(nonAsciiName).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(misalignedData).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(paddingHidesFixedFields).Success,
                Is.False);
            Assert.Throws<RtcpPacketFormatException>(() =>
                codec.DecodeRtcpPacket(shortBody));
        });
    }

    [Test]
    public void InvalidApplicationModelsFailWithoutPartiallyWriting()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        RtcpPacket missingBody = new RtcpPacket();
        missingBody.Type = RtcpPacketType.ApplicationDefined;
        RtcpPacket invalidSubtype = CreateApplicationPacket();
        invalidSubtype.ApplicationDefined!.Subtype = 32;
        RtcpPacket shortName = CreateApplicationPacket();
        shortName.ApplicationDefined!.Name = new byte[] { 1, 2, 3 };
        RtcpPacket nonAsciiName = CreateApplicationPacket();
        nonAsciiName.ApplicationDefined!.Name = new byte[] { 1, 2, 3, 255 };
        RtcpPacket misalignedData = CreateApplicationPacket();
        misalignedData.ApplicationDefined!.Data = new byte[] { 1, 2 };
        byte[] destination = new byte[32];
        int destinationIndex = 0;
        while (destinationIndex < destination.Length)
        {
            destination[destinationIndex] = 77;
            destinationIndex = destinationIndex + 1;
        }

        RtcpCodecResult missingResult = codec.TryEncodeRtcpPacket(missingBody, destination);
        RtcpCodecResult subtypeResult = codec.TryEncodeRtcpPacket(
            invalidSubtype,
            destination);
        RtcpCodecResult nameResult = codec.TryEncodeRtcpPacket(shortName, destination);
        RtcpCodecResult asciiResult = codec.TryEncodeRtcpPacket(nonAsciiName, destination);
        RtcpCodecResult dataResult = codec.TryEncodeRtcpPacket(
            misalignedData,
            destination);

        Assert.Multiple(() =>
        {
            Assert.That(missingResult.Success, Is.False);
            Assert.That(subtypeResult.Success, Is.False);
            Assert.That(nameResult.Success, Is.False);
            Assert.That(asciiResult.Success, Is.False);
            Assert.That(dataResult.Success, Is.False);
            Assert.That(destination, Has.All.EqualTo(77));
            Assert.Throws<RtcpPacketFormatException>(() =>
                codec.GetRtcpPacketEncodedSize(misalignedData));
        });
    }

    static RtcpPacket CreateApplicationPacket()
    {
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.ApplicationDefined;
        packet.ApplicationDefined = new RtcpApplicationDefined();
        packet.ApplicationDefined.Name = new byte[] { 0x54, 0x45, 0x53, 0x54 };
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
