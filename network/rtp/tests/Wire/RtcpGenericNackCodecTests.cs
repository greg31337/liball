using rtp;

namespace rtp.Tests.Wire;

public class RtcpGenericNackCodecTests
{
    [Test]
    public void PackingSortsDeduplicatesAndGroupsAcrossSequenceWrap()
    {
        IRtcpPacketCodec codec = CreateCodec();
        ushort[] lostSequenceNumbers = new ushort[]
        {
            1, 65535, 0, 65534, 20, 1, 36, 35
        };

        RtcpGenericNack genericNack = codec.PackRtcpGenericNack(
            lostSequenceNumbers);
        ushort[] expanded = codec.ExpandRtcpGenericNack(genericNack);

        Assert.Multiple(() =>
        {
            Assert.That(genericNack.Entries.Length, Is.EqualTo(2));
            Assert.That(genericNack.Entries[0].PacketIdentifier, Is.EqualTo(65534));
            Assert.That(genericNack.Entries[0].LostPacketBitmask, Is.EqualTo(0x0007));
            Assert.That(genericNack.Entries[1].PacketIdentifier, Is.EqualTo(20));
            Assert.That(genericNack.Entries[1].LostPacketBitmask, Is.EqualTo(0xC000));
            Assert.That(expanded,
                Is.EqualTo(new ushort[] { 65534, 65535, 0, 1, 20, 35, 36 }));
        });
    }

