using System;
using System.Collections.Generic;

namespace rtp;

internal class RtpSessionPoller
{
    internal object syncRoot = new object();
    internal RtpSessionConfiguration configuration;
    internal IRtpClock clock;
    internal RtpSeparatePortIngress ingress;
    internal RtcpScheduler controlScheduler;
    internal RtcpFeedbackScheduler feedbackScheduler;
    internal RtcpReportBuilder reportBuilder;
    internal IRtcpPacketCodec controlCodec;
    internal List<RtcpCompoundPacket> queuedControlPackets =
        new List<RtcpCompoundPacket>();
    internal List<RtpSessionEvent> queuedEvents =
        new List<RtpSessionEvent>();
    internal List<RtpRepairRequest> queuedRepairRequests =
        new List<RtpRepairRequest>();
    internal List<RtpRepairResult> queuedRepairResults =
        new List<RtpRepairResult>();
    internal bool hasPollTime;
    internal long pollTimeTicks;
    internal bool goodbyeRequested;
    internal bool goodbyeCompleted;
    internal byte[] goodbyeReason = [];

    internal RtpSessionPoller(
        RtpSessionConfiguration configuration,
        IRtpClock clock,
        RtpSeparatePortIngress ingress)
    {
        RtpConfigurationValidator.ValidateSessionConfiguration(configuration);
        if (clock == null)
        {
            throw new RtpConfigurationException(
                "An RTP clock is required for session polling.");
        }

        if (ingress == null)
        {
            throw new RtpConfigurationException(
                "RTP ingress state is required for session polling.");
        }

        this.configuration = configuration.Copy();
        this.clock = clock;
        this.ingress = ingress;
        controlScheduler = new RtcpScheduler(
            ingress.feedbackRandomSource);
        feedbackScheduler = ingress.GetIngressFeedbackScheduler();
        reportBuilder = new RtcpReportBuilder();
        controlCodec = new RtcpPacketCodec(this.configuration);
        long currentTimeTicks = clock.GetMonotonicTimeTicks();
        ValidatePollTime(currentTimeTicks);
        EnsureLocalSource();
        controlScheduler.InitializeRtcpSchedule(
            this.configuration.RtcpScheduler,
            currentTimeTicks);
        hasPollTime = true;
        pollTimeTicks = currentTimeTicks;
    }

    internal void QueueOutboundControlPacket(
        RtcpCompoundPacket compoundPacket)
    {
        lock (syncRoot)
        {
            if (compoundPacket == null)
            {
                throw new RtpConfigurationException(
                    "An outbound RTCP compound packet is required.");
            }

            int packetSize = controlCodec.GetRtcpCompoundPacketEncodedSize(
                compoundPacket);
            queuedControlPackets.Add(compoundPacket.Copy());
            controlScheduler.RecordRtcpPacketSize(packetSize);
        }
    }

    internal void QueueSessionEvent(RtpSessionEvent sessionEvent)
    {
        lock (syncRoot)
        {
            if (sessionEvent == null)
            {
                throw new RtpConfigurationException(
                    "An RTP session event is required.");
            }

            queuedEvents.Add(sessionEvent.Copy());
        }
    }

    internal void QueueSessionRepairRequest(RtpRepairRequest request)
    {
        lock (syncRoot)
        {
            RtpRepairRequest retainedRequest = ingress.repairCoordinator
                .CopyValidatedRequest(request);
            queuedRepairRequests.Add(retainedRequest);
        }
    }

    internal void QueueSessionRepairResult(RtpRepairResult result)
    {
        lock (syncRoot)
        {
            if (result == null)
            {
                throw new RtpConfigurationException(
                    "An RTP repair result is required.");
            }

            RtpRepairResult retainedResult = ingress.repairCoordinator
                .CopyValidatedResult(result, result.Kind);
            queuedRepairResults.Add(retainedResult);
        }
    }

    internal void QueueSessionFeedbackRequest(RtpFeedbackRequest request)
    {
        lock (syncRoot)
        {
            feedbackScheduler.QueueRtcpFeedback(request);
        }
    }

    internal void RecordReceivedControlPacketSize(int packetSize)
    {
        lock (syncRoot)
        {
            controlScheduler.RecordRtcpPacketSize(packetSize);
        }
    }

    internal RtpSessionUpdate RequestSessionGoodbye(
        ReadOnlySpan<byte> reason)
    {
        lock (syncRoot)
        {
            return RequestSessionGoodbyeState(reason);
        }
    }

