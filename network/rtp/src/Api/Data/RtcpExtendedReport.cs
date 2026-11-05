namespace rtp;

/// <summary>
/// Contains an RFC 3611 RTCP extended report body.
/// </summary>
public class RtcpExtendedReport
{
    /// <summary>The synchronization source sending the extended report.</summary>
    public uint SenderSynchronizationSource;

    /// <summary>The ordered extended-report blocks.</summary>
    public RtcpExtendedReportBlock[] Blocks = [];

    internal RtcpExtendedReport Copy()
    {
        RtcpExtendedReport copy = new RtcpExtendedReport();
        copy.SenderSynchronizationSource = SenderSynchronizationSource;
        copy.Blocks = new RtcpExtendedReportBlock[Blocks.Length];
        int index = 0;
        while (index < Blocks.Length)
        {
            copy.Blocks[index] = Blocks[index].Copy();
            index = index + 1;
        }

        return copy;
    }
}