    [Test]
    public void GenericNackWireVectorRoundTripsTypedPidAndBlpFields()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x81, 205, 0, 3,
            0x01, 0x02, 0x03, 0x04,
            0x05, 0x06, 0x07, 0x08,
            0xFF, 0xFE, 0x00, 0x07
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        ushort[] expanded = codec.ExpandRtcpGenericNack(packet.Feedback!.GenericNack!);
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(packet.Type, Is.EqualTo(RtcpPacketType.TransportFeedback));
            Assert.That(packet.Feedback?.TransportFormat,
                Is.EqualTo(RtcpTransportFeedbackFormat.GenericNack));
            Assert.That(packet.Feedback?.GenericNack?.Entries.Length, Is.EqualTo(1));
            Assert.That(packet.Feedback?.GenericNack?.Entries[0].PacketIdentifier,
                Is.EqualTo(65534));
            Assert.That(packet.Feedback?.GenericNack?.Entries[0].LostPacketBitmask,
                Is.EqualTo(7));
            Assert.That(expanded,
                Is.EqualTo(new ushort[] { 65534, 65535, 0, 1 }));
            Assert.That(encoded, Is.EqualTo(source));
        });
    }

    [Test]
    public void DecodeCanonicalizesOverlappingAndDuplicateNackEntries()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x81, 205, 0, 4,
            0, 0, 0, 1,
            0, 0, 0, 2,
            0, 100, 0, 1,
            0, 101, 0, 1
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        RtcpGenericNack genericNack = packet.Feedback!.GenericNack!;
        ushort[] expanded = codec.ExpandRtcpGenericNack(genericNack);
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(genericNack.Entries.Length, Is.EqualTo(1));
            Assert.That(genericNack.Entries[0].PacketIdentifier, Is.EqualTo(100));
            Assert.That(genericNack.Entries[0].LostPacketBitmask, Is.EqualTo(3));
            Assert.That(expanded, Is.EqualTo(new ushort[] { 100, 101, 102 }));
            Assert.That(packet.Feedback?.RawFeedbackControlInformation,
                Is.EqualTo(new byte[] { 0, 100, 0, 1, 0, 101, 0, 1 }));
            Assert.That(encoded,
                Is.EqualTo(new byte[]
                {
                    0x81, 205, 0, 3,
                    0, 0, 0, 1,
                    0, 0, 0, 2,
                    0, 100, 0, 3
                }));
        });
    }

    [Test]
    public void TypedGenericNackConstructionUsesCanonicalEntriesInsteadOfRawFci()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpGenericNack genericNack = new RtcpGenericNack();
        RtcpGenericNackEntry later = new RtcpGenericNackEntry();
        later.PacketIdentifier = 201;
        later.LostPacketBitmask = 1;
        RtcpGenericNackEntry earlier = new RtcpGenericNackEntry();
        earlier.PacketIdentifier = 200;
        earlier.LostPacketBitmask = 0;
        genericNack.Entries = new RtcpGenericNackEntry[] { later, earlier };
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.TransportLayer;
        feedback.TransportFormat = RtcpTransportFeedbackFormat.GenericNack;
        feedback.SenderSourceIdentifier = 1U;
        feedback.MediaSourceIdentifier = 2U;
        feedback.GenericNack = genericNack;
        feedback.RawFeedbackControlInformation = new byte[] { 9, 9, 9, 9 };
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.TransportFeedback;
        packet.Feedback = feedback;

        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.That(encoded,
            Is.EqualTo(new byte[]
            {
                0x81, 205, 0, 3,
                0, 0, 0, 1,
                0, 0, 0, 2,
                0, 200, 0, 3
            }));
    }

    [Test]
    public void EveryPidBlpBitRepresentsTheCorrespondingFollowingSequence()
    {
        IRtcpPacketCodec codec = CreateCodec();
        int distance = 1;
        while (distance <= 16)
        {
            ushort[] lost = new ushort[]
            {
                40000,
                (ushort)(40000 + distance)
            };

            RtcpGenericNack packed = codec.PackRtcpGenericNack(lost);
            ushort[] expanded = codec.ExpandRtcpGenericNack(packed);

            Assert.Multiple(() =>
            {
                Assert.That(packed.Entries, Has.Length.EqualTo(1));
                Assert.That(packed.Entries[0].PacketIdentifier,
                    Is.EqualTo(40000));
                Assert.That(packed.Entries[0].LostPacketBitmask,
                    Is.EqualTo((ushort)(1U << (distance - 1))));
                Assert.That(expanded, Is.EqualTo(lost));
            });
            distance = distance + 1;
        }

        RtcpGenericNack outsideBlp = codec.PackRtcpGenericNack(
            new ushort[] { 40000, 40017 });
        Assert.Multiple(() =>
        {
            Assert.That(outsideBlp.Entries, Has.Length.EqualTo(2));
            Assert.That(outsideBlp.Entries[0].PacketIdentifier,
                Is.EqualTo(40000));
            Assert.That(outsideBlp.Entries[0].LostPacketBitmask, Is.Zero);
            Assert.That(outsideBlp.Entries[1].PacketIdentifier,
                Is.EqualTo(40017));
        });
    }

    [Test]
    public void DenseLossSetsFillBlpWordsAndContinueCanonicallyAcrossWrap()
    {
        IRtcpPacketCodec codec = CreateCodec();
        int lostCount = 1;
        while (lostCount <= 40)
        {
            ushort[] lost = new ushort[lostCount];
            int lostIndex = 0;
            while (lostIndex < lost.Length)
            {
                lost[lostIndex] = (ushort)((65520 + lostIndex) & 65535);
                lostIndex = lostIndex + 1;
            }

            RtcpGenericNack packed = codec.PackRtcpGenericNack(lost);
            ushort[] expanded = codec.ExpandRtcpGenericNack(packed);
            int expectedEntryCount = (lostCount + 16) / 17;

            Assert.That(packed.Entries, Has.Length.EqualTo(expectedEntryCount));
            int entryIndex = 0;
            while (entryIndex < packed.Entries.Length)
            {
                int representedAfterPid = lostCount - entryIndex * 17 - 1;
                if (representedAfterPid > 16)
                {
                    representedAfterPid = 16;
                }

                ushort expectedBitmask = 0;
                if (representedAfterPid == 16)
                {
                    expectedBitmask = ushort.MaxValue;
                }
                else if (representedAfterPid > 0)
                {
                    expectedBitmask = (ushort)((1U << representedAfterPid) - 1);
                }

                Assert.Multiple(() =>
                {
                    Assert.That(packed.Entries[entryIndex].PacketIdentifier,
                        Is.EqualTo((ushort)((65520 + entryIndex * 17) & 65535)));
                    Assert.That(packed.Entries[entryIndex].LostPacketBitmask,
                        Is.EqualTo(expectedBitmask));
                });
                entryIndex = entryIndex + 1;
            }

            Assert.That(expanded, Is.EqualTo(lost));
            lostCount = lostCount + 1;
        }
    }

    [Test]
    public void SparsePackingIsIndependentOfInputOrderAndDuplicates()
    {
        IRtcpPacketCodec codec = CreateCodec();
        ushort[][] inputs = new ushort[][]
        {
            new ushort[] { 100, 117, 500, 501, 502 },
            new ushort[] { 502, 100, 500, 117, 501 },
            new ushort[] { 500, 100, 501, 100, 117, 502, 501 }
        };
        int inputIndex = 0;
        while (inputIndex < inputs.Length)
        {
            RtcpGenericNack packed = codec.PackRtcpGenericNack(inputs[inputIndex]);
            ushort[] expanded = codec.ExpandRtcpGenericNack(packed);

            Assert.Multiple(() =>
            {
                Assert.That(GetPacketIdentifiers(packed),
                    Is.EqualTo(new ushort[] { 100, 117, 500 }));
                Assert.That(GetBitmasks(packed),
                    Is.EqualTo(new ushort[] { 0, 0, 3 }));
                Assert.That(expanded,
                    Is.EqualTo(new ushort[] { 100, 117, 500, 501, 502 }));
            });
            inputIndex = inputIndex + 1;
        }
    }

    [Test]
    public void PionGenerationVectorsProduceIdenticalPidAndBlpPairs()
    {
        IRtcpPacketCodec codec = CreateCodec();

        AssertPacked(
            codec,
            new ushort[] { 100 },
            new ushort[] { 100 },
            new ushort[] { 0 });
        AssertPacked(
            codec,
            new ushort[] { 100, 101, 105, 115 },
            new ushort[] { 100 },
            new ushort[] { 0x4011 });
        AssertPacked(
            codec,
            new ushort[] { 100, 117, 500, 501, 502 },
            new ushort[] { 100, 117, 500 },
            new ushort[] { 0, 0, 3 });
    }

    [Test]
    public void PionWireVectorsDecodeAndEncodeWithIdenticalFields()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[][] vectors = new byte[][]
        {
            new byte[]
            {
                0x81, 0xCD, 0, 3,
                0x90, 0x2F, 0x9E, 0x2E,
                0x90, 0x2F, 0x9E, 0x2E,
                0xAA, 0xAA, 0x55, 0x55
            },
            new byte[]
            {
                0x81, 0xCD, 0, 4,
                0x90, 0x2F, 0x9E, 0x2E,
                0x90, 0x2F, 0x9E, 0x2E,
                0, 1, 0, 0xAA,
                4, 10, 0, 5
            }
        };
        int vectorIndex = 0;
        while (vectorIndex < vectors.Length)
        {
            RtcpPacket packet = codec.DecodeRtcpPacket(vectors[vectorIndex]);

            Assert.Multiple(() =>
            {
                Assert.That(packet.Feedback?.SenderSourceIdentifier,
                    Is.EqualTo(0x902F9E2EU));
                Assert.That(packet.Feedback?.MediaSourceIdentifier,
                    Is.EqualTo(0x902F9E2EU));
                Assert.That(codec.EncodeRtcpPacketToArray(packet),
                    Is.EqualTo(vectors[vectorIndex]));
            });
            vectorIndex = vectorIndex + 1;
        }
    }

    [Test]
    public void OverlappingWraparoundEntriesCanonicalizeToOnePair()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x81, 205, 0, 4,
            0, 0, 0, 1,
            0, 0, 0, 2,
            0xFF, 0xFF, 0, 1,
            0, 0, 0, 1
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        RtcpGenericNack genericNack = packet.Feedback!.GenericNack!;

        Assert.Multiple(() =>
        {
            Assert.That(genericNack.Entries, Has.Length.EqualTo(1));
            Assert.That(genericNack.Entries[0].PacketIdentifier,
                Is.EqualTo(ushort.MaxValue));
            Assert.That(genericNack.Entries[0].LostPacketBitmask,
                Is.EqualTo(3));
            Assert.That(codec.ExpandRtcpGenericNack(genericNack),
                Is.EqualTo(new ushort[] { ushort.MaxValue, 0, 1 }));
            Assert.That(codec.EncodeRtcpPacketToArray(packet),
                Is.EqualTo(new byte[]
                {
                    0x81, 205, 0, 3,
                    0, 0, 0, 1,
                    0, 0, 0, 2,
                    0xFF, 0xFF, 0, 3
                }));
        });
    }

    [Test]
    public void EveryIncompleteFciRemainderAndPacketTruncationIsRejected()
    {
        IRtcpPacketCodec codec = CreateCodec();
        int fciLength = 0;
        while (fciLength < 4)
        {
            byte[] malformed = CreateIncompleteFciPacket(fciLength);
            RtcpCodecResult result = codec.TryDecodeRtcpPacket(malformed);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.ConsumedCount, Is.Zero);
                Assert.That(result.Packet, Is.Null);
            });
            fciLength = fciLength + 1;
        }

        RtcpPacket validPacket = CreateGenericNackPacket(
            new ushort[] { 100, 101, 105, 115, 500 });
        byte[] valid = codec.EncodeRtcpPacketToArray(validPacket);
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
    }

    [Test]
    public void EmptyAndPartialGenericNackFciAreRejectedAtomically()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] emptyFci = new byte[]
        {
            0x81, 205, 0, 2,
            0, 0, 0, 1,
            0, 0, 0, 2
        };
        byte[] partialFci = new byte[]
        {
            0xA1, 205, 0, 3,
            0, 0, 0, 1,
            0, 0, 0, 2,
            0, 100, 0, 2
        };
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.TransportLayer;
        feedback.TransportFormat = RtcpTransportFeedbackFormat.GenericNack;
        feedback.GenericNack = new RtcpGenericNack();
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.TransportFeedback;
        packet.Feedback = feedback;
        byte[] destination = new byte[32];
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = 77;
            index = index + 1;
        }

        RtcpCodecResult encodeResult = codec.TryEncodeRtcpPacket(packet, destination);

        Assert.Multiple(() =>
        {
            Assert.That(codec.TryDecodeRtcpPacket(emptyFci).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(partialFci).Success, Is.False);
            Assert.That(encodeResult.Success, Is.False);
            Assert.That(encodeResult.WrittenCount, Is.Zero);
            Assert.That(destination, Has.All.EqualTo(77));
            Assert.Throws<RtcpPacketFormatException>(() =>
                codec.PackRtcpGenericNack([]));
            Assert.Throws<RtcpPacketFormatException>(() =>
                codec.ExpandRtcpGenericNack(new RtcpGenericNack()));
        });
    }

    static void AssertPacked(
        IRtcpPacketCodec codec,
        ushort[] lost,
        ushort[] expectedPacketIdentifiers,
        ushort[] expectedBitmasks)
    {
        RtcpGenericNack packed = codec.PackRtcpGenericNack(lost);
        Assert.Multiple(() =>
        {
            Assert.That(GetPacketIdentifiers(packed),
                Is.EqualTo(expectedPacketIdentifiers));
            Assert.That(GetBitmasks(packed), Is.EqualTo(expectedBitmasks));
            Assert.That(codec.ExpandRtcpGenericNack(packed), Is.EqualTo(lost));
        });
    }

    static ushort[] GetPacketIdentifiers(RtcpGenericNack genericNack)
    {
        ushort[] packetIdentifiers = new ushort[genericNack.Entries.Length];
        int entryIndex = 0;
        while (entryIndex < genericNack.Entries.Length)
        {
            packetIdentifiers[entryIndex] =
                genericNack.Entries[entryIndex].PacketIdentifier;
            entryIndex = entryIndex + 1;
        }

        return packetIdentifiers;
    }

    static ushort[] GetBitmasks(RtcpGenericNack genericNack)
    {
        ushort[] bitmasks = new ushort[genericNack.Entries.Length];
        int entryIndex = 0;
        while (entryIndex < genericNack.Entries.Length)
        {
            bitmasks[entryIndex] = genericNack.Entries[entryIndex].LostPacketBitmask;
            entryIndex = entryIndex + 1;
        }

        return bitmasks;
    }

    static byte[] CreateIncompleteFciPacket(int fciLength)
    {
        if (fciLength == 0)
        {
            return new byte[]
            {
                0x81, 205, 0, 2,
                0, 0, 0, 1,
                0, 0, 0, 2
            };
        }

        byte[] packet = new byte[16];
        packet[0] = 0xA1;
        packet[1] = 205;
        packet[3] = 3;
        packet[7] = 1;
        packet[11] = 2;
        int paddingLength = 4 - fciLength;
        packet[^1] = (byte)paddingLength;
        return packet;
    }

    static RtcpPacket CreateGenericNackPacket(ushort[] lostSequenceNumbers)
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.TransportLayer;
        feedback.TransportFormat = RtcpTransportFeedbackFormat.GenericNack;
        feedback.SenderSourceIdentifier = 1U;
        feedback.MediaSourceIdentifier = 2U;
        feedback.GenericNack = codec.PackRtcpGenericNack(lostSequenceNumbers);
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
