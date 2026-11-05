using System.Collections.Generic;

namespace rtp;

/// <summary>
/// Tracks RTP transmission state and reception reports describing one local source.
/// </summary>
public class RtpTransmissionStatistics : IRtpTransmissionStatistics
{
    internal const int SenderReportHistoryCapacity = 64;

    internal object syncRoot = new object();
    internal uint sourceIdentifier;
    internal IRtpClock clock;
    internal List<uint> senderReportTimestamps = new List<uint>();
    internal List<long> senderReportSendTimes = new List<long>();
    internal List<RtpRemoteReportSnapshot> remoteReports =
        new List<RtpRemoteReportSnapshot>();
    internal long sentPacketCount;
    internal long sentOctetCount;
    internal uint lastRtpTimestamp;
    internal long lastSendTimeTicks;
    internal bool senderReportEligible;
    internal bool activeSender;
    internal bool timedOut;

    /// <summary>Initializes transmission statistics for one local RTP source.</summary>
    /// <param name="sourceIdentifier">The local RTP synchronization source.</param>
    /// <param name="clock">The monotonic clock used when sender reports are built.</param>
    public RtpTransmissionStatistics(uint sourceIdentifier, IRtpClock clock)
    {
        if (clock == null)
        {
            throw new RtpConfigurationException(
                "A clock is required for RTP transmission statistics.");
        }

        this.sourceIdentifier = sourceIdentifier;
        this.clock = clock;
    }

    /// <summary>Records one RTP packet accepted for transport.</summary>
    /// <param name="packet">The transmitted RTP packet.</param>
    /// <param name="context">The packet send-time and media-clock context.</param>
    public void RecordSentRtpPacket(RtpPacket packet, RtpPacketContext context)
    {
        if (packet == null || context == null)
        {
            throw new RtpConfigurationException(
                "The sent RTP packet and context are required.");
        }

        if (packet.SynchronizationSource != sourceIdentifier)
        {
            throw new RtpSessionException(
                "The sent RTP packet belongs to a different local source.");
        }

        if (packet.Payload == null)
        {
            throw new RtpConfigurationException(
                "The sent RTP payload array is required.");
        }

        if (context.ArrivalTimeTicks < 0)
        {
            throw new RtpConfigurationException(
                "The RTP packet send time cannot be negative.");
        }

        if (context.ClockRate <= 0)
        {
            throw new RtpConfigurationException(
                "The sent RTP packet clock rate must be positive.");
        }

        lock (syncRoot)
        {
            if (senderReportEligible &&
                context.ArrivalTimeTicks < lastSendTimeTicks)
            {
                throw new RtpSessionException(
                    "The RTP packet send time cannot move backward.");
            }

            sentPacketCount = AddSaturatedTransmissionCount(
                sentPacketCount,
                1);
            sentOctetCount = AddSaturatedTransmissionCount(
                sentOctetCount,
                packet.Payload.Length);
            lastRtpTimestamp = packet.Timestamp;
            lastSendTimeTicks = context.ArrivalTimeTicks;
            senderReportEligible = true;
            activeSender = true;
            timedOut = false;
        }
    }

    /// <summary>Records a remote reception report describing the local source.</summary>
    /// <param name="reporterSourceIdentifier">The remote reporter SSRC.</param>
    /// <param name="report">The report block describing the local source.</param>
    /// <param name="arrivalTimeTicks">The monotonic report arrival time.</param>
    public void RecordRemoteReceptionReport(
        uint reporterSourceIdentifier,
        RtcpReceptionReport report,
        long arrivalTimeTicks)
    {
        if (report == null)
        {
            throw new RtpConfigurationException(
                "A remote RTCP reception report is required.");
        }

        if (arrivalTimeTicks < 0)
        {
            throw new RtpConfigurationException(
                "The remote report arrival time cannot be negative.");
        }

        if (report.SourceIdentifier != sourceIdentifier)
        {
            throw new RtpSessionException(
                "The remote reception report describes a different local source.");
        }

        lock (syncRoot)
        {
            int reportIndex = FindRemoteReportIndex(reporterSourceIdentifier);
            if (reportIndex >= 0 &&
                arrivalTimeTicks < remoteReports[reportIndex].ArrivalTimeTicks)
            {
                throw new RtpSessionException(
                    "The remote report arrival time cannot move backward.");
            }

            RtpRemoteReportSnapshot snapshot = CreateRemoteReportSnapshot(
                reporterSourceIdentifier,
                report,
                arrivalTimeTicks);
            if (reportIndex < 0)
            {
                remoteReports.Add(snapshot);
            }
            else
            {
                remoteReports[reportIndex] = snapshot;
            }
        }
    }

