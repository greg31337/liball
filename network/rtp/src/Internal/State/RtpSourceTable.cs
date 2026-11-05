using System.Collections.Generic;
using System.Text;

namespace rtp;

internal class RtpSourceTable
{
    internal object syncRoot = new object();
    internal List<RtpSourceState> sources = new List<RtpSourceState>();
    internal RtpSessionConfiguration sequenceConfiguration;
    internal IRtpRandomSource randomSource;
    internal int maximumSourceCount;
    internal int maximumPacketSize;
    internal bool strictValidation;
    internal long senderTimeoutTicks;
    internal long memberTimeoutTicks;
    internal bool hasMaintenanceTime;
    internal long maintenanceTimeTicks;
    internal IRtpPayloadTypeRegistry? payloadTypeRegistry;

    internal RtpSourceTable(
        RtpSessionConfiguration configuration,
        IRtpRandomSource randomSource)
    {
        RtpConfigurationValidator.ValidateRtpSourceTableConfiguration(
            configuration);
        if (randomSource == null)
        {
            throw new RtpConfigurationException(
                "An RTP source-table random source is required.");
        }

        sequenceConfiguration = new RtpSessionConfiguration();
        sequenceConfiguration.MinimumSequential =
            configuration.MinimumSequential;
        sequenceConfiguration.JitterBuffer =
            configuration.JitterBuffer.Copy();
        this.randomSource = randomSource;
        maximumSourceCount = configuration.MaximumSourceCount;
        maximumPacketSize = configuration.MaximumRtpPacketSize;
        strictValidation = configuration.StrictValidation;
        senderTimeoutTicks = configuration.RtcpScheduler.SenderTimeoutTicks;
        memberTimeoutTicks = configuration.RtcpScheduler.MemberTimeoutTicks;
    }

    internal void ConfigureRemotePayloadTypeRegistry(
        IRtpPayloadTypeRegistry payloadTypeRegistry)
    {
        if (payloadTypeRegistry == null)
        {
            throw new RtpConfigurationException(
                "A remote RTP payload-type registry is required.");
        }

        lock (syncRoot)
        {
            this.payloadTypeRegistry = payloadTypeRegistry;
        }
    }

    internal RtpSequenceUpdate RecordRemoteRtpPacket(
        RtpPacket packet,
        RtpPacketContext context)
    {
        ValidateRemoteRtpPacket(packet, context);
        return RecordRemoteRtpPacketState(packet, context, null);
    }

    internal RtpSequenceUpdate RecordMappedRemoteRtpPacket(
        RtpPacket packet,
        RtpPacketContext context)
    {
        RtpPacketEncoder.GetEncodedSize(
            packet,
            strictValidation,
            maximumPacketSize);
        ValidateRemoteActivityContext(context);
        IRtpPayloadTypeRegistry? registry = payloadTypeRegistry;
        if (registry == null)
        {
            throw new RtpConfigurationException(
                "A remote RTP payload-type registry has not been configured.");
        }

        RtpPayloadTypeDefinition? payloadDefinition =
            registry.FindPayloadTypeDefinition(packet.PayloadType);
        if (payloadDefinition == null)
        {
            throw new RtpSessionException(
                "The remote RTP payload type is not registered.");
        }

        RtpConfigurationValidator.ValidatePayloadTypeDefinition(
            payloadDefinition);
        if (payloadDefinition.PayloadType != packet.PayloadType)
        {
            throw new RtpSessionException(
                "The remote RTP payload mapping does not match the packet payload type.");
        }

        RtpPacketContext mappedContext = context.Copy();
        mappedContext.ClockRate = payloadDefinition.ClockRate;
        ValidateRemoteRtpPacket(packet, mappedContext);
        return RecordRemoteRtpPacketState(
            packet,
            mappedContext,
            payloadDefinition);
    }

