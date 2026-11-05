namespace rtp;

/// <summary>
/// Contains an exception-less RTCP codec outcome and owned result data.
/// </summary>
public class RtcpCodecResult
{
    /// <summary>Whether the codec operation completed successfully.</summary>
    public bool Success;

    /// <summary>The number of input bytes consumed.</summary>
    public int ConsumedCount;

    /// <summary>The number of output bytes written.</summary>
    public int WrittenCount;

    /// <summary>The decoded single RTCP packet when applicable.</summary>
    public RtcpPacket? Packet;

    /// <summary>The decoded compound RTCP packet when applicable.</summary>
    public RtcpCompoundPacket? CompoundPacket;

    /// <summary>The owned encoded bytes produced by an array convenience operation.</summary>
    public byte[] Data = [];

    /// <summary>A description of a recoverable codec failure.</summary>
    public string? ErrorMessage;

    internal RtcpCodecResult Copy()
    {
        RtcpCodecResult copy = new RtcpCodecResult();
        copy.Success = Success;
        copy.ConsumedCount = ConsumedCount;
        copy.WrittenCount = WrittenCount;
        if (Packet != null)
        {
            copy.Packet = Packet.Copy();
        }

        if (CompoundPacket != null)
        {
            copy.CompoundPacket = CompoundPacket.Copy();
        }

        copy.Data = (byte[])Data.Clone();
        copy.ErrorMessage = ErrorMessage;
        return copy;
    }
}
