namespace sip;

/// <summary>
/// Holds a securely parsed SIP XML event body and its original bytes.
/// </summary>
public class SipXmlBody
{
    /// <summary>The XML body media type.</summary>
    public SipMediaType? MediaType;

    /// <summary>The root element local name.</summary>
    public string? RootName;

    /// <summary>The root element namespace URI.</summary>
    public string? NamespaceUri;

    /// <summary>The decoded XML text.</summary>
    public string? XmlText;

    /// <summary>The original XML body bytes when preservation was requested.</summary>
    public byte[] OriginalBytes = new byte[0];
}
