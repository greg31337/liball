using System.Collections.Generic;

namespace rtp;

/// <summary>Retains validated RTP packets in bounded extended-sequence order.</summary>
public class RtpJitterBuffer : IRtpJitterBuffer
{
    internal object syncRoot = new object();
    internal RtpJitterBufferConfiguration configuration;
    internal List<RtpPacketHeap> sourcePacketHeaps =
        new List<RtpPacketHeap>();
    internal bool hasCurrentTime;
    internal long currentTimeTicks;
    internal long releasedPacketCount;
    internal long duplicatePacketCount;
    internal long latePacketCount;
    internal long lostPacketCount;
    internal long overflowPacketCount;
    internal long recoveredPacketCount;

    /// <summary>Initializes a jitter buffer from isolated configuration.</summary>
    /// <param name="configuration">The packet capacity and reorder settings.</param>
    public RtpJitterBuffer(RtpJitterBufferConfiguration configuration)
    {
        RtpConfigurationValidator.ValidateRtpJitterBufferConfiguration(
            configuration);
        this.configuration = configuration.Copy();
    }

    /// <summary>Adds one validated packet or throws when its source is full.</summary>
    /// <param name="packet">The packet to retain.</param>
    /// <param name="context">The packet timing and repair context.</param>
    /// <param name="sequenceUpdate">The corresponding extended sequence update.</param>
    /// <returns>The buffer update produced by the packet.</returns>
    public RtpJitterBufferUpdate AddRtpJitterBufferPacket(
        RtpPacket packet,
        RtpPacketContext context,
        RtpSequenceUpdate sequenceUpdate)
    {
        return AddBufferedPacket(packet, context, sequenceUpdate, true);
    }

    /// <summary>Attempts to add one packet with deterministic overflow eviction.</summary>
    /// <param name="packet">The packet to retain.</param>
    /// <param name="context">The packet timing and repair context.</param>
    /// <param name="sequenceUpdate">The corresponding extended sequence update.</param>
    /// <returns>The buffer update, including overflow when capacity was reached.</returns>
    public RtpJitterBufferUpdate TryAddRtpJitterBufferPacket(
        RtpPacket packet,
        RtpPacketContext context,
        RtpSequenceUpdate sequenceUpdate)
    {
        return AddBufferedPacket(packet, context, sequenceUpdate, false);
    }

    /// <summary>Polls buffered gaps and packets at a monotonic time.</summary>
    /// <param name="currentTimeTicks">The current monotonic time.</param>
    /// <returns>The resulting releases, losses, and repair requests.</returns>
    public RtpJitterBufferUpdate PollRtpJitterBuffer(long currentTimeTicks)
    {
        ValidateJitterBufferTime(currentTimeTicks);
        lock (syncRoot)
        {
            ValidateJitterBufferChronology(currentTimeTicks);
            RecordJitterBufferTime(currentTimeTicks);
            List<RtpPacket> releasedPackets = new List<RtpPacket>();
            List<RtpPacket> recoveredPackets = new List<RtpPacket>();
            List<ushort> lostSequenceNumbers = new List<ushort>();
            List<RtpRepairRequest> repairRequests =
                new List<RtpRepairRequest>();
            int sourceIndex = 0;
            while (sourceIndex < sourcePacketHeaps.Count)
            {
                ExpireSourceGap(
                    sourcePacketHeaps[sourceIndex],
                    currentTimeTicks,
                    releasedPackets,
                    lostSequenceNumbers);
                AppendSourceRepairRequests(
                    sourcePacketHeaps[sourceIndex],
                    currentTimeTicks,
                    repairRequests);
                sourceIndex = sourceIndex + 1;
            }

            return CreateOperationUpdate(
                GetReleaseStatus(releasedPackets, lostSequenceNumbers),
                releasedPackets,
                recoveredPackets,
                lostSequenceNumbers,
                repairRequests);
        }
    }

