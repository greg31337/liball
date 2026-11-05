using System;

namespace rtp;

/// <summary>
/// Defines application media packetization without owning RTP header state.
/// </summary>
public interface IRtpPayloadPacketizer
{
    /// <summary>Splits media data into payload fragments within a size limit.</summary>
    /// <param name="mediaData">The application media bytes to packetize.</param>
    /// <param name="maximumPayloadSize">The maximum payload bytes per RTP packet.</param>
    /// <returns>The owned payload fragments and marker decisions.</returns>
    RtpPacketizationResult PacketizeRtpPayload(
        ReadOnlySpan<byte> mediaData,
        int maximumPayloadSize);
}
