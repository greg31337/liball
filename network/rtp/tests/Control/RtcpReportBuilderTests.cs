using System.Text;
using rtp;

namespace rtp.Tests.Control;

public class RtcpReportBuilderTests
{
    const uint LocalSourceIdentifier = 0x10203040U;
    const uint RemoteSourceIdentifier = 0x50607080U;
    const ulong NtpTimestamp = 0x0102030405060708UL;

    [Test]
    public void ActiveLocalSourceBuildsACompleteSenderReportCompound()
    {
        RtpSessionSnapshot snapshot = CreateSnapshot(true, true);
        RtpSourceSnapshot remote = CreateRemoteSource();
        snapshot.Sources = new RtpSourceSnapshot[]
        {
            snapshot.Sources[0],
            remote
        };
        IRtcpReportBuilder builder = new RtcpReportBuilder();

        RtcpCompoundPacket compound = builder.BuildRegularRtcpReport(
            snapshot,
            NtpTimestamp);
        snapshot.Sources[0].TransmissionStatistics!.SentPacketCount = 0L;
        remote.ReceptionStatistics!.CumulativePacketsLost = 0;
        snapshot.Configuration.CanonicalName = "changed";

        RtcpPacket reportPacket = compound.Packets[0];
        RtcpPacket descriptionPacket = compound.Packets[1];
        RtcpSenderReport senderReport = reportPacket.SenderReport!;
        RtcpReceptionReport receptionReport = senderReport.ReceptionReports[0];
        RtcpSourceDescriptionChunk description =
            descriptionPacket.SourceDescription!.Chunks[0];

        Assert.Multiple(() =>
        {
            Assert.That(compound.Packets, Has.Length.EqualTo(2));
            Assert.That(reportPacket.Type, Is.EqualTo(RtcpPacketType.SenderReport));
            Assert.That(senderReport.SenderSynchronizationSource,
                Is.EqualTo(LocalSourceIdentifier));
            Assert.That(senderReport.SenderInformation.NtpTimestamp,
                Is.EqualTo(NtpTimestamp));
            Assert.That(senderReport.SenderInformation.RtpTimestamp,
                Is.EqualTo(0xA0B0C0D0U));
            Assert.That(senderReport.SenderInformation.PacketCount,
                Is.EqualTo(1U));
            Assert.That(senderReport.SenderInformation.OctetCount,
                Is.EqualTo(2U));
            Assert.That(receptionReport.SourceIdentifier,
                Is.EqualTo(RemoteSourceIdentifier));
            Assert.That(receptionReport.FractionLost, Is.EqualTo(64));
            Assert.That(receptionReport.CumulativePacketsLost, Is.EqualTo(-2));
            Assert.That(receptionReport.ExtendedHighestSequenceNumber,
                Is.EqualTo(300U));
            Assert.That(receptionReport.InterarrivalJitter, Is.EqualTo(12U));
            Assert.That(receptionReport.LastSenderReport, Is.EqualTo(0x11223344U));
            Assert.That(receptionReport.DelaySinceLastSenderReport,
                Is.EqualTo(0x00008000U));
            Assert.That(descriptionPacket.Type,
                Is.EqualTo(RtcpPacketType.SourceDescription));
            Assert.That(description.SourceIdentifier,
                Is.EqualTo(LocalSourceIdentifier));
            Assert.That(description.Items[0].Type,
                Is.EqualTo(RtcpSdesItemType.CanonicalName));
            Assert.That(description.Items[0].Value,
                Is.EqualTo(Encoding.UTF8.GetBytes("local@example")));
        });

        IRtcpPacketCodec codec = new RtcpPacketCodec(new RtpSessionConfiguration());
        Assert.DoesNotThrow(() => codec.EncodeRtcpCompoundPacketToArray(compound));
    }

    [Test]
    public void InactiveAndActiveLocalSnapshotsSelectReportsIndependently()
    {
        IRtcpReportBuilder builder = new RtcpReportBuilder();
        RtpSessionSnapshot inactive = CreateSnapshot(false, true);
        RtpSessionSnapshot active = CreateSnapshot(true, true);

        RtcpCompoundPacket receiverCompound =
            builder.BuildRegularRtcpReport(inactive, NtpTimestamp);
        RtcpCompoundPacket senderCompound =
            builder.BuildRegularRtcpReport(active, NtpTimestamp);

        Assert.Multiple(() =>
        {
            Assert.That(receiverCompound.Packets[0].Type,
                Is.EqualTo(RtcpPacketType.ReceiverReport));
            Assert.That(receiverCompound.Packets[0].ReceiverReport?
                .SenderSynchronizationSource,
                Is.EqualTo(LocalSourceIdentifier));
            Assert.That(receiverCompound.Packets[0].SenderReport, Is.Null);
            Assert.That(senderCompound.Packets[0].Type,
                Is.EqualTo(RtcpPacketType.SenderReport));
            Assert.That(senderCompound.Packets[0].ReceiverReport, Is.Null);
        });
    }

