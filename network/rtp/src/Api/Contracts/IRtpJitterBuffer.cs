namespace rtp;

/// <summary>
/// Defines bounded RTP reordering, release, and loss-finalization operations.
/// </summary>
public interface IRtpJitterBuffer
{
    /// <summary>Adds one validated packet to the reorder buffer.</summary>
    /// <param name="packet">The packet to retain or release.</param>
    /// <param name="context">The packet timing and repair context.</param>
    /// <param name="sequenceUpdate">The corresponding extended sequence update.</param>
    /// <returns>The buffer update produced by the packet.</returns>
    RtpJitterBufferUpdate AddRtpJitterBufferPacket(
        RtpPacket packet,
        RtpPacketContext context,
        RtpSequenceUpdate sequenceUpdate);

    /// <summary>Attempts to add one packet without throwing for capacity failure.</summary>
    /// <param name="packet">The packet to retain or release.</param>
    /// <param name="context">The packet timing and repair context.</param>
    /// <param name="sequenceUpdate">The corresponding extended sequence update.</param>
    /// <returns>The buffer update, including an overflow status when full.</returns>
    RtpJitterBufferUpdate TryAddRtpJitterBufferPacket(
        RtpPacket packet,
        RtpPacketContext context,
        RtpSequenceUpdate sequenceUpdate);

    /// <summary>Polls buffered gaps and packets at a monotonic time.</summary>
    /// <param name="currentTimeTicks">The current monotonic time.</param>
    /// <returns>The resulting releases, losses, and repair requests.</returns>
    RtpJitterBufferUpdate PollRtpJitterBuffer(long currentTimeTicks);

    /// <summary>Releases every currently contiguous buffered packet.</summary>
    /// <returns>The packets released without finalizing all gaps.</returns>
    RtpJitterBufferUpdate FlushRtpJitterBuffer();

    /// <summary>Finalizes gaps and releases every retained packet.</summary>
    /// <returns>The drained packets and finalized losses.</returns>
    RtpJitterBufferUpdate DrainRtpJitterBuffer();

    /// <summary>Clears all buffered packets, gaps, and repair state.</summary>
    /// <returns>The reset buffer update.</returns>
    RtpJitterBufferUpdate ResetRtpJitterBuffer();
}
