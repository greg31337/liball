namespace rtp;

/// <summary>
/// Contains one RFC 5104 full intra request entry.
/// </summary>
public class RtcpFullIntraRequestEntry
{
    /// <summary>The SSRC of the requested media sender.</summary>
    public uint SourceIdentifier;

    /// <summary>The command sequence number.</summary>
    public byte SequenceNumber;

    internal RtcpFullIntraRequestEntry Copy()
    {
        RtcpFullIntraRequestEntry copy = new RtcpFullIntraRequestEntry();
        copy.SourceIdentifier = SourceIdentifier;
        copy.SequenceNumber = SequenceNumber;
        return copy;
    }
}
