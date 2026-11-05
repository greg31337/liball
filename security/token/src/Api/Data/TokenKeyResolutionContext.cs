using System;
namespace token;

/// <summary>
/// Describes the token metadata available while resolving a cryptographic key.
/// </summary>
/// <remarks>
/// Header and claim values in this context are untrusted until the operation using the
/// resolved key completes successfully. They may be used to select candidate keys but must
/// not be treated as authenticated application data.
/// </remarks>
public class TokenKeyResolutionContext
{
    /// <summary>Identifies the token serialization being processed.</summary>
    public TokenKind Kind;

    /// <summary>Contains the unverified JOSE or COSE algorithm identifier.</summary>
    public string? Algorithm;

    /// <summary>Contains the unverified JWE or COSE content-encryption algorithm, or <c>null</c>.</summary>
    public string? EncryptionAlgorithm;

    /// <summary>Contains the unverified key identifier, or <c>null</c>.</summary>
    public string? KeyId;

    /// <summary>Contains the unverified COSE key identifier, or <c>null</c>.</summary>
    public byte[]? KeyIdBytes;

    /// <summary>Contains the unverified COSE algorithm label.</summary>
    public int CoseAlgorithm;

    /// <summary>Indicates whether <see cref="CoseAlgorithm"/> is present.</summary>
    public bool HasCoseAlgorithm;

    /// <summary>Contains the unverified COSE header used for key selection.</summary>
    public CoseHeader? CoseHeader;

    /// <summary>Contains the unverified token type, or <c>null</c>.</summary>
    public string? TokenType;

    /// <summary>Contains the unverified issuer claim, or <c>null</c>.</summary>
    public string? Issuer;

    /// <summary>Contains the unverified audience values.</summary>
    public string[] Audiences;

    /// <summary>Contains the requested normalized key operation.</summary>
    public string? Operation;

    /// <summary>Contains the unverified protected JOSE header.</summary>
    public JoseHeader? ProtectedHeader;

    /// <summary>Contains unverified JWT claims when they are available before key resolution.</summary>
    public JwtClaims? UnverifiedClaims;

    /// <summary>Contains unverified RFC 9597 CWT header claims when available before key resolution.</summary>
    public CwtClaims? UnverifiedCwtClaims;

    /// <summary>Initializes an empty key-resolution context.</summary>
    public TokenKeyResolutionContext()
    {
        Kind = TokenKind.Unknown;
        Algorithm = null;
        EncryptionAlgorithm = null;
        KeyId = null;
        KeyIdBytes = null;
        CoseAlgorithm = 0;
        HasCoseAlgorithm = false;
        CoseHeader = null;
        TokenType = null;
        Issuer = null;
        Audiences = Array.Empty<string>();
        Operation = null;
        ProtectedHeader = null;
        UnverifiedClaims = null;
        UnverifiedCwtClaims = null;
    }
}
