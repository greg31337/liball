namespace diameter;

/// <summary>Decides whether an authenticated transport peer may enter Diameter processing.</summary>
public interface IPeerAdmissionPolicy
{
    /// <summary>Evaluates one authenticated peer admission request.</summary>
    /// <param name="request">The connection and expected Diameter identity.</param>
    /// <returns>The peer admission decision.</returns>
    DiameterPeerAdmissionResult EvaluatePeerAdmission(
        DiameterPeerAdmissionRequest request);
}

/// <summary>Authorizes a peer certificate for an expected Diameter identity and realm.</summary>
public interface ICertificatePeerPolicy
{
    /// <summary>Evaluates certificate binding for one authenticated peer.</summary>
    /// <param name="request">The certificate metadata and expected Diameter identity.</param>
    /// <returns>The certificate authorization decision.</returns>
    DiameterPeerAdmissionResult AuthorizeCertificatePeer(
        DiameterPeerAdmissionRequest request);
}
