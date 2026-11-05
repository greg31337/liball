using rtp;

namespace rtp.Tests.Wire;

public class RandomizedWireModelTests
{
    const int IterationCount = 24;

    [Test]
    public void RandomizedRtpModelsRoundTripAndRejectOneByteTruncation()
    {
        Random random = new Random(10801);
        IRtpPacketCodec codec = new RtpPacketCodec(
            new RtpSessionConfiguration());
        int iteration = 0;
        while (iteration < IterationCount)
        {
            RtpPacket packet = CreateRandomRtpPacket(random, iteration);
            byte[] encoded = codec.EncodeRtpPacketToArray(packet);
            RtpPacket decoded = codec.DecodeRtpPacket(encoded);
            byte[] roundTrip = codec.EncodeRtpPacketToArray(decoded);
            RtpCodecResult truncated = codec.TryDecodeRtpPacket(encoded[..^1]);

            Assert.Multiple(() =>
            {
                Assert.That(roundTrip, Is.EqualTo(encoded),
                    $"RTP iteration {iteration}");
                Assert.That(truncated.Success, Is.False,
                    $"RTP truncation iteration {iteration}");
                Assert.That(truncated.ConsumedCount, Is.Zero);
            });

            iteration = iteration + 1;
        }
    }

    [Test]
    public void RandomizedHeaderExtensionModelsRoundTripAndRejectOneByteTruncation()
    {
        Random random = new Random(10802);
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        int iteration = 0;
        while (iteration < IterationCount)
        {
            int formatIndex = iteration % 2;
            RtpHeaderExtension extension = CreateRandomElementExtension(
                random,
                formatIndex);
            byte[] encoded = new byte[
                codec.GetRtpHeaderExtensionEncodedSize(extension)];
            codec.EncodeRtpHeaderExtension(extension, encoded);
            RtpHeaderExtension decoded = codec.DecodeRtpHeaderExtension(encoded);
            byte[] roundTrip = new byte[
                codec.GetRtpHeaderExtensionEncodedSize(decoded)];
            codec.EncodeRtpHeaderExtension(decoded, roundTrip);
            RtpCodecResult truncated = codec.TryDecodeRtpHeaderExtension(
                encoded[..^1]);

            Assert.Multiple(() =>
            {
                Assert.That(roundTrip, Is.EqualTo(encoded),
                    $"Extension iteration {iteration}");
                Assert.That(truncated.Success, Is.False,
                    $"Extension truncation iteration {iteration}");
                Assert.That(truncated.ConsumedCount, Is.Zero);
            });

            iteration = iteration + 1;
        }
    }

    [Test]
    public void RandomizedCoreRtcpModelsRoundTripAndRejectOneByteTruncation()
    {
        Random random = new Random(10803);
        IRtcpPacketCodec codec = CreateRtcpCodec();
        int iteration = 0;
        while (iteration < IterationCount)
        {
            RtcpPacket[] packets = CreateRandomCoreRtcpPackets(random);
            int packetIndex = 0;
            while (packetIndex < packets.Length)
            {
                AssertRtcpRoundTripAndTruncation(
                    codec,
                    packets[packetIndex],
                    $"Core iteration {iteration}, packet {packetIndex}");
                packetIndex = packetIndex + 1;
            }

            iteration = iteration + 1;
        }
    }

    [Test]
    public void RandomizedFeedbackAndXrModelsRoundTripAndRejectOneByteTruncation()
    {
        Random random = new Random(10804);
        IRtcpPacketCodec codec = CreateRtcpCodec();
        int iteration = 0;
        while (iteration < IterationCount)
        {
            RtcpPacket[] feedbackPackets = CreateRandomFeedbackPackets(random);
            int feedbackIndex = 0;
            while (feedbackIndex < feedbackPackets.Length)
            {
                AssertRtcpRoundTripAndTruncation(
                    codec,
                    feedbackPackets[feedbackIndex],
                    $"Feedback iteration {iteration}, packet {feedbackIndex}");
                feedbackIndex = feedbackIndex + 1;
            }

            AssertRtcpRoundTripAndTruncation(
                codec,
                CreateRandomExtendedReport(random),
                $"XR iteration {iteration}");
            iteration = iteration + 1;
        }
    }

