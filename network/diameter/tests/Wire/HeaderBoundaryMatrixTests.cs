using System;

namespace diameter.Tests;

public class HeaderBoundaryMatrixTests
{
    [Test]
    public void StrictMessageHeaderCodecsClassifyEveryFlagByte()
    {
        int flagValue = 0;
        while (flagValue <= byte.MaxValue)
        {
            bool valid = IsValidMessageFlags(flagValue);
            DiameterMessage message = CreateMessage();
            message.Flags = (DiameterMessageFlags)flagValue;
            DiameterEncodeResult measurement = MessageEncoder.MeasureHeader(message, 20);
            byte[] header = CreateRawMessageHeader(20, flagValue);
            DiameterDecodeResult decoding = MessageDecoder.DecodeHeader(header);

            if (valid)
            {
                Assert.That(
                    measurement.Status,
                    Is.EqualTo(DiameterCodecStatus.Success),
                    $"encoder flag byte {flagValue}");
                Assert.That(
                    decoding.Status,
                    Is.EqualTo(DiameterCodecStatus.Success),
                    $"decoder flag byte {flagValue}");
                Assert.That(
                    decoding.Message!.Flags,
                    Is.EqualTo((DiameterMessageFlags)flagValue),
                    $"decoded flag byte {flagValue}");
                Assert.That(decoding.Issues, Is.Empty, $"issues for flag byte {flagValue}");
            }
            else
            {
                AssertEncodeFailure(
                    measurement,
                    DiameterCodecStatus.Invalid,
                    4,
                    "invalid-message-flags",
                    "encoder flag byte " + flagValue);
                AssertDecodeFailure(
                    decoding,
                    DiameterCodecStatus.Invalid,
                    4,
                    "invalid-message-flags",
                    "decoder flag byte " + flagValue);
            }

            flagValue = flagValue + 1;
        }
    }

    [Test]
    public void PermissiveMessageHeaderDecoderPreservesEveryFlagByte()
    {
        int flagValue = 0;
        while (flagValue <= byte.MaxValue)
        {
            bool valid = IsValidMessageFlags(flagValue);
            DiameterDecodeResult result = MessageDecoder.DecodeHeaderWithProfile(
                CreateRawMessageHeader(20, flagValue),
                DiameterDecodeProfile.Permissive);

            Assert.That(
                result.Status,
                Is.EqualTo(DiameterCodecStatus.Success),
                $"flag byte {flagValue}");
            Assert.That(
                result.Message!.Flags,
                Is.EqualTo((DiameterMessageFlags)flagValue),
                $"flag byte {flagValue}");
            if (valid)
            {
                Assert.That(result.Issues, Is.Empty, $"flag byte {flagValue}");
            }
            else
            {
                Assert.That(result.Issues, Has.Count.EqualTo(1), $"flag byte {flagValue}");
                Assert.That(result.Issues[0].Severity, Is.EqualTo(DiameterIssueSeverity.Warning));
                Assert.That(result.Issues[0].Status, Is.EqualTo(DiameterCodecStatus.Success));
                Assert.That(result.Issues[0].Offset, Is.EqualTo(4));
                Assert.That(result.Issues[0].Code, Is.EqualTo("invalid-message-flags"));
            }

            flagValue = flagValue + 1;
        }
    }

    [Test]
    public void MessageHeaderDecoderClassifiesEveryVersionByte()
    {
        int version = 0;
        while (version <= byte.MaxValue)
        {
            byte[] header = CreateRawMessageHeader(20, 0);
            header[0] = (byte)version;

            DiameterDecodeResult result = MessageDecoder.DecodeHeader(header);

            if (version == DiameterProtocolConstants.Version)
            {
                Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
            }
            else
            {
                AssertDecodeFailure(
                    result,
                    DiameterCodecStatus.Invalid,
                    0,
                    "unsupported-version",
                    "version " + version);
            }

            version = version + 1;
        }
    }