    internal RtpSessionUpdate RequestSessionGoodbyeState(
        ReadOnlySpan<byte> reason)
    {
        if (goodbyeRequested || goodbyeCompleted)
        {
            throw new RtpSessionException(
                "RTP session goodbye has already been requested.");
        }

        ValidateGoodbyeReason(reason);
        long currentTimeTicks = clock.GetMonotonicTimeTicks();
        ValidateAdvancingPollTime(currentTimeTicks);
        RtpSessionSnapshot snapshot = CreateSessionSnapshot(
            currentTimeTicks);
        RtcpScheduleDecision decision = controlScheduler
            .ScheduleRtcpGoodbye(snapshot, currentTimeTicks);
        goodbyeReason = reason.ToArray();
        goodbyeRequested = true;
        List<RtcpCompoundPacket> outboundControlPackets =
            new List<RtcpCompoundPacket>();
        List<RtpSessionEvent> events = new List<RtpSessionEvent>();
        AppendScheduledGoodbye(
            snapshot,
            decision,
            outboundControlPackets,
            events);
        hasPollTime = true;
        pollTimeTicks = currentTimeTicks;

        RtpSessionUpdate update = new RtpSessionUpdate();
        update.Events = events.ToArray();
        update.OutboundControlPackets = outboundControlPackets.ToArray();
        update.ControlScheduleDecisions = new RtcpScheduleDecision[]
        {
            decision.Copy()
        };
        update.Snapshot = CreateSessionSnapshot(currentTimeTicks);
        return update;
    }

    internal RtpSessionUpdate FlushSession()
    {
        lock (syncRoot)
        {
            return FlushSessionState();
        }
    }

    internal RtpSessionUpdate FlushSessionState()
    {
        long currentTimeTicks = clock.GetMonotonicTimeTicks();
        ValidateAdvancingPollTime(currentTimeTicks);
        RtpJitterBufferUpdate jitterUpdate = ingress.jitterBuffer
            .FlushRtpJitterBuffer();
        ingress.QueueIngressFeedbackRequests(
            jitterUpdate.FeedbackRequests);
        List<RtpSessionEvent> events = CopyQueuedEvents();
        List<RtpRepairRequest> repairRequests =
            CopyQueuedRepairRequests();
        AppendJitterWork(
            jitterUpdate,
            repairRequests,
            events,
            currentTimeTicks);
        List<RtcpCompoundPacket> outboundControlPackets =
            CopyQueuedControlPackets();
        List<RtpRepairResult> repairResults =
            CopyQueuedRepairResults();
        ClearQueuedWork();
        hasPollTime = true;
        pollTimeTicks = currentTimeTicks;

        RtpSessionUpdate update = new RtpSessionUpdate();
        update.Events = events.ToArray();
        update.ReleasedPackets = CopyRtpPackets(
            jitterUpdate.ReleasedPackets);
        update.OutboundControlPackets = outboundControlPackets.ToArray();
        update.FeedbackRequests = CopyFeedbackRequests(
            jitterUpdate.FeedbackRequests);
        update.RepairRequests = repairRequests.ToArray();
        update.RepairResults = repairResults.ToArray();
        update.Snapshot = CreateSessionSnapshot(currentTimeTicks);
        return update;
    }

    internal RtpSessionSnapshot GetSessionSnapshot()
    {
        lock (syncRoot)
        {
            return GetSessionSnapshotState();
        }
    }

    internal RtpSessionSnapshot GetSessionSnapshotState()
    {
        long currentTimeTicks = clock.GetMonotonicTimeTicks();
        ValidateAdvancingPollTime(currentTimeTicks);
        return CreateSessionSnapshot(currentTimeTicks).Copy();
    }

    internal RtpSessionUpdate PollSession()
    {
        lock (syncRoot)
        {
            return PollSessionState();
        }
    }