    [Test]
    public void RandomizedCompoundModelsRoundTripAndRejectOneByteTruncation()
    {
        Random random = new Random(10805);
        IRtcpPacketCodec codec = CreateRtcpCodec();
        int iteration = 0;
        while (iteration < IterationCount)
        {
            uint sourceIdentifier = NextUInt32(random);
            RtcpPacket report = CreateRandomReceiverReport(
                random,
                sourceIdentifier);
            RtcpPacket sourceDescription = CreateRandomSourceDescription(
                random,
                sourceIdentifier);
            RtcpPacket application = CreateRandomApplication(random);
            RtcpCompoundPacket compound = new RtcpCompoundPacket();
            compound.Packets = new RtcpPacket[]
            {
                report, sourceDescription, application
            };

            byte[] encoded = codec.EncodeRtcpCompoundPacketToArray(compound);
            RtcpCompoundPacket decoded = codec.DecodeRtcpCompoundPacket(encoded);
            byte[] roundTrip = codec.EncodeRtcpCompoundPacketToArray(decoded);
            RtcpCodecResult truncated = codec.TryDecodeRtcpCompoundPacket(
                encoded[..^1]);

            Assert.Multiple(() =>
            {
                Assert.That(roundTrip, Is.EqualTo(encoded),
                    $"Compound iteration {iteration}");
                Assert.That(decoded.Packets.Length, Is.EqualTo(3));
                Assert.That(truncated.Success, Is.False,
                    $"Compound truncation iteration {iteration}");
                Assert.That(truncated.ConsumedCount, Is.Zero);
            });

            iteration = iteration + 1;
        }
    }

    static RtpPacket CreateRandomRtpPacket(Random random, int iteration)
    {
        RtpPacket packet = new RtpPacket();
        packet.Marker = random.Next(0, 2) != 0;
        packet.PayloadType = (byte)random.Next(0, 128);
        if (packet.Marker && packet.PayloadType >= 64 &&
            packet.PayloadType <= 95)
        {
            packet.Marker = false;
        }

        packet.SequenceNumber = (ushort)random.Next(0, 65536);
        packet.Timestamp = NextUInt32(random);
        packet.SynchronizationSource = NextUInt32(random);
        packet.ContributingSources = new uint[random.Next(0, 16)];
        int sourceIndex = 0;
        while (sourceIndex < packet.ContributingSources.Length)
        {
            packet.ContributingSources[sourceIndex] = NextUInt32(random);
            sourceIndex = sourceIndex + 1;
        }

        packet.HasExtension = true;
        if (iteration % 3 == 2)
        {
            RtpHeaderExtension raw = new RtpHeaderExtension();
            raw.Profile = 0x1234;
            raw.Data = RandomBytes(random, random.Next(0, 7) * 4);
            packet.HeaderExtension = raw;
        }
        else
        {
            packet.HeaderExtension = CreateRandomElementExtension(
                random,
                iteration % 2);
        }

        packet.Payload = RandomBytes(random, random.Next(0, 65));
        packet.HasPadding = true;
        packet.Padding = new byte[random.Next(2, 17)];
        packet.Padding[packet.Padding.Length - 1] = (byte)packet.Padding.Length;
        return packet;
    }

    static RtpHeaderExtension CreateRandomElementExtension(
        Random random,
        int formatIndex)
    {
        RtpHeaderExtension extension = new RtpHeaderExtension();
        int elementCount = random.Next(1, 6);
        extension.Elements = new RtpHeaderExtensionElement[elementCount];
        int index = 0;
        if (formatIndex == 0)
        {
            extension.Format = RtpHeaderExtensionFormat.OneByte;
            extension.Profile = RtpHeaderExtensionProfiles.OneByte;
            while (index < elementCount)
            {
                extension.Elements[index] = CreateExtensionElement(
                    index + 1,
                    RandomBytes(random, random.Next(1, 17)));
                index = index + 1;
            }
        }
        else
        {
            extension.Format = RtpHeaderExtensionFormat.TwoByte;
            extension.ApplicationBits = (byte)random.Next(0, 16);
            extension.Profile = (ushort)(RtpHeaderExtensionProfiles.TwoByte |
                extension.ApplicationBits);
            while (index < elementCount)
            {
                extension.Elements[index] = CreateExtensionElement(
                    20 + index,
                    RandomBytes(random, random.Next(0, 33)));
                index = index + 1;
            }
        }

        return extension;
    }

