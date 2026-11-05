namespace rtp;

/// <summary>
/// Defines RTP timestamp extension and clock-rate transition operations.
/// </summary>
public interface IRtpTimestampTracker
{
    /// <summary>Processes one RTP timestamp and isolates changes in media clock rate.</summary>
    /// <param name="timestamp">The 32-bit RTP timestamp.</param>
    /// <param name="clockRate">The payload clock rate in ticks per second.</param>
    /// <returns>The resulting extended timestamp update.</returns>
    RtpTimestampUpdate ProcessRtpTimestamp(uint timestamp, int clockRate);

    /// <summary>Creates a clock correlation from one RTCP sender report.</summary>
    /// <param name="senderReport">The sender report containing corresponding clocks.</param>
    /// <param name="clockRate">The RTP timestamp clock rate for the report.</param>
    /// <param name="eraReferenceUtcTicks">UTC ticks selecting the nearest NTP era.</param>
    /// <returns>The accepted or rejected sender-report correlation.</returns>
    RtpClockCorrelation CorrelateRtcpSenderReport(
        RtcpSenderReport senderReport,
        int clockRate,
        long eraReferenceUtcTicks);

    /// <summary>Maps a timestamp update onto the correlated presentation timeline.</summary>
    /// <param name="timestampUpdate">An update from the matching timestamp domain.</param>
    /// <returns>The timestamp's presentation-time correlation.</returns>
    RtpClockCorrelation ConvertRtpTimestampToPresentationTime(
        RtpTimestampUpdate timestampUpdate);

    /// <summary>Returns an independent snapshot of current timestamp state.</summary>
    /// <returns>The current timestamp state.</returns>
    RtpTimestampUpdate GetRtpTimestampState();

    /// <summary>Returns an independent snapshot of the current clock correlation.</summary>
    /// <returns>The current sender-report correlation.</returns>
    RtpClockCorrelation GetRtpClockCorrelation();

    /// <summary>Resets timestamp tracking around a supplied value and clock rate.</summary>
    /// <param name="timestamp">The new initial RTP timestamp.</param>
    /// <param name="clockRate">The new timestamp clock rate.</param>
    /// <returns>The reset timestamp state.</returns>
    RtpTimestampUpdate ResetRtpTimestampTracker(uint timestamp, int clockRate);
}