    internal RtpSequenceUpdate RecordRemoteRtpPacketState(
        RtpPacket packet,
        RtpPacketContext context,
        RtpPayloadTypeDefinition? payloadDefinition)
    {
        lock (syncRoot)
        {
            ValidateMaintenanceChronology(context.ArrivalTimeTicks);
            ValidatePacketSourceChronology(packet, context.ArrivalTimeTicks);
            EnsurePacketSourceCapacity(packet);
            RtpSourceState? source = FindSourceState(
                packet.SynchronizationSource);
            if (source == null)
            {
                source = CreateSourceState(
                    packet.SynchronizationSource,
                    RtpSourceStatus.Probationary);
                InsertSourceState(source);
            }

            RtpSequenceUpdate sequenceUpdate =
                source.sequenceTracker.ProcessRtpSequenceNumber(
                    packet.SequenceNumber);
            RecordPrimaryRtpActivity(
                source,
                packet,
                context,
                sequenceUpdate,
                payloadDefinition);
            if (sequenceUpdate.Accepted)
            {
                source.timestampUpdate =
                    source.timestampTracker.ProcessRtpTimestamp(
                        packet.Timestamp,
                        context.ClockRate);
                source.isSender = true;
                source.status = RtpSourceStatus.ActiveSender;
                DiscoverContributingSources(
                    packet,
                    context,
                    payloadDefinition);
            }

            return sequenceUpdate.Copy();
        }
    }

    internal RtpSourceSnapshot RecordRemoteRtpReception(
        RtpPacket packet,
        RtpPacketContext context,
        RtpSequenceUpdate sequenceUpdate)
    {
        lock (syncRoot)
        {
            RtpSourceState? source = FindSourceState(
                packet.SynchronizationSource);
            if (source == null)
            {
                throw new RtpSessionException(
                    "The inbound RTP source state was not created.");
            }

            if (sequenceUpdate.Accepted ||
                sequenceUpdate.Status == RtpSequenceStatus.Duplicate)
            {
                source.receptionStatistics.RecordReceivedRtpPacket(
                    packet,
                    context,
                    sequenceUpdate);
            }
            else
            {
                source.receptionStatistics.RecordDiscardedRtpPacket(
                    context);
            }

            return source.CreateSourceStatisticsSnapshot(
                context.ArrivalTimeTicks);
        }
    }

    internal RtpSourceSnapshot RecordRemoteRtcpActivity(
        uint sourceIdentifier,
        RtpPacketContext context)
    {
        ValidateRemoteActivityContext(context);
        lock (syncRoot)
        {
            ValidateMaintenanceChronology(context.ArrivalTimeTicks);
            RtpSourceState? source = FindSourceState(sourceIdentifier);
            if (source == null)
            {
                EnsureAdditionalSourceCapacity(1);
                source = CreateSourceState(
                    sourceIdentifier,
                    RtpSourceStatus.ActiveMember);
                InsertSourceState(source);
            }
            else
            {
                ValidateSourceChronology(source, context.ArrivalTimeTicks);
                if (source.status == RtpSourceStatus.Probationary)
                {
                    source.status = RtpSourceStatus.ActiveMember;
                }
            }

            source.hasRtcpActivity = true;
            source.lastRtcpActivityTicks = context.ArrivalTimeTicks;
            source.transportIdentity = context.TransportIdentity;
            return source.CreateSourceSnapshot();
        }
    }

    internal RtpSourceSnapshot RecordRemoteExtendedReport(
        RtcpExtendedReport report,
        RtpPacketContext context)
    {
        string? reportError =
            RtcpExtendedReportWireCodec.GetExtendedReportValidationError(
                report);
        if (reportError != null)
        {
            throw new RtcpPacketFormatException(reportError);
        }

        ValidateRemoteActivityContext(context);
        lock (syncRoot)
        {
            ValidateMaintenanceChronology(context.ArrivalTimeTicks);
            RtpSourceState? source = FindSourceState(
                report.SenderSynchronizationSource);
            if (source == null)
            {
                EnsureAdditionalSourceCapacity(1);
                source = CreateSourceState(
                    report.SenderSynchronizationSource,
                    RtpSourceStatus.ActiveMember);
                InsertSourceState(source);
            }
            else
            {
                ValidateSourceChronology(source, context.ArrivalTimeTicks);
                if (source.status == RtpSourceStatus.Probationary)
                {
                    source.status = RtpSourceStatus.ActiveMember;
                }
            }

            source.hasRtcpActivity = true;
            source.lastRtcpActivityTicks = context.ArrivalTimeTicks;
            source.transportIdentity = context.TransportIdentity;
            source.extendedReportBlocks =
                new RtcpExtendedReportBlock[report.Blocks.Length];
            int blockIndex = 0;
            while (blockIndex < report.Blocks.Length)
            {
                source.extendedReportBlocks[blockIndex] =
                    report.Blocks[blockIndex].Copy();
                blockIndex = blockIndex + 1;
            }

            source.lastExtendedReportTimeTicks = context.ArrivalTimeTicks;
            return source.CreateSourceSnapshot();
        }
    }