    /// <summary>Releases every currently contiguous buffered packet.</summary>
    /// <returns>The packets released without finalizing all gaps.</returns>
    public RtpJitterBufferUpdate FlushRtpJitterBuffer()
    {
        lock (syncRoot)
        {
            List<RtpPacket> releasedPackets = new List<RtpPacket>();
            List<RtpPacket> recoveredPackets = new List<RtpPacket>();
            List<ushort> lostSequenceNumbers = new List<ushort>();
            List<RtpRepairRequest> repairRequests =
                new List<RtpRepairRequest>();
            int sourceIndex = 0;
            while (sourceIndex < sourcePacketHeaps.Count)
            {
                RtpPacketHeap sourceHeap = sourcePacketHeaps[sourceIndex];
                ReleaseContiguousPackets(sourceHeap, releasedPackets);
                RefreshSourceGap(sourceHeap);
                if (hasCurrentTime)
                {
                    AppendSourceRepairRequests(
                        sourceHeap,
                        currentTimeTicks,
                        repairRequests);
                }

                sourceIndex = sourceIndex + 1;
            }

            return CreateOperationUpdate(
                GetReleaseStatus(releasedPackets, lostSequenceNumbers),
                releasedPackets,
                recoveredPackets,
                lostSequenceNumbers,
                repairRequests);
        }
    }

    /// <summary>Finalizes gaps and releases every retained packet.</summary>
    /// <returns>The drained packets and finalized losses.</returns>
    public RtpJitterBufferUpdate DrainRtpJitterBuffer()
    {
        lock (syncRoot)
        {
            List<RtpPacket> releasedPackets = new List<RtpPacket>();
            List<RtpPacket> recoveredPackets = new List<RtpPacket>();
            List<ushort> lostSequenceNumbers = new List<ushort>();
            List<RtpRepairRequest> repairRequests =
                new List<RtpRepairRequest>();
            int sourceIndex = 0;
            while (sourceIndex < sourcePacketHeaps.Count)
            {
                RtpPacketHeap sourceHeap = sourcePacketHeaps[sourceIndex];
                while (sourceHeap.Entries.Count > 0)
                {
                    FinalizeSourceGap(
                        sourceHeap,
                        releasedPackets,
                        lostSequenceNumbers);
                }

                sourceHeap.HasOpenGap = false;
                sourceHeap.GapStartTimeTicks = 0L;
                sourceHeap.MissingPackets.Clear();
                sourceIndex = sourceIndex + 1;
            }

            return CreateOperationUpdate(
                GetReleaseStatus(releasedPackets, lostSequenceNumbers),
                releasedPackets,
                recoveredPackets,
                lostSequenceNumbers,
                repairRequests);
        }
    }

    /// <summary>Clears all buffered packets, gaps, and repair state.</summary>
    /// <returns>The reset buffer update.</returns>
    public RtpJitterBufferUpdate ResetRtpJitterBuffer()
    {
        lock (syncRoot)
        {
            sourcePacketHeaps.Clear();
            hasCurrentTime = false;
            currentTimeTicks = 0L;
            releasedPacketCount = 0L;
            duplicatePacketCount = 0L;
            latePacketCount = 0L;
            lostPacketCount = 0L;
            overflowPacketCount = 0L;
            recoveredPacketCount = 0L;
            return CreateJitterBufferUpdate(RtpJitterBufferStatus.Reset);
        }
    }

