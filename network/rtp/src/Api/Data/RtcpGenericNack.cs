namespace rtp;

/// <summary>
/// Contains RFC 4585 Generic NACK feedback entries.
/// </summary>
public class RtcpGenericNack
{
    /// <summary>The canonical PID and bitmask entries.</summary>
    public RtcpGenericNackEntry[] Entries = [];

    internal RtcpGenericNack Copy()
    {
        RtcpGenericNack copy = new RtcpGenericNack();
        copy.Entries = new RtcpGenericNackEntry[Entries.Length];
        int index = 0;
        while (index < Entries.Length)
        {
            copy.Entries[index] = Entries[index].Copy();
            index = index + 1;
        }

        return copy;
    }
}
