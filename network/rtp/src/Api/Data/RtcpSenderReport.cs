namespace rtp;

/// <summary>
/// Contains an RTCP sender report body.
/// </summary>
public class RtcpSenderReport
{
    /// <summary>The synchronization source sending the report.</summary>
    public uint SenderSynchronizationSource;

    /// <summary>The sender timing and transmission counters.</summary>
    public RtcpSenderInformation SenderInformation = new RtcpSenderInformation();

    /// <summary>The reception report blocks.</summary>
    public RtcpReceptionReport[] ReceptionReports = [];

    /// <summary>Profile-specific bytes following the standard report blocks.</summary>
    public byte[] ProfileExtension = [];

    internal RtcpSenderReport Copy()
    {
        RtcpSenderReport copy = new RtcpSenderReport();
        copy.SenderSynchronizationSource = SenderSynchronizationSource;
        copy.SenderInformation = SenderInformation.Copy();
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
