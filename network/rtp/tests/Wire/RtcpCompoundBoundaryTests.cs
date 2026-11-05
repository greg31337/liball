using rtp;

namespace rtp.Tests.Wire;

public class RtcpCompoundBoundaryTests
{
    [Test]
    public void FullModeRoundTripsLegalReceiverAndSenderReportForms()
    {
        IRtcpPacketCodec codec = CreateCodec(
            RtcpCompoundMode.Full,
            true,
            true,
            65535,
            32);
        RtcpCompoundPacket[] compounds = new RtcpCompoundPacket[]
        {
            CreateCompound(new RtcpPacket[]
            {
                CreateReceiverReport(1U),
                CreateSourceDescription(1U, true)
            }),
            CreateCompound(new RtcpPacket[]
            {
                CreateSenderReport(2U),
                CreateReceiverReport(3U),
                CreateSenderReport(4U),
                CreateSourceDescription(2U, true),
                CreateSourceDescription(3U, false),
                CreateApplicationPacket(),
                CreateUnknownPacket(222, 17, new byte[] { 9, 8, 7, 6 }),
                CreateGoodbyePacket(2U),
                CreateGoodbyePacket(3U)
            })
        };
        int compoundIndex = 0;
        while (compoundIndex < compounds.Length)
        {
            RtcpCompoundPacket compound = compounds[compoundIndex];

            byte[] encoded = codec.EncodeRtcpCompoundPacketToArray(compound);
            RtcpCompoundPacket decoded = codec.DecodeRtcpCompoundPacket(encoded);

            Assert.Multiple(() =>
            {
                Assert.That(decoded.Packets.Length,
                    Is.EqualTo(compound.Packets.Length));
                Assert.That(codec.GetRtcpCompoundPacketEncodedSize(compound),
                    Is.EqualTo(encoded.Length));
                Assert.That(codec.EncodeRtcpCompoundPacketToArray(decoded),
                    Is.EqualTo(encoded));
            });
            compoundIndex = compoundIndex + 1;
        }
    }

    [Test]
    public void MinimalModeAcceptsEitherReportAndOneOrMoreFeedbackPackets()
    {
        IRtcpPacketCodec codec = CreateCodec(
            RtcpCompoundMode.MinimalFeedback,
            true,
            true,
            65535,
            32);
        RtcpCompoundPacket[] compounds = new RtcpCompoundPacket[]
        {
            CreateCompound(new RtcpPacket[]
            {
                CreateReceiverReport(7U),
                CreateSourceDescription(7U, true),
                CreatePictureLossIndication()
            }),
            CreateCompound(new RtcpPacket[]
            {
                CreateSenderReport(8U),
                CreateSourceDescription(8U, true),
                CreatePictureLossIndication(),
                CreatePictureLossIndication(),
                CreatePictureLossIndication()
            })
        };
        int compoundIndex = 0;
        while (compoundIndex < compounds.Length)
        {
            byte[] encoded = codec.EncodeRtcpCompoundPacketToArray(
                compounds[compoundIndex]);
            RtcpCompoundPacket decoded = codec.DecodeRtcpCompoundPacket(encoded);

            Assert.Multiple(() =>
            {
                Assert.That(decoded.Packets.Length,
                    Is.EqualTo(compounds[compoundIndex].Packets.Length));
                Assert.That(decoded.Packets[1].Type,
                    Is.EqualTo(RtcpPacketType.SourceDescription));
                Assert.That(decoded.Packets[2].Feedback, Is.Not.Null);
                Assert.That(codec.EncodeRtcpCompoundPacketToArray(decoded),
                    Is.EqualTo(encoded));
            });
            compoundIndex = compoundIndex + 1;
        }
    }

