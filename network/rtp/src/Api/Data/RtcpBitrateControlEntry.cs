namespace rtp;

/// <summary>
/// Contains one RFC 5104 TMMBR or TMMBN bitrate-control entry.
/// </summary>
public class RtcpBitrateControlEntry
{
    /// <summary>The SSRC to which the bitrate limit applies.</summary>
    public uint SourceIdentifier;

    /// <summary>The six-bit maximum bitrate exponent.</summary>
    public byte Exponent;

    /// <summary>The seventeen-bit maximum bitrate mantissa.</summary>
    public uint Mantissa;

    /// <summary>The nine-bit measured packet overhead in bytes.</summary>
    public ushort Overhead;

    internal RtcpBitrateControlEntry Copy()
    {
        RtcpBitrateControlEntry copy = new RtcpBitrateControlEntry();
        copy.SourceIdentifier = SourceIdentifier;
        copy.Exponent = Exponent;
        copy.Mantissa = Mantissa;
        copy.Overhead = Overhead;
        return copy;
    }
}
