namespace rtp;

/// <summary>
/// Contains the events produced by a synchronous RTP session operation.
/// </summary>
public class RtpSessionUpdate
{
    /// <summary>The events produced by the operation.</summary>
    public RtpSessionEvent[] Events = [];

    /// <summary>The RTP packets accepted directly from session ingress.</summary>
    public RtpPacket[] AcceptedPackets = [];

    /// <summary>The RTP packets released in media sequence order.</summary>
    public RtpPacket[] ReleasedPackets = [];

    /// <summary>The RTP packets prepared for application transport.</summary>
    public RtpPacket[] OutboundPackets = [];

    /// <summary>The compound RTCP packets prepared for application transport.</summary>
    public RtcpCompoundPacket[] OutboundControlPackets = [];

    /// <summary>The pending feedback requests produced by stream processing.</summary>
    public RtpFeedbackRequest[] FeedbackRequests = [];

    /// <summary>The feedback scheduling decisions produced by the operation.</summary>
    public RtpFeedbackDecision[] FeedbackDecisions = [];

    /// <summary>The regular RTCP scheduling decisions produced by the operation.</summary>
    public RtcpScheduleDecision[] ControlScheduleDecisions = [];

    /// <summary>The repair requests produced by loss detection.</summary>
    public RtpRepairRequest[] RepairRequests = [];

    /// <summary>The repair results accepted by the session operation.</summary>
    public RtpRepairResult[] RepairResults = [];

    /// <summary>The session snapshot produced by the operation when requested.</summary>
    public RtpSessionSnapshot? Snapshot;

    internal RtpSessionUpdate Copy()
    {
        RtpSessionUpdate copy = new RtpSessionUpdate();
        copy.Events = new RtpSessionEvent[Events.Length];
        int eventIndex = 0;
        while (eventIndex < Events.Length)
        {
            copy.Events[eventIndex] = Events[eventIndex].Copy();
            eventIndex = eventIndex + 1;
        }

        copy.AcceptedPackets = CopyPackets(AcceptedPackets);
        copy.ReleasedPackets = CopyPackets(ReleasedPackets);
        copy.OutboundPackets = CopyPackets(OutboundPackets);
        copy.OutboundControlPackets = new RtcpCompoundPacket[OutboundControlPackets.Length];
        int controlIndex = 0;
        while (controlIndex < OutboundControlPackets.Length)
        {
            copy.OutboundControlPackets[controlIndex] =
                OutboundControlPackets[controlIndex].Copy();
            controlIndex = controlIndex + 1;
        }

        copy.FeedbackRequests = new RtpFeedbackRequest[FeedbackRequests.Length];
        int feedbackRequestIndex = 0;
        while (feedbackRequestIndex < FeedbackRequests.Length)
        {
            copy.FeedbackRequests[feedbackRequestIndex] =
                FeedbackRequests[feedbackRequestIndex].Copy();
            feedbackRequestIndex = feedbackRequestIndex + 1;
        }

        copy.FeedbackDecisions = new RtpFeedbackDecision[FeedbackDecisions.Length];
        int feedbackDecisionIndex = 0;
        while (feedbackDecisionIndex < FeedbackDecisions.Length)
        {
            copy.FeedbackDecisions[feedbackDecisionIndex] =
                FeedbackDecisions[feedbackDecisionIndex].Copy();
            feedbackDecisionIndex = feedbackDecisionIndex + 1;
        }

        copy.ControlScheduleDecisions =
            new RtcpScheduleDecision[ControlScheduleDecisions.Length];
        int scheduleIndex = 0;
        while (scheduleIndex < ControlScheduleDecisions.Length)
        {
            copy.ControlScheduleDecisions[scheduleIndex] =
                ControlScheduleDecisions[scheduleIndex].Copy();
            scheduleIndex = scheduleIndex + 1;
        }

        copy.RepairRequests = new RtpRepairRequest[RepairRequests.Length];
        int repairRequestIndex = 0;
        while (repairRequestIndex < RepairRequests.Length)
        {
            copy.RepairRequests[repairRequestIndex] = RepairRequests[repairRequestIndex].Copy();
            repairRequestIndex = repairRequestIndex + 1;
        }

        copy.RepairResults = new RtpRepairResult[RepairResults.Length];
        int repairResultIndex = 0;
        while (repairResultIndex < RepairResults.Length)
        {
            copy.RepairResults[repairResultIndex] = RepairResults[repairResultIndex].Copy();
            repairResultIndex = repairResultIndex + 1;
        }

        if (Snapshot != null)
        {
            copy.Snapshot = Snapshot.Copy();
        }

        return copy;
    }

    internal RtpPacket[] CopyPackets(RtpPacket[] packets)
    {
        RtpPacket[] copies = new RtpPacket[packets.Length];
        int packetIndex = 0;
        while (packetIndex < packets.Length)
        {
            copies[packetIndex] = packets[packetIndex].Copy();
            packetIndex = packetIndex + 1;
        }

        return copies;
    }
}