    [Test]
    public void MissingLocalStateStillProducesAReceiverReport()
    {
        RtpSessionSnapshot snapshot = CreateSnapshot(false, true);
        snapshot.Sources = new RtpSourceSnapshot[] { CreateRemoteSource() };
        IRtcpReportBuilder builder = new RtcpReportBuilder();

        RtcpCompoundPacket compound = builder.BuildRegularRtcpReport(
            snapshot,
            NtpTimestamp);

        Assert.Multiple(() =>
        {
            Assert.That(compound.Packets[0].Type,
                Is.EqualTo(RtcpPacketType.ReceiverReport));
            Assert.That(compound.Packets[0].ReceiverReport?
                .SenderSynchronizationSource,
                Is.EqualTo(LocalSourceIdentifier));
            Assert.That(compound.Packets[0].ReceiverReport?.ReceptionReports,
                Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void InvalidSessionSnapshotsAreRejectedBeforeConstruction()
    {
        IRtcpReportBuilder builder = new RtcpReportBuilder();
        RtpSessionSnapshot noLocals = CreateSnapshot(false, true);
        noLocals.LocalSourceIdentifiers = [];
        RtpSessionSnapshot noCanonicalName = CreateSnapshot(false, true);
        noCanonicalName.Configuration.CanonicalName = "";
        RtpSessionSnapshot mismatchedLocal = CreateSnapshot(false, true);
        mismatchedLocal.Sources[0].IsLocal = false;
        RtpSessionSnapshot ineligibleSender = CreateSnapshot(true, false);
        RtpSessionSnapshot mismatchedTransmission = CreateSnapshot(true, true);
        mismatchedTransmission.Sources[0].TransmissionStatistics!
            .SourceIdentifier = LocalSourceIdentifier + 1U;
        RtpSessionSnapshot mismatchedReception = CreateSnapshot(false, true);
        RtpSourceSnapshot badRemote = CreateRemoteSource();
        badRemote.ReceptionStatistics!.SourceIdentifier = RemoteSourceIdentifier + 1U;
        mismatchedReception.Sources = new RtpSourceSnapshot[]
        {
            mismatchedReception.Sources[0],
            badRemote
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                builder.BuildRegularRtcpReport(null!, NtpTimestamp));
            Assert.Throws<RtpConfigurationException>(() =>
                builder.BuildRegularRtcpReport(noLocals, NtpTimestamp));
            Assert.Throws<RtpConfigurationException>(() =>
                builder.BuildRegularRtcpReport(noCanonicalName, NtpTimestamp));
            Assert.Throws<RtpConfigurationException>(() =>
                builder.BuildRegularRtcpReport(mismatchedLocal, NtpTimestamp));
            Assert.Throws<RtpConfigurationException>(() =>
                builder.BuildRegularRtcpReport(ineligibleSender, NtpTimestamp));
            Assert.Throws<RtpConfigurationException>(() => builder
                .BuildRegularRtcpReport(mismatchedTransmission, NtpTimestamp));
            Assert.Throws<RtpConfigurationException>(() => builder
                .BuildRegularRtcpReport(mismatchedReception, NtpTimestamp));
        });
    }

    [Test]
    public void SenderInformationRejectsNegativeSnapshotCounters()
    {
        RtpSessionSnapshot snapshot = CreateSnapshot(true, true);
        snapshot.Sources[0].TransmissionStatistics!.SentPacketCount = -1L;
        IRtcpReportBuilder builder = new RtcpReportBuilder();

        Assert.Throws<RtpConfigurationException>(() =>
            builder.BuildRegularRtcpReport(snapshot, NtpTimestamp));
    }

    [Test]
    public void MultiStreamCompoundsSplitAndRotateReceptionReportBlocks()
    {
        RtpSessionSnapshot snapshot = CreateMultiStreamSnapshot(35);
        IRtcpReportBuilder builder = new RtcpReportBuilder();

        RtcpCompoundPacket first = builder.BuildRegularRtcpReport(
            snapshot,
            NtpTimestamp);
        RtcpCompoundPacket rotated = builder.BuildRegularRtcpReport(
            snapshot,
            NtpTimestamp);

        Assert.Multiple(() =>
        {
            Assert.That(first.Packets, Has.Length.EqualTo(5));
            Assert.That(first.Packets[0].Type,
                Is.EqualTo(RtcpPacketType.SenderReport));
            Assert.That(first.Packets[0].SenderReport?.SenderSynchronizationSource,
                Is.EqualTo(LocalSourceIdentifier));
            Assert.That(first.Packets[0].SenderReport?.ReceptionReports,
                Has.Length.EqualTo(31));
            Assert.That(first.Packets[0].SenderReport?.ReceptionReports[0]
                .SourceIdentifier,
                Is.EqualTo(1000U));
            Assert.That(first.Packets[1].Type,
                Is.EqualTo(RtcpPacketType.ReceiverReport));
            Assert.That(first.Packets[1].ReceiverReport?.SenderSynchronizationSource,
                Is.EqualTo(LocalSourceIdentifier));
            Assert.That(first.Packets[1].ReceiverReport?.ReceptionReports,
                Has.Length.EqualTo(4));
            Assert.That(first.Packets[2].Type,
                Is.EqualTo(RtcpPacketType.SenderReport));
            Assert.That(first.Packets[2].SenderReport?.SenderSynchronizationSource,
                Is.EqualTo(LocalSourceIdentifier + 1U));
            Assert.That(first.Packets[3].Type,
                Is.EqualTo(RtcpPacketType.ReceiverReport));
            Assert.That(first.Packets[4].Type,
                Is.EqualTo(RtcpPacketType.SourceDescription));
            Assert.That(first.Packets[4].SourceDescription?.Chunks,
                Has.Length.EqualTo(2));
            Assert.That(rotated.Packets[0].SenderReport?.ReceptionReports[0]
                .SourceIdentifier,
                Is.EqualTo(1031U));
            Assert.That(rotated.Packets[1].ReceiverReport?.ReceptionReports[0]
                .SourceIdentifier,
                Is.EqualTo(1027U));
        });

        first.Packets[0].SenderReport!.ReceptionReports[0].FractionLost = 99;
        Assert.That(
            first.Packets[2].SenderReport!.ReceptionReports[0].FractionLost,
            Is.EqualTo(64));
        IRtcpPacketCodec codec = new RtcpPacketCodec(new RtpSessionConfiguration());
        Assert.DoesNotThrow(() => codec.EncodeRtcpCompoundPacketToArray(first));
    }

    [Test]
    public void OptionalSourceDescriptionsRotateWithinTheirByteBudget()
    {
        RtpSessionSnapshot snapshot = CreateSnapshot(false, true);
        snapshot.AverageRtcpPacketSize = 40;
        RtcpSourceDescriptionItem name = CreateSourceDescriptionItem(
            RtcpSdesItemType.Name,
            new string('n', 30));
        RtcpSourceDescriptionItem email = CreateSourceDescriptionItem(
            RtcpSdesItemType.Email,
            "mail@example.t");
        snapshot.Configuration.OptionalSourceDescriptionItems =
            new RtcpSourceDescriptionItem[] { name, email };
        IRtcpReportBuilder builder = new RtcpReportBuilder();

        RtcpCompoundPacket[] compounds = new RtcpCompoundPacket[10];
        int reportIndex = 0;
        while (reportIndex < compounds.Length)
        {
            compounds[reportIndex] = builder.BuildRegularRtcpReport(
                snapshot,
                NtpTimestamp);
            reportIndex = reportIndex + 1;
        }

        name.Value[0] = (byte)'x';
        RtcpSourceDescriptionItem[] fourthItems = compounds[3].Packets[1]
            .SourceDescription!.Chunks[0].Items;
        RtcpSourceDescriptionItem[] sixthItems = compounds[5].Packets[1]
            .SourceDescription!.Chunks[0].Items;
        RtcpSourceDescriptionItem[] tenthItems = compounds[9].Packets[1]
            .SourceDescription!.Chunks[0].Items;

        Assert.Multiple(() =>
        {
            int compoundIndex = 0;
            while (compoundIndex < compounds.Length)
            {
                Assert.That(compounds[compoundIndex].Packets[1]
                    .SourceDescription?.Chunks[0].Items[0].Type,
                    Is.EqualTo(RtcpSdesItemType.CanonicalName));
                compoundIndex = compoundIndex + 1;
            }

            Assert.That(compounds[0].Packets[1].SourceDescription?
                .Chunks[0].Items, Has.Length.EqualTo(1));
            Assert.That(compounds[1].Packets[1].SourceDescription?
                .Chunks[0].Items, Has.Length.EqualTo(1));
            Assert.That(compounds[2].Packets[1].SourceDescription?
                .Chunks[0].Items, Has.Length.EqualTo(1));
            Assert.That(fourthItems, Has.Length.EqualTo(2));
            Assert.That(fourthItems[1].Type, Is.EqualTo(RtcpSdesItemType.Name));
            Assert.That(fourthItems[1].Value[0], Is.EqualTo((byte)'n'));
            Assert.That(compounds[4].Packets[1].SourceDescription?
                .Chunks[0].Items, Has.Length.EqualTo(1));
            Assert.That(sixthItems[1].Type, Is.EqualTo(RtcpSdesItemType.Email));
            Assert.That(tenthItems[1].Type, Is.EqualTo(RtcpSdesItemType.Name));
        });

        IRtcpPacketCodec codec = new RtcpPacketCodec(new RtpSessionConfiguration());
        Assert.DoesNotThrow(() => codec.EncodeRtcpCompoundPacketToArray(compounds[3]));
        Assert.DoesNotThrow(() => codec.EncodeRtcpCompoundPacketToArray(compounds[5]));
    }

    [Test]
    public void RegularFeedbackDecisionBuildsAFullCompound()
    {
        RtpSessionSnapshot snapshot = CreateSnapshot(false, true);
        snapshot.Sources = new RtpSourceSnapshot[]
        {
            snapshot.Sources[0],
            CreateRemoteSource()
        };
        snapshot.Configuration.RtcpCompoundMode = RtcpCompoundMode.Full;
        RtcpFeedbackPacket feedback = CreatePictureLossFeedback();
        IRtcpReportBuilder builder = new RtcpReportBuilder();

        RtcpCompoundPacket compound = builder.BuildFeedbackRtcpReport(
            snapshot,
            feedback,
            RtcpFeedbackTimingMode.Regular);
        feedback.MediaSourceIdentifier = 1U;

        Assert.Multiple(() =>
        {
            Assert.That(compound.Packets, Has.Length.EqualTo(3));
            Assert.That(compound.Packets[0].Type,
                Is.EqualTo(RtcpPacketType.ReceiverReport));
            Assert.That(compound.Packets[0].ReceiverReport?
                .SenderSynchronizationSource,
                Is.EqualTo(LocalSourceIdentifier));
            Assert.That(compound.Packets[0].ReceiverReport?.ReceptionReports,
                Has.Length.EqualTo(1));
            Assert.That(compound.Packets[1].Type,
                Is.EqualTo(RtcpPacketType.SourceDescription));
            Assert.That(compound.Packets[2].Type,
                Is.EqualTo(RtcpPacketType.PayloadFeedback));
            Assert.That(compound.Packets[2].Feedback?.SenderSourceIdentifier,
                Is.EqualTo(LocalSourceIdentifier));
            Assert.That(compound.Packets[2].Feedback?.MediaSourceIdentifier,
                Is.EqualTo(RemoteSourceIdentifier));
        });

        IRtcpPacketCodec codec = new RtcpPacketCodec(snapshot.Configuration);
        Assert.DoesNotThrow(() =>
            codec.EncodeRtcpCompoundPacketToArray(compound));
    }

    [Test]
    public void EarlyFeedbackDecisionBuildsAMinimalCompound()
    {
        RtpSessionSnapshot snapshot = CreateSnapshot(false, true);
        snapshot.Sources = new RtpSourceSnapshot[]
        {
            snapshot.Sources[0],
            CreateRemoteSource()
        };
        snapshot.Configuration.RtcpCompoundMode =
            RtcpCompoundMode.MinimalFeedback;
        snapshot.Configuration.OptionalSourceDescriptionItems =
            new RtcpSourceDescriptionItem[]
            {
                CreateSourceDescriptionItem(RtcpSdesItemType.Name, "optional")
            };
        IRtcpReportBuilder builder = new RtcpReportBuilder();

        RtcpCompoundPacket compound = builder.BuildFeedbackRtcpReport(
            snapshot,
            CreateGenericNackFeedback(),
            RtcpFeedbackTimingMode.Early);

        Assert.Multiple(() =>
        {
            Assert.That(compound.Packets, Has.Length.EqualTo(3));
            Assert.That(compound.Packets[0].ReceiverReport?.ReceptionReports,
                Is.Empty);
            Assert.That(compound.Packets[1].SourceDescription?.Chunks,
                Has.Length.EqualTo(1));
            Assert.That(compound.Packets[1].SourceDescription?.Chunks[0].Items,
                Has.Length.EqualTo(1));
            Assert.That(compound.Packets[1].SourceDescription?.Chunks[0]
                .Items[0].Type,
                Is.EqualTo(RtcpSdesItemType.CanonicalName));
            Assert.That(compound.Packets[2].Type,
                Is.EqualTo(RtcpPacketType.TransportFeedback));
        });

        IRtcpPacketCodec codec = new RtcpPacketCodec(snapshot.Configuration);
        Assert.DoesNotThrow(() =>
            codec.EncodeRtcpCompoundPacketToArray(compound));
    }

    [Test]
    public void NegotiatedReducedSizeDecisionBuildsOnlyFeedback()
    {
        RtpSessionSnapshot snapshot = CreateSnapshot(false, true);
        snapshot.Configuration.RtcpCompoundMode = RtcpCompoundMode.ReducedSize;
        snapshot.Configuration.RtcpScheduler.ReducedSizeEnabled = true;
        IRtcpReportBuilder builder = new RtcpReportBuilder();

        RtcpCompoundPacket reduced = builder.BuildFeedbackRtcpReport(
            snapshot,
            CreatePictureLossFeedback(),
            RtcpFeedbackTimingMode.ReducedSize);
        RtcpCompoundPacket regular = builder.BuildFeedbackRtcpReport(
            snapshot,
            CreatePictureLossFeedback(),
            RtcpFeedbackTimingMode.Regular);

        Assert.Multiple(() =>
        {
            Assert.That(reduced.Packets, Has.Length.EqualTo(1));
            Assert.That(reduced.Packets[0].Type,
                Is.EqualTo(RtcpPacketType.PayloadFeedback));
            Assert.That(reduced.Packets[0].Feedback?.SenderSourceIdentifier,
                Is.EqualTo(LocalSourceIdentifier));
            Assert.That(regular.Packets, Has.Length.EqualTo(3));
            Assert.That(regular.Packets[0].Type,
                Is.EqualTo(RtcpPacketType.ReceiverReport));
            Assert.That(regular.Packets[1].Type,
                Is.EqualTo(RtcpPacketType.SourceDescription));
        });

        IRtcpPacketCodec codec = new RtcpPacketCodec(snapshot.Configuration);
        Assert.DoesNotThrow(() =>
            codec.EncodeRtcpCompoundPacketToArray(reduced));
        Assert.DoesNotThrow(() =>
            codec.EncodeRtcpCompoundPacketToArray(regular));
    }

    [Test]
    public void FeedbackConstructionRejectsInvalidOrUnnegotiatedInput()
    {
        IRtcpReportBuilder builder = new RtcpReportBuilder();
        RtpSessionSnapshot disabledReduced = CreateSnapshot(false, true);
        disabledReduced.Configuration.RtcpCompoundMode =
            RtcpCompoundMode.ReducedSize;
        RtpSessionSnapshot wrongReducedMode = CreateSnapshot(false, true);
        wrongReducedMode.Configuration.RtcpCompoundMode =
            RtcpCompoundMode.MinimalFeedback;
        wrongReducedMode.Configuration.RtcpScheduler.ReducedSizeEnabled = true;
        RtpSessionSnapshot foreignSender = CreateSnapshot(false, true);
        RtcpFeedbackPacket foreignFeedback = CreatePictureLossFeedback();
        foreignFeedback.SenderSourceIdentifier = 99U;
        RtcpFeedbackPacket invalidFeedback = CreatePictureLossFeedback();
        invalidFeedback.PictureLossIndication!.Requested = false;

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                builder.BuildFeedbackRtcpReport(
                    disabledReduced,
                    CreatePictureLossFeedback(),
                    RtcpFeedbackTimingMode.ReducedSize));
            Assert.Throws<RtpConfigurationException>(() =>
                builder.BuildFeedbackRtcpReport(
                    wrongReducedMode,
                    CreatePictureLossFeedback(),
                    RtcpFeedbackTimingMode.ReducedSize));
            Assert.Throws<RtpConfigurationException>(() =>
                builder.BuildFeedbackRtcpReport(
                    foreignSender,
                    foreignFeedback,
                    RtcpFeedbackTimingMode.Early));
            Assert.Throws<RtpConfigurationException>(() =>
                builder.BuildFeedbackRtcpReport(
                    foreignSender,
                    CreatePictureLossFeedback(),
                    (RtcpFeedbackTimingMode)99));
            Assert.Throws<RtcpPacketFormatException>(() =>
                builder.BuildFeedbackRtcpReport(
                    foreignSender,
                    invalidFeedback,
                    RtcpFeedbackTimingMode.Early));
        });
    }

    [Test]
    public void GoodbyeReportIsReusableEncodableAndIsolatedFromCallerMutation()
    {
        RtpSessionSnapshot snapshot = CreateSnapshot(false, true);
        byte[] reason = Encoding.UTF8.GetBytes("leaving");
        IRtcpReportBuilder builder = new RtcpReportBuilder();

        RtcpCompoundPacket first = builder.BuildGoodbyeRtcpReport(
            snapshot,
            reason);
        RtcpCompoundPacket second = builder.BuildGoodbyeRtcpReport(
            snapshot,
            reason);
        reason[0] = (byte)'X';
        snapshot.LocalSourceIdentifiers[0] = 99U;
        snapshot.Configuration.CanonicalName = "changed";

        RtcpGoodbye firstGoodbye = first.Packets[2].Goodbye!;
        RtcpGoodbye secondGoodbye = second.Packets[2].Goodbye!;
        IRtcpPacketCodec codec = new RtcpPacketCodec(
            new RtpSessionConfiguration());
        byte[] firstWire = codec.EncodeRtcpCompoundPacketToArray(first);
        byte[] secondWire = codec.EncodeRtcpCompoundPacketToArray(second);

        Assert.Multiple(() =>
        {
            Assert.That(first.Packets, Has.Length.EqualTo(3));
            Assert.That(first.Packets[0].Type,
                Is.EqualTo(RtcpPacketType.ReceiverReport));
            Assert.That(first.Packets[1].Type,
                Is.EqualTo(RtcpPacketType.SourceDescription));
            Assert.That(first.Packets[2].Type,
                Is.EqualTo(RtcpPacketType.Goodbye));
            Assert.That(firstGoodbye.Sources,
                Is.EqualTo(new uint[] { LocalSourceIdentifier }));
            Assert.That(firstGoodbye.HasReason, Is.True);
            Assert.That(firstGoodbye.Reason,
                Is.EqualTo(Encoding.UTF8.GetBytes("leaving")));
            Assert.That(secondGoodbye.Sources,
                Is.EqualTo(new uint[] { LocalSourceIdentifier }));
            Assert.That(secondGoodbye.Reason,
                Is.EqualTo(Encoding.UTF8.GetBytes("leaving")));
            Assert.That(secondGoodbye, Is.Not.SameAs(firstGoodbye));
            Assert.That(secondWire, Is.EqualTo(firstWire));
        });
    }

    [Test]
    public void GoodbyeReportRejectsInvalidSnapshotReasonAndEncodedCapacity()
    {
        RtpSessionSnapshot snapshot = CreateSnapshot(false, true);
        IRtcpReportBuilder builder = new RtcpReportBuilder();
        byte[] oversizedReason = new byte[256];
        byte[] invalidUtf8 = new byte[] { 0xC0, 0xAF };
        RtcpCompoundPacket compound = builder.BuildGoodbyeRtcpReport(
            snapshot,
            ReadOnlySpan<byte>.Empty);
        IRtcpPacketCodec codec = new RtcpPacketCodec(
            new RtpSessionConfiguration());
        byte[] destination = new byte[] { 0xA5 };

        RtcpCodecResult capacity = codec.TryEncodeRtcpCompoundPacket(
            compound,
            destination);

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                builder.BuildGoodbyeRtcpReport(null!, ReadOnlySpan<byte>.Empty));
            Assert.Throws<RtpConfigurationException>(() =>
                builder.BuildGoodbyeRtcpReport(snapshot, oversizedReason));
            Assert.Throws<RtpConfigurationException>(() =>
                builder.BuildGoodbyeRtcpReport(snapshot, invalidUtf8));
            Assert.That(capacity.Success, Is.False);
            Assert.That(capacity.WrittenCount, Is.Zero);
            Assert.That(destination, Is.EqualTo(new byte[] { 0xA5 }));
            Assert.Throws<RtpBufferCapacityException>(() =>
                codec.EncodeRtcpCompoundPacket(compound, destination));
        });
    }

    [Test]
    public void GoodbyeReportSplitsMaximumSourceListsAndDescriptionChunks()
    {
        RtpSessionSnapshot snapshot = CreateSnapshot(false, true);
        snapshot.LocalSourceIdentifiers = new uint[32];
        snapshot.Sources = new RtpSourceSnapshot[32];
        int index = 0;
        while (index < snapshot.LocalSourceIdentifiers.Length)
        {
            uint sourceIdentifier = (uint)(1000 + index);
            snapshot.LocalSourceIdentifiers[index] = sourceIdentifier;
            RtpSourceSnapshot source = new RtpSourceSnapshot();
            source.SourceIdentifier = sourceIdentifier;
            source.IsLocal = true;
            snapshot.Sources[index] = source;
            index = index + 1;
        }

        IRtcpReportBuilder builder = new RtcpReportBuilder();
        RtcpCompoundPacket compound = builder.BuildGoodbyeRtcpReport(
            snapshot,
            Encoding.UTF8.GetBytes("done"));
        RtcpPacket[] goodbyePackets = compound.Packets
            .Where(packet => packet.Type == RtcpPacketType.Goodbye)
            .ToArray();
        RtcpPacket[] descriptionPackets = compound.Packets
            .Where(packet => packet.Type == RtcpPacketType.SourceDescription)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(goodbyePackets, Has.Length.EqualTo(2));
            Assert.That(goodbyePackets[0].Goodbye?.Sources, Has.Length.EqualTo(31));
            Assert.That(goodbyePackets[0].Goodbye?.HasReason, Is.False);
            Assert.That(goodbyePackets[1].Goodbye?.Sources,
                Is.EqualTo(new uint[] { 1031U }));
            Assert.That(goodbyePackets[1].Goodbye?.Reason,
                Is.EqualTo(Encoding.UTF8.GetBytes("done")));
            Assert.That(descriptionPackets, Has.Length.EqualTo(2));
            Assert.That(descriptionPackets[0].SourceDescription?.Chunks,
                Has.Length.EqualTo(31));
            Assert.That(descriptionPackets[1].SourceDescription?.Chunks,
                Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void ReportBuilderRejectsEveryMalformedSnapshotShape()
    {
        IRtcpReportBuilder builder = new RtcpReportBuilder();
        RtpSessionSnapshot noIdentifiers = CreateSnapshot(false, true);
        noIdentifiers.LocalSourceIdentifiers = null!;
        RtpSessionSnapshot noSources = CreateSnapshot(false, true);
        noSources.Sources = null!;
        RtpSessionSnapshot nullCanonicalName = CreateSnapshot(false, true);
        nullCanonicalName.Configuration.CanonicalName = null!;
        RtpSessionSnapshot longCanonicalName = CreateSnapshot(false, true);
        longCanonicalName.Configuration.CanonicalName = new string('x', 256);
        RtpSessionSnapshot nullSource = CreateSnapshot(false, true);
        nullSource.Sources = new RtpSourceSnapshot[] { null! };
        RtpSessionSnapshot duplicateIdentifier = CreateSnapshot(false, true);
        duplicateIdentifier.LocalSourceIdentifiers = new uint[]
        {
            LocalSourceIdentifier,
            LocalSourceIdentifier
        };
        RtpSessionSnapshot missingScheduler = CreateSnapshot(false, true);
        missingScheduler.Configuration.OptionalSourceDescriptionItems =
            new RtcpSourceDescriptionItem[]
            {
                CreateSourceDescriptionItem(RtcpSdesItemType.Name, "name")
            };
        missingScheduler.Configuration.RtcpScheduler = null!;

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                builder.BuildRegularRtcpReport(noIdentifiers, NtpTimestamp));
            Assert.Throws<RtpConfigurationException>(() =>
                builder.BuildRegularRtcpReport(noSources, NtpTimestamp));
            Assert.Throws<RtpConfigurationException>(() =>
                builder.BuildRegularRtcpReport(nullCanonicalName, NtpTimestamp));
            Assert.Throws<RtpConfigurationException>(() =>
                builder.BuildRegularRtcpReport(longCanonicalName, NtpTimestamp));
            Assert.Throws<RtpConfigurationException>(() =>
                builder.BuildRegularRtcpReport(nullSource, NtpTimestamp));
            Assert.Throws<RtpConfigurationException>(() =>
                builder.BuildRegularRtcpReport(duplicateIdentifier, NtpTimestamp));
            Assert.Throws<RtpConfigurationException>(() =>
                builder.BuildRegularRtcpReport(missingScheduler, NtpTimestamp));
            Assert.Throws<RtpConfigurationException>(() =>
                builder.BuildFeedbackRtcpReport(
                    CreateSnapshot(false, true),
                    null!,
                    RtcpFeedbackTimingMode.Regular));
        });
    }

    private static RtpSessionSnapshot CreateSnapshot(
        bool activeSender,
        bool senderReportEligible)
    {
        RtpTransmissionStatisticsSnapshot transmission =
            new RtpTransmissionStatisticsSnapshot();
        transmission.SourceIdentifier = LocalSourceIdentifier;
        transmission.SentPacketCount = 4294967297L;
        transmission.SentOctetCount = 4294967298L;
        transmission.LastRtpTimestamp = 0xA0B0C0D0U;
        transmission.IsActiveSender = activeSender;
        transmission.SenderReportEligible = senderReportEligible;
        RtpSourceSnapshot local = new RtpSourceSnapshot();
        local.SourceIdentifier = LocalSourceIdentifier;
        local.IsLocal = true;
        local.TransmissionStatistics = transmission;
        RtpSessionSnapshot snapshot = new RtpSessionSnapshot();
        snapshot.Configuration.CanonicalName = "local@example";
        snapshot.LocalSourceIdentifiers =
            new uint[] { LocalSourceIdentifier };
        snapshot.Sources = new RtpSourceSnapshot[] { local };
        return snapshot;
    }

    private static RtpSessionSnapshot CreateMultiStreamSnapshot(int remoteCount)
    {
        RtpSessionSnapshot snapshot = CreateSnapshot(true, true);
        RtpTransmissionStatisticsSnapshot secondTransmission =
            new RtpTransmissionStatisticsSnapshot();
        secondTransmission.SourceIdentifier = LocalSourceIdentifier + 1U;
        secondTransmission.SenderReportEligible = true;
        secondTransmission.IsActiveSender = true;
        RtpSourceSnapshot secondLocal = new RtpSourceSnapshot();
        secondLocal.SourceIdentifier = LocalSourceIdentifier + 1U;
        secondLocal.IsLocal = true;
        secondLocal.TransmissionStatistics = secondTransmission;
        snapshot.LocalSourceIdentifiers = new uint[]
        {
            LocalSourceIdentifier,
            LocalSourceIdentifier + 1U
        };
        snapshot.Sources = new RtpSourceSnapshot[remoteCount + 2];
        snapshot.Sources[0] = CreateSnapshot(true, true).Sources[0];
        snapshot.Sources[1] = secondLocal;
        int remoteIndex = 0;
        while (remoteIndex < remoteCount)
        {
            snapshot.Sources[remoteIndex + 2] =
                CreateRemoteSourceWithIdentifier((uint)(1000 + remoteIndex));
            remoteIndex = remoteIndex + 1;
        }

        return snapshot;
    }

    private static RtpSourceSnapshot CreateRemoteSource()
    {
        return CreateRemoteSourceWithIdentifier(RemoteSourceIdentifier);
    }

    private static RtpSourceSnapshot CreateRemoteSourceWithIdentifier(
        uint sourceIdentifier)
    {
        RtpReceptionStatisticsSnapshot reception =
            new RtpReceptionStatisticsSnapshot();
        reception.SourceIdentifier = sourceIdentifier;
        reception.FractionLost = 64;
        reception.CumulativePacketsLost = -2;
        reception.HighestExtendedSequenceNumber = 300U;
        reception.InterarrivalJitter = 12U;
        reception.LastSenderReport = 0x11223344U;
        reception.DelaySinceLastSenderReport = 0x00008000U;
        RtpSourceSnapshot remote = new RtpSourceSnapshot();
        remote.SourceIdentifier = sourceIdentifier;
        remote.IsLocal = false;
        remote.ReceptionStatistics = reception;
        return remote;
    }

    private static RtcpSourceDescriptionItem CreateSourceDescriptionItem(
        RtcpSdesItemType type,
        string value)
    {
        RtcpSourceDescriptionItem item = new RtcpSourceDescriptionItem();
        item.Type = type;
        item.Value = Encoding.UTF8.GetBytes(value);
        return item;
    }

    private static RtcpFeedbackPacket CreatePictureLossFeedback()
    {
        RtcpPictureLossIndication indication =
            new RtcpPictureLossIndication();
        indication.Requested = true;
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        feedback.PayloadFormat =
            RtcpPayloadFeedbackFormat.PictureLossIndication;
        feedback.MediaSourceIdentifier = RemoteSourceIdentifier;
        feedback.PictureLossIndication = indication;
        return feedback;
    }

    private static RtcpFeedbackPacket CreateGenericNackFeedback()
    {
        RtcpGenericNackEntry entry = new RtcpGenericNackEntry();
        entry.PacketIdentifier = 10;
        RtcpGenericNack genericNack = new RtcpGenericNack();
        genericNack.Entries = new RtcpGenericNackEntry[] { entry };
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.TransportLayer;
        feedback.TransportFormat = RtcpTransportFeedbackFormat.GenericNack;
        feedback.MediaSourceIdentifier = RemoteSourceIdentifier;
        feedback.GenericNack = genericNack;
        return feedback;
    }
}
