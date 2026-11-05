using rtp;

namespace rtp.Tests.Wire;

public class RtcpGoodbyeApplicationUnknownBoundaryTests
{
    [Test]
    public void GoodbyeRoundTripsEverySourceCountFromZeroTo31()
    {
        IRtcpPacketCodec codec = CreateCodec(true, true);
        int sourceCount = 0;
        while (sourceCount <= 31)
        {
            RtcpPacket packet = CreateGoodbyePacket(sourceCount, false, []);

            byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
            RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);

            Assert.Multiple(() =>
            {
                Assert.That(encoded, Has.Length.EqualTo(4 + sourceCount * 4));
                Assert.That(encoded[0], Is.EqualTo((byte)(0x80 | sourceCount)));
                Assert.That(encoded[1], Is.EqualTo(203));
                Assert.That(decoded.Header.Count, Is.EqualTo(sourceCount));
                Assert.That(decoded.Goodbye?.Sources,
                    Is.EqualTo(packet.Goodbye!.Sources));
                Assert.That(decoded.Goodbye?.HasReason, Is.False);
                Assert.That(decoded.Goodbye?.Reason, Is.Empty);
                Assert.That(codec.EncodeRtcpPacketToArray(decoded),
                    Is.EqualTo(encoded));
            });
            sourceCount = sourceCount + 1;
        }
    }

    [Test]
    public void GoodbyeRoundTripsEveryReasonLengthFromZeroTo255()
    {
        IRtcpPacketCodec codec = CreateCodec(true, true);
        int reasonLength = 0;
        while (reasonLength <= 255)
        {
            byte[] reason = CreateBytes(reasonLength, (byte)'a');
            RtcpPacket packet = CreateGoodbyePacket(1, true, reason);
            int reasonSectionLength = (1 + reasonLength + 3) & ~3;
            int expectedSize = 8 + reasonSectionLength;

            byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
            RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);
            int reasonLengthOffset = 8;
            int reasonEnd = reasonLengthOffset + 1 + reasonLength;

            Assert.Multiple(() =>
            {
                Assert.That(encoded, Has.Length.EqualTo(expectedSize));
                Assert.That(encoded[reasonLengthOffset], Is.EqualTo(reasonLength));
                Assert.That(encoded[(reasonLengthOffset + 1)..reasonEnd],
                    Is.EqualTo(reason));
                Assert.That(encoded[reasonEnd..], Is.All.Zero);
                Assert.That(decoded.Goodbye?.HasReason, Is.True);
                Assert.That(decoded.Goodbye?.Reason, Is.EqualTo(reason));
                Assert.That(codec.EncodeRtcpPacketToArray(decoded),
                    Is.EqualTo(encoded));
            });
            reasonLength = reasonLength + 1;
        }
    }

    [Test]
    public void GoodbyeRejectsMalformedUtf8OnEncodeAndDecode()
    {
        IRtcpPacketCodec codec = CreateCodec(true, true);
        byte[][] malformedReasons = new byte[][]
        {
            new byte[] { 0x80 },
            new byte[] { 0xC0, 0x80 },
            new byte[] { 0xC2 },
            new byte[] { 0xE0, 0x9F, 0x80 },
            new byte[] { 0xED, 0xA0, 0x80 },
            new byte[] { 0xF0, 0x8F, 0x80, 0x80 },
            new byte[] { 0xF4, 0x90, 0x80, 0x80 },
            new byte[] { 0xFF }
        };
        int reasonIndex = 0;
        while (reasonIndex < malformedReasons.Length)
        {
            RtcpPacket model = CreateGoodbyePacket(
                1,
                true,
                malformedReasons[reasonIndex]);
            byte[] wire = CreateGoodbyeWire(malformedReasons[reasonIndex]);

            RtcpCodecResult encodeResult = codec.TryEncodeRtcpPacket(
                model,
                new byte[64]);
            RtcpCodecResult decodeResult = codec.TryDecodeRtcpPacket(wire);

            Assert.Multiple(() =>
            {
                Assert.That(encodeResult.Success, Is.False);
                Assert.That(encodeResult.ErrorMessage, Does.Contain("UTF-8"));
                Assert.That(decodeResult.Success, Is.False);
                Assert.That(decodeResult.ErrorMessage, Does.Contain("UTF-8"));
            });
            reasonIndex = reasonIndex + 1;
        }
    }

    [Test]
    public void GoodbyeRejectsEveryReasonLengthThatExceedsAvailableBytes()
    {
        IRtcpPacketCodec codec = CreateCodec(true, true);
        int declaredLength = 4;
        while (declaredLength <= 255)
        {
            byte[] source = new byte[]
            {
                0x81, 203, 0, 2,
                0, 0, 0, 1,
                (byte)declaredLength, 0, 0, 0
            };

            RtcpCodecResult result = codec.TryDecodeRtcpPacket(source);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.ConsumedCount, Is.Zero);
                Assert.That(result.ErrorMessage, Does.Contain("reason length"));
            });
            declaredLength = declaredLength + 1;
        }
    }

    [Test]
    public void GoodbyeRejectsNonzeroReasonAlignmentBytesAndExtraTailWords()
    {
        IRtcpPacketCodec codec = CreateCodec(true, true);
        int paddingIndex = 0;
        while (paddingIndex < 3)
        {
            byte[] source = new byte[]
            {
                0x81, 203, 0, 2,
                0, 0, 0, 1,
                0, 0, 0, 0
            };
            source[9 + paddingIndex] = 1;

            RtcpCodecResult result = codec.TryDecodeRtcpPacket(source);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("padding"));
            });
            paddingIndex = paddingIndex + 1;
        }

        byte[] extraTail = new byte[]
        {
            0x81, 203, 0, 3,
            0, 0, 0, 1,
            0, 0, 0, 0,
            0, 0, 0, 0
        };
        Assert.That(codec.TryDecodeRtcpPacket(extraTail).Success, Is.False);
    }

    [Test]
    public void ApplicationDefinedRoundTripsEverySubtypeAndSourceBoundary()
    {
        IRtcpPacketCodec codec = CreateCodec(true, true);
        int subtype = 0;
        while (subtype <= 31)
        {
            RtcpPacket packet = CreateApplicationPacket();
            packet.ApplicationDefined!.Subtype = (byte)subtype;
            packet.ApplicationDefined.SourceIdentifier = subtype == 0 ? 0 : uint.MaxValue;

            byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
            RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);

            Assert.Multiple(() =>
            {
                Assert.That(encoded[0], Is.EqualTo((byte)(0x80 | subtype)));
                Assert.That(encoded[1], Is.EqualTo(204));
                Assert.That(decoded.Header.Count, Is.EqualTo(subtype));
                Assert.That(decoded.ApplicationDefined?.Subtype, Is.EqualTo(subtype));
                Assert.That(decoded.ApplicationDefined?.SourceIdentifier,
                    Is.EqualTo(packet.ApplicationDefined.SourceIdentifier));
                Assert.That(decoded.ApplicationDefined?.Name,
                    Is.EqualTo(packet.ApplicationDefined.Name));
            });
            subtype = subtype + 1;
        }
    }

    [Test]
    public void ApplicationDefinedAcceptsEveryAsciiNameByte()
    {
        IRtcpPacketCodec codec = CreateCodec(true, true);
        int nameValue = 0;
        while (nameValue <= 127)
        {
            RtcpPacket packet = CreateApplicationPacket();
            packet.ApplicationDefined!.Name = new byte[]
            {
                (byte)nameValue,
                (byte)nameValue,
                (byte)nameValue,
                (byte)nameValue
            };

            byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
            RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);

            Assert.Multiple(() =>
            {
                Assert.That(encoded[8..12], Is.EqualTo(packet.ApplicationDefined.Name));
                Assert.That(decoded.ApplicationDefined?.Name,
                    Is.EqualTo(packet.ApplicationDefined.Name));
            });
            nameValue = nameValue + 1;
        }
    }

    [Test]
    public void ApplicationDefinedRejectsEveryNonAsciiNameByteInEveryPosition()
    {
        IRtcpPacketCodec codec = CreateCodec(true, true);
        int nameValue = 128;
        while (nameValue <= byte.MaxValue)
        {
            int position = 0;
            while (position < 4)
            {
                RtcpPacket packet = CreateApplicationPacket();
                packet.ApplicationDefined!.Name[position] = (byte)nameValue;
                byte[] destination = new byte[12];
                Array.Fill(destination, (byte)0xA5);

                RtcpCodecResult encodeResult = codec.TryEncodeRtcpPacket(
                    packet,
                    destination);
                byte[] wire = codec.EncodeRtcpPacketToArray(CreateApplicationPacket());
                wire[8 + position] = (byte)nameValue;
                RtcpCodecResult decodeResult = codec.TryDecodeRtcpPacket(wire);

                Assert.Multiple(() =>
                {
                    Assert.That(encodeResult.Success, Is.False);
                    Assert.That(encodeResult.ErrorMessage, Does.Contain("ASCII"));
                    Assert.That(destination, Has.All.EqualTo(0xA5));
                    Assert.That(decodeResult.Success, Is.False);
                    Assert.That(decodeResult.ErrorMessage, Does.Contain("ASCII"));
                });
                position = position + 1;
            }

            nameValue = nameValue + 1;
        }
    }

    [Test]
    public void ApplicationDefinedRoundTripsOpaqueDataAtEveryAlignedLength()
    {
        IRtcpPacketCodec codec = CreateCodec(true, true);
        int dataLength = 0;
        while (dataLength <= 256)
        {
            RtcpPacket packet = CreateApplicationPacket();
            packet.ApplicationDefined!.Data = CreatePattern(dataLength);
            byte[] expectedData = (byte[])packet.ApplicationDefined.Data.Clone();

            byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
            RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);
            if (packet.ApplicationDefined.Data.Length > 0)
            {
                packet.ApplicationDefined.Data[0] =
                    (byte)(packet.ApplicationDefined.Data[0] + 1);
            }

            Assert.Multiple(() =>
            {
                Assert.That(encoded, Has.Length.EqualTo(12 + dataLength));
                Assert.That(decoded.ApplicationDefined?.Data,
                    Is.EqualTo(expectedData));
                Assert.That(codec.EncodeRtcpPacketToArray(decoded),
                    Is.EqualTo(encoded));
            });
            dataLength = dataLength + 4;
        }
    }

    [Test]
    public void ApplicationDefinedRejectsEveryMisalignedDataRemainder()
    {
        IRtcpPacketCodec codec = CreateCodec(true, true);
        int dataLength = 1;
        while (dataLength <= 7)
        {
            if ((dataLength & 3) != 0)
            {
                RtcpPacket packet = CreateApplicationPacket();
                packet.ApplicationDefined!.Data = new byte[dataLength];
                byte[] destination = new byte[32];
                Array.Fill(destination, (byte)0xA5);

                RtcpCodecResult result = codec.TryEncodeRtcpPacket(packet, destination);

                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.False);
                    Assert.That(result.ErrorMessage, Does.Contain("word-aligned"));
                    Assert.That(destination, Has.All.EqualTo(0xA5));
                });
            }

            dataLength = dataLength + 1;
        }
    }

    [Test]
    public void EveryStrictOpaquePacketTypeAndCountRoundTripsLosslessly()
    {
        IRtcpPacketCodec codec = CreateCodec(true, true);
        int packetType = 192;
        while (packetType <= 223)
        {
            if (packetType < 200 || packetType > 207)
            {
                int count = 0;
                while (count <= 31)
                {
                    byte[] source = new byte[]
                    {
                        (byte)(0x80 | count), (byte)packetType, 0, 1,
                        (byte)packetType, (byte)count, 0xAA, 0x55
                    };
                    byte[] expected = (byte[])source.Clone();

                    RtcpPacket decoded = codec.DecodeRtcpPacket(source);
                    source[4] = 0;
                    byte[] reencoded = codec.EncodeRtcpPacketToArray(decoded);

                    Assert.Multiple(() =>
                    {
                        Assert.That(decoded.UnknownPacket, Is.Not.Null);
                        Assert.That(decoded.UnknownPacket?.PacketType,
                            Is.EqualTo(packetType));
                        Assert.That(decoded.UnknownPacket?.Count, Is.EqualTo(count));
                        Assert.That(decoded.UnknownPacket?.Data,
                            Is.EqualTo(new byte[]
                            {
                                (byte)packetType, (byte)count, 0xAA, 0x55
                            }));
                        Assert.That(reencoded, Is.EqualTo(expected));
                    });
                    count = count + 1;
                }
            }

            packetType = packetType + 1;
        }
    }

    [Test]
    public void TolerantCodecPreservesUnknownPacketTypesAtRangeEdges()
    {
        IRtcpPacketCodec codec = CreateCodec(false, true);
        byte[] packetTypes = new byte[] { 0, 1, 191, 224, 255 };
        int typeIndex = 0;
        while (typeIndex < packetTypes.Length)
        {
            byte packetType = packetTypes[typeIndex];
            byte[] source = new byte[]
            {
                0x9F, packetType, 0, 1,
                1, 2, 3, 4
            };

            RtcpPacket decoded = codec.DecodeRtcpPacket(source);

            Assert.Multiple(() =>
            {
                Assert.That(decoded.Type, Is.EqualTo(RtcpPacketType.Unknown));
                Assert.That(decoded.UnknownPacket?.PacketType, Is.EqualTo(packetType));
                Assert.That(decoded.UnknownPacket?.Count, Is.EqualTo(31));
                Assert.That(decoded.UnknownPacket?.Data,
                    Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
                Assert.That(codec.EncodeRtcpPacketToArray(decoded), Is.EqualTo(source));
            });
            typeIndex = typeIndex + 1;
        }
    }

    [Test]
    public void UnknownPacketsRoundTripOpaqueDataAtBoundarySizes()
    {
        IRtcpPacketCodec codec = CreateCodec(true, true);
        int[] dataLengths = new int[] { 0, 4, 256, 65528 };
        int lengthIndex = 0;
        while (lengthIndex < dataLengths.Length)
        {
            byte[] data = CreatePattern(dataLengths[lengthIndex]);
            RtcpPacket packet = CreateUnknownPacket(220, 17, data);

            byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
            RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);
            if (data.Length > 0)
            {
                data[0] = (byte)(data[0] + 1);
            }

            Assert.Multiple(() =>
            {
                Assert.That(encoded, Has.Length.EqualTo(4 + dataLengths[lengthIndex]));
                Assert.That(decoded.UnknownPacket?.Data,
                    Is.EqualTo(encoded[4..]));
                Assert.That(decoded.UnknownPacket?.Data, Is.Not.SameAs(data));
                Assert.That(codec.EncodeRtcpPacketToArray(decoded),
                    Is.EqualTo(encoded));
            });
            lengthIndex = lengthIndex + 1;
        }
    }

    [Test]
    public void GoodbyeApplicationAndUnknownPaddingCanBePreservedOrNormalized()
    {
        IRtcpPacketCodec preservingCodec = CreateCodec(true, true);
        IRtcpPacketCodec normalizingCodec = CreateCodec(true, false);
        RtcpPacket[] packets = new RtcpPacket[]
        {
            CreateGoodbyePacket(1, false, []),
            CreateApplicationPacket(),
            CreateUnknownPacket(220, 3, new byte[] { 1, 2, 3, 4 })
        };
        int packetIndex = 0;
        while (packetIndex < packets.Length)
        {
            RtcpPacket packet = packets[packetIndex];
            packet.Header.HasPadding = true;
            packet.Padding = CreateBytes(252, 0x31);
            packet.Padding[^1] = 252;

            byte[] source = preservingCodec.EncodeRtcpPacketToArray(packet);
            RtcpPacket preserved = preservingCodec.DecodeRtcpPacket(source);
            RtcpPacket normalized = normalizingCodec.DecodeRtcpPacket(source);
            byte[] normalizedWire = normalizingCodec.EncodeRtcpPacketToArray(preserved);

            Assert.Multiple(() =>
            {
                Assert.That(preserved.Padding, Is.EqualTo(packet.Padding));
                Assert.That(normalized.Padding[..^1], Is.All.Zero);
                Assert.That(normalized.Padding[^1], Is.EqualTo(252));
                Assert.That(normalizedWire[^252..^1], Is.All.Zero);
                Assert.That(normalizedWire[^1], Is.EqualTo(252));
            });
            packetIndex = packetIndex + 1;
        }
    }

    [Test]
    public void EveryTruncationOfGoodbyeApplicationAndUnknownPacketsIsRejected()
    {
        IRtcpPacketCodec codec = CreateCodec(true, true);
        RtcpPacket goodbye = CreateGoodbyePacket(
            2,
            true,
            new byte[] { (byte)'b', (byte)'y', (byte)'e' });
        RtcpPacket application = CreateApplicationPacket();
        application.ApplicationDefined!.Data = new byte[] { 1, 2, 3, 4 };
        RtcpPacket unknown = CreateUnknownPacket(
            220,
            7,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        RtcpPacket[] packets = new RtcpPacket[] { goodbye, application, unknown };
        int packetIndex = 0;
        while (packetIndex < packets.Length)
        {
            byte[] valid = codec.EncodeRtcpPacketToArray(packets[packetIndex]);
            int length = 0;
            while (length < valid.Length)
            {
                RtcpCodecResult result = codec.TryDecodeRtcpPacket(valid[..length]);
                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.False);
                    Assert.That(result.ConsumedCount, Is.Zero);
                    Assert.That(result.Packet, Is.Null);
                });
                length = length + 1;
            }

            packetIndex = packetIndex + 1;
        }
    }

    [Test]
    public void InvalidApplicationUnknownAndGoodbyeModelsDoNotWrite()
    {
        IRtcpPacketCodec codec = CreateCodec(true, true);
        RtcpPacket invalidGoodbye = CreateGoodbyePacket(0, false, []);
        invalidGoodbye.Goodbye!.Sources = new uint[32];
        RtcpPacket invalidApplication = CreateApplicationPacket();
        invalidApplication.ApplicationDefined!.Subtype = 32;
        RtcpPacket invalidUnknownCount = CreateUnknownPacket(220, 32, []);
        RtcpPacket invalidUnknownData = CreateUnknownPacket(220, 1, new byte[3]);
        RtcpPacket mismatchedUnknown = CreateUnknownPacket(220, 1, []);
        mismatchedUnknown.Type = RtcpPacketType.AudioVideoBridging;
        RtcpPacket[] packets = new RtcpPacket[]
        {
            invalidGoodbye,
            invalidApplication,
            invalidUnknownCount,
            invalidUnknownData,
            mismatchedUnknown
        };
        int packetIndex = 0;
        while (packetIndex < packets.Length)
        {
            byte[] destination = new byte[256];
            Array.Fill(destination, (byte)0xA5);

            RtcpCodecResult result = codec.TryEncodeRtcpPacket(
                packets[packetIndex],
                destination);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.WrittenCount, Is.Zero);
                Assert.That(destination, Has.All.EqualTo(0xA5));
            });
            packetIndex = packetIndex + 1;
        }
    }

    static RtcpPacket CreateGoodbyePacket(
        int sourceCount,
        bool hasReason,
        byte[] reason)
    {
        RtcpGoodbye goodbye = new RtcpGoodbye();
        goodbye.Sources = new uint[sourceCount];
        int sourceIndex = 0;
        while (sourceIndex < goodbye.Sources.Length)
        {
            goodbye.Sources[sourceIndex] = (uint)(0x10203040 + sourceIndex);
            sourceIndex = sourceIndex + 1;
        }

        goodbye.HasReason = hasReason;
        goodbye.Reason = reason;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.Goodbye;
        packet.Goodbye = goodbye;
        return packet;
    }

    static RtcpPacket CreateApplicationPacket()
    {
        RtcpApplicationDefined application = new RtcpApplicationDefined();
        application.Name = new byte[]
        {
            (byte)'T', (byte)'E', (byte)'S', (byte)'T'
        };
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.ApplicationDefined;
        packet.ApplicationDefined = application;
        return packet;
    }

    static RtcpPacket CreateUnknownPacket(byte packetType, byte count, byte[] data)
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

    static byte[] CreateGoodbyeWire(byte[] reason)
    {
        int reasonSectionLength = (1 + reason.Length + 3) & ~3;
        byte[] source = new byte[8 + reasonSectionLength];
        source[0] = 0x81;
        source[1] = 203;
        int lengthInWordsMinusOne = source.Length / 4 - 1;
        source[2] = (byte)(lengthInWordsMinusOne >> 8);
        source[3] = (byte)lengthInWordsMinusOne;
        source[7] = 1;
        source[8] = (byte)reason.Length;
        reason.CopyTo(source, 9);
        return source;
    }

    static byte[] CreatePattern(int length)
    {
        byte[] data = new byte[length];
        int dataIndex = 0;
        while (dataIndex < data.Length)
        {
            data[dataIndex] = (byte)(dataIndex * 37 + 11);
            dataIndex = dataIndex + 1;
        }

        return data;
    }

    static byte[] CreateBytes(int length, int seed)
    {
        byte[] data = new byte[length];
        int dataIndex = 0;
        while (dataIndex < data.Length)
        {
            data[dataIndex] = (byte)(seed + dataIndex);
            dataIndex = dataIndex + 1;
        }

        return data;
    }

    static byte[] CreateBytes(int length, byte value)
    {
        byte[] data = new byte[length];
        Array.Fill(data, value);
        return data;
    }

    static IRtcpPacketCodec CreateCodec(bool strictValidation, bool preservePadding)
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.StrictValidation = strictValidation;
        configuration.PreservePadding = preservePadding;
        configuration.MaximumRtcpCompoundPacketSize = 65535;
        return new RtcpPacketCodec(configuration);
    }
}