    [Test]
    public void ReducedSizeModeAcceptsEveryStandalonePacketCategory()
    {
        IRtcpPacketCodec codec = CreateCodec(
            RtcpCompoundMode.ReducedSize,
            true,
            true,
            65535,
            32);
        RtcpPacket[] packets = new RtcpPacket[]
        {
            CreateSenderReport(1U),
            CreateReceiverReport(1U),
            CreateSourceDescription(1U, false),
            CreateGoodbyePacket(1U),
            CreateApplicationPacket(),
            CreatePictureLossIndication(),
            CreateUnknownPacket(222, 31, new byte[] { 1, 2, 3, 4 })
        };
        int packetIndex = 0;
        while (packetIndex < packets.Length)
        {
            RtcpCompoundPacket compound = CreateCompound(new RtcpPacket[]
            {
                packets[packetIndex]
            });

            byte[] encoded = codec.EncodeRtcpCompoundPacketToArray(compound);
            RtcpCompoundPacket decoded = codec.DecodeRtcpCompoundPacket(encoded);

            Assert.Multiple(() =>
            {
                Assert.That(decoded.Packets, Has.Length.EqualTo(1));
                Assert.That(decoded.Packets[0].Type,
                    Is.EqualTo(packets[packetIndex].Type));
                Assert.That(codec.EncodeRtcpCompoundPacketToArray(decoded),
                    Is.EqualTo(encoded));
            });
            packetIndex = packetIndex + 1;
        }

        RtcpCompoundPacket sequence = CreateCompound(new RtcpPacket[]
        {
            CreateUnknownPacket(222, 1, new byte[] { 1, 2, 3, 4 }),
            CreateApplicationPacket(),
            CreateGoodbyePacket(1U)
        });
        Assert.DoesNotThrow(() => codec.EncodeRtcpCompoundPacketToArray(sequence));
    }

    [Test]
    public void FullModeEnforcesEveryOrderingPhaseOnEncodeAndDecode()
    {
        IRtcpPacketCodec strictCodec = CreateCodec(
            RtcpCompoundMode.Full,
            true,
            true,
            65535,
            32);
        IRtcpPacketCodec tolerantCodec = CreateCodec(
            RtcpCompoundMode.Full,
            false,
            true,
            65535,
            32);
        RtcpCompoundPacket[] invalidCompounds = new RtcpCompoundPacket[]
        {
            CreateCompound(new RtcpPacket[]
            {
                CreateApplicationPacket(),
                CreateSourceDescription(1U, true)
            }),
            CreateCompound(new RtcpPacket[]
            {
                CreateReceiverReport(1U),
                CreateApplicationPacket(),
                CreateSourceDescription(1U, true)
            }),
            CreateCompound(new RtcpPacket[]
            {
                CreateReceiverReport(1U),
                CreateSourceDescription(1U, true),
                CreateSenderReport(1U)
            }),
            CreateCompound(new RtcpPacket[]
            {
                CreateReceiverReport(1U),
                CreateSourceDescription(1U, true),
                CreateGoodbyePacket(1U),
                CreateApplicationPacket()
            }),
            CreateCompound(new RtcpPacket[]
            {
                CreateReceiverReport(1U),
                CreateSourceDescription(1U, true),
                CreateGoodbyePacket(1U),
                CreateSourceDescription(1U, true)
            })
        };
        int compoundIndex = 0;
        while (compoundIndex < invalidCompounds.Length)
        {
            RtcpCompoundPacket invalid = invalidCompounds[compoundIndex];
            byte[] wire = tolerantCodec.EncodeRtcpCompoundPacketToArray(invalid);
            byte[] destination = CreateFilledBytes(256, 0xA5);

            RtcpCodecResult encodeResult = strictCodec.TryEncodeRtcpCompoundPacket(
                invalid,
                destination);
            RtcpCodecResult decodeResult = strictCodec.TryDecodeRtcpCompoundPacket(
                wire);

            Assert.Multiple(() =>
            {
                Assert.That(encodeResult.Success, Is.False);
                Assert.That(encodeResult.WrittenCount, Is.Zero);
                Assert.That(destination, Has.All.EqualTo(0xA5));
                Assert.That(decodeResult.Success, Is.False);
                Assert.That(decodeResult.ConsumedCount, Is.Zero);
                Assert.That(decodeResult.CompoundPacket, Is.Null);
            });
            compoundIndex = compoundIndex + 1;
        }
    }

