using System;
using System.Collections.Generic;
using System.Text;

namespace rtp;

/// <summary>Builds RTCP reports from independent RTP session snapshots.</summary>
public class RtcpReportBuilder : IRtcpReportBuilder
{
    internal object syncRoot = new object();
    internal int reportBlockCursor;
    internal int sourceDescriptionItemCursor;
    internal double optionalSourceDescriptionCredit;

    /// <summary>Initializes an RTCP report builder.</summary>
    public RtcpReportBuilder()
    {
    }

    /// <summary>Builds regular reports for the local sources.</summary>
    /// <param name="snapshot">The independent session snapshot to report.</param>
    /// <param name="ntpTimestamp">The current 64-bit NTP timestamp.</param>
    /// <returns>A full RTCP compound containing the report and mandatory CNAME.</returns>
    public RtcpCompoundPacket BuildRegularRtcpReport(
        RtpSessionSnapshot snapshot,
        ulong ntpTimestamp)
    {
        ValidateRegularReportSnapshot(snapshot);
        lock (syncRoot)
        {
            RtcpReceptionReport[] receptionReports =
                CreateReceptionReportBlocks(snapshot, reportBlockCursor);
            List<RtcpPacket> packets = new List<RtcpPacket>();
            int localIndex = 0;
            while (localIndex < snapshot.LocalSourceIdentifiers.Length)
            {
                uint localSourceIdentifier =
                    snapshot.LocalSourceIdentifiers[localIndex];
                RtpSourceSnapshot? localSource = FindLocalSource(
                    snapshot,
                    localSourceIdentifier);
                AddLocalReportPackets(
                    packets,
                    localSourceIdentifier,
                    localSource,
                    receptionReports,
                    ntpTimestamp);
                localIndex = localIndex + 1;
            }

            RtcpSourceDescriptionItem? optionalItem =
                SelectOptionalSourceDescriptionItem(snapshot);
            RtcpPacket[] descriptionPackets = CreateSourceDescriptionPackets(
                snapshot.LocalSourceIdentifiers,
                snapshot.Configuration.CanonicalName,
                optionalItem);
            int descriptionIndex = 0;
            while (descriptionIndex < descriptionPackets.Length)
            {
                packets.Add(descriptionPackets[descriptionIndex]);
                descriptionIndex = descriptionIndex + 1;
            }

            RtcpCompoundPacket compound = new RtcpCompoundPacket();
            compound.Packets = packets.ToArray();
            AdvanceReportBlockCursor(receptionReports.Length);
            return compound;
        }
    }

    /// <summary>Builds a legal RTCP compound carrying one feedback message.</summary>
    /// <param name="snapshot">The independent session snapshot to report.</param>
    /// <param name="feedback">The feedback packet to include.</param>
    /// <param name="timingMode">The negotiated feedback emission mode.</param>
    /// <returns>The feedback compound packet.</returns>
    public RtcpCompoundPacket BuildFeedbackRtcpReport(
        RtpSessionSnapshot snapshot,
        RtcpFeedbackPacket feedback,
        RtcpFeedbackTimingMode timingMode)
    {
        ValidateFeedbackReportInput(snapshot, feedback, timingMode);
        lock (syncRoot)
        {
            RtcpFeedbackPacket retainedFeedback = feedback.Copy();
            uint reportingSourceIdentifier = GetFeedbackReportingSource(
                snapshot,
                retainedFeedback);
            retainedFeedback.SenderSourceIdentifier = reportingSourceIdentifier;
            ValidateFeedbackPacket(retainedFeedback);
            if (timingMode == RtcpFeedbackTimingMode.Regular)
            {
                return CreateFullFeedbackCompound(
                    snapshot,
                    retainedFeedback,
                    reportingSourceIdentifier);
            }

            if (timingMode == RtcpFeedbackTimingMode.ReducedSize)
            {
                ValidateReducedSizeFeedbackNegotiation(snapshot);
                return CreateReducedSizeFeedbackCompound(retainedFeedback);
            }

            if (snapshot.Configuration.RtcpCompoundMode == RtcpCompoundMode.Full)
            {
                return CreateFullFeedbackCompound(
                    snapshot,
                    retainedFeedback,
                    reportingSourceIdentifier);
            }

            return CreateMinimalFeedbackCompound(
                snapshot,
                retainedFeedback,
                reportingSourceIdentifier);
        }
    }

