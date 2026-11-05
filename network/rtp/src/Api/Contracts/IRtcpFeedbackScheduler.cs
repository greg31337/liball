namespace rtp;

/// <summary>
/// Defines deterministic AVPF feedback queuing, suppression, and polling.
/// </summary>
public interface IRtcpFeedbackScheduler
{
    /// <summary>Queues one independently retained feedback request.</summary>
    /// <param name="request">The feedback request to schedule.</param>
    void QueueRtcpFeedback(RtpFeedbackRequest request);

    /// <summary>Observes received feedback that can suppress an equivalent request.</summary>
    /// <param name="feedback">The received feedback packet.</param>
    /// <param name="arrivalTimeTicks">The monotonic feedback arrival time.</param>
    void ObserveRtcpFeedback(
        RtcpFeedbackPacket feedback,
        long arrivalTimeTicks);

    /// <summary>Polls pending feedback against current session state.</summary>
    /// <param name="snapshot">The current independent session snapshot.</param>
    /// <param name="currentTimeTicks">The current monotonic time.</param>
    /// <returns>The feedback scheduling decision.</returns>
    RtpFeedbackDecision PollRtcpFeedback(
        RtpSessionSnapshot snapshot,
        long currentTimeTicks);

    /// <summary>Resets queued feedback and bandwidth-accounting state.</summary>
    /// <param name="currentTimeTicks">The reset monotonic time.</param>
    void ResetRtcpFeedbackScheduler(long currentTimeTicks);
}