    [Test]
    public void FullModeRequiresCnameButFindsItInAnySdesChunk()
    {
        IRtcpPacketCodec strictCodec = CreateCodec(
            RtcpCompoundMode.Full,
            true,
            true,
            65535,
            32);
        IRtcpPacketCodec tolerantCodec = CreateCodec(
            RtcpCompoundMode.Full,
            false,
            true,
            65535,
            32);
        RtcpCompoundPacket[] missingCname = new RtcpCompoundPacket[]
        {
            CreateCompound(new RtcpPacket[]
            {
                CreateReceiverReport(1U),
                CreateApplicationPacket()
            }),
            CreateCompound(new RtcpPacket[]
            {
                CreateReceiverReport(1U),
                CreateSourceDescription(1U, false)
            })
        };
        RtcpPacket description = CreateSourceDescription(9U, false);
        RtcpSourceDescriptionChunk cnameChunk = CreateSdesChunk(1U, true);
        RtcpSourceDescriptionChunk firstChunk =
            description.SourceDescription!.Chunks[0];
        description.SourceDescription.Chunks = new RtcpSourceDescriptionChunk[]
        {
            firstChunk,
            cnameChunk
        };
        RtcpCompoundPacket cnameInSecondChunk = CreateCompound(new RtcpPacket[]
        {
            CreateReceiverReport(1U),
            description
        });

        int invalidIndex = 0;
        while (invalidIndex < missingCname.Length)
        {
            byte[] wire = tolerantCodec.EncodeRtcpCompoundPacketToArray(
                missingCname[invalidIndex]);

            Assert.Multiple(() =>
            {
                Assert.That(strictCodec.TryEncodeRtcpCompoundPacket(
                    missingCname[invalidIndex],
                    new byte[128]).Success, Is.False);
                Assert.That(strictCodec.TryDecodeRtcpCompoundPacket(wire).Success,
                    Is.False);
            });
            invalidIndex = invalidIndex + 1;
        }

        byte[] validWire = strictCodec.EncodeRtcpCompoundPacketToArray(
            cnameInSecondChunk);
        Assert.That(strictCodec.DecodeRtcpCompoundPacket(validWire)
            .Packets[1].SourceDescription?.Chunks, Has.Length.EqualTo(2));
    }

    [Test]
    public void FinalGoodbyePaddingIsPreservedOrNormalizedAcrossACompound()
    {
        IRtcpPacketCodec preservingCodec = CreateCodec(
            RtcpCompoundMode.Full,
            true,
            true,
            65535,
            32);
        IRtcpPacketCodec normalizingCodec = CreateCodec(
            RtcpCompoundMode.Full,
            true,
            false,
            65535,
            32);
        RtcpPacket goodbye = CreateGoodbyePacket(1U);
        goodbye.Header.HasPadding = true;
        goodbye.Padding = new byte[] { 9, 8, 7, 6, 5, 4, 3, 8 };
        RtcpCompoundPacket compound = CreateCompound(new RtcpPacket[]
        {
            CreateReceiverReport(1U),
            CreateSourceDescription(1U, true),
            CreateGoodbyePacket(2U),
            goodbye
        });

        byte[] source = preservingCodec.EncodeRtcpCompoundPacketToArray(compound);
        RtcpCompoundPacket preserved = preservingCodec.DecodeRtcpCompoundPacket(source);
        RtcpCompoundPacket normalized = normalizingCodec.DecodeRtcpCompoundPacket(source);
        byte[] normalizedWire = normalizingCodec.EncodeRtcpCompoundPacketToArray(
            preserved);

        Assert.Multiple(() =>
        {
            Assert.That(preserved.Packets[^1].Padding,
                Is.EqualTo(new byte[] { 9, 8, 7, 6, 5, 4, 3, 8 }));
            Assert.That(normalized.Packets[^1].Padding[..^1], Is.All.Zero);
            Assert.That(normalized.Packets[^1].Padding[^1], Is.EqualTo(8));
            Assert.That(normalizedWire[^8..^1], Is.All.Zero);
            Assert.That(normalizedWire[^1], Is.EqualTo(8));
            Assert.That(preserved.Packets[^2].Type,
                Is.EqualTo(RtcpPacketType.Goodbye));
        });
    }

