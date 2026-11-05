using System;
using System.Collections.Generic;

namespace token;

/// <summary>
/// Contains the authenticated result of JWT validation.
/// </summary>
public class JwtValidationResult
{
    /// <summary>Indicates whether every requested validation succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when validation succeeded.</summary>
    public TokenError? Error;

    /// <summary>Identifies the outer token serialization.</summary>
    public TokenKind Kind;

    /// <summary>Identifies the inner token serialization, or <see cref="TokenKind.Unknown"/> for a single layer.</summary>
    public TokenKind InnerKind;

    /// <summary>Contains authenticated JWT claims, or <c>null</c> when validation failed.</summary>
    public JwtClaims? Claims;

    /// <summary>Contains the authenticated protected header for a single-layer token or outer layer.</summary>
    public JoseHeader? ProtectedHeader;

    /// <summary>Contains the authenticated inner protected header for a nested token.</summary>
    public JoseHeader? InnerProtectedHeader;

    /// <summary>Contains the authenticated JWS algorithm, or <c>null</c> when no signature layer is present.</summary>
    public string? Algorithm;

    /// <summary>Contains the authenticated JWE key-management algorithm, or <c>null</c>.</summary>
    public string? KeyManagementAlgorithm;

    /// <summary>Contains the authenticated JWE content-encryption algorithm, or <c>null</c>.</summary>
    public string? EncryptionAlgorithm;

    /// <summary>Contains the authenticated token type, or <c>null</c> when absent.</summary>
    public string? TokenType;

    /// <summary>Contains the authenticated signing key identifier, or <c>null</c>.</summary>
    public string? SigningKeyId;

    /// <summary>Contains the authenticated encryption key identifier, or <c>null</c>.</summary>
    public string? EncryptionKeyId;

    /// <summary>Indicates whether a signature or MAC was cryptographically validated.</summary>
    public bool SignatureValidated;

    /// <summary>Indicates whether authenticated encryption was cryptographically validated.</summary>
    public bool EncryptionValidated;

    /// <summary>Indicates whether the result represents multiple JOSE layers.</summary>
    public bool Nested;

    /// <summary>Initializes an unsuccessful, empty JWT validation result.</summary>
    public JwtValidationResult()
    {
        Success = false;
        Error = null;
        Kind = TokenKind.Unknown;
        InnerKind = TokenKind.Unknown;
        Claims = null;
        ProtectedHeader = null;
        InnerProtectedHeader = null;
        Algorithm = null;
        KeyManagementAlgorithm = null;
        EncryptionAlgorithm = null;
        TokenType = null;
        SigningKeyId = null;
        EncryptionKeyId = null;
        SignatureValidated = false;
        EncryptionValidated = false;
        Nested = false;
    }
}
