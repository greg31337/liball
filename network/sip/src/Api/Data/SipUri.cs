namespace sip;

/// <summary>
/// Holds a SIP, SIPS, TEL, or extension absolute URI.
/// </summary>
public class SipUri
{
    /// <summary>The recognized URI scheme.</summary>
    public SipUriScheme Scheme;

    /// <summary>The original scheme token, including an unknown absolute URI scheme.</summary>
    public string? SchemeToken;

    /// <summary>The decoded user or telephone-number component.</summary>
    public string? User;

    /// <summary>The decoded password component.</summary>
    public string? Password;

    /// <summary>The host name, IPv4 address, or unbracketed IPv6 address.</summary>
    public string? Host;

    /// <summary>The explicit port, or negative one when no port was supplied.</summary>
    public int Port = -1;

    /// <summary>The ordered URI parameters.</summary>
    public SipUriParameter[] Parameters = new SipUriParameter[0];

    /// <summary>The ordered URI query headers.</summary>
    public SipUriHeader[] Headers = new SipUriHeader[0];

    /// <summary>The scheme-specific part of an unrecognized absolute URI.</summary>
    public string? OpaquePart;

    /// <summary>The original URI text when preservation was requested.</summary>
    public string? OriginalText;

    internal string? ComparisonUserSource;
    internal string? ComparisonUserValue;
    internal string? ComparisonPasswordSource;
    internal string? ComparisonPasswordValue;
}