    [Test]
    public void EveryModeRejectsPaddingBeforeTheFinalPacketOnDecode()
    {
        RtcpCompoundMode[] modes = new RtcpCompoundMode[]
        {
            RtcpCompoundMode.Full,
            RtcpCompoundMode.MinimalFeedback,
            RtcpCompoundMode.ReducedSize
        };
        int modeIndex = 0;
        while (modeIndex < modes.Length)
        {
            RtcpPacket firstPacket = CreateReceiverReport(7U);
            firstPacket.Header.HasPadding = true;
            firstPacket.Padding = new byte[] { 0, 0, 0, 4 };
            RtcpCompoundPacket compound;
            if (modes[modeIndex] == RtcpCompoundMode.MinimalFeedback)
            {
                compound = CreateCompound(new RtcpPacket[]
                {
                    firstPacket,
                    CreateSourceDescription(7U, true),
                    CreatePictureLossIndication()
                });
            }
            else if (modes[modeIndex] == RtcpCompoundMode.Full)
            {
                compound = CreateCompound(new RtcpPacket[]
                {
                    firstPacket,
                    CreateSourceDescription(7U, true)
                });
            }
            else
            {
                compound = CreateCompound(new RtcpPacket[]
                {
                    firstPacket,
                    CreateApplicationPacket()
                });
            }

            IRtcpPacketCodec tolerantCodec = CreateCodec(
                modes[modeIndex],
                false,
                true,
                65535,
                32);
            IRtcpPacketCodec strictCodec = CreateCodec(
                modes[modeIndex],
                true,
                true,
                65535,
                32);
            byte[] wire = tolerantCodec.EncodeRtcpCompoundPacketToArray(compound);

            Assert.That(strictCodec.TryDecodeRtcpCompoundPacket(wire).Success,
                Is.False);
            modeIndex = modeIndex + 1;
        }
    }

    [Test]
    public void AggregateSizeAndCountLimitsAcceptExactBoundariesOnly()
    {
        RtcpCompoundPacket compound = CreateCompound(new RtcpPacket[]
        {
            CreateUnknownPacket(220, 0, new byte[] { 1, 2, 3, 4 }),
            CreateUnknownPacket(221, 1, new byte[] { 5, 6, 7, 8 }),
            CreateUnknownPacket(222, 2, new byte[] { 9, 10, 11, 12 }),
            CreateUnknownPacket(223, 3, new byte[] { 13, 14, 15, 16 })
        });
        IRtcpPacketCodec exactCodec = CreateCodec(
            RtcpCompoundMode.ReducedSize,
            true,
            true,
            32,
            4);
        byte[] source = exactCodec.EncodeRtcpCompoundPacketToArray(compound);
        IRtcpPacketCodec sizeLimitedCodec = CreateCodec(
            RtcpCompoundMode.ReducedSize,
            true,
            true,
            31,
            4);
        IRtcpPacketCodec countLimitedCodec = CreateCodec(
            RtcpCompoundMode.ReducedSize,
            true,
            true,
            32,
            3);
        byte[] sizeDestination = CreateFilledBytes(32, 0xA5);
        byte[] countDestination = CreateFilledBytes(32, 0xA5);

        RtcpCodecResult exactDecode = exactCodec.TryDecodeRtcpCompoundPacket(source);
        RtcpCodecResult sizeDecode =
            sizeLimitedCodec.TryDecodeRtcpCompoundPacket(source);
        RtcpCodecResult countDecode =
            countLimitedCodec.TryDecodeRtcpCompoundPacket(source);
        RtcpCodecResult sizeEncode = sizeLimitedCodec.TryEncodeRtcpCompoundPacket(
            compound,
            sizeDestination);
        RtcpCodecResult countEncode = countLimitedCodec.TryEncodeRtcpCompoundPacket(
            compound,
            countDestination);

        Assert.Multiple(() =>
        {
            Assert.That(source, Has.Length.EqualTo(32));
            Assert.That(exactDecode.Success, Is.True);
            Assert.That(exactDecode.ConsumedCount, Is.EqualTo(32));
            Assert.That(exactDecode.CompoundPacket?.Packets, Has.Length.EqualTo(4));
            Assert.That(sizeDecode.Success, Is.False);
            Assert.That(sizeDecode.ErrorMessage, Does.Contain("size limit"));
            Assert.That(countDecode.Success, Is.False);
            Assert.That(countDecode.ErrorMessage, Does.Contain("packet-count"));
            Assert.That(sizeEncode.Success, Is.False);
            Assert.That(countEncode.Success, Is.False);
            Assert.That(sizeDestination, Has.All.EqualTo(0xA5));
            Assert.That(countDestination, Has.All.EqualTo(0xA5));
        });
    }

