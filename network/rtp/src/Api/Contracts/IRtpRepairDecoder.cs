using System;

namespace rtp;

/// <summary>
/// Defines application-provided RTP retransmission, redundancy, or FEC decoding.
/// </summary>
public interface IRtpRepairDecoder
{
    /// <summary>Attempts to recover media packets from application repair bytes.</summary>
    /// <param name="request">The repair operation being satisfied.</param>
    /// <param name="repairData">The repair bytes to decode.</param>
    /// <returns>The independently owned recovered packets and remaining losses.</returns>
    RtpRepairResult DecodeRtpRepair(
        RtpRepairRequest request,
        ReadOnlySpan<byte> repairData);
}