    static RtpHeaderExtensionElement CreateExtensionElement(
        int identifier,
        byte[] data)
    {
        RtpHeaderExtensionElement element = new RtpHeaderExtensionElement();
        element.Identifier = identifier;
        element.Data = data;
        return element;
    }

    static RtcpPacket[] CreateRandomCoreRtcpPackets(Random random)
    {
        return new RtcpPacket[]
        {
            CreateRandomSenderReport(random),
            CreateRandomReceiverReport(random, NextUInt32(random)),
            CreateRandomSourceDescription(random, NextUInt32(random)),
            CreateRandomGoodbye(random),
            CreateRandomApplication(random),
            CreateRandomUnknownRtcp(random)
        };
    }

    static RtcpPacket CreateRandomSenderReport(Random random)
    {
        RtcpSenderInformation senderInformation = new RtcpSenderInformation();
        senderInformation.NtpTimestamp = NextUInt64(random);
        senderInformation.RtpTimestamp = NextUInt32(random);
        senderInformation.PacketCount = NextUInt32(random);
        senderInformation.OctetCount = NextUInt32(random);
        RtcpSenderReport report = new RtcpSenderReport();
        report.SenderSynchronizationSource = NextUInt32(random);
        report.SenderInformation = senderInformation;
        report.ReceptionReports = CreateRandomReceptionReports(random);
        report.ProfileExtension = RandomBytes(random, random.Next(0, 5) * 4);
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.SenderReport;
        packet.SenderReport = report;
        return packet;
    }

    static RtcpPacket CreateRandomReceiverReport(
        Random random,
        uint sourceIdentifier)
    {
        RtcpReceiverReport report = new RtcpReceiverReport();
        report.SenderSynchronizationSource = sourceIdentifier;
        report.ReceptionReports = CreateRandomReceptionReports(random);
        report.ProfileExtension = RandomBytes(random, random.Next(0, 5) * 4);
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.ReceiverReport;
        packet.ReceiverReport = report;
        return packet;
    }

    static RtcpReceptionReport[] CreateRandomReceptionReports(Random random)
    {
        RtcpReceptionReport[] reports =
            new RtcpReceptionReport[random.Next(0, 5)];
        int index = 0;
        while (index < reports.Length)
        {
            RtcpReceptionReport report = new RtcpReceptionReport();
            report.SourceIdentifier = NextUInt32(random);
            report.FractionLost = (byte)random.Next(0, 256);
            report.CumulativePacketsLost = random.Next(-8388608, 8388608);
            report.ExtendedHighestSequenceNumber = NextUInt32(random);
            report.InterarrivalJitter = NextUInt32(random);
            report.LastSenderReport = NextUInt32(random);
            report.DelaySinceLastSenderReport = NextUInt32(random);
            reports[index] = report;
            index = index + 1;
        }

        return reports;
    }

    static RtcpPacket CreateRandomSourceDescription(
        Random random,
        uint sourceIdentifier)
    {
        RtcpSourceDescriptionItem canonicalName =
            new RtcpSourceDescriptionItem();
        canonicalName.Type = RtcpSdesItemType.CanonicalName;
        canonicalName.Value = RandomAsciiBytes(random, random.Next(1, 33));
        RtcpSourceDescriptionItem displayName =
            new RtcpSourceDescriptionItem();
        displayName.Type = RtcpSdesItemType.Name;
        displayName.Value = RandomAsciiBytes(random, random.Next(0, 33));
        RtcpSourceDescriptionChunk chunk = new RtcpSourceDescriptionChunk();
        chunk.SourceIdentifier = sourceIdentifier;
        chunk.Items = new RtcpSourceDescriptionItem[]
        {
            canonicalName, displayName
        };
        RtcpSourceDescription description = new RtcpSourceDescription();
        description.Chunks = new RtcpSourceDescriptionChunk[] { chunk };
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.SourceDescription;
        packet.SourceDescription = description;
        return packet;
    }

