namespace rtp;

/// <summary>
/// Contains a request for retransmission, redundancy, FEC, or application repair.
/// </summary>
public class RtpRepairRequest
{
    /// <summary>The requested repair mechanism.</summary>
    public RtpRepairKind Kind;

    /// <summary>The synchronization source with missing media packets.</summary>
    public uint SourceIdentifier;

    /// <summary>The missing RTP sequence numbers to repair.</summary>
    public ushort[] SequenceNumbers = [];

    /// <summary>The number of repair attempts already made.</summary>
    public int AttemptCount;

    /// <summary>The monotonic time at which the request was created.</summary>
    public long RequestTimeTicks;

    /// <summary>The monotonic deadline after which repair is no longer useful.</summary>
    public long DeadlineTicks;

    /// <summary>Application-defined metadata accompanying the repair request.</summary>
    public byte[] Data = [];

    internal RtpRepairRequest Copy()
    {
        RtpRepairRequest copy = new RtpRepairRequest();
        copy.Kind = Kind;
        copy.SourceIdentifier = SourceIdentifier;
        copy.SequenceNumbers = (ushort[])SequenceNumbers.Clone();
        copy.AttemptCount = AttemptCount;
        copy.RequestTimeTicks = RequestTimeTicks;
        copy.DeadlineTicks = DeadlineTicks;
        copy.Data = (byte[])Data.Clone();
        return copy;
    }
}
