namespace sip;

/// <summary>Holds stable identity and endpoints for a reliable connection.</summary>
public class SipConnectionInfo
{
    /// <summary>The connection identifier.</summary>
    public string? Id;
    /// <summary>The connection state.</summary>
    public SipConnectionState State;
    /// <summary>The transport kind.</summary>
    public SipTransportKind Transport;
    /// <summary>The local endpoint.</summary>
    public SipEndpointAddress? LocalAddress;
    /// <summary>The remote endpoint.</summary>
    public SipEndpointAddress? RemoteAddress;
    /// <summary>Indicates an authenticated secure connection.</summary>
    public bool Secure;
    /// <summary>The authenticated peer identity.</summary>
    public string? PeerIdentity;
    /// <summary>The peer certificate validation and identity details.</summary>
    public SipCertificateIdentity? CertificateIdentity;
}
