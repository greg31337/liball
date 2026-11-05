using rtp;

namespace rtp.Tests.Wire;

public class RtpPacketGoldenVectorTests
{
    [Test]
    public void FixedHeaderFlagGoldenVectorsUseTheAssignedWireBits()
    {
        IRtpPacketCodec codec = CreateCodec();
        RtpPacket markerPacket = new RtpPacket();
        markerPacket.Marker = true;
        RtpPacket paddingPacket = new RtpPacket();
        paddingPacket.HasPadding = true;
        paddingPacket.Padding = new byte[] { 1 };
        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.Profile = 0x1234;
        extension.Format = RtpHeaderExtensionFormat.Raw;
        RtpPacket extensionPacket = new RtpPacket();
        extensionPacket.HasExtension = true;
        extensionPacket.HeaderExtension = extension;

        AssertGoldenRoundTrip(
            codec,
            markerPacket,
            new byte[]
            {
                0x80, 0x80, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0
            });
        AssertGoldenRoundTrip(
            codec,
            paddingPacket,
            new byte[]
            {
                0xA0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0,
                1
            });
        AssertGoldenRoundTrip(
            codec,
            extensionPacket,
            new byte[]
            {
                0x90, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0,
                0x12, 0x34, 0, 0
            });
    }

    [Test]
    public void EveryPayloadTypeBitHasItsExactWirePosition()
    {
        IRtpPacketCodec codec = CreateCodec();
        byte[] payloadTypes = new byte[]
        {
            0, 1, 2, 4, 8, 16, 32, 64, 127
        };
        int payloadTypeIndex = 0;
        while (payloadTypeIndex < payloadTypes.Length)
        {
            byte payloadType = payloadTypes[payloadTypeIndex];
            RtpPacket packet = new RtpPacket();
            packet.PayloadType = payloadType;
            byte[] encoded = codec.EncodeRtpPacketToArray(packet);
            RtpPacket decoded = codec.DecodeRtpPacket(encoded);
            Assert.Multiple(() =>
            {
                Assert.That(encoded[0], Is.EqualTo(0x80));
                Assert.That(encoded[1], Is.EqualTo(payloadType));
                Assert.That(decoded.PayloadType, Is.EqualTo(payloadType));
                Assert.That(decoded.Marker, Is.False);
            });
            payloadTypeIndex = payloadTypeIndex + 1;
        }
    }

    [Test]
    public void ScalarFieldGoldenVectorUsesNetworkByteOrderAndFullWidths()
    {
        IRtpPacketCodec codec = CreateCodec();
        RtpPacket packet = new RtpPacket();
        packet.PayloadType = 127;
        packet.SequenceNumber = ushort.MaxValue;
        packet.Timestamp = uint.MaxValue;
        packet.SynchronizationSource = uint.MaxValue;

        AssertGoldenRoundTrip(
            codec,
            packet,
            new byte[]
            {
                0x80, 0x7F, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF
            });
    }

    [Test]
    public void ZeroCsrcGoldenVectorHasOnlyTheFixedHeader()
    {
        IRtpPacketCodec codec = CreateCodec();
        RtpPacket packet = new RtpPacket();

        AssertGoldenRoundTrip(
            codec,
            packet,
            new byte[]
            {
                0x80, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0
            });
    }

    [Test]
    public void FifteenCsrcGoldenVectorUsesTheEntireCountField()
    {
        IRtpPacketCodec codec = CreateCodec();
        RtpPacket packet = new RtpPacket();
        packet.SequenceNumber = 0x1234;
        packet.Timestamp = 0x56789ABCU;
        packet.SynchronizationSource = 0xDEF01234U;
        packet.ContributingSources = new uint[]
        {
            1U, 2U, 3U, 4U, 5U,
            6U, 7U, 8U, 9U, 10U,
            11U, 12U, 13U, 14U, 15U
        };

        AssertGoldenRoundTrip(
            codec,
            packet,
            new byte[]
            {
                0x8F, 0, 0x12, 0x34,
                0x56, 0x78, 0x9A, 0xBC,
                0xDE, 0xF0, 0x12, 0x34,
                0, 0, 0, 1,
                0, 0, 0, 2,
                0, 0, 0, 3,
                0, 0, 0, 4,
                0, 0, 0, 5,
                0, 0, 0, 6,
                0, 0, 0, 7,
                0, 0, 0, 8,
                0, 0, 0, 9,
                0, 0, 0, 10,
                0, 0, 0, 11,
                0, 0, 0, 12,
                0, 0, 0, 13,
                0, 0, 0, 14,
                0, 0, 0, 15
            });
    }

