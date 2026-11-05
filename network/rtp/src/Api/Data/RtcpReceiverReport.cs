namespace rtp;

/// <summary>
/// Contains an RTCP receiver report body.
/// </summary>
public class RtcpReceiverReport
{
    /// <summary>The synchronization source sending the report.</summary>
    public uint SenderSynchronizationSource;

    /// <summary>The reception report blocks.</summary>
    public RtcpReceptionReport[] ReceptionReports = [];

    /// <summary>Profile-specific bytes following the standard report blocks.</summary>
    public byte[] ProfileExtension = [];

    internal RtcpReceiverReport Copy()
    {
        RtcpReceiverReport copy = new RtcpReceiverReport();
        copy.SenderSynchronizationSource = SenderSynchronizationSource;
        copy.ReceptionReports = new RtcpReceptionReport[ReceptionReports.Length];
        int index = 0;
        while (index < ReceptionReports.Length)
        {
            copy.ReceptionReports[index] = ReceptionReports[index].Copy();
            index = index + 1;
        }

        copy.ProfileExtension = (byte[])ProfileExtension.Clone();
        return copy;
    }
}
