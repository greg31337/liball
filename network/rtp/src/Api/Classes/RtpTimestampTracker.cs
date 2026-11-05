namespace rtp;

/// <summary>
/// Extends RTP timestamps onto a signed media timeline and detects discontinuities.
/// </summary>
public class RtpTimestampTracker : IRtpTimestampTracker
{
    internal const long TimestampModulus = 4294967296L;
    internal const long TimestampHalfRange = 2147483648L;

    internal object syncRoot = new object();
    internal bool hasTimestamp;
    internal uint timelineHeadTimestamp;
    internal long timelineHeadExtendedTimestamp;
    internal int activeClockRate;
    internal RtpTimestampUpdate currentUpdate = new RtpTimestampUpdate();
    internal RtpClockCorrelation currentCorrelation = new RtpClockCorrelation();

    /// <summary>Initializes an empty RTP timestamp tracker.</summary>
    public RtpTimestampTracker()
    {
    }

    /// <summary>Processes one RTP timestamp and isolates changes in media clock rate.</summary>
    /// <param name="timestamp">The 32-bit RTP timestamp.</param>
    /// <param name="clockRate">The payload clock rate in ticks per second.</param>
    /// <returns>The resulting extended timestamp update.</returns>
    public RtpTimestampUpdate ProcessRtpTimestamp(uint timestamp, int clockRate)
    {
        if (clockRate <= 0)
        {
            throw new RtpConfigurationException(
                "The RTP timestamp clock rate must be positive.");
        }

        lock (syncRoot)
        {
            if (!hasTimestamp)
            {
                InitializeTimestampTimeline(timestamp, clockRate);
                currentUpdate = CreateTimestampUpdate(
                    timestamp,
                    timestamp,
                    0,
                    true,
                    false,
                    false,
                    false,
                    false);
                return currentUpdate.Copy();
            }

            if (clockRate != activeClockRate)
            {
                InvalidateClockCorrelation();
                InitializeTimestampTimeline(timestamp, clockRate);
                currentUpdate = CreateTimestampUpdate(
                    timestamp,
                    timestamp,
                    0,
                    true,
                    false,
                    false,
                    true,
                    true);
                return currentUpdate.Copy();
            }

            long delta = (long)timestamp - (long)timelineHeadTimestamp;
            if (delta == TimestampHalfRange || delta == -TimestampHalfRange)
            {
                InvalidateClockCorrelation();
                InitializeTimestampTimeline(timestamp, clockRate);
                currentUpdate = CreateTimestampUpdate(
                    timestamp,
                    timestamp,
                    0,
                    true,
                    false,
                    false,
                    true,
                    false);
                return currentUpdate.Copy();
            }

            if (delta > TimestampHalfRange)
            {
                delta = delta - TimestampModulus;
            }
            else if (delta < -TimestampHalfRange)
            {
                delta = delta + TimestampModulus;
            }

            if ((delta > 0 && timelineHeadExtendedTimestamp > long.MaxValue - delta) ||
                (delta < 0 && timelineHeadExtendedTimestamp < long.MinValue - delta))
            {
                InvalidateClockCorrelation();
                InitializeTimestampTimeline(timestamp, clockRate);
                currentUpdate = CreateTimestampUpdate(
                    timestamp,
                    timestamp,
                    0,
                    true,
                    false,
                    false,
                    true,
                    false);
                return currentUpdate.Copy();
            }

            long extendedTimestamp = timelineHeadExtendedTimestamp + delta;
            if (delta > 0)
            {
                timelineHeadTimestamp = timestamp;
                timelineHeadExtendedTimestamp = extendedTimestamp;
                currentUpdate = CreateTimestampUpdate(
                    timestamp,
                    extendedTimestamp,
                    delta,
                    true,
                    true,
                    false,
                    false,
                    false);
            }
            else if (delta < 0)
            {
                currentUpdate = CreateTimestampUpdate(
                    timestamp,
                    extendedTimestamp,
                    delta,
                    true,
                    false,
                    true,
                    false,
                    false);
            }
            else
            {
                currentUpdate = CreateTimestampUpdate(
                    timestamp,
                    extendedTimestamp,
                    0,
                    true,
                    false,
                    false,
                    false,
                    false);
            }

            return currentUpdate.Copy();
        }
    }