    internal RtpSourceSnapshot[] RecordRemoteSourceDescription(
        RtcpSourceDescription description,
        RtpPacketContext context)
    {
        ValidateRemoteSourceDescription(description);
        ValidateRemoteActivityContext(context);
        lock (syncRoot)
        {
            ValidateMaintenanceChronology(context.ArrivalTimeTicks);
            ValidateSourceDescriptionChronology(
                description,
                context.ArrivalTimeTicks);
            EnsureSourceDescriptionCapacity(description);
            RtpSourceSnapshot[] snapshots =
                new RtpSourceSnapshot[description.Chunks.Length];
            int chunkIndex = 0;
            while (chunkIndex < description.Chunks.Length)
            {
                RtcpSourceDescriptionChunk chunk =
                    description.Chunks[chunkIndex];
                RtpSourceState? source = FindSourceState(
                    chunk.SourceIdentifier);
                if (source == null)
                {
                    source = CreateSourceState(
                        chunk.SourceIdentifier,
                        RtpSourceStatus.ActiveMember);
                    InsertSourceState(source);
                }
                else if (source.status == RtpSourceStatus.Probationary)
                {
                    source.status = RtpSourceStatus.ActiveMember;
                }

                ApplySourceDescriptionChunk(source, chunk, context);
                snapshots[chunkIndex] = source.CreateSourceSnapshot();
                chunkIndex = chunkIndex + 1;
            }

            return snapshots;
        }
    }

    internal RtpSourceSnapshot[] RecordRemoteGoodbye(
        RtcpGoodbye goodbye,
        RtpPacketContext context)
    {
        ValidateRemoteGoodbye(goodbye);
        ValidateRemoteActivityContext(context);
        lock (syncRoot)
        {
            ValidateMaintenanceChronology(context.ArrivalTimeTicks);
            ValidateGoodbyeChronology(goodbye, context.ArrivalTimeTicks);
            List<RtpSourceSnapshot> snapshots =
                new List<RtpSourceSnapshot>();
            int sourceIndex = 0;
            while (sourceIndex < sources.Count)
            {
                RtpSourceState source = sources[sourceIndex];
                if (ContainsSourceIdentifier(
                    goodbye.Sources,
                    source.sourceIdentifier))
                {
                    source.status = RtpSourceStatus.Goodbye;
                    source.isSender = false;
                    source.hasRtcpActivity = true;
                    source.lastRtcpActivityTicks = context.ArrivalTimeTicks;
                    source.transportIdentity = context.TransportIdentity;
                    source.goodbyeReason = (byte[])goodbye.Reason.Clone();
                    snapshots.Add(source.CreateSourceSnapshot());
                }

                sourceIndex = sourceIndex + 1;
            }

            return snapshots.ToArray();
        }
    }

    internal RtpSourceSnapshot[] FindRemoteSourcesByCanonicalName(
        string canonicalName)
    {
        if (canonicalName == null || canonicalName.Length == 0)
        {
            throw new RtpConfigurationException(
                "A remote source canonical name is required.");
        }

        lock (syncRoot)
        {
            List<RtpSourceSnapshot> snapshots =
                new List<RtpSourceSnapshot>();
            int sourceIndex = 0;
            while (sourceIndex < sources.Count)
            {
                RtpSourceState source = sources[sourceIndex];
                if (source.canonicalName == canonicalName)
                {
                    snapshots.Add(source.CreateSourceSnapshot());
                }

                sourceIndex = sourceIndex + 1;
            }

            return snapshots.ToArray();
        }
    }

    internal int RemoveGoodbyeSources()
    {
        lock (syncRoot)
        {
            int removedCount = 0;
            int sourceIndex = sources.Count - 1;
            while (sourceIndex >= 0)
            {
                if (sources[sourceIndex].status == RtpSourceStatus.Goodbye)
                {
                    sources.RemoveAt(sourceIndex);
                    removedCount = removedCount + 1;
                }

                sourceIndex = sourceIndex - 1;
            }

            return removedCount;
        }
    }