    internal RtpSessionUpdate PollSessionState()
    {
        long currentTimeTicks = clock.GetMonotonicTimeTicks();
        ValidateAdvancingPollTime(currentTimeTicks);

        RtpJitterBufferUpdate jitterUpdate = ingress.jitterBuffer
            .PollRtpJitterBuffer(currentTimeTicks);
        ingress.QueueIngressFeedbackRequests(
            jitterUpdate.FeedbackRequests);
        RtpSourceSnapshot[] expiredSources = ingress.remoteSourceTable
            .ExpireRemoteSourceActivity(currentTimeTicks);

        RtpSessionSnapshot schedulingSnapshot = CreateSessionSnapshot(
            currentTimeTicks);
        RtpFeedbackDecision feedbackDecision = new RtpFeedbackDecision();
        RtcpScheduleDecision? controlDecision = null;
        if (goodbyeRequested)
        {
            controlDecision = controlScheduler.ScheduleRtcpGoodbye(
                schedulingSnapshot,
                currentTimeTicks);
        }
        else if (!goodbyeCompleted)
        {
            feedbackDecision = feedbackScheduler.PollRtcpFeedback(
                schedulingSnapshot,
                currentTimeTicks);
            controlDecision = controlScheduler.PollRtcpSchedule(
                schedulingSnapshot,
                currentTimeTicks);
            schedulingSnapshot.NextRtcpSendTimeTicks =
                controlScheduler.nextReportTimeTicks;
        }
        List<RtcpCompoundPacket> outboundControlPackets =
            CopyQueuedControlPackets();
        List<RtpSessionEvent> events = CopyQueuedEvents();
        List<RtpRepairRequest> repairRequests =
            CopyQueuedRepairRequests();
        List<RtpRepairResult> repairResults =
            CopyQueuedRepairResults();
        AppendJitterWork(
            jitterUpdate,
            repairRequests,
            events,
            currentTimeTicks);
        AppendExpiryEvents(expiredSources, events);
        if (controlDecision != null)
        {
            if (goodbyeRequested)
            {
                AppendScheduledGoodbye(
                    schedulingSnapshot,
                    controlDecision,
                    outboundControlPackets,
                    events);
            }
            else
            {
                AppendScheduledControl(
                    schedulingSnapshot,
                    feedbackDecision,
                    controlDecision,
                    outboundControlPackets,
                    events,
                    currentTimeTicks);
            }
        }

        ClearQueuedWork();
        hasPollTime = true;
        pollTimeTicks = currentTimeTicks;

        RtpSessionUpdate update = new RtpSessionUpdate();
        update.Events = events.ToArray();
        update.ReleasedPackets = CopyRtpPackets(
            jitterUpdate.ReleasedPackets);
        update.OutboundControlPackets = outboundControlPackets.ToArray();
        update.FeedbackRequests = CopyFeedbackRequests(
            jitterUpdate.FeedbackRequests);
        if (feedbackDecision.Request != null)
        {
            update.FeedbackDecisions = new RtpFeedbackDecision[]
            {
                feedbackDecision.Copy()
            };
        }

        if (controlDecision != null)
        {
            update.ControlScheduleDecisions = new RtcpScheduleDecision[]
            {
                controlDecision.Copy()
            };
        }

        update.RepairRequests = repairRequests.ToArray();
        update.RepairResults = repairResults.ToArray();
        update.Snapshot = CreateSessionSnapshot(currentTimeTicks);
        return update;
    }

    internal void AppendScheduledGoodbye(
        RtpSessionSnapshot snapshot,
        RtcpScheduleDecision decision,
        List<RtcpCompoundPacket> outboundControlPackets,
        List<RtpSessionEvent> events)
    {
        if (!decision.ShouldSend)
        {
            return;
        }

        RtcpCompoundPacket compound = reportBuilder
            .BuildGoodbyeRtcpReport(snapshot, goodbyeReason);
        decision.CompoundPacket = compound.Copy();
        outboundControlPackets.Add(compound.Copy());
        RecordGeneratedControlPacket(compound);
        RtpSessionEvent sessionEvent = CreateReportEvent(compound);
        sessionEvent.Type = RtpSessionEventType.Goodbye;
        sessionEvent.Message = "The local RTCP goodbye compound packet is ready.";
        events.Add(sessionEvent);
        goodbyeRequested = false;
        goodbyeCompleted = true;
    }

    internal RtpSessionSnapshot CreateSessionSnapshot(
        long currentTimeTicks)
    {
        lock (syncRoot)
        {
            return CreateSessionSnapshotState(currentTimeTicks);
        }
    }

