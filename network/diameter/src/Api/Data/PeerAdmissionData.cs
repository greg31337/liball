namespace diameter;

/// <summary>Contains the authenticated connection and expected Diameter peer identity.</summary>
public class DiameterPeerAdmissionRequest
{
    /// <summary>The authenticated connection metadata.</summary>
    public DiameterConnectionMetadata Connection = new DiameterConnectionMetadata();

    /// <summary>The expected Diameter host identity.</summary>
    public DiameterIdentity PeerIdentity = new DiameterIdentity();

    /// <summary>The expected Diameter realm identity.</summary>
    public DiameterIdentity PeerRealm = new DiameterIdentity();
}

/// <summary>Contains a peer or certificate admission decision.</summary>
public class DiameterPeerAdmissionResult
{
    /// <summary>Indicates that the peer is admitted.</summary>
    public bool Admitted;

    /// <summary>The stable machine-readable decision code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable decision description.</summary>
    public string Message = string.Empty;
}
