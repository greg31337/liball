namespace sip;

/// <summary>
/// Holds a MIME entity, including multipart framing data and nested parts.
/// </summary>
public class SipMimeEntity
{
    /// <summary>The ordered MIME header fields.</summary>
    public SipHeader[] Headers = new SipHeader[0];

    /// <summary>The parsed Content-Type value.</summary>
    public SipMediaType? ContentType;

    /// <summary>The parsed Content-Disposition value.</summary>
    public SipContentDisposition? ContentDisposition;

    /// <summary>The Content-Transfer-Encoding token, including an unknown encoding.</summary>
    public string? ContentTransferEncoding;

    /// <summary>The raw body bytes for a non-multipart entity.</summary>
    public byte[] Body = new byte[0];

    /// <summary>The nested body parts for a multipart entity.</summary>
    public SipMimeEntity[] Parts = new SipMimeEntity[0];

    /// <summary>The multipart preamble bytes.</summary>
    public byte[] Preamble = new byte[0];

    /// <summary>The multipart epilogue bytes.</summary>
    public byte[] Epilogue = new byte[0];

    /// <summary>The parsed SIP fragment when the media type is message/sipfrag.</summary>
    public SipMessage? SipFragment;

    /// <summary>The original complete MIME entity bytes when preservation was requested.</summary>
    public byte[] OriginalBytes = new byte[0];
}
