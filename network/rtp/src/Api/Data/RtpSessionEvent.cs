namespace rtp;

/// <summary>
/// Contains one event produced by an RTP session operation.
/// </summary>
public class RtpSessionEvent
{
    /// <summary>The event category.</summary>
    public RtpSessionEventType Type;

    /// <summary>The classified packet kind associated with the event.</summary>
    public RtpPacketKind PacketKind;

    /// <summary>The sequence-processing status associated with the event.</summary>
    public RtpSequenceStatus SequenceStatus;

    /// <summary>The source status associated with the event.</summary>
    public RtpSourceStatus SourceStatus;

    /// <summary>The jitter-buffer status associated with the event.</summary>
    public RtpJitterBufferStatus JitterBufferStatus;

    /// <summary>The repair mechanism associated with the event.</summary>
    public RtpRepairKind RepairKind;

    /// <summary>The synchronization source associated with the event.</summary>
    public uint SourceIdentifier;

    /// <summary>A human-readable description for recoverable errors or state changes.</summary>
    public string? Message;

    /// <summary>The packet timing and transport context associated with the event.</summary>
    public RtpPacketContext? PacketContext;

    /// <summary>The RTP packet associated with the event.</summary>
    public RtpPacket? Packet;

    /// <summary>The header-extension data associated with the event.</summary>
    public RtpHeaderExtension? HeaderExtension;

    /// <summary>The RTCP packet associated with the event.</summary>
    public RtcpPacket? ControlPacket;

    /// <summary>The feedback decision associated with the event.</summary>
    public RtpFeedbackDecision? FeedbackDecision;

    /// <summary>The session configuration associated with a configuration event.</summary>
    public RtpSessionConfiguration? Configuration;

    /// <summary>The source snapshot associated with the event.</summary>
    public RtpSourceSnapshot? Source;

    /// <summary>The sequence-number update associated with the event.</summary>
    public RtpSequenceUpdate? SequenceUpdate;

    /// <summary>The timestamp update associated with the event.</summary>
    public RtpTimestampUpdate? TimestampUpdate;

    /// <summary>The clock correlation associated with the event.</summary>
    public RtpClockCorrelation? ClockCorrelation;

    /// <summary>The jitter-buffer update associated with the event.</summary>
    public RtpJitterBufferUpdate? JitterBufferUpdate;

    /// <summary>The repair request associated with the event.</summary>
    public RtpRepairRequest? RepairRequest;

    /// <summary>The repair result associated with the event.</summary>
    public RtpRepairResult? RepairResult;

    /// <summary>The payload packetization result associated with the event.</summary>
    public RtpPacketizationResult? PacketizationResult;

    /// <summary>The payload depacketization result associated with the event.</summary>
    public RtpDepacketizationResult? DepacketizationResult;

    internal RtpSessionEvent Copy()
    {
        RtpSessionEvent copy = new RtpSessionEvent();
        copy.Type = Type;
        copy.PacketKind = PacketKind;
        copy.SequenceStatus = SequenceStatus;
        copy.SourceStatus = SourceStatus;
        copy.JitterBufferStatus = JitterBufferStatus;
        copy.RepairKind = RepairKind;
        copy.SourceIdentifier = SourceIdentifier;
        copy.Message = Message;
        if (PacketContext != null)
        {
            copy.PacketContext = PacketContext.Copy();
        }

        if (Packet != null)
        {
            copy.Packet = Packet.Copy();
        }

        if (HeaderExtension != null)
        {
            copy.HeaderExtension = HeaderExtension.Copy();
        }

        if (ControlPacket != null)
        {
            copy.ControlPacket = ControlPacket.Copy();
        }

        if (FeedbackDecision != null)
        {
            copy.FeedbackDecision = FeedbackDecision.Copy();
        }

        if (Configuration != null)
        {
            copy.Configuration = Configuration.Copy();
        }

        if (Source != null)
        {
            copy.Source = Source.Copy();
        }

        if (SequenceUpdate != null)
        {
            copy.SequenceUpdate = SequenceUpdate.Copy();
        }

        if (TimestampUpdate != null)
        {
            copy.TimestampUpdate = TimestampUpdate.Copy();
        }

        if (ClockCorrelation != null)
        {
            copy.ClockCorrelation = ClockCorrelation.Copy();
        }

        if (JitterBufferUpdate != null)
        {
            copy.JitterBufferUpdate = JitterBufferUpdate.Copy();
        }

        if (RepairRequest != null)
        {
            copy.RepairRequest = RepairRequest.Copy();
        }

        if (RepairResult != null)
        {
            copy.RepairResult = RepairResult.Copy();
        }

        if (PacketizationResult != null)
        {
            copy.PacketizationResult = PacketizationResult.Copy();
        }

        if (DepacketizationResult != null)
        {
            copy.DepacketizationResult = DepacketizationResult.Copy();
        }

        return copy;
    }
}