    internal RtpSessionSnapshot CreateSessionSnapshotState(
        long currentTimeTicks)
    {
        ValidatePollTime(currentTimeTicks);
        RtpSourceSnapshot[] localSources = ingress.localSourceTable
            .GetLocalSourceSnapshots();
        RtpSourceSnapshot[] remoteSources = ingress.remoteSourceTable
            .GetRemoteSourceStatisticsSnapshots(currentTimeTicks);
        RtpSessionSnapshot snapshot = new RtpSessionSnapshot();
        snapshot.Configuration = configuration.Copy();
        snapshot.Sources = CombineSourceSnapshots(
            localSources,
            remoteSources);
        snapshot.LocalSourceIdentifiers = ingress.localSourceTable
            .GetLocalSourceIdentifiers();
        snapshot.MemberCount = 1 + ingress.remoteSourceTable
            .GetRemoteMemberCount();
        snapshot.SenderCount = ingress.remoteSourceTable
            .GetRemoteSenderCount();
        if (HasLocalSender(localSources))
        {
            snapshot.SenderCount = snapshot.SenderCount + 1;
        }

        if (snapshot.SenderCount > snapshot.MemberCount)
        {
            snapshot.SenderCount = snapshot.MemberCount;
        }

        snapshot.AverageRtcpPacketSize = (int)Math.Ceiling(
            controlScheduler.averagePacketSize);
        snapshot.SnapshotTimeTicks = currentTimeTicks;
        snapshot.NextRtcpSendTimeTicks =
            controlScheduler.nextReportTimeTicks;
        return snapshot;
    }

    internal void AppendScheduledControl(
        RtpSessionSnapshot snapshot,
        RtpFeedbackDecision feedbackDecision,
        RtcpScheduleDecision controlDecision,
        List<RtcpCompoundPacket> outboundControlPackets,
        List<RtpSessionEvent> events,
        long currentTimeTicks)
    {
        bool feedbackSent = false;
        if (feedbackDecision.ShouldSend &&
            feedbackDecision.Request != null &&
            feedbackDecision.Request.Feedback != null)
        {
            if (feedbackDecision.Piggyback)
            {
                ingress.remoteSourceTable
                    .AdvanceRemoteReceptionReportIntervals(
                        currentTimeTicks);
                snapshot = CreateSessionSnapshot(currentTimeTicks);
            }

            RtcpCompoundPacket feedbackCompound = reportBuilder
                .BuildFeedbackRtcpReport(
                    snapshot,
                    feedbackDecision.Request.Feedback,
                    feedbackDecision.TimingMode);
            feedbackDecision.CompoundPacket = feedbackCompound.Copy();
            outboundControlPackets.Add(feedbackCompound.Copy());
            RecordGeneratedControlPacket(feedbackCompound);
            events.Add(CreateFeedbackEvent(
                feedbackDecision,
                feedbackCompound));
            feedbackSent = true;
            if (feedbackDecision.Piggyback && controlDecision.ShouldSend)
            {
                controlDecision.CompoundPacket = feedbackCompound.Copy();
            }
        }

        if (controlDecision.ShouldSend &&
            (!feedbackSent || !feedbackDecision.Piggyback))
        {
            ingress.remoteSourceTable.AdvanceRemoteReceptionReportIntervals(
                currentTimeTicks);
            RtpSessionSnapshot reportSnapshot = CreateSessionSnapshot(
                currentTimeTicks);
            RtcpCompoundPacket regularCompound = reportBuilder
                .BuildRegularRtcpReport(
                    reportSnapshot,
                    clock.GetNtpTimestamp());
            controlDecision.CompoundPacket = regularCompound.Copy();
            outboundControlPackets.Add(regularCompound.Copy());
            RecordGeneratedControlPacket(regularCompound);
            events.Add(CreateReportEvent(regularCompound));
        }
    }

    internal void RecordGeneratedControlPacket(
        RtcpCompoundPacket compoundPacket)
    {
        int packetSize = controlCodec.GetRtcpCompoundPacketEncodedSize(
            compoundPacket);
        controlScheduler.RecordRtcpPacketSize(packetSize);
    }

    internal void AppendJitterWork(
        RtpJitterBufferUpdate jitterUpdate,
        List<RtpRepairRequest> repairRequests,
        List<RtpSessionEvent> events,
        long currentTimeTicks)
    {
        int requestIndex = 0;
        while (requestIndex < jitterUpdate.RepairRequests.Length)
        {
            RtpRepairRequest request =
                jitterUpdate.RepairRequests[requestIndex].Copy();
            repairRequests.Add(request);
            events.Add(CreateRepairEvent(request));
            requestIndex = requestIndex + 1;
        }

        if (jitterUpdate.LostSequenceNumbers.Length > 0)
        {
            events.Add(CreateLossEvent(
                jitterUpdate,
                currentTimeTicks));
        }
    }

