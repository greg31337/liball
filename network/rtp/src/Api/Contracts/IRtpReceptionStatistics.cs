namespace rtp;

/// <summary>
/// Defines RTP reception accounting and RTCP report-block generation.
/// </summary>
public interface IRtpReceptionStatistics
{
    /// <summary>Records one accepted RTP packet and its sequence outcome.</summary>
    /// <param name="packet">The accepted RTP packet.</param>
    /// <param name="context">The packet timing and repair context.</param>
    /// <param name="sequenceUpdate">The corresponding sequence update.</param>
    void RecordReceivedRtpPacket(
        RtpPacket packet,
        RtpPacketContext context,
        RtpSequenceUpdate sequenceUpdate);

    /// <summary>Records one RTP packet discarded before normal acceptance.</summary>
    /// <param name="context">The timing and repair context of the discarded packet.</param>
    void RecordDiscardedRtpPacket(RtpPacketContext context);

    /// <summary>Records timing from a received RTCP sender report.</summary>
    /// <param name="senderReport">The received sender report.</param>
    /// <param name="arrivalTimeTicks">Its monotonic arrival time.</param>
    void RecordReceivedSenderReport(RtcpSenderReport senderReport, long arrivalTimeTicks);

    /// <summary>Builds an RTCP reception report from current interval statistics.</summary>
    /// <param name="currentTimeTicks">The current monotonic time.</param>
    /// <returns>An independently owned reception report.</returns>
    RtcpReceptionReport BuildRtcpReceptionReport(long currentTimeTicks);

    /// <summary>Returns an independent reception-statistics snapshot.</summary>
    /// <param name="currentTimeTicks">The current monotonic time.</param>
    /// <returns>The current reception statistics.</returns>
    RtpReceptionStatisticsSnapshot GetReceptionStatisticsSnapshot(long currentTimeTicks);

    /// <summary>Resets all reception statistics and report interval state.</summary>
    void ResetReceptionStatistics();
}
