namespace rtp;

/// <summary>
/// Defines bounded storage and lookup of sent RTP packets for retransmission.
/// </summary>
public interface IRtpPacketHistory
{
    /// <summary>Stores an independent copy of one sent RTP packet.</summary>
    /// <param name="packet">The packet to retain.</param>
    /// <param name="context">The packet send-time context.</param>
    void StoreRtpHistoryPacket(RtpPacket packet, RtpPacketContext context);

    /// <summary>Attempts to store a packet without throwing for capacity failure.</summary>
    /// <param name="packet">The packet to retain.</param>
    /// <param name="context">The packet send-time context.</param>
    /// <returns><c>true</c> when the packet was retained.</returns>
    bool TryStoreRtpHistoryPacket(RtpPacket packet, RtpPacketContext context);

    /// <summary>Finds an exact sent packet by SSRC and sequence number.</summary>
    /// <param name="sourceIdentifier">The packet synchronization source.</param>
    /// <param name="sequenceNumber">The packet sequence number.</param>
    /// <returns>An owned packet copy, or <c>null</c> when absent.</returns>
    RtpPacket? FindRtpHistoryPacket(
        uint sourceIdentifier,
        ushort sequenceNumber);

    /// <summary>Removes packets older than the configured history age.</summary>
    /// <param name="currentTimeTicks">The current monotonic time.</param>
    /// <returns>The number of packets removed.</returns>
    int RemoveExpiredRtpHistoryPackets(long currentTimeTicks);

    /// <summary>Clears every retained history packet.</summary>
    void ClearRtpPacketHistory();
}
