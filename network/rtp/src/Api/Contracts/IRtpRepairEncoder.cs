namespace rtp;

/// <summary>
/// Defines application-provided RTP retransmission, redundancy, or FEC encoding.
/// </summary>
public interface IRtpRepairEncoder
{
    /// <summary>Produces repair data or packets from retained source packets.</summary>
    /// <param name="request">The requested repair operation.</param>
    /// <param name="sourcePackets">The source packets available for repair.</param>
    /// <returns>The independently owned repair result.</returns>
    RtpRepairResult EncodeRtpRepair(
        RtpRepairRequest request,
        RtpPacket[] sourcePackets);
}
