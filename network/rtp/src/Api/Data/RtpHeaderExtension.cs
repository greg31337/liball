namespace rtp;

/// <summary>
/// Contains an RTP header extension and its representation format.
/// </summary>
public class RtpHeaderExtension
{
    /// <summary>The RFC 3550 extension profile identifier.</summary>
    public ushort Profile;

    /// <summary>
    /// The representation used by the extension. A raw extension with profile zero,
    /// empty raw data, and elements requests automatic smallest-format selection.
    /// </summary>
    public RtpHeaderExtensionFormat Format;

    /// <summary>The application-defined bits from an RFC 8285 two-byte profile.</summary>
    public byte ApplicationBits;

    /// <summary>The parsed RFC 8285 extension elements.</summary>
    public RtpHeaderExtensionElement[] Elements = [];

    /// <summary>The complete extension data following the envelope.</summary>
    public byte[] Data = [];

    internal RtpHeaderExtension Copy()
    {
        RtpHeaderExtension copy = new RtpHeaderExtension();
        copy.Profile = Profile;
        copy.Format = Format;
        copy.ApplicationBits = ApplicationBits;
        copy.Data = (byte[])Data.Clone();
        copy.Elements = new RtpHeaderExtensionElement[Elements.Length];
        int index = 0;
        while (index < Elements.Length)
        {
            copy.Elements[index] = Elements[index].Copy();
            index = index + 1;
        }

        return copy;
    }
}
