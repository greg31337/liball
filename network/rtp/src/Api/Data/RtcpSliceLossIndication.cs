namespace rtp;

/// <summary>
/// Contains RFC 4585 slice loss indication entries.
/// </summary>
public class RtcpSliceLossIndication
{
    /// <summary>The reported lost-slice entries.</summary>
    public RtcpSliceLossEntry[] Entries = [];

    internal RtcpSliceLossIndication Copy()
    {
        RtcpSliceLossIndication copy = new RtcpSliceLossIndication();
        copy.Entries = new RtcpSliceLossEntry[Entries.Length];
        int index = 0;
        while (index < Entries.Length)
        {
            copy.Entries[index] = Entries[index].Copy();
            index = index + 1;
        }

        return copy;
    }
}