    [Test]
    public void MessageHeaderCodecsClassifyLengthAndAlignmentBoundaries()
    {
        int[] lengths = new int[]
        {
            0, 1, 2, 3, 4, 5, 6, 7,
            8, 9, 10, 11, 12, 13, 14, 15,
            16, 17, 18, 19, 20, 21, 22, 23,
            24, 25, 26, 27, 28,
            DiameterProtocolConstants.MaximumMessageLength - 4,
            DiameterProtocolConstants.MaximumMessageLength - 3,
            DiameterProtocolConstants.MaximumMessageLength - 2,
            DiameterProtocolConstants.MaximumMessageLength - 1,
            DiameterProtocolConstants.MaximumMessageLength,
            DiameterProtocolConstants.MaximumMessageLength + 1,
            DiameterProtocolConstants.MaximumMessageLength + 2,
            DiameterProtocolConstants.MaximumMessageLength + 3
        };

        int index = 0;
        while (index < lengths.Length)
        {
            int length = lengths[index];
            DiameterEncodeResult measurement = MessageEncoder.MeasureHeader(
                CreateMessage(),
                length);
            DiameterDecodeResult decoding = MessageDecoder.DecodeHeader(
                CreateRawMessageHeader(length, 0));

            if (length < DiameterProtocolConstants.MinimumMessageLength)
            {
                AssertEncodeFailure(
                    measurement,
                    DiameterCodecStatus.Invalid,
                    1,
                    "invalid-message-length",
                    "encoded length " + length);
                AssertDecodeFailure(
                    decoding,
                    DiameterCodecStatus.Invalid,
                    1,
                    "invalid-message-length",
                    "decoded length " + length);
            }
            else if (length > DiameterProtocolConstants.MaximumMessageLength)
            {
                AssertEncodeFailure(
                    measurement,
                    DiameterCodecStatus.LimitExceeded,
                    1,
                    "message-length-limit",
                    "encoded length " + length);
                AssertDecodeFailure(
                    decoding,
                    DiameterCodecStatus.LimitExceeded,
                    1,
                    "message-length-limit",
                    "decoded length " + length);
            }
            else if (length % DiameterProtocolConstants.Alignment != 0)
            {
                AssertEncodeFailure(
                    measurement,
                    DiameterCodecStatus.Invalid,
                    1,
                    "invalid-message-length",
                    "encoded length " + length);
                AssertDecodeFailure(
                    decoding,
                    DiameterCodecStatus.Invalid,
                    1,
                    "invalid-message-length",
                    "decoded length " + length);
            }
            else
            {
                Assert.That(
                    measurement.Status,
                    Is.EqualTo(DiameterCodecStatus.Success),
                    $"encoded length {length}");
                Assert.That(
                    measurement.RequiredCount,
                    Is.EqualTo(DiameterProtocolConstants.MessageHeaderSize));
                Assert.That(
                    decoding.Status,
                    Is.EqualTo(DiameterCodecStatus.Success),
                    $"decoded length {length}");
                Assert.That(decoding.RequiredCount, Is.EqualTo(length));
                Assert.That(
                    decoding.ConsumedCount,
                    Is.EqualTo(DiameterProtocolConstants.MessageHeaderSize));
            }

            index = index + 1;
        }

        AssertEncodeFailure(
            MessageEncoder.MeasureHeader(CreateMessage(), -1),
            DiameterCodecStatus.Invalid,
            1,
            "invalid-message-length",
            "negative length");
        AssertEncodeFailure(
            MessageEncoder.MeasureHeader(
                CreateMessage(),
                DiameterProtocolConstants.MaximumMessageLength + 4),
            DiameterCodecStatus.LimitExceeded,
            1,
            "message-length-limit",
            "unrepresentable length");
    }

    [Test]
    public void MessageAndAvpHeaderDecodersRejectEveryShortHeaderLength()
    {
        int messageLength = 0;
        while (messageLength < DiameterProtocolConstants.MessageHeaderSize)
        {
            DiameterDecodeResult message = MessageDecoder.DecodeHeader(
                new byte[messageLength]);
            AssertDecodeFailure(
                message,
                DiameterCodecStatus.Incomplete,
                messageLength,
                "incomplete-header",
                "message source length " + messageLength);
            Assert.That(message.RequiredCount, Is.EqualTo(20));
            messageLength = messageLength + 1;
        }

        int avpLength = 0;
        while (avpLength < DiameterProtocolConstants.AvpHeaderSize)
        {
            DiameterAvpDecodeResult avp = AvpDecoder.DecodeAvp(new byte[avpLength]);
            AssertAvpDecodeFailure(
                avp,
                DiameterCodecStatus.Incomplete,
                avpLength,
                "incomplete-avp-header",
                "AVP source length " + avpLength);
            Assert.That(avp.RequiredCount, Is.EqualTo(8));
            avpLength = avpLength + 1;
        }
    }

