using rtp;

namespace rtp.Tests.Wire;

public class RtpPacketMalformedTests
{
    [Test]
    public void EveryTruncationPointOfACompletePacketIsRejected()
    {
        IRtpPacketCodec codec = CreateCodec(true, 65535);
        byte[] complete = new byte[]
        {
            0xB1, 0xE0, 0x12, 0x34,
            0x56, 0x78, 0x9A, 0xBC,
            0xDE, 0xF0, 0x12, 0x34,
            0x01, 0x02, 0x03, 0x04,
            0x12, 0x34, 0x00, 0x01,
            0x10, 0x20, 0x30, 0x40,
            0xFF, 0xFE,
            0xAA, 0xBB, 0x03
        };
        Assert.That(codec.DecodeRtpPacket(complete).Padding,
            Is.EqualTo(new byte[] { 0xAA, 0xBB, 0x03 }));

        int length = 0;
        while (length < complete.Length)
        {
            byte[] truncated = complete[..length];
            RtpCodecResult result = codec.TryDecodeRtpPacket(truncated);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.ConsumedCount, Is.Zero);
                Assert.That(result.Packet, Is.Null);
                Assert.That(result.ErrorMessage, Is.Not.Empty);
                Assert.Throws<RtpPacketFormatException>(() =>
                    codec.DecodeRtpPacket(truncated));
            });
            length = length + 1;
        }
    }

    [Test]
    public void EveryNonzeroCsrcCountRejectsAListMissingItsFinalByte()
    {
        IRtpPacketCodec codec = CreateCodec(true, 65535);
        int sourceCount = 1;
        while (sourceCount <= RtpProtocolConstants.MaximumCsrcCount)
        {
            int requiredLength = RtpProtocolConstants.FixedHeaderSize +
                sourceCount * RtpProtocolConstants.SourceIdentifierSize;
            byte[] source = new byte[requiredLength - 1];
            source[0] = (byte)(0x80 | sourceCount);

            RtpCodecResult result = codec.TryDecodeRtpPacket(source);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.ConsumedCount, Is.Zero);
                Assert.That(result.ErrorMessage,
                    Does.Contain("contributing-source"));
            });
            sourceCount = sourceCount + 1;
        }
    }

    [Test]
    public void ExtensionWordCountsRejectEveryMissingAdvertisedByte()
    {
        IRtpPacketCodec codec = CreateCodec(true, 65535);
        int wordCount = 1;
        while (wordCount <= 4)
        {
            int dataLength = wordCount * 4;
            int missingByteCount = 1;
            while (missingByteCount <= dataLength)
            {
                int availableDataLength = dataLength - missingByteCount;
                byte[] source = new byte[16 + availableDataLength];
                source[0] = 0x90;
                source[12] = 0x12;
                source[13] = 0x34;
                source[14] = (byte)(wordCount >> 8);
                source[15] = (byte)wordCount;

                RtpCodecResult result = codec.TryDecodeRtpPacket(source);

                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.False);
                    Assert.That(result.ConsumedCount, Is.Zero);
                    Assert.That(result.ErrorMessage,
                        Does.Contain("extension data"));
                });
                missingByteCount = missingByteCount + 1;
            }

            wordCount = wordCount + 1;
        }
    }

    [Test]
    public void MaximumExtensionWordCountCannotAdvertiseUnavailableData()
    {
        IRtpPacketCodec codec = CreateCodec(true, 65535);
        byte[] source = new byte[]
        {
            0x90, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0x12, 0x34, 0xFF, 0xFF
        };

        RtpCodecResult result = codec.TryDecodeRtpPacket(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ConsumedCount, Is.Zero);
            Assert.That(result.ErrorMessage,
                Does.Contain("extension data"));
        });
    }

    [Test]
    public void EveryOversizedPaddingCountIsRejected()
    {
        IRtpPacketCodec codec = CreateCodec(true, 65535);
        int remainingLength = 1;
        while (remainingLength <= 255)
        {
            byte[] source = new byte[
                RtpProtocolConstants.FixedHeaderSize + remainingLength];
            source[0] = 0xA0;
            int invalidPaddingCount = remainingLength + 1;
            if (invalidPaddingCount > 255)
            {
                invalidPaddingCount = 255;
                source[source.Length - 2] = 1;
            }

            source[source.Length - 1] = (byte)invalidPaddingCount;
            RtpCodecResult result = codec.TryDecodeRtpPacket(source);

            if (remainingLength < 255)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.False);
                    Assert.That(result.ConsumedCount, Is.Zero);
                    Assert.That(result.ErrorMessage,
                        Does.Contain("padding count exceeds"));
                });
            }
            else
            {
                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.True);
                    Assert.That(result.Packet?.Payload, Is.Empty);
                    Assert.That(result.Packet?.Padding,
                        Has.Length.EqualTo(255));
                });
            }

            remainingLength = remainingLength + 1;
        }
    }

    [Test]
    public void MissingAndZeroPaddingCountsFailInBothValidationModes()
    {
        IRtpPacketCodec strictCodec = CreateCodec(true, 65535);
        IRtpPacketCodec tolerantCodec = CreateCodec(false, 65535);
        byte[] missing = new byte[RtpProtocolConstants.FixedHeaderSize];
        missing[0] = 0xA0;
        byte[] zero = new byte[RtpProtocolConstants.FixedHeaderSize + 1];
        zero[0] = 0xA0;

        AssertMalformedInBothModes(strictCodec, tolerantCodec, missing);
        AssertMalformedInBothModes(strictCodec, tolerantCodec, zero);
    }

    [Test]
    public void ConfiguredMaximumRejectsOversizedInputBeforeParsingInBothModes()
    {
        int maximumSize = RtpProtocolConstants.FixedHeaderSize;
        IRtpPacketCodec strictCodec = CreateCodec(true, maximumSize);
        IRtpPacketCodec tolerantCodec = CreateCodec(false, maximumSize);
        byte[] source = new byte[maximumSize + 1];
        source[0] = 0x80;

        RtpCodecResult strictResult = strictCodec.TryDecodeRtpPacket(source);
        RtpCodecResult tolerantResult = tolerantCodec.TryDecodeRtpPacket(
            source);

        Assert.Multiple(() =>
        {
            Assert.That(strictResult.Success, Is.False);
            Assert.That(strictResult.ConsumedCount, Is.Zero);
            Assert.That(strictResult.ErrorMessage,
                Does.Contain("maximum packet size"));
            Assert.That(tolerantResult.Success, Is.False);
            Assert.That(tolerantResult.ConsumedCount, Is.Zero);
            Assert.That(tolerantResult.ErrorMessage,
                Does.Contain("maximum packet size"));
        });
    }

    [Test]
    public void StrictModeRejectsBothRtcpCollisionOctetsThatTolerantModeRetains()
    {
        IRtpPacketCodec strictCodec = CreateCodec(true, 65535);
        IRtpPacketCodec tolerantCodec = CreateCodec(false, 65535);
        byte[] collisionOctets = new byte[]
        {
            RtcpProtocolConstants.SenderReportPacketType,
            RtcpProtocolConstants.ReceiverReportPacketType
        };
        int collisionIndex = 0;
        while (collisionIndex < collisionOctets.Length)
        {
            byte collisionOctet = collisionOctets[collisionIndex];
            byte[] source = new byte[RtpProtocolConstants.FixedHeaderSize];
            source[0] = 0x80;
            source[1] = collisionOctet;
            RtpCodecResult strictResult = strictCodec.TryDecodeRtpPacket(
                source);
            RtpCodecResult tolerantResult = tolerantCodec.TryDecodeRtpPacket(
                source);
            RtpPacket packet = new RtpPacket();
            packet.Marker = true;
            packet.PayloadType = (byte)(collisionOctet & 0x7F);
            RtpCodecResult strictEncode = strictCodec.TryEncodeRtpPacket(
                packet,
                new byte[source.Length]);
            byte[] tolerantEncoding = tolerantCodec
                .EncodeRtpPacketToArray(packet);

            Assert.Multiple(() =>
            {
                Assert.That(strictResult.Success, Is.False);
                Assert.That(strictResult.ErrorMessage,
                    Does.Contain("RTCP report"));
                Assert.That(strictEncode.Success, Is.False);
                Assert.That(strictEncode.WrittenCount, Is.Zero);
                Assert.That(tolerantResult.Success, Is.True);
                Assert.That(tolerantResult.Packet?.Marker, Is.True);
                Assert.That(tolerantResult.Packet?.PayloadType,
                    Is.EqualTo((byte)(collisionOctet & 0x7F)));
                Assert.That(tolerantEncoding, Is.EqualTo(source));
            });
            collisionIndex = collisionIndex + 1;
        }
    }

    [Test]
    public void TolerantModeDoesNotRelaxStructuralSafetyChecks()
    {
        IRtpPacketCodec strictCodec = CreateCodec(true, 65535);
        IRtpPacketCodec tolerantCodec = CreateCodec(false, 65535);
        byte[] wrongVersion = new byte[
            RtpProtocolConstants.FixedHeaderSize];
        wrongVersion[0] = 0x40;
        byte[] truncatedSources = new byte[
            RtpProtocolConstants.FixedHeaderSize + 3];
        truncatedSources[0] = 0x81;
        byte[] truncatedExtension = new byte[]
        {
            0x90, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0x12, 0x34, 0, 1,
            1, 2, 3
        };
        byte[] invalidPadding = new byte[]
        {
            0xA0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            2
        };

        AssertMalformedInBothModes(
            strictCodec,
            tolerantCodec,
            wrongVersion);
        AssertMalformedInBothModes(
            strictCodec,
            tolerantCodec,
            truncatedSources);
        AssertMalformedInBothModes(
            strictCodec,
            tolerantCodec,
            truncatedExtension);
        AssertMalformedInBothModes(
            strictCodec,
            tolerantCodec,
            invalidPadding);
    }

    static void AssertMalformedInBothModes(
        IRtpPacketCodec strictCodec,
        IRtpPacketCodec tolerantCodec,
        byte[] source)
    {
        RtpCodecResult strictResult = strictCodec.TryDecodeRtpPacket(source);
        RtpCodecResult tolerantResult = tolerantCodec.TryDecodeRtpPacket(
            source);

        Assert.Multiple(() =>
        {
            Assert.That(strictResult.Success, Is.False);
            Assert.That(strictResult.ConsumedCount, Is.Zero);
            Assert.That(strictResult.Packet, Is.Null);
            Assert.That(tolerantResult.Success, Is.False);
            Assert.That(tolerantResult.ConsumedCount, Is.Zero);
            Assert.That(tolerantResult.Packet, Is.Null);
        });
    }

    static IRtpPacketCodec CreateCodec(
        bool strictValidation,
        int maximumPacketSize)
    {
        RtpSessionConfiguration configuration =
            new RtpSessionConfiguration();
        configuration.StrictValidation = strictValidation;
        configuration.PreservePadding = true;
        configuration.MaximumRtpPacketSize = maximumPacketSize;
        return new RtpPacketCodec(configuration);
    }
}
