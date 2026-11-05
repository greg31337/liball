namespace rtp;

/// <summary>
/// Contains a MID, RID, or repaired RID RTP header-extension value.
/// </summary>
public class RtpStreamIdentifierExtension
{
    /// <summary>The registered URI identifying the stream-identifier kind.</summary>
    public string Uri = "";

    /// <summary>The stream identifier text.</summary>
    public string Value = "";

    internal RtpStreamIdentifierExtension Copy()
    {
        RtpStreamIdentifierExtension copy = new RtpStreamIdentifierExtension();
        copy.Uri = Uri;
        copy.Value = Value;
        return copy;
    }
}
