using rtp;

namespace rtp.Tests.Wire;

public class RtpTwoByteHeaderExtensionBoundaryTests
{
    [Test]
    public void EveryIdentifierRoundTripsAtBothLengthBoundaries()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        int identifier = RtpHeaderExtensionProfiles.TwoByteIdentifierMinimum;
        while (identifier <= RtpHeaderExtensionProfiles.TwoByteIdentifierMaximum)
        {
            AssertRoundTrip(codec, identifier, 0, 15);
            AssertRoundTrip(codec, identifier, 255, 15);
            identifier = identifier + 1;
        }
    }

    [Test]
    public void EveryLengthRoundTripsAtBothIdentifierBoundaries()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        int dataLength = RtpHeaderExtensionProfiles.TwoByteDataLengthMinimum;
        while (dataLength <= RtpHeaderExtensionProfiles.TwoByteDataLengthMaximum)
        {
            AssertRoundTrip(codec, 1, dataLength, 0);
            AssertRoundTrip(codec, 255, dataLength, 0);
            dataLength = dataLength + 1;
        }
    }

    [Test]
    public void EveryApplicationBitsValueRoundTripsInTheProfile()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        int applicationBits = 0;
        while (applicationBits <= 15)
        {
            RtpHeaderExtension extension = CreateExtension(
                1,
                new byte[] { 0xA5 },
                (byte)applicationBits);
            byte[] encoded = EncodeToArray(codec, extension);
            RtpHeaderExtension decoded = codec.DecodeRtpHeaderExtension(encoded);

            Assert.Multiple(() =>
            {
                Assert.That(encoded[0], Is.EqualTo(0x10));
                Assert.That(encoded[1], Is.EqualTo(applicationBits));
                Assert.That(decoded.Profile,
                    Is.EqualTo(RtpHeaderExtensionProfiles.TwoByte | applicationBits));
                Assert.That(decoded.ApplicationBits, Is.EqualTo(applicationBits));
                Assert.That(decoded.Format,
                    Is.EqualTo(RtpHeaderExtensionFormat.TwoByte));
            });
            applicationBits = applicationBits + 1;
        }
    }

    [Test]
    public void EncoderAlignmentProducesZeroToThreeTrailingPaddingBytes()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        int[] dataLengths = new int[] { 2, 1, 0, 3 };
        int[] expectedPaddingLengths = new int[] { 0, 1, 2, 3 };
        int caseIndex = 0;
        while (caseIndex < dataLengths.Length)
        {
            int dataLength = dataLengths[caseIndex];
            int paddingLength = expectedPaddingLengths[caseIndex];
            RtpHeaderExtension extension = CreateExtension(
                1,
                CreateData(dataLength),
                0);
            int expectedSize = 4 + 2 + dataLength + paddingLength;
            byte[] encoded = EncodeToArray(codec, extension);

            Assert.Multiple(() =>
            {
                Assert.That(encoded, Has.Length.EqualTo(expectedSize));
                Assert.That(encoded[2], Is.Zero);
                Assert.That(encoded[3],
                    Is.EqualTo((byte)((expectedSize - 4) / 4)));
                Assert.That(encoded[(6 + dataLength)..], Is.All.Zero);
            });
            caseIndex = caseIndex + 1;
        }
    }

    [Test]
    public void DecoderAcceptsPaddingAroundZeroAndNonzeroLengthElements()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        byte[] source = new byte[]
        {
            0x10, 0x0C, 0, 3,
            0, 0,
            1, 0,
            0,
            2, 1, 0xAA,
            0,
            255, 0,
            0
        };

        RtpCodecResult result = codec.TryDecodeRtpHeaderExtension(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ConsumedCount, Is.EqualTo(source.Length));
            Assert.That(result.HeaderExtension?.ApplicationBits, Is.EqualTo(12));
            Assert.That(result.HeaderExtension?.Elements, Has.Length.EqualTo(3));
            Assert.That(result.HeaderExtension?.Elements[0].Identifier, Is.EqualTo(1));
            Assert.That(result.HeaderExtension?.Elements[0].Data, Is.Empty);
            Assert.That(result.HeaderExtension?.Elements[1].Identifier, Is.EqualTo(2));
            Assert.That(result.HeaderExtension?.Elements[1].Data,
                Is.EqualTo(new byte[] { 0xAA }));
            Assert.That(result.HeaderExtension?.Elements[2].Identifier, Is.EqualTo(255));
            Assert.That(result.HeaderExtension?.Elements[2].Data, Is.Empty);
            Assert.That(result.HeaderExtension?.Data, Is.EqualTo(source[4..]));
        });
    }

    [Test]
    public void AutomaticSelectionUsesOneByteOnlyAtAllOneByteBoundaries()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        RtpHeaderExtension compatible = CreateAutomaticExtension(
            new RtpHeaderExtensionElement[]
            {
                CreateElement(14, CreateData(16)),
                CreateElement(1, new byte[] { 0xA1 })
            },
            0);
        RtpHeaderExtension identifierPromotion = CreateAutomaticExtension(
            new RtpHeaderExtensionElement[]
            {
                CreateElement(15, new byte[] { 1 }),
                CreateElement(1, new byte[] { 2 })
            },
            0);
        RtpHeaderExtension emptyPromotion = CreateAutomaticExtension(
            new RtpHeaderExtensionElement[] { CreateElement(1, []) },
            0);
        RtpHeaderExtension lengthPromotion = CreateAutomaticExtension(
            new RtpHeaderExtensionElement[] { CreateElement(1, new byte[17]) },
            0);
        RtpHeaderExtension appbitsPromotion = CreateAutomaticExtension(
            new RtpHeaderExtensionElement[] { CreateElement(1, new byte[] { 1 }) },
            1);

        Assert.Multiple(() =>
        {
            Assert.That(ReadProfile(codec, compatible),
                Is.EqualTo(RtpHeaderExtensionProfiles.OneByte));
            Assert.That(ReadProfile(codec, identifierPromotion),
                Is.EqualTo(RtpHeaderExtensionProfiles.TwoByte));
            Assert.That(ReadProfile(codec, emptyPromotion),
                Is.EqualTo(RtpHeaderExtensionProfiles.TwoByte));
            Assert.That(ReadProfile(codec, lengthPromotion),
                Is.EqualTo(RtpHeaderExtensionProfiles.TwoByte));
            Assert.That(ReadProfile(codec, appbitsPromotion),
                Is.EqualTo(RtpHeaderExtensionProfiles.TwoByte | 1));
        });
    }

    [Test]
    public void AutomaticSelectionPromotesAWholeMixedElementSetAndOrdersIt()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        RtpHeaderExtension extension = CreateAutomaticExtension(
            new RtpHeaderExtensionElement[]
            {
                CreateElement(200, []),
                CreateElement(2, new byte[] { 0xA2 })
            },
            7);

        byte[] encoded = EncodeToArray(codec, extension);
        RtpHeaderExtension decoded = codec.DecodeRtpHeaderExtension(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded[0..2], Is.EqualTo(new byte[] { 0x10, 0x07 }));
            Assert.That(decoded.Elements, Has.Length.EqualTo(2));
            Assert.That(decoded.Elements[0].Identifier, Is.EqualTo(2));
            Assert.That(decoded.Elements[1].Identifier, Is.EqualTo(200));
            Assert.That(decoded.Elements[1].Data, Is.Empty);
        });
    }

    [Test]
    public void EncoderRejectsIdentifiersLengthsAndAppbitsJustOutsideTheirRanges()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        int[] invalidIdentifiers = new int[] { -1, 0, 256, int.MaxValue };
        int identifierIndex = 0;
        while (identifierIndex < invalidIdentifiers.Length)
        {
            RtpHeaderExtension invalid = CreateExtension(
                invalidIdentifiers[identifierIndex],
                [],
                0);
            RtpCodecResult result = codec.TryEncodeRtpHeaderExtension(
                invalid,
                new byte[8]);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.WrittenCount, Is.Zero);
                Assert.That(result.ErrorMessage, Does.Contain("between 1 and 255"));
            });
            identifierIndex = identifierIndex + 1;
        }

        RtpHeaderExtension oversizedData = CreateExtension(1, new byte[256], 0);
        RtpHeaderExtension oversizedAppbits = CreateExtension(1, [], 0);
        oversizedAppbits.ApplicationBits = 16;

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.GetRtpHeaderExtensionEncodedSize(oversizedData));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.GetRtpHeaderExtensionEncodedSize(oversizedAppbits));
        });
    }

    [Test]
    public void EveryDuplicateIdentifierIsRejectedEvenWithPaddingAndEmptyData()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        int identifier = RtpHeaderExtensionProfiles.TwoByteIdentifierMinimum;
        while (identifier <= RtpHeaderExtensionProfiles.TwoByteIdentifierMaximum)
        {
            RtpHeaderExtension duplicateModel = CreateExtension(
                identifier,
                [],
                0);
            duplicateModel.Elements = new RtpHeaderExtensionElement[]
            {
                CreateElement(identifier, []),
                CreateElement(identifier, new byte[] { 1 })
            };
            byte[] duplicateWire = new byte[]
            {
                0x10, 0, 0, 2,
                (byte)identifier, 0,
                0, 0,
                (byte)identifier, 0,
                0, 0
            };

            RtpCodecResult encodeResult = codec.TryEncodeRtpHeaderExtension(
                duplicateModel,
                new byte[12]);
            RtpCodecResult decodeResult = codec.TryDecodeRtpHeaderExtension(
                duplicateWire);

            Assert.Multiple(() =>
            {
                Assert.That(encodeResult.Success, Is.False);
                Assert.That(encodeResult.ErrorMessage, Does.Contain("duplicate"));
                Assert.That(decodeResult.Success, Is.False);
                Assert.That(decodeResult.ConsumedCount, Is.Zero);
                Assert.That(decodeResult.ErrorMessage, Does.Contain("duplicate"));
            });
            identifier = identifier + 1;
        }
    }

    [Test]
    public void EveryIdentifierRejectsAMissingLengthAtTheDataTail()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        int identifier = RtpHeaderExtensionProfiles.TwoByteIdentifierMinimum;
        while (identifier <= RtpHeaderExtensionProfiles.TwoByteIdentifierMaximum)
        {
            byte[] source = new byte[]
            {
                0x10, 0, 0, 1,
                0, 0, 0, (byte)identifier
            };

            RtpCodecResult result = codec.TryDecodeRtpHeaderExtension(source);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.ConsumedCount, Is.Zero);
                Assert.That(result.HeaderExtension, Is.Null);
                Assert.That(result.ErrorMessage, Does.Contain("length byte"));
            });
            identifier = identifier + 1;
        }
    }

    [Test]
    public void EveryPositiveLengthRejectsDataMissingAtTheTail()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        int dataLength = 1;
        while (dataLength <= RtpHeaderExtensionProfiles.TwoByteDataLengthMaximum)
        {
            byte[] source = new byte[]
            {
                0x10, 0, 0, 1,
                0, 0, 1, (byte)dataLength
            };

            RtpCodecResult result = codec.TryDecodeRtpHeaderExtension(source);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.ConsumedCount, Is.Zero);
                Assert.That(result.HeaderExtension, Is.Null);
                Assert.That(result.ErrorMessage, Does.Contain("truncated data"));
                Assert.Throws<RtpPacketFormatException>(() =>
                    codec.DecodeRtpHeaderExtension(source));
            });
            dataLength = dataLength + 1;
        }
    }

    [Test]
    public void ZeroLengthAtTheDataTailIsAValidElement()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        byte[] source = new byte[]
        {
            0x10, 0x0F, 0, 1,
            0, 0, 255, 0
        };

        RtpHeaderExtension decoded = codec.DecodeRtpHeaderExtension(source);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.ApplicationBits, Is.EqualTo(15));
            Assert.That(decoded.Elements, Has.Length.EqualTo(1));
            Assert.That(decoded.Elements[0].Identifier, Is.EqualTo(255));
            Assert.That(decoded.Elements[0].Data, Is.Empty);
        });
    }

    static void AssertRoundTrip(
        IRtpHeaderExtensionCodec codec,
        int identifier,
        int dataLength,
        byte applicationBits)
    {
        byte[] data = CreateData(dataLength);
        RtpHeaderExtension extension = CreateExtension(
            identifier,
            data,
            applicationBits);
        int unalignedDataLength = 2 + dataLength;
        int paddingLength = (4 - (unalignedDataLength & 3)) & 3;
        int expectedDataLength = unalignedDataLength + paddingLength;
        int expectedSize = 4 + expectedDataLength;

        int size = codec.GetRtpHeaderExtensionEncodedSize(extension);
        byte[] encoded = EncodeToArray(codec, extension);
        RtpHeaderExtension decoded = codec.DecodeRtpHeaderExtension(encoded);
        byte[] reencoded = EncodeToArray(codec, decoded);
        string context = $"identifier {identifier}, length {dataLength}";

        Assert.That(size, Is.EqualTo(expectedSize), context);
        Assert.That(encoded, Has.Length.EqualTo(expectedSize), context);
        Assert.That(encoded[0], Is.EqualTo(0x10), context);
        Assert.That(encoded[1], Is.EqualTo(applicationBits), context);
        Assert.That(encoded[2],
            Is.EqualTo((byte)(expectedDataLength / 4 >> 8)),
            context);
        Assert.That(encoded[3],
            Is.EqualTo((byte)(expectedDataLength / 4)),
            context);
        Assert.That(encoded[4], Is.EqualTo(identifier), context);
        Assert.That(encoded[5], Is.EqualTo(dataLength), context);
        Assert.That(encoded[6..(6 + dataLength)], Is.EqualTo(data), context);
        Assert.That(encoded[(6 + dataLength)..], Is.All.Zero, context);
        Assert.That(decoded.Format,
            Is.EqualTo(RtpHeaderExtensionFormat.TwoByte),
            context);
        Assert.That(decoded.ApplicationBits, Is.EqualTo(applicationBits), context);
        Assert.That(decoded.Elements, Has.Length.EqualTo(1), context);
        Assert.That(decoded.Elements[0].Identifier, Is.EqualTo(identifier), context);
        Assert.That(decoded.Elements[0].Data, Is.EqualTo(data), context);
        Assert.That(reencoded, Is.EqualTo(encoded), context);
    }

    static byte[] CreateData(int length)
    {
        byte[] data = new byte[length];
        int byteIndex = 0;
        while (byteIndex < data.Length)
        {
            data[byteIndex] = (byte)(byteIndex + 1);
            byteIndex = byteIndex + 1;
        }

        return data;
    }

    static RtpHeaderExtension CreateExtension(
        int identifier,
        byte[] data,
        byte applicationBits)
    {
        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.Profile = (ushort)(RtpHeaderExtensionProfiles.TwoByte |
            applicationBits);
        extension.Format = RtpHeaderExtensionFormat.TwoByte;
        extension.ApplicationBits = applicationBits;
        extension.Elements = new RtpHeaderExtensionElement[]
        {
            CreateElement(identifier, data)
        };
        return extension;
    }

    static RtpHeaderExtension CreateAutomaticExtension(
        RtpHeaderExtensionElement[] elements,
        byte applicationBits)
    {
        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.ApplicationBits = applicationBits;
        extension.Elements = elements;
        return extension;
    }

    static RtpHeaderExtensionElement CreateElement(int identifier, byte[] data)
    {
        RtpHeaderExtensionElement element = new RtpHeaderExtensionElement();
        element.Identifier = identifier;
        element.Data = data;
        return element;
    }

    static ushort ReadProfile(
        IRtpHeaderExtensionCodec codec,
        RtpHeaderExtension extension)
    {
        byte[] encoded = EncodeToArray(codec, extension);
        return (ushort)(encoded[0] << 8 | encoded[1]);
    }

    static byte[] EncodeToArray(
        IRtpHeaderExtensionCodec codec,
        RtpHeaderExtension extension)
    {
        byte[] destination = new byte[
            codec.GetRtpHeaderExtensionEncodedSize(extension)];
        codec.EncodeRtpHeaderExtension(extension, destination);
        return destination;
    }
}
