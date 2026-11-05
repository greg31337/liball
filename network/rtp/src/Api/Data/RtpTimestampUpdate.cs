namespace rtp;

/// <summary>
/// Contains the result of extending one RTP timestamp across wraparound.
/// </summary>
public class RtpTimestampUpdate
{
    /// <summary>The received 32-bit RTP timestamp.</summary>
    public uint Timestamp;

    /// <summary>The timestamp extended onto a signed 64-bit media timeline.</summary>
    public long ExtendedTimestamp;

    /// <summary>The signed change from the previously accepted extended timestamp.</summary>
    public long Delta;

    /// <summary>The active RTP timestamp clock rate.</summary>
    public int ClockRate;

    /// <summary>Whether the timestamp was accepted into the source timeline.</summary>
    public bool Accepted;

    /// <summary>Whether the timestamp advances the source timeline.</summary>
    public bool IsForward;

    /// <summary>Whether the timestamp belongs before the current timeline head.</summary>
    public bool IsReordered;

    /// <summary>Whether processing detected and reset a timestamp discontinuity.</summary>
    public bool IsDiscontinuity;

    /// <summary>Whether the active payload clock rate changed for this update.</summary>
    public bool ClockRateChanged;

    internal RtpTimestampUpdate Copy()
    {
        RtpTimestampUpdate copy = new RtpTimestampUpdate();
        copy.Timestamp = Timestamp;
        copy.ExtendedTimestamp = ExtendedTimestamp;
        copy.Delta = Delta;
        copy.ClockRate = ClockRate;
        copy.Accepted = Accepted;
        copy.IsForward = IsForward;
        copy.IsReordered = IsReordered;
        copy.IsDiscontinuity = IsDiscontinuity;
        copy.ClockRateChanged = ClockRateChanged;
        return copy;
    }
}
