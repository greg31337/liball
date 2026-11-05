namespace rtp;

/// <summary>
/// Contains an independent snapshot of RTP session state.
/// </summary>
public class RtpSessionSnapshot
{
    /// <summary>The configuration active when the snapshot was produced.</summary>
    public RtpSessionConfiguration Configuration = new RtpSessionConfiguration();

    /// <summary>The known local and remote sources.</summary>
    public RtpSourceSnapshot[] Sources = [];

    /// <summary>The synchronization source identifiers owned by the local session.</summary>
    public uint[] LocalSourceIdentifiers = [];

    /// <summary>The current RTCP membership count.</summary>
    public int MemberCount;

    /// <summary>The current active RTP sender count.</summary>
    public int SenderCount;

    /// <summary>The current average compound RTCP packet size in bytes.</summary>
    public int AverageRtcpPacketSize;

    /// <summary>The monotonic time at which the snapshot was captured.</summary>
    public long SnapshotTimeTicks;

    /// <summary>The scheduled monotonic time of the next regular RTCP transmission.</summary>
    public long NextRtcpSendTimeTicks;

    internal RtpSessionSnapshot Copy()
    {
        RtpSessionSnapshot copy = new RtpSessionSnapshot();
        copy.Configuration = Configuration.Copy();
        copy.Sources = new RtpSourceSnapshot[Sources.Length];
        int sourceIndex = 0;
        while (sourceIndex < Sources.Length)
        {
            copy.Sources[sourceIndex] = Sources[sourceIndex].Copy();
            sourceIndex = sourceIndex + 1;
        }

        copy.LocalSourceIdentifiers = (uint[])LocalSourceIdentifiers.Clone();
        copy.MemberCount = MemberCount;
        copy.SenderCount = SenderCount;
        copy.AverageRtcpPacketSize = AverageRtcpPacketSize;
        copy.SnapshotTimeTicks = SnapshotTimeTicks;
        copy.NextRtcpSendTimeTicks = NextRtcpSendTimeTicks;
        return copy;
    }
}
