namespace sip;

/// <summary>Holds advertised SIP methods, extensions, media, events, headers, and URI schemes.</summary>
public class SipCapabilitySet
{
    /// <summary>The supported method tokens.</summary>
    public string[] Methods = new string[0];
    /// <summary>The supported option tags.</summary>
    public string[] OptionTags = new string[0];
    /// <summary>The accepted media types.</summary>
    public string[] MediaTypes = new string[0];
    /// <summary>The supported event package tokens.</summary>
    public string[] EventPackages = new string[0];
    /// <summary>The supported extension header names.</summary>
    public string[] HeaderNames = new string[0];
    /// <summary>The supported URI scheme tokens.</summary>
    public string[] UriSchemes = new string[0];
    /// <summary>The supported transport content-coding names.</summary>
    public string[] ContentCodings = new string[0];
}
