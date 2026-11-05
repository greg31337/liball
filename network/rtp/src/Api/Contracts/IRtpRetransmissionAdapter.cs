namespace rtp;

/// <summary>
/// Defines RFC 4588 retransmission wrapping and restoration operations.
/// </summary>
public interface IRtpRetransmissionAdapter
{
    /// <summary>Wraps one original packet using negotiated RTX identifiers.</summary>
    /// <param name="originalPacket">The original media packet.</param>
    /// <param name="retransmissionSourceIdentifier">The RTX SSRC.</param>
    /// <param name="retransmissionPayloadType">The RTX payload type.</param>
    /// <param name="retransmissionSequenceNumber">The separate RTX sequence number.</param>
    /// <returns>The independently owned RTX packet.</returns>
    RtpPacket WrapRtpRetransmission(
        RtpPacket originalPacket,
        uint retransmissionSourceIdentifier,
        byte retransmissionPayloadType,
        ushort retransmissionSequenceNumber);

    /// <summary>Restores one RTX packet to its original media identifiers.</summary>
    /// <param name="retransmissionPacket">The received RTX packet.</param>
    /// <param name="originalSourceIdentifier">The original media SSRC.</param>
    /// <param name="originalPayloadType">The original media payload type.</param>
    /// <returns>The independently owned restored media packet.</returns>
    RtpPacket UnwrapRtpRetransmission(
        RtpPacket retransmissionPacket,
        uint originalSourceIdentifier,
        byte originalPayloadType);
}
