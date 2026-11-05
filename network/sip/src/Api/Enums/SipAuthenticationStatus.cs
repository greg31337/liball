namespace sip;

/// <summary>Identifies an authentication outcome.</summary>
public enum SipAuthenticationStatus
{
    /// <summary>No outcome is available.</summary>
    None = 0,
    /// <summary>Authentication succeeded.</summary>
    Success = 1,
    /// <summary>A challenge is required.</summary>
    ChallengeRequired = 2,
    /// <summary>The credentials are invalid.</summary>
    InvalidCredentials = 3,
    /// <summary>The nonce is stale.</summary>
    StaleNonce = 4,
    /// <summary>A replay was detected.</summary>
    ReplayDetected = 5,
    /// <summary>The authentication mechanism is unsupported.</summary>
    Unsupported = 6,
    /// <summary>Authentication failed for another reason.</summary>
    Failed = 7
}
