namespace rtp;

/// <summary>
/// Defines application media assembly from ordered RTP packet payloads.
/// </summary>
public interface IRtpPayloadDepacketizer
{
    /// <summary>Assembles application media from one ordered RTP packet group.</summary>
    /// <param name="packets">The independently owned packets to consume.</param>
    /// <returns>The assembled media payload and consumed packet count.</returns>
    RtpDepacketizationResult DepacketizeRtpPayload(RtpPacket[] packets);
}