    internal RtpSourceSnapshot[] ExpireRemoteSourceActivity(
        long currentTimeTicks)
    {
        if (currentTimeTicks < 0)
        {
            throw new RtpConfigurationException(
                "Remote source maintenance time cannot be negative.");
        }

        lock (syncRoot)
        {
            ValidateMaintenanceChronology(currentTimeTicks);
            ValidateAllSourceChronology(currentTimeTicks);
            List<RtpSourceSnapshot> changes =
                new List<RtpSourceSnapshot>();
            int sourceIndex = 0;
            while (sourceIndex < sources.Count)
            {
                RtpSourceState source = sources[sourceIndex];
                long latestActivityTicks = GetLatestSourceActivity(source);
                if (source.status != RtpSourceStatus.Goodbye &&
                    memberTimeoutTicks > 0L &&
                    currentTimeTicks - latestActivityTicks >=
                    memberTimeoutTicks)
                {
                    source.status = RtpSourceStatus.Inactive;
                    source.isSender = false;
                    changes.Add(source.CreateSourceSnapshot());
                    sources.RemoveAt(sourceIndex);
                }
                else
                {
                    if (source.isSender &&
                        senderTimeoutTicks > 0L &&
                        source.hasRtpActivity &&
                        currentTimeTicks - source.lastRtpActivityTicks >=
                        senderTimeoutTicks)
                    {
                        source.status = RtpSourceStatus.ActiveMember;
                        source.isSender = false;
                        changes.Add(source.CreateSourceSnapshot());
                    }

                    sourceIndex = sourceIndex + 1;
                }
            }

            hasMaintenanceTime = true;
            maintenanceTimeTicks = currentTimeTicks;
            return changes.ToArray();
        }
    }

    internal RtpSourceSnapshot UpdateRemoteSenderState(
        uint sourceIdentifier,
        bool isSender)
    {
        lock (syncRoot)
        {
            RtpSourceState? source = FindSourceState(sourceIdentifier);
            if (source == null)
            {
                throw new RtpSessionException(
                    "The remote RTP source is not known.");
            }

            source.isSender = isSender;
            if (isSender)
            {
                source.status = RtpSourceStatus.ActiveSender;
            }
            else
            {
                source.status = RtpSourceStatus.ActiveMember;
            }

            return source.CreateSourceSnapshot();
        }
    }

    internal RtpSourceSnapshot? FindRemoteSourceSnapshot(
        uint sourceIdentifier)
    {
        lock (syncRoot)
        {
            RtpSourceState? source = FindSourceState(sourceIdentifier);
            if (source == null)
            {
                return null;
            }

            return source.CreateSourceSnapshot();
        }
    }

    internal RtpSourceSnapshot[] GetRemoteSourceSnapshots()
    {
        lock (syncRoot)
        {
            RtpSourceSnapshot[] snapshots =
                new RtpSourceSnapshot[sources.Count];
            int sourceIndex = 0;
            while (sourceIndex < sources.Count)
            {
                snapshots[sourceIndex] =
                    sources[sourceIndex].CreateSourceSnapshot();
                sourceIndex = sourceIndex + 1;
            }

            return snapshots;
        }
    }

    internal RtpSourceSnapshot[] GetRemoteSourceStatisticsSnapshots(
        long currentTimeTicks)
    {
        if (currentTimeTicks < 0)
        {
            throw new RtpConfigurationException(
                "Remote source snapshot time cannot be negative.");
        }

        lock (syncRoot)
        {
            ValidateMaintenanceChronology(currentTimeTicks);
            ValidateAllSourceChronology(currentTimeTicks);
            RtpSourceSnapshot[] snapshots =
                new RtpSourceSnapshot[sources.Count];
            int sourceIndex = 0;
            while (sourceIndex < sources.Count)
            {
                RtpSourceState source = sources[sourceIndex];
                if (source.hasRtpActivity)
                {
                    snapshots[sourceIndex] =
                        source.CreateSourceStatisticsSnapshot(
                            currentTimeTicks);
                }
                else
                {
                    snapshots[sourceIndex] = source.CreateSourceSnapshot();
                }

                sourceIndex = sourceIndex + 1;
            }

            return snapshots;
        }
    }

