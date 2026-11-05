namespace sip;

/// <summary>Validates externally issued bearer tokens.</summary>
public interface ISipTokenValidator
{
    /// <summary>Validates one bearer token.</summary>
    /// <param name="token">The token and requested scope data.</param>
    /// <param name="request">The authentication request.</param>
    /// <returns>The authentication result.</returns>
    SipAuthenticationResult ValidateBearerToken(SipBearerToken token, SipAuthenticationRequest request);
}