    [Test]
    public void StrictAndPermissiveAvpDecodersClassifyEveryFlagByte()
    {
        int flagValue = 0;
        while (flagValue <= byte.MaxValue)
        {
            bool vendorSpecific = (flagValue & 128) != 0;
            bool strictValid = (flagValue & DiameterProtocolConstants.AvpReservedFlagMask) == 0;
            byte[] source = CreateRawAvp(flagValue, vendorSpecific ? 12 : 8, 1U);
            DiameterAvp avp = new DiameterAvp();
            avp.Flags = (DiameterAvpFlags)flagValue;
            if (vendorSpecific)
            {
                avp.VendorId = 1U;
            }

            DiameterEncodeResult measurement = AvpEncoder.MeasureAvp(avp);
            DiameterAvpDecodeResult strict = AvpDecoder.DecodeAvpWithProfile(
                source,
                DiameterDecodeProfile.Strict);
            DiameterAvpDecodeResult permissive = AvpDecoder.DecodeAvpWithProfile(
                source,
                DiameterDecodeProfile.Permissive);

            if (strictValid)
            {
                Assert.That(measurement.Status, Is.EqualTo(DiameterCodecStatus.Success));
                Assert.That(strict.Status, Is.EqualTo(DiameterCodecStatus.Success));
                Assert.That(strict.Issues, Is.Empty);
            }
            else
            {
                AssertEncodeFailure(
                    measurement,
                    DiameterCodecStatus.Invalid,
                    4,
                    "invalid-avp-flags",
                    "encoded AVP flag byte " + flagValue);
                AssertAvpDecodeFailure(
                    strict,
                    DiameterCodecStatus.Invalid,
                    4,
                    "invalid-avp-flags",
                    "strict AVP flag byte " + flagValue);
            }

            Assert.That(
                permissive.Status,
                Is.EqualTo(DiameterCodecStatus.Success),
                $"permissive AVP flag byte {flagValue}");
            Assert.That(
                permissive.Avp!.Flags,
                Is.EqualTo((DiameterAvpFlags)flagValue),
                $"permissive AVP flag byte {flagValue}");
            Assert.That(
                permissive.Avp.VendorId,
                Is.EqualTo(vendorSpecific ? 1U : 0U));
            Assert.That(permissive.Issues.Count, Is.EqualTo(strictValid ? 0 : 1));
            if (!strictValid)
            {
                Assert.That(permissive.Issues[0].Severity, Is.EqualTo(DiameterIssueSeverity.Warning));
                Assert.That(permissive.Issues[0].Offset, Is.EqualTo(4));
                Assert.That(permissive.Issues[0].Code, Is.EqualTo("invalid-avp-flags"));
            }

            flagValue = flagValue + 1;
        }
    }

