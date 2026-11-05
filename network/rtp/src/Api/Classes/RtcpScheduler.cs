using System;

namespace rtp;

/// <summary>Maintains deterministic RFC 3550 RTCP scheduling state.</summary>
public class RtcpScheduler : IRtcpScheduler
{
    internal object syncRoot = new object();
    internal IRtpRandomSource randomSource;
    internal RtcpSchedulerConfiguration? configuration;
    internal bool initialized;
    internal bool isInitial;
    internal int memberCount;
    internal int senderCount;
    internal int previousMemberCount;
    internal double averagePacketSize;
    internal long previousReportTimeTicks;
    internal long nextReportTimeTicks;
    internal long currentSchedulerTimeTicks;
    internal bool goodbyePending;
    internal bool localSenderActive;

    /// <summary>Initializes an RTCP scheduler with an injected random source.</summary>
    /// <param name="randomSource">The source of RTCP interval randomization.</param>
    public RtcpScheduler(IRtpRandomSource randomSource)
    {
        if (randomSource == null)
        {
            throw new RtpConfigurationException(
                "An RTP random source is required for RTCP scheduling.");
        }

        this.randomSource = randomSource;
    }

    /// <summary>Initializes regular RTCP scheduling at a monotonic time.</summary>
    /// <param name="configuration">The validated scheduler configuration.</param>
    /// <param name="currentTimeTicks">The current monotonic time.</param>
    public void InitializeRtcpSchedule(
        RtcpSchedulerConfiguration configuration,
        long currentTimeTicks)
    {
        RtpConfigurationValidator.ValidateRtcpSchedulerConfiguration(configuration);
        ValidateNonnegativeTime(currentTimeTicks);
        RtcpSchedulerConfiguration configurationCopy = configuration.Copy();
        double randomValue = randomSource.CreateUnitIntervalValue();
        double intervalSeconds = RtcpIntervalCalculator.CalculateIntervalSeconds(
            configurationCopy,
            1,
            0,
            false,
            configurationCopy.InitialAveragePacketSize,
            true,
            randomValue);
        long intervalTicks = ConvertIntervalToTicks(intervalSeconds);
        long scheduledTimeTicks = AddIntervalToTime(
            currentTimeTicks,
            intervalTicks);

        lock (syncRoot)
        {
            this.configuration = configurationCopy;
            initialized = true;
            isInitial = true;
            memberCount = 1;
            senderCount = 0;
            previousMemberCount = 1;
            averagePacketSize = configurationCopy.InitialAveragePacketSize;
            previousReportTimeTicks = currentTimeTicks;
            nextReportTimeTicks = scheduledTimeTicks;
            currentSchedulerTimeTicks = currentTimeTicks;
            goodbyePending = false;
            localSenderActive = false;
        }
    }

    /// <summary>Records an observed compound RTCP size for interval averaging.</summary>
    /// <param name="packetSize">The compound packet size in bytes.</param>
    public void RecordRtcpPacketSize(int packetSize)
    {
        if (packetSize <= 0)
        {
            throw new RtpConfigurationException(
                "The observed RTCP packet size must be positive.");
        }

        lock (syncRoot)
        {
            EnsureSchedulerInitialized();
            averagePacketSize =
                (RtcpProtocolConstants.AverageSizeSampleWeight * packetSize +
                RtcpProtocolConstants.AverageSizePreviousWeight * averagePacketSize) /
                RtcpProtocolConstants.AverageSizeWeightDenominator;
        }
    }

    /// <summary>Applies timer reconsideration after membership changes.</summary>
    /// <param name="memberCount">The current session member count.</param>
    /// <param name="senderCount">The current active sender count.</param>
    /// <param name="currentTimeTicks">The current monotonic time.</param>
    public void ReconsiderRtcpSchedule(
        int memberCount,
        int senderCount,
        long currentTimeTicks)
    {
        ValidateMemberAndSenderCounts(memberCount, senderCount);
        ValidateNonnegativeTime(currentTimeTicks);
        lock (syncRoot)
        {
            EnsureSchedulerInitialized();
            ValidateAdvancingTime(currentTimeTicks);
            ApplyReconsideredMemberCounts(
                memberCount,
                senderCount,
                currentTimeTicks);
        }
    }

