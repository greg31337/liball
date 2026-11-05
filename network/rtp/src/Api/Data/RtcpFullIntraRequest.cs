namespace rtp;

/// <summary>
/// Contains RFC 5104 full intra request entries.
/// </summary>
public class RtcpFullIntraRequest
{
    /// <summary>The media-source requests.</summary>
    public RtcpFullIntraRequestEntry[] Entries = [];

    internal RtcpFullIntraRequest Copy()
    {
        RtcpFullIntraRequest copy = new RtcpFullIntraRequest();
        copy.Entries = new RtcpFullIntraRequestEntry[Entries.Length];
        int index = 0;
        while (index < Entries.Length)
        {
            copy.Entries[index] = Entries[index].Copy();
            index = index + 1;
        }

        return copy;
    }
}