    static RtcpPacket CreateRandomGoodbye(Random random)
    {
        RtcpGoodbye goodbye = new RtcpGoodbye();
        goodbye.Sources = new uint[random.Next(1, 6)];
        int index = 0;
        while (index < goodbye.Sources.Length)
        {
            goodbye.Sources[index] = NextUInt32(random);
            index = index + 1;
        }

        goodbye.HasReason = true;
        goodbye.Reason = RandomAsciiBytes(random, random.Next(1, 33));
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.Goodbye;
        packet.Goodbye = goodbye;
        return packet;
    }

    static RtcpPacket CreateRandomApplication(Random random)
    {
        RtcpApplicationDefined application = new RtcpApplicationDefined();
        application.Subtype = (byte)random.Next(0, 32);
        application.SourceIdentifier = NextUInt32(random);
        application.Name = new byte[]
        {
            (byte)random.Next(65, 91),
            (byte)random.Next(65, 91),
            (byte)random.Next(65, 91),
            (byte)random.Next(65, 91)
        };
        application.Data = RandomBytes(random, random.Next(0, 9) * 4);
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.ApplicationDefined;
        packet.ApplicationDefined = application;
        return packet;
    }

    static RtcpPacket CreateRandomUnknownRtcp(Random random)
    {
        RtcpUnknownPacket unknown = new RtcpUnknownPacket();
        unknown.PacketType = 222;
        unknown.Count = (byte)random.Next(0, 32);
        unknown.Data = RandomBytes(random, random.Next(0, 9) * 4);
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.Unknown;
        packet.UnknownPacket = unknown;
        return packet;
    }

    static RtcpPacket[] CreateRandomFeedbackPackets(Random random)
    {
        return new RtcpPacket[]
        {
            CreateRandomGenericNack(random),
            CreateRandomBitrateFeedback(random, false),
            CreateRandomBitrateFeedback(random, true),
            CreateRapidResynchronizationFeedback(random),
            CreateRandomCongestionFeedback(random),
            CreateRandomOpaqueFeedback(random, true),
            CreatePictureLossFeedback(random),
            CreateRandomSliceLossFeedback(random),
            CreateRandomReferencePictureFeedback(random),
            CreateRandomFullIntraFeedback(random),
            CreateRandomTradeoffFeedback(random, false),
            CreateRandomTradeoffFeedback(random, true),
            CreateRandomApplicationFeedback(random),
            CreateRandomOpaqueFeedback(random, false)
        };
    }

    static RtcpPacket CreateRandomGenericNack(Random random)
    {
        RtcpGenericNackEntry entry = new RtcpGenericNackEntry();
        entry.PacketIdentifier = (ushort)random.Next(0, 65536);
        entry.LostPacketBitmask = (ushort)random.Next(0, 65536);
        RtcpGenericNack nack = new RtcpGenericNack();
        nack.Entries = new RtcpGenericNackEntry[] { entry };
        RtcpFeedbackPacket feedback = CreateFeedbackBody(true, random);
        feedback.TransportFormat = RtcpTransportFeedbackFormat.GenericNack;
        feedback.GenericNack = nack;
        return CreateFeedbackPacket(feedback, true);
    }

    static RtcpPacket CreateRandomBitrateFeedback(
        Random random,
        bool notification)
    {
        RtcpBitrateControlEntry entry = new RtcpBitrateControlEntry();
        entry.SourceIdentifier = NextUInt32(random);
        entry.Exponent = (byte)random.Next(0, 64);
        entry.Mantissa = (uint)random.Next(0, 131072);
        entry.Overhead = (ushort)random.Next(0, 512);
        RtcpBitrateControl control = new RtcpBitrateControl();
        control.IsNotification = notification;
        control.Entries = new RtcpBitrateControlEntry[] { entry };
        RtcpFeedbackPacket feedback = CreateFeedbackBody(true, random);
        feedback.MediaSourceIdentifier = 0U;
        if (notification)
        {
            feedback.TransportFormat = RtcpTransportFeedbackFormat
                .TemporaryMaximumBitrateNotification;
        }
        else
        {
            feedback.TransportFormat = RtcpTransportFeedbackFormat
                .TemporaryMaximumBitrateRequest;
        }

        feedback.BitrateControl = control;
        return CreateFeedbackPacket(feedback, true);
    }