    /// <summary>Builds a legal RTCP compound carrying a goodbye packet.</summary>
    /// <param name="snapshot">The independent session snapshot to report.</param>
    /// <param name="reason">The optional UTF-8 goodbye reason bytes.</param>
    /// <returns>The goodbye compound packet.</returns>
    public RtcpCompoundPacket BuildGoodbyeRtcpReport(
        RtpSessionSnapshot snapshot,
        ReadOnlySpan<byte> reason)
    {
        ValidateRegularReportSnapshot(snapshot);
        if (reason.Length > RtcpProtocolConstants.MaximumTextLength)
        {
            throw new RtpConfigurationException(
                "The RTCP goodbye reason cannot exceed 255 bytes.");
        }

        if (!RtpKnownHeaderExtensionWireCodec.IsValidUtf8(reason))
        {
            throw new RtpConfigurationException(
                "The RTCP goodbye reason must be valid UTF-8.");
        }

        lock (syncRoot)
        {
            RtcpReceptionReport[] receptionReports =
                CreateReceptionReportBlocks(snapshot, reportBlockCursor);
            List<RtcpPacket> packets = new List<RtcpPacket>();
            int localIndex = 0;
            while (localIndex < snapshot.LocalSourceIdentifiers.Length)
            {
                uint localSourceIdentifier =
                    snapshot.LocalSourceIdentifiers[localIndex];
                FindLocalSource(snapshot, localSourceIdentifier);
                AddLocalReportPackets(
                    packets,
                    localSourceIdentifier,
                    null,
                    receptionReports,
                    0UL);
                localIndex = localIndex + 1;
            }

            RtcpSourceDescriptionItem? optionalItem =
                SelectOptionalSourceDescriptionItem(snapshot);
            RtcpPacket[] descriptionPackets =
                CreateSourceDescriptionPackets(
                    snapshot.LocalSourceIdentifiers,
                    snapshot.Configuration.CanonicalName,
                    optionalItem);
            int descriptionIndex = 0;
            while (descriptionIndex < descriptionPackets.Length)
            {
                packets.Add(descriptionPackets[descriptionIndex]);
                descriptionIndex = descriptionIndex + 1;
            }

            int sourceOffset = 0;
            while (sourceOffset < snapshot.LocalSourceIdentifiers.Length)
            {
                int sourceCount = snapshot.LocalSourceIdentifiers.Length -
                    sourceOffset;
                if (sourceCount > RtcpProtocolConstants.MaximumSourceCount)
                {
                    sourceCount = RtcpProtocolConstants.MaximumSourceCount;
                }

                RtcpGoodbye goodbye = new RtcpGoodbye();
                goodbye.Sources = new uint[sourceCount];
                int sourceIndex = 0;
                while (sourceIndex < sourceCount)
                {
                    goodbye.Sources[sourceIndex] =
                        snapshot.LocalSourceIdentifiers[
                            sourceOffset + sourceIndex];
                    sourceIndex = sourceIndex + 1;
                }

                bool isFinalGoodbye = sourceOffset + sourceCount ==
                    snapshot.LocalSourceIdentifiers.Length;
                if (isFinalGoodbye && reason.Length > 0)
                {
                    goodbye.HasReason = true;
                    goodbye.Reason = reason.ToArray();
                }

                RtcpPacket goodbyePacket = new RtcpPacket();
                goodbyePacket.Type = RtcpPacketType.Goodbye;
                goodbyePacket.Goodbye = goodbye;
                packets.Add(goodbyePacket);
                sourceOffset = sourceOffset + sourceCount;
            }

            RtcpCompoundPacket compound = new RtcpCompoundPacket();
            compound.Packets = packets.ToArray();
            AdvanceReportBlockCursor(receptionReports.Length);
            return compound;
        }
    }

