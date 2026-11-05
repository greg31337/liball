using rtp;

namespace rtp.Tests.Wire;

public class RtcpPayloadFeedbackBoundaryTests
{
    [Test]
    public void EmptyFciRulesAreAppliedIndependentlyForEveryFeedbackKind()
    {
        IRtcpPacketCodec codec = CreateCodec(65535);
        RtcpPacket pictureLoss = CreatePictureLossPacket();
        RtcpPacket emptySliceLoss = CreateSliceLossPacket([]);
        RtcpPacket emptyReferencePicture = CreateReferencePicturePacket(
            16,
            0,
            0,
            []);
        RtcpPacket emptyApplication = CreateApplicationFeedbackPacket([]);
        RtcpPacket emptyUnknown = CreateOpaquePayloadFeedbackPacket(12, []);
        byte[] pictureLossWire = codec.EncodeRtcpPacketToArray(pictureLoss);
        byte[] referencePictureWire = codec.EncodeRtcpPacketToArray(
            emptyReferencePicture);
        byte[] applicationWire = codec.EncodeRtcpPacketToArray(emptyApplication);
        byte[] unknownWire = codec.EncodeRtcpPacketToArray(emptyUnknown);

        Assert.Multiple(() =>
        {
            Assert.That(pictureLossWire, Has.Length.EqualTo(12));
            Assert.That(codec.DecodeRtcpPacket(pictureLossWire).Feedback?
                .RawFeedbackControlInformation, Is.Empty);
            Assert.That(codec.TryEncodeRtcpPacket(
                emptySliceLoss,
                new byte[12]).Success, Is.False);
            Assert.That(referencePictureWire, Has.Length.EqualTo(16));
            Assert.That(referencePictureWire[12..],
                Is.EqualTo(new byte[] { 16, 0, 0, 0 }));
            Assert.That(applicationWire, Has.Length.EqualTo(12));
            Assert.That(codec.DecodeRtcpPacket(applicationWire).Feedback?
                .ApplicationFeedback?.Data, Is.Empty);
            Assert.That(unknownWire, Has.Length.EqualTo(12));
            Assert.That(codec.DecodeRtcpPacket(unknownWire).Feedback?
                .RawFeedbackControlInformation, Is.Empty);
        });
    }

    [Test]
    public void SliceLossFieldsRoundTripEveryBitAndPictureIdentifierValue()
    {
        IRtcpPacketCodec codec = CreateCodec(65535);
        int bitIndex = 0;
        while (bitIndex < 13)
        {
            int value = 1 << bitIndex;
            RtcpSliceLossEntry entry = new RtcpSliceLossEntry();
            entry.FirstMacroblock = value;
            entry.MacroblockCount = value;
            entry.PictureIdentifier = (byte)(bitIndex * 5);
            RtcpPacket packet = CreateSliceLossPacket(
                new RtcpSliceLossEntry[] { entry });

            byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
            RtcpSliceLossEntry decoded = codec.DecodeRtcpPacket(encoded)
                .Feedback!.SliceLossIndication!.Entries[0];
            uint packed = ((uint)value << 19) |
                ((uint)value << 6) |
                entry.PictureIdentifier;

            Assert.Multiple(() =>
            {
                Assert.That(encoded[12], Is.EqualTo((byte)(packed >> 24)));
                Assert.That(encoded[13], Is.EqualTo((byte)(packed >> 16)));
                Assert.That(encoded[14], Is.EqualTo((byte)(packed >> 8)));
                Assert.That(encoded[15], Is.EqualTo((byte)packed));
                Assert.That(decoded.FirstMacroblock, Is.EqualTo(value));
                Assert.That(decoded.MacroblockCount, Is.EqualTo(value));
                Assert.That(decoded.PictureIdentifier,
                    Is.EqualTo(entry.PictureIdentifier));
            });
            bitIndex = bitIndex + 1;
        }

        int pictureIdentifier = 0;
        while (pictureIdentifier <= 63)
        {
            RtcpSliceLossEntry entry = new RtcpSliceLossEntry();
            entry.FirstMacroblock = 8191;
            entry.MacroblockCount = 8191;
            entry.PictureIdentifier = (byte)pictureIdentifier;
            byte[] encoded = codec.EncodeRtcpPacketToArray(
                CreateSliceLossPacket(new RtcpSliceLossEntry[] { entry }));
            RtcpSliceLossEntry decoded = codec.DecodeRtcpPacket(encoded)
                .Feedback!.SliceLossIndication!.Entries[0];

            Assert.Multiple(() =>
            {
                Assert.That(decoded.FirstMacroblock, Is.EqualTo(8191));
                Assert.That(decoded.MacroblockCount, Is.EqualTo(8191));
                Assert.That(decoded.PictureIdentifier,
                    Is.EqualTo(pictureIdentifier));
            });
            pictureIdentifier = pictureIdentifier + 1;
        }
    }

