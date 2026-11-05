namespace rtp;

/// <summary>
/// Contains the result of polling the regular RTCP scheduler.
/// </summary>
public class RtcpScheduleDecision
{
    /// <summary>Whether an RTCP compound packet should be sent now.</summary>
    public bool ShouldSend;

    /// <summary>Whether the decision belongs to the initial RTCP interval.</summary>
    public bool IsInitial;

    /// <summary>Whether the local source is treated as an active sender.</summary>
    public bool IsSender;

    /// <summary>Whether this decision schedules a goodbye transmission.</summary>
    public bool IsGoodbye;

    /// <summary>The scheduled monotonic transmission time.</summary>
    public long ScheduledTimeTicks;

    /// <summary>The interval until the scheduled transmission in monotonic ticks.</summary>
    public long DelayTicks;

    /// <summary>The member count used to calculate the interval.</summary>
    public int MemberCount;

    /// <summary>The sender count used to calculate the interval.</summary>
    public int SenderCount;

    /// <summary>The compound packet to transmit when a packet is ready.</summary>
    public RtcpCompoundPacket? CompoundPacket;

    internal RtcpScheduleDecision Copy()
    {
        RtcpScheduleDecision copy = new RtcpScheduleDecision();
        copy.ShouldSend = ShouldSend;
        copy.IsInitial = IsInitial;
        copy.IsSender = IsSender;
        copy.IsGoodbye = IsGoodbye;
        copy.ScheduledTimeTicks = ScheduledTimeTicks;
        copy.DelayTicks = DelayTicks;
        copy.MemberCount = MemberCount;
        copy.SenderCount = SenderCount;
        if (CompoundPacket != null)
        {
            copy.CompoundPacket = CompoundPacket.Copy();
        }

        return copy;
    }
}