    internal void ValidateRegularReportSnapshot(RtpSessionSnapshot snapshot)
    {
        if (snapshot == null || snapshot.Configuration == null)
        {
            throw new RtpConfigurationException(
                "An RTP session snapshot with configuration is required.");
        }

        if (snapshot.LocalSourceIdentifiers == null ||
            snapshot.LocalSourceIdentifiers.Length == 0)
        {
            throw new RtpConfigurationException(
                "A regular RTCP report requires one local source.");
        }

        if (snapshot.Sources == null)
        {
            throw new RtpConfigurationException(
                "The RTP session source snapshots are required.");
        }

        if (snapshot.Configuration.CanonicalName == null ||
            snapshot.Configuration.CanonicalName.Length == 0)
        {
            throw new RtpConfigurationException(
                "A regular RTCP report requires a canonical name.");
        }

        int canonicalNameLength = Encoding.UTF8.GetByteCount(
            snapshot.Configuration.CanonicalName);
        if (canonicalNameLength > RtcpProtocolConstants.MaximumTextLength)
        {
            throw new RtpConfigurationException(
                "The RTCP canonical name exceeds the wire length limit.");
        }

        RtpConfigurationValidator.ValidateOptionalSourceDescriptionItems(
            snapshot.Configuration.OptionalSourceDescriptionItems);

        if (snapshot.Configuration.OptionalSourceDescriptionItems.Length > 0 &&
            snapshot.AverageRtcpPacketSize <= 0)
        {
            if (snapshot.Configuration.RtcpScheduler == null ||
                snapshot.Configuration.RtcpScheduler.InitialAveragePacketSize <= 0)
            {
                throw new RtpConfigurationException(
                    "Optional source descriptions require a positive average RTCP packet size.");
            }
        }

        int sourceIndex = 0;
        while (sourceIndex < snapshot.Sources.Length)
        {
            if (snapshot.Sources[sourceIndex] == null)
            {
                throw new RtpConfigurationException(
                    "RTP session source snapshots cannot contain null entries.");
            }

            sourceIndex = sourceIndex + 1;
        }


        int localIndex = 0;
        while (localIndex < snapshot.LocalSourceIdentifiers.Length)
        {
            int comparisonIndex = localIndex + 1;
            while (comparisonIndex < snapshot.LocalSourceIdentifiers.Length)
            {
                if (snapshot.LocalSourceIdentifiers[comparisonIndex] ==
                    snapshot.LocalSourceIdentifiers[localIndex])
                {
                    throw new RtpConfigurationException(
                        "Local source identifiers cannot contain duplicates.");
                }

                comparisonIndex = comparisonIndex + 1;
            }

            localIndex = localIndex + 1;
        }
    }

    internal void ValidateFeedbackReportInput(
        RtpSessionSnapshot snapshot,
        RtcpFeedbackPacket feedback,
        RtcpFeedbackTimingMode timingMode)
    {
        ValidateRegularReportSnapshot(snapshot);
        if (feedback == null)
        {
            throw new RtpConfigurationException(
                "An RTCP feedback packet is required.");
        }

        if (timingMode != RtcpFeedbackTimingMode.Regular &&
            timingMode != RtcpFeedbackTimingMode.Early &&
            timingMode != RtcpFeedbackTimingMode.Immediate &&
            timingMode != RtcpFeedbackTimingMode.ReducedSize)
        {
            throw new RtpConfigurationException(
                "The feedback timing mode is not recognized.");
        }

        RtpConfigurationValidator.ValidateRtcpCompoundMode(
            snapshot.Configuration.RtcpCompoundMode);
    }