    [Test]
    public void SliceLossRejectsEveryIncompleteFciAndPacketTruncation()
    {
        IRtcpPacketCodec codec = CreateCodec(65535);
        int fciLength = 0;
        while (fciLength < 4)
        {
            RtcpCodecResult result = codec.TryDecodeRtcpPacket(
                CreateIncompleteFeedbackPacket(2, fciLength));
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.ConsumedCount, Is.Zero);
                Assert.That(result.Packet, Is.Null);
            });
            fciLength = fciLength + 1;
        }

        RtcpSliceLossEntry first = new RtcpSliceLossEntry();
        first.FirstMacroblock = 1;
        first.MacroblockCount = 2;
        first.PictureIdentifier = 3;
        RtcpSliceLossEntry second = new RtcpSliceLossEntry();
        second.FirstMacroblock = 8191;
        second.MacroblockCount = 8191;
        second.PictureIdentifier = 63;
        byte[] valid = codec.EncodeRtcpPacketToArray(CreateSliceLossPacket(
            new RtcpSliceLossEntry[] { first, second }));
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
    public void ReferencePictureSelectionRoundTripsEveryPaddingBitCount()
    {
        IRtcpPacketCodec codec = CreateCodec(65535);
        int bitLength = 0;
        while (bitLength < 32)
        {
            int paddingBitCount = (32 - ((16 + bitLength) % 32)) % 32;
            byte[] nativeBits = CreateNativeBits(bitLength);
            RtcpPacket packet = CreateReferencePicturePacket(
                (byte)paddingBitCount,
                127,
                bitLength,
                nativeBits);

            byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
            RtcpReferencePictureSelectionIndication decoded = codec
                .DecodeRtcpPacket(encoded).Feedback!
                .ReferencePictureSelectionIndication!;

            Assert.Multiple(() =>
            {
                Assert.That(encoded[12], Is.EqualTo(paddingBitCount));
                Assert.That(encoded[13], Is.EqualTo(127));
                Assert.That(decoded.PaddingBitCount, Is.EqualTo(paddingBitCount));
                Assert.That(decoded.PayloadType, Is.EqualTo(127));
                Assert.That(decoded.BitLength, Is.EqualTo(bitLength));
                Assert.That(decoded.NativeBitString, Is.EqualTo(nativeBits));
                Assert.That(codec.EncodeRtcpPacketToArray(
                    codec.DecodeRtcpPacket(encoded)), Is.EqualTo(encoded));
            });
            bitLength = bitLength + 1;
        }
    }

    [Test]
    public void ReferencePictureSelectionRejectsEveryNonzeroPaddingBit()
    {
        IRtcpPacketCodec codec = CreateCodec(65535);
        int bitLength = 0;
        while (bitLength < 32)
        {
            int paddingBitCount = (32 - ((16 + bitLength) % 32)) % 32;
            byte[] valid = codec.EncodeRtcpPacketToArray(
                CreateReferencePicturePacket(
                    (byte)paddingBitCount,
                    96,
                    bitLength,
                    CreateNativeBits(bitLength)));
            int fciBitCount = (valid.Length - 12) * 8;
            int paddingIndex = 0;
            while (paddingIndex < paddingBitCount)
            {
                byte[] invalid = (byte[])valid.Clone();
                int bitPosition = fciBitCount - paddingBitCount + paddingIndex;
                int byteIndex = 12 + bitPosition / 8;
                int bitMask = 1 << (7 - bitPosition % 8);
                invalid[byteIndex] = (byte)(invalid[byteIndex] | bitMask);

                RtcpCodecResult result = codec.TryDecodeRtcpPacket(invalid);
                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.False);
                    Assert.That(result.ConsumedCount, Is.Zero);
                    Assert.That(result.Packet, Is.Null);
                });
                paddingIndex = paddingIndex + 1;
            }

            bitLength = bitLength + 1;
        }
    }

    [Test]
    public void ReferencePictureSelectionAcceptsEverySevenBitPayloadType()
    {
        IRtcpPacketCodec codec = CreateCodec(65535);
        int payloadType = 0;
        while (payloadType <= 127)
        {
            RtcpPacket packet = CreateReferencePicturePacket(
                0,
                (byte)payloadType,
                16,
                new byte[] { 0xAA, 0x55 });

            byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
            RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);

            Assert.Multiple(() =>
            {
                Assert.That(encoded[13], Is.EqualTo(payloadType));
                Assert.That(decoded.Feedback?
                    .ReferencePictureSelectionIndication?.PayloadType,
                    Is.EqualTo(payloadType));
            });
            payloadType = payloadType + 1;
        }
    }

    [Test]
    public void ApplicationFeedbackRoundTripsEmptyAndMaximumWireData()
    {
        IRtcpPacketCodec codec = CreateCodec(RtcpProtocolConstants.MaximumPacketSize);
        int maximumDataLength = RtcpProtocolConstants.MaximumPacketSize -
            RtcpProtocolConstants.FeedbackMinimumSize;
        int[] dataLengths = new int[] { 0, 4, 256, maximumDataLength };
        int lengthIndex = 0;
        while (lengthIndex < dataLengths.Length)
        {
            byte[] data = CreatePattern(dataLengths[lengthIndex]);
            RtcpPacket packet = CreateApplicationFeedbackPacket(data);

            byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
            RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);
            if (data.Length > 0)
            {
                data[0] = (byte)(data[0] + 1);
            }

            Assert.Multiple(() =>
            {
                Assert.That(encoded, Has.Length.EqualTo(
                    RtcpProtocolConstants.FeedbackMinimumSize +
                    dataLengths[lengthIndex]));
                Assert.That(decoded.Feedback?.ApplicationFeedback?.Data,
                    Is.EqualTo(encoded[12..]));
                Assert.That(codec.EncodeRtcpPacketToArray(decoded),
                    Is.EqualTo(encoded));
            });
            lengthIndex = lengthIndex + 1;
        }

        RtcpPacket oversized = CreateApplicationFeedbackPacket(
            new byte[maximumDataLength + 4]);
        byte[] destination = CreateFilledBytes(32, 0xA5);
        RtcpCodecResult oversizedResult = codec.TryEncodeRtcpPacket(
            oversized,
            destination);
        Assert.Multiple(() =>
        {
            Assert.That(oversizedResult.Success, Is.False);
            Assert.That(oversizedResult.WrittenCount, Is.Zero);
            Assert.That(oversizedResult.ErrorMessage, Does.Contain("wire length"));
            Assert.That(destination, Has.All.EqualTo(0xA5));
        });
    }

    [Test]
    public void EveryPayloadFormatWithoutTypedFciPreservesOpaqueData()
    {
        IRtcpPacketCodec codec = CreateCodec(65535);
        byte[] formats = new byte[]
        {
            0,
            7, 8, 9, 10, 11, 12, 13, 14,
            16, 17, 18, 19, 20, 21, 22, 23,
            24, 25, 26, 27, 28, 29, 30, 31
        };
        int formatIndex = 0;
        while (formatIndex < formats.Length)
        {
            byte format = formats[formatIndex];
            int fciLength = format % 4;
            byte[] source = CreateOpaqueFeedbackWire(format, fciLength);
            byte[] expected = (byte[])source.Clone();

            RtcpPacket decoded = codec.DecodeRtcpPacket(source);
            Array.Fill(source, (byte)0);
            byte[] reencoded = codec.EncodeRtcpPacketToArray(decoded);

            Assert.Multiple(() =>
            {
                Assert.That(decoded.Feedback?.Format, Is.EqualTo(format));
                Assert.That(decoded.Feedback?.PayloadFormat,
                    Is.EqualTo(RtcpFeedbackWireCodec.GetPayloadFormat(format)));
                Assert.That(decoded.Feedback?.RawFeedbackControlInformation,
                    Is.EqualTo(expected[12..(12 + fciLength)]));
                Assert.That(reencoded, Is.EqualTo(expected));
            });
            formatIndex = formatIndex + 1;
        }
    }

    static RtcpPacket CreatePictureLossPacket()
    {
        RtcpPictureLossIndication indication = new RtcpPictureLossIndication();
        indication.Requested = true;
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        feedback.PayloadFormat = RtcpPayloadFeedbackFormat.PictureLossIndication;
        feedback.SenderSourceIdentifier = 1U;
        feedback.MediaSourceIdentifier = 2U;
        feedback.PictureLossIndication = indication;
        return CreateFeedbackPacket(feedback);
    }

    static RtcpPacket CreateSliceLossPacket(RtcpSliceLossEntry[] entries)
    {
        RtcpSliceLossIndication indication = new RtcpSliceLossIndication();
        indication.Entries = entries;
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        feedback.PayloadFormat = RtcpPayloadFeedbackFormat.SliceLossIndication;
        feedback.SenderSourceIdentifier = 1U;
        feedback.MediaSourceIdentifier = 2U;
        feedback.SliceLossIndication = indication;
        return CreateFeedbackPacket(feedback);
    }

    static RtcpPacket CreateReferencePicturePacket(
        byte paddingBitCount,
        byte payloadType,
        int bitLength,
        byte[] nativeBits)
    {
        RtcpReferencePictureSelectionIndication indication =
            new RtcpReferencePictureSelectionIndication();
        indication.PaddingBitCount = paddingBitCount;
        indication.PayloadType = payloadType;
        indication.BitLength = bitLength;
        indication.NativeBitString = nativeBits;
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        feedback.PayloadFormat =
            RtcpPayloadFeedbackFormat.ReferencePictureSelectionIndication;
        feedback.SenderSourceIdentifier = 1U;
        feedback.MediaSourceIdentifier = 2U;
        feedback.ReferencePictureSelectionIndication = indication;
        return CreateFeedbackPacket(feedback);
    }

    static RtcpPacket CreateApplicationFeedbackPacket(byte[] data)
    {
        RtcpApplicationFeedback applicationFeedback =
            new RtcpApplicationFeedback();
        applicationFeedback.Data = data;
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        feedback.PayloadFormat = RtcpPayloadFeedbackFormat.ApplicationFeedback;
        feedback.SenderSourceIdentifier = 1U;
        feedback.MediaSourceIdentifier = 2U;
        feedback.ApplicationFeedback = applicationFeedback;
        return CreateFeedbackPacket(feedback);
    }

    static RtcpPacket CreateOpaquePayloadFeedbackPacket(byte format, byte[] data)
    {
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        feedback.Format = format;
        feedback.RawFeedbackControlInformation = data;
        feedback.SenderSourceIdentifier = 1U;
        feedback.MediaSourceIdentifier = 2U;
        return CreateFeedbackPacket(feedback);
    }

    static RtcpPacket CreateFeedbackPacket(RtcpFeedbackPacket feedback)
    {
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.PayloadFeedback;
        packet.Feedback = feedback;
        return packet;
    }

    static byte[] CreateIncompleteFeedbackPacket(byte format, int fciLength)
    {
        if (fciLength == 0)
        {
            return new byte[]
            {
                (byte)(0x80 | format), 206, 0, 2,
                0, 0, 0, 1,
                0, 0, 0, 2
            };
        }

        byte[] packet = new byte[16];
        packet[0] = (byte)(0xA0 | format);
        packet[1] = 206;
        packet[3] = 3;
        packet[7] = 1;
        packet[11] = 2;
        packet[^1] = (byte)(4 - fciLength);
        return packet;
    }

    static byte[] CreateOpaqueFeedbackWire(byte format, int fciLength)
    {
        int paddingLength = 0;
        if (fciLength > 0)
        {
            paddingLength = 4 - fciLength;
        }

        byte[] packet = new byte[12 + fciLength + paddingLength];
        packet[0] = (byte)(0x80 | format);
        if (paddingLength > 0)
        {
            packet[0] = (byte)(packet[0] | 0x20);
        }

        packet[1] = 206;
        int lengthInWordsMinusOne = packet.Length / 4 - 1;
        packet[2] = (byte)(lengthInWordsMinusOne >> 8);
        packet[3] = (byte)lengthInWordsMinusOne;
        packet[7] = 1;
        packet[11] = 2;
        int fciIndex = 0;
        while (fciIndex < fciLength)
        {
            packet[12 + fciIndex] = (byte)(0xA0 + fciIndex);
            fciIndex = fciIndex + 1;
        }

        if (paddingLength > 0)
        {
            int paddingIndex = packet.Length - paddingLength;
            while (paddingIndex < packet.Length - 1)
            {
                packet[paddingIndex] = (byte)(0x50 + paddingIndex);
                paddingIndex = paddingIndex + 1;
            }

            packet[^1] = (byte)paddingLength;
        }

        return packet;
    }

    static byte[] CreateNativeBits(int bitLength)
    {
        int byteLength = (bitLength + 7) / 8;
        byte[] bits = new byte[byteLength];
        Array.Fill(bits, byte.MaxValue);
        int finalBitCount = bitLength % 8;
        if (finalBitCount != 0)
        {
            bits[^1] = (byte)(byte.MaxValue << (8 - finalBitCount));
        }

        return bits;
    }

    static byte[] CreatePattern(int length)
    {
        byte[] data = new byte[length];
        int index = 0;
        while (index < data.Length)
        {
            data[index] = (byte)(index * 37 + 11);
            index = index + 1;
        }

        return data;
    }

    static byte[] CreateFilledBytes(int length, byte value)
    {
        byte[] data = new byte[length];
        Array.Fill(data, value);
        return data;
    }

    static IRtcpPacketCodec CreateCodec(int maximumPacketSize)
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.StrictValidation = true;
        configuration.PreservePadding = true;
        configuration.MaximumRtcpCompoundPacketSize = maximumPacketSize;
        return new RtcpPacketCodec(configuration);
    }
}
