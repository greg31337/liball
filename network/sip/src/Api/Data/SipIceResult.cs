namespace sip;

/// <summary>Holds an ICE signaling inspection or transformation result.</summary>
public class SipIceResult
{
    /// <summary>Indicates that the signaling operation succeeded.</summary>
    public bool Success;
    /// <summary>The recommended SIP failure status, or 200 on success.</summary>
    public int StatusCode;
    /// <summary>The detached typed ICE signaling.</summary>
    public SipIceDescription? Description;
    /// <summary>The detached SDP produced by a transformation.</summary>
    public SipSessionDescription? SessionDescription;
    /// <summary>The detached SIP message produced by an option-tag transformation.</summary>
    public SipMessage? Message;
    /// <summary>The Trickle ICE INFO request prepared for the INFO framework.</summary>
    public SipInfoRequest? InfoRequest;
    /// <summary>The encoded Trickle ICE SDP fragment.</summary>
    public byte[] Fragment = new byte[0];
    /// <summary>Indicates remote support for traditional ICE signaling.</summary>
    public bool IceSupported;
    /// <summary>Indicates remote support for Trickle ICE signaling.</summary>
    public bool TrickleIceSupported;
    /// <summary>Indicates that the remote endpoint requires Trickle ICE.</summary>
    public bool TrickleIceRequired;
    /// <summary>The failure description, or null on success.</summary>
    public string? ErrorMessage;
}