    static RtcpPacket CreateRapidResynchronizationFeedback(Random random)
    {
        RtcpRapidResynchronizationRequest request =
            new RtcpRapidResynchronizationRequest();
        request.Requested = true;
        RtcpFeedbackPacket feedback = CreateFeedbackBody(true, random);
        feedback.TransportFormat =
            RtcpTransportFeedbackFormat.RapidResynchronizationRequest;
        feedback.RapidResynchronizationRequest = request;
        return CreateFeedbackPacket(feedback, true);
    }

    static RtcpPacket CreateRandomCongestionFeedback(Random random)
    {
        int count = random.Next(1, 8);
        RtcpCongestionControlReport report = new RtcpCongestionControlReport();
        report.SourceIdentifier = NextUInt32(random);
        report.BeginSequenceNumber = (ushort)random.Next(0, 65536);
        report.ReportCount = count;
        report.Received = new bool[count];
        report.EcnMarks = new byte[count];
        report.ArrivalTimeOffsets = new ushort[count];
        int index = 0;
        while (index < count)
        {
            report.Received[index] = random.Next(0, 2) != 0;
            if (report.Received[index])
            {
                report.EcnMarks[index] = (byte)random.Next(0, 4);
                report.ArrivalTimeOffsets[index] =
                    (ushort)random.Next(0, 8192);
            }

            index = index + 1;
        }

        RtcpCongestionControlFeedback congestion =
            new RtcpCongestionControlFeedback();
        congestion.Reports = new RtcpCongestionControlReport[] { report };
        congestion.ReportTimestamp = NextUInt32(random);
        RtcpFeedbackPacket feedback = CreateFeedbackBody(true, random);
        feedback.MediaSourceIdentifier = 0U;
        feedback.TransportFormat =
            RtcpTransportFeedbackFormat.CongestionControlFeedback;
        feedback.CongestionControlFeedback = congestion;
        return CreateFeedbackPacket(feedback, true);
    }

    static RtcpPacket CreatePictureLossFeedback(Random random)
    {
        RtcpPictureLossIndication indication = new RtcpPictureLossIndication();
        indication.Requested = true;
        RtcpFeedbackPacket feedback = CreateFeedbackBody(false, random);
        feedback.PayloadFormat = RtcpPayloadFeedbackFormat.PictureLossIndication;
        feedback.PictureLossIndication = indication;
        return CreateFeedbackPacket(feedback, false);
    }

    static RtcpPacket CreateRandomSliceLossFeedback(Random random)
    {
        RtcpSliceLossEntry entry = new RtcpSliceLossEntry();
        entry.FirstMacroblock = random.Next(1, 8192);
        entry.MacroblockCount = random.Next(1, 8192);
        entry.PictureIdentifier = (byte)random.Next(0, 64);
        RtcpSliceLossIndication indication = new RtcpSliceLossIndication();
        indication.Entries = new RtcpSliceLossEntry[] { entry };
        RtcpFeedbackPacket feedback = CreateFeedbackBody(false, random);
        feedback.PayloadFormat = RtcpPayloadFeedbackFormat.SliceLossIndication;
        feedback.SliceLossIndication = indication;
        return CreateFeedbackPacket(feedback, false);
    }

    static RtcpPacket CreateRandomReferencePictureFeedback(Random random)
    {
        byte[] bits = RandomBytes(random, 2);
        bits[1] = (byte)(bits[1] & 0xF8);
        RtcpReferencePictureSelectionIndication indication =
            new RtcpReferencePictureSelectionIndication();
        indication.PaddingBitCount = 3;
        indication.PayloadType = (byte)random.Next(0, 128);
        indication.BitLength = 13;
        indication.NativeBitString = bits;
        RtcpFeedbackPacket feedback = CreateFeedbackBody(false, random);
        feedback.PayloadFormat = RtcpPayloadFeedbackFormat
            .ReferencePictureSelectionIndication;
        feedback.ReferencePictureSelectionIndication = indication;
        return CreateFeedbackPacket(feedback, false);
    }