    internal void AdvanceRemoteReceptionReportIntervals(
        long currentTimeTicks)
    {
        if (currentTimeTicks < 0)
        {
            throw new RtpConfigurationException(
                "Remote reception report time cannot be negative.");
        }

        lock (syncRoot)
        {
            ValidateMaintenanceChronology(currentTimeTicks);
            ValidateAllSourceChronology(currentTimeTicks);
            int sourceIndex = 0;
            while (sourceIndex < sources.Count)
            {
                RtpSourceState source = sources[sourceIndex];
                if (source.hasRtpActivity &&
                    (source.status == RtpSourceStatus.ActiveMember ||
                    source.status == RtpSourceStatus.ActiveSender))
                {
                    source.receptionStatistics.BuildRtcpReceptionReport(
                        currentTimeTicks);
                }

                sourceIndex = sourceIndex + 1;
            }
        }
    }

    internal uint[] GetRemoteSourceIdentifiers()
    {
        lock (syncRoot)
        {
            uint[] sourceIdentifiers = new uint[sources.Count];
            int sourceIndex = 0;
            while (sourceIndex < sources.Count)
            {
                sourceIdentifiers[sourceIndex] =
                    sources[sourceIndex].sourceIdentifier;
                sourceIndex = sourceIndex + 1;
            }

            return sourceIdentifiers;
        }
    }

    internal bool CanRetainRemoteSourceIdentifiers(
        uint[] sourceIdentifiers)
    {
        if (sourceIdentifiers == null)
        {
            throw new RtpConfigurationException(
                "The RTP remote source identifier array is required.");
        }

        lock (syncRoot)
        {
            int additionalSourceCount = 0;
            int sourceIndex = 0;
            while (sourceIndex < sourceIdentifiers.Length)
            {
                uint sourceIdentifier = sourceIdentifiers[sourceIndex];
                if (FindSourceState(sourceIdentifier) == null &&
                    !IsEarlierContributingSource(
                        sourceIdentifiers,
                        sourceIndex,
                        sourceIdentifier))
                {
                    additionalSourceCount = additionalSourceCount + 1;
                }

                sourceIndex = sourceIndex + 1;
            }

            return sources.Count <=
                maximumSourceCount - additionalSourceCount;
        }
    }

    internal RtpSourceSnapshot MarkRemoteSourceCollision(
        uint sourceIdentifier,
        string collisionTransportIdentity)
    {
        if (collisionTransportIdentity == null ||
            collisionTransportIdentity.Length == 0)
        {
            throw new RtpConfigurationException(
                "A colliding remote transport identity is required.");
        }

        lock (syncRoot)
        {
            RtpSourceState? source = FindSourceState(sourceIdentifier);
            if (source == null)
            {
                throw new RtpSessionException(
                    "The colliding remote RTP source is not known.");
            }

            source.status = RtpSourceStatus.Collision;
            source.isSender = false;
            source.collisionTransportIdentity =
                collisionTransportIdentity;
            return source.CreateSourceSnapshot();
        }
    }

    internal int GetRemoteMemberCount()
    {
        lock (syncRoot)
        {
            int memberCount = 0;
            int sourceIndex = 0;
            while (sourceIndex < sources.Count)
            {
                if (sources[sourceIndex].status ==
                    RtpSourceStatus.ActiveMember ||
                    sources[sourceIndex].status ==
                    RtpSourceStatus.ActiveSender)
                {
                    memberCount = memberCount + 1;
                }

                sourceIndex = sourceIndex + 1;
            }

            return memberCount;
        }
    }

    internal int GetRemoteSenderCount()
    {
        lock (syncRoot)
        {
            int senderCount = 0;
            int sourceIndex = 0;
            while (sourceIndex < sources.Count)
            {
                if (sources[sourceIndex].isSender)
                {
                    senderCount = senderCount + 1;
                }

                sourceIndex = sourceIndex + 1;
            }

            return senderCount;
        }
    }

    internal void ClearRemoteSources()
    {
        lock (syncRoot)
        {
            sources.Clear();
            hasMaintenanceTime = false;
            maintenanceTimeTicks = 0L;
        }
    }

