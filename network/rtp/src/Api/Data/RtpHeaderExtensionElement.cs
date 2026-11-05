namespace rtp;

/// <summary>
/// Contains one RFC 8285 RTP header-extension element.
/// </summary>
public class RtpHeaderExtensionElement
{
    /// <summary>The locally negotiated extension identifier.</summary>
    public int Identifier;

    /// <summary>The element payload bytes.</summary>
    public byte[] Data = [];

    internal RtpHeaderExtensionElement Copy()
    {
        RtpHeaderExtensionElement copy = new RtpHeaderExtensionElement();
        copy.Identifier = Identifier;
        copy.Data = (byte[])Data.Clone();
        return copy;
    }
}
