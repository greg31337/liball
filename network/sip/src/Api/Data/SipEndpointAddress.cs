namespace sip;

/// <summary>Identifies a network endpoint and transport.</summary>
public class SipEndpointAddress
{
    /// <summary>The host name or numeric address.</summary>
    public string? Host;
    /// <summary>The port, or negative one when unspecified.</summary>
    public int Port = -1;
    /// <summary>The selected transport.</summary>
    public SipTransportKind Transport;
    /// <summary>Indicates that secure transport is required.</summary>
    public bool Secure;
}