    internal uint GetFeedbackReportingSource(
        RtpSessionSnapshot snapshot,
        RtcpFeedbackPacket feedback)
    {
        if (feedback.SenderSourceIdentifier == 0)
        {
            return snapshot.LocalSourceIdentifiers[0];
        }

        int sourceIndex = 0;
        while (sourceIndex < snapshot.LocalSourceIdentifiers.Length)
        {
            if (snapshot.LocalSourceIdentifiers[sourceIndex] ==
                feedback.SenderSourceIdentifier)
            {
                return feedback.SenderSourceIdentifier;
            }

            sourceIndex = sourceIndex + 1;
        }

        throw new RtpConfigurationException(
            "The feedback sender SSRC is not owned by the local session.");
    }

    internal void ValidateFeedbackPacket(RtcpFeedbackPacket feedback)
    {
        RtcpPacketType packetType = RtcpPacketType.PayloadFeedback;
        if (feedback.PacketType == RtcpFeedbackPacketType.TransportLayer)
        {
            packetType = RtcpPacketType.TransportFeedback;
        }
        else if (feedback.PacketType != RtcpFeedbackPacketType.PayloadSpecific)
        {
            throw new RtcpPacketFormatException(
                "The RTCP feedback packet type is not recognized.");
        }

        string? validationError =
            RtcpFeedbackWireCodec.GetFeedbackValidationError(
                feedback,
                packetType);
        if (validationError != null)
        {
            throw new RtcpPacketFormatException(validationError);
        }
    }

    internal void ValidateReducedSizeFeedbackNegotiation(
        RtpSessionSnapshot snapshot)
    {
        if (snapshot.Configuration.RtcpCompoundMode !=
            RtcpCompoundMode.ReducedSize ||
            snapshot.Configuration.RtcpScheduler == null ||
            !snapshot.Configuration.RtcpScheduler.ReducedSizeEnabled)
        {
            throw new RtpConfigurationException(
                "Reduced-size feedback has not been negotiated.");
        }
    }

    internal RtcpCompoundPacket CreateFullFeedbackCompound(
        RtpSessionSnapshot snapshot,
        RtcpFeedbackPacket feedback,
        uint reportingSourceIdentifier)
    {
        RtcpReceptionReport[] receptionReports =
            CreateReceptionReportBlocks(snapshot, reportBlockCursor);
        List<RtcpPacket> packets = new List<RtcpPacket>();
        AddLocalReportPackets(
            packets,
            reportingSourceIdentifier,
            null,
            receptionReports,
            0UL);
        int localIndex = 0;
        while (localIndex < snapshot.LocalSourceIdentifiers.Length)
        {
            uint localSourceIdentifier =
                snapshot.LocalSourceIdentifiers[localIndex];
            FindLocalSource(snapshot, localSourceIdentifier);
            if (localSourceIdentifier != reportingSourceIdentifier)
            {
                AddLocalReportPackets(
                    packets,
                    localSourceIdentifier,
                    null,
                    receptionReports,
                    0UL);
            }

            localIndex = localIndex + 1;
        }

        RtcpSourceDescriptionItem? optionalItem =
            SelectOptionalSourceDescriptionItem(snapshot);
        RtcpPacket[] descriptionPackets = CreateSourceDescriptionPackets(
            snapshot.LocalSourceIdentifiers,
            snapshot.Configuration.CanonicalName,
            optionalItem);
        int descriptionIndex = 0;
        while (descriptionIndex < descriptionPackets.Length)
        {
            packets.Add(descriptionPackets[descriptionIndex]);
            descriptionIndex = descriptionIndex + 1;
        }

        packets.Add(CreateFeedbackPacket(feedback));
        RtcpCompoundPacket compound = new RtcpCompoundPacket();
        compound.Packets = packets.ToArray();
        AdvanceReportBlockCursor(receptionReports.Length);
        return compound;
    }

