namespace sip;

/// <summary>
/// Holds generic and typed values produced by parsing one SIP header field.
/// </summary>
public class SipHeaderParseResult
{
    /// <summary>The parse outcome.</summary>
    public SipParseStatus Status;

    /// <summary>The generic parsed header field.</summary>
    public SipHeader? Header;

    /// <summary>The parsed name-address value.</summary>
    public SipNameAddress? NameAddress;

    /// <summary>The parsed Via values.</summary>
    public SipViaValue[] ViaValues = new SipViaValue[0];

    /// <summary>The parsed route values.</summary>
    public SipRouteValue[] RouteValues = new SipRouteValue[0];

    /// <summary>The parsed contact values.</summary>
    public SipContactValue[] ContactValues = new SipContactValue[0];

    /// <summary>The parsed CSeq value.</summary>
    public SipCSeqValue? CSeq;

    /// <summary>The parsed Call-ID value.</summary>
    public string? CallId;

    /// <summary>The parsed Max-Forwards value, or negative one when this is another header.</summary>
    public int MaxForwards = -1;

    /// <summary>The parsed media type value.</summary>
    public SipMediaType? MediaType;

    /// <summary>The parsed content disposition value.</summary>
    public SipContentDisposition? ContentDisposition;

    /// <summary>The parsed Content-Length value, or negative one for another header.</summary>
    public int ContentLength = -1;

    /// <summary>The parsed Accept media ranges.</summary>
    public SipMediaType[] MediaTypes = new SipMediaType[0];

    /// <summary>The parsed Allow method tokens.</summary>
    public string[] Methods = new string[0];

    /// <summary>The parsed Supported, Require, Proxy-Require, or Unsupported option tags.</summary>
    public string[] OptionTags = new string[0];

    /// <summary>The parsed Date value.</summary>
    public System.DateTime Date;

    /// <summary>Indicates that the header field supplied a parsed Date value.</summary>
    public bool HasDate;

    /// <summary>The parsed Expires value, or negative one for another header.</summary>
    public int ExpiresSeconds = -1;

    /// <summary>The parsed Min-Expires value, or negative one for another header.</summary>
    public int MinExpiresSeconds = -1;

    /// <summary>The parsed Retry-After value.</summary>
    public SipRetryAfterValue? RetryAfter;

    /// <summary>The parsed Warning values.</summary>
    public SipWarningValue[] WarningValues = new SipWarningValue[0];

    /// <summary>The parsed Server or User-Agent product and comment values.</summary>
    public SipProductValue[] ProductValues = new SipProductValue[0];

    /// <summary>The parsed WWW-Authenticate or Proxy-Authenticate challenge.</summary>
    public SipDigestChallenge? DigestChallenge;

    /// <summary>The parsed Authorization, Proxy-Authorization, or Authentication-Info value.</summary>
    public SipDigestAuthorization? DigestAuthorization;

    /// <summary>The parsed RFC 8898 Bearer token or challenge.</summary>
    public SipBearerToken? BearerToken;

    /// <summary>The parsed values from a recognized SIP extension header field.</summary>
    public SipExtensionHeaderValue[] ExtensionValues = new SipExtensionHeaderValue[0];

    /// <summary>The number of source bytes consumed.</summary>
    public int Consumed;

    /// <summary>The total number of bytes required, or negative one when it cannot yet be determined.</summary>
    public int RequiredBytes = -1;

    /// <summary>The failure byte offset, or negative one when unavailable.</summary>
    public int ErrorOffset = -1;

    /// <summary>The failure message, or null when no failure occurred.</summary>
    public string? ErrorMessage;

    /// <summary>The validation issues found while parsing.</summary>
    public SipValidationIssue[] ValidationIssues = new SipValidationIssue[0];
}
