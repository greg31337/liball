using rtp;

namespace rtp.Tests.Wire;

public class RtpPacketCodecTests
{
    [Test]
    public void EncodeWritesMinimalPacketAndReportsExactSize()
    {
        IRtpPacketCodec codec = CreateDefaultCodec();
        RtpPacket packet = new RtpPacket();
        packet.Marker = true;
        packet.PayloadType = 96;
        packet.SequenceNumber = 0xFEDC;
        packet.Timestamp = 0x01234567U;
        packet.SynchronizationSource = 0x89ABCDEFU;
        byte[] expected = new byte[]
        {
            0x80, 0xE0, 0xFE, 0xDC,
            0x01, 0x23, 0x45, 0x67,
            0x89, 0xAB, 0xCD, 0xEF
        };
        byte[] destination = new byte[expected.Length];

        int size = codec.GetRtpPacketEncodedSize(packet);
        int written = codec.EncodeRtpPacket(packet, destination);

        Assert.Multiple(() =>
        {
            Assert.That(size, Is.EqualTo(expected.Length));
            Assert.That(written, Is.EqualTo(expected.Length));
            Assert.That(destination, Is.EqualTo(expected));
            Assert.That(codec.EncodeRtpPacketToArray(packet), Is.EqualTo(expected));
        });
    }

    [Test]
    public void EncodeWritesEveryPacketSectionInNetworkByteOrder()
    {
        IRtpPacketCodec codec = CreateDefaultCodec();
        RtpPacket packet = CreateCompletePacket();
        byte[] expected = CreateCompletePacketBytes();
        byte[] destination = new byte[expected.Length + 3];
        destination[expected.Length] = 0x41;
        destination[expected.Length + 1] = 0x42;
        destination[expected.Length + 2] = 0x43;

        int size = codec.GetRtpPacketEncodedSize(packet);
        int written = codec.EncodeRtpPacket(packet, destination);

        Assert.Multiple(() =>
        {
            Assert.That(size, Is.EqualTo(expected.Length));
            Assert.That(written, Is.EqualTo(expected.Length));
            Assert.That(destination[..expected.Length], Is.EqualTo(expected));
            Assert.That(destination[expected.Length..], Is.EqualTo(new byte[] { 0x41, 0x42, 0x43 }));
        });
    }