    /// <summary>Polls the regular RTCP scheduler.</summary>
    /// <param name="snapshot">The current independent session snapshot.</param>
    /// <param name="currentTimeTicks">The current monotonic time.</param>
    /// <returns>The current scheduling decision.</returns>
    public RtcpScheduleDecision PollRtcpSchedule(
        RtpSessionSnapshot snapshot,
        long currentTimeTicks)
    {
        ValidateScheduleSnapshot(snapshot);
        ValidateNonnegativeTime(currentTimeTicks);
        lock (syncRoot)
        {
            EnsureSchedulerInitialized();
            ValidateAdvancingTime(currentTimeTicks);
            if (goodbyePending)
            {
                throw new RtpSessionException(
                    "A regular RTCP report cannot be polled while BYE is pending.");
            }

            ApplyInactivityTimeouts(snapshot, currentTimeTicks);
            if (currentTimeTicks < nextReportTimeTicks)
            {
                return CreateRegularScheduleDecision(
                    false,
                    nextReportTimeTicks,
                    currentTimeTicks,
                    isInitial,
                    localSenderActive);
            }

            RtcpSchedulerConfiguration currentConfiguration =
                GetSchedulerConfiguration();
            int intervalAveragePacketSize = (int)Math.Ceiling(averagePacketSize);
            bool intervalIsInitial = isInitial;
            double reconsideredIntervalSeconds =
                RtcpIntervalCalculator.CalculateIntervalSeconds(
                    currentConfiguration,
                    memberCount,
                    senderCount,
                    localSenderActive,
                    intervalAveragePacketSize,
                    intervalIsInitial,
                    randomSource.CreateUnitIntervalValue());
            long reconsideredIntervalTicks = ConvertIntervalToTicks(
                reconsideredIntervalSeconds);
            long reconsideredTimeTicks = AddIntervalToTime(
                previousReportTimeTicks,
                reconsideredIntervalTicks);
            if (reconsideredTimeTicks > currentTimeTicks)
            {
                nextReportTimeTicks = reconsideredTimeTicks;
                previousMemberCount = memberCount;
                currentSchedulerTimeTicks = currentTimeTicks;
                return CreateRegularScheduleDecision(
                    false,
                    nextReportTimeTicks,
                    currentTimeTicks,
                    intervalIsInitial,
                    localSenderActive);
            }

            double nextIntervalSeconds =
                RtcpIntervalCalculator.CalculateIntervalSeconds(
                    currentConfiguration,
                    memberCount,
                    senderCount,
                    localSenderActive,
                    intervalAveragePacketSize,
                    intervalIsInitial,
                    randomSource.CreateUnitIntervalValue());
            long nextIntervalTicks = ConvertIntervalToTicks(nextIntervalSeconds);
            long scheduledNextTimeTicks = AddIntervalToTime(
                currentTimeTicks,
                nextIntervalTicks);
            previousReportTimeTicks = currentTimeTicks;
            nextReportTimeTicks = scheduledNextTimeTicks;
            previousMemberCount = memberCount;
            currentSchedulerTimeTicks = currentTimeTicks;
            isInitial = false;
            return CreateRegularScheduleDecision(
                true,
                currentTimeTicks,
                currentTimeTicks,
                intervalIsInitial,
                localSenderActive);
        }
    }

    /// <summary>Schedules a goodbye transmission using RTCP backoff rules.</summary>
    /// <param name="snapshot">The current independent session snapshot.</param>
    /// <param name="currentTimeTicks">The current monotonic time.</param>
    /// <returns>The goodbye scheduling decision.</returns>
    public RtcpScheduleDecision ScheduleRtcpGoodbye(
        RtpSessionSnapshot snapshot,
        long currentTimeTicks)
    {
        ValidateScheduleSnapshot(snapshot);
        ValidateNonnegativeTime(currentTimeTicks);
        lock (syncRoot)
        {
            RtcpSchedulerConfiguration currentConfiguration =
                GetSchedulerConfiguration();
            ValidateAdvancingTime(currentTimeTicks);
            if (!goodbyePending)
            {
                if (snapshot.MemberCount <=
                    RtcpProtocolConstants.GoodbyeImmediateMemberThreshold)
                {
                    currentSchedulerTimeTicks = currentTimeTicks;
                    return CreateGoodbyeDecision(
                        true,
                        currentTimeTicks,
                        currentTimeTicks,
                        snapshot.MemberCount,
                        snapshot.SenderCount,
                        HasLocalSender(snapshot));
                }

                int goodbyeAveragePacketSize = snapshot.AverageRtcpPacketSize;
                if (goodbyeAveragePacketSize <= 0)
                {
                    goodbyeAveragePacketSize = (int)Math.Ceiling(averagePacketSize);
                }

                double intervalSeconds =
                    RtcpIntervalCalculator.CalculateIntervalSeconds(
                        currentConfiguration,
                        1,
                        0,
                        false,
                        goodbyeAveragePacketSize,
                        true,
                        randomSource.CreateUnitIntervalValue());
                long intervalTicks = ConvertIntervalToTicks(intervalSeconds);
                long scheduledTimeTicks = AddIntervalToTime(
                    currentTimeTicks,
                    intervalTicks);
                memberCount = 1;
                senderCount = 0;
                previousMemberCount = 1;
                averagePacketSize = goodbyeAveragePacketSize;
                previousReportTimeTicks = currentTimeTicks;
                nextReportTimeTicks = scheduledTimeTicks;
                currentSchedulerTimeTicks = currentTimeTicks;
                isInitial = true;
                goodbyePending = true;
                localSenderActive = false;
            }

            bool shouldSend = currentTimeTicks >= nextReportTimeTicks;
            RtcpScheduleDecision decision = CreateGoodbyeDecision(
                shouldSend,
                nextReportTimeTicks,
                currentTimeTicks,
                memberCount,
                senderCount,
                false);
            currentSchedulerTimeTicks = currentTimeTicks;
            if (shouldSend)
            {
                goodbyePending = false;
            }

            return decision;
        }
    }

