using rtp;

namespace rtp.Tests.Wire;

public class RtcpSenderReceiverReportBoundaryTests
{
    [Test]
    public void MinimumSenderReportHasTheExactRfc3550Layout()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        RtcpPacket packet = CreateReportPacket(RtcpPacketType.SenderReport, 0);
        RtcpSenderReport report = packet.SenderReport!;
        report.SenderSynchronizationSource = 0x01020304U;
        report.SenderInformation.NtpTimestamp = 0x1112131415161718UL;
        report.SenderInformation.RtpTimestamp = 0x21222324U;
        report.SenderInformation.PacketCount = 0x31323334U;
        report.SenderInformation.OctetCount = 0x41424344U;
        byte[] expected = new byte[]
        {
            0x80, 200, 0, 6,
            0x01, 0x02, 0x03, 0x04,
            0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
            0x21, 0x22, 0x23, 0x24,
            0x31, 0x32, 0x33, 0x34,
            0x41, 0x42, 0x43, 0x44
        };

        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
        RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(codec.GetRtcpPacketEncodedSize(packet), Is.EqualTo(28));
            Assert.That(encoded, Is.EqualTo(expected));
            Assert.That(decoded.Header.Count, Is.Zero);
            Assert.That(decoded.Header.LengthInWordsMinusOne, Is.EqualTo(6));
            Assert.That(decoded.SenderReport?.SenderSynchronizationSource,
                Is.EqualTo(0x01020304U));
            Assert.That(decoded.SenderReport?.SenderInformation.NtpTimestamp,
                Is.EqualTo(0x1112131415161718UL));
            Assert.That(decoded.SenderReport?.SenderInformation.RtpTimestamp,
                Is.EqualTo(0x21222324U));
            Assert.That(decoded.SenderReport?.SenderInformation.PacketCount,
                Is.EqualTo(0x31323334U));
            Assert.That(decoded.SenderReport?.SenderInformation.OctetCount,
                Is.EqualTo(0x41424344U));
            Assert.That(decoded.SenderReport?.ReceptionReports, Is.Empty);
        });
    }

    [Test]
    public void MinimumReceiverReportHasTheExactRfc3550Layout()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        RtcpPacket packet = CreateReportPacket(RtcpPacketType.ReceiverReport, 0);
        packet.ReceiverReport!.SenderSynchronizationSource = 0xFEDCBA98U;
        byte[] expected = new byte[]
        {
            0x80, 201, 0, 1,
            0xFE, 0xDC, 0xBA, 0x98
        };

        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
        RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(codec.GetRtcpPacketEncodedSize(packet), Is.EqualTo(8));
            Assert.That(encoded, Is.EqualTo(expected));
            Assert.That(decoded.Header.Count, Is.Zero);
            Assert.That(decoded.Header.LengthInWordsMinusOne, Is.EqualTo(1));
            Assert.That(decoded.ReceiverReport?.SenderSynchronizationSource,
                Is.EqualTo(0xFEDCBA98U));
            Assert.That(decoded.ReceiverReport?.ReceptionReports, Is.Empty);
        });
    }

    [Test]
    public void SenderAndReceiverReportsRoundTripAtZeroAnd31ReportCounts()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        RtcpPacketType[] types = new RtcpPacketType[]
        {
            RtcpPacketType.SenderReport,
            RtcpPacketType.ReceiverReport
        };
        int[] counts = new int[] { 0, 31 };
        int typeIndex = 0;
        while (typeIndex < types.Length)
        {
            int countIndex = 0;
            while (countIndex < counts.Length)
            {
                int count = counts[countIndex];
                RtcpPacket packet = CreateReportPacket(types[typeIndex], count);
                int expectedSize = 8 + count * 24;
                byte expectedPacketType = 201;
                if (types[typeIndex] == RtcpPacketType.SenderReport)
                {
                    expectedSize = expectedSize + 20;
                    expectedPacketType = 200;
                }

                byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
                RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);
                RtcpReceptionReport[] reports = GetReports(decoded);

                Assert.Multiple(() =>
                {
                    Assert.That(encoded, Has.Length.EqualTo(expectedSize));
                    Assert.That(encoded[0], Is.EqualTo((byte)(0x80 | count)));
                    Assert.That(encoded[1], Is.EqualTo(expectedPacketType));
                    Assert.That((encoded[2] << 8 | encoded[3]),
                        Is.EqualTo(expectedSize / 4 - 1));
                    Assert.That(decoded.Header.Count, Is.EqualTo(count));
                    Assert.That(reports, Has.Length.EqualTo(count));
                });
                if (count > 0)
                {
                    AssertReportEqual(
                        GetReports(packet)[0],
                        reports[0],
                        "first report");
                    AssertReportEqual(
                        GetReports(packet)[count - 1],
                        reports[count - 1],
                        "last report");
                }

                countIndex = countIndex + 1;
            }

            typeIndex = typeIndex + 1;
        }
    }

    [Test]
    public void SenderInformationZeroAndMaximumValuesHaveGoldenEncodings()
    {
        RtcpSenderInformation zero = new RtcpSenderInformation();
        RtcpSenderInformation maximum = new RtcpSenderInformation();
        maximum.NtpTimestamp = ulong.MaxValue;
        maximum.RtpTimestamp = uint.MaxValue;
        maximum.PacketCount = uint.MaxValue;
        maximum.OctetCount = uint.MaxValue;

        byte[] zeroBytes = new byte[20];
        byte[] maximumBytes = new byte[20];
        RtcpReportBlockWireCodec.EncodeSenderInformation(zero, zeroBytes);
        RtcpReportBlockWireCodec.EncodeSenderInformation(maximum, maximumBytes);
        RtcpSenderInformation decodedZero =
            RtcpReportBlockWireCodec.DecodeSenderInformation(zeroBytes);
        RtcpSenderInformation decodedMaximum =
            RtcpReportBlockWireCodec.DecodeSenderInformation(maximumBytes);

        Assert.Multiple(() =>
        {
            Assert.That(zeroBytes, Is.All.Zero);
            Assert.That(maximumBytes, Has.All.EqualTo(0xFF));
            Assert.That(decodedZero.NtpTimestamp, Is.Zero);
            Assert.That(decodedZero.RtpTimestamp, Is.Zero);
            Assert.That(decodedZero.PacketCount, Is.Zero);
            Assert.That(decodedZero.OctetCount, Is.Zero);
            Assert.That(decodedMaximum.NtpTimestamp, Is.EqualTo(ulong.MaxValue));
            Assert.That(decodedMaximum.RtpTimestamp, Is.EqualTo(uint.MaxValue));
            Assert.That(decodedMaximum.PacketCount, Is.EqualTo(uint.MaxValue));
            Assert.That(decodedMaximum.OctetCount, Is.EqualTo(uint.MaxValue));
        });
    }

    [Test]
    public void ReceptionReportSignedAndUnsignedExtremaHaveGoldenEncodings()
    {
        RtcpReceptionReport minimum = new RtcpReceptionReport();
        minimum.CumulativePacketsLost = RtcpProtocolConstants.CumulativeLossMinimum;
        RtcpReceptionReport maximum = new RtcpReceptionReport();
        maximum.SourceIdentifier = uint.MaxValue;
        maximum.FractionLost = byte.MaxValue;
        maximum.CumulativePacketsLost = RtcpProtocolConstants.CumulativeLossMaximum;
        maximum.ExtendedHighestSequenceNumber = uint.MaxValue;
        maximum.InterarrivalJitter = uint.MaxValue;
        maximum.LastSenderReport = uint.MaxValue;
        maximum.DelaySinceLastSenderReport = uint.MaxValue;
        byte[] minimumBytes = new byte[24];
        byte[] maximumBytes = new byte[24];

        RtcpReportBlockWireCodec.EncodeReceptionReport(minimum, minimumBytes);
        RtcpReportBlockWireCodec.EncodeReceptionReport(maximum, maximumBytes);
        RtcpReceptionReport decodedMinimum =
            RtcpReportBlockWireCodec.DecodeReceptionReport(minimumBytes);
        RtcpReceptionReport decodedMaximum =
            RtcpReportBlockWireCodec.DecodeReceptionReport(maximumBytes);

        Assert.Multiple(() =>
        {
            Assert.That(minimumBytes,
                Is.EqualTo(new byte[]
                {
                    0, 0, 0, 0,
                    0, 0x80, 0, 0,
                    0, 0, 0, 0,
                    0, 0, 0, 0,
                    0, 0, 0, 0,
                    0, 0, 0, 0
                }));
            Assert.That(maximumBytes[0..5], Has.All.EqualTo(0xFF));
            Assert.That(maximumBytes[5..8],
                Is.EqualTo(new byte[] { 0x7F, 0xFF, 0xFF }));
            Assert.That(maximumBytes[8..], Has.All.EqualTo(0xFF));
        });
        AssertReportEqual(minimum, decodedMinimum, "minimum report");
        AssertReportEqual(maximum, decodedMaximum, "maximum report");
    }

    [Test]
    public void ReceptionReportRejectsBothValuesOutsideSigned24RangeBeforeWriting()
    {
        int[] invalidLosses = new int[]
        {
            RtcpProtocolConstants.CumulativeLossMinimum - 1,
            RtcpProtocolConstants.CumulativeLossMaximum + 1
        };
        int lossIndex = 0;
        while (lossIndex < invalidLosses.Length)
        {
            RtcpReceptionReport report = new RtcpReceptionReport();
            report.CumulativePacketsLost = invalidLosses[lossIndex];
            byte[] destination = new byte[24];
            Array.Fill(destination, (byte)0xA5);

            Assert.Throws<RtcpPacketFormatException>(() =>
                RtcpReportBlockWireCodec.EncodeReceptionReport(report, destination));
            Assert.That(destination, Has.All.EqualTo(0xA5));
            lossIndex = lossIndex + 1;
        }
    }

    [Test]
    public void FixedReportStructuresRejectEveryIncorrectLength()
    {
        int length = 0;
        while (length <= 25)
        {
            if (length != 24)
            {
                Assert.Throws<RtcpPacketFormatException>(() =>
                    RtcpReportBlockWireCodec.DecodeReceptionReport(new byte[length]));
            }

            length = length + 1;
        }

        length = 0;
        while (length <= 21)
        {
            if (length != 20)
            {
                Assert.Throws<RtcpPacketFormatException>(() =>
                    RtcpReportBlockWireCodec.DecodeSenderInformation(new byte[length]));
            }

            length = length + 1;
        }
    }

    [Test]
    public void FixedReportEncodersRejectEveryShortDestinationWithoutWriting()
    {
        RtcpReceptionReport report = new RtcpReceptionReport();
        RtcpSenderInformation information = new RtcpSenderInformation();
        int length = 0;
        while (length < 24)
        {
            byte[] destination = new byte[length];
            Array.Fill(destination, (byte)0xA5);
            Assert.Throws<RtpBufferCapacityException>(() =>
                RtcpReportBlockWireCodec.EncodeReceptionReport(report, destination));
            Assert.That(destination, Has.All.EqualTo(0xA5));
            length = length + 1;
        }

        length = 0;
        while (length < 20)
        {
            byte[] destination = new byte[length];
            Array.Fill(destination, (byte)0xA5);
            Assert.Throws<RtpBufferCapacityException>(() =>
                RtcpReportBlockWireCodec.EncodeSenderInformation(information, destination));
            Assert.That(destination, Has.All.EqualTo(0xA5));
            length = length + 1;
        }
    }

    [Test]
    public void ReceptionReportArraysSupportZeroAnd31BlocksExactly()
    {
        RtcpReceptionReport[] empty = [];
        RtcpReceptionReport[] maximum = CreateReports(31);
        byte[] maximumBytes = new byte[31 * 24];

        int emptyWritten = RtcpReportBlockWireCodec.EncodeReceptionReports(empty, []);
        RtcpReceptionReport[] decodedEmpty =
            RtcpReportBlockWireCodec.DecodeReceptionReports([], 0);
        int maximumWritten = RtcpReportBlockWireCodec.EncodeReceptionReports(
            maximum,
            maximumBytes);
        RtcpReceptionReport[] decodedMaximum =
            RtcpReportBlockWireCodec.DecodeReceptionReports(maximumBytes, 31);

        Assert.Multiple(() =>
        {
            Assert.That(emptyWritten, Is.Zero);
            Assert.That(decodedEmpty, Is.Empty);
            Assert.That(maximumWritten, Is.EqualTo(744));
            Assert.That(decodedMaximum, Has.Length.EqualTo(31));
        });
        AssertReportEqual(maximum[0], decodedMaximum[0], "first report");
        AssertReportEqual(maximum[30], decodedMaximum[30], "last report");
    }

    [Test]
    public void EveryDeclaredNonzeroCountFailsAtTheMinimumReportLength()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        int count = 1;
        while (count <= 31)
        {
            byte[] sender = new byte[28];
            sender[0] = (byte)(0x80 | count);
            sender[1] = 200;
            sender[3] = 6;
            byte[] receiver = new byte[8];
            receiver[0] = (byte)(0x80 | count);
            receiver[1] = 201;
            receiver[3] = 1;

            RtcpCodecResult senderResult = codec.TryDecodeRtcpPacket(sender);
            RtcpCodecResult receiverResult = codec.TryDecodeRtcpPacket(receiver);

            Assert.Multiple(() =>
            {
                Assert.That(senderResult.Success, Is.False);
                Assert.That(senderResult.ErrorMessage, Does.Contain("report count"));
                Assert.That(receiverResult.Success, Is.False);
                Assert.That(receiverResult.ErrorMessage, Does.Contain("report count"));
            });
            count = count + 1;
        }
    }

    [Test]
    public void EveryTruncationOfOneBlockReportsIsRejected()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        RtcpPacketType[] types = new RtcpPacketType[]
        {
            RtcpPacketType.SenderReport,
            RtcpPacketType.ReceiverReport
        };
        int typeIndex = 0;
        while (typeIndex < types.Length)
        {
            byte[] valid = codec.EncodeRtcpPacketToArray(
                CreateReportPacket(types[typeIndex], 1));
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

            typeIndex = typeIndex + 1;
        }
    }

    [Test]
    public void DeclaredLengthsShorterAndLongerThanAReportAreRejected()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        RtcpPacketType[] types = new RtcpPacketType[]
        {
            RtcpPacketType.SenderReport,
            RtcpPacketType.ReceiverReport
        };
        int typeIndex = 0;
        while (typeIndex < types.Length)
        {
            byte[] valid = codec.EncodeRtcpPacketToArray(
                CreateReportPacket(types[typeIndex], 1));
            int validWordLength = valid.Length / 4 - 1;
            byte[] shorter = (byte[])valid.Clone();
            shorter[2] = (byte)((validWordLength - 1) >> 8);
            shorter[3] = (byte)(validWordLength - 1);
            byte[] longer = (byte[])valid.Clone();
            longer[2] = (byte)((validWordLength + 1) >> 8);
            longer[3] = (byte)(validWordLength + 1);

            RtcpCodecResult shorterResult = codec.TryDecodeRtcpPacket(shorter);
            RtcpCodecResult longerResult = codec.TryDecodeRtcpPacket(longer);

            Assert.Multiple(() =>
            {
                Assert.That(shorterResult.Success, Is.False);
                Assert.That(shorterResult.ErrorMessage, Does.Contain("report count"));
                Assert.That(longerResult.Success, Is.False);
                Assert.That(longerResult.ErrorMessage, Does.Contain("truncated"));
            });
            typeIndex = typeIndex + 1;
        }
    }

    [Test]
    public void AlignedProfileExtensionsRoundTripForBothReportTypes()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        RtcpPacketType[] types = new RtcpPacketType[]
        {
            RtcpPacketType.SenderReport,
            RtcpPacketType.ReceiverReport
        };
        int[] extensionLengths = new int[] { 0, 4, 8 };
        int typeIndex = 0;
        while (typeIndex < types.Length)
        {
            int lengthIndex = 0;
            while (lengthIndex < extensionLengths.Length)
            {
                byte[] extension = CreateBytes(extensionLengths[lengthIndex], 0x41);
                RtcpPacket packet = CreateReportPacket(types[typeIndex], 1);
                SetProfileExtension(packet, extension);

                byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
                RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);

                Assert.Multiple(() =>
                {
                    Assert.That(GetProfileExtension(decoded), Is.EqualTo(extension));
                    Assert.That(decoded.Header.LengthInWordsMinusOne,
                        Is.EqualTo(encoded.Length / 4 - 1));
                    Assert.That(codec.EncodeRtcpPacketToArray(decoded),
                        Is.EqualTo(encoded));
                });
                lengthIndex = lengthIndex + 1;
            }

            typeIndex = typeIndex + 1;
        }
    }

    [Test]
    public void EveryMisalignedProfileExtensionLengthIsRejectedBeforeWriting()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        RtcpPacketType[] types = new RtcpPacketType[]
        {
            RtcpPacketType.SenderReport,
            RtcpPacketType.ReceiverReport
        };
        int typeIndex = 0;
        while (typeIndex < types.Length)
        {
            int length = 1;
            while (length <= 7)
            {
                if ((length & 3) != 0)
                {
                    RtcpPacket packet = CreateReportPacket(types[typeIndex], 0);
                    SetProfileExtension(packet, new byte[length]);
                    byte[] destination = new byte[64];
                    Array.Fill(destination, (byte)0xA5);

                    RtcpCodecResult result = codec.TryEncodeRtcpPacket(
                        packet,
                        destination);
                    Assert.Multiple(() =>
                    {
                        Assert.That(result.Success, Is.False);
                        Assert.That(result.WrittenCount, Is.Zero);
                        Assert.That(result.ErrorMessage, Does.Contain("word-aligned"));
                        Assert.That(destination, Has.All.EqualTo(0xA5));
                    });
                }

                length = length + 1;
            }

            typeIndex = typeIndex + 1;
        }
    }

    [Test]
    public void MinimumAndMaximumPracticalPaddingRoundTripForBothReportTypes()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        RtcpPacketType[] types = new RtcpPacketType[]
        {
            RtcpPacketType.SenderReport,
            RtcpPacketType.ReceiverReport
        };
        int[] paddingLengths = new int[] { 4, 252 };
        int typeIndex = 0;
        while (typeIndex < types.Length)
        {
            int lengthIndex = 0;
            while (lengthIndex < paddingLengths.Length)
            {
                int paddingLength = paddingLengths[lengthIndex];
                RtcpPacket packet = CreateReportPacket(types[typeIndex], 0);
                packet.Header.HasPadding = true;
                packet.Padding = CreateBytes(paddingLength, 0x51);
                packet.Padding[packet.Padding.Length - 1] = (byte)paddingLength;

                byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
                RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);

                Assert.Multiple(() =>
                {
                    Assert.That((encoded[0] & 0x20), Is.EqualTo(0x20));
                    Assert.That(encoded[^1], Is.EqualTo(paddingLength));
                    Assert.That(decoded.Header.HasPadding, Is.True);
                    Assert.That(decoded.Padding, Is.EqualTo(packet.Padding));
                    Assert.That(GetProfileExtension(decoded), Is.Empty);
                    Assert.That(codec.EncodeRtcpPacketToArray(decoded),
                        Is.EqualTo(encoded));
                });
                lengthIndex = lengthIndex + 1;
            }

            typeIndex = typeIndex + 1;
        }
    }

    [Test]
    public void MalformedPaddingCountsAreRejectedForBothReportTypes()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        byte[] packetTypes = new byte[] { 200, 201 };
        int typeIndex = 0;
        while (typeIndex < packetTypes.Length)
        {
            int minimumSize = 8;
            int minimumWordLength = 1;
            if (packetTypes[typeIndex] == 200)
            {
                minimumSize = 28;
                minimumWordLength = 6;
            }

            byte[] zeroPadding = new byte[minimumSize];
            zeroPadding[0] = 0xA0;
            zeroPadding[1] = packetTypes[typeIndex];
            zeroPadding[2] = (byte)(minimumWordLength >> 8);
            zeroPadding[3] = (byte)minimumWordLength;
            byte[] excessivePadding = (byte[])zeroPadding.Clone();
            excessivePadding[^1] = (byte)(minimumSize - 3);

            Assert.Multiple(() =>
            {
                Assert.That(codec.TryDecodeRtcpPacket(zeroPadding).Success, Is.False);
                Assert.That(codec.TryDecodeRtcpPacket(excessivePadding).Success,
                    Is.False);
            });

            int paddingCount = 1;
            while (paddingCount <= 3)
            {
                byte[] misaligned = new byte[minimumSize + 4];
                misaligned[0] = 0xA0;
                misaligned[1] = packetTypes[typeIndex];
                int lengthInWordsMinusOne = misaligned.Length / 4 - 1;
                misaligned[2] = (byte)(lengthInWordsMinusOne >> 8);
                misaligned[3] = (byte)lengthInWordsMinusOne;
                misaligned[^1] = (byte)paddingCount;

                RtcpCodecResult result = codec.TryDecodeRtcpPacket(misaligned);
                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.False);
                    Assert.That(result.ErrorMessage, Does.Contain("word-aligned"));
                });
                paddingCount = paddingCount + 1;
            }

            typeIndex = typeIndex + 1;
        }
    }

    [Test]
    public void InvalidReportModelsAndCountsLeaveDestinationUntouched()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        RtcpPacket sender = CreateReportPacket(RtcpPacketType.SenderReport, 0);
        sender.SenderReport!.SenderInformation = null!;
        RtcpPacket receiver = CreateReportPacket(RtcpPacketType.ReceiverReport, 0);
        receiver.ReceiverReport!.ReceptionReports = new RtcpReceptionReport[32];
        byte[] destination = new byte[1024];
        Array.Fill(destination, (byte)0xA5);

        RtcpCodecResult senderResult = codec.TryEncodeRtcpPacket(sender, destination);
        RtcpCodecResult receiverResult = codec.TryEncodeRtcpPacket(receiver, destination);

        Assert.Multiple(() =>
        {
            Assert.That(senderResult.Success, Is.False);
            Assert.That(receiverResult.Success, Is.False);
            Assert.That(destination, Has.All.EqualTo(0xA5));
            Assert.Throws<RtcpPacketFormatException>(() =>
                RtcpReportBlockWireCodec.GetReceptionReportsEncodedSize(null!));
            Assert.Throws<RtcpPacketFormatException>(() =>
                RtcpReportBlockWireCodec.DecodeReceptionReports([], -1));
            Assert.Throws<RtcpPacketFormatException>(() =>
                RtcpReportBlockWireCodec.DecodeReceptionReports(new byte[744], 32));
        });
    }

    static RtcpPacket CreateReportPacket(RtcpPacketType type, int reportCount)
    {
        RtcpPacket packet = new RtcpPacket();
        packet.Type = type;
        RtcpReceptionReport[] reports = CreateReports(reportCount);
        if (type == RtcpPacketType.SenderReport)
        {
            RtcpSenderReport sender = new RtcpSenderReport();
            sender.SenderSynchronizationSource = 0x10203040U;
            sender.SenderInformation.NtpTimestamp = 0x0102030405060708UL;
            sender.SenderInformation.RtpTimestamp = 0x11223344U;
            sender.SenderInformation.PacketCount = 0x55667788U;
            sender.SenderInformation.OctetCount = 0x99AABBCCU;
            sender.ReceptionReports = reports;
            packet.SenderReport = sender;
        }
        else
        {
            RtcpReceiverReport receiver = new RtcpReceiverReport();
            receiver.SenderSynchronizationSource = 0x10203040U;
            receiver.ReceptionReports = reports;
            packet.ReceiverReport = receiver;
        }

        return packet;
    }

    static RtcpReceptionReport[] CreateReports(int count)
    {
        RtcpReceptionReport[] reports = new RtcpReceptionReport[count];
        int reportIndex = 0;
        while (reportIndex < reports.Length)
        {
            RtcpReceptionReport report = new RtcpReceptionReport();
            report.SourceIdentifier = (uint)(0x10000000 + reportIndex);
            report.FractionLost = (byte)(reportIndex * 7);
            report.CumulativePacketsLost = reportIndex - 15;
            report.ExtendedHighestSequenceNumber = (uint)(0x20000000 + reportIndex);
            report.InterarrivalJitter = (uint)(0x30000000 + reportIndex);
            report.LastSenderReport = (uint)(0x40000000 + reportIndex);
            report.DelaySinceLastSenderReport = (uint)(0x50000000 + reportIndex);
            reports[reportIndex] = report;
            reportIndex = reportIndex + 1;
        }

        return reports;
    }

    static RtcpReceptionReport[] GetReports(RtcpPacket packet)
    {
        if (packet.Type == RtcpPacketType.SenderReport)
        {
            return packet.SenderReport!.ReceptionReports;
        }

        return packet.ReceiverReport!.ReceptionReports;
    }

    static byte[] GetProfileExtension(RtcpPacket packet)
    {
        if (packet.Type == RtcpPacketType.SenderReport)
        {
            return packet.SenderReport!.ProfileExtension;
        }

        return packet.ReceiverReport!.ProfileExtension;
    }

    static void SetProfileExtension(RtcpPacket packet, byte[] profileExtension)
    {
        if (packet.Type == RtcpPacketType.SenderReport)
        {
            packet.SenderReport!.ProfileExtension = profileExtension;
        }
        else
        {
            packet.ReceiverReport!.ProfileExtension = profileExtension;
        }
    }

    static byte[] CreateBytes(int length, int seed)
    {
        byte[] bytes = new byte[length];
        int byteIndex = 0;
        while (byteIndex < bytes.Length)
        {
            bytes[byteIndex] = (byte)(seed + byteIndex);
            byteIndex = byteIndex + 1;
        }

        return bytes;
    }

    static void AssertReportEqual(
        RtcpReceptionReport expected,
        RtcpReceptionReport actual,
        string context)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.SourceIdentifier,
                Is.EqualTo(expected.SourceIdentifier), context);
            Assert.That(actual.FractionLost,
                Is.EqualTo(expected.FractionLost), context);
            Assert.That(actual.CumulativePacketsLost,
                Is.EqualTo(expected.CumulativePacketsLost), context);
            Assert.That(actual.ExtendedHighestSequenceNumber,
                Is.EqualTo(expected.ExtendedHighestSequenceNumber), context);
            Assert.That(actual.InterarrivalJitter,
                Is.EqualTo(expected.InterarrivalJitter), context);
            Assert.That(actual.LastSenderReport,
                Is.EqualTo(expected.LastSenderReport), context);
            Assert.That(actual.DelaySinceLastSenderReport,
                Is.EqualTo(expected.DelaySinceLastSenderReport), context);
        });
    }

    static IRtcpPacketCodec CreateCodec(bool preservePadding)
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.StrictValidation = true;
        configuration.PreservePadding = preservePadding;
        configuration.MaximumRtcpCompoundPacketSize = 65535;
        return new RtcpPacketCodec(configuration);
    }
}
