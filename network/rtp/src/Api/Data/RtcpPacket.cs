namespace rtp;

/// <summary>
/// Contains an RTCP packet discriminator and its typed packet data.
/// </summary>
public class RtcpPacket
{
    /// <summary>The common RTCP header fields.</summary>
    public RtcpHeader Header = new RtcpHeader();

    /// <summary>The RTCP packet type.</summary>
    public RtcpPacketType Type;

    /// <summary>The sender report body when the packet is an SR.</summary>
    public RtcpSenderReport? SenderReport;

    /// <summary>The receiver report body when the packet is an RR.</summary>
    public RtcpReceiverReport? ReceiverReport;

    /// <summary>The source-description body when the packet is SDES.</summary>
    public RtcpSourceDescription? SourceDescription;

    /// <summary>The goodbye body when the packet is BYE.</summary>
    public RtcpGoodbye? Goodbye;

    /// <summary>The application-defined body when the packet is APP.</summary>
    public RtcpApplicationDefined? ApplicationDefined;

    /// <summary>The opaque body when the packet type is not recognized.</summary>
    public RtcpUnknownPacket? UnknownPacket;

    /// <summary>The feedback data when the packet carries feedback.</summary>
    public RtcpFeedbackPacket? Feedback;

    /// <summary>The extended-report body when the packet is XR.</summary>
    public RtcpExtendedReport? ExtendedReport;

    /// <summary>The source-description items carried by the packet.</summary>
    public RtcpSourceDescriptionItem[] SourceDescriptionItems = [];

    /// <summary>The extended-report blocks carried by the packet.</summary>
    public RtcpExtendedReportBlock[] ExtendedReportBlocks = [];

    /// <summary>The preserved RTCP padding bytes, including the final count byte.</summary>
    public byte[] Padding = [];

    internal RtcpPacket Copy()
    {
        RtcpPacket copy = new RtcpPacket();
        copy.Header = Header.Copy();
        copy.Type = Type;
        if (SenderReport != null)
        {
            copy.SenderReport = SenderReport.Copy();
        }

        if (ReceiverReport != null)
        {
            copy.ReceiverReport = ReceiverReport.Copy();
        }

        if (SourceDescription != null)
        {
            copy.SourceDescription = SourceDescription.Copy();
        }

        if (Goodbye != null)
        {
            copy.Goodbye = Goodbye.Copy();
        }

        if (ApplicationDefined != null)
        {
            copy.ApplicationDefined = ApplicationDefined.Copy();
        }

        if (UnknownPacket != null)
        {
            copy.UnknownPacket = UnknownPacket.Copy();
        }

        if (Feedback != null)
        {
            copy.Feedback = Feedback.Copy();
        }

        if (ExtendedReport != null)
        {
            copy.ExtendedReport = ExtendedReport.Copy();
        }

        copy.SourceDescriptionItems = new RtcpSourceDescriptionItem[SourceDescriptionItems.Length];
        int itemIndex = 0;
        while (itemIndex < SourceDescriptionItems.Length)
        {
            copy.SourceDescriptionItems[itemIndex] = SourceDescriptionItems[itemIndex].Copy();
            itemIndex = itemIndex + 1;
        }

        copy.ExtendedReportBlocks = new RtcpExtendedReportBlock[ExtendedReportBlocks.Length];
        int blockIndex = 0;
        while (blockIndex < ExtendedReportBlocks.Length)
        {
            copy.ExtendedReportBlocks[blockIndex] = ExtendedReportBlocks[blockIndex].Copy();
            blockIndex = blockIndex + 1;
        }

        copy.Padding = (byte[])Padding.Clone();
        return copy;
    }
}
