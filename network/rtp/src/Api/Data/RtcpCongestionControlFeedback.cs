namespace rtp;

/// <summary>
/// Contains an RFC 8888 congestion-control feedback message.
/// </summary>
public class RtcpCongestionControlFeedback
{
    /// <summary>The per-stream congestion-control reports.</summary>
    public RtcpCongestionControlReport[] Reports = [];

    /// <summary>The compact-NTP report timestamp.</summary>
    public uint ReportTimestamp;

    internal RtcpCongestionControlFeedback Copy()
    {
        RtcpCongestionControlFeedback copy = new RtcpCongestionControlFeedback();
        copy.Reports = new RtcpCongestionControlReport[Reports.Length];
        int index = 0;
        while (index < Reports.Length)
        {
            copy.Reports[index] = Reports[index].Copy();
            index = index + 1;
        }

        copy.ReportTimestamp = ReportTimestamp;
        return copy;
    }
}
