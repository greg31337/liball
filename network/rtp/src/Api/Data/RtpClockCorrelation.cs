namespace rtp;

/// <summary>
/// Contains an RTP-to-NTP clock correlation for one source.
/// </summary>
public class RtpClockCorrelation
{
    /// <summary>Whether the correlation contains a usable sender-report sample.</summary>
    public bool IsValid;

    /// <summary>The synchronization source described by the correlation.</summary>
    public uint SourceIdentifier;

    /// <summary>The RTP timestamp from the correlated sender report.</summary>
    public uint RtpTimestamp;

    /// <summary>The correlated RTP timestamp extended across wraparound.</summary>
    public long ExtendedRtpTimestamp;

    /// <summary>The correlated 64-bit NTP timestamp.</summary>
    public ulong NtpTimestamp;

    /// <summary>The RTP timestamp clock rate used by the correlation.</summary>
    public int ClockRate;

    /// <summary>The corresponding time on the common presentation timeline.</summary>
    public long PresentationTimeTicks;

    internal RtpClockCorrelation Copy()
    {
        RtpClockCorrelation copy = new RtpClockCorrelation();
        copy.IsValid = IsValid;
        copy.SourceIdentifier = SourceIdentifier;
        copy.RtpTimestamp = RtpTimestamp;
        copy.ExtendedRtpTimestamp = ExtendedRtpTimestamp;
        copy.NtpTimestamp = NtpTimestamp;
        copy.ClockRate = ClockRate;
        copy.PresentationTimeTicks = PresentationTimeTicks;
        return copy;
    }
}
