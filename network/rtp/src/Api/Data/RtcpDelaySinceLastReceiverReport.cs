namespace rtp;

/// <summary>
/// Contains one RFC 3611 DLRR sub-block.
/// </summary>
public class RtcpDelaySinceLastReceiverReport
{
    /// <summary>The SSRC of the receiver whose report is referenced.</summary>
    public uint ReceiverSourceIdentifier;

    /// <summary>The middle 32 bits of the referenced receiver-report NTP timestamp.</summary>
    public uint LastReceiverReport;

    /// <summary>The delay since that receiver report in compact-NTP units.</summary>
    public uint DelaySinceLastReceiverReport;

    internal RtcpDelaySinceLastReceiverReport Copy()
    {
        RtcpDelaySinceLastReceiverReport copy = new RtcpDelaySinceLastReceiverReport();
        copy.ReceiverSourceIdentifier = ReceiverSourceIdentifier;
        copy.LastReceiverReport = LastReceiverReport;
        copy.DelaySinceLastReceiverReport = DelaySinceLastReceiverReport;
        return copy;
    }
}
