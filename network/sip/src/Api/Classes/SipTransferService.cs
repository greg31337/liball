namespace sip;

/// <summary>Validates and correlates transfer-related SIP header fields.</summary>
public class SipTransferService : ISipTransferService
{
    SipTransferServiceCore core;

    /// <summary>Initializes a transfer correlation service.</summary>
    public SipTransferService()
    {
        core = new SipTransferServiceCore();
    }

    /// <summary>Configures the application policy used after successful correlation.</summary>
    /// <param name="authorizationPolicy">The transfer authorization policy.</param>
    public void ConfigureTransferAuthorizationPolicy(
        ISipTransferAuthorizationPolicy authorizationPolicy)
    {
        core.ConfigureAuthorizationPolicy(authorizationPolicy);
    }

    /// <summary>Validates, correlates, and optionally authorizes one call-control request.</summary>
    /// <param name="request">The request and available dialog snapshots.</param>
    /// <returns>The validation, correlation, and authorization result.</returns>
    public SipTransferResult EvaluateTransfer(SipTransferRequest request)
    {
        return core.Evaluate(request);
    }
}
