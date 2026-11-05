using System.Text;
using rtp;

namespace rtp.Tests.Wire;

public class RtcpSourceDescriptionBoundaryTests
{
    [Test]
    public void EveryStandardItemTypeRoundTripsEmptyAndMaximumUtf8Text()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpSdesItemType[] types = GetStandardTypes();
        byte[][] values = new byte[][]
        {
            [],
            Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("界", 85)))
        };
        int typeIndex = 0;
        while (typeIndex < types.Length)
        {
            int valueIndex = 0;
            while (valueIndex < values.Length)
            {
                RtcpSourceDescriptionItem item = CreateItem(
                    types[typeIndex],
                    values[valueIndex]);
                RtcpPacket packet = CreatePacket(
                    new RtcpSourceDescriptionChunk[]
                    {
                        CreateChunk((uint)(typeIndex + 1), item)
                    });

                byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
                RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);
                RtcpSourceDescriptionItem decodedItem =
                    decoded.SourceDescription!.Chunks[0].Items[0];

                Assert.Multiple(() =>
                {
                    Assert.That(values[valueIndex], Has.Length.EqualTo(
                        valueIndex == 0 ? 0 : 255));
                    Assert.That(decodedItem.Type, Is.EqualTo(types[typeIndex]));
                    Assert.That(decodedItem.WireType, Is.EqualTo((byte)types[typeIndex]));
                    Assert.That(decodedItem.Value, Is.EqualTo(values[valueIndex]));
                    Assert.That(decodedItem.PrivatePrefix, Is.Empty);
                    Assert.That(codec.EncodeRtcpPacketToArray(decoded),
                        Is.EqualTo(encoded));
                });
                valueIndex = valueIndex + 1;
            }

            typeIndex = typeIndex + 1;
        }
    }

    [Test]
    public void EveryStandardTextLengthFromZeroTo255RoundTrips()
    {
        IRtcpPacketCodec codec = CreateCodec();
        int length = 0;
        while (length <= 255)
        {
            byte[] value = CreateAsciiBytes(length, (byte)'a');
            RtcpPacket packet = CreatePacket(
                new RtcpSourceDescriptionChunk[]
                {
                    CreateChunk(1, CreateItem(RtcpSdesItemType.Note, value))
                });

            byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
            RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);

            Assert.Multiple(() =>
            {
                Assert.That(decoded.SourceDescription!.Chunks[0].Items[0].Value,
                    Is.EqualTo(value));
                Assert.That(decoded.Header.LengthInWordsMinusOne,
                    Is.EqualTo(encoded.Length / 4 - 1));
            });
            length = length + 1;
        }
    }

    [Test]
    public void ValidUtf8SequencesRoundTripWithoutTextNormalization()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] value = Encoding.UTF8.GetBytes("A¢€😀\0Z");
        RtcpPacket packet = CreatePacket(
            new RtcpSourceDescriptionChunk[]
            {
                CreateChunk(0x01020304U, CreateItem(RtcpSdesItemType.Name, value))
            });

        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
        RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.SourceDescription!.Chunks[0].Items[0].Value,
                Is.EqualTo(value));
            Assert.That(codec.EncodeRtcpPacketToArray(decoded), Is.EqualTo(encoded));
        });
    }

    [Test]
    public void EveryKnownItemRejectsMalformedUtf8DuringEncodeAndDecode()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[][] malformedValues = new byte[][]
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
        RtcpSdesItemType[] types = GetStandardTypes();
        int typeIndex = 0;
        while (typeIndex < types.Length)
        {
            int valueIndex = 0;
            while (valueIndex < malformedValues.Length)
            {
                RtcpPacket model = CreatePacket(
                    new RtcpSourceDescriptionChunk[]
                    {
                        CreateChunk(
                            1,
                            CreateItem(types[typeIndex], malformedValues[valueIndex]))
                    });
                byte[] wire = CreateSingleItemWirePacket(
                    (byte)types[typeIndex],
                    malformedValues[valueIndex]);

                RtcpCodecResult encodeResult = codec.TryEncodeRtcpPacket(
                    model,
                    new byte[512]);
                RtcpCodecResult decodeResult = codec.TryDecodeRtcpPacket(wire);

                Assert.Multiple(() =>
                {
                    Assert.That(encodeResult.Success, Is.False);
                    Assert.That(encodeResult.ErrorMessage, Does.Contain("UTF-8"));
                    Assert.That(decodeResult.Success, Is.False);
                    Assert.That(decodeResult.ErrorMessage, Does.Contain("UTF-8"));
                });
                valueIndex = valueIndex + 1;
            }

            typeIndex = typeIndex + 1;
        }
    }

    [Test]
    public void PrivateItemRoundTripsAllCombinedLengthBoundaries()
    {
        IRtcpPacketCodec codec = CreateCodec();
        int[] prefixLengths = new int[] { 0, 254, 0, 127 };
        int[] valueLengths = new int[] { 0, 0, 254, 127 };
        int caseIndex = 0;
        while (caseIndex < prefixLengths.Length)
        {
            byte[] prefix = CreateAsciiBytes(prefixLengths[caseIndex], (byte)'p');
            byte[] value = CreateAsciiBytes(valueLengths[caseIndex], (byte)'v');
            RtcpSourceDescriptionItem item = CreateItem(
                RtcpSdesItemType.Private,
                value);
            item.PrivatePrefix = prefix;
            RtcpPacket packet = CreatePacket(
                new RtcpSourceDescriptionChunk[] { CreateChunk(1, item) });

            byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
            RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);
            RtcpSourceDescriptionItem decodedItem =
                decoded.SourceDescription!.Chunks[0].Items[0];

            Assert.Multiple(() =>
            {
                Assert.That(encoded[8], Is.EqualTo((byte)RtcpSdesItemType.Private));
                Assert.That(encoded[9],
                    Is.EqualTo(1 + prefix.Length + value.Length));
                Assert.That(encoded[10], Is.EqualTo(prefix.Length));
                Assert.That(decodedItem.PrivatePrefix, Is.EqualTo(prefix));
                Assert.That(decodedItem.Value, Is.EqualTo(value));
                Assert.That(codec.EncodeRtcpPacketToArray(decoded), Is.EqualTo(encoded));
            });
            caseIndex = caseIndex + 1;
        }
    }

    [Test]
    public void PrivateItemRejectsOversizeAndMalformedUtf8Components()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpSourceDescriptionItem oversizedPrefix = CreateItem(
            RtcpSdesItemType.Private,
            []);
        oversizedPrefix.PrivatePrefix = CreateAsciiBytes(255, (byte)'p');
        RtcpSourceDescriptionItem oversizedValue = CreateItem(
            RtcpSdesItemType.Private,
            CreateAsciiBytes(255, (byte)'v'));
        RtcpSourceDescriptionItem malformedPrefix = CreateItem(
            RtcpSdesItemType.Private,
            new byte[] { (byte)'v' });
        malformedPrefix.PrivatePrefix = new byte[] { 0xFF };
        RtcpSourceDescriptionItem malformedValue = CreateItem(
            RtcpSdesItemType.Private,
            new byte[] { 0xFF });
        malformedValue.PrivatePrefix = new byte[] { (byte)'p' };
        RtcpSourceDescriptionItem[] items = new RtcpSourceDescriptionItem[]
        {
            oversizedPrefix,
            oversizedValue,
            malformedPrefix,
            malformedValue
        };
        int itemIndex = 0;
        while (itemIndex < items.Length)
        {
            RtcpPacket packet = CreatePacket(
                new RtcpSourceDescriptionChunk[] { CreateChunk(1, items[itemIndex]) });
            RtcpCodecResult result = codec.TryEncodeRtcpPacket(packet, new byte[512]);
            Assert.That(result.Success, Is.False);
            itemIndex = itemIndex + 1;
        }

        byte[] malformedPrefixWire = CreatePrivateWirePacket(
            new byte[] { 0xFF },
            new byte[] { (byte)'v' });
        byte[] malformedValueWire = CreatePrivateWirePacket(
            new byte[] { (byte)'p' },
            new byte[] { 0xFF });

        Assert.Multiple(() =>
        {
            Assert.That(codec.TryDecodeRtcpPacket(malformedPrefixWire).Success,
                Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(malformedValueWire).Success,
                Is.False);
        });
    }

    [Test]
    public void EveryUnknownWireTypePreservesOpaqueItemBytes()
    {
        IRtcpPacketCodec codec = CreateCodec();
        int wireType = 9;
        while (wireType <= byte.MaxValue)
        {
            byte[] value = new byte[] { 0xFF, (byte)wireType };
            byte[] source = CreateSingleItemWirePacket((byte)wireType, value);
            byte[] expected = (byte[])source.Clone();

            RtcpPacket decoded = codec.DecodeRtcpPacket(source);
            source[10] = 0;
            RtcpSourceDescriptionItem item =
                decoded.SourceDescription!.Chunks[0].Items[0];
            byte[] reencoded = codec.EncodeRtcpPacketToArray(decoded);

            Assert.Multiple(() =>
            {
                Assert.That(item.Type, Is.EqualTo(RtcpSdesItemType.Unknown));
                Assert.That(item.WireType, Is.EqualTo(wireType));
                Assert.That(item.Value, Is.EqualTo(value));
                Assert.That(reencoded, Is.EqualTo(expected));
            });
            wireType = wireType + 1;
        }
    }

    [Test]
    public void EncoderProducesEveryPossibleChunkAlignment()
    {
        IRtcpPacketCodec codec = CreateCodec();
        int[] valueLengths = new int[] { 1, 4, 3, 2 };
        int[] expectedPaddingLengths = new int[] { 0, 1, 2, 3 };
        int caseIndex = 0;
        while (caseIndex < valueLengths.Length)
        {
            byte[] value = CreateAsciiBytes(valueLengths[caseIndex], (byte)'a');
            RtcpPacket packet = CreatePacket(
                new RtcpSourceDescriptionChunk[]
                {
                    CreateChunk(1, CreateItem(RtcpSdesItemType.Name, value))
                });
            byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
            int endOffset = 10 + value.Length;
            int paddingLength = expectedPaddingLengths[caseIndex];

            Assert.Multiple(() =>
            {
                Assert.That(encoded[endOffset], Is.Zero);
                Assert.That(encoded[(endOffset + 1)..],
                    Has.Length.EqualTo(paddingLength));
                Assert.That(encoded[(endOffset + 1)..], Is.All.Zero);
                Assert.That(encoded.Length % 4, Is.Zero);
                Assert.That(codec.EncodeRtcpPacketToArray(
                    codec.DecodeRtcpPacket(encoded)), Is.EqualTo(encoded));
            });
            caseIndex = caseIndex + 1;
        }
    }

    [Test]
    public void MultipleChunksKeepIndependentEndsAlignmentItemsAndSources()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpSourceDescriptionItem privateItem = CreateItem(
            RtcpSdesItemType.Private,
            Encoding.UTF8.GetBytes("value"));
        privateItem.PrivatePrefix = Encoding.UTF8.GetBytes("app");
        RtcpPacket packet = CreatePacket(
            new RtcpSourceDescriptionChunk[]
            {
                CreateChunk(
                    0x01020304U,
                    CreateItem(RtcpSdesItemType.CanonicalName,
                        Encoding.UTF8.GetBytes("one@example")),
                    CreateItem(RtcpSdesItemType.Name,
                        Encoding.UTF8.GetBytes("One"))),
                CreateChunk(0x11121314U),
                CreateChunk(0x21222324U, privateItem)
            });

        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
        RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);
        RtcpSourceDescriptionChunk[] chunks = decoded.SourceDescription!.Chunks;

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Header.Count, Is.EqualTo(3));
            Assert.That(chunks, Has.Length.EqualTo(3));
            Assert.That(chunks[0].SourceIdentifier, Is.EqualTo(0x01020304U));
            Assert.That(chunks[0].Items, Has.Length.EqualTo(2));
            Assert.That(chunks[1].SourceIdentifier, Is.EqualTo(0x11121314U));
            Assert.That(chunks[1].Items, Is.Empty);
            Assert.That(chunks[2].SourceIdentifier, Is.EqualTo(0x21222324U));
            Assert.That(chunks[2].Items[0].PrivatePrefix,
                Is.EqualTo(Encoding.UTF8.GetBytes("app")));
            Assert.That(chunks[2].Items[0].Value,
                Is.EqualTo(Encoding.UTF8.GetBytes("value")));
            Assert.That(codec.EncodeRtcpPacketToArray(decoded), Is.EqualTo(encoded));
        });
    }

    [Test]
    public void StandaloneSdesAllowsMissingCnameButStrictCompoundRejectsIt()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpPacket description = CreatePacket(
            new RtcpSourceDescriptionChunk[]
            {
                CreateChunk(
                    1,
                    CreateItem(RtcpSdesItemType.Name, Encoding.UTF8.GetBytes("name")))
            });
        RtcpPacket receiverReport = new RtcpPacket();
        receiverReport.Type = RtcpPacketType.ReceiverReport;
        receiverReport.ReceiverReport = new RtcpReceiverReport();
        receiverReport.ReceiverReport.SenderSynchronizationSource = 1;
        RtcpCompoundPacket compound = new RtcpCompoundPacket();
        compound.Packets = new RtcpPacket[] { receiverReport, description };

        byte[] standalone = codec.EncodeRtcpPacketToArray(description);
        RtcpCodecResult compoundResult = codec.TryEncodeRtcpCompoundPacket(
            compound,
            new byte[128]);

        Assert.Multiple(() =>
        {
            Assert.That(codec.DecodeRtcpPacket(standalone).SourceDescription,
                Is.Not.Null);
            Assert.That(compoundResult.Success, Is.False);
            Assert.That(compoundResult.ErrorMessage, Does.Contain("CNAME"));
        });
    }

    [Test]
    public void EveryTruncationOfAMultiChunkPacketIsRejected()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpPacket packet = CreatePacket(
            new RtcpSourceDescriptionChunk[]
            {
                CreateChunk(
                    1,
                    CreateItem(RtcpSdesItemType.CanonicalName,
                        Encoding.UTF8.GetBytes("first"))),
                CreateChunk(
                    2,
                    CreateItem(RtcpSdesItemType.Note,
                        Encoding.UTF8.GetBytes("second")))
            });
        byte[] valid = codec.EncodeRtcpPacketToArray(packet);
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
    public void MalformedItemHeadersLengthsEndsAndAlignmentAreRejected()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[][] malformedBodies = new byte[][]
        {
            new byte[] { 0, 0, 0, 1 },
            new byte[] { 0, 0, 0, 1, 1, 1, (byte)'a', 1 },
            new byte[] { 0, 0, 0, 1, 1, 3, (byte)'a', (byte)'b' },
            new byte[] { 0, 0, 0, 1, 0, 1, 0, 0 },
            new byte[] { 0, 0, 0, 1, 8, 0, 0, 0 },
            new byte[] { 0, 0, 0, 1, 8, 2, 2, (byte)'p', 0, 0, 0, 0 }
        };
        int bodyIndex = 0;
        while (bodyIndex < malformedBodies.Length)
        {
            byte[] source = CreateWirePacket(1, malformedBodies[bodyIndex]);
            RtcpCodecResult result = codec.TryDecodeRtcpPacket(source);
            Assert.That(result.Success, Is.False, $"body {bodyIndex}");
            bodyIndex = bodyIndex + 1;
        }

        byte[] countMismatch = CreateWirePacket(
            0,
            new byte[] { 0, 0, 0, 1, 0, 0, 0, 0 });
        Assert.That(codec.TryDecodeRtcpPacket(countMismatch).Success, Is.False);
    }

    [Test]
    public void InvalidModelsAndItemLengthsFailBeforeWriting()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpSourceDescriptionItem end = CreateItem(RtcpSdesItemType.End, []);
        RtcpSourceDescriptionItem oversized = CreateItem(
            RtcpSdesItemType.Name,
            new byte[256]);
        RtcpSourceDescriptionItem invalidUnknown = CreateItem(
            RtcpSdesItemType.Unknown,
            []);
        invalidUnknown.WireType = 8;
        RtcpSourceDescriptionItem invalidType = CreateItem(
            (RtcpSdesItemType)9,
            []);
        RtcpSourceDescriptionItem[] invalidItems = new RtcpSourceDescriptionItem[]
        {
            end,
            oversized,
            invalidUnknown,
            invalidType
        };
        int itemIndex = 0;
        while (itemIndex < invalidItems.Length)
        {
            RtcpPacket packet = CreatePacket(
                new RtcpSourceDescriptionChunk[]
                {
                    CreateChunk(1, invalidItems[itemIndex])
                });
            byte[] destination = new byte[512];
            Array.Fill(destination, (byte)0xA5);

            RtcpCodecResult result = codec.TryEncodeRtcpPacket(packet, destination);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.WrittenCount, Is.Zero);
                Assert.That(destination, Has.All.EqualTo(0xA5));
            });
            itemIndex = itemIndex + 1;
        }

        RtcpPacket nullChunk = CreatePacket(new RtcpSourceDescriptionChunk[] { null! });
        RtcpSourceDescriptionItem nullValue = CreateItem(RtcpSdesItemType.Name, []);
        nullValue.Value = null!;
        RtcpPacket nullItemValue = CreatePacket(
            new RtcpSourceDescriptionChunk[] { CreateChunk(1, nullValue) });

        Assert.Multiple(() =>
        {
            Assert.That(codec.TryEncodeRtcpPacket(nullChunk, new byte[16]).Success,
                Is.False);
            Assert.That(codec.TryEncodeRtcpPacket(nullItemValue, new byte[16]).Success,
                Is.False);
        });
    }

    static RtcpSdesItemType[] GetStandardTypes()
    {
        return new RtcpSdesItemType[]
        {
            RtcpSdesItemType.CanonicalName,
            RtcpSdesItemType.Name,
            RtcpSdesItemType.Email,
            RtcpSdesItemType.Phone,
            RtcpSdesItemType.Location,
            RtcpSdesItemType.Tool,
            RtcpSdesItemType.Note
        };
    }

    static RtcpSourceDescriptionItem CreateItem(
        RtcpSdesItemType type,
        byte[] value)
    {
        RtcpSourceDescriptionItem item = new RtcpSourceDescriptionItem();
        item.Type = type;
        item.Value = value;
        return item;
    }

    static RtcpSourceDescriptionChunk CreateChunk(
        uint sourceIdentifier,
        params RtcpSourceDescriptionItem[] items)
    {
        RtcpSourceDescriptionChunk chunk = new RtcpSourceDescriptionChunk();
        chunk.SourceIdentifier = sourceIdentifier;
        chunk.Items = items;
        return chunk;
    }

    static RtcpPacket CreatePacket(RtcpSourceDescriptionChunk[] chunks)
    {
        RtcpSourceDescription description = new RtcpSourceDescription();
        description.Chunks = chunks;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.SourceDescription;
        packet.SourceDescription = description;
        return packet;
    }

    static byte[] CreateSingleItemWirePacket(byte wireType, byte[] value)
    {
        int unalignedBodyLength = 4 + 2 + value.Length + 1;
        int bodyLength = (unalignedBodyLength + 3) & ~3;
        byte[] body = new byte[bodyLength];
        body[3] = 1;
        body[4] = wireType;
        body[5] = (byte)value.Length;
        value.CopyTo(body, 6);
        return CreateWirePacket(1, body);
    }

    static byte[] CreatePrivateWirePacket(byte[] prefix, byte[] value)
    {
        byte[] payload = new byte[1 + prefix.Length + value.Length];
        payload[0] = (byte)prefix.Length;
        prefix.CopyTo(payload, 1);
        value.CopyTo(payload, 1 + prefix.Length);
        return CreateSingleItemWirePacket((byte)RtcpSdesItemType.Private, payload);
    }

    static byte[] CreateWirePacket(int sourceCount, byte[] body)
    {
        byte[] packet = new byte[4 + body.Length];
        packet[0] = (byte)(0x80 | sourceCount);
        packet[1] = 202;
        int lengthInWordsMinusOne = packet.Length / 4 - 1;
        packet[2] = (byte)(lengthInWordsMinusOne >> 8);
        packet[3] = (byte)lengthInWordsMinusOne;
        body.CopyTo(packet, 4);
        return packet;
    }

    static byte[] CreateAsciiBytes(int length, byte value)
    {
        byte[] bytes = new byte[length];
        Array.Fill(bytes, value);
        return bytes;
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
