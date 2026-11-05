namespace rtp;

/// <summary>
/// Defines RTP transmission accounting and sender-report timing operations.
/// </summary>
public interface IRtpTransmissionStatistics
{
    /// <summary>Records one RTP packet accepted for transport.</summary>
    /// <param name="packet">The transmitted RTP packet.</param>
    /// <param name="context">The packet send-time and media-clock context.</param>
    void RecordSentRtpPacket(RtpPacket packet, RtpPacketContext context);

    /// <summary>Records a remote reception report describing the local source.</summary>
    /// <param name="reporterSourceIdentifier">The remote reporter SSRC.</param>
    /// <param name="report">The report block describing the local source.</param>
    /// <param name="arrivalTimeTicks">The monotonic report arrival time.</param>
    void RecordRemoteReceptionReport(
        uint reporterSourceIdentifier,
        RtcpReceptionReport report,
        long arrivalTimeTicks);

    /// <summary>Builds sender information for an RTCP sender report.</summary>
    /// <param name="ntpTimestamp">The current 64-bit NTP timestamp.</param>
    /// <returns>An independently owned sender-information model.</returns>
    RtcpSenderInformation BuildRtcpSenderInformation(ulong ntpTimestamp);

    /// <summary>Returns an independent transmission-statistics snapshot.</summary>
    /// <returns>The current transmission statistics.</returns>
    RtpTransmissionStatisticsSnapshot GetTransmissionStatisticsSnapshot();

    /// <summary>Returns independent snapshots of reports received from remote sources.</summary>
    /// <returns>The current remote report snapshots.</returns>
    RtpRemoteReportSnapshot[] GetRemoteReportSnapshots();

    /// <summary>Resets all transmission statistics and remote report state.</summary>
    void ResetTransmissionStatistics();
}