    internal RtpJitterBufferUpdate AddBufferedPacket(
        RtpPacket packet,
        RtpPacketContext context,
        RtpSequenceUpdate sequenceUpdate,
        bool throwOnOverflow)
    {
        ValidateJitterBufferPacket(packet, context, sequenceUpdate);
        lock (syncRoot)
        {
            ValidateJitterBufferChronology(context.ArrivalTimeTicks);
            List<RtpPacket> releasedPackets = new List<RtpPacket>();
            List<RtpPacket> recoveredPackets = new List<RtpPacket>();
            List<ushort> lostSequenceNumbers = new List<ushort>();
            List<RtpRepairRequest> repairRequests =
                new List<RtpRepairRequest>();
            if (!sequenceUpdate.Accepted &&
                sequenceUpdate.Status == RtpSequenceStatus.Duplicate)
            {
                RecordJitterBufferTime(context.ArrivalTimeTicks);
                return CreateOperationUpdate(
                    RtpJitterBufferStatus.Duplicate,
                    releasedPackets,
                    recoveredPackets,
                    lostSequenceNumbers,
                    repairRequests);
            }

            RtpPacketHeap? sourceHeap = FindSourcePacketHeap(
                packet.SynchronizationSource);
            bool resetForDiscontinuity = false;
            if (sourceHeap != null &&
                sourceHeap.FindExtendedSequenceIndex(
                    sequenceUpdate.ExtendedSequenceNumber) >= 0)
            {
                RecordJitterBufferTime(context.ArrivalTimeTicks);
                return CreateOperationUpdate(
                    RtpJitterBufferStatus.Duplicate,
                    releasedPackets,
                    recoveredPackets,
                    lostSequenceNumbers,
                    repairRequests);
            }

            bool resetsSource = sequenceUpdate.Status ==
                RtpSequenceStatus.Restarted &&
                configuration.ResetOnDiscontinuity;
            if (sourceHeap != null && !resetsSource &&
                sourceHeap.Entries.Count == configuration.Capacity &&
                throwOnOverflow &&
                !IsPacketAtReleaseSequence(sourceHeap, sequenceUpdate))
            {
                throw new RtpBufferCapacityException(
                    "The RTP jitter buffer source capacity has been reached.");
            }

            RecordJitterBufferTime(context.ArrivalTimeTicks);

            if (sourceHeap == null)
            {
                sourceHeap = new RtpPacketHeap();
                sourceHeap.SourceIdentifier = packet.SynchronizationSource;
                sourcePacketHeaps.Add(sourceHeap);
            }

            if (resetsSource)
            {
                sourceHeap.ClearBufferedEntries();
                resetForDiscontinuity = true;
            }

            InitializeSourceReleaseSequence(sourceHeap, sequenceUpdate);
            ExpireSourceGap(
                sourceHeap,
                context.ArrivalTimeTicks,
                releasedPackets,
                lostSequenceNumbers);
            bool isLate = context.IsLate ||
                IsPacketBeforeReleaseSequence(sourceHeap, sequenceUpdate);
            if (isLate)
            {
                if (configuration.ReleaseLatePackets)
                {
                    releasedPackets.Add(packet.Copy());
                    AddRecoveredPacketEvent(
                        packet,
                        context,
                        recoveredPackets);
                }

                AppendSourceRepairRequests(
                    sourceHeap,
                    context.ArrivalTimeTicks,
                    repairRequests);

                return CreateOperationUpdate(
                    RtpJitterBufferStatus.Late,
                    releasedPackets,
                    recoveredPackets,
                    lostSequenceNumbers,
                    repairRequests);
            }

            RtpJitterBufferEntry entry = CreateJitterBufferEntry(
                packet,
                context,
                sequenceUpdate);
            bool immediatelyReleasable =
                IsPacketAtReleaseSequence(sourceHeap, sequenceUpdate);
            if (sourceHeap.Entries.Count < configuration.Capacity)
            {
                sourceHeap.InsertBufferedEntry(entry);
                AddRecoveredPacketEvent(packet, context, recoveredPackets);
                ReleaseContiguousPackets(sourceHeap, releasedPackets);
                RefreshSourceGap(sourceHeap);
                RtpJitterBufferStatus status = GetReleaseStatus(
                    releasedPackets,
                    lostSequenceNumbers);
                if (resetForDiscontinuity)
                {
                    status = RtpJitterBufferStatus.Reset;
                }

                AppendSourceRepairRequests(
                    sourceHeap,
                    context.ArrivalTimeTicks,
                    repairRequests);

                return CreateOperationUpdate(
                    status,
                    releasedPackets,
                    recoveredPackets,
                    lostSequenceNumbers,
                    repairRequests);
            }

            if (immediatelyReleasable)
            {
                sourceHeap.InsertBufferedEntry(entry);
                AddRecoveredPacketEvent(packet, context, recoveredPackets);
                ReleaseContiguousPackets(sourceHeap, releasedPackets);
                RefreshSourceGap(sourceHeap);
                AppendSourceRepairRequests(
                    sourceHeap,
                    context.ArrivalTimeTicks,
                    repairRequests);
                return CreateOperationUpdate(
                    GetReleaseStatus(releasedPackets, lostSequenceNumbers),
                    releasedPackets,
                    recoveredPackets,
                    lostSequenceNumbers,
                    repairRequests);
            }

            int largestIndex = sourceHeap.Entries.Count - 1;
            uint largestExtendedSequenceNumber = sourceHeap.Entries[largestIndex]
                .SequenceUpdate.ExtendedSequenceNumber;
            if (sequenceUpdate.ExtendedSequenceNumber <
                largestExtendedSequenceNumber)
            {
                sourceHeap.RemoveBufferedEntry(largestIndex);
                sourceHeap.InsertBufferedEntry(entry);
                AddRecoveredPacketEvent(packet, context, recoveredPackets);
            }

            RefreshSourceGap(sourceHeap);
            AppendSourceRepairRequests(
                sourceHeap,
                context.ArrivalTimeTicks,
                repairRequests);
            return CreateOperationUpdate(
                RtpJitterBufferStatus.Overflow,
                releasedPackets,
                recoveredPackets,
                lostSequenceNumbers,
                repairRequests);
        }
    }

