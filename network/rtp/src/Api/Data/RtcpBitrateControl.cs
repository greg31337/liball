namespace rtp;

/// <summary>
/// Contains RFC 5104 temporary maximum bitrate control entries.
/// </summary>
public class RtcpBitrateControl
{
    /// <summary>Whether the message is a TMMBN notification rather than a TMMBR request.</summary>
    public bool IsNotification;

    /// <summary>The bitrate-control entries.</summary>
    public RtcpBitrateControlEntry[] Entries = [];

    internal RtcpBitrateControl Copy()
    {
        RtcpBitrateControl copy = new RtcpBitrateControl();
        copy.IsNotification = IsNotification;
        copy.Entries = new RtcpBitrateControlEntry[Entries.Length];
        int index = 0;
        while (index < Entries.Length)
        {
            copy.Entries[index] = Entries[index].Copy();
            index = index + 1;
        }

        return copy;
    }
}
