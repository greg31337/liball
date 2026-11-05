namespace rtp;

/// <summary>
/// Contains one RFC 8888 congestion-control report for an RTP stream.
/// </summary>
public class RtcpCongestionControlReport
{
    /// <summary>The SSRC of the reported RTP stream.</summary>
    public uint SourceIdentifier;

    /// <summary>The first RTP sequence number represented by the report.</summary>
    public ushort BeginSequenceNumber;

    /// <summary>The number of packet metric blocks represented by the report.</summary>
    public int ReportCount;

    /// <summary>Whether each represented packet was received.</summary>
    public bool[] Received = [];

    /// <summary>The echoed two-bit ECN mark for each represented packet.</summary>
    public byte[] EcnMarks = [];

    /// <summary>The thirteen-bit arrival-time offset for each represented packet.</summary>
    public ushort[] ArrivalTimeOffsets = [];

    internal RtcpCongestionControlReport Copy()
    {
        RtcpCongestionControlReport copy = new RtcpCongestionControlReport();
        copy.SourceIdentifier = SourceIdentifier;
        copy.BeginSequenceNumber = BeginSequenceNumber;
        copy.ReportCount = ReportCount;
        copy.Received = (bool[])Received.Clone();
        copy.EcnMarks = (byte[])EcnMarks.Clone();
        copy.ArrivalTimeOffsets = (ushort[])ArrivalTimeOffsets.Clone();
        return copy;
    }
}
