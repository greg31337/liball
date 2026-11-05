namespace sip;

/// <summary>Holds signaling-only ICE metadata for an SDP description or fragment.</summary>
public class SipIceDescription
{
    /// <summary>The session-level ICE username fragment.</summary>
    public string? UsernameFragment;
    /// <summary>The session-level ICE password.</summary>
    public string? Password;
    /// <summary>The ordered ICE option tokens, including future options.</summary>
    public string[] Options = new string[0];
    /// <summary>Indicates ICE-lite signaling.</summary>
    public bool IceLite;
    /// <summary>Indicates the end of candidate trickling for the whole session.</summary>
    public bool EndOfCandidates;
    /// <summary>The ordered media ICE sections.</summary>
    public SipIceMedia[] Media = new SipIceMedia[0];
    /// <summary>Future SDP attributes preserved at session level.</summary>
    public SipSdpAttribute[] ExtensionAttributes = new SipSdpAttribute[0];
}
