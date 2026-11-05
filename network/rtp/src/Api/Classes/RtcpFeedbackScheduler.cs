using System;
using System.Collections.Generic;

namespace rtp;

/// <summary>Schedules AVPF feedback without timers or background work.</summary>
public class RtcpFeedbackScheduler : IRtcpFeedbackScheduler
{
    internal object syncRoot = new object();
    internal RtcpSchedulerConfiguration configuration;
    internal IRtpRandomSource randomSource;
    internal List<RtpFeedbackRequest> pendingRequests =
        new List<RtpFeedbackRequest>();
    internal List<RtcpFeedbackPacket> observedFeedback =
        new List<RtcpFeedbackPacket>();
    internal List<long> observedFeedbackArrivalTimes = new List<long>();
    internal bool allowEarly = true;
    internal bool hasScheduledFeedback;
    internal bool hasFeedbackTransmission;
    internal bool hasSchedulerTime;
    internal long scheduledFeedbackTimeTicks;
    internal long scheduledRegularTimeTicks;
    internal long earlyAllowanceResetTimeTicks;
    internal long lastFeedbackTimeTicks;
    internal long bandwidthUpdateTimeTicks;
    internal long schedulerTimeTicks;
    internal double feedbackByteCredit;
    internal RtcpFeedbackTimingMode scheduledTimingMode;
    internal bool scheduledPiggyback;

    /// <summary>Initializes an AVPF scheduler from isolated configuration.</summary>
    /// <param name="configuration">The RTCP bandwidth and feedback settings.</param>
    /// <param name="randomSource">The source of feedback dithering values.</param>
    public RtcpFeedbackScheduler(
        RtcpSchedulerConfiguration configuration,
        IRtpRandomSource randomSource)
    {
        RtpConfigurationValidator.ValidateRtcpSchedulerConfiguration(configuration);
        if (randomSource == null)
        {
            throw new RtpConfigurationException(
                "An RTP random source is required for feedback scheduling.");
        }

        this.configuration = configuration.Copy();
        this.randomSource = randomSource;
        feedbackByteCredit = configuration.InitialAveragePacketSize;
    }

    /// <summary>Queues one independently retained feedback request.</summary>
    /// <param name="request">The feedback request to schedule.</param>
    public void QueueRtcpFeedback(RtpFeedbackRequest request)
    {
        ValidateFeedbackRequest(request);
        lock (syncRoot)
        {
            RtpFeedbackRequest retainedRequest = request.Copy();
            int requestIndex = 0;
            while (requestIndex < pendingRequests.Count)
            {
                RtpFeedbackRequest pendingRequest = pendingRequests[requestIndex];
                if (RtcpFeedbackSemanticComparer.CanCoalesceFeedback(
                    GetRequiredRequestFeedback(pendingRequest),
                    GetRequiredRequestFeedback(retainedRequest)))
                {
                    MergeFeedbackRequest(pendingRequest, retainedRequest);
                    return;
                }

                requestIndex = requestIndex + 1;
            }

            pendingRequests.Add(retainedRequest);
        }
    }

    /// <summary>Observes received feedback that can suppress an equivalent request.</summary>
    /// <param name="feedback">The received feedback packet.</param>
    /// <param name="arrivalTimeTicks">The monotonic feedback arrival time.</param>
    public void ObserveRtcpFeedback(
        RtcpFeedbackPacket feedback,
        long arrivalTimeTicks)
    {
        if (feedback == null)
        {
            throw new RtpConfigurationException(
                "A received feedback packet is required.");
        }

        ValidateFeedbackTime(arrivalTimeTicks);
        lock (syncRoot)
        {
            ValidateFeedbackChronology(arrivalTimeTicks);
            RecordFeedbackTime(arrivalTimeTicks);
            PruneObservedFeedback(arrivalTimeTicks);
            observedFeedback.Add(feedback.Copy());
            observedFeedbackArrivalTimes.Add(arrivalTimeTicks);
        }
    }

