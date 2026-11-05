namespace rtp;

/// <summary>
/// Defines deterministic RFC 3550 RTCP interval and reconsideration operations.
/// </summary>
public interface IRtcpScheduler
{
    /// <summary>Initializes regular RTCP scheduling at a monotonic time.</summary>
    /// <param name="configuration">The validated scheduler configuration.</param>
    /// <param name="currentTimeTicks">The current monotonic time.</param>
    void InitializeRtcpSchedule(
        RtcpSchedulerConfiguration configuration,
        long currentTimeTicks);

    /// <summary>Records an observed compound RTCP size for interval averaging.</summary>
    /// <param name="packetSize">The compound packet size in bytes.</param>
    void RecordRtcpPacketSize(int packetSize);

    /// <summary>Applies forward or reverse reconsideration after membership changes.</summary>
    /// <param name="memberCount">The current session member count.</param>
    /// <param name="senderCount">The current active sender count.</param>
    /// <param name="currentTimeTicks">The current monotonic time.</param>
    void ReconsiderRtcpSchedule(
        int memberCount,
        int senderCount,
        long currentTimeTicks);

    /// <summary>Polls the regular RTCP scheduler.</summary>
    /// <param name="snapshot">The current independent session snapshot.</param>
    /// <param name="currentTimeTicks">The current monotonic time.</param>
    /// <returns>The current scheduling decision.</returns>
    RtcpScheduleDecision PollRtcpSchedule(
        RtpSessionSnapshot snapshot,
        long currentTimeTicks);

    /// <summary>Schedules a goodbye transmission using RTCP backoff rules.</summary>
    /// <param name="snapshot">The current independent session snapshot.</param>
    /// <param name="currentTimeTicks">The current monotonic time.</param>
    /// <returns>The goodbye scheduling decision.</returns>
    RtcpScheduleDecision ScheduleRtcpGoodbye(
        RtpSessionSnapshot snapshot,
        long currentTimeTicks);

    /// <summary>Resets scheduler interval and averaging state.</summary>
    /// <param name="currentTimeTicks">The reset monotonic time.</param>
    void ResetRtcpScheduler(long currentTimeTicks);
}
