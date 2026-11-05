namespace sip;

/// <summary>
/// Holds the outcome of a codec parse operation.
/// </summary>
public class SipParseResult
{
    /// <summary>The parse outcome.</summary>
    public SipParseStatus Status;

    /// <summary>The parsed message, or null unless parsing completed.</summary>
    public SipMessage? Message;

    /// <summary>The parsed URI, or null when the operation did not parse a URI.</summary>
    public SipUri? Uri;

    /// <summary>The parsed MIME entity, or null when the operation did not parse MIME.</summary>
    public SipMimeEntity? MimeEntity;

    /// <summary>The parsed SDP session, or null when the operation did not parse SDP.</summary>
    public SipSessionDescription? SessionDescription;

    /// <summary>The parsed XML body, or null when the operation did not parse XML.</summary>
    public SipXmlBody? XmlBody;

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
