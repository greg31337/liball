using System;
using System.Security.Cryptography;

namespace token;

/// <summary>
/// Contains an authenticated DPoP proof and its embedded public-key binding.
/// </summary>
/// <remarks>
/// On success, <see cref="PublicKey"/> owns native key material parsed from the proof. The caller
/// should call <see cref="TokenKey.ReleaseOwnedMaterial"/> when that key is no longer needed.
/// </remarks>
public class DpopProofValidationResult
{
    /// <summary>Indicates whether every DPoP proof check succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when validation succeeded.</summary>
    public TokenError? Error;

    /// <summary>Contains the authenticated JWT validation result, or <c>null</c>.</summary>
    public JwtValidationResult? Jwt;

    /// <summary>Contains the authenticated embedded public key, or <c>null</c>.</summary>
    public TokenKey? PublicKey;

    /// <summary>Contains the base64url JWK SHA-256 thumbprint, or <c>null</c>.</summary>
    public string? JwkThumbprint;

    /// <summary>Initializes an unsuccessful, empty DPoP validation result.</summary>
    public DpopProofValidationResult()
    {
        Success = false;
        Error = null;
        Jwt = null;
        PublicKey = null;
        JwkThumbprint = null;
    }
}