    [Test]
    public void CompoundIterationPreservesEveryOpaquePacketAndWireOrder()
    {
        IRtcpPacketCodec codec = CreateCodec(
            RtcpCompoundMode.ReducedSize,
            true,
            true,
            65535,
            32);
        byte[] packetTypes = new byte[] { 192, 199, 208, 223 };
        byte[] counts = new byte[] { 0, 31, 17, 5 };
        byte[] source = new byte[32];
        int packetIndex = 0;
        while (packetIndex < packetTypes.Length)
        {
            int offset = packetIndex * 8;
            source[offset] = (byte)(0x80 | counts[packetIndex]);
            source[offset + 1] = packetTypes[packetIndex];
            source[offset + 2] = 0;
            source[offset + 3] = 1;
            source[offset + 4] = (byte)(packetIndex + 1);
            source[offset + 5] = (byte)(packetIndex + 2);
            source[offset + 6] = (byte)(packetIndex + 3);
            source[offset + 7] = (byte)(packetIndex + 4);
            packetIndex = packetIndex + 1;
        }

        byte[] expected = (byte[])source.Clone();
        RtcpCompoundPacket decoded = codec.DecodeRtcpCompoundPacket(source);
        packetIndex = 0;
        while (packetIndex < decoded.Packets.Length)
        {
            RtcpPacket packet = decoded.Packets[packetIndex];
            Assert.Multiple(() =>
            {
                Assert.That(packet.Type,
                    Is.EqualTo(RtcpPacketWireCodec.GetPacketType(
                        packetTypes[packetIndex])));
                Assert.That(packet.UnknownPacket?.PacketType,
                    Is.EqualTo(packetTypes[packetIndex]));
                Assert.That(packet.UnknownPacket?.Count,
                    Is.EqualTo(counts[packetIndex]));
                Assert.That(packet.UnknownPacket?.Data,
                    Is.EqualTo(expected[(packetIndex * 8 + 4)..(packetIndex * 8 + 8)]));
            });
            packetIndex = packetIndex + 1;
        }

        Array.Fill(source, (byte)0);
        Assert.That(codec.EncodeRtcpCompoundPacketToArray(decoded),
            Is.EqualTo(expected));
    }

