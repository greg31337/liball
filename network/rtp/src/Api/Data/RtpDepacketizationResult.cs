namespace rtp;

/// <summary>
/// Contains the owned result of assembling media payload from RTP packets.
/// </summary>
public class RtpDepacketizationResult
{
    /// <summary>Whether depacketization completed successfully.</summary>
    public bool Success;

    /// <summary>The number of RTP packets consumed.</summary>
    public int ConsumedPacketCount;

    /// <summary>The assembled application media payload.</summary>
    public byte[] Payload = [];

    /// <summary>The RTP timestamp shared by the assembled media unit.</summary>
    public uint Timestamp;

    /// <summary>Whether the completed media unit ended with an RTP marker.</summary>
    public bool Marker;

    /// <summary>A description of a recoverable depacketization failure.</summary>
    public string? ErrorMessage;

    internal RtpDepacketizationResult Copy()
    {
        RtpDepacketizationResult copy = new RtpDepacketizationResult();
        copy.Success = Success;
        copy.ConsumedPacketCount = ConsumedPacketCount;
        copy.Payload = (byte[])Payload.Clone();
        copy.Timestamp = Timestamp;
        copy.Marker = Marker;
        copy.ErrorMessage = ErrorMessage;
        return copy;
    }
}
