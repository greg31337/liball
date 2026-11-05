namespace rtp;

/// <summary>
/// Contains the result of transport-neutral RTP and RTCP datagram classification.
/// </summary>
public class RtpDatagramClassification
{
    /// <summary>The classified datagram kind.</summary>
    public RtpPacketKind Kind;

    /// <summary>The RTP payload type when the datagram is RTP.</summary>
    public byte PayloadType;

    /// <summary>The RTCP packet type when the datagram is recognized as RTCP.</summary>
    public RtcpPacketType ControlPacketType;

    /// <summary>A description of why a datagram could not be safely classified.</summary>
    public string? ErrorMessage;

    internal RtpDatagramClassification Copy()
    {
        RtpDatagramClassification copy = new RtpDatagramClassification();
        copy.Kind = Kind;
        copy.PayloadType = PayloadType;
        copy.ControlPacketType = ControlPacketType;
        copy.ErrorMessage = ErrorMessage;
        return copy;
    }
}