    /// <summary>Resets scheduler interval and averaging state.</summary>
    /// <param name="currentTimeTicks">The reset monotonic time.</param>
    public void ResetRtcpScheduler(long currentTimeTicks)
    {
        ValidateNonnegativeTime(currentTimeTicks);
        RtcpSchedulerConfiguration currentConfiguration;
        lock (syncRoot)
        {
            currentConfiguration = GetSchedulerConfiguration();
        }

        InitializeRtcpSchedule(currentConfiguration, currentTimeTicks);
    }

    internal void EnsureSchedulerInitialized()
    {
        if (!initialized)
        {
            throw new RtpSessionException(
                "The RTCP scheduler has not been initialized.");
        }
    }

    internal void ApplyInactivityTimeouts(
        RtpSessionSnapshot snapshot,
        long currentTimeTicks)
    {
        ValidateScheduleSnapshot(snapshot);
        ValidateNonnegativeTime(currentTimeTicks);
        lock (syncRoot)
        {
            RtcpSchedulerConfiguration currentConfiguration =
                GetSchedulerConfiguration();
            ValidateAdvancingTime(currentTimeTicks);
            int activeMemberCount = snapshot.MemberCount;
            int activeSenderCount = snapshot.SenderCount;
            int timeoutAveragePacketSize = (int)Math.Ceiling(averagePacketSize);
            double deterministicIntervalSeconds =
                RtcpIntervalCalculator.CalculateDeterministicIntervalSeconds(
                    currentConfiguration,
                    snapshot.MemberCount,
                    snapshot.SenderCount,
                    false,
                    timeoutAveragePacketSize,
                    isInitial);
            long deterministicIntervalTicks = ConvertIntervalToTicks(
                deterministicIntervalSeconds);
            long senderTimeoutTicks = currentConfiguration.SenderTimeoutTicks;
            if (senderTimeoutTicks == 0)
            {
                senderTimeoutTicks = MultiplyTimeoutInterval(
                    deterministicIntervalTicks,
                    RtcpProtocolConstants.SenderTimeoutIntervalMultiplier);
            }

            long memberTimeoutTicks = currentConfiguration.MemberTimeoutTicks;
            if (memberTimeoutTicks == 0)
            {
                memberTimeoutTicks = MultiplyTimeoutInterval(
                    deterministicIntervalTicks,
                    RtcpProtocolConstants.MemberTimeoutIntervalMultiplier);
            }

            int sourceIndex = 0;
            bool hasActiveLocalSender = false;
            while (sourceIndex < snapshot.Sources.Length)
            {
                RtpSourceSnapshot source = snapshot.Sources[sourceIndex];
                ValidateSourceActivity(source, currentTimeTicks);
                bool isMember = IsActiveMember(source);
                bool isSender = IsActiveSender(source);
                bool memberTimedOut = false;
                bool senderTimedOut = false;
                if (!source.IsLocal && isMember && memberTimeoutTicks > 0)
                {
                    long latestActivity = GetLatestSourceActivity(source);
                    memberTimedOut = HasActivityTimedOut(
                        latestActivity,
                        currentTimeTicks,
                        memberTimeoutTicks);
                }

                if (!memberTimedOut && isSender && senderTimeoutTicks > 0)
                {
                    senderTimedOut = HasActivityTimedOut(
                        source.LastRtpActivityTicks,
                        currentTimeTicks,
                        senderTimeoutTicks);
                }

                if (memberTimedOut)
                {
                    if (activeMemberCount > 1)
                    {
                        activeMemberCount = activeMemberCount - 1;
                    }

                    if (isSender && activeSenderCount > 0)
                    {
                        activeSenderCount = activeSenderCount - 1;
                    }
                }
                else if (senderTimedOut)
                {
                    if (activeSenderCount > 0)
                    {
                        activeSenderCount = activeSenderCount - 1;
                    }
                }

                if (source.IsLocal && isSender && !senderTimedOut)
                {
                    hasActiveLocalSender = true;
                }

                sourceIndex = sourceIndex + 1;
            }

            if (activeSenderCount > activeMemberCount)
            {
                activeSenderCount = activeMemberCount;
            }

            localSenderActive = hasActiveLocalSender;
            ApplyReconsideredMemberCounts(
                activeMemberCount,
                activeSenderCount,
                currentTimeTicks);
        }
    }