    internal void ValidateRemoteRtpPacket(
        RtpPacket packet,
        RtpPacketContext context)
    {
        RtpPacketEncoder.GetEncodedSize(
            packet,
            strictValidation,
            maximumPacketSize);
        ValidateRemoteActivityContext(context);
        if (context.ClockRate <= 0)
        {
            throw new RtpConfigurationException(
                "The remote RTP packet clock rate must be positive.");
        }
    }

    internal void ValidateRemoteActivityContext(RtpPacketContext context)
    {
        if (context == null || context.TransportIdentity == null)
        {
            throw new RtpConfigurationException(
                "Remote RTP source activity context is required.");
        }

        if (context.ArrivalTimeTicks < 0)
        {
            throw new RtpConfigurationException(
                "Remote RTP source activity time cannot be negative.");
        }
    }

    internal void ValidateRemoteSourceDescription(
        RtcpSourceDescription description)
    {
        string? validationError =
            RtcpSourceDescriptionWireCodec.GetSourceDescriptionValidationError(
                description);
        if (validationError != null)
        {
            throw new RtcpPacketFormatException(validationError);
        }

        int chunkIndex = 0;
        while (chunkIndex < description.Chunks.Length)
        {
            RtcpSourceDescriptionChunk chunk =
                description.Chunks[chunkIndex];
            int comparisonIndex = 0;
            while (comparisonIndex < chunkIndex)
            {
                if (description.Chunks[comparisonIndex].SourceIdentifier ==
                    chunk.SourceIdentifier)
                {
                    throw new RtcpPacketFormatException(
                        "An RTCP source-description packet cannot repeat a source.");
                }

                comparisonIndex = comparisonIndex + 1;
            }

            int canonicalNameCount = 0;
            int itemIndex = 0;
            while (itemIndex < chunk.Items.Length)
            {
                RtcpSourceDescriptionItem item = chunk.Items[itemIndex];
                if (item.Type == RtcpSdesItemType.CanonicalName)
                {
                    canonicalNameCount = canonicalNameCount + 1;
                    if (!RtpKnownHeaderExtensionWireCodec.IsValidUtf8(
                        item.Value))
                    {
                        throw new RtcpPacketFormatException(
                            "An RTCP canonical name is not valid UTF-8.");
                    }
                }

                itemIndex = itemIndex + 1;
            }

            if (canonicalNameCount > 1)
            {
                throw new RtcpPacketFormatException(
                    "An RTCP source-description chunk cannot repeat its canonical name.");
            }

            chunkIndex = chunkIndex + 1;
        }
    }

    internal void ValidateRemoteGoodbye(RtcpGoodbye goodbye)
    {
        string? validationError =
            RtcpGoodbyeWireCodec.GetGoodbyeValidationError(goodbye);
        if (validationError != null)
        {
            throw new RtcpPacketFormatException(validationError);
        }
    }

    internal void ValidateMaintenanceChronology(long activityTimeTicks)
    {
        if (hasMaintenanceTime && activityTimeTicks < maintenanceTimeTicks)
        {
            throw new RtpSessionException(
                "Remote RTP source activity time cannot precede maintenance.");
        }
    }

    internal void ValidateAllSourceChronology(long activityTimeTicks)
    {
        int sourceIndex = 0;
        while (sourceIndex < sources.Count)
        {
            ValidateSourceChronology(
                sources[sourceIndex],
                activityTimeTicks);
            sourceIndex = sourceIndex + 1;
        }
    }

    internal void ValidateSourceDescriptionChronology(
        RtcpSourceDescription description,
        long activityTimeTicks)
    {
        int chunkIndex = 0;
        while (chunkIndex < description.Chunks.Length)
        {
            RtpSourceState? source = FindSourceState(
                description.Chunks[chunkIndex].SourceIdentifier);
            if (source != null)
            {
                ValidateSourceChronology(source, activityTimeTicks);
            }

            chunkIndex = chunkIndex + 1;
        }
    }

    internal void ValidateGoodbyeChronology(
        RtcpGoodbye goodbye,
        long activityTimeTicks)
    {
        int sourceIndex = 0;
        while (sourceIndex < goodbye.Sources.Length)
        {
            RtpSourceState? source = FindSourceState(
                goodbye.Sources[sourceIndex]);
            if (source != null)
            {
                ValidateSourceChronology(source, activityTimeTicks);
            }

            sourceIndex = sourceIndex + 1;
        }
    }

