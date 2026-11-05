namespace sip;

/// <summary>Identifies externally managed SigComp state associated with one remote application.</summary>
public class SipSigCompCompartment
{
    /// <summary>The opaque application-selected compartment identifier.</summary>
    public string? Id;
    /// <summary>The remote SigComp capabilities associated with the compartment.</summary>
    public SipSigCompCapability? RemoteCapability;
    /// <summary>The remote transport endpoint associated with the compartment.</summary>
    public SipEndpointAddress? RemoteAddress;
    /// <summary>The transport associated with the compartment.</summary>
    public SipTransportKind Transport;
    /// <summary>The reliable connection identifier associated with the compartment.</summary>
    public string? ConnectionId;
    /// <summary>The outbound flow identifier associated with the compartment.</summary>
    public string? FlowId;
    /// <summary>Indicates that the application has ended the compartment.</summary>
    public bool Closed;
}