    internal RtcpCompoundPacket CreateMinimalFeedbackCompound(
        RtpSessionSnapshot snapshot,
        RtcpFeedbackPacket feedback,
        uint reportingSourceIdentifier)
    {
        RtcpPacket reportPacket = CreateReceiverReportPacket(
            reportingSourceIdentifier,
            []);
        RtcpPacket[] descriptionPackets = CreateSourceDescriptionPackets(
            new uint[] { reportingSourceIdentifier },
            snapshot.Configuration.CanonicalName,
            null);
        RtcpCompoundPacket compound = new RtcpCompoundPacket();
        compound.Packets = new RtcpPacket[]
        {
            reportPacket,
            descriptionPackets[0],
            CreateFeedbackPacket(feedback)
        };
        return compound;
    }

    internal RtcpCompoundPacket CreateReducedSizeFeedbackCompound(
        RtcpFeedbackPacket feedback)
    {
        RtcpCompoundPacket compound = new RtcpCompoundPacket();
        compound.Packets = new RtcpPacket[]
        {
            CreateFeedbackPacket(feedback)
        };
        return compound;
    }

    internal RtcpPacket CreateFeedbackPacket(RtcpFeedbackPacket feedback)
    {
        RtcpPacket packet = new RtcpPacket();
        if (feedback.PacketType == RtcpFeedbackPacketType.TransportLayer)
        {
            packet.Type = RtcpPacketType.TransportFeedback;
        }
        else
        {
            packet.Type = RtcpPacketType.PayloadFeedback;
        }

        packet.Feedback = feedback.Copy();
        return packet;
    }

    internal RtpSourceSnapshot? FindLocalSource(
        RtpSessionSnapshot snapshot,
        uint localSourceIdentifier)
    {
        RtpSourceSnapshot? localSource = null;
        int index = 0;
        while (index < snapshot.Sources.Length)
        {
            RtpSourceSnapshot source = snapshot.Sources[index];
            if (source.SourceIdentifier == localSourceIdentifier)
            {
                if (!source.IsLocal)
                {
                    throw new RtpConfigurationException(
                        "The configured local SSRC is not marked as local.");
                }

                if (localSource != null)
                {
                    throw new RtpConfigurationException(
                        "The local SSRC appears more than once in the source snapshot.");
                }

                localSource = source;
            }

            index = index + 1;
        }

        return localSource;
    }

    internal RtcpReceptionReport[] CreateReceptionReportBlocks(
        RtpSessionSnapshot snapshot,
        int startIndex)
    {
        List<RtcpReceptionReport> reports = new List<RtcpReceptionReport>();
        int sourceIndex = 0;
        while (sourceIndex < snapshot.Sources.Length)
        {
            RtpSourceSnapshot source = snapshot.Sources[sourceIndex];
            if (!source.IsLocal && source.ReceptionStatistics != null)
            {
                if (source.ReceptionStatistics.SourceIdentifier !=
                    source.SourceIdentifier)
                {
                    throw new RtpConfigurationException(
                        "Reception statistics must identify their source snapshot.");
                }

                reports.Add(CreateReceptionReportBlock(
                    source.ReceptionStatistics));
            }

            sourceIndex = sourceIndex + 1;
        }

        if (reports.Count == 0)
        {
            return [];
        }

        int normalizedStartIndex = startIndex % reports.Count;
        RtcpReceptionReport[] rotatedReports =
            new RtcpReceptionReport[reports.Count];
        int reportIndex = 0;
        while (reportIndex < reports.Count)
        {
            int sourceReportIndex = normalizedStartIndex + reportIndex;
            if (sourceReportIndex >= reports.Count)
            {
                sourceReportIndex = sourceReportIndex - reports.Count;
            }

            rotatedReports[reportIndex] = reports[sourceReportIndex];
            reportIndex = reportIndex + 1;
        }

        return rotatedReports;
    }

