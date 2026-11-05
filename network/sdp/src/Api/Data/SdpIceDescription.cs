using System.Collections.Generic;

namespace sdp;

/// <summary>Collects effective ICE signaling for one transport scope.</summary>
public class SdpIceDescription
{
    /// <summary>Contains effective ICE credentials when present.</summary>
    public SdpIceCredentials? Credentials = null;
    /// <summary>Indicates that the endpoint is ICE-lite.</summary>
    public bool IceLite = false;
    /// <summary>Indicates that the default destination does not match ICE candidates.</summary>
    public bool IceMismatch = false;
    /// <summary>Indicates that end-of-candidates was signaled.</summary>
    public bool EndOfCandidates = false;
    /// <summary>Contains ICE option tokens in source order.</summary>
    public List<string> Options = new List<string>();
    /// <summary>Contains candidates in source order.</summary>
    public List<SdpIceCandidate> Candidates = new List<SdpIceCandidate>();
    /// <summary>Contains signaled remote candidates in source order.</summary>
    public List<SdpRemoteCandidate> RemoteCandidates = new List<SdpRemoteCandidate>();
}
