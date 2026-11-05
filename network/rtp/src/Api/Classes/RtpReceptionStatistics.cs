namespace rtp;

/// <summary>
/// Tracks RTP reception counts and RFC 3550 packet-loss intervals for one source.
/// </summary>
public class RtpReceptionStatistics : IRtpReceptionStatistics
{
    internal object syncRoot = new object();
    internal uint sourceIdentifier;
    internal bool hasSequenceState;
    internal uint baseExtendedSequenceNumber;
    internal uint highestExtendedSequenceNumber;
    internal long receivedPacketCount;
    internal long receivedOctetCount;
    internal long duplicatePacketCount;
    internal long reorderedPacketCount;
    internal long discardedPacketCount;
    internal long latePacketCount;
    internal long repairedPacketCount;
    internal long recoveredPacketCount;
    internal long expectedPacketCountPrior;
    internal long receivedPacketCountPrior;
    internal byte fractionLost;
    internal bool hasJitterSample;
    internal uint previousRtpTimestamp;
    internal long previousArrivalTimeTicks;
    internal long previousMediaClockTimestamp;
    internal int activeClockRate;
    internal long interarrivalJitterScaled;
    internal bool hasSenderReport;
    internal uint lastSenderReport;
    internal long senderReportArrivalTimeTicks;
    internal bool timedOut;
    internal bool hasBuiltReceptionReport;
    internal long lastReceptionReportTimeTicks;

    /// <summary>Initializes reception statistics for one RTP synchronization source.</summary>
    /// <param name="sourceIdentifier">The remote RTP source being measured.</param>
    public RtpReceptionStatistics(uint sourceIdentifier)
    {
        this.sourceIdentifier = sourceIdentifier;
    }

    /// <summary>Records one accepted RTP packet and its sequence outcome.</summary>
    /// <param name="packet">The accepted RTP packet.</param>
    /// <param name="context">The packet timing and repair context.</param>
    /// <param name="sequenceUpdate">The corresponding sequence update.</param>
    public void RecordReceivedRtpPacket(
        RtpPacket packet,
        RtpPacketContext context,
        RtpSequenceUpdate sequenceUpdate)
    {
        ValidateReceivedPacketInput(packet, context, sequenceUpdate);
        lock (syncRoot)
        {
            if (sequenceUpdate.Status == RtpSequenceStatus.Restarted)
            {
                ResetSequenceIntervalState();
            }

            if (hasSequenceState &&
                sequenceUpdate.Status != RtpSequenceStatus.Restarted)
            {
                if (sequenceUpdate.BaseExtendedSequenceNumber !=
                    baseExtendedSequenceNumber)
                {
                    throw new RtpSessionException(
                        "The RTP sequence base changed without a confirmed restart.");
                }

                if (sequenceUpdate.HighestExtendedSequenceNumber <
                    highestExtendedSequenceNumber)
                {
                    throw new RtpSessionException(
                        "The highest extended RTP sequence number moved backward.");
                }
            }

            if (sequenceUpdate.Status != RtpSequenceStatus.Restarted)
            {
                ValidateJitterProgress(context);
            }

            hasSequenceState = true;
            baseExtendedSequenceNumber = sequenceUpdate.BaseExtendedSequenceNumber;
            highestExtendedSequenceNumber =
                sequenceUpdate.HighestExtendedSequenceNumber;
            receivedPacketCount = AddSaturatedCount(receivedPacketCount, 1);
            receivedOctetCount = AddSaturatedCount(
                receivedOctetCount,
                packet.Payload.Length);
            if (sequenceUpdate.Status == RtpSequenceStatus.Duplicate)
            {
                duplicatePacketCount = AddSaturatedCount(duplicatePacketCount, 1);
            }

            if (sequenceUpdate.Status == RtpSequenceStatus.Reordered)
            {
                reorderedPacketCount = AddSaturatedCount(reorderedPacketCount, 1);
            }

            if (context.IsRepaired)
            {
                repairedPacketCount = AddSaturatedCount(repairedPacketCount, 1);
                if (sequenceUpdate.Accepted)
                {
                    recoveredPacketCount = AddSaturatedCount(
                        recoveredPacketCount,
                        1);
                }
            }

            if (context.IsLate)
            {
                latePacketCount = AddSaturatedCount(latePacketCount, 1);
            }

            UpdateInterarrivalJitter(packet.Timestamp, context);
            timedOut = false;
        }
    }

