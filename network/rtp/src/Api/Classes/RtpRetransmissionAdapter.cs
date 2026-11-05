namespace rtp;

/// <summary>Provides RFC 4588 retransmission wrapping and restoration.</summary>
public class RtpRetransmissionAdapter : IRtpRetransmissionAdapter
{
    /// <summary>Initializes an RTP retransmission adapter.</summary>
    public RtpRetransmissionAdapter()
    {
    }

    /// <summary>Wraps one original packet using negotiated RTX identifiers.</summary>
    /// <param name="originalPacket">The original media packet.</param>
    /// <param name="retransmissionSourceIdentifier">The RTX SSRC.</param>
    /// <param name="retransmissionPayloadType">The RTX payload type.</param>
    /// <param name="retransmissionSequenceNumber">The separate RTX sequence number.</param>
    /// <returns>The independently owned RTX packet.</returns>
    public RtpPacket WrapRtpRetransmission(
        RtpPacket originalPacket,
        uint retransmissionSourceIdentifier,
        byte retransmissionPayloadType,
        ushort retransmissionSequenceNumber)
    {
        return RtpRetransmissionPacketAdapter.CreateRetransmissionPacket(
            originalPacket,
            retransmissionSourceIdentifier,
            retransmissionPayloadType,
            retransmissionSequenceNumber);
    }

    /// <summary>Restores one RTX packet to its original media identifiers.</summary>
    /// <param name="retransmissionPacket">The received RTX packet.</param>
    /// <param name="originalSourceIdentifier">The original media SSRC.</param>
    /// <param name="originalPayloadType">The original media payload type.</param>
    /// <returns>The independently owned restored media packet.</returns>
    public RtpPacket UnwrapRtpRetransmission(
        RtpPacket retransmissionPacket,
        uint originalSourceIdentifier,
        byte originalPayloadType)
    {
        return RtpRetransmissionPacketAdapter.RestoreOriginalPacket(
            retransmissionPacket,
            originalSourceIdentifier,
            originalPayloadType);
    }
}