    internal void AppendExpiryEvents(
        RtpSourceSnapshot[] expiredSources,
        List<RtpSessionEvent> events)
    {
        int sourceIndex = 0;
        while (sourceIndex < expiredSources.Length)
        {
            RtpSourceSnapshot source = expiredSources[sourceIndex];
            RtpSessionEvent sessionEvent = new RtpSessionEvent();
            sessionEvent.Type = RtpSessionEventType.Timeout;
            sessionEvent.SourceIdentifier = source.SourceIdentifier;
            sessionEvent.SourceStatus = source.Status;
            sessionEvent.Source = source.Copy();
            if (source.Status == RtpSourceStatus.Inactive)
            {
                sessionEvent.Message =
                    "The remote RTP source membership expired.";
            }
            else
            {
                sessionEvent.Message =
                    "The remote RTP source sender activity expired.";
            }

            events.Add(sessionEvent);
            sourceIndex = sourceIndex + 1;
        }
    }

    internal RtpSessionEvent CreateRepairEvent(RtpRepairRequest request)
    {
        RtpSessionEvent sessionEvent = new RtpSessionEvent();
        sessionEvent.Type = RtpSessionEventType.Repair;
        sessionEvent.PacketKind = RtpPacketKind.Rtp;
        sessionEvent.RepairKind = request.Kind;
        sessionEvent.SourceIdentifier = request.SourceIdentifier;
        sessionEvent.RepairRequest = request.Copy();
        sessionEvent.Message = "RTP packet repair work is ready.";
        return sessionEvent;
    }

    internal RtpSessionEvent CreateLossEvent(
        RtpJitterBufferUpdate jitterUpdate,
        long currentTimeTicks)
    {
        RtpSessionEvent sessionEvent = new RtpSessionEvent();
        sessionEvent.Type = RtpSessionEventType.Loss;
        sessionEvent.PacketKind = RtpPacketKind.Rtp;
        sessionEvent.JitterBufferStatus = jitterUpdate.Status;
        sessionEvent.JitterBufferUpdate = jitterUpdate.Copy();
        sessionEvent.Message = "RTP packet loss was finalized while polling.";
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = currentTimeTicks;
        sessionEvent.PacketContext = context;
        if (jitterUpdate.RepairRequests.Length > 0)
        {
            sessionEvent.SourceIdentifier =
                jitterUpdate.RepairRequests[0].SourceIdentifier;
        }

        return sessionEvent;
    }

    internal RtpSessionEvent CreateFeedbackEvent(
        RtpFeedbackDecision decision,
        RtcpCompoundPacket compoundPacket)
    {
        RtpSessionEvent sessionEvent = new RtpSessionEvent();
        sessionEvent.Type = RtpSessionEventType.Report;
        sessionEvent.PacketKind = RtpPacketKind.Rtcp;
        sessionEvent.FeedbackDecision = decision.Copy();
        if (compoundPacket.Packets.Length > 0)
        {
            sessionEvent.ControlPacket = compoundPacket.Packets[0].Copy();
        }

        sessionEvent.Message = "An RTCP feedback compound packet is ready.";
        return sessionEvent;
    }

    internal RtpSessionEvent CreateReportEvent(
        RtcpCompoundPacket compoundPacket)
    {
        RtpSessionEvent sessionEvent = new RtpSessionEvent();
        sessionEvent.Type = RtpSessionEventType.Report;
        sessionEvent.PacketKind = RtpPacketKind.Rtcp;
        if (compoundPacket.Packets.Length > 0)
        {
            sessionEvent.ControlPacket = compoundPacket.Packets[0].Copy();
        }

        sessionEvent.Message = "A regular RTCP compound packet is ready.";
        return sessionEvent;
    }

    internal RtpSourceSnapshot[] CombineSourceSnapshots(
        RtpSourceSnapshot[] localSources,
        RtpSourceSnapshot[] remoteSources)
    {
        RtpSourceSnapshot[] combined = new RtpSourceSnapshot[
            localSources.Length + remoteSources.Length];
        int sourceIndex = 0;
        while (sourceIndex < localSources.Length)
        {
            combined[sourceIndex] = localSources[sourceIndex].Copy();
            sourceIndex = sourceIndex + 1;
        }

        int remoteIndex = 0;
        while (remoteIndex < remoteSources.Length)
        {
            combined[sourceIndex] = remoteSources[remoteIndex].Copy();
            sourceIndex = sourceIndex + 1;
            remoteIndex = remoteIndex + 1;
        }

        return combined;
    }

