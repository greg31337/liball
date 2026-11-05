namespace sip;

/// <summary>
/// Holds one SDP media description and its media-level fields.
/// </summary>
public class SipMediaDescription
{
    /// <summary>The media token.</summary>
    public string? Media;

    /// <summary>The media port.</summary>
    public int Port = -1;

    /// <summary>The media port count.</summary>
    public int PortCount = 1;

    /// <summary>The transport protocol token.</summary>
    public string? Protocol;

    /// <summary>The ordered media format tokens.</summary>
    public string[] Formats = new string[0];

    /// <summary>The optional media title.</summary>
    public string? Title;

    /// <summary>The media connection field value.</summary>
    public string? Connection;

    /// <summary>The ordered media bandwidth field values.</summary>
    public string[] Bandwidths = new string[0];

    /// <summary>The optional encryption key field value.</summary>
    public string? EncryptionKey;

    /// <summary>The ordered media attributes.</summary>
    public SipSdpAttribute[] Attributes = new SipSdpAttribute[0];

    /// <summary>The ordered unrecognized media-level lines retained for forwarding.</summary>
    public string[] UnknownLines = new string[0];
}