    /// <summary>Polls pending feedback against current session state.</summary>
    /// <param name="snapshot">The current independent session snapshot.</param>
    /// <param name="currentTimeTicks">The current monotonic time.</param>
    /// <returns>The feedback scheduling decision.</returns>
    public RtpFeedbackDecision PollRtcpFeedback(
        RtpSessionSnapshot snapshot,
        long currentTimeTicks)
    {
        ValidateFeedbackSnapshot(snapshot);
        ValidateFeedbackTime(currentTimeTicks);
        lock (syncRoot)
        {
            ValidateFeedbackChronology(currentTimeTicks);
            UpdateFeedbackBandwidthCredit(snapshot, currentTimeTicks);
            RecordFeedbackTime(currentTimeTicks);
            PruneObservedFeedback(currentTimeTicks);
            RefreshEarlyFeedbackAllowance(currentTimeTicks);
            if (pendingRequests.Count == 0)
            {
                return new RtpFeedbackDecision();
            }

            RtpFeedbackRequest request = pendingRequests[0];
            if (IsFeedbackRequestExpired(request, currentTimeTicks))
            {
                return CompleteFeedbackWithoutSending(
                    request,
                    false,
                    request.ExpirationTimeTicks);
            }

            if (request.AllowSuppression &&
                HasSuppressingFeedback(request, currentTimeTicks))
            {
                return CompleteFeedbackWithoutSending(
                    request,
                    true,
                    currentTimeTicks);
            }

            if (!hasScheduledFeedback)
            {
                SchedulePendingFeedback(request, snapshot, currentTimeTicks);
            }

            if (request.ExpirationTimeTicks > 0 &&
                scheduledFeedbackTimeTicks > request.ExpirationTimeTicks)
            {
                return CompleteFeedbackWithoutSending(
                    request,
                    false,
                    request.ExpirationTimeTicks);
            }

            bool shouldSend = currentTimeTicks >= scheduledFeedbackTimeTicks;
            RtpFeedbackDecision decision = CreateFeedbackDecision(
                request,
                shouldSend);
            if (shouldSend)
            {
                int averagePacketSize = GetFeedbackAveragePacketSize(snapshot);
                if (!scheduledPiggyback)
                {
                    feedbackByteCredit = feedbackByteCredit - averagePacketSize;
                    if (feedbackByteCredit < 0.0)
                    {
                        feedbackByteCredit = 0.0;
                    }

                    allowEarly = false;
                    earlyAllowanceResetTimeTicks = scheduledRegularTimeTicks;
                }
                else
                {
                    allowEarly = true;
                    earlyAllowanceResetTimeTicks = 0L;
                }

                hasFeedbackTransmission = true;
                lastFeedbackTimeTicks = currentTimeTicks;
                pendingRequests.RemoveAt(0);
                ClearScheduledFeedback();
            }

            return decision;
        }
    }

    /// <summary>Resets queued feedback and bandwidth-accounting state.</summary>
    /// <param name="currentTimeTicks">The reset monotonic time.</param>
    public void ResetRtcpFeedbackScheduler(long currentTimeTicks)
    {
        ValidateFeedbackTime(currentTimeTicks);
        lock (syncRoot)
        {
            pendingRequests.Clear();
            observedFeedback.Clear();
            observedFeedbackArrivalTimes.Clear();
            allowEarly = true;
            hasScheduledFeedback = false;
            hasFeedbackTransmission = false;
            hasSchedulerTime = true;
            scheduledFeedbackTimeTicks = 0L;
            scheduledRegularTimeTicks = 0L;
            earlyAllowanceResetTimeTicks = 0L;
            lastFeedbackTimeTicks = 0L;
            bandwidthUpdateTimeTicks = currentTimeTicks;
            schedulerTimeTicks = currentTimeTicks;
            feedbackByteCredit = configuration.InitialAveragePacketSize;
            scheduledTimingMode = RtcpFeedbackTimingMode.Regular;
            scheduledPiggyback = false;
        }
    }