    internal void ValidatePacketSourceChronology(
        RtpPacket packet,
        long activityTimeTicks)
    {
        RtpSourceState? primarySource = FindSourceState(
            packet.SynchronizationSource);
        if (primarySource != null)
        {
            ValidateSourceChronology(primarySource, activityTimeTicks);
        }

        int sourceIndex = 0;
        while (sourceIndex < packet.ContributingSources.Length)
        {
            RtpSourceState? contributingSource = FindSourceState(
                packet.ContributingSources[sourceIndex]);
            if (contributingSource != null)
            {
                ValidateSourceChronology(
                    contributingSource,
                    activityTimeTicks);
            }

            sourceIndex = sourceIndex + 1;
        }
    }

    internal void ValidateSourceChronology(
        RtpSourceState source,
        long activityTimeTicks)
    {
        if (source.hasRtpActivity &&
            activityTimeTicks < source.lastRtpActivityTicks)
        {
            throw new RtpSessionException(
                "Remote RTP source activity time cannot move backward.");
        }

        if (source.hasRtcpActivity &&
            activityTimeTicks < source.lastRtcpActivityTicks)
        {
            throw new RtpSessionException(
                "Remote RTP source activity time cannot move backward.");
        }
    }

    internal void EnsurePacketSourceCapacity(RtpPacket packet)
    {
        int additionalSourceCount = 0;
        if (FindSourceState(packet.SynchronizationSource) == null)
        {
            additionalSourceCount = 1;
        }

        int sourceIndex = 0;
        while (sourceIndex < packet.ContributingSources.Length)
        {
            uint sourceIdentifier = packet.ContributingSources[sourceIndex];
            if (sourceIdentifier != packet.SynchronizationSource &&
                FindSourceState(sourceIdentifier) == null &&
                !IsEarlierContributingSource(
                    packet.ContributingSources,
                    sourceIndex,
                    sourceIdentifier))
            {
                additionalSourceCount = additionalSourceCount + 1;
            }

            sourceIndex = sourceIndex + 1;
        }

        EnsureAdditionalSourceCapacity(additionalSourceCount);
    }

    internal void EnsureSourceDescriptionCapacity(
        RtcpSourceDescription description)
    {
        int additionalSourceCount = 0;
        int chunkIndex = 0;
        while (chunkIndex < description.Chunks.Length)
        {
            if (FindSourceState(
                description.Chunks[chunkIndex].SourceIdentifier) == null)
            {
                additionalSourceCount = additionalSourceCount + 1;
            }

            chunkIndex = chunkIndex + 1;
        }

        EnsureAdditionalSourceCapacity(additionalSourceCount);
    }

    internal bool ContainsSourceIdentifier(
        uint[] sourceIdentifiers,
        uint sourceIdentifier)
    {
        int sourceIndex = 0;
        while (sourceIndex < sourceIdentifiers.Length)
        {
            if (sourceIdentifiers[sourceIndex] == sourceIdentifier)
            {
                return true;
            }

            sourceIndex = sourceIndex + 1;
        }

        return false;
    }

    internal bool IsEarlierContributingSource(
        uint[] sourceIdentifiers,
        int sourceIndex,
        uint sourceIdentifier)
    {
        int comparisonIndex = 0;
        while (comparisonIndex < sourceIndex)
        {
            if (sourceIdentifiers[comparisonIndex] == sourceIdentifier)
            {
                return true;
            }

            comparisonIndex = comparisonIndex + 1;
        }

        return false;
    }

    internal void EnsureAdditionalSourceCapacity(int additionalSourceCount)
    {
        if (sources.Count > maximumSourceCount - additionalSourceCount)
        {
            throw new RtpBufferCapacityException(
                "The RTP remote source table capacity has been reached.");
        }
    }

    internal RtpSourceState CreateSourceState(
        uint sourceIdentifier,
        RtpSourceStatus status)
    {
        RtpSequenceTracker sequenceTracker = new RtpSequenceTracker(
            sequenceConfiguration,
            randomSource);
        return new RtpSourceState(
            sourceIdentifier,
            status,
            sequenceTracker);
    }

