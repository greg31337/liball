using System;

namespace rtp;

/// <summary>
/// Defines construction of regular, feedback, and goodbye RTCP compounds.
/// </summary>
public interface IRtcpReportBuilder
{
    /// <summary>Builds regular sender or receiver reports for current session state.</summary>
    /// <param name="snapshot">The independent session snapshot to report.</param>
    /// <param name="ntpTimestamp">The current 64-bit NTP timestamp.</param>
    /// <returns>The compound RTCP packet to transmit.</returns>
    RtcpCompoundPacket BuildRegularRtcpReport(
        RtpSessionSnapshot snapshot,
        ulong ntpTimestamp);

    /// <summary>Builds a legal RTCP compound carrying one feedback message.</summary>
    /// <param name="snapshot">The independent session snapshot to report.</param>
    /// <param name="feedback">The feedback packet to include.</param>
    /// <param name="timingMode">The negotiated feedback emission mode.</param>
    /// <returns>The feedback compound packet to transmit.</returns>
    RtcpCompoundPacket BuildFeedbackRtcpReport(
        RtpSessionSnapshot snapshot,
        RtcpFeedbackPacket feedback,
        RtcpFeedbackTimingMode timingMode);

    /// <summary>Builds a legal RTCP compound carrying a goodbye packet.</summary>
    /// <param name="snapshot">The independent session snapshot to report.</param>
    /// <param name="reason">The optional UTF-8 goodbye reason bytes.</param>
    /// <returns>The goodbye compound packet to transmit.</returns>
    RtcpCompoundPacket BuildGoodbyeRtcpReport(
        RtpSessionSnapshot snapshot,
        ReadOnlySpan<byte> reason);
}