    static RtcpPacket CreateRandomFullIntraFeedback(Random random)
    {
        RtcpFullIntraRequestEntry entry = new RtcpFullIntraRequestEntry();
        entry.SourceIdentifier = NextUInt32(random);
        entry.SequenceNumber = (byte)random.Next(0, 256);
        RtcpFullIntraRequest request = new RtcpFullIntraRequest();
        request.Entries = new RtcpFullIntraRequestEntry[] { entry };
        RtcpFeedbackPacket feedback = CreateFeedbackBody(false, random);
        feedback.MediaSourceIdentifier = 0U;
        feedback.PayloadFormat = RtcpPayloadFeedbackFormat.FullIntraRequest;
        feedback.FullIntraRequest = request;
        return CreateFeedbackPacket(feedback, false);
    }

    static RtcpPacket CreateRandomTradeoffFeedback(
        Random random,
        bool notification)
    {
        RtcpTemporalSpatialTradeoff tradeoff =
            new RtcpTemporalSpatialTradeoff();
        tradeoff.IsNotification = notification;
        tradeoff.SourceIdentifier = NextUInt32(random);
        tradeoff.SequenceNumber = (byte)random.Next(0, 256);
        tradeoff.TradeoffIndex = (byte)random.Next(0, 32);
        RtcpFeedbackPacket feedback = CreateFeedbackBody(false, random);
        feedback.MediaSourceIdentifier = 0U;
        if (notification)
        {
            feedback.PayloadFormat = RtcpPayloadFeedbackFormat
                .TemporalSpatialTradeoffNotification;
        }
        else
        {
            feedback.PayloadFormat = RtcpPayloadFeedbackFormat
                .TemporalSpatialTradeoffRequest;
        }

        feedback.TemporalSpatialTradeoffs =
            new RtcpTemporalSpatialTradeoff[] { tradeoff };
        return CreateFeedbackPacket(feedback, false);
    }

    static RtcpPacket CreateRandomApplicationFeedback(Random random)
    {
        RtcpApplicationFeedback application = new RtcpApplicationFeedback();
        application.Data = RandomBytes(random, random.Next(0, 9) * 4);
        RtcpFeedbackPacket feedback = CreateFeedbackBody(false, random);
        feedback.PayloadFormat = RtcpPayloadFeedbackFormat.ApplicationFeedback;
        feedback.ApplicationFeedback = application;
        return CreateFeedbackPacket(feedback, false);
    }

    static RtcpPacket CreateRandomOpaqueFeedback(
        Random random,
        bool transport)
    {
        RtcpFeedbackPacket feedback = CreateFeedbackBody(transport, random);
        feedback.Format = (byte)random.Next(12, 15);
        feedback.RawFeedbackControlInformation = RandomBytes(
            random,
            random.Next(0, 9) * 4);
        return CreateFeedbackPacket(feedback, transport);
    }

    static RtcpFeedbackPacket CreateFeedbackBody(
        bool transport,
        Random random)
    {
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        if (transport)
        {
            feedback.PacketType = RtcpFeedbackPacketType.TransportLayer;
        }
        else
        {
            feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        }

        feedback.SenderSourceIdentifier = NextUInt32(random);
        feedback.MediaSourceIdentifier = NextUInt32(random);
        return feedback;
    }

    static RtcpPacket CreateFeedbackPacket(
        RtcpFeedbackPacket feedback,
        bool transport)
    {
        RtcpPacket packet = new RtcpPacket();
        if (transport)
        {
            packet.Type = RtcpPacketType.TransportFeedback;
        }
        else
        {
            packet.Type = RtcpPacketType.PayloadFeedback;
        }

        packet.Feedback = feedback;
        return packet;
    }