    [Test]
    public void EveryByteTruncationOfAMinimalCompoundIsRejectedAtomically()
    {
        IRtcpPacketCodec codec = CreateCodec(
            RtcpCompoundMode.MinimalFeedback,
            true,
            true,
            65535,
            32);
        RtcpCompoundPacket compound = CreateCompound(new RtcpPacket[]
        {
            CreateReceiverReport(7U),
            CreateSourceDescription(7U, true),
            CreatePictureLossIndication()
        });
        byte[] source = codec.EncodeRtcpCompoundPacketToArray(compound);
        byte[] expected = (byte[])source.Clone();
        int length = 0;
        while (length < source.Length)
        {
            byte[] truncated = source[..length];

            RtcpCodecResult result = codec.TryDecodeRtcpCompoundPacket(truncated);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.ConsumedCount, Is.Zero);
                Assert.That(result.CompoundPacket, Is.Null);
                Assert.Throws<RtcpPacketFormatException>(() =>
                    codec.DecodeRtcpCompoundPacket(truncated));
                Assert.That(source, Is.EqualTo(expected));
            });
            length = length + 1;
        }

        RtcpCodecResult complete = codec.TryDecodeRtcpCompoundPacket(source);
        Assert.Multiple(() =>
        {
            Assert.That(complete.Success, Is.True);
            Assert.That(complete.ConsumedCount, Is.EqualTo(source.Length));
            Assert.That(complete.CompoundPacket?.Packets, Has.Length.EqualTo(3));
        });
    }

    static RtcpCompoundPacket CreateCompound(RtcpPacket[] packets)
    {
        RtcpCompoundPacket compound = new RtcpCompoundPacket();
        compound.Packets = packets;
        return compound;
    }

    static RtcpPacket CreateSenderReport(uint sourceIdentifier)
    {
        RtcpSenderReport report = new RtcpSenderReport();
        report.SenderSynchronizationSource = sourceIdentifier;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.SenderReport;
        packet.SenderReport = report;
        return packet;
    }

    static RtcpPacket CreateReceiverReport(uint sourceIdentifier)
    {
        RtcpReceiverReport report = new RtcpReceiverReport();
        report.SenderSynchronizationSource = sourceIdentifier;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.ReceiverReport;
        packet.ReceiverReport = report;
        return packet;
    }

    static RtcpPacket CreateSourceDescription(
        uint sourceIdentifier,
        bool canonicalName)
    {
        RtcpSourceDescription description = new RtcpSourceDescription();
        description.Chunks = new RtcpSourceDescriptionChunk[]
        {
            CreateSdesChunk(sourceIdentifier, canonicalName)
        };
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.SourceDescription;
        packet.SourceDescription = description;
        return packet;
    }

    static RtcpSourceDescriptionChunk CreateSdesChunk(
        uint sourceIdentifier,
        bool canonicalName)
    {
        RtcpSourceDescriptionItem item = new RtcpSourceDescriptionItem();
        if (canonicalName)
        {
            item.Type = RtcpSdesItemType.CanonicalName;
        }
        else
        {
            item.Type = RtcpSdesItemType.Name;
        }

        item.Value = new byte[] { (byte)'x' };
        RtcpSourceDescriptionChunk chunk = new RtcpSourceDescriptionChunk();
        chunk.SourceIdentifier = sourceIdentifier;
        chunk.Items = new RtcpSourceDescriptionItem[] { item };
        return chunk;
    }

    static RtcpPacket CreateGoodbyePacket(uint sourceIdentifier)
    {
        RtcpGoodbye goodbye = new RtcpGoodbye();
        goodbye.Sources = new uint[] { sourceIdentifier };
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.Goodbye;
        packet.Goodbye = goodbye;
        return packet;
    }

    static RtcpPacket CreateApplicationPacket()
    {
        RtcpApplicationDefined application = new RtcpApplicationDefined();
        application.SourceIdentifier = 1U;
        application.Name = new byte[]
        {
            (byte)'T', (byte)'E', (byte)'S', (byte)'T'
        };
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.ApplicationDefined;
        packet.ApplicationDefined = application;
        return packet;
    }

    static RtcpPacket CreatePictureLossIndication()
    {
        RtcpPictureLossIndication indication = new RtcpPictureLossIndication();
        indication.Requested = true;
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        feedback.PayloadFormat = RtcpPayloadFeedbackFormat.PictureLossIndication;
        feedback.SenderSourceIdentifier = 1U;
        feedback.MediaSourceIdentifier = 2U;
        feedback.PictureLossIndication = indication;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.PayloadFeedback;
        packet.Feedback = feedback;
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

    static byte[] CreateFilledBytes(int length, byte value)
    {
        byte[] data = new byte[length];
        Array.Fill(data, value);
        return data;
    }

    static IRtcpPacketCodec CreateCodec(
        RtcpCompoundMode mode,
        bool strictValidation,
        bool preservePadding,
        int maximumPacketSize,
        int maximumPacketCount)
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.RtcpCompoundMode = mode;
        configuration.StrictValidation = strictValidation;
        configuration.PreservePadding = preservePadding;
        configuration.MaximumRtcpCompoundPacketSize = maximumPacketSize;
        configuration.MaximumRtcpCompoundPacketCount = maximumPacketCount;
        return new RtcpPacketCodec(configuration);
    }
}
