namespace sip;

/// <summary>Holds ICE signaling associated with one SDP media section.</summary>
public class SipIceMedia
{
    /// <summary>The SDP media identification tag.</summary>
    public string? Mid;
    /// <summary>The media-level ICE username fragment.</summary>
    public string? UsernameFragment;
    /// <summary>The media-level ICE password.</summary>
    public string? Password;
    /// <summary>The ordered media candidates.</summary>
    public SipIceCandidate[] Candidates = new SipIceCandidate[0];
    /// <summary>Indicates the end of candidate trickling for this media section.</summary>
    public bool EndOfCandidates;
    /// <summary>Future SDP attributes preserved with this media section.</summary>
    public SipSdpAttribute[] ExtensionAttributes = new SipSdpAttribute[0];
}