    internal bool HasLocalSender(RtpSourceSnapshot[] localSources)
    {
        int sourceIndex = 0;
        while (sourceIndex < localSources.Length)
        {
            if (localSources[sourceIndex].IsSender)
            {
                return true;
            }

            sourceIndex = sourceIndex + 1;
        }

        return false;
    }

    internal void EnsureLocalSource()
    {
        if (ingress.localSourceTable.GetLocalSourceIdentifiers().Length == 0)
        {
            ingress.localSourceTable.CreateLocalSourceState();
        }
    }

    internal List<RtcpCompoundPacket> CopyQueuedControlPackets()
    {
        List<RtcpCompoundPacket> copies =
            new List<RtcpCompoundPacket>();
        int packetIndex = 0;
        while (packetIndex < queuedControlPackets.Count)
        {
            copies.Add(queuedControlPackets[packetIndex].Copy());
            packetIndex = packetIndex + 1;
        }

        return copies;
    }

    internal List<RtpSessionEvent> CopyQueuedEvents()
    {
        List<RtpSessionEvent> copies = new List<RtpSessionEvent>();
        int eventIndex = 0;
        while (eventIndex < queuedEvents.Count)
        {
            copies.Add(queuedEvents[eventIndex].Copy());
            eventIndex = eventIndex + 1;
        }

        return copies;
    }

    internal List<RtpRepairRequest> CopyQueuedRepairRequests()
    {
        List<RtpRepairRequest> copies = new List<RtpRepairRequest>();
        int requestIndex = 0;
        while (requestIndex < queuedRepairRequests.Count)
        {
            copies.Add(queuedRepairRequests[requestIndex].Copy());
            requestIndex = requestIndex + 1;
        }

        return copies;
    }

    internal List<RtpRepairResult> CopyQueuedRepairResults()
    {
        List<RtpRepairResult> copies = new List<RtpRepairResult>();
        int resultIndex = 0;
        while (resultIndex < queuedRepairResults.Count)
        {
            copies.Add(queuedRepairResults[resultIndex].Copy());
            resultIndex = resultIndex + 1;
        }

        return copies;
    }

    internal RtpPacket[] CopyRtpPackets(RtpPacket[] packets)
    {
        RtpPacket[] copies = new RtpPacket[packets.Length];
        int packetIndex = 0;
        while (packetIndex < packets.Length)
        {
            copies[packetIndex] = packets[packetIndex].Copy();
            packetIndex = packetIndex + 1;
        }

        return copies;
    }

    internal RtpFeedbackRequest[] CopyFeedbackRequests(
        RtpFeedbackRequest[] requests)
    {
        RtpFeedbackRequest[] copies =
            new RtpFeedbackRequest[requests.Length];
        int requestIndex = 0;
        while (requestIndex < requests.Length)
        {
            copies[requestIndex] = requests[requestIndex].Copy();
            requestIndex = requestIndex + 1;
        }

        return copies;
    }

    internal void ClearQueuedWork()
    {
        queuedControlPackets.Clear();
        queuedEvents.Clear();
        queuedRepairRequests.Clear();
        queuedRepairResults.Clear();
    }

    internal void ValidateAdvancingPollTime(long currentTimeTicks)
    {
        ValidatePollTime(currentTimeTicks);
        if (hasPollTime && currentTimeTicks < pollTimeTicks)
        {
            throw new RtpSessionException(
                "RTP session poll time cannot move backward.");
        }
    }

    internal void ValidatePollTime(long currentTimeTicks)
    {
        if (currentTimeTicks < 0)
        {
            throw new RtpConfigurationException(
                "RTP session poll time cannot be negative.");
        }
    }

    internal void ValidateGoodbyeReason(ReadOnlySpan<byte> reason)
    {
        if (reason.Length > RtcpProtocolConstants.MaximumTextLength)
        {
            throw new RtpConfigurationException(
                "The RTP session goodbye reason cannot exceed 255 bytes.");
        }

        if (!RtpKnownHeaderExtensionWireCodec.IsValidUtf8(reason))
        {
            throw new RtpConfigurationException(
                "The RTP session goodbye reason must be valid UTF-8.");
        }
    }
}