    static RtcpPacket CreateRandomExtendedReport(Random random)
    {
        RtcpExtendedReportBlock loss = new RtcpExtendedReportBlock();
        loss.Type = RtcpExtendedReportBlockType.LossRunLength;
        loss.SourceIdentifier = NextUInt32(random);
        loss.BeginSequenceNumber = 0;
        loss.EndSequenceNumber = 15;
        RtcpRunLengthChunk vector = new RtcpRunLengthChunk();
        vector.IsBitVector = true;
        vector.BitVector = (ushort)random.Next(0, 32768);
        loss.RunLengthChunks = new RtcpRunLengthChunk[] { vector };

        int runLength = random.Next(1, 101);
        RtcpExtendedReportBlock duplicate = new RtcpExtendedReportBlock();
        duplicate.Type = RtcpExtendedReportBlockType.DuplicateRunLength;
        duplicate.SourceIdentifier = NextUInt32(random);
        duplicate.BeginSequenceNumber = 100;
        duplicate.EndSequenceNumber = (ushort)(100 + runLength);
        RtcpRunLengthChunk run = new RtcpRunLengthChunk();
        run.PositiveRun = random.Next(0, 2) != 0;
        run.RunLength = runLength;
        duplicate.RunLengthChunks = new RtcpRunLengthChunk[] { run };

        int receiptCount = random.Next(0, 6);
        RtcpExtendedReportBlock receipt = new RtcpExtendedReportBlock();
        receipt.Type = RtcpExtendedReportBlockType.PacketReceiptTimes;
        receipt.SourceIdentifier = NextUInt32(random);
        receipt.BeginSequenceNumber = 65533;
        receipt.EndSequenceNumber = (ushort)((65533 + receiptCount) & 65535);
        receipt.ReceiptTimes = new uint[receiptCount];
        int receiptIndex = 0;
        while (receiptIndex < receiptCount)
        {
            receipt.ReceiptTimes[receiptIndex] = NextUInt32(random);
            receiptIndex = receiptIndex + 1;
        }

        RtcpExtendedReportBlock reference = new RtcpExtendedReportBlock();
        reference.Type = RtcpExtendedReportBlockType.ReceiverReferenceTime;
        reference.ReceiverReferenceTime = NextUInt64(random);

        RtcpExtendedReportBlock delay = new RtcpExtendedReportBlock();
        delay.Type =
            RtcpExtendedReportBlockType.DelaySinceLastReceiverReport;
        delay.DelayReports =
            new RtcpDelaySinceLastReceiverReport[random.Next(1, 4)];
        int delayIndex = 0;
        while (delayIndex < delay.DelayReports.Length)
        {
            RtcpDelaySinceLastReceiverReport delayReport =
                new RtcpDelaySinceLastReceiverReport();
            delayReport.ReceiverSourceIdentifier = NextUInt32(random);
            delayReport.LastReceiverReport = NextUInt32(random);
            delayReport.DelaySinceLastReceiverReport = NextUInt32(random);
            delay.DelayReports[delayIndex] = delayReport;
            delayIndex = delayIndex + 1;
        }

        RtcpExtendedReportBlock statistics = CreateRandomStatistics(random);
        RtcpExtendedReportBlock voice = CreateRandomVoiceMetrics(random);
        RtcpExtendedReport report = new RtcpExtendedReport();
        report.SenderSynchronizationSource = NextUInt32(random);
        report.Blocks = new RtcpExtendedReportBlock[]
        {
            loss,
            duplicate,
            receipt,
            reference,
            delay,
            statistics,
            voice
        };
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.ExtendedReport;
        packet.ExtendedReport = report;
        return packet;
    }

    static RtcpExtendedReportBlock CreateRandomStatistics(Random random)
    {
        RtcpExtendedReportBlock block = new RtcpExtendedReportBlock();
        block.Type = RtcpExtendedReportBlockType.StatisticsSummary;
        block.SourceIdentifier = NextUInt32(random);
        block.BeginSequenceNumber = 65530;
        block.EndSequenceNumber = 4;
        block.ReportsLoss = random.Next(0, 2) != 0;
        block.ReportsDuplicates = random.Next(0, 2) != 0;
        block.ReportsJitter = random.Next(0, 2) != 0;
        block.TimeToLiveOrHopLimitMode = (byte)random.Next(0, 3);
        if (block.ReportsLoss)
        {
            block.LostPackets = NextUInt32(random);
        }

        if (block.ReportsDuplicates)
        {
            block.DuplicatePackets = NextUInt32(random);
        }

        if (block.ReportsJitter)
        {
            block.MinimumJitter = NextUInt32(random);
            block.MaximumJitter = NextUInt32(random);
            block.MeanJitter = NextUInt32(random);
            block.JitterDeviation = NextUInt32(random);
        }

        if (block.TimeToLiveOrHopLimitMode != 0)
        {
            block.MinimumTimeToLiveOrHopLimit = (byte)random.Next(0, 256);
            block.MaximumTimeToLiveOrHopLimit = (byte)random.Next(0, 256);
            block.MeanTimeToLiveOrHopLimit = (byte)random.Next(0, 256);
            block.TimeToLiveOrHopLimitDeviation =
                (byte)random.Next(0, 256);
        }

        return block;
    }

