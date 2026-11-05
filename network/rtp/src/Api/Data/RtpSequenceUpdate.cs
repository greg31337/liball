namespace rtp;

/// <summary>
/// Contains the result of processing one RTP sequence number.
/// </summary>
public class RtpSequenceUpdate
{
    /// <summary>The sequence-processing outcome.</summary>
    public RtpSequenceStatus Status;

    /// <summary>The received 16-bit RTP sequence number.</summary>
    public ushort SequenceNumber;

    /// <summary>The received sequence number extended across wraparound.</summary>
    public uint ExtendedSequenceNumber;

    /// <summary>The first validated extended sequence number for the source.</summary>
    public uint BaseExtendedSequenceNumber;

    /// <summary>The greatest validated extended sequence number observed.</summary>
    public uint HighestExtendedSequenceNumber;

    /// <summary>The number of remaining sequential packets required during probation.</summary>
    public int ProbationRemaining;

    /// <summary>Whether the packet is accepted for normal stream processing.</summary>
    public bool Accepted;

    internal RtpSequenceUpdate Copy()
    {
        RtpSequenceUpdate copy = new RtpSequenceUpdate();
        copy.Status = Status;
        copy.SequenceNumber = SequenceNumber;
        copy.ExtendedSequenceNumber = ExtendedSequenceNumber;
        copy.BaseExtendedSequenceNumber = BaseExtendedSequenceNumber;
        copy.HighestExtendedSequenceNumber = HighestExtendedSequenceNumber;
        copy.ProbationRemaining = ProbationRemaining;
        copy.Accepted = Accepted;
        return copy;
    }
}