    internal void ValidateJitterBufferPacket(
        RtpPacket packet,
        RtpPacketContext context,
        RtpSequenceUpdate sequenceUpdate)
    {
        string? packetError = RtpPacketEncoder.GetPacketValidationError(
            packet,
            true);
        if (packetError != null)
        {
            throw new RtpPacketFormatException(packetError);
        }

        if (context == null)
        {
            throw new RtpConfigurationException(
                "RTP packet context is required by the jitter buffer.");
        }

        if (context.ArrivalTimeTicks < 0)
        {
            throw new RtpConfigurationException(
                "RTP packet arrival time cannot be negative.");
        }

        if (sequenceUpdate == null)
        {
            throw new RtpConfigurationException(
                "An RTP sequence update is required by the jitter buffer.");
        }

        if (sequenceUpdate.SequenceNumber != packet.SequenceNumber ||
            (ushort)(sequenceUpdate.ExtendedSequenceNumber &
            RtpProtocolConstants.MaximumSequenceNumber) !=
            packet.SequenceNumber)
        {
            throw new RtpConfigurationException(
                "The RTP sequence update does not identify the packet.");
        }

        if (!sequenceUpdate.Accepted &&
            sequenceUpdate.Status != RtpSequenceStatus.Duplicate)
        {
            throw new RtpConfigurationException(
                "The RTP sequence update has not accepted the packet.");
        }

        if (sequenceUpdate.Accepted &&
            sequenceUpdate.Status != RtpSequenceStatus.Accepted &&
            sequenceUpdate.Status != RtpSequenceStatus.Reordered &&
            sequenceUpdate.Status != RtpSequenceStatus.Restarted)
        {
            throw new RtpConfigurationException(
                "The accepted RTP sequence status is not recognized.");
        }
    }

    internal void InitializeSourceReleaseSequence(
        RtpPacketHeap sourceHeap,
        RtpSequenceUpdate sequenceUpdate)
    {
        if (!sourceHeap.HasReleaseSequence)
        {
            sourceHeap.HasReleaseSequence = true;
            sourceHeap.ReleaseSequenceExhausted = false;
            sourceHeap.NextReleaseExtendedSequenceNumber =
                sequenceUpdate.BaseExtendedSequenceNumber;
        }
    }

    internal bool IsPacketBeforeReleaseSequence(
        RtpPacketHeap sourceHeap,
        RtpSequenceUpdate sequenceUpdate)
    {
        if (!sourceHeap.HasReleaseSequence)
        {
            return false;
        }

        if (sourceHeap.ReleaseSequenceExhausted)
        {
            return true;
        }

        return sequenceUpdate.ExtendedSequenceNumber <
            sourceHeap.NextReleaseExtendedSequenceNumber;
    }

    internal bool IsPacketAtReleaseSequence(
        RtpPacketHeap sourceHeap,
        RtpSequenceUpdate sequenceUpdate)
    {
        return sourceHeap.HasReleaseSequence &&
            !sourceHeap.ReleaseSequenceExhausted &&
            sequenceUpdate.ExtendedSequenceNumber ==
            sourceHeap.NextReleaseExtendedSequenceNumber;
    }

