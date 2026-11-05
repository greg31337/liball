namespace fake;

/// <summary>
/// Represents an explicitly synthetic, unsecured JWT-shaped fixture and its correlated claims.
/// </summary>
/// <remarks>
/// The token uses the <c>none</c> algorithm and has no signature. It is not authenticated
/// and must never be used as a credential or authorization artifact.
/// </remarks>
public class JwtRecord
{
    /// <summary>The complete three-segment JWT-shaped value, including an empty signature segment.</summary>
    public string Token = string.Empty;

    /// <summary>The decoded synthetic JOSE header JSON.</summary>
    public string HeaderJson = string.Empty;

    /// <summary>The decoded synthetic claims JSON.</summary>
    public string PayloadJson = string.Empty;

    /// <summary>The JOSE algorithm label, always <c>none</c> for generated fixtures.</summary>
    public string Algorithm = string.Empty;

    /// <summary>The JOSE type label, always <c>JWT</c>.</summary>
    public string Type = string.Empty;

    /// <summary>The issuer claim using a reserved example domain.</summary>
    public string Issuer = string.Empty;

    /// <summary>The synthetic subject claim.</summary>
    public string Subject = string.Empty;

    /// <summary>The audience claim using a reserved example domain.</summary>
    public string Audience = string.Empty;

    /// <summary>The issued-at claim as Unix seconds from the captured reference time.</summary>
    public long IssuedAtUnixSeconds;

    /// <summary>The not-before claim as Unix seconds, correlated to the issued-at claim.</summary>
    public long NotBeforeUnixSeconds;

    /// <summary>The expiry claim as Unix seconds, correlated to the requested lifetime.</summary>
    public long ExpiresAtUnixSeconds;

    /// <summary>The deterministic synthetic JWT identifier claim.</summary>
    public string JwtId = string.Empty;

    /// <summary>Whether cryptographic signature material is present, always false.</summary>
    public bool IsSigned;

    /// <summary>Whether the token is authenticated, always false.</summary>
    public bool IsAuthenticated;

    /// <summary>Whether the token is explicitly synthetic, always true.</summary>
    public bool IsSynthetic;

    internal JwtRecord()
    {
    }
}