    [Test]
    public void VendorSpecificFlagCombinationsClassifyZeroVendorIdentifiers()
    {
        int[] flags = new int[] { 128, 160, 192, 224 };
        int index = 0;
        while (index < flags.Length)
        {
            byte[] source = CreateRawAvp(flags[index], 12, 0U);
            DiameterAvpDecodeResult strict = AvpDecoder.DecodeAvpWithProfile(
                source,
                DiameterDecodeProfile.Strict);
            DiameterAvpDecodeResult permissive = AvpDecoder.DecodeAvpWithProfile(
                source,
                DiameterDecodeProfile.Permissive);

            AssertAvpDecodeFailure(
                strict,
                DiameterCodecStatus.Invalid,
                8,
                "invalid-vendor-id",
                "strict flags " + flags[index]);
            Assert.That(permissive.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(permissive.Avp!.VendorId, Is.Zero);
            Assert.That(permissive.Issues, Has.Count.EqualTo(1));
            Assert.That(permissive.Issues[0].Severity, Is.EqualTo(DiameterIssueSeverity.Warning));
            Assert.That(permissive.Issues[0].Offset, Is.EqualTo(8));
            Assert.That(permissive.Issues[0].Code, Is.EqualTo("invalid-vendor-id"));
            index = index + 1;
        }
    }

    [Test]
    public void AvpDecoderClassifiesBaseAndVendorLengthBoundaries()
    {
        int baseLength = 0;
        while (baseLength <= 19)
        {
            int paddedLength = Align(baseLength);
            int sourceLength = Math.Max(DiameterProtocolConstants.AvpHeaderSize, paddedLength);
            DiameterAvpDecodeResult result = AvpDecoder.DecodeAvp(
                CreateRawAvp(0, baseLength, 0U, sourceLength));
            if (baseLength < DiameterProtocolConstants.AvpHeaderSize)
            {
                AssertAvpDecodeFailure(
                    result,
                    DiameterCodecStatus.Invalid,
                    5,
                    "invalid-avp-length",
                    "base AVP length " + baseLength);
            }
            else
            {
                Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
                Assert.That(result.RequiredCount, Is.EqualTo(paddedLength));
                Assert.That(result.ConsumedCount, Is.EqualTo(paddedLength));
                Assert.That(result.Avp!.Data, Has.Length.EqualTo(baseLength - 8));
            }

            baseLength = baseLength + 1;
        }

        int vendorLength = 0;
        while (vendorLength <= 23)
        {
            int paddedLength = Align(vendorLength);
            int sourceLength = Math.Max(
                DiameterProtocolConstants.VendorSpecificAvpHeaderSize,
                paddedLength);
            DiameterAvpDecodeResult result = AvpDecoder.DecodeAvp(
                CreateRawAvp(128, vendorLength, 1U, sourceLength));
            if (vendorLength < DiameterProtocolConstants.VendorSpecificAvpHeaderSize)
            {
                AssertAvpDecodeFailure(
                    result,
                    DiameterCodecStatus.Invalid,
                    5,
                    "missing-vendor-header",
                    "vendor AVP length " + vendorLength);
            }
            else
            {
                Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
                Assert.That(result.RequiredCount, Is.EqualTo(paddedLength));
                Assert.That(result.ConsumedCount, Is.EqualTo(paddedLength));
                Assert.That(result.Avp!.Data, Has.Length.EqualTo(vendorLength - 12));
            }

            vendorLength = vendorLength + 1;
        }

        byte[] maximum = CreateRawAvp(
            0,
            DiameterProtocolConstants.MaximumAvpLength,
            0U,
            DiameterProtocolConstants.AvpHeaderSize);
        DiameterAvpDecodeResult maximumResult = AvpDecoder.DecodeAvp(maximum);
        AssertAvpDecodeFailure(
            maximumResult,
            DiameterCodecStatus.Incomplete,
            DiameterProtocolConstants.AvpHeaderSize,
            "incomplete-avp",
            "maximum AVP length");
        Assert.That(
            maximumResult.RequiredCount,
            Is.EqualTo(DiameterProtocolConstants.MaximumAvpLength + 1));
    }

    [Test]
    public void AvpEncodingAndDecodingCoverEveryPaddingWidthForBothHeaderForms()
    {
        int vendorIndex = 0;
        while (vendorIndex < 2)
        {
            bool vendorSpecific = vendorIndex == 1;
            int payloadLength = 0;
            while (payloadLength <= 7)
            {
                DiameterAvp avp = new DiameterAvp();
                avp.Code = 1234U;
                if (vendorSpecific)
                {
                    avp.Flags = DiameterAvpFlags.VendorSpecific;
                    avp.VendorId = 77U;
                }

                avp.Data = CreatePayload(payloadLength);
                DiameterEncodeResult measurement = AvpEncoder.MeasureAvp(avp);
                byte[] encoded = new byte[measurement.RequiredCount];
                Array.Fill(encoded, (byte)255);
                DiameterEncodeResult encoding = AvpEncoder.EncodeAvp(encoded, avp);
                DiameterAvpDecodeResult decoding = AvpDecoder.DecodeAvp(encoded);
                int headerLength = vendorSpecific ? 12 : 8;
                int declaredLength = headerLength + payloadLength;

                Assert.That(encoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
                Assert.That(encoding.RequiredCount, Is.EqualTo(Align(declaredLength)));
                Assert.That(encoding.WrittenCount, Is.EqualTo(Align(declaredLength)));
                Assert.That(ReadUInt24(encoded, 5), Is.EqualTo(declaredLength));
                int paddingIndex = declaredLength;
                while (paddingIndex < encoded.Length)
                {
                    Assert.That(encoded[paddingIndex], Is.Zero);
                    paddingIndex = paddingIndex + 1;
                }

                Assert.That(decoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
                Assert.That(decoding.ConsumedCount, Is.EqualTo(encoded.Length));
                Assert.That(decoding.Avp!.Data, Is.EqualTo(avp.Data));
                payloadLength = payloadLength + 1;
            }

            vendorIndex = vendorIndex + 1;
        }
    }

    [Test]
    public void StrictAndPermissiveAvpDecodersClassifyEveryPaddingOctet()
    {
        int payloadLength = 1;
        while (payloadLength <= 3)
        {
            int declaredLength = DiameterProtocolConstants.AvpHeaderSize + payloadLength;
            int paddedLength = Align(declaredLength);
            int paddingOffset = declaredLength;
            while (paddingOffset < paddedLength)
            {
                byte[] source = CreateRawAvp(0, declaredLength, 0U, paddedLength);
                source[paddingOffset] = 1;

                DiameterAvpDecodeResult strict = AvpDecoder.DecodeAvpWithProfile(
                    source,
                    DiameterDecodeProfile.Strict);
                DiameterAvpDecodeResult permissive = AvpDecoder.DecodeAvpWithProfile(
                    source,
                    DiameterDecodeProfile.Permissive);

                AssertAvpDecodeFailure(
                    strict,
                    DiameterCodecStatus.Invalid,
                    paddingOffset,
                    "nonzero-avp-padding",
                    "payload " + payloadLength + " padding " + paddingOffset);
                Assert.That(permissive.Status, Is.EqualTo(DiameterCodecStatus.Success));
                Assert.That(permissive.ConsumedCount, Is.EqualTo(paddedLength));
                Assert.That(permissive.Issues, Has.Count.EqualTo(1));
                Assert.That(permissive.Issues[0].Severity, Is.EqualTo(DiameterIssueSeverity.Warning));
                Assert.That(permissive.Issues[0].Offset, Is.EqualTo(paddingOffset));
                Assert.That(permissive.Issues[0].Code, Is.EqualTo("nonzero-avp-padding"));
                paddingOffset = paddingOffset + 1;
            }

            payloadLength = payloadLength + 1;
        }

        byte[] multiple = CreateRawAvp(0, 9, 0U, 12);
        multiple[9] = 1;
        multiple[10] = 2;
        multiple[11] = 3;
        DiameterAvpDecodeResult multipleResult = AvpDecoder.DecodeAvpWithProfile(
            multiple,
            DiameterDecodeProfile.Permissive);
        Assert.That(multipleResult.Status, Is.EqualTo(DiameterCodecStatus.Success));
        Assert.That(multipleResult.Issues, Has.Count.EqualTo(1));
        Assert.That(multipleResult.Issues[0].Offset, Is.EqualTo(9));
    }

    [Test]
    public void HeaderDecodersRejectUnknownProfilesBeforeReadingInput()
    {
        DiameterDecodeResult message = MessageDecoder.DecodeHeaderWithProfile(
            new byte[0],
            (DiameterDecodeProfile)2);
        DiameterAvpDecodeResult avp = AvpDecoder.DecodeAvpWithProfile(
            new byte[0],
            (DiameterDecodeProfile)(-1));

        AssertDecodeFailure(
            message,
            DiameterCodecStatus.Invalid,
            0,
            "invalid-decode-profile",
            "message profile");
        AssertAvpDecodeFailure(
            avp,
            DiameterCodecStatus.Invalid,
            0,
            "invalid-decode-profile",
            "AVP profile");
    }

    private static bool IsValidMessageFlags(int flags)
    {
        bool request = (flags & 128) != 0;
        bool error = (flags & 32) != 0;
        bool retransmitted = (flags & 16) != 0;
        return (flags & DiameterProtocolConstants.MessageReservedFlagMask) == 0 &&
            !(request && error) &&
            !(!request && retransmitted);
    }

    private static DiameterMessage CreateMessage()
    {
        DiameterMessage message = new DiameterMessage();
        message.CommandCode = DiameterCommandCodes.DeviceWatchdog;
        message.HopByHopIdentifier = 1U;
        message.EndToEndIdentifier = 2U;
        return message;
    }

    private static byte[] CreateRawMessageHeader(int messageLength, int flags)
    {
        byte[] header = new byte[DiameterProtocolConstants.MessageHeaderSize];
        header[0] = DiameterProtocolConstants.Version;
        WriteUInt24(header, 1, messageLength);
        header[4] = (byte)flags;
        WriteUInt24(header, 5, (int)DiameterCommandCodes.DeviceWatchdog);
        return header;
    }

    private static byte[] CreateRawAvp(int flags, int declaredLength, uint vendorId)
    {
        return CreateRawAvp(flags, declaredLength, vendorId, Align(declaredLength));
    }

    private static byte[] CreateRawAvp(
        int flags,
        int declaredLength,
        uint vendorId,
        int sourceLength)
    {
        byte[] source = new byte[sourceLength];
        source[3] = 1;
        source[4] = (byte)flags;
        WriteUInt24(source, 5, declaredLength);
        if ((flags & 128) != 0 && source.Length >= 12)
        {
            source[8] = (byte)(vendorId >> 24);
            source[9] = (byte)(vendorId >> 16);
            source[10] = (byte)(vendorId >> 8);
            source[11] = (byte)vendorId;
        }

        return source;
    }

    private static byte[] CreatePayload(int length)
    {
        byte[] payload = new byte[length];
        int index = 0;
        while (index < length)
        {
            payload[index] = (byte)(index + 1);
            index = index + 1;
        }

        return payload;
    }

    private static int Align(int length)
    {
        int remainder = length % DiameterProtocolConstants.Alignment;
        if (remainder == 0)
        {
            return length;
        }

        return length + DiameterProtocolConstants.Alignment - remainder;
    }

    private static int ReadUInt24(byte[] source, int offset)
    {
        return (source[offset] << 16) |
            (source[offset + 1] << 8) |
            source[offset + 2];
    }

    private static void WriteUInt24(byte[] destination, int offset, int value)
    {
        destination[offset] = (byte)(value >> 16);
        destination[offset + 1] = (byte)(value >> 8);
        destination[offset + 2] = (byte)value;
    }

    private static void AssertEncodeFailure(
        DiameterEncodeResult result,
        DiameterCodecStatus status,
        int offset,
        string code,
        string context)
    {
        Assert.That(result.Status, Is.EqualTo(status), context);
        Assert.That(result.WrittenCount, Is.Zero, context);
        Assert.That(result.Issues, Has.Count.EqualTo(1), context);
        Assert.That(result.Issues[0].Severity, Is.EqualTo(DiameterIssueSeverity.Error), context);
        Assert.That(result.Issues[0].Status, Is.EqualTo(status), context);
        Assert.That(result.Issues[0].Offset, Is.EqualTo(offset), context);
        Assert.That(result.Issues[0].Code, Is.EqualTo(code), context);
    }

    private static void AssertDecodeFailure(
        DiameterDecodeResult result,
        DiameterCodecStatus status,
        int offset,
        string code,
        string context)
    {
        Assert.That(result.Status, Is.EqualTo(status), context);
        Assert.That(result.ConsumedCount, Is.Zero, context);
        Assert.That(result.Message, Is.Null, context);
        Assert.That(result.Issues, Has.Count.EqualTo(1), context);
        Assert.That(result.Issues[0].Status, Is.EqualTo(status), context);
        Assert.That(result.Issues[0].Offset, Is.EqualTo(offset), context);
        Assert.That(result.Issues[0].Code, Is.EqualTo(code), context);
    }

    private static void AssertAvpDecodeFailure(
        DiameterAvpDecodeResult result,
        DiameterCodecStatus status,
        int offset,
        string code,
        string context)
    {
        Assert.That(result.Status, Is.EqualTo(status), context);
        Assert.That(result.ConsumedCount, Is.Zero, context);
        Assert.That(result.Avp, Is.Null, context);
        Assert.That(result.Issues, Has.Count.EqualTo(1), context);
        Assert.That(result.Issues[0].Status, Is.EqualTo(status), context);
        Assert.That(result.Issues[0].Offset, Is.EqualTo(offset), context);
        Assert.That(result.Issues[0].Code, Is.EqualTo(code), context);
    }
}