    static RtcpExtendedReportBlock CreateRandomVoiceMetrics(Random random)
    {
        RtcpExtendedReportBlock block = new RtcpExtendedReportBlock();
        block.Type = RtcpExtendedReportBlockType.VoiceOverIpMetrics;
        block.SourceIdentifier = NextUInt32(random);
        block.LossRate = (byte)random.Next(0, 256);
        block.DiscardRate = (byte)random.Next(0, 256);
        block.BurstDensity = (byte)random.Next(0, 256);
        block.GapDensity = (byte)random.Next(0, 256);
        block.BurstDuration = (ushort)random.Next(0, 65536);
        block.GapDuration = (ushort)random.Next(0, 65536);
        block.RoundTripDelay = (ushort)random.Next(0, 65536);
        block.EndSystemDelay = (ushort)random.Next(0, 65536);
        block.SignalLevel = (byte)random.Next(0, 256);
        block.NoiseLevel = (byte)random.Next(0, 256);
        block.ResidualEchoReturnLoss = (byte)random.Next(0, 256);
        block.MinimumGapThreshold = (byte)random.Next(1, 256);
        block.RFactor = RandomRFactor(random);
        block.ExternalRFactor = RandomRFactor(random);
        block.MeanOpinionScoreListeningQuality = RandomMeanOpinionScore(random);
        block.MeanOpinionScoreConversationalQuality =
            RandomMeanOpinionScore(random);
        block.ReceiverConfiguration = (byte)random.Next(0, 16);
        block.JitterBufferNominalDelay = (ushort)random.Next(0, 65536);
        block.JitterBufferMaximumDelay = (ushort)random.Next(0, 65536);
        block.JitterBufferAbsoluteMaximumDelay =
            (ushort)random.Next(0, 65536);
        return block;
    }

    static byte RandomRFactor(Random random)
    {
        if (random.Next(0, 4) == 0)
        {
            return 127;
        }

        return (byte)random.Next(0, 101);
    }

    static byte RandomMeanOpinionScore(Random random)
    {
        if (random.Next(0, 4) == 0)
        {
            return 127;
        }

        return (byte)random.Next(10, 51);
    }

    static void AssertRtcpRoundTripAndTruncation(
        IRtcpPacketCodec codec,
        RtcpPacket packet,
        string label)
    {
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
        RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);
        byte[] roundTrip = codec.EncodeRtcpPacketToArray(decoded);
        RtcpCodecResult truncated = codec.TryDecodeRtcpPacket(encoded[..^1]);

        Assert.Multiple(() =>
        {
            Assert.That(roundTrip, Is.EqualTo(encoded), label);
            Assert.That(truncated.Success, Is.False, label + " truncation");
            Assert.That(truncated.ConsumedCount, Is.Zero);
        });
    }

    static byte[] RandomBytes(Random random, int length)
    {
        byte[] data = new byte[length];
        random.NextBytes(data);
        return data;
    }

    static byte[] RandomAsciiBytes(Random random, int length)
    {
        byte[] data = new byte[length];
        int index = 0;
        while (index < data.Length)
        {
            data[index] = (byte)random.Next(32, 127);
            index = index + 1;
        }

        return data;
    }

    static uint NextUInt32(Random random)
    {
        byte[] data = RandomBytes(random, 4);
        return ((uint)data[0] << 24) |
            ((uint)data[1] << 16) |
            ((uint)data[2] << 8) |
            data[3];
    }

    static ulong NextUInt64(Random random)
    {
        return ((ulong)NextUInt32(random) << 32) | NextUInt32(random);
    }

    static IRtcpPacketCodec CreateRtcpCodec()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.StrictValidation = true;
        configuration.PreservePadding = true;
        configuration.MaximumRtcpCompoundPacketSize = 65535;
        configuration.MaximumRtcpCompoundPacketCount = 64;
        return new RtcpPacketCodec(configuration);
    }
}