    internal void SchedulePendingFeedback(
        RtpFeedbackRequest request,
        RtpSessionSnapshot snapshot,
        long currentTimeTicks)
    {
        long regularTimeTicks = snapshot.NextRtcpSendTimeTicks;
        if (regularTimeTicks < currentTimeTicks)
        {
            regularTimeTicks = currentTimeTicks;
        }

        scheduledRegularTimeTicks = regularTimeTicks;

        long feedbackBaseTimeTicks = request.EarliestSendTimeTicks;
        if (feedbackBaseTimeTicks < currentTimeTicks)
        {
            feedbackBaseTimeTicks = currentTimeTicks;
        }

        if (hasFeedbackTransmission && configuration.MinimumFeedbackIntervalTicks > 0)
        {
            long minimumFeedbackTimeTicks = AddFeedbackTime(
                lastFeedbackTimeTicks,
                configuration.MinimumFeedbackIntervalTicks);
            if (feedbackBaseTimeTicks < minimumFeedbackTimeTicks)
            {
                feedbackBaseTimeTicks = minimumFeedbackTimeTicks;
            }
        }

        int averagePacketSize = GetFeedbackAveragePacketSize(snapshot);
        bool canSendEarly = configuration.FeedbackEnabled && allowEarly &&
            request.PreferredTimingMode != RtcpFeedbackTimingMode.Regular &&
            feedbackByteCredit >= averagePacketSize;
        if (!canSendEarly || feedbackBaseTimeTicks >= regularTimeTicks)
        {
            ScheduleRegularFeedback(regularTimeTicks, feedbackBaseTimeTicks);
            return;
        }

        long ditherMaximumTicks = GetFeedbackDitherMaximumTicks(
            snapshot,
            averagePacketSize);
        if (ditherMaximumTicks > regularTimeTicks - feedbackBaseTimeTicks)
        {
            ScheduleRegularFeedback(regularTimeTicks, feedbackBaseTimeTicks);
            return;
        }

        long ditherTicks = GetFeedbackDitherTicks(ditherMaximumTicks);
        scheduledFeedbackTimeTicks = AddFeedbackTime(
            feedbackBaseTimeTicks,
            ditherTicks);
        scheduledTimingMode = RtcpFeedbackTimingMode.Early;
        if (request.PreferredTimingMode == RtcpFeedbackTimingMode.Immediate &&
            ditherMaximumTicks == 0)
        {
            scheduledTimingMode = RtcpFeedbackTimingMode.Immediate;
        }
        else if (request.PreferredTimingMode == RtcpFeedbackTimingMode.ReducedSize &&
            configuration.ReducedSizeEnabled)
        {
            scheduledTimingMode = RtcpFeedbackTimingMode.ReducedSize;
        }

        scheduledPiggyback = false;
        hasScheduledFeedback = true;
    }

    internal void ScheduleRegularFeedback(
        long regularTimeTicks,
        long feedbackBaseTimeTicks)
    {
        scheduledFeedbackTimeTicks = regularTimeTicks;
        if (scheduledFeedbackTimeTicks < feedbackBaseTimeTicks)
        {
            scheduledFeedbackTimeTicks = feedbackBaseTimeTicks;
        }

        scheduledTimingMode = RtcpFeedbackTimingMode.Regular;
        scheduledPiggyback = true;
        hasScheduledFeedback = true;
    }

    internal long GetFeedbackDitherMaximumTicks(
        RtpSessionSnapshot snapshot,
        int averagePacketSize)
    {
        if (snapshot.MemberCount <= 2)
        {
            return 0L;
        }

        double intervalSeconds =
            RtcpIntervalCalculator.CalculateDeterministicIntervalSeconds(
                configuration,
                snapshot.MemberCount,
                snapshot.SenderCount,
                snapshot.SenderCount > 0 && HasActiveLocalSender(snapshot),
                averagePacketSize,
                false);
        double ditherTicks = intervalSeconds * TimeSpan.TicksPerSecond * 0.5;
        if (double.IsNaN(ditherTicks) || double.IsInfinity(ditherTicks) ||
            ditherTicks >= 9223372036854775808.0)
        {
            throw new RtpConfigurationException(
                "The feedback dithering interval cannot be represented.");
        }

        return (long)Math.Ceiling(ditherTicks);
    }