    /// <summary>Builds sender information for an RTCP sender report.</summary>
    /// <param name="ntpTimestamp">The current 64-bit NTP timestamp.</param>
    /// <returns>An independently owned sender-information model.</returns>
    public RtcpSenderInformation BuildRtcpSenderInformation(ulong ntpTimestamp)
    {
        long sendTimeTicks = clock.GetMonotonicTimeTicks();
        if (sendTimeTicks < 0)
        {
            throw new RtpConfigurationException(
                "The sender-report monotonic time cannot be negative.");
        }

        lock (syncRoot)
        {
            if (!senderReportEligible)
            {
                throw new RtpSessionException(
                    "Sender information requires at least one sent RTP packet.");
            }

            if (sendTimeTicks < lastSendTimeTicks)
            {
                throw new RtpSessionException(
                    "Sender information cannot precede the last sent RTP packet.");
            }

            if (senderReportSendTimes.Count > 0 &&
                sendTimeTicks < senderReportSendTimes[
                    senderReportSendTimes.Count - 1])
            {
                throw new RtpSessionException(
                    "The sender-report monotonic time cannot move backward.");
            }

            uint compactTimestamp = RtpNtpTime.ToCompactNtp(ntpTimestamp);
            if (compactTimestamp != 0)
            {
                RememberSenderReport(compactTimestamp, sendTimeTicks);
            }

            RtcpSenderInformation information = new RtcpSenderInformation();
            information.NtpTimestamp = ntpTimestamp;
            information.RtpTimestamp = lastRtpTimestamp;
            information.PacketCount = GetRfcSenderCount(sentPacketCount);
            information.OctetCount = GetRfcSenderCount(sentOctetCount);
            activeSender = false;
            return information;
        }
    }

    /// <summary>Returns an independent transmission-statistics snapshot.</summary>
    /// <returns>The current transmission statistics.</returns>
    public RtpTransmissionStatisticsSnapshot GetTransmissionStatisticsSnapshot()
    {
        lock (syncRoot)
        {
            RtpTransmissionStatisticsSnapshot snapshot =
                new RtpTransmissionStatisticsSnapshot();
            snapshot.SourceIdentifier = sourceIdentifier;
            snapshot.SentPacketCount = sentPacketCount;
            snapshot.SentOctetCount = sentOctetCount;
            snapshot.LastRtpTimestamp = lastRtpTimestamp;
            snapshot.LastSendTimeTicks = lastSendTimeTicks;
            snapshot.IsActiveSender = activeSender;
            snapshot.SenderReportEligible = senderReportEligible;
            snapshot.TimedOut = timedOut;
            return snapshot;
        }
    }

    /// <summary>Returns independent snapshots of reports received from remote sources.</summary>
    /// <returns>The current remote report snapshots.</returns>
    public RtpRemoteReportSnapshot[] GetRemoteReportSnapshots()
    {
        lock (syncRoot)
        {
            RtpRemoteReportSnapshot[] snapshots =
                new RtpRemoteReportSnapshot[remoteReports.Count];
            int index = 0;
            while (index < remoteReports.Count)
            {
                snapshots[index] = remoteReports[index].Copy();
                index = index + 1;
            }

            return snapshots;
        }
    }