    internal void ReleaseContiguousPackets(
        RtpPacketHeap sourceHeap,
        List<RtpPacket> releasedPackets)
    {
        while (sourceHeap.Entries.Count > 0 &&
            sourceHeap.HasReleaseSequence &&
            !sourceHeap.ReleaseSequenceExhausted &&
            sourceHeap.Entries[0].SequenceUpdate.ExtendedSequenceNumber ==
            sourceHeap.NextReleaseExtendedSequenceNumber)
        {
            RtpJitterBufferEntry entry = sourceHeap.Entries[0];
            releasedPackets.Add(entry.Packet.Copy());
            sourceHeap.RemoveBufferedEntry(0);
            AdvanceSourceReleaseSequence(sourceHeap);
        }
    }

    internal void AdvanceSourceReleaseSequence(RtpPacketHeap sourceHeap)
    {
        if (sourceHeap.NextReleaseExtendedSequenceNumber == uint.MaxValue)
        {
            sourceHeap.ReleaseSequenceExhausted = true;
            return;
        }

        sourceHeap.NextReleaseExtendedSequenceNumber =
            sourceHeap.NextReleaseExtendedSequenceNumber + 1U;
    }

    internal void RefreshSourceGap(RtpPacketHeap sourceHeap)
    {
        bool hasGap = sourceHeap.Entries.Count > 0 &&
            sourceHeap.HasReleaseSequence &&
            !sourceHeap.ReleaseSequenceExhausted &&
            sourceHeap.Entries[0].SequenceUpdate.ExtendedSequenceNumber >
            sourceHeap.NextReleaseExtendedSequenceNumber;
        if (!hasGap)
        {
            sourceHeap.HasOpenGap = false;
            sourceHeap.GapExtendedSequenceNumber = 0U;
            sourceHeap.GapStartTimeTicks = 0L;
            return;
        }

        if (!sourceHeap.HasOpenGap ||
            sourceHeap.GapExtendedSequenceNumber !=
            sourceHeap.NextReleaseExtendedSequenceNumber)
        {
            sourceHeap.HasOpenGap = true;
            sourceHeap.GapExtendedSequenceNumber =
                sourceHeap.NextReleaseExtendedSequenceNumber;
            sourceHeap.GapStartTimeTicks =
                sourceHeap.Entries[0].Context.ArrivalTimeTicks;
        }
    }

    internal void ExpireSourceGap(
        RtpPacketHeap sourceHeap,
        long operationTimeTicks,
        List<RtpPacket> releasedPackets,
        List<ushort> lostSequenceNumbers)
    {
        RefreshSourceGap(sourceHeap);
        if (configuration.MaximumLatencyTicks == 0)
        {
            return;
        }

        bool inspectGap = sourceHeap.HasOpenGap;
        while (inspectGap)
        {
            long deadlineTicks = long.MaxValue;
            if (sourceHeap.GapStartTimeTicks <=
                long.MaxValue - configuration.MaximumLatencyTicks)
            {
                deadlineTicks = sourceHeap.GapStartTimeTicks +
                    configuration.MaximumLatencyTicks;
            }

            if (operationTimeTicks < deadlineTicks)
            {
                return;
            }

            sourceHeap.HasOpenGap = false;
            FinalizeSourceGap(
                sourceHeap,
                releasedPackets,
                lostSequenceNumbers);
            RefreshSourceGap(sourceHeap);
            inspectGap = sourceHeap.HasOpenGap;
        }
    }

    internal void FinalizeSourceGap(
        RtpPacketHeap sourceHeap,
        List<RtpPacket> releasedPackets,
        List<ushort> lostSequenceNumbers)
    {
        if (sourceHeap.Entries.Count == 0 ||
            !sourceHeap.HasReleaseSequence ||
            sourceHeap.ReleaseSequenceExhausted)
        {
            return;
        }

        uint firstBufferedSequenceNumber = sourceHeap.Entries[0]
            .SequenceUpdate.ExtendedSequenceNumber;
        while (!sourceHeap.ReleaseSequenceExhausted &&
            sourceHeap.NextReleaseExtendedSequenceNumber <
            firstBufferedSequenceNumber)
        {
            lostSequenceNumbers.Add((ushort)(
                sourceHeap.NextReleaseExtendedSequenceNumber &
                RtpProtocolConstants.MaximumSequenceNumber));
            sourceHeap.RemoveMissingPacketState(
                sourceHeap.NextReleaseExtendedSequenceNumber);
            AdvanceSourceReleaseSequence(sourceHeap);
        }

        ReleaseContiguousPackets(sourceHeap, releasedPackets);
    }