    /// <summary>Records one RTP packet discarded before normal acceptance.</summary>
    /// <param name="context">The timing and repair context of the discarded packet.</param>
    public void RecordDiscardedRtpPacket(RtpPacketContext context)
    {
        if (context == null)
        {
            throw new RtpConfigurationException(
                "The discarded RTP packet context is required.");
        }

        lock (syncRoot)
        {
            discardedPacketCount = AddSaturatedCount(discardedPacketCount, 1);
            if (context.IsLate)
            {
                latePacketCount = AddSaturatedCount(latePacketCount, 1);
            }

            if (context.IsRepaired)
            {
                repairedPacketCount = AddSaturatedCount(repairedPacketCount, 1);
            }

            timedOut = false;
        }
    }

    /// <summary>Records timing from a received RTCP sender report.</summary>
    /// <param name="senderReport">The received sender report.</param>
    /// <param name="arrivalTimeTicks">Its monotonic arrival time.</param>
    public void RecordReceivedSenderReport(
        RtcpSenderReport senderReport,
        long arrivalTimeTicks)
    {
        if (senderReport == null || senderReport.SenderInformation == null)
        {
            throw new RtpConfigurationException(
                "An RTCP sender report with sender information is required.");
        }

        if (senderReport.SenderSynchronizationSource != sourceIdentifier)
        {
            throw new RtpSessionException(
                "The RTCP sender report belongs to a different RTP source.");
        }

        if (arrivalTimeTicks < 0)
        {
            throw new RtpConfigurationException(
                "The sender-report arrival time cannot be negative.");
        }

        lock (syncRoot)
        {
            if (hasSenderReport &&
                arrivalTimeTicks < senderReportArrivalTimeTicks)
            {
                throw new RtpSessionException(
                    "The sender-report arrival time cannot move backward.");
            }

            hasSenderReport = true;
            lastSenderReport = RtpNtpTime.ToCompactNtp(
                senderReport.SenderInformation.NtpTimestamp);
            senderReportArrivalTimeTicks = arrivalTimeTicks;
        }
    }

    /// <summary>Builds an RTCP reception report from current interval statistics.</summary>
    /// <param name="currentTimeTicks">The current monotonic time.</param>
    /// <returns>An independently owned reception report.</returns>
    public RtcpReceptionReport BuildRtcpReceptionReport(long currentTimeTicks)
    {
        ValidateCurrentTime(currentTimeTicks);
        lock (syncRoot)
        {
            ValidateSenderReportTime(currentTimeTicks);
            ValidateReceptionReportTime(currentTimeTicks);
            long expectedPacketCount = GetExpectedPacketCount();
            long expectedInterval = expectedPacketCount - expectedPacketCountPrior;
            long receivedInterval = receivedPacketCount - receivedPacketCountPrior;
            fractionLost = CalculateFractionLost(expectedInterval, receivedInterval);
            expectedPacketCountPrior = expectedPacketCount;
            receivedPacketCountPrior = receivedPacketCount;

            RtcpReceptionReport report = new RtcpReceptionReport();
            report.SourceIdentifier = sourceIdentifier;
            report.FractionLost = fractionLost;
            report.CumulativePacketsLost = ClampCumulativePacketLoss(
                expectedPacketCount - receivedPacketCount);
            report.ExtendedHighestSequenceNumber = highestExtendedSequenceNumber;
            report.InterarrivalJitter = GetReportedInterarrivalJitter();
            report.LastSenderReport = lastSenderReport;
            report.DelaySinceLastSenderReport =
                GetDelaySinceLastSenderReport(currentTimeTicks);
            hasBuiltReceptionReport = true;
            lastReceptionReportTimeTicks = currentTimeTicks;
            return report;
        }
    }