    internal long GetFeedbackDitherTicks(long maximumTicks)
    {
        if (maximumTicks == 0)
        {
            return 0L;
        }

        double randomValue = randomSource.CreateUnitIntervalValue();
        RtcpIntervalCalculator.ValidateIntervalRandomValue(randomValue);
        return (long)Math.Floor(maximumTicks * randomValue);
    }

    internal void UpdateFeedbackBandwidthCredit(
        RtpSessionSnapshot snapshot,
        long currentTimeTicks)
    {
        if (currentTimeTicks < bandwidthUpdateTimeTicks)
        {
            throw new RtpSessionException(
                "Feedback scheduler time cannot move backward.");
        }

        int averagePacketSize = GetFeedbackAveragePacketSize(snapshot);
        double elapsedSeconds =
            (currentTimeTicks - bandwidthUpdateTimeTicks) /
            (double)TimeSpan.TicksPerSecond;
        double bandwidthBytesPerSecond =
            GetParticipantRtcpBandwidthBytesPerSecond(snapshot);
        feedbackByteCredit = feedbackByteCredit +
            elapsedSeconds * bandwidthBytesPerSecond;
        if (feedbackByteCredit > averagePacketSize)
        {
            feedbackByteCredit = averagePacketSize;
        }

        bandwidthUpdateTimeTicks = currentTimeTicks;
    }

    internal void RefreshEarlyFeedbackAllowance(long currentTimeTicks)
    {
        if (!allowEarly && earlyAllowanceResetTimeTicks > 0 &&
            currentTimeTicks >= earlyAllowanceResetTimeTicks)
        {
            allowEarly = true;
            earlyAllowanceResetTimeTicks = 0L;
        }
    }

    internal void MergeFeedbackRequest(
        RtpFeedbackRequest pendingRequest,
        RtpFeedbackRequest newRequest)
    {
        pendingRequest.Feedback =
            RtcpFeedbackSemanticComparer.MergeCoalescedFeedback(
                GetRequiredRequestFeedback(pendingRequest),
                GetRequiredRequestFeedback(newRequest));
        if (newRequest.EarliestSendTimeTicks <
            pendingRequest.EarliestSendTimeTicks)
        {
            pendingRequest.EarliestSendTimeTicks =
                newRequest.EarliestSendTimeTicks;
        }

        if (pendingRequest.ExpirationTimeTicks == 0 ||
            newRequest.ExpirationTimeTicks == 0)
        {
            pendingRequest.ExpirationTimeTicks = 0;
        }
        else if (newRequest.ExpirationTimeTicks >
            pendingRequest.ExpirationTimeTicks)
        {
            pendingRequest.ExpirationTimeTicks =
                newRequest.ExpirationTimeTicks;
        }

        if (GetFeedbackTimingPriority(newRequest.PreferredTimingMode) >
            GetFeedbackTimingPriority(pendingRequest.PreferredTimingMode))
        {
            pendingRequest.PreferredTimingMode =
                newRequest.PreferredTimingMode;
        }

        pendingRequest.AllowSuppression = pendingRequest.AllowSuppression &&
            newRequest.AllowSuppression;
    }

    internal int GetFeedbackTimingPriority(RtcpFeedbackTimingMode timingMode)
    {
        if (timingMode == RtcpFeedbackTimingMode.Immediate)
        {
            return 3;
        }

        if (timingMode == RtcpFeedbackTimingMode.ReducedSize)
        {
            return 2;
        }

        if (timingMode == RtcpFeedbackTimingMode.Early)
        {
            return 1;
        }

        return 0;
    }

