namespace sip;

/// <summary>
/// Holds a display name, URI, and ordered header parameters.
/// </summary>
public class SipNameAddress
{
    /// <summary>The decoded display name.</summary>
    public string? DisplayName;

    /// <summary>The addressed URI.</summary>
    public SipUri? Uri;

    /// <summary>The ordered parameters following the URI.</summary>
    public SipUriParameter[] Parameters = new SipUriParameter[0];

    /// <summary>The original name-address text when preservation was requested.</summary>
    public string? OriginalText;
}
