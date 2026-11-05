namespace sip;

/// <summary>Validates and correlates transfer-related SIP header fields.</summary>
public interface ISipTransferService
{
    /// <summary>Configures the application policy used after successful correlation.</summary>
    /// <param name="authorizationPolicy">The transfer authorization policy.</param>
    void ConfigureTransferAuthorizationPolicy(
        ISipTransferAuthorizationPolicy authorizationPolicy);
    /// <summary>Validates, correlates, and optionally authorizes one call-control request.</summary>
    /// <param name="request">The request and available dialog snapshots.</param>
    /// <returns>The validation, correlation, and authorization result.</returns>
    SipTransferResult EvaluateTransfer(SipTransferRequest request);
}