    internal bool HasSuppressingFeedback(
        RtpFeedbackRequest request,
        long currentTimeTicks)
    {
        long firstRelevantTimeTicks = 0L;
        if (request.EarliestSendTimeTicks > configuration.FeedbackRetentionTicks)
        {
            firstRelevantTimeTicks = request.EarliestSendTimeTicks -
                configuration.FeedbackRetentionTicks;
        }

        int feedbackIndex = 0;
        while (feedbackIndex < observedFeedback.Count)
        {
            long arrivalTimeTicks =
                observedFeedbackArrivalTimes[feedbackIndex];
            if (arrivalTimeTicks >= firstRelevantTimeTicks &&
                arrivalTimeTicks <= currentTimeTicks &&
                RtcpFeedbackSemanticComparer.IsFeedbackSuperset(
                    observedFeedback[feedbackIndex],
                    GetRequiredRequestFeedback(request)))
            {
                return true;
            }

            feedbackIndex = feedbackIndex + 1;
        }

        return false;
    }

    internal void PruneObservedFeedback(long currentTimeTicks)
    {
        long retentionCutoffTicks = 0L;
        if (currentTimeTicks > configuration.FeedbackRetentionTicks)
        {
            retentionCutoffTicks = currentTimeTicks -
                configuration.FeedbackRetentionTicks;
        }

        while (observedFeedbackArrivalTimes.Count > 0 &&
            observedFeedbackArrivalTimes[0] < retentionCutoffTicks)
        {
            observedFeedbackArrivalTimes.RemoveAt(0);
            observedFeedback.RemoveAt(0);
        }
    }

    internal bool IsFeedbackRequestExpired(
        RtpFeedbackRequest request,
        long currentTimeTicks)
    {
        return request.ExpirationTimeTicks > 0 &&
            currentTimeTicks > request.ExpirationTimeTicks;
    }

    internal RtpFeedbackDecision CompleteFeedbackWithoutSending(
        RtpFeedbackRequest request,
        bool suppressed,
        long completionTimeTicks)
    {
        RtpFeedbackDecision decision = new RtpFeedbackDecision();
        decision.ShouldSend = false;
        decision.Suppressed = suppressed;
        decision.Piggyback = false;
        if (hasScheduledFeedback)
        {
            decision.TimingMode = scheduledTimingMode;
        }
        else
        {
            decision.TimingMode = request.PreferredTimingMode;
        }

        decision.ScheduledTimeTicks = completionTimeTicks;
        decision.Request = request.Copy();
        pendingRequests.RemoveAt(0);
        ClearScheduledFeedback();
        return decision;
    }

    internal void ClearScheduledFeedback()
    {
        hasScheduledFeedback = false;
        scheduledFeedbackTimeTicks = 0L;
        scheduledRegularTimeTicks = 0L;
        scheduledTimingMode = RtcpFeedbackTimingMode.Regular;
        scheduledPiggyback = false;
    }

    internal void ValidateFeedbackChronology(long currentTimeTicks)
    {
        if (hasSchedulerTime && currentTimeTicks < schedulerTimeTicks)
        {
            throw new RtpSessionException(
                "Feedback scheduler time cannot move backward.");
        }
    }

    internal void RecordFeedbackTime(long currentTimeTicks)
    {
        hasSchedulerTime = true;
        schedulerTimeTicks = currentTimeTicks;
    }

    internal RtcpFeedbackPacket GetRequiredRequestFeedback(
        RtpFeedbackRequest request)
    {
        RtcpFeedbackPacket? feedback = request.Feedback;
        if (feedback == null)
        {
            throw new RtpSessionException(
                "A retained feedback request has no feedback packet.");
        }

        return feedback;
    }

    internal double GetParticipantRtcpBandwidthBytesPerSecond(
        RtpSessionSnapshot snapshot)
    {
        double bandwidthBytesPerSecond =
            configuration.SessionBandwidthBitsPerSecond *
            configuration.RtcpBandwidthFraction / 8.0;
        if (snapshot.SenderCount <=
            snapshot.MemberCount * configuration.SenderBandwidthFraction)
        {
            if (HasActiveLocalSender(snapshot) && snapshot.SenderCount > 0)
            {
                return bandwidthBytesPerSecond *
                    configuration.SenderBandwidthFraction /
                    snapshot.SenderCount;
            }

            int receiverCount = snapshot.MemberCount - snapshot.SenderCount;
            if (receiverCount > 0)
            {
                return bandwidthBytesPerSecond *
                    configuration.ReceiverBandwidthFraction /
                    receiverCount;
            }
        }

        return bandwidthBytesPerSecond / snapshot.MemberCount;
    }