    /// <summary>Creates a clock correlation from one RTCP sender report.</summary>
    /// <param name="senderReport">The sender report containing corresponding clocks.</param>
    /// <param name="clockRate">The RTP timestamp clock rate for the report.</param>
    /// <param name="eraReferenceUtcTicks">UTC ticks selecting the nearest NTP era.</param>
    /// <returns>The accepted or rejected sender-report correlation.</returns>
    public RtpClockCorrelation CorrelateRtcpSenderReport(
        RtcpSenderReport senderReport,
        int clockRate,
        long eraReferenceUtcTicks)
    {
        if (senderReport == null || senderReport.SenderInformation == null)
        {
            throw new RtpConfigurationException(
                "An RTCP sender report with sender information is required.");
        }

        if (clockRate <= 0)
        {
            throw new RtpConfigurationException(
                "The sender-report RTP clock rate must be positive.");
        }

        lock (syncRoot)
        {
            RtcpSenderInformation information = senderReport.SenderInformation;
            if (information.NtpTimestamp == 0)
            {
                return CreateRejectedSenderReportCorrelation(senderReport, clockRate);
            }

            long presentationTimeTicks = RtpNtpTime.ToUtcTicks(
                information.NtpTimestamp,
                eraReferenceUtcTicks);
            if (!RtpClockMath.IsUtcClockCorrelationSafe(presentationTimeTicks))
            {
                return CreateRejectedSenderReportCorrelation(senderReport, clockRate);
            }

            long extendedRtpTimestamp;
            if (!hasTimestamp)
            {
                InitializeTimestampTimeline(information.RtpTimestamp, clockRate);
                currentUpdate = CreateTimestampUpdate(
                    information.RtpTimestamp,
                    information.RtpTimestamp,
                    0,
                    true,
                    false,
                    false,
                    false,
                    false);
                extendedRtpTimestamp = information.RtpTimestamp;
            }
            else
            {
                if (activeClockRate != clockRate)
                {
                    return CreateRejectedSenderReportCorrelation(senderReport, clockRate);
                }

                long delta =
                    (long)information.RtpTimestamp - (long)timelineHeadTimestamp;
                if (delta == TimestampHalfRange || delta == -TimestampHalfRange)
                {
                    return CreateRejectedSenderReportCorrelation(senderReport, clockRate);
                }

                if (delta > TimestampHalfRange)
                {
                    delta = delta - TimestampModulus;
                }
                else if (delta < -TimestampHalfRange)
                {
                    delta = delta + TimestampModulus;
                }

                if ((delta > 0 &&
                    timelineHeadExtendedTimestamp > long.MaxValue - delta) ||
                    (delta < 0 &&
                    timelineHeadExtendedTimestamp < long.MinValue - delta))
                {
                    return CreateRejectedSenderReportCorrelation(senderReport, clockRate);
                }

                extendedRtpTimestamp = timelineHeadExtendedTimestamp + delta;
            }

            RtpClockCorrelation correlation = new RtpClockCorrelation();
            correlation.IsValid = true;
            correlation.SourceIdentifier = senderReport.SenderSynchronizationSource;
            correlation.RtpTimestamp = information.RtpTimestamp;
            correlation.ExtendedRtpTimestamp = extendedRtpTimestamp;
            correlation.NtpTimestamp = information.NtpTimestamp;
            correlation.ClockRate = clockRate;
            correlation.PresentationTimeTicks = presentationTimeTicks;
            currentCorrelation = correlation;
            return currentCorrelation.Copy();
        }
    }

    /// <summary>Maps a timestamp update onto the correlated presentation timeline.</summary>
    /// <param name="timestampUpdate">An update from the matching timestamp domain.</param>
    /// <returns>The timestamp's presentation-time correlation.</returns>
    public RtpClockCorrelation ConvertRtpTimestampToPresentationTime(
        RtpTimestampUpdate timestampUpdate)
    {
        if (timestampUpdate == null)
        {
            throw new RtpConfigurationException("An RTP timestamp update is required.");
        }

        lock (syncRoot)
        {
            if (!currentCorrelation.IsValid ||
                !timestampUpdate.Accepted ||
                timestampUpdate.IsDiscontinuity ||
                timestampUpdate.ClockRateChanged ||
                timestampUpdate.ClockRate != currentCorrelation.ClockRate)
            {
                return CreateRejectedTimestampCorrelation(timestampUpdate);
            }

            long correlatedTimestamp = currentCorrelation.ExtendedRtpTimestamp;
            long extendedTimestamp = timestampUpdate.ExtendedTimestamp;
            if ((correlatedTimestamp > 0 &&
                extendedTimestamp < long.MinValue + correlatedTimestamp) ||
                (correlatedTimestamp < 0 &&
                extendedTimestamp > long.MaxValue + correlatedTimestamp))
            {
                return CreateRejectedTimestampCorrelation(timestampUpdate);
            }

            long timestampDelta = extendedTimestamp - correlatedTimestamp;
            long wholeSeconds = timestampDelta / currentCorrelation.ClockRate;
            long remainingTimestampUnits =
                timestampDelta % currentCorrelation.ClockRate;
            if (wholeSeconds > long.MaxValue / RtpNtpTime.TimeSpanTicksPerSecond ||
                wholeSeconds < long.MinValue / RtpNtpTime.TimeSpanTicksPerSecond)
            {
                return CreateRejectedTimestampCorrelation(timestampUpdate);
            }

            long presentationOffset =
                wholeSeconds * RtpNtpTime.TimeSpanTicksPerSecond;
            long fractionalOffset =
                (remainingTimestampUnits * RtpNtpTime.TimeSpanTicksPerSecond) /
                currentCorrelation.ClockRate;
            if ((fractionalOffset > 0 &&
                presentationOffset > long.MaxValue - fractionalOffset) ||
                (fractionalOffset < 0 &&
                presentationOffset < long.MinValue - fractionalOffset))
            {
                return CreateRejectedTimestampCorrelation(timestampUpdate);
            }

            presentationOffset = presentationOffset + fractionalOffset;
            if ((presentationOffset > 0 &&
                currentCorrelation.PresentationTimeTicks >
                long.MaxValue - presentationOffset) ||
                (presentationOffset < 0 &&
                currentCorrelation.PresentationTimeTicks <
                long.MinValue - presentationOffset))
            {
                return CreateRejectedTimestampCorrelation(timestampUpdate);
            }

            long presentationTimeTicks =
                currentCorrelation.PresentationTimeTicks + presentationOffset;
            if (presentationTimeTicks < RtpNtpTime.NtpEpochTicks ||
                presentationTimeTicks > RtpNtpTime.MaximumUtcTicks)
            {
                return CreateRejectedTimestampCorrelation(timestampUpdate);
            }

            RtpClockCorrelation result = new RtpClockCorrelation();
            result.IsValid = true;
            result.SourceIdentifier = currentCorrelation.SourceIdentifier;
            result.RtpTimestamp = timestampUpdate.Timestamp;
            result.ExtendedRtpTimestamp = timestampUpdate.ExtendedTimestamp;
            result.NtpTimestamp = RtpNtpTime.FromUtcTicks(presentationTimeTicks);
            result.ClockRate = currentCorrelation.ClockRate;
            result.PresentationTimeTicks = presentationTimeTicks;
            return result;
        }
    }

