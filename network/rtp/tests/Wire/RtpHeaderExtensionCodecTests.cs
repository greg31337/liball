using rtp;

namespace rtp.Tests.Wire;

public class RtpHeaderExtensionCodecTests
{
    [Test]
    public void OneByteEncodeWritesBoundaryElementsAndExactAlignment()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        RtpHeaderExtension extension = CreateOneByteExtension();
        extension.Elements = new RtpHeaderExtensionElement[]
        {
            CreateElement(1, new byte[] { 0xA1 }),
            CreateElement(14, new byte[]
            {
                0, 1, 2, 3, 4, 5, 6, 7,
                8, 9, 10, 11, 12, 13, 14, 15
            })
        };
        byte[] expected = new byte[]
        {
            0xBE, 0xDE, 0, 5,
            0x10, 0xA1,
            0xEF, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
            0
        };
        byte[] destination = new byte[expected.Length + 1];
        destination[expected.Length] = 0x77;

        int size = codec.GetRtpHeaderExtensionEncodedSize(extension);
        int written = codec.EncodeRtpHeaderExtension(extension, destination);

        Assert.Multiple(() =>
        {
            Assert.That(size, Is.EqualTo(expected.Length));
            Assert.That(written, Is.EqualTo(expected.Length));
            Assert.That(destination[..expected.Length], Is.EqualTo(expected));
            Assert.That(destination[expected.Length], Is.EqualTo(0x77));
        });
    }

    [Test]
    public void OneByteDecodeSkipsPaddingAndOwnsElementAndRawData()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        byte[] source = new byte[]
        {
            0xBE, 0xDE, 0, 2,
            0, 0x21, 0xAA, 0xBB,
            0, 0x30, 0xCC, 0,
            0x99
        };

        RtpCodecResult result = codec.TryDecodeRtpHeaderExtension(source);
        source[5] = 0;
        source[6] = 0;
        source[10] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ConsumedCount, Is.EqualTo(12));
            Assert.That(result.HeaderExtension?.Profile,
                Is.EqualTo(RtpHeaderExtensionProfiles.OneByte));
            Assert.That(result.HeaderExtension?.Format,
                Is.EqualTo(RtpHeaderExtensionFormat.OneByte));
            Assert.That(result.HeaderExtension?.Elements.Length, Is.EqualTo(2));
            Assert.That(result.HeaderExtension?.Elements[0].Identifier, Is.EqualTo(2));
            Assert.That(result.HeaderExtension?.Elements[0].Data,
                Is.EqualTo(new byte[] { 0xAA, 0xBB }));
            Assert.That(result.HeaderExtension?.Elements[1].Identifier, Is.EqualTo(3));
            Assert.That(result.HeaderExtension?.Elements[1].Data,
                Is.EqualTo(new byte[] { 0xCC }));
            Assert.That(result.HeaderExtension?.Data,
                Is.EqualTo(new byte[] { 0, 0x21, 0xAA, 0xBB, 0, 0x30, 0xCC, 0 }));
        });
    }

    [Test]
    public void OneByteDecodeStopsAtReservedIdentifierAndPreservesTheTail()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        byte[] source = new byte[]
        {
            0xBE, 0xDE, 0, 2,
            0x10, 0xAA, 0xF0, 0x20,
            0xBB, 0xCC, 0, 0
        };

        RtpHeaderExtension extension = codec.DecodeRtpHeaderExtension(source);

        Assert.Multiple(() =>
        {
            Assert.That(extension.Elements.Length, Is.EqualTo(1));
            Assert.That(extension.Elements[0].Identifier, Is.EqualTo(1));
            Assert.That(extension.Elements[0].Data, Is.EqualTo(new byte[] { 0xAA }));
            Assert.That(extension.Data, Is.EqualTo(source[4..]));
        });
    }

    [Test]
    public void OneByteDecodeRejectsMalformedEnvelopePaddingAndElementTails()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        byte[] wrongProfile = new byte[] { 0x12, 0x34, 0, 0 };
        byte[] truncatedEnvelope = new byte[] { 0xBE, 0xDE, 0 };
        byte[] truncatedData = new byte[] { 0xBE, 0xDE, 0, 1, 0, 0, 0 };
        byte[] invalidPadding = new byte[] { 0xBE, 0xDE, 0, 1, 1, 0, 0, 0 };
        byte[] truncatedElement = new byte[] { 0xBE, 0xDE, 0, 1, 0x13, 1, 2, 3 };

        RtpCodecResult profileResult = codec.TryDecodeRtpHeaderExtension(wrongProfile);
        RtpCodecResult envelopeResult = codec.TryDecodeRtpHeaderExtension(truncatedEnvelope);
        RtpCodecResult dataResult = codec.TryDecodeRtpHeaderExtension(truncatedData);
        RtpCodecResult paddingResult = codec.TryDecodeRtpHeaderExtension(invalidPadding);
        RtpCodecResult elementResult = codec.TryDecodeRtpHeaderExtension(truncatedElement);

        Assert.Multiple(() =>
        {
            Assert.That(profileResult.Success, Is.False);
            Assert.That(profileResult.ConsumedCount, Is.Zero);
            Assert.That(profileResult.ErrorMessage, Does.Contain("profile"));
            Assert.That(envelopeResult.ErrorMessage, Does.Contain("envelope"));
            Assert.That(dataResult.ErrorMessage, Does.Contain("data is truncated"));
            Assert.That(paddingResult.ErrorMessage, Does.Contain("padding"));
            Assert.That(elementResult.ErrorMessage, Does.Contain("truncated data"));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.DecodeRtpHeaderExtension(truncatedElement));
        });
    }

    [Test]
    public void OneByteEncodeRejectsInvalidElementLimits()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        RtpHeaderExtension zeroIdentifier = CreateOneByteExtension();
        zeroIdentifier.Elements = new RtpHeaderExtensionElement[]
        {
            CreateElement(0, new byte[] { 1 })
        };
        RtpHeaderExtension reservedIdentifier = CreateOneByteExtension();
        reservedIdentifier.Elements = new RtpHeaderExtensionElement[]
        {
            CreateElement(15, new byte[] { 1 })
        };
        RtpHeaderExtension emptyData = CreateOneByteExtension();
        emptyData.Elements = new RtpHeaderExtensionElement[]
        {
            CreateElement(1, [])
        };
        RtpHeaderExtension oversizedData = CreateOneByteExtension();
        oversizedData.Elements = new RtpHeaderExtensionElement[]
        {
            CreateElement(1, new byte[17])
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.GetRtpHeaderExtensionEncodedSize(zeroIdentifier));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.GetRtpHeaderExtensionEncodedSize(reservedIdentifier));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.GetRtpHeaderExtensionEncodedSize(emptyData));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.GetRtpHeaderExtensionEncodedSize(oversizedData));
        });
    }

    [Test]
    public void OneByteTryEncodeCapacityFailureLeavesDestinationUnchanged()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        RtpHeaderExtension extension = CreateOneByteExtension();
        extension.Elements = new RtpHeaderExtensionElement[]
        {
            CreateElement(4, new byte[] { 1, 2, 3 })
        };
        byte[] destination = new byte[7];
        Array.Fill(destination, (byte)0x5C);
        byte[] original = (byte[])destination.Clone();

        RtpCodecResult result = codec.TryEncodeRtpHeaderExtension(extension, destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.WrittenCount, Is.Zero);
            Assert.That(result.ErrorMessage, Does.Contain("too small"));
            Assert.That(destination, Is.EqualTo(original));
            Assert.Throws<RtpBufferCapacityException>(() =>
                codec.EncodeRtpHeaderExtension(extension, destination));
            Assert.That(destination, Is.EqualTo(original));
        });
    }

    [Test]
    public void RtpPacketCodecEncodesAndDecodesOneByteElements()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        IRtpPacketCodec packetCodec = new RtpPacketCodec(configuration);
        RtpHeaderExtension extension = CreateOneByteExtension();
        extension.Data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        extension.Elements = new RtpHeaderExtensionElement[]
        {
            CreateElement(1, new byte[] { 0xAA }),
            CreateElement(2, new byte[] { 0xBB, 0xCC })
        };
        RtpPacket packet = new RtpPacket();
        packet.HasExtension = true;
        packet.HeaderExtension = extension;
        packet.Payload = new byte[] { 0x55 };
        byte[] expected = new byte[]
        {
            0x90, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0xBE, 0xDE, 0, 2,
            0x10, 0xAA, 0x21, 0xBB,
            0xCC, 0, 0, 0,
            0x55
        };

        byte[] encoded = packetCodec.EncodeRtpPacketToArray(packet);
        RtpPacket decoded = packetCodec.DecodeRtpPacket(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(packetCodec.GetRtpPacketEncodedSize(packet), Is.EqualTo(expected.Length));
            Assert.That(encoded, Is.EqualTo(expected));
            Assert.That(extension.Data, Is.EqualTo(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }));
            Assert.That(decoded.HeaderExtension?.Elements.Length, Is.EqualTo(2));
            Assert.That(decoded.HeaderExtension?.Elements[0].Data,
                Is.EqualTo(new byte[] { 0xAA }));
            Assert.That(decoded.HeaderExtension?.Elements[1].Data,
                Is.EqualTo(new byte[] { 0xBB, 0xCC }));
            Assert.That(decoded.HeaderExtension?.Data,
                Is.EqualTo(new byte[] { 0x10, 0xAA, 0x21, 0xBB, 0xCC, 0, 0, 0 }));
            Assert.That(decoded.Payload, Is.EqualTo(new byte[] { 0x55 }));
        });
    }

    [Test]
    public void TwoByteEncodeSupportsBoundaryIdentifiersLengthsAndApplicationBits()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        byte[] maximumData = new byte[255];
        maximumData[0] = 0xA1;
        maximumData[254] = 0xFE;
        RtpHeaderExtension extension = CreateTwoByteExtension(15);
        extension.Elements = new RtpHeaderExtensionElement[]
        {
            CreateElement(1, []),
            CreateElement(255, maximumData)
        };
        byte[] destination = new byte[264];

        int size = codec.GetRtpHeaderExtensionEncodedSize(extension);
        RtpCodecResult encodeResult = codec.TryEncodeRtpHeaderExtension(
            extension,
            destination);
        RtpHeaderExtension decoded = codec.DecodeRtpHeaderExtension(destination);

        Assert.Multiple(() =>
        {
            Assert.That(size, Is.EqualTo(264));
            Assert.That(encodeResult.Success, Is.True);
            Assert.That(encodeResult.WrittenCount, Is.EqualTo(264));
            Assert.That(destination[..8],
                Is.EqualTo(new byte[] { 0x10, 0x0F, 0, 65, 1, 0, 255, 255 }));
            Assert.That(destination[8], Is.EqualTo(0xA1));
            Assert.That(destination[262], Is.EqualTo(0xFE));
            Assert.That(destination[263], Is.Zero);
            Assert.That(decoded.ApplicationBits, Is.EqualTo(15));
            Assert.That(decoded.Elements.Length, Is.EqualTo(2));
            Assert.That(decoded.Elements[0].Identifier, Is.EqualTo(1));
            Assert.That(decoded.Elements[0].Data, Is.Empty);
            Assert.That(decoded.Elements[1].Identifier, Is.EqualTo(255));
            Assert.That(decoded.Elements[1].Data, Is.EqualTo(maximumData));
        });
    }

    [Test]
    public void TwoByteDecodeSkipsPaddingAndOwnsZeroLengthElements()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        byte[] source = new byte[]
        {
            0x10, 0x0D, 0, 2,
            0, 1, 0, 0,
            255, 1, 0xAA, 0
        };

        RtpCodecResult result = codec.TryDecodeRtpHeaderExtension(source);
        source[4] = 99;
        source[10] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ConsumedCount, Is.EqualTo(12));
            Assert.That(result.HeaderExtension?.Profile, Is.EqualTo(0x100D));
            Assert.That(result.HeaderExtension?.Format,
                Is.EqualTo(RtpHeaderExtensionFormat.TwoByte));
            Assert.That(result.HeaderExtension?.ApplicationBits, Is.EqualTo(13));
            Assert.That(result.HeaderExtension?.Elements.Length, Is.EqualTo(2));
            Assert.That(result.HeaderExtension?.Elements[0].Identifier, Is.EqualTo(1));
            Assert.That(result.HeaderExtension?.Elements[0].Data, Is.Empty);
            Assert.That(result.HeaderExtension?.Elements[1].Identifier, Is.EqualTo(255));
            Assert.That(result.HeaderExtension?.Elements[1].Data,
                Is.EqualTo(new byte[] { 0xAA }));
            Assert.That(result.HeaderExtension?.Data,
                Is.EqualTo(new byte[] { 0, 1, 0, 0, 255, 1, 0xAA, 0 }));
        });
    }

    [Test]
    public void TwoByteDecodeRejectsMissingLengthAndTruncatedElementData()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        byte[] missingLength = new byte[]
        {
            0x10, 0, 0, 1,
            0, 0, 0, 1
        };
        byte[] truncatedData = new byte[]
        {
            0x10, 0, 0, 1,
            1, 3, 0xAA, 0xBB
        };

        RtpCodecResult lengthResult = codec.TryDecodeRtpHeaderExtension(missingLength);
        RtpCodecResult dataResult = codec.TryDecodeRtpHeaderExtension(truncatedData);

        Assert.Multiple(() =>
        {
            Assert.That(lengthResult.Success, Is.False);
            Assert.That(lengthResult.ConsumedCount, Is.Zero);
            Assert.That(lengthResult.ErrorMessage, Does.Contain("length byte"));
            Assert.That(dataResult.Success, Is.False);
            Assert.That(dataResult.ConsumedCount, Is.Zero);
            Assert.That(dataResult.ErrorMessage, Does.Contain("truncated data"));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.DecodeRtpHeaderExtension(truncatedData));
        });
    }

    [Test]
    public void TwoByteEncodeRejectsIdentifiersLengthsAndInconsistentAppbits()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        RtpHeaderExtension zeroIdentifier = CreateTwoByteExtension(0);
        zeroIdentifier.Elements = new RtpHeaderExtensionElement[]
        {
            CreateElement(0, [])
        };
        RtpHeaderExtension oversizedIdentifier = CreateTwoByteExtension(0);
        oversizedIdentifier.Elements = new RtpHeaderExtensionElement[]
        {
            CreateElement(256, [])
        };
        RtpHeaderExtension oversizedData = CreateTwoByteExtension(0);
        oversizedData.Elements = new RtpHeaderExtensionElement[]
        {
            CreateElement(1, new byte[256])
        };
        RtpHeaderExtension oversizedAppbits = CreateTwoByteExtension(0);
        oversizedAppbits.ApplicationBits = 16;
        RtpHeaderExtension mismatchedProfile = CreateTwoByteExtension(3);
        mismatchedProfile.Profile = 0x1004;

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.GetRtpHeaderExtensionEncodedSize(zeroIdentifier));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.GetRtpHeaderExtensionEncodedSize(oversizedIdentifier));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.GetRtpHeaderExtensionEncodedSize(oversizedData));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.GetRtpHeaderExtensionEncodedSize(oversizedAppbits));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.GetRtpHeaderExtensionEncodedSize(mismatchedProfile));
        });
    }

    [Test]
    public void RtpPacketCodecEncodesAndDecodesTwoByteElements()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        IRtpPacketCodec packetCodec = new RtpPacketCodec(configuration);
        RtpHeaderExtension extension = CreateTwoByteExtension(10);
        extension.Data = new byte[] { 0xEE, 0xEE, 0xEE, 0xEE };
        extension.Elements = new RtpHeaderExtensionElement[]
        {
            CreateElement(1, []),
            CreateElement(255, new byte[] { 0xAA, 0xBB })
        };
        RtpPacket packet = new RtpPacket();
        packet.HasExtension = true;
        packet.HeaderExtension = extension;
        packet.Payload = new byte[] { 0x55 };
        byte[] expected = new byte[]
        {
            0x90, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0x10, 0x0A, 0, 2,
            1, 0, 255, 2,
            0xAA, 0xBB, 0, 0,
            0x55
        };

        byte[] encoded = packetCodec.EncodeRtpPacketToArray(packet);
        RtpPacket decoded = packetCodec.DecodeRtpPacket(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(packetCodec.GetRtpPacketEncodedSize(packet), Is.EqualTo(expected.Length));
            Assert.That(encoded, Is.EqualTo(expected));
            Assert.That(extension.Data, Is.EqualTo(new byte[] { 0xEE, 0xEE, 0xEE, 0xEE }));
            Assert.That(decoded.HeaderExtension?.ApplicationBits, Is.EqualTo(10));
            Assert.That(decoded.HeaderExtension?.Elements.Length, Is.EqualTo(2));
            Assert.That(decoded.HeaderExtension?.Elements[0].Data, Is.Empty);
            Assert.That(decoded.HeaderExtension?.Elements[1].Identifier, Is.EqualTo(255));
            Assert.That(decoded.HeaderExtension?.Elements[1].Data,
                Is.EqualTo(new byte[] { 0xAA, 0xBB }));
            Assert.That(decoded.HeaderExtension?.Data,
                Is.EqualTo(new byte[] { 1, 0, 255, 2, 0xAA, 0xBB, 0, 0 }));
            Assert.That(decoded.Payload, Is.EqualTo(new byte[] { 0x55 }));
        });
    }

    [Test]
    public void AutomaticEncodingChoosesOneByteFormatAndOrdersIdentifiers()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.Elements = new RtpHeaderExtensionElement[]
        {
            CreateElement(3, new byte[] { 0xCC }),
            CreateElement(1, new byte[] { 0xAA, 0xBB })
        };
        byte[] expected = new byte[]
        {
            0xBE, 0xDE, 0, 2,
            0x11, 0xAA, 0xBB,
            0x30, 0xCC, 0, 0, 0
        };
        byte[] destination = new byte[expected.Length];

        int written = codec.EncodeRtpHeaderExtension(extension, destination);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(expected.Length));
            Assert.That(destination, Is.EqualTo(expected));
            Assert.That(extension.Format, Is.EqualTo(RtpHeaderExtensionFormat.Raw));
            Assert.That(extension.Profile, Is.Zero);
            Assert.That(extension.Elements[0].Identifier, Is.EqualTo(3));
            Assert.That(extension.Elements[1].Identifier, Is.EqualTo(1));
        });
    }

    [Test]
    public void AutomaticEncodingPromotesMixedRequirementsToOneTwoByteBlock()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.ApplicationBits = 5;
        extension.Elements = new RtpHeaderExtensionElement[]
        {
            CreateElement(200, []),
            CreateElement(2, new byte[] { 0xAA })
        };
        byte[] expected = new byte[]
        {
            0x10, 0x05, 0, 2,
            2, 1, 0xAA,
            200, 0,
            0, 0, 0
        };

        byte[] encoded = new byte[codec.GetRtpHeaderExtensionEncodedSize(extension)];
        codec.EncodeRtpHeaderExtension(extension, encoded);

        Assert.That(encoded, Is.EqualTo(expected));
    }

    [Test]
    public void ExplicitTwoByteSelectionIsRetainedForOneByteCompatibleElements()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        RtpHeaderExtension extension = CreateTwoByteExtension(7);
        extension.Elements = new RtpHeaderExtensionElement[]
        {
            CreateElement(4, new byte[] { 0xBB }),
            CreateElement(1, new byte[] { 0xAA })
        };
        byte[] expected = new byte[]
        {
            0x10, 0x07, 0, 2,
            1, 1, 0xAA,
            4, 1, 0xBB,
            0, 0
        };

        byte[] encoded = new byte[codec.GetRtpHeaderExtensionEncodedSize(extension)];
        codec.EncodeRtpHeaderExtension(extension, encoded);

        Assert.That(encoded, Is.EqualTo(expected));
    }

    [Test]
    public void AutomaticSelectionRetainsApplicationBitsByChoosingTwoByteFormat()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.ApplicationBits = 6;
        extension.Elements = new RtpHeaderExtensionElement[]
        {
            CreateElement(1, new byte[] { 0xAA })
        };
        byte[] expected = new byte[]
        {
            0x10, 0x06, 0, 1,
            1, 1, 0xAA, 0
        };

        byte[] encoded = new byte[codec.GetRtpHeaderExtensionEncodedSize(extension)];
        codec.EncodeRtpHeaderExtension(extension, encoded);

        Assert.That(encoded, Is.EqualTo(expected));
    }

    [Test]
    public void DuplicateIdentifiersAreRejectedDuringEncodeAndDecode()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        RtpHeaderExtension duplicateModel = CreateOneByteExtension();
        duplicateModel.Elements = new RtpHeaderExtensionElement[]
        {
            CreateElement(2, new byte[] { 1 }),
            CreateElement(2, new byte[] { 2 })
        };
        byte[] duplicateOneByte = new byte[]
        {
            0xBE, 0xDE, 0, 1,
            0x20, 1, 0x20, 2
        };
        byte[] duplicateTwoByte = new byte[]
        {
            0x10, 0, 0, 2,
            2, 1, 1,
            2, 1, 2,
            0, 0
        };

        RtpCodecResult oneByteResult =
            codec.TryDecodeRtpHeaderExtension(duplicateOneByte);
        RtpCodecResult twoByteResult =
            codec.TryDecodeRtpHeaderExtension(duplicateTwoByte);

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.GetRtpHeaderExtensionEncodedSize(duplicateModel));
            Assert.That(oneByteResult.Success, Is.False);
            Assert.That(oneByteResult.ErrorMessage, Does.Contain("duplicate"));
            Assert.That(twoByteResult.Success, Is.False);
            Assert.That(twoByteResult.ErrorMessage, Does.Contain("duplicate"));
        });
    }

    [Test]
    public void ElementOperationsSelectFormatOrderAndReturnIndependentCopies()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        RtpHeaderExtension original = new RtpHeaderExtension();
        RtpHeaderExtensionElement fourth = CreateElement(4, new byte[] { 4 });
        RtpHeaderExtensionElement second = CreateElement(2, new byte[] { 2 });

        RtpHeaderExtension withFourth =
            codec.AddRtpHeaderExtensionElement(original, fourth);
        RtpHeaderExtension withBoth =
            codec.AddRtpHeaderExtensionElement(withFourth, second);
        RtpHeaderExtension promoted = codec.ReplaceRtpHeaderExtensionElement(
            withBoth,
            CreateElement(4, []));
        RtpHeaderExtension removed =
            codec.RemoveRtpHeaderExtensionElement(promoted, 4);
        RtpHeaderExtensionElement? found =
            codec.FindRtpHeaderExtensionElement(withBoth, 2);
        fourth.Data[0] = 99;
        second.Data[0] = 98;
        if (found != null)
        {
            found.Data[0] = 97;
        }

        Assert.Multiple(() =>
        {
            Assert.That(original.Elements, Is.Empty);
            Assert.That(withFourth.Format, Is.EqualTo(RtpHeaderExtensionFormat.OneByte));
            Assert.That(withFourth.Elements[0].Data, Is.EqualTo(new byte[] { 4 }));
            Assert.That(withBoth.Elements[0].Identifier, Is.EqualTo(2));
            Assert.That(withBoth.Elements[1].Identifier, Is.EqualTo(4));
            Assert.That(withBoth.Elements[0].Data, Is.EqualTo(new byte[] { 2 }));
            Assert.That(promoted.Format, Is.EqualTo(RtpHeaderExtensionFormat.TwoByte));
            Assert.That(promoted.Elements[1].Data, Is.Empty);
            Assert.That(removed.Format, Is.EqualTo(RtpHeaderExtensionFormat.OneByte));
            Assert.That(removed.Elements.Length, Is.EqualTo(1));
            Assert.That(removed.Elements[0].Identifier, Is.EqualTo(2));
            Assert.That(withBoth.Elements[0].Data, Is.EqualTo(new byte[] { 2 }));
            Assert.That(codec.FindRtpHeaderExtensionElement(withBoth, 99), Is.Null);
        });
    }

    [Test]
    public void ElementOperationsRejectDuplicatesMissingIdentifiersAndOpaqueProfiles()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension = codec.AddRtpHeaderExtensionElement(
            extension,
            CreateElement(1, new byte[] { 1 }));
        RtpHeaderExtension opaque = new RtpHeaderExtension();
        opaque.Profile = 0x1234;
        opaque.Data = new byte[] { 0, 0, 0, 0 };

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.AddRtpHeaderExtensionElement(
                    extension,
                    CreateElement(1, new byte[] { 2 })));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.ReplaceRtpHeaderExtensionElement(
                    extension,
                    CreateElement(2, new byte[] { 2 })));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.RemoveRtpHeaderExtensionElement(extension, 2));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.FindRtpHeaderExtensionElement(opaque, 1));
        });
    }

    [Test]
    public void RtpPacketCodecUsesAutomaticExtensionSelection()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        IRtpPacketCodec packetCodec = new RtpPacketCodec(configuration);
        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.ApplicationBits = 9;
        extension.Elements = new RtpHeaderExtensionElement[]
        {
            CreateElement(200, new byte[] { 0xAA })
        };
        RtpPacket packet = new RtpPacket();
        packet.HasExtension = true;
        packet.HeaderExtension = extension;
        byte[] expected = new byte[]
        {
            0x90, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0x10, 0x09, 0, 1,
            200, 1, 0xAA, 0
        };

        byte[] encoded = packetCodec.EncodeRtpPacketToArray(packet);
        RtpPacket decoded = packetCodec.DecodeRtpPacket(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(expected));
            Assert.That(decoded.HeaderExtension?.Format,
                Is.EqualTo(RtpHeaderExtensionFormat.TwoByte));
            Assert.That(decoded.HeaderExtension?.ApplicationBits, Is.EqualTo(9));
            Assert.That(decoded.HeaderExtension?.Elements[0].Identifier, Is.EqualTo(200));
        });
    }

    static RtpHeaderExtension CreateOneByteExtension()
    {
        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.Profile = RtpHeaderExtensionProfiles.OneByte;
        extension.Format = RtpHeaderExtensionFormat.OneByte;
        return extension;
    }

    static RtpHeaderExtension CreateTwoByteExtension(byte applicationBits)
    {
        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.Profile = (ushort)(RtpHeaderExtensionProfiles.TwoByte | applicationBits);
        extension.Format = RtpHeaderExtensionFormat.TwoByte;
        extension.ApplicationBits = applicationBits;
        return extension;
    }

    static RtpHeaderExtensionElement CreateElement(int identifier, byte[] data)
    {
        RtpHeaderExtensionElement element = new RtpHeaderExtensionElement();
        element.Identifier = identifier;
        element.Data = data;
        return element;
    }
}
