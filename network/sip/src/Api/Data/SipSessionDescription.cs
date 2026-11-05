namespace sip;

/// <summary>
/// Holds an SDP session description and its ordered media descriptions.
/// </summary>
public class SipSessionDescription
{
    /// <summary>The SDP protocol version.</summary>
    public int ProtocolVersion;

    /// <summary>The complete origin field value.</summary>
    public string? Origin;

    /// <summary>The session name.</summary>
    public string? SessionName;

    /// <summary>The optional session information.</summary>
    public string? SessionInformation;

    /// <summary>The optional session URI.</summary>
    public string? Uri;

    /// <summary>The ordered email field values.</summary>
    public string[] Emails = new string[0];

    /// <summary>The ordered phone field values.</summary>
    public string[] Phones = new string[0];

    /// <summary>The session connection field value.</summary>
    public string? Connection;

    /// <summary>The ordered session bandwidth field values.</summary>
    public string[] Bandwidths = new string[0];

    /// <summary>The ordered timing field values.</summary>
    public string[] Timings = new string[0];

    /// <summary>The ordered repeat timing field values.</summary>
    public string[] RepeatTimes = new string[0];

    /// <summary>The optional time-zone adjustment field value.</summary>
    public string? TimeZones;

    /// <summary>The optional encryption key field value.</summary>
    public string? EncryptionKey;

    /// <summary>The ordered session attributes.</summary>
    public SipSdpAttribute[] Attributes = new SipSdpAttribute[0];

    /// <summary>The ordered media descriptions.</summary>
    public SipMediaDescription[] MediaDescriptions = new SipMediaDescription[0];

    /// <summary>The ordered unrecognized SDP lines retained for forwarding.</summary>
    public string[] UnknownLines = new string[0];

    /// <summary>The original SDP bytes when preservation was requested.</summary>
    public byte[] OriginalBytes = new byte[0];
}