    internal bool HasActiveLocalSender(RtpSessionSnapshot snapshot)
    {
        int sourceIndex = 0;
        while (sourceIndex < snapshot.Sources.Length)
        {
            RtpSourceSnapshot source = snapshot.Sources[sourceIndex];
            if (source.IsLocal &&
                (source.IsSender || source.Status == RtpSourceStatus.ActiveSender))
            {
                return true;
            }

            sourceIndex = sourceIndex + 1;
        }

        return false;
    }

    internal int GetFeedbackAveragePacketSize(RtpSessionSnapshot snapshot)
    {
        if (snapshot.AverageRtcpPacketSize > 0)
        {
            return snapshot.AverageRtcpPacketSize;
        }

        return configuration.InitialAveragePacketSize;
    }

    internal long AddFeedbackTime(long timeTicks, long delayTicks)
    {
        if (timeTicks > long.MaxValue - delayTicks)
        {
            throw new RtpConfigurationException(
                "The scheduled feedback time is too large.");
        }

        return timeTicks + delayTicks;
    }

    internal RtpFeedbackDecision CreateFeedbackDecision(
        RtpFeedbackRequest request,
        bool shouldSend)
    {
        RtpFeedbackDecision decision = new RtpFeedbackDecision();
        decision.ShouldSend = shouldSend;
        decision.Piggyback = scheduledPiggyback;
        decision.TimingMode = scheduledTimingMode;
        decision.ScheduledTimeTicks = scheduledFeedbackTimeTicks;
        decision.Request = request.Copy();
        return decision;
    }

    internal void ValidateFeedbackRequest(RtpFeedbackRequest request)
    {
        if (request == null || request.Feedback == null)
        {
            throw new RtpConfigurationException(
                "A feedback request with a packet is required.");
        }

        if (request.EarliestSendTimeTicks < 0 || request.ExpirationTimeTicks < 0)
        {
            throw new RtpConfigurationException(
                "Feedback request times cannot be negative.");
        }

        if (request.ExpirationTimeTicks > 0 &&
            request.ExpirationTimeTicks < request.EarliestSendTimeTicks)
        {
            throw new RtpConfigurationException(
                "Feedback expiration cannot precede its earliest send time.");
        }

        if (request.PreferredTimingMode != RtcpFeedbackTimingMode.Regular &&
            request.PreferredTimingMode != RtcpFeedbackTimingMode.Early &&
            request.PreferredTimingMode != RtcpFeedbackTimingMode.Immediate &&
            request.PreferredTimingMode != RtcpFeedbackTimingMode.ReducedSize)
        {
            throw new RtpConfigurationException(
                "The preferred feedback timing mode is not recognized.");
        }
    }

    internal void ValidateFeedbackSnapshot(RtpSessionSnapshot snapshot)
    {
        if (snapshot == null || snapshot.Sources == null)
        {
            throw new RtpConfigurationException(
                "An RTP session snapshot with sources is required.");
        }

        if (snapshot.MemberCount <= 0 || snapshot.SenderCount < 0 ||
            snapshot.SenderCount > snapshot.MemberCount)
        {
            throw new RtpConfigurationException(
                "Feedback scheduling requires consistent member and sender counts.");
        }

        if (snapshot.NextRtcpSendTimeTicks < 0)
        {
            throw new RtpConfigurationException(
                "The next regular RTCP send time cannot be negative.");
        }

        int sourceIndex = 0;
        while (sourceIndex < snapshot.Sources.Length)
        {
            if (snapshot.Sources[sourceIndex] == null)
            {
                throw new RtpConfigurationException(
                    "RTP source snapshots cannot contain null entries.");
            }

            sourceIndex = sourceIndex + 1;
        }
    }

    internal void ValidateFeedbackTime(long currentTimeTicks)
    {
        if (currentTimeTicks < 0)
        {
            throw new RtpConfigurationException(
                "The feedback scheduler time cannot be negative.");
        }
    }
}
