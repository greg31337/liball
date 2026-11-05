namespace rtp;

/// <summary>
/// Contains an RFC 6051 64-bit NTP header-extension timestamp.
/// </summary>
public class RtpNtp64Extension
{
    /// <summary>The complete unsigned 64-bit NTP timestamp.</summary>
    public ulong Timestamp;

    internal RtpNtp64Extension Copy()
    {
        RtpNtp64Extension copy = new RtpNtp64Extension();
        copy.Timestamp = Timestamp;
        return copy;
    }
}