    internal void AddLocalReportPackets(
        List<RtcpPacket> packets,
        uint localSourceIdentifier,
        RtpSourceSnapshot? localSource,
        RtcpReceptionReport[] receptionReports,
        ulong ntpTimestamp)
    {
        int firstCount = receptionReports.Length;
        if (firstCount > RtcpProtocolConstants.MaximumReceptionReportCount)
        {
            firstCount = RtcpProtocolConstants.MaximumReceptionReportCount;
        }

        RtcpReceptionReport[] firstReports = CreateReceptionReportSlice(
            receptionReports,
            0,
            firstCount);
        packets.Add(CreatePrimaryReportPacket(
            localSourceIdentifier,
            localSource,
            firstReports,
            ntpTimestamp));
        int reportOffset = firstCount;
        while (reportOffset < receptionReports.Length)
        {
            int reportCount = receptionReports.Length - reportOffset;
            if (reportCount > RtcpProtocolConstants.MaximumReceptionReportCount)
            {
                reportCount = RtcpProtocolConstants.MaximumReceptionReportCount;
            }

            RtcpReceptionReport[] reportSlice = CreateReceptionReportSlice(
                receptionReports,
                reportOffset,
                reportCount);
            packets.Add(CreateReceiverReportPacket(
                localSourceIdentifier,
                reportSlice));
            reportOffset = reportOffset + reportCount;
        }
    }

    internal RtcpReceptionReport[] CreateReceptionReportSlice(
        RtcpReceptionReport[] reports,
        int offset,
        int count)
    {
        RtcpReceptionReport[] slice = new RtcpReceptionReport[count];
        int index = 0;
        while (index < count)
        {
            slice[index] = reports[offset + index].Copy();
            index = index + 1;
        }

        return slice;
    }

    internal RtcpPacket CreateReceiverReportPacket(
        uint localSourceIdentifier,
        RtcpReceptionReport[] receptionReports)
    {
        RtcpReceiverReport receiverReport = new RtcpReceiverReport();
        receiverReport.SenderSynchronizationSource = localSourceIdentifier;
        receiverReport.ReceptionReports = receptionReports;
        RtcpPacket receiverPacket = new RtcpPacket();
        receiverPacket.Type = RtcpPacketType.ReceiverReport;
        receiverPacket.ReceiverReport = receiverReport;
        return receiverPacket;
    }

    internal RtcpReceptionReport CreateReceptionReportBlock(
        RtpReceptionStatisticsSnapshot statistics)
    {
        RtcpReceptionReport report = new RtcpReceptionReport();
        report.SourceIdentifier = statistics.SourceIdentifier;
        report.FractionLost = statistics.FractionLost;
        report.CumulativePacketsLost = statistics.CumulativePacketsLost;
        report.ExtendedHighestSequenceNumber =
            statistics.HighestExtendedSequenceNumber;
        report.InterarrivalJitter = statistics.InterarrivalJitter;
        report.LastSenderReport = statistics.LastSenderReport;
        report.DelaySinceLastSenderReport =
            statistics.DelaySinceLastSenderReport;
        return report;
    }