    internal void ApplyReconsideredMemberCounts(
        int updatedMemberCount,
        int updatedSenderCount,
        long currentTimeTicks)
    {
        double membershipRatio =
            updatedMemberCount / (double)previousMemberCount;
        long reconsideredNextTime = ScaleTimeAroundCurrent(
            nextReportTimeTicks,
            currentTimeTicks,
            membershipRatio);
        long reconsideredPreviousTime = ScaleTimeAroundCurrent(
            previousReportTimeTicks,
            currentTimeTicks,
            membershipRatio);

        memberCount = updatedMemberCount;
        senderCount = updatedSenderCount;
        previousMemberCount = updatedMemberCount;
        nextReportTimeTicks = reconsideredNextTime;
        previousReportTimeTicks = reconsideredPreviousTime;
        currentSchedulerTimeTicks = currentTimeTicks;
    }

    internal void ValidateScheduleSnapshot(RtpSessionSnapshot snapshot)
    {
        if (snapshot == null)
        {
            throw new RtpConfigurationException(
                "An RTP session snapshot is required for RTCP scheduling.");
        }

        ValidateMemberAndSenderCounts(snapshot.MemberCount, snapshot.SenderCount);
        if (snapshot.Sources == null)
        {
            throw new RtpConfigurationException(
                "RTP source snapshots are required for RTCP scheduling.");
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

    internal void ValidateSourceActivity(
        RtpSourceSnapshot source,
        long currentTimeTicks)
    {
        if (source.LastRtpActivityTicks < 0 ||
            source.LastRtcpActivityTicks < 0)
        {
            throw new RtpConfigurationException(
                "RTP source activity times cannot be negative.");
        }

        if (source.LastRtpActivityTicks > currentTimeTicks ||
            source.LastRtcpActivityTicks > currentTimeTicks)
        {
            throw new RtpSessionException(
                "RTP source activity cannot be later than scheduler time.");
        }
    }

    internal bool IsActiveMember(RtpSourceSnapshot source)
    {
        return source.Status == RtpSourceStatus.ActiveMember ||
            source.Status == RtpSourceStatus.ActiveSender;
    }

    internal bool IsActiveSender(RtpSourceSnapshot source)
    {
        return IsActiveMember(source) &&
            (source.IsSender || source.Status == RtpSourceStatus.ActiveSender);
    }

    internal long GetLatestSourceActivity(RtpSourceSnapshot source)
    {
        if (source.LastRtpActivityTicks > source.LastRtcpActivityTicks)
        {
            return source.LastRtpActivityTicks;
        }

        return source.LastRtcpActivityTicks;
    }

    internal bool HasActivityTimedOut(
        long activityTimeTicks,
        long currentTimeTicks,
        long timeoutTicks)
    {
        return currentTimeTicks - activityTimeTicks >= timeoutTicks;
    }

    internal long MultiplyTimeoutInterval(
        long intervalTicks,
        int multiplier)
    {
        if (intervalTicks > long.MaxValue / multiplier)
        {
            return long.MaxValue;
        }

        return intervalTicks * multiplier;
    }

    internal bool HasLocalSender(RtpSessionSnapshot snapshot)
    {
        int sourceIndex = 0;
        while (sourceIndex < snapshot.Sources.Length)
        {
            RtpSourceSnapshot source = snapshot.Sources[sourceIndex];
            if (source.IsLocal && IsActiveSender(source))
            {
                return true;
            }

            sourceIndex = sourceIndex + 1;
        }

        return false;
    }

    internal RtcpScheduleDecision CreateGoodbyeDecision(
        bool shouldSend,
        long scheduledTimeTicks,
        long currentTimeTicks,
        int memberCount,
        int senderCount,
        bool localIsSender)
    {
        RtcpScheduleDecision decision = new RtcpScheduleDecision();
        decision.ShouldSend = shouldSend;
        decision.IsInitial = isInitial;
        decision.IsSender = localIsSender;
        decision.IsGoodbye = true;
        decision.ScheduledTimeTicks = scheduledTimeTicks;
        if (scheduledTimeTicks > currentTimeTicks)
        {
            decision.DelayTicks = scheduledTimeTicks - currentTimeTicks;
        }

        decision.MemberCount = memberCount;
        decision.SenderCount = senderCount;
        return decision;
    }

    internal RtcpScheduleDecision CreateRegularScheduleDecision(
        bool shouldSend,
        long scheduledTimeTicks,
        long currentTimeTicks,
        bool decisionIsInitial,
        bool decisionIsSender)
    {
        RtcpScheduleDecision decision = new RtcpScheduleDecision();
        decision.ShouldSend = shouldSend;
        decision.IsInitial = decisionIsInitial;
        decision.IsSender = decisionIsSender;
        decision.ScheduledTimeTicks = scheduledTimeTicks;
        if (scheduledTimeTicks > currentTimeTicks)
        {
            decision.DelayTicks = scheduledTimeTicks - currentTimeTicks;
        }

        decision.MemberCount = memberCount;
        decision.SenderCount = senderCount;
        return decision;
    }

    internal RtcpSchedulerConfiguration GetSchedulerConfiguration()
    {
        EnsureSchedulerInitialized();
        RtcpSchedulerConfiguration? currentConfiguration = configuration;
        if (currentConfiguration == null)
        {
            throw new RtpSessionException(
                "The RTCP scheduler configuration is unavailable.");
        }

        return currentConfiguration;
    }

    internal void ValidateMemberAndSenderCounts(
        int memberCount,
        int senderCount)
    {
        if (memberCount <= 0)
        {
            throw new RtpConfigurationException(
                "The RTCP member count must be positive.");
        }

        if (senderCount < 0 || senderCount > memberCount)
        {
            throw new RtpConfigurationException(
                "The RTCP sender count must be between zero and the member count.");
        }
    }

    internal void ValidateNonnegativeTime(long currentTimeTicks)
    {
        if (currentTimeTicks < 0)
        {
            throw new RtpConfigurationException(
                "The current monotonic time cannot be negative.");
        }
    }

    internal void ValidateAdvancingTime(long currentTimeTicks)
    {
        if (currentTimeTicks < currentSchedulerTimeTicks)
        {
            throw new RtpSessionException(
                "RTCP scheduler time cannot move backward.");
        }
    }

    internal long ConvertIntervalToTicks(double intervalSeconds)
    {
        double intervalTicks = intervalSeconds * TimeSpan.TicksPerSecond;
        if (double.IsNaN(intervalTicks) || double.IsInfinity(intervalTicks) ||
            intervalTicks >= 9223372036854775808.0)
        {
            throw new RtpConfigurationException(
                "The calculated RTCP interval is too large.");
        }

        return (long)Math.Ceiling(intervalTicks);
    }

    internal long AddIntervalToTime(long currentTimeTicks, long intervalTicks)
    {
        if (currentTimeTicks > long.MaxValue - intervalTicks)
        {
            throw new RtpConfigurationException(
                "The scheduled RTCP transmission time is too large.");
        }

        return currentTimeTicks + intervalTicks;
    }

    internal long ScaleTimeAroundCurrent(
        long timeTicks,
        long currentTimeTicks,
        double scale)
    {
        double scaledTime = currentTimeTicks +
            ((double)timeTicks - currentTimeTicks) * scale;
        if (double.IsNaN(scaledTime) || double.IsInfinity(scaledTime) ||
            scaledTime < -9223372036854775808.0 ||
            scaledTime >= 9223372036854775808.0)
        {
            throw new RtpConfigurationException(
                "The reconsidered RTCP transmission time cannot be represented.");
        }

        return (long)Math.Round(scaledTime);
    }
}