    internal void AppendSourceRepairRequests(
        RtpPacketHeap sourceHeap,
        long operationTimeTicks,
        List<RtpRepairRequest> repairRequests)
    {
        SynchronizeMissingPacketStates(sourceHeap, operationTimeTicks);
        int missingIndex = 0;
        while (missingIndex < sourceHeap.MissingPackets.Count)
        {
            RtpMissingPacketState state =
                sourceHeap.MissingPackets[missingIndex];
            if (IsMissingPacketRequestDue(state, operationTimeTicks))
            {
                state.AttemptCount = state.AttemptCount + 1;
                state.LastRequestTimeTicks = operationTimeTicks;
                RtpRepairRequest request = new RtpRepairRequest();
                request.Kind = RtpRepairKind.Retransmission;
                request.SourceIdentifier = sourceHeap.SourceIdentifier;
                request.SequenceNumbers =
                    new ushort[] { (ushort)(state.ExtendedSequenceNumber &
                    RtpProtocolConstants.MaximumSequenceNumber) };
                request.AttemptCount = state.AttemptCount;
                request.RequestTimeTicks = operationTimeTicks;
                request.DeadlineTicks = GetMissingPacketDeadline(state);
                repairRequests.Add(request);
            }

            missingIndex = missingIndex + 1;
        }
    }

    internal void SynchronizeMissingPacketStates(
        RtpPacketHeap sourceHeap,
        long operationTimeTicks)
    {
        if (configuration.MaximumNackRetryCount == 0 ||
            sourceHeap.Entries.Count == 0 ||
            !sourceHeap.HasReleaseSequence ||
            sourceHeap.ReleaseSequenceExhausted)
        {
            sourceHeap.MissingPackets.Clear();
            return;
        }

        uint highestBufferedSequenceNumber = sourceHeap.Entries[
            sourceHeap.Entries.Count - 1]
            .SequenceUpdate.ExtendedSequenceNumber;
        int missingIndex = 0;
        while (missingIndex < sourceHeap.MissingPackets.Count)
        {
            uint missingSequenceNumber = sourceHeap.MissingPackets[
                missingIndex].ExtendedSequenceNumber;
            bool stateIsCurrent = missingSequenceNumber >=
                sourceHeap.NextReleaseExtendedSequenceNumber &&
                missingSequenceNumber <= highestBufferedSequenceNumber &&
                sourceHeap.FindExtendedSequenceIndex(
                    missingSequenceNumber) < 0;
            if (stateIsCurrent)
            {
                missingIndex = missingIndex + 1;
            }
            else
            {
                sourceHeap.MissingPackets.RemoveAt(missingIndex);
            }
        }

        uint candidateSequenceNumber =
            sourceHeap.NextReleaseExtendedSequenceNumber;
        long candidateCount = (long)highestBufferedSequenceNumber -
            (long)candidateSequenceNumber + 1L;
        if (candidateCount > configuration.MaximumDropout)
        {
            candidateCount = configuration.MaximumDropout;
        }

        while (candidateCount > 0)
        {
            if (sourceHeap.FindExtendedSequenceIndex(
                candidateSequenceNumber) < 0 &&
                sourceHeap.FindMissingPacketIndex(
                candidateSequenceNumber) < 0)
            {
                RtpMissingPacketState state =
                    new RtpMissingPacketState();
                state.ExtendedSequenceNumber = candidateSequenceNumber;
                state.MissingSinceTicks = operationTimeTicks;
                sourceHeap.InsertMissingPacketState(state);
            }

            if (candidateSequenceNumber == uint.MaxValue)
            {
                return;
            }

            candidateSequenceNumber = candidateSequenceNumber + 1U;
            candidateCount = candidateCount - 1L;
        }
    }