    [Test]
    public void EveryCsrcCountBitHasItsExactWirePosition()
    {
        IRtpPacketCodec codec = CreateCodec();
        int[] sourceCounts = new int[] { 1, 2, 4, 8 };
        int countIndex = 0;
        while (countIndex < sourceCounts.Length)
        {
            int sourceCount = sourceCounts[countIndex];
            RtpPacket packet = new RtpPacket();
            packet.ContributingSources = new uint[sourceCount];
            byte[] encoded = codec.EncodeRtpPacketToArray(packet);
            RtpPacket decoded = codec.DecodeRtpPacket(encoded);
            Assert.Multiple(() =>
            {
                Assert.That(encoded[0],
                    Is.EqualTo((byte)(0x80 | sourceCount)));
                Assert.That(encoded.Length,
                    Is.EqualTo(12 + sourceCount * 4));
                Assert.That(decoded.ContributingSources,
                    Has.Length.EqualTo(sourceCount));
            });
            countIndex = countIndex + 1;
        }
    }

    [Test]
    public void BinaryPayloadGoldenVectorPreservesEveryOctet()
    {
        IRtpPacketCodec codec = CreateCodec();
        RtpPacket packet = new RtpPacket();
        packet.PayloadType = 96;
        packet.Payload = new byte[]
        {
            0x00, 0x01, 0x7F, 0x80, 0xFE, 0xFF
        };

        AssertGoldenRoundTrip(
            codec,
            packet,
            new byte[]
            {
                0x80, 0x60, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0,
                0x00, 0x01, 0x7F, 0x80, 0xFE, 0xFF
            });
    }

