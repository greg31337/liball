namespace sip;

/// <summary>Holds per-dialog RFC 6086 package negotiation state.</summary>
public class SipInfoNegotiation
{
    /// <summary>The associated dialog identifier.</summary>
    public SipDialogId? DialogId;
    /// <summary>The locally registered and advertised packages.</summary>
    public string[] LocalPackages = new string[0];
    /// <summary>The packages the remote endpoint is willing to receive.</summary>
    public string[] RemotePackages = new string[0];
    /// <summary>Indicates that a remote Recv-Info advertisement was observed.</summary>
    public bool RemoteSupportsInfoPackages;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
