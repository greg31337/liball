namespace sip;

/// <summary>Holds a redirect-server response and ordered contacts.</summary>
public class SipRedirectResult
{
    /// <summary>Indicates successful redirect generation.</summary>
    public bool Success;
    /// <summary>The generated redirect response.</summary>
    public SipMessage? Response;
    /// <summary>The ordered redirect contacts.</summary>
    public SipContactValue[] Contacts = new SipContactValue[0];
    /// <summary>The failure message.</summary>
    public string? ErrorMessage;
}