    [Test]
    public void PaddingGoldenVectorsCoverOneAndMaximumCounts()
    {
        IRtpPacketCodec codec = CreateCodec();
        RtpPacket oneBytePadding = new RtpPacket();
        oneBytePadding.Payload = new byte[] { 0xAA };
        oneBytePadding.HasPadding = true;
        oneBytePadding.Padding = new byte[] { 1 };
        AssertGoldenRoundTrip(
            codec,
            oneBytePadding,
            new byte[]
            {
                0xA0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0,
                0xAA, 1
            });

        RtpPacket maximumPadding = new RtpPacket();
        maximumPadding.Payload = new byte[] { 0xBB };
        maximumPadding.HasPadding = true;
        maximumPadding.Padding =
            new byte[RtpProtocolConstants.MaximumPaddingCount];
        maximumPadding.Padding[
            maximumPadding.Padding.Length - 1] = byte.MaxValue;
        byte[] encoded = codec.EncodeRtpPacketToArray(maximumPadding);
        RtpPacket decoded = codec.DecodeRtpPacket(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(codec.GetRtpPacketEncodedSize(maximumPadding),
                Is.EqualTo(268));
            Assert.That(encoded, Has.Length.EqualTo(268));
            Assert.That(encoded[0], Is.EqualTo(0xA0));
            Assert.That(encoded[12], Is.EqualTo(0xBB));
            Assert.That(encoded[13..267], Is.All.Zero);
            Assert.That(encoded[267], Is.EqualTo(byte.MaxValue));
            Assert.That(decoded.Payload,
                Is.EqualTo(new byte[] { 0xBB }));
            Assert.That(decoded.Padding,
                Is.EqualTo(maximumPadding.Padding));
            Assert.That(codec.EncodeRtpPacketToArray(decoded),
                Is.EqualTo(encoded));
        });
    }

    [Test]
    public void RawExtensionEnvelopeGoldenVectorIncludesProfileAndWordCount()
    {
        IRtpPacketCodec codec = CreateCodec();
        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.Profile = 0xABCD;
        extension.Format = RtpHeaderExtensionFormat.Raw;
        extension.Data = new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8
        };
        RtpPacket packet = new RtpPacket();
        packet.HasExtension = true;
        packet.HeaderExtension = extension;
        packet.Payload = new byte[] { 0xEE };

        AssertGoldenRoundTrip(
            codec,
            packet,
            new byte[]
            {
                0x90, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0,
                0xAB, 0xCD, 0, 2,
                1, 2, 3, 4, 5, 6, 7, 8,
                0xEE
            });
    }

    [Test]
    public void FixedHeaderSectionsRoundTripInEveryFlagCombination()
    {
        IRtpPacketCodec codec = CreateCodec();
        int paddingValue = 0;
        while (paddingValue <= 1)
        {
            int extensionValue = 0;
            while (extensionValue <= 1)
            {
                int markerValue = 0;
                while (markerValue <= 1)
                {
                    int sourceMode = 0;
                    while (sourceMode <= 1)
                    {
                        RtpPacket packet = CreateCombinationPacket(
                            paddingValue == 1,
                            extensionValue == 1,
                            markerValue == 1,
                            sourceMode == 1);
                        int expectedSize = 14;
                        if (paddingValue == 1)
                        {
                            expectedSize = expectedSize + 2;
                        }

                        if (extensionValue == 1)
                        {
                            expectedSize = expectedSize + 8;
                        }

                        if (sourceMode == 1)
                        {
                            expectedSize = expectedSize + 60;
                        }

                        byte expectedFirstByte = 0x80;
                        if (paddingValue == 1)
                        {
                            expectedFirstByte = (byte)(
                                expectedFirstByte | 0x20);
                        }

                        if (extensionValue == 1)
                        {
                            expectedFirstByte = (byte)(
                                expectedFirstByte | 0x10);
                        }

                        if (sourceMode == 1)
                        {
                            expectedFirstByte = (byte)(
                                expectedFirstByte | 0x0F);
                        }

                        byte expectedSecondByte = 96;
                        if (markerValue == 1)
                        {
                            expectedSecondByte = (byte)(
                                expectedSecondByte | 0x80);
                        }

                        byte[] encoded = codec.EncodeRtpPacketToArray(packet);
                        RtpPacket decoded = codec.DecodeRtpPacket(encoded);
                        Assert.Multiple(() =>
                        {
                            Assert.That(
                                codec.GetRtpPacketEncodedSize(packet),
                                Is.EqualTo(expectedSize));
                            Assert.That(encoded, Has.Length.EqualTo(expectedSize));
                            Assert.That(encoded[0],
                                Is.EqualTo(expectedFirstByte));
                            Assert.That(encoded[1],
                                Is.EqualTo(expectedSecondByte));
                            AssertPacketFields(decoded, packet);
                            Assert.That(codec.EncodeRtpPacketToArray(decoded),
                                Is.EqualTo(encoded));
                        });
                        sourceMode = sourceMode + 1;
                    }

                    markerValue = markerValue + 1;
                }

                extensionValue = extensionValue + 1;
            }

            paddingValue = paddingValue + 1;
        }
    }

    static RtpPacket CreateCombinationPacket(
        bool hasPadding,
        bool hasExtension,
        bool marker,
        bool hasMaximumSources)
    {
        RtpPacket packet = new RtpPacket();
        packet.HasPadding = hasPadding;
        packet.HasExtension = hasExtension;
        packet.Marker = marker;
        packet.PayloadType = 96;
        packet.SequenceNumber = 0x1234;
        packet.Timestamp = 0x56789ABCU;
        packet.SynchronizationSource = 0xDEF01234U;
        packet.Payload = new byte[] { 0xAA, 0xBB };
        if (hasPadding)
        {
            packet.Padding = new byte[] { 0, 2 };
        }

        if (hasExtension)
        {
            RtpHeaderExtension extension = new RtpHeaderExtension();
            extension.Profile = 0x4321;
            extension.Format = RtpHeaderExtensionFormat.Raw;
            extension.Data = new byte[] { 1, 2, 3, 4 };
            packet.HeaderExtension = extension;
        }

        if (hasMaximumSources)
        {
            packet.ContributingSources = new uint[]
            {
                1U, 2U, 3U, 4U, 5U,
                6U, 7U, 8U, 9U, 10U,
                11U, 12U, 13U, 14U, 15U
            };
        }

        return packet;
    }

    static void AssertGoldenRoundTrip(
        IRtpPacketCodec codec,
        RtpPacket packet,
        byte[] expected)
    {
        int encodedSize = codec.GetRtpPacketEncodedSize(packet);
        byte[] destination = new byte[encodedSize];
        int writtenCount = codec.EncodeRtpPacket(packet, destination);
        byte[] allocated = codec.EncodeRtpPacketToArray(packet);
        RtpPacket decoded = codec.DecodeRtpPacket(expected);
        RtpCodecResult tryDecoded = codec.TryDecodeRtpPacket(expected);
        byte[] roundTrip = codec.EncodeRtpPacketToArray(decoded);

        Assert.Multiple(() =>
        {
            Assert.That(encodedSize, Is.EqualTo(expected.Length));
            Assert.That(writtenCount, Is.EqualTo(expected.Length));
            Assert.That(destination, Is.EqualTo(expected));
            Assert.That(allocated, Is.EqualTo(expected));
            Assert.That(tryDecoded.Success, Is.True);
            Assert.That(tryDecoded.ConsumedCount,
                Is.EqualTo(expected.Length));
            Assert.That(tryDecoded.Packet, Is.Not.Null);
            AssertPacketFields(decoded, packet);
            AssertPacketFields(tryDecoded.Packet!, packet);
            Assert.That(roundTrip, Is.EqualTo(expected));
        });
    }

    static void AssertPacketFields(RtpPacket actual, RtpPacket expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Version, Is.EqualTo(expected.Version));
            Assert.That(actual.HasPadding,
                Is.EqualTo(expected.HasPadding));
            Assert.That(actual.HasExtension,
                Is.EqualTo(expected.HasExtension));
            Assert.That(actual.Marker, Is.EqualTo(expected.Marker));
            Assert.That(actual.PayloadType,
                Is.EqualTo(expected.PayloadType));
            Assert.That(actual.SequenceNumber,
                Is.EqualTo(expected.SequenceNumber));
            Assert.That(actual.Timestamp, Is.EqualTo(expected.Timestamp));
            Assert.That(actual.SynchronizationSource,
                Is.EqualTo(expected.SynchronizationSource));
            Assert.That(actual.ContributingSources,
                Is.EqualTo(expected.ContributingSources));
            Assert.That(actual.Payload, Is.EqualTo(expected.Payload));
            Assert.That(actual.Padding, Is.EqualTo(expected.Padding));
            if (expected.HeaderExtension == null)
            {
                Assert.That(actual.HeaderExtension, Is.Null);
            }
            else
            {
                Assert.That(actual.HeaderExtension?.Profile,
                    Is.EqualTo(expected.HeaderExtension.Profile));
                Assert.That(actual.HeaderExtension?.Data,
                    Is.EqualTo(expected.HeaderExtension.Data));
            }
        });
    }

    static IRtpPacketCodec CreateCodec()
    {
        RtpSessionConfiguration configuration =
            new RtpSessionConfiguration();
        configuration.StrictValidation = true;
        configuration.PreservePadding = true;
        configuration.MaximumRtpPacketSize = 65535;
        return new RtpPacketCodec(configuration);
    }
}
