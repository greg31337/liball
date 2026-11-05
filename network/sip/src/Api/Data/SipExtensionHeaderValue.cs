namespace sip;

/// <summary>
/// Holds one typed value from a SIP extension header field.
/// </summary>
public class SipExtensionHeaderValue
{
    /// <summary>The primary token, identifier, state, protocol, or call identifier.</summary>
    public string? Token;

    /// <summary>The first typed secondary token.</summary>
    public string? SecondaryToken;

    /// <summary>The second typed secondary token.</summary>
    public string? TertiaryToken;

    /// <summary>The primary integer value, or negative one when absent.</summary>
    public int Number = -1;

    /// <summary>The secondary integer value, or negative one when absent.</summary>
    public int SecondNumber = -1;

    /// <summary>The recognized request method.</summary>
    public SipMethod Method;

    /// <summary>The request method token, including an extension method.</summary>
    public string? MethodToken;

    /// <summary>The typed boolean or presence flag value.</summary>
    public bool Flag;

    /// <summary>Indicates that the typed flag was supplied.</summary>
    public bool HasFlag;

    /// <summary>The parsed URI when the header value carries a URI.</summary>
    public SipUri? Uri;

    /// <summary>The parsed name-address when the header value carries an address.</summary>
    public SipNameAddress? Address;

    /// <summary>The ordered extension parameters.</summary>
    public SipUriParameter[] Parameters = new SipUriParameter[0];

    /// <summary>The complete original value when preservation was requested.</summary>
    public string? OriginalText;
}
