namespace rtp;

/// <summary>
/// Contains an RTCP goodbye packet body.
/// </summary>
public class RtcpGoodbye
{
    /// <summary>The SSRC or CSRC identifiers leaving the session.</summary>
    public uint[] Sources = [];

    /// <summary>Whether the optional reason-length field is present.</summary>
    public bool HasReason;

    /// <summary>The UTF-8 reason bytes, preserved exactly as received.</summary>
    public byte[] Reason = [];

    internal RtcpGoodbye Copy()
    {
        RtcpGoodbye copy = new RtcpGoodbye();
        copy.Sources = (uint[])Sources.Clone();
        copy.HasReason = HasReason;
        copy.Reason = (byte[])Reason.Clone();
        return copy;
    }
}