    internal bool IsMissingPacketRequestDue(
        RtpMissingPacketState state,
        long operationTimeTicks)
    {
        if (state.AttemptCount >= configuration.MaximumNackRetryCount ||
            operationTimeTicks > GetMissingPacketDeadline(state))
        {
            return false;
        }

        long requestTimeTicks;
        if (state.AttemptCount == 0)
        {
            requestTimeTicks = AddSaturatedJitterBufferTime(
                state.MissingSinceTicks,
                configuration.NackDelayTicks);
        }
        else
        {
            requestTimeTicks = AddSaturatedJitterBufferTime(
                state.LastRequestTimeTicks,
                configuration.NackRetryIntervalTicks);
        }

        return operationTimeTicks >= requestTimeTicks;
    }

    internal long GetMissingPacketDeadline(RtpMissingPacketState state)
    {
        long deadlineTicks = AddSaturatedJitterBufferTime(
            state.MissingSinceTicks,
            configuration.MaximumNackRetryPeriodTicks);
        if (configuration.MaximumLatencyTicks > 0)
        {
            long latencyDeadlineTicks = AddSaturatedJitterBufferTime(
                state.MissingSinceTicks,
                configuration.MaximumLatencyTicks);
            if (latencyDeadlineTicks < deadlineTicks)
            {
                deadlineTicks = latencyDeadlineTicks;
            }
        }

        return deadlineTicks;
    }

    internal long AddSaturatedJitterBufferTime(long timeTicks, long delayTicks)
    {
        if (timeTicks > long.MaxValue - delayTicks)
        {
            return long.MaxValue;
        }

        return timeTicks + delayTicks;
    }

    internal RtpJitterBufferStatus GetReleaseStatus(
        List<RtpPacket> releasedPackets,
        List<ushort> lostSequenceNumbers)
    {
        if (lostSequenceNumbers.Count > 0)
        {
            return RtpJitterBufferStatus.Lost;
        }

        if (releasedPackets.Count > 0)
        {
            return RtpJitterBufferStatus.Released;
        }

        return RtpJitterBufferStatus.Buffered;
    }

    internal RtpJitterBufferUpdate CreateOperationUpdate(
        RtpJitterBufferStatus status,
        List<RtpPacket> releasedPackets,
        List<RtpPacket> recoveredPackets,
        List<ushort> lostSequenceNumbers,
        List<RtpRepairRequest> repairRequests)
    {
        RecordJitterBufferOutcome(
            status,
            releasedPackets.Count,
            lostSequenceNumbers.Count,
            recoveredPackets.Count);
        RtpJitterBufferUpdate update = CreateJitterBufferUpdate(status);
        update.ReleasedPackets = releasedPackets.ToArray();
        update.RecoveredPackets = recoveredPackets.ToArray();
        update.LostSequenceNumbers = lostSequenceNumbers.ToArray();
        update.RepairRequests = repairRequests.ToArray();
        update.FeedbackRequests =
            CreateGenericNackFeedbackRequests(repairRequests);
        return update;
    }

    internal RtpFeedbackRequest[] CreateGenericNackFeedbackRequests(
        List<RtpRepairRequest> repairRequests)
    {
        RtpFeedbackRequest[] feedbackRequests =
            new RtpFeedbackRequest[repairRequests.Count];
        int requestIndex = 0;
        while (requestIndex < repairRequests.Count)
        {
            RtpRepairRequest repairRequest = repairRequests[requestIndex];
            RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
            feedback.PacketType = RtcpFeedbackPacketType.TransportLayer;
            feedback.TransportFormat =
                RtcpTransportFeedbackFormat.GenericNack;
            feedback.MediaSourceIdentifier = repairRequest.SourceIdentifier;
            feedback.GenericNack =
                RtcpGenericNackWireCodec.PackLostSequenceNumbers(
                    repairRequest.SequenceNumbers);

            RtpFeedbackRequest feedbackRequest =
                new RtpFeedbackRequest();
            feedbackRequest.Feedback = feedback;
            feedbackRequest.EarliestSendTimeTicks =
                repairRequest.RequestTimeTicks;
            feedbackRequest.ExpirationTimeTicks =
                repairRequest.DeadlineTicks;
            feedbackRequest.PreferredTimingMode =
                RtcpFeedbackTimingMode.Immediate;
            feedbackRequest.AllowSuppression = true;
            feedbackRequests[requestIndex] = feedbackRequest;
            requestIndex = requestIndex + 1;
        }

        return feedbackRequests;
    }