    internal RtcpPacket CreatePrimaryReportPacket(
        uint localSourceIdentifier,
        RtpSourceSnapshot? localSource,
        RtcpReceptionReport[] receptionReports,
        ulong ntpTimestamp)
    {
        RtpTransmissionStatisticsSnapshot? transmissionStatistics = null;
        if (localSource != null)
        {
            transmissionStatistics = localSource.TransmissionStatistics;
        }

        if (transmissionStatistics != null &&
            transmissionStatistics.SourceIdentifier != localSourceIdentifier)
        {
            throw new RtpConfigurationException(
                "Transmission statistics must identify the local source snapshot.");
        }

        if (transmissionStatistics != null &&
            transmissionStatistics.IsActiveSender)
        {
            if (!transmissionStatistics.SenderReportEligible)
            {
                throw new RtpConfigurationException(
                    "An active sender lacks sender-report timing data.");
            }

            RtcpSenderReport senderReport = new RtcpSenderReport();
            senderReport.SenderSynchronizationSource = localSourceIdentifier;
            senderReport.SenderInformation = CreateSenderInformation(
                transmissionStatistics,
                ntpTimestamp);
            senderReport.ReceptionReports = receptionReports;
            RtcpPacket senderPacket = new RtcpPacket();
            senderPacket.Type = RtcpPacketType.SenderReport;
            senderPacket.SenderReport = senderReport;
            return senderPacket;
        }

        return CreateReceiverReportPacket(
            localSourceIdentifier,
            receptionReports);
    }

    internal RtcpSenderInformation CreateSenderInformation(
        RtpTransmissionStatisticsSnapshot statistics,
        ulong ntpTimestamp)
    {
        if (statistics.SentPacketCount < 0 || statistics.SentOctetCount < 0)
        {
            throw new RtpConfigurationException(
                "Sender packet and octet counts cannot be negative.");
        }

        RtcpSenderInformation information = new RtcpSenderInformation();
        information.NtpTimestamp = ntpTimestamp;
        information.RtpTimestamp = statistics.LastRtpTimestamp;
        information.PacketCount = RtpTransmissionStatistics.GetRfcSenderCount(
            statistics.SentPacketCount);
        information.OctetCount = RtpTransmissionStatistics.GetRfcSenderCount(
            statistics.SentOctetCount);
        return information;
    }

    internal RtcpPacket[] CreateSourceDescriptionPackets(
        uint[] localSourceIdentifiers,
        string canonicalName,
        RtcpSourceDescriptionItem? optionalItem)
    {
        List<RtcpPacket> packets = new List<RtcpPacket>();
        int localIndex = 0;
        while (localIndex < localSourceIdentifiers.Length)
        {
            int chunkCount = localSourceIdentifiers.Length - localIndex;
            if (chunkCount > RtcpProtocolConstants.MaximumSourceCount)
            {
                chunkCount = RtcpProtocolConstants.MaximumSourceCount;
            }

            RtcpSourceDescriptionChunk[] chunks =
                new RtcpSourceDescriptionChunk[chunkCount];
            int chunkIndex = 0;
            while (chunkIndex < chunkCount)
            {
                RtcpSourceDescriptionItem item =
                    new RtcpSourceDescriptionItem();
                item.Type = RtcpSdesItemType.CanonicalName;
                item.Value = Encoding.UTF8.GetBytes(canonicalName);
                RtcpSourceDescriptionChunk chunk =
                    new RtcpSourceDescriptionChunk();
                chunk.SourceIdentifier =
                    localSourceIdentifiers[localIndex + chunkIndex];
                if (optionalItem != null && localIndex == 0 && chunkIndex == 0)
                {
                    chunk.Items = new RtcpSourceDescriptionItem[]
                    {
                        item,
                        optionalItem.Copy()
                    };
                }
                else
                {
                    chunk.Items = new RtcpSourceDescriptionItem[] { item };
                }

                chunks[chunkIndex] = chunk;
                chunkIndex = chunkIndex + 1;
            }

            RtcpSourceDescription description = new RtcpSourceDescription();
            description.Chunks = chunks;
            RtcpPacket packet = new RtcpPacket();
            packet.Type = RtcpPacketType.SourceDescription;
            packet.SourceDescription = description;
            packets.Add(packet);
            localIndex = localIndex + chunkCount;
        }

        return packets.ToArray();
    }

