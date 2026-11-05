namespace sip;

/// <summary>Holds parsed and negotiated SIP capability information.</summary>
public class SipCapabilityNegotiation
{
    /// <summary>Indicates that the message is compatible with local capabilities.</summary>
    public bool Compatible;
    /// <summary>The protocol status corresponding to the compatibility result.</summary>
    public int StatusCode;
    /// <summary>The normalized capabilities advertised by the remote entity.</summary>
    public SipCapabilitySet? RemoteCapabilities;
    /// <summary>The mutually supported methods, option tags, media types, and events.</summary>
    public SipCapabilitySet? NegotiatedCapabilities;
    /// <summary>The end-system option tags required by the message.</summary>
    public string[] RequiredOptionTags = new string[0];
    /// <summary>The proxy-sensitive option tags required by the message.</summary>
    public string[] ProxyRequiredOptionTags = new string[0];
    /// <summary>The option tags reported as unsupported or found to be unsupported.</summary>
    public string[] UnsupportedOptionTags = new string[0];
    /// <summary>The caller preferences carried by the message.</summary>
    public SipCallerPreferences? CallerPreferences;
    /// <summary>The ordered Feature-Caps values carried by the message.</summary>
    public SipExtensionHeaderValue[] FeatureCapabilities =
        new SipExtensionHeaderValue[0];
    /// <summary>The failure description, or null when inspection succeeded.</summary>
    public string? ErrorMessage;
}