    internal void AddRecoveredPacketEvent(
        RtpPacket packet,
        RtpPacketContext context,
        List<RtpPacket> recoveredPackets)
    {
        if (context.IsRepaired)
        {
            recoveredPackets.Add(packet.Copy());
        }
    }

    internal void RecordJitterBufferOutcome(
        RtpJitterBufferStatus status,
        int releasedCount,
        int lostCount,
        int recoveredCount)
    {
        releasedPacketCount = AddSaturatedJitterBufferCount(
            releasedPacketCount,
            releasedCount);
        lostPacketCount = AddSaturatedJitterBufferCount(
            lostPacketCount,
            lostCount);
        recoveredPacketCount = AddSaturatedJitterBufferCount(
            recoveredPacketCount,
            recoveredCount);
        if (status == RtpJitterBufferStatus.Duplicate)
        {
            duplicatePacketCount = AddSaturatedJitterBufferCount(
                duplicatePacketCount,
                1L);
        }

        if (status == RtpJitterBufferStatus.Late)
        {
            latePacketCount = AddSaturatedJitterBufferCount(
                latePacketCount,
                1L);
        }

        if (status == RtpJitterBufferStatus.Overflow)
        {
            overflowPacketCount = AddSaturatedJitterBufferCount(
                overflowPacketCount,
                1L);
        }
    }

    internal long AddSaturatedJitterBufferCount(long count, long increment)
    {
        if (count > long.MaxValue - increment)
        {
            return long.MaxValue;
        }

        return count + increment;
    }

    internal void ValidateJitterBufferTime(long operationTimeTicks)
    {
        if (operationTimeTicks < 0)
        {
            throw new RtpConfigurationException(
                "Jitter-buffer time cannot be negative.");
        }
    }

    internal void ValidateJitterBufferChronology(long operationTimeTicks)
    {
        if (hasCurrentTime && operationTimeTicks < currentTimeTicks)
        {
            throw new RtpSessionException(
                "Jitter-buffer time cannot move backward.");
        }
    }

    internal void RecordJitterBufferTime(long operationTimeTicks)
    {
        hasCurrentTime = true;
        currentTimeTicks = operationTimeTicks;
    }

    internal RtpPacketHeap? FindSourcePacketHeap(uint sourceIdentifier)
    {
        int sourceIndex = 0;
        while (sourceIndex < sourcePacketHeaps.Count)
        {
            if (sourcePacketHeaps[sourceIndex].SourceIdentifier ==
                sourceIdentifier)
            {
                return sourcePacketHeaps[sourceIndex];
            }

            sourceIndex = sourceIndex + 1;
        }

        return null;
    }

    internal RtpJitterBufferEntry CreateJitterBufferEntry(
        RtpPacket packet,
        RtpPacketContext context,
        RtpSequenceUpdate sequenceUpdate)
    {
        RtpJitterBufferEntry entry = new RtpJitterBufferEntry();
        entry.Packet = packet.Copy();
        entry.Context = context.Copy();
        entry.SequenceUpdate = sequenceUpdate.Copy();
        return entry;
    }

    internal RtpJitterBufferUpdate CreateJitterBufferUpdate(
        RtpJitterBufferStatus status)
    {
        RtpJitterBufferUpdate update = new RtpJitterBufferUpdate();
        update.Status = status;
        update.ReleasedPacketCount = releasedPacketCount;
        update.DuplicatePacketCount = duplicatePacketCount;
        update.LatePacketCount = latePacketCount;
        update.LostPacketCount = lostPacketCount;
        update.OverflowPacketCount = overflowPacketCount;
        update.RecoveredPacketCount = recoveredPacketCount;
        int sourceIndex = 0;
        while (sourceIndex < sourcePacketHeaps.Count)
        {
            RtpPacketHeap sourceHeap = sourcePacketHeaps[sourceIndex];
            update.BufferedPacketCount = update.BufferedPacketCount +
                sourceHeap.Entries.Count;
            update.BufferedOctetCount = update.BufferedOctetCount +
                sourceHeap.BufferedOctetCount;
            sourceIndex = sourceIndex + 1;
        }

        return update;
    }
}