    [Test]
    public void EncodeRoundTripsAllPacketSections()
    {
        IRtpPacketCodec codec = CreateDefaultCodec();
        RtpPacket source = CreateCompletePacket();

        RtpPacket decoded = codec.DecodeRtpPacket(codec.EncodeRtpPacketToArray(source));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Version, Is.EqualTo(source.Version));
            Assert.That(decoded.HasPadding, Is.EqualTo(source.HasPadding));
            Assert.That(decoded.HasExtension, Is.EqualTo(source.HasExtension));
            Assert.That(decoded.Marker, Is.EqualTo(source.Marker));
            Assert.That(decoded.PayloadType, Is.EqualTo(source.PayloadType));
            Assert.That(decoded.SequenceNumber, Is.EqualTo(source.SequenceNumber));
            Assert.That(decoded.Timestamp, Is.EqualTo(source.Timestamp));
            Assert.That(decoded.SynchronizationSource, Is.EqualTo(source.SynchronizationSource));
            Assert.That(decoded.ContributingSources, Is.EqualTo(source.ContributingSources));
            Assert.That(decoded.HeaderExtension?.Profile, Is.EqualTo(source.HeaderExtension?.Profile));
            Assert.That(decoded.HeaderExtension?.Data, Is.EqualTo(source.HeaderExtension?.Data));
            Assert.That(decoded.Payload, Is.EqualTo(source.Payload));
            Assert.That(decoded.Padding, Is.EqualTo(source.Padding));
        });
    }

    [Test]
    public void EncodeCapacityFailureDoesNotModifyDestination()
    {
        IRtpPacketCodec codec = CreateDefaultCodec();
        RtpPacket packet = CreateCompletePacket();
        byte[] destination = new byte[codec.GetRtpPacketEncodedSize(packet) - 1];
        Array.Fill(destination, (byte)0xA5);
        byte[] original = (byte[])destination.Clone();

        RtpBufferCapacityException? exception = Assert.Throws<RtpBufferCapacityException>(() =>
            codec.EncodeRtpPacket(packet, destination));

        Assert.Multiple(() =>
        {
            Assert.That(exception?.Message, Does.Contain("too small"));
            Assert.That(destination, Is.EqualTo(original));
        });
    }

    [Test]
    public void EncodeValidationFailureDoesNotModifyDestination()
    {
        IRtpPacketCodec codec = CreateDefaultCodec();
        RtpPacket packet = CreateCompletePacket();
        packet.HeaderExtension!.Data = new byte[] { 1, 2, 3 };
        byte[] destination = new byte[64];
        Array.Fill(destination, (byte)0x5A);
        byte[] original = (byte[])destination.Clone();

        RtpPacketFormatException? exception = Assert.Throws<RtpPacketFormatException>(() =>
            codec.EncodeRtpPacket(packet, destination));

        Assert.Multiple(() =>
        {
            Assert.That(exception?.Message, Does.Contain("multiple of four"));
            Assert.That(destination, Is.EqualTo(original));
        });
    }

    [Test]
    public void EncodeToArrayReturnsStorageIndependentOfPacketArrays()
    {
        IRtpPacketCodec codec = CreateDefaultCodec();
        RtpPacket packet = CreateCompletePacket();

        byte[] encoded = codec.EncodeRtpPacketToArray(packet);
        packet.ContributingSources[0] = 0;
        packet.HeaderExtension!.Data[0] = 0;
        packet.Payload[0] = 0;
        packet.Padding[1] = 0;

        Assert.That(encoded, Is.EqualTo(CreateCompletePacketBytes()));
    }

    [Test]
    public void EncodeRejectsInvalidHeaderFieldModels()
    {
        IRtpPacketCodec codec = CreateDefaultCodec();
        RtpPacket wrongVersion = new RtpPacket();
        wrongVersion.Version = 1;
        RtpPacket oversizedPayloadType = new RtpPacket();
        oversizedPayloadType.PayloadType = 128;
        RtpPacket tooManySources = new RtpPacket();
        tooManySources.ContributingSources = new uint[16];

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.GetRtpPacketEncodedSize(wrongVersion));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.GetRtpPacketEncodedSize(oversizedPayloadType));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.GetRtpPacketEncodedSize(tooManySources));
        });
    }

    [Test]
    public void EncodeRejectsInconsistentExtensionAndPaddingModels()
    {
        IRtpPacketCodec codec = CreateDefaultCodec();
        RtpPacket extensionMissing = new RtpPacket();
        extensionMissing.HasExtension = true;
        RtpPacket extensionFlagMissing = new RtpPacket();
        extensionFlagMissing.HeaderExtension = new RtpHeaderExtension();
        RtpPacket paddingMissing = new RtpPacket();
        paddingMissing.HasPadding = true;
        RtpPacket paddingFlagMissing = new RtpPacket();
        paddingFlagMissing.Padding = new byte[] { 1 };
        RtpPacket paddingCountMismatch = new RtpPacket();
        paddingCountMismatch.HasPadding = true;
        paddingCountMismatch.Padding = new byte[] { 0, 3 };

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.GetRtpPacketEncodedSize(extensionMissing));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.GetRtpPacketEncodedSize(extensionFlagMissing));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.GetRtpPacketEncodedSize(paddingMissing));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.GetRtpPacketEncodedSize(paddingFlagMissing));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.GetRtpPacketEncodedSize(paddingCountMismatch));
        });
    }

    [Test]
    public void TryEncodeReportsSuccessAndWritesTheExactPacket()
    {
        IRtpPacketCodec codec = CreateDefaultCodec();
        RtpPacket packet = CreateCompletePacket();
        byte[] expected = CreateCompletePacketBytes();
        byte[] destination = new byte[expected.Length + 2];
        destination[expected.Length] = 0x61;
        destination[expected.Length + 1] = 0x62;

        RtpCodecResult result = codec.TryEncodeRtpPacket(packet, destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ConsumedCount, Is.Zero);
            Assert.That(result.WrittenCount, Is.EqualTo(expected.Length));
            Assert.That(result.ErrorMessage, Is.Null);
            Assert.That(result.Packet, Is.Null);
            Assert.That(destination[..expected.Length], Is.EqualTo(expected));
            Assert.That(destination[expected.Length..], Is.EqualTo(new byte[] { 0x61, 0x62 }));
        });
    }

    [Test]
    public void TryEncodeReportsValidationFailureWithoutWriting()
    {
        IRtpPacketCodec codec = CreateDefaultCodec();
        RtpPacket packet = CreateCompletePacket();
        packet.Version = 1;
        byte[] destination = new byte[64];
        Array.Fill(destination, (byte)0xA7);
        byte[] original = (byte[])destination.Clone();

        RtpCodecResult result = codec.TryEncodeRtpPacket(packet, destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ConsumedCount, Is.Zero);
            Assert.That(result.WrittenCount, Is.Zero);
            Assert.That(result.ErrorMessage, Does.Contain("version"));
            Assert.That(destination, Is.EqualTo(original));
        });
    }

    [Test]
    public void TryEncodeReportsCapacityFailureWithoutWriting()
    {
        IRtpPacketCodec codec = CreateDefaultCodec();
        RtpPacket packet = CreateCompletePacket();
        byte[] destination = new byte[CreateCompletePacketBytes().Length - 1];
        Array.Fill(destination, (byte)0xB8);
        byte[] original = (byte[])destination.Clone();

        RtpCodecResult result = codec.TryEncodeRtpPacket(packet, destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ConsumedCount, Is.Zero);
            Assert.That(result.WrittenCount, Is.Zero);
            Assert.That(result.ErrorMessage, Does.Contain("too small"));
            Assert.That(destination, Is.EqualTo(original));
        });
    }

    [Test]
    public void TryDecodeReportsSuccessAndReturnsOwnedPacketData()
    {
        IRtpPacketCodec codec = CreateDefaultCodec();
        byte[] source = CreateCompletePacketBytes();

        RtpCodecResult result = codec.TryDecodeRtpPacket(source);
        source[12] = 0;
        source[28] = 0;
        source[32] = 0;
        source[35] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ConsumedCount, Is.EqualTo(source.Length));
            Assert.That(result.WrittenCount, Is.Zero);
            Assert.That(result.ErrorMessage, Is.Null);
            Assert.That(result.Packet?.ContributingSources[0], Is.EqualTo(0x01020304U));
            Assert.That(result.Packet?.HeaderExtension?.Data[0], Is.EqualTo(0xAA));
            Assert.That(result.Packet?.Payload[0], Is.EqualTo(0x09));
            Assert.That(result.Packet?.Padding[1], Is.EqualTo(2));
        });
    }

    [Test]
    public void TryDecodeReportsMalformedInputWithoutConsumingBytes()
    {
        IRtpPacketCodec codec = CreateDefaultCodec();
        byte[] truncatedHeader = new byte[11];
        byte[] truncatedExtension = new byte[]
        {
            0x90, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0x12, 0x34, 0, 1,
            1, 2, 3
        };
        byte[] invalidPadding = new byte[RtpProtocolConstants.FixedHeaderSize + 1];
        invalidPadding[0] = 0xA0;

        RtpCodecResult headerResult = codec.TryDecodeRtpPacket(truncatedHeader);
        RtpCodecResult extensionResult = codec.TryDecodeRtpPacket(truncatedExtension);
        RtpCodecResult paddingResult = codec.TryDecodeRtpPacket(invalidPadding);

        Assert.Multiple(() =>
        {
            Assert.That(headerResult.Success, Is.False);
            Assert.That(headerResult.ConsumedCount, Is.Zero);
            Assert.That(headerResult.Packet, Is.Null);
            Assert.That(headerResult.ErrorMessage, Does.Contain("fixed header"));
            Assert.That(extensionResult.Success, Is.False);
            Assert.That(extensionResult.ConsumedCount, Is.Zero);
            Assert.That(extensionResult.Packet, Is.Null);
            Assert.That(extensionResult.ErrorMessage, Does.Contain("extension data"));
            Assert.That(paddingResult.Success, Is.False);
            Assert.That(paddingResult.ConsumedCount, Is.Zero);
            Assert.That(paddingResult.Packet, Is.Null);
            Assert.That(paddingResult.ErrorMessage, Does.Contain("positive"));
        });
    }

    [Test]
    public void CodecConstructorRejectsMissingAndInvalidPacketConfiguration()
    {
        RtpSessionConfiguration tooSmall = new RtpSessionConfiguration();
        tooSmall.MaximumRtpPacketSize = RtpProtocolConstants.FixedHeaderSize - 1;
        RtpSessionConfiguration tooLarge = new RtpSessionConfiguration();
        tooLarge.MaximumRtpPacketSize = 65536;

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() => new RtpPacketCodec(null!));
            Assert.Throws<RtpConfigurationException>(() => new RtpPacketCodec(tooSmall));
            Assert.Throws<RtpConfigurationException>(() => new RtpPacketCodec(tooLarge));
        });
    }

    [Test]
    public void ConfiguredMaximumPacketSizeAppliesToEncodeAndDecodeFromASnapshot()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.MaximumRtpPacketSize = RtpProtocolConstants.FixedHeaderSize + 1;
        IRtpPacketCodec codec = new RtpPacketCodec(configuration);
        configuration.MaximumRtpPacketSize = 65535;
        RtpPacket packet = new RtpPacket();
        packet.Payload = new byte[] { 1, 2 };
        byte[] source = new byte[RtpProtocolConstants.FixedHeaderSize + 2];
        source[0] = 0x80;
        byte[] destination = new byte[source.Length];
        Array.Fill(destination, (byte)0x4C);
        byte[] original = (byte[])destination.Clone();

        RtpCodecResult encodeResult = codec.TryEncodeRtpPacket(packet, destination);
        RtpCodecResult decodeResult = codec.TryDecodeRtpPacket(source);

        Assert.Multiple(() =>
        {
            Assert.That(encodeResult.Success, Is.False);
            Assert.That(encodeResult.ErrorMessage, Does.Contain("maximum packet size"));
            Assert.That(encodeResult.WrittenCount, Is.Zero);
            Assert.That(destination, Is.EqualTo(original));
            Assert.That(decodeResult.Success, Is.False);
            Assert.That(decodeResult.ErrorMessage, Does.Contain("maximum packet size"));
            Assert.That(decodeResult.ConsumedCount, Is.Zero);
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.GetRtpPacketEncodedSize(packet));
            Assert.Throws<RtpPacketFormatException>(() => codec.DecodeRtpPacket(source));
        });
    }

    [Test]
    public void StrictModeRejectsRtcpReportCollisionThatTolerantModePreserves()
    {
        IRtpPacketCodec strictCodec = CreateConfiguredCodec(true, true, 65535);
        IRtpPacketCodec tolerantCodec = CreateConfiguredCodec(false, true, 65535);
        byte[] source = new byte[RtpProtocolConstants.FixedHeaderSize];
        source[0] = 0x80;
        source[1] = RtcpProtocolConstants.SenderReportPacketType;

        RtpCodecResult strictDecode = strictCodec.TryDecodeRtpPacket(source);
        RtpCodecResult tolerantDecode = tolerantCodec.TryDecodeRtpPacket(source);
        RtpPacket packet = new RtpPacket();
        packet.Marker = true;
        packet.PayloadType = 72;
        byte[] destination = new byte[source.Length];
        RtpCodecResult strictEncode = strictCodec.TryEncodeRtpPacket(packet, destination);
        RtpCodecResult tolerantEncode = tolerantCodec.TryEncodeRtpPacket(packet, destination);

        Assert.Multiple(() =>
        {
            Assert.That(strictDecode.Success, Is.False);
            Assert.That(strictDecode.ErrorMessage, Does.Contain("RTCP report"));
            Assert.That(strictEncode.Success, Is.False);
            Assert.That(strictEncode.WrittenCount, Is.Zero);
            Assert.That(tolerantDecode.Success, Is.True);
            Assert.That(tolerantDecode.Packet?.Marker, Is.True);
            Assert.That(tolerantDecode.Packet?.PayloadType, Is.EqualTo(72));
            Assert.That(tolerantEncode.Success, Is.True);
            Assert.That(destination, Is.EqualTo(source));
        });
    }

    [Test]
    public void PaddingCanBePreservedOrNormalizedWithoutMutatingThePacket()
    {
        IRtpPacketCodec preservingCodec = CreateConfiguredCodec(true, true, 65535);
        IRtpPacketCodec normalizingCodec = CreateConfiguredCodec(true, false, 65535);
        byte[] source = new byte[]
        {
            0xA0, 0x60, 0, 1,
            0, 0, 0, 2,
            0, 0, 0, 3,
            0x11, 0xAA, 0xBB, 3
        };
        RtpPacket preservedPacket = preservingCodec.DecodeRtpPacket(source);
        RtpPacket normalizedPacket = normalizingCodec.DecodeRtpPacket(source);

        byte[] preservedEncoding = preservingCodec.EncodeRtpPacketToArray(preservedPacket);
        byte[] normalizedEncoding = normalizingCodec.EncodeRtpPacketToArray(preservedPacket);

        Assert.Multiple(() =>
        {
            Assert.That(preservedPacket.Payload, Is.EqualTo(new byte[] { 0x11 }));
            Assert.That(preservedPacket.Padding, Is.EqualTo(new byte[] { 0xAA, 0xBB, 3 }));
            Assert.That(normalizedPacket.Padding, Is.EqualTo(new byte[] { 0, 0, 3 }));
            Assert.That(preservedEncoding, Is.EqualTo(source));
            Assert.That(normalizedEncoding[..13], Is.EqualTo(source[..13]));
            Assert.That(normalizedEncoding[13..], Is.EqualTo(new byte[] { 0, 0, 3 }));
            Assert.That(preservedPacket.Padding, Is.EqualTo(new byte[] { 0xAA, 0xBB, 3 }));
        });
    }

    [Test]
    public void TolerantModeStillRejectsUnsafePacketLengths()
    {
        IRtpPacketCodec codec = CreateConfiguredCodec(false, false, 65535);
        byte[] truncatedExtension = new byte[]
        {
            0x90, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0x12, 0x34, 0, 2,
            1, 2, 3, 4
        };

        RtpCodecResult result = codec.TryDecodeRtpPacket(truncatedExtension);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ConsumedCount, Is.Zero);
            Assert.That(result.ErrorMessage, Does.Contain("extension data"));
        });
    }

    [Test]
    public void DecodeReadsStrictFixedHeaderFieldsInNetworkByteOrder()
    {
        IRtpPacketCodec codec = CreateDefaultCodec();
        byte[] source = new byte[]
        {
            0x80, 0xE0, 0xFE, 0xDC,
            0x01, 0x23, 0x45, 0x67,
            0x89, 0xAB, 0xCD, 0xEF
        };

        RtpPacket packet = codec.DecodeRtpPacket(source);

        Assert.Multiple(() =>
        {
            Assert.That(packet.Version, Is.EqualTo(2));
            Assert.That(packet.HasPadding, Is.False);
            Assert.That(packet.HasExtension, Is.False);
            Assert.That(packet.Marker, Is.True);
            Assert.That(packet.PayloadType, Is.EqualTo(96));
            Assert.That(packet.SequenceNumber, Is.EqualTo(0xFEDC));
            Assert.That(packet.Timestamp, Is.EqualTo(0x01234567U));
            Assert.That(packet.SynchronizationSource, Is.EqualTo(0x89ABCDEFU));
            Assert.That(packet.ContributingSources, Is.Empty);
        });
    }

    [Test]
    public void DecodeReadsFlagsAndContributingSourcesWithoutRetainingInput()
    {
        IRtpPacketCodec codec = CreateDefaultCodec();
        byte[] source = new byte[]
        {
            0xB3, 0x7F, 0x00, 0x01,
            0x11, 0x22, 0x33, 0x44,
            0x55, 0x66, 0x77, 0x88,
            0x01, 0x02, 0x03, 0x04,
            0x10, 0x20, 0x30, 0x40,
            0xFE, 0xDC, 0xBA, 0x98,
            0x12, 0x34, 0x00, 0x01,
            0xAA, 0xBB, 0xCC, 0xDD,
            0x09, 0x08, 0x00, 0x02
        };

        RtpPacket packet = codec.DecodeRtpPacket(source);
        source[12] = 0;
        source[16] = 0;
        source[20] = 0;
        source[28] = 0;
        source[32] = 0;
        source[34] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(packet.HasPadding, Is.True);
            Assert.That(packet.HasExtension, Is.True);
            Assert.That(packet.Marker, Is.False);
            Assert.That(packet.PayloadType, Is.EqualTo(127));
            Assert.That(packet.ContributingSources,
                Is.EqualTo(new uint[] { 0x01020304U, 0x10203040U, 0xFEDCBA98U }));
            Assert.That(packet.HeaderExtension?.Profile, Is.EqualTo(0x1234));
            Assert.That(packet.HeaderExtension?.Format, Is.EqualTo(RtpHeaderExtensionFormat.Raw));
            Assert.That(packet.HeaderExtension?.Data,
                Is.EqualTo(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }));
            Assert.That(packet.Payload, Is.EqualTo(new byte[] { 0x09, 0x08 }));
            Assert.That(packet.Padding, Is.EqualTo(new byte[] { 0x00, 0x02 }));
        });
    }

    [Test]
    public void DecodeClassifiesRfc8285ProfilesAndPreservesExtensionData()
    {
        IRtpPacketCodec codec = CreateDefaultCodec();
        byte[] oneByteSource = new byte[]
        {
            0x90, 0x60, 0, 1,
            0, 0, 0, 2,
            0, 0, 0, 3,
            0xBE, 0xDE, 0, 1,
            0x10, 0xAA, 0, 0,
            0x55
        };
        byte[] twoByteSource = new byte[]
        {
            0x90, 0x61, 0, 2,
            0, 0, 0, 3,
            0, 0, 0, 4,
            0x10, 0x0A, 0, 1,
            0x01, 0x01, 0xBB, 0,
            0x66, 0x67
        };

        RtpPacket oneBytePacket = codec.DecodeRtpPacket(oneByteSource);
        RtpPacket twoBytePacket = codec.DecodeRtpPacket(twoByteSource);
        oneByteSource[16] = 0;
        twoByteSource[18] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(oneBytePacket.HeaderExtension?.Profile,
                Is.EqualTo(RtpHeaderExtensionProfiles.OneByte));
            Assert.That(oneBytePacket.HeaderExtension?.Format,
                Is.EqualTo(RtpHeaderExtensionFormat.OneByte));
            Assert.That(oneBytePacket.HeaderExtension?.ApplicationBits, Is.Zero);
            Assert.That(oneBytePacket.HeaderExtension?.Data,
                Is.EqualTo(new byte[] { 0x10, 0xAA, 0, 0 }));
            Assert.That(oneBytePacket.Payload, Is.EqualTo(new byte[] { 0x55 }));
            Assert.That(twoBytePacket.HeaderExtension?.Profile, Is.EqualTo(0x100A));
            Assert.That(twoBytePacket.HeaderExtension?.Format,
                Is.EqualTo(RtpHeaderExtensionFormat.TwoByte));
            Assert.That(twoBytePacket.HeaderExtension?.ApplicationBits, Is.EqualTo(10));
            Assert.That(twoBytePacket.HeaderExtension?.Data,
                Is.EqualTo(new byte[] { 0x01, 0x01, 0xBB, 0 }));
            Assert.That(twoBytePacket.Payload, Is.EqualTo(new byte[] { 0x66, 0x67 }));
        });
    }

    [Test]
    public void DecodeAcceptsEmptyExtensionDataAndPaddingOnlyBodies()
    {
        IRtpPacketCodec codec = CreateDefaultCodec();
        byte[] source = new byte[]
        {
            0xB0, 0x60, 0, 1,
            0, 0, 0, 2,
            0, 0, 0, 3,
            0x44, 0x55, 0, 0,
            0, 0, 0, 4
        };

        RtpPacket packet = codec.DecodeRtpPacket(source);

        Assert.Multiple(() =>
        {
            Assert.That(packet.HeaderExtension?.Profile, Is.EqualTo(0x4455));
            Assert.That(packet.HeaderExtension?.Data, Is.Empty);
            Assert.That(packet.Payload, Is.Empty);
            Assert.That(packet.Padding, Is.EqualTo(new byte[] { 0, 0, 0, 4 }));
        });
    }

    [Test]
    public void DecodeAcceptsTheMaximumContributingSourceCount()
    {
        IRtpPacketCodec codec = CreateDefaultCodec();
        byte[] source = new byte[
            RtpProtocolConstants.FixedHeaderSize +
            RtpProtocolConstants.MaximumCsrcCount *
            RtpProtocolConstants.SourceIdentifierSize];
        source[0] = 0x8F;
        int sourceIndex = 0;
        int offset = RtpProtocolConstants.FixedHeaderSize;
        while (sourceIndex < RtpProtocolConstants.MaximumCsrcCount)
        {
            uint value = (uint)(sourceIndex + 1);
            source[offset] = (byte)(value >> 24);
            source[offset + 1] = (byte)(value >> 16);
            source[offset + 2] = (byte)(value >> 8);
            source[offset + 3] = (byte)value;
            sourceIndex = sourceIndex + 1;
            offset = offset + RtpProtocolConstants.SourceIdentifierSize;
        }

        RtpPacket packet = codec.DecodeRtpPacket(source);

        Assert.Multiple(() =>
        {
            Assert.That(packet.ContributingSources.Length,
                Is.EqualTo(RtpProtocolConstants.MaximumCsrcCount));
            Assert.That(packet.ContributingSources[0], Is.EqualTo(1U));
            Assert.That(packet.ContributingSources[14], Is.EqualTo(15U));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(3)]
    public void DecodeRejectsEveryUnsupportedRtpVersion(int version)
    {
        IRtpPacketCodec codec = CreateDefaultCodec();
        byte[] source = new byte[RtpProtocolConstants.FixedHeaderSize];
        source[0] = (byte)(version << RtpProtocolConstants.VersionShift);

        RtpPacketFormatException? exception = Assert.Throws<RtpPacketFormatException>(() =>
            codec.DecodeRtpPacket(source));

        Assert.That(exception?.Message, Does.Contain("version"));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(11)]
    public void DecodeRejectsPacketsShorterThanTheFixedHeader(int length)
    {
        IRtpPacketCodec codec = CreateDefaultCodec();
        byte[] source = new byte[length];

        RtpPacketFormatException? exception = Assert.Throws<RtpPacketFormatException>(() =>
            codec.DecodeRtpPacket(source));

        Assert.That(exception?.Message, Does.Contain("fixed header"));
    }

    [Test]
    public void DecodeRejectsTruncatedContributingSourceLists()
    {
        IRtpPacketCodec codec = CreateDefaultCodec();
        byte[] oneSourceTruncated = new byte[15];
        oneSourceTruncated[0] = 0x81;
        byte[] fifteenSourcesTruncated = new byte[71];
        fifteenSourcesTruncated[0] = 0x8F;

        Assert.Multiple(() =>
        {
            RtpPacketFormatException? oneException =
                Assert.Throws<RtpPacketFormatException>(() =>
                    codec.DecodeRtpPacket(oneSourceTruncated));
            RtpPacketFormatException? fifteenException =
                Assert.Throws<RtpPacketFormatException>(() =>
                    codec.DecodeRtpPacket(fifteenSourcesTruncated));
            Assert.That(oneException?.Message, Does.Contain("contributing-source"));
            Assert.That(fifteenException?.Message, Does.Contain("contributing-source"));
        });
    }

    [Test]
    public void DecodeRejectsTruncatedExtensionEnvelopeAndData()
    {
        IRtpPacketCodec codec = CreateDefaultCodec();
        byte[] missingEnvelope = new byte[RtpProtocolConstants.FixedHeaderSize];
        missingEnvelope[0] = 0x90;
        byte[] partialEnvelope = new byte[15];
        partialEnvelope[0] = 0x90;
        byte[] truncatedData = new byte[]
        {
            0x90, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0x12, 0x34, 0, 1,
            1, 2, 3
        };

        Assert.Multiple(() =>
        {
            RtpPacketFormatException? missingException =
                Assert.Throws<RtpPacketFormatException>(() =>
                    codec.DecodeRtpPacket(missingEnvelope));
            RtpPacketFormatException? partialException =
                Assert.Throws<RtpPacketFormatException>(() =>
                    codec.DecodeRtpPacket(partialEnvelope));
            RtpPacketFormatException? dataException =
                Assert.Throws<RtpPacketFormatException>(() =>
                    codec.DecodeRtpPacket(truncatedData));
            Assert.That(missingException?.Message, Does.Contain("extension envelope"));
            Assert.That(partialException?.Message, Does.Contain("extension envelope"));
            Assert.That(dataException?.Message, Does.Contain("extension data"));
        });
    }

    [Test]
    public void DecodeRejectsMissingZeroAndOversizedPaddingCounts()
    {
        IRtpPacketCodec codec = CreateDefaultCodec();
        byte[] missingPadding = new byte[RtpProtocolConstants.FixedHeaderSize];
        missingPadding[0] = 0xA0;
        byte[] zeroPadding = new byte[RtpProtocolConstants.FixedHeaderSize + 1];
        zeroPadding[0] = 0xA0;
        byte[] oversizedPadding = new byte[RtpProtocolConstants.FixedHeaderSize + 2];
        oversizedPadding[0] = 0xA0;
        oversizedPadding[oversizedPadding.Length - 1] = 3;

        Assert.Multiple(() =>
        {
            RtpPacketFormatException? missingException =
                Assert.Throws<RtpPacketFormatException>(() =>
                    codec.DecodeRtpPacket(missingPadding));
            RtpPacketFormatException? zeroException =
                Assert.Throws<RtpPacketFormatException>(() =>
                    codec.DecodeRtpPacket(zeroPadding));
            RtpPacketFormatException? oversizedException =
                Assert.Throws<RtpPacketFormatException>(() =>
                    codec.DecodeRtpPacket(oversizedPadding));
            Assert.That(missingException?.Message, Does.Contain("padding-count"));
            Assert.That(zeroException?.Message, Does.Contain("positive"));
            Assert.That(oversizedException?.Message, Does.Contain("exceeds"));
        });
    }

    static RtpPacket CreateCompletePacket()
    {
        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.Profile = 0x1234;
        extension.Format = RtpHeaderExtensionFormat.Raw;
        extension.Data = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };

        RtpPacket packet = new RtpPacket();
        packet.HasPadding = true;
        packet.HasExtension = true;
        packet.PayloadType = 127;
        packet.SequenceNumber = 1;
        packet.Timestamp = 0x11223344U;
        packet.SynchronizationSource = 0x55667788U;
        packet.ContributingSources = new uint[]
        {
            0x01020304U,
            0x10203040U,
            0xFEDCBA98U
        };
        packet.HeaderExtension = extension;
        packet.Payload = new byte[] { 0x09, 0x08 };
        packet.Padding = new byte[] { 0, 2 };
        return packet;
    }

    static IRtpPacketCodec CreateDefaultCodec()
    {
        return CreateConfiguredCodec(true, true, 65535);
    }

    static IRtpPacketCodec CreateConfiguredCodec(
        bool strictValidation,
        bool preservePadding,
        int maximumPacketSize)
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.StrictValidation = strictValidation;
        configuration.PreservePadding = preservePadding;
        configuration.MaximumRtpPacketSize = maximumPacketSize;
        return new RtpPacketCodec(configuration);
    }

    static byte[] CreateCompletePacketBytes()
    {
        return new byte[]
        {
            0xB3, 0x7F, 0x00, 0x01,
            0x11, 0x22, 0x33, 0x44,
            0x55, 0x66, 0x77, 0x88,
            0x01, 0x02, 0x03, 0x04,
            0x10, 0x20, 0x30, 0x40,
            0xFE, 0xDC, 0xBA, 0x98,
            0x12, 0x34, 0x00, 0x01,
            0xAA, 0xBB, 0xCC, 0xDD,
            0x09, 0x08, 0x00, 0x02
        };
    }
}