    /// <summary>Returns an independent reception-statistics snapshot.</summary>
    /// <param name="currentTimeTicks">The current monotonic time.</param>
    /// <returns>The current reception statistics.</returns>
    public RtpReceptionStatisticsSnapshot GetReceptionStatisticsSnapshot(
        long currentTimeTicks)
    {
        ValidateCurrentTime(currentTimeTicks);
        lock (syncRoot)
        {
            ValidateSenderReportTime(currentTimeTicks);
            long expectedPacketCount = GetExpectedPacketCount();
            RtpReceptionStatisticsSnapshot snapshot =
                new RtpReceptionStatisticsSnapshot();
            snapshot.SourceIdentifier = sourceIdentifier;
            snapshot.BaseExtendedSequenceNumber = baseExtendedSequenceNumber;
            snapshot.HighestExtendedSequenceNumber = highestExtendedSequenceNumber;
            snapshot.ExpectedPacketCount = expectedPacketCount;
            snapshot.ReceivedPacketCount = receivedPacketCount;
            snapshot.ReceivedOctetCount = receivedOctetCount;
            snapshot.DuplicatePacketCount = duplicatePacketCount;
            snapshot.ReorderedPacketCount = reorderedPacketCount;
            snapshot.DiscardedPacketCount = discardedPacketCount;
            snapshot.LatePacketCount = latePacketCount;
            snapshot.RepairedPacketCount = repairedPacketCount;
            snapshot.RecoveredPacketCount = recoveredPacketCount;
            snapshot.CumulativePacketsLost = ClampCumulativePacketLoss(
                expectedPacketCount - receivedPacketCount);
            snapshot.FractionLost = fractionLost;
            snapshot.InterarrivalJitter = GetReportedInterarrivalJitter();
            snapshot.LastSenderReport = lastSenderReport;
            snapshot.DelaySinceLastSenderReport =
                GetDelaySinceLastSenderReport(currentTimeTicks);
            snapshot.TimedOut = timedOut;
            return snapshot;
        }
    }

    /// <summary>Resets all reception statistics and report interval state.</summary>
    public void ResetReceptionStatistics()
    {
        lock (syncRoot)
        {
            ResetSequenceIntervalState();
            duplicatePacketCount = 0;
            reorderedPacketCount = 0;
            discardedPacketCount = 0;
            latePacketCount = 0;
            repairedPacketCount = 0;
            recoveredPacketCount = 0;
            hasSenderReport = false;
            lastSenderReport = 0;
            senderReportArrivalTimeTicks = 0;
            timedOut = false;
            hasBuiltReceptionReport = false;
            lastReceptionReportTimeTicks = 0;
        }
    }

    internal void SetReceptionTimeoutState(bool timedOut)
    {
        lock (syncRoot)
        {
            this.timedOut = timedOut;
        }
    }

