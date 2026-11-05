namespace sip;

/// <summary>Holds loss-preserving SIP Identity data for application verification.</summary>
public class SipIdentityVerificationRequest
{
    /// <summary>The detached SIP request whose signaling identity is authoritative.</summary>
    public SipMessage? Message;
    /// <summary>The detached identity-related metadata.</summary>
    public SipIdentityMetadata? Metadata;
    /// <summary>The raw Identity and legacy Identity-Info fields in wire order.</summary>
    public SipHeader[] IdentityHeaders = new SipHeader[0];
    /// <summary>The candidate full or compact PASSporT values.</summary>
    public string[] Passports = new string[0];
}