    /// <summary>Returns an independent snapshot of current timestamp state.</summary>
    /// <returns>The current timestamp state.</returns>
    public RtpTimestampUpdate GetRtpTimestampState()
    {
        lock (syncRoot)
        {
            return currentUpdate.Copy();
        }
    }

    /// <summary>Returns an independent snapshot of the current clock correlation.</summary>
    /// <returns>The current sender-report correlation.</returns>
    public RtpClockCorrelation GetRtpClockCorrelation()
    {
        lock (syncRoot)
        {
            return currentCorrelation.Copy();
        }
    }

    /// <summary>Resets timestamp tracking around a supplied value and clock rate.</summary>
    /// <param name="timestamp">The new initial RTP timestamp.</param>
    /// <param name="clockRate">The new timestamp clock rate.</param>
    /// <returns>The reset timestamp state.</returns>
    public RtpTimestampUpdate ResetRtpTimestampTracker(uint timestamp, int clockRate)
    {
        if (clockRate <= 0)
        {
            throw new RtpConfigurationException(
                "The RTP timestamp clock rate must be positive.");
        }

        lock (syncRoot)
        {
            bool clockRateChanged = hasTimestamp && activeClockRate != clockRate;
            InvalidateClockCorrelation();
            InitializeTimestampTimeline(timestamp, clockRate);
            currentUpdate = CreateTimestampUpdate(
                timestamp,
                timestamp,
                0,
                true,
                false,
                false,
                true,
                clockRateChanged);
            return currentUpdate.Copy();
        }
    }

    internal void InitializeTimestampTimeline(uint timestamp, int clockRate)
    {
        hasTimestamp = true;
        timelineHeadTimestamp = timestamp;
        timelineHeadExtendedTimestamp = timestamp;
        activeClockRate = clockRate;
    }

    internal RtpTimestampUpdate CreateTimestampUpdate(
        uint timestamp,
        long extendedTimestamp,
        long delta,
        bool accepted,
        bool isForward,
        bool isReordered,
        bool isDiscontinuity,
        bool clockRateChanged)
    {
        RtpTimestampUpdate update = new RtpTimestampUpdate();
        update.Timestamp = timestamp;
        update.ExtendedTimestamp = extendedTimestamp;
        update.Delta = delta;
        update.ClockRate = activeClockRate;
        update.Accepted = accepted;
        update.IsForward = isForward;
        update.IsReordered = isReordered;
        update.IsDiscontinuity = isDiscontinuity;
        update.ClockRateChanged = clockRateChanged;
        return update;
    }

    internal RtpClockCorrelation CreateRejectedSenderReportCorrelation(
        RtcpSenderReport senderReport,
        int clockRate)
    {
        RtpClockCorrelation correlation = new RtpClockCorrelation();
        correlation.SourceIdentifier = senderReport.SenderSynchronizationSource;
        correlation.RtpTimestamp = senderReport.SenderInformation.RtpTimestamp;
        correlation.NtpTimestamp = senderReport.SenderInformation.NtpTimestamp;
        correlation.ClockRate = clockRate;
        return correlation;
    }

    internal RtpClockCorrelation CreateRejectedTimestampCorrelation(
        RtpTimestampUpdate timestampUpdate)
    {
        RtpClockCorrelation correlation = new RtpClockCorrelation();
        correlation.SourceIdentifier = currentCorrelation.SourceIdentifier;
        correlation.RtpTimestamp = timestampUpdate.Timestamp;
        correlation.ExtendedRtpTimestamp = timestampUpdate.ExtendedTimestamp;
        correlation.ClockRate = timestampUpdate.ClockRate;
        return correlation;
    }

    internal void InvalidateClockCorrelation()
    {
        currentCorrelation = new RtpClockCorrelation();
    }
}