    internal void ValidateReceivedPacketInput(
        RtpPacket packet,
        RtpPacketContext context,
        RtpSequenceUpdate sequenceUpdate)
    {
        if (packet == null || context == null || sequenceUpdate == null)
        {
            throw new RtpConfigurationException(
                "The RTP packet, context, and sequence update are required.");
        }

        if (packet.Payload == null)
        {
            throw new RtpConfigurationException(
                "The received RTP payload array is required.");
        }


        if (context.ArrivalTimeTicks < 0)
        {
            throw new RtpConfigurationException(
                "The RTP packet arrival time cannot be negative.");
        }

        if (context.ClockRate <= 0)
        {
            throw new RtpConfigurationException(
                "The RTP packet clock rate must be positive.");
        }

        if (packet.SynchronizationSource != sourceIdentifier)
        {
            throw new RtpSessionException(
                "The received RTP packet belongs to a different source.");
        }

        if (packet.SequenceNumber != sequenceUpdate.SequenceNumber)
        {
            throw new RtpSessionException(
                "The RTP packet and sequence update do not identify the same packet.");
        }

        if (sequenceUpdate.HighestExtendedSequenceNumber <
            sequenceUpdate.BaseExtendedSequenceNumber)
        {
            throw new RtpSessionException(
                "The RTP sequence range is not valid for reception accounting.");
        }

        bool recordableStatus =
            sequenceUpdate.Status == RtpSequenceStatus.Accepted ||
            sequenceUpdate.Status == RtpSequenceStatus.Duplicate ||
            sequenceUpdate.Status == RtpSequenceStatus.Reordered ||
            sequenceUpdate.Status == RtpSequenceStatus.Restarted;
        if (!recordableStatus)
        {
            throw new RtpSessionException(
                "The RTP sequence outcome is not valid for reception accounting.");
        }

        if (sequenceUpdate.Status != RtpSequenceStatus.Duplicate &&
            !sequenceUpdate.Accepted)
        {
            throw new RtpSessionException(
                "The RTP sequence outcome was not accepted for reception accounting.");
        }
    }

    internal void ValidateCurrentTime(long currentTimeTicks)
    {
        if (currentTimeTicks < 0)
        {
            throw new RtpConfigurationException(
                "The current monotonic time cannot be negative.");
        }
    }

    internal void ValidateSenderReportTime(long currentTimeTicks)
    {
        if (hasSenderReport && currentTimeTicks < senderReportArrivalTimeTicks)
        {
            throw new RtpSessionException(
                "The report time cannot precede the last sender report.");
        }
    }

    internal void ValidateReceptionReportTime(long currentTimeTicks)
    {
        if (hasJitterSample && currentTimeTicks < previousArrivalTimeTicks)
        {
            throw new RtpSessionException(
                "The reception report cannot precede the latest RTP packet.");
        }

        if (hasBuiltReceptionReport &&
            currentTimeTicks < lastReceptionReportTimeTicks)
        {
            throw new RtpSessionException(
                "The reception-report time cannot move backward.");
        }
    }

    internal uint GetDelaySinceLastSenderReport(long currentTimeTicks)
    {
        if (!hasSenderReport)
        {
            return 0;
        }

        return RtpNtpTime.ElapsedTicksToCompactDelay(
            currentTimeTicks - senderReportArrivalTimeTicks);
    }

    internal long GetExpectedPacketCount()
    {
        if (!hasSequenceState)
        {
            return 0;
        }

        return (long)highestExtendedSequenceNumber -
            (long)baseExtendedSequenceNumber + 1L;
    }

    internal void ResetSequenceIntervalState()
    {
        hasSequenceState = false;
        baseExtendedSequenceNumber = 0;
        highestExtendedSequenceNumber = 0;
        receivedPacketCount = 0;
        receivedOctetCount = 0;
        expectedPacketCountPrior = 0;
        receivedPacketCountPrior = 0;
        fractionLost = 0;
        ResetInterarrivalJitter();
    }

    internal void ValidateJitterProgress(RtpPacketContext context)
    {
        if (!hasJitterSample)
        {
            return;
        }

        if (context.ArrivalTimeTicks < previousArrivalTimeTicks)
        {
            throw new RtpSessionException(
                "The RTP packet arrival time cannot move backward.");
        }

        if (context.ClockRate == activeClockRate &&
            context.MediaClockTimestamp < previousMediaClockTimestamp)
        {
            throw new RtpSessionException(
                "The RTP packet media-clock arrival time cannot move backward.");
        }
    }