    internal RtcpSourceDescriptionItem? SelectOptionalSourceDescriptionItem(
        RtpSessionSnapshot snapshot)
    {
        RtcpSourceDescriptionItem[] items =
            snapshot.Configuration.OptionalSourceDescriptionItems;
        if (items.Length == 0)
        {
            sourceDescriptionItemCursor = 0;
            optionalSourceDescriptionCredit = 0.0;
            return null;
        }

        if (sourceDescriptionItemCursor >= items.Length)
        {
            sourceDescriptionItemCursor = sourceDescriptionItemCursor % items.Length;
        }

        int averagePacketSize = snapshot.AverageRtcpPacketSize;
        if (averagePacketSize <= 0)
        {
            averagePacketSize =
                snapshot.Configuration.RtcpScheduler.InitialAveragePacketSize;
        }

        optionalSourceDescriptionCredit = optionalSourceDescriptionCredit +
            averagePacketSize *
            RtcpProtocolConstants.OptionalSourceDescriptionBandwidthFraction;
        int largestItemCost = GetLargestOptionalSourceDescriptionCost(
            items,
            snapshot.Configuration.CanonicalName);
        if (optionalSourceDescriptionCredit > largestItemCost)
        {
            optionalSourceDescriptionCredit = largestItemCost;
        }

        RtcpSourceDescriptionItem selectedItem =
            items[sourceDescriptionItemCursor];
        int selectedItemCost = GetOptionalSourceDescriptionCost(
            selectedItem,
            snapshot.Configuration.CanonicalName);
        if (optionalSourceDescriptionCredit < selectedItemCost)
        {
            return null;
        }

        optionalSourceDescriptionCredit = optionalSourceDescriptionCredit -
            selectedItemCost;
        sourceDescriptionItemCursor = sourceDescriptionItemCursor + 1;
        if (sourceDescriptionItemCursor == items.Length)
        {
            sourceDescriptionItemCursor = 0;
        }

        return selectedItem.Copy();
    }

    internal int GetLargestOptionalSourceDescriptionCost(
        RtcpSourceDescriptionItem[] items,
        string canonicalName)
    {
        int largestCost = 0;
        int itemIndex = 0;
        while (itemIndex < items.Length)
        {
            int itemCost = GetOptionalSourceDescriptionCost(
                items[itemIndex],
                canonicalName);
            if (itemCost > largestCost)
            {
                largestCost = itemCost;
            }

            itemIndex = itemIndex + 1;
        }

        return largestCost;
    }

    internal int GetOptionalSourceDescriptionCost(
        RtcpSourceDescriptionItem item,
        string canonicalName)
    {
        int canonicalNameLength = Encoding.UTF8.GetByteCount(canonicalName);
        int mandatoryChunkSize = AlignSourceDescriptionChunkSize(
            RtcpProtocolConstants.SourceIdentifierSize + 1 +
            RtcpProtocolConstants.SourceDescriptionItemHeaderSize +
            canonicalNameLength);
        int optionalChunkSize = AlignSourceDescriptionChunkSize(
            RtcpProtocolConstants.SourceIdentifierSize + 1 +
            RtcpProtocolConstants.SourceDescriptionItemHeaderSize +
            canonicalNameLength +
            RtcpSourceDescriptionWireCodec.CalculateSourceDescriptionItemSize(item));
        return optionalChunkSize - mandatoryChunkSize;
    }

    internal int AlignSourceDescriptionChunkSize(int size)
    {
        int remainder = size % RtcpProtocolConstants.LengthWordSize;
        if (remainder == 0)
        {
            return size;
        }

        return size + RtcpProtocolConstants.LengthWordSize - remainder;
    }

    internal void AdvanceReportBlockCursor(int reportCount)
    {
        if (reportCount <= RtcpProtocolConstants.MaximumReceptionReportCount)
        {
            reportBlockCursor = 0;
            return;
        }

        long nextCursor = (long)reportBlockCursor +
            RtcpProtocolConstants.MaximumReceptionReportCount;
        reportBlockCursor = (int)(nextCursor % reportCount);
    }
}