    internal void InsertSourceState(RtpSourceState source)
    {
        int insertionIndex = 0;
        while (insertionIndex < sources.Count &&
            sources[insertionIndex].sourceIdentifier <
            source.sourceIdentifier)
        {
            insertionIndex = insertionIndex + 1;
        }

        sources.Insert(insertionIndex, source);
    }

    internal RtpSourceState? FindSourceState(uint sourceIdentifier)
    {
        int sourceIndex = 0;
        while (sourceIndex < sources.Count)
        {
            if (sources[sourceIndex].sourceIdentifier == sourceIdentifier)
            {
                return sources[sourceIndex];
            }

            sourceIndex = sourceIndex + 1;
        }

        return null;
    }

    internal void ApplySourceDescriptionChunk(
        RtpSourceState source,
        RtcpSourceDescriptionChunk chunk,
        RtpPacketContext context)
    {
        source.sourceDescriptionItems =
            new RtcpSourceDescriptionItem[chunk.Items.Length];
        int itemIndex = 0;
        while (itemIndex < chunk.Items.Length)
        {
            RtcpSourceDescriptionItem item = chunk.Items[itemIndex];
            source.sourceDescriptionItems[itemIndex] = item.Copy();
            if (item.Type == RtcpSdesItemType.CanonicalName)
            {
                source.canonicalName = Encoding.UTF8.GetString(item.Value);
            }

            itemIndex = itemIndex + 1;
        }

        source.hasRtcpActivity = true;
        source.lastRtcpActivityTicks = context.ArrivalTimeTicks;
        source.transportIdentity = context.TransportIdentity;
    }

    internal long GetLatestSourceActivity(RtpSourceState source)
    {
        long latestActivityTicks = source.lastRtpActivityTicks;
        if (source.hasRtcpActivity &&
            (!source.hasRtpActivity ||
            source.lastRtcpActivityTicks > latestActivityTicks))
        {
            latestActivityTicks = source.lastRtcpActivityTicks;
        }

        return latestActivityTicks;
    }

    internal void RecordPrimaryRtpActivity(
        RtpSourceState source,
        RtpPacket packet,
        RtpPacketContext context,
        RtpSequenceUpdate sequenceUpdate,
        RtpPayloadTypeDefinition? payloadDefinition)
    {
        source.hasRtpActivity = true;
        source.lastRtpActivityTicks = context.ArrivalTimeTicks;
        source.transportIdentity = context.TransportIdentity;
        source.payloadType = packet.PayloadType;
        source.clockRate = context.ClockRate;
        source.sequenceUpdate = sequenceUpdate.Copy();
        ApplyRemotePayloadDefinition(source, payloadDefinition);
    }

    internal void DiscoverContributingSources(
        RtpPacket packet,
        RtpPacketContext context,
        RtpPayloadTypeDefinition? payloadDefinition)
    {
        int sourceIndex = 0;
        while (sourceIndex < packet.ContributingSources.Length)
        {
            uint sourceIdentifier = packet.ContributingSources[sourceIndex];
            if (sourceIdentifier != packet.SynchronizationSource)
            {
                RtpSourceState? source = FindSourceState(sourceIdentifier);
                if (source == null)
                {
                    source = CreateSourceState(
                        sourceIdentifier,
                        RtpSourceStatus.ActiveMember);
                    InsertSourceState(source);
                }

                source.hasRtpActivity = true;
                source.lastRtpActivityTicks = context.ArrivalTimeTicks;
                source.transportIdentity = context.TransportIdentity;
                source.payloadType = packet.PayloadType;
                source.clockRate = context.ClockRate;
                ApplyRemotePayloadDefinition(source, payloadDefinition);
            }

            sourceIndex = sourceIndex + 1;
        }
    }

    internal void ApplyRemotePayloadDefinition(
        RtpSourceState source,
        RtpPayloadTypeDefinition? payloadDefinition)
    {
        if (payloadDefinition != null)
        {
            source.mediaIdentity = payloadDefinition.MediaIdentity;
            source.channelCount = payloadDefinition.ChannelCount;
        }
        else
        {
            source.mediaIdentity = "";
            source.channelCount = 0;
        }
    }
}