    internal void UpdateInterarrivalJitter(
        uint rtpTimestamp,
        RtpPacketContext context)
    {
        if (!hasJitterSample || context.ClockRate != activeClockRate)
        {
            ResetInterarrivalJitter();
            hasJitterSample = true;
            previousRtpTimestamp = rtpTimestamp;
            previousArrivalTimeTicks = context.ArrivalTimeTicks;
            previousMediaClockTimestamp = context.MediaClockTimestamp;
            activeClockRate = context.ClockRate;
            return;
        }

        long arrivalDelta = CalculateNonnegativeDifference(
            context.MediaClockTimestamp,
            previousMediaClockTimestamp);
        long timestampDelta = CalculateRtpTimestampDifference(
            rtpTimestamp,
            previousRtpTimestamp);
        long transitVariation = CalculateAbsoluteDifference(
            arrivalDelta,
            timestampDelta);
        long decay = interarrivalJitterScaled / 16L;
        if (interarrivalJitterScaled % 16L >= 8L)
        {
            decay = decay + 1L;
        }

        long retainedJitter = interarrivalJitterScaled - decay;
        if (transitVariation > long.MaxValue - retainedJitter)
        {
            interarrivalJitterScaled = long.MaxValue;
        }
        else
        {
            interarrivalJitterScaled = retainedJitter + transitVariation;
        }

        previousRtpTimestamp = rtpTimestamp;
        previousArrivalTimeTicks = context.ArrivalTimeTicks;
        previousMediaClockTimestamp = context.MediaClockTimestamp;
    }

    internal void ResetInterarrivalJitter()
    {
        hasJitterSample = false;
        previousRtpTimestamp = 0;
        previousArrivalTimeTicks = 0;
        previousMediaClockTimestamp = 0;
        activeClockRate = 0;
        interarrivalJitterScaled = 0;
    }

    internal uint GetReportedInterarrivalJitter()
    {
        long jitter = interarrivalJitterScaled / 16L;
        if (jitter > uint.MaxValue)
        {
            return uint.MaxValue;
        }

        return (uint)jitter;
    }

    internal static long CalculateNonnegativeDifference(
        long currentValue,
        long previousValue)
    {
        if (previousValue < 0 &&
            currentValue > long.MaxValue + previousValue)
        {
            return long.MaxValue;
        }

        return currentValue - previousValue;
    }

    internal static long CalculateRtpTimestampDifference(
        uint currentTimestamp,
        uint previousTimestamp)
    {
        long difference = (long)currentTimestamp - (long)previousTimestamp;
        if (difference > 2147483647L)
        {
            difference = difference - 4294967296L;
        }
        else if (difference < -2147483648L)
        {
            difference = difference + 4294967296L;
        }

        return difference;
    }

    internal static long CalculateAbsoluteDifference(
        long nonnegativeValue,
        long signedValue)
    {
        if (signedValue < 0)
        {
            long magnitude = -signedValue;
            if (nonnegativeValue > long.MaxValue - magnitude)
            {
                return long.MaxValue;
            }

            return nonnegativeValue + magnitude;
        }

        if (nonnegativeValue >= signedValue)
        {
            return nonnegativeValue - signedValue;
        }

        return signedValue - nonnegativeValue;
    }

    internal static long AddSaturatedCount(long currentCount, int addition)
    {
        if (currentCount > long.MaxValue - addition)
        {
            return long.MaxValue;
        }

        return currentCount + addition;
    }

    internal static int ClampCumulativePacketLoss(long cumulativePacketLoss)
    {
        if (cumulativePacketLoss > RtcpProtocolConstants.CumulativeLossMaximum)
        {
            return RtcpProtocolConstants.CumulativeLossMaximum;
        }

        if (cumulativePacketLoss < RtcpProtocolConstants.CumulativeLossMinimum)
        {
            return RtcpProtocolConstants.CumulativeLossMinimum;
        }

        return (int)cumulativePacketLoss;
    }

    internal static byte CalculateFractionLost(
        long expectedInterval,
        long receivedInterval)
    {
        long lostInterval = expectedInterval - receivedInterval;
        if (expectedInterval <= 0 || lostInterval <= 0)
        {
            return 0;
        }

        long fraction = (lostInterval * 256L) / expectedInterval;
        if (fraction > RtcpProtocolConstants.FractionLostMaximum)
        {
            return (byte)RtcpProtocolConstants.FractionLostMaximum;
        }

        return (byte)fraction;
    }
}