    /// <summary>Resets all transmission statistics and remote report state.</summary>
    public void ResetTransmissionStatistics()
    {
        lock (syncRoot)
        {
            senderReportTimestamps.Clear();
            senderReportSendTimes.Clear();
            remoteReports.Clear();
            sentPacketCount = 0;
            sentOctetCount = 0;
            lastRtpTimestamp = 0;
            lastSendTimeTicks = 0;
            senderReportEligible = false;
            activeSender = false;
            timedOut = false;
        }
    }

    internal void SetTransmissionTimeoutState(bool timedOut)
    {
        lock (syncRoot)
        {
            this.timedOut = timedOut;
        }
    }

    internal int FindRemoteReportIndex(uint reporterSourceIdentifier)
    {
        int index = 0;
        while (index < remoteReports.Count)
        {
            if (remoteReports[index].ReporterSourceIdentifier ==
                reporterSourceIdentifier)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal RtpRemoteReportSnapshot CreateRemoteReportSnapshot(
        uint reporterSourceIdentifier,
        RtcpReceptionReport report,
        long arrivalTimeTicks)
    {
        RtpRemoteReportSnapshot snapshot = new RtpRemoteReportSnapshot();
        snapshot.ReporterSourceIdentifier = reporterSourceIdentifier;
        snapshot.ReportedSourceIdentifier = report.SourceIdentifier;
        snapshot.FractionLost = report.FractionLost;
        snapshot.CumulativePacketsLost = report.CumulativePacketsLost;
        snapshot.ExtendedHighestSequenceNumber =
            report.ExtendedHighestSequenceNumber;
        snapshot.InterarrivalJitter = report.InterarrivalJitter;
        snapshot.LastSenderReport = report.LastSenderReport;
        snapshot.DelaySinceLastSenderReport = report.DelaySinceLastSenderReport;
        snapshot.ArrivalTimeTicks = arrivalTimeTicks;
        SetRoundTripTime(snapshot, report, arrivalTimeTicks);
        return snapshot;
    }

    internal void SetRoundTripTime(
        RtpRemoteReportSnapshot snapshot,
        RtcpReceptionReport report,
        long arrivalTimeTicks)
    {
        if (report.LastSenderReport == 0)
        {
            return;
        }

        int historyIndex = FindSenderReportHistoryIndex(
            report.LastSenderReport,
            arrivalTimeTicks);
        if (historyIndex < 0)
        {
            return;
        }

        long totalElapsedTicks = arrivalTimeTicks -
            senderReportSendTimes[historyIndex];
        long remoteDelayTicks = RtpNtpTime.CompactDelayToElapsedTicks(
            report.DelaySinceLastSenderReport);
        if (remoteDelayTicks > totalElapsedTicks)
        {
            return;
        }

        snapshot.HasRoundTripTime = true;
        snapshot.RoundTripTimeTicks = totalElapsedTicks - remoteDelayTicks;
    }

    internal int FindSenderReportHistoryIndex(
        uint compactTimestamp,
        long arrivalTimeTicks)
    {
        int index = senderReportTimestamps.Count - 1;
        while (index >= 0)
        {
            if (senderReportTimestamps[index] == compactTimestamp &&
                senderReportSendTimes[index] <= arrivalTimeTicks)
            {
                return index;
            }

            index = index - 1;
        }

        return -1;
    }

    internal void RememberSenderReport(
        uint compactTimestamp,
        long sendTimeTicks)
    {
        if (senderReportTimestamps.Count == SenderReportHistoryCapacity)
        {
            senderReportTimestamps.RemoveAt(0);
            senderReportSendTimes.RemoveAt(0);
        }

        senderReportTimestamps.Add(compactTimestamp);
        senderReportSendTimes.Add(sendTimeTicks);
    }

    internal static long AddSaturatedTransmissionCount(
        long currentCount,
        int addition)
    {
        if (currentCount > long.MaxValue - addition)
        {
            return long.MaxValue;
        }

        return currentCount + addition;
    }

    internal static uint GetRfcSenderCount(long count)
    {
        return (uint)(count % 4294967296L);
    }
}
