using System;
namespace token;

/// <summary>Contains representation-neutral authenticated token data.</summary>
public class TokenValidationResult
{
    /// <summary>Indicates whether cryptographic and configured claim validation succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> on success.</summary>
    public TokenError? Error;

    /// <summary>Contains ordered validation diagnostics, or an empty array on success.</summary>
    public TokenValidationDiagnostic[] Diagnostics;

    /// <summary>Identifies the outer token representation.</summary>
    public TokenKind Kind;

    /// <summary>Identifies a nested inner representation, or <see cref="TokenKind.Unknown"/>.</summary>
    public TokenKind InnerKind;

    /// <summary>Contains authenticated JWT claims, or <c>null</c>.</summary>
    public JwtClaims? JwtClaims;

    /// <summary>Contains authenticated CWT claims, or <c>null</c>.</summary>
    public CwtClaims? CwtClaims;

    /// <summary>Contains token headers in serialization order.</summary>
    public TokenHeaderInspection[] Headers;

    /// <summary>Contains the primary signature or MAC algorithm, or <c>null</c>.</summary>
    public string? Algorithm;

    /// <summary>Contains signature algorithms in serialization order.</summary>
    public string[] Algorithms;

    /// <summary>Contains the authenticated key-management algorithm, or <c>null</c>.</summary>
    public string? KeyManagementAlgorithm;

    /// <summary>Contains the authenticated content-encryption algorithm, or <c>null</c>.</summary>
    public string? EncryptionAlgorithm;

    /// <summary>Contains the primary key identifier used for validation, or <c>null</c>.</summary>
    public string? KeyId;

    /// <summary>Contains key identifiers used for validation in operation order.</summary>
    public string[] KeyIds;

    /// <summary>Contains the authenticated token type, or <c>null</c>.</summary>
    public string? TokenType;

    /// <summary>Contains the authenticated issuer, or <c>null</c>.</summary>
    public string? Issuer;

    /// <summary>Contains the authenticated subject, or <c>null</c>.</summary>
    public string? Subject;

    /// <summary>Contains authenticated audience values.</summary>
    public string[] Audiences;

    /// <summary>Contains the authenticated expiration instant when present.</summary>
    public DateTimeOffset ExpirationTime;

    /// <summary>Indicates whether <see cref="ExpirationTime"/> is present.</summary>
    public bool HasExpirationTime;

    /// <summary>Contains the authenticated not-before instant when present.</summary>
    public DateTimeOffset NotBefore;

    /// <summary>Indicates whether <see cref="NotBefore"/> is present.</summary>
    public bool HasNotBefore;

    /// <summary>Contains the authenticated issued-at instant when present.</summary>
    public DateTimeOffset IssuedAt;

    /// <summary>Indicates whether <see cref="IssuedAt"/> is present.</summary>
    public bool HasIssuedAt;

    /// <summary>Indicates whether a signature or MAC was authenticated.</summary>
    public bool SignatureValidated;

    /// <summary>Indicates whether authenticated encryption was validated.</summary>
    public bool EncryptionValidated;

    /// <summary>Indicates whether multiple protection layers were validated.</summary>
    public bool Nested;

    /// <summary>Initializes an unsuccessful result with empty metadata.</summary>
    public TokenValidationResult()
    {
        Success = false;
        Error = null;
        Diagnostics = Array.Empty<TokenValidationDiagnostic>();
        Kind = TokenKind.Unknown;
        InnerKind = TokenKind.Unknown;
        JwtClaims = null;
        CwtClaims = null;
        Headers = Array.Empty<TokenHeaderInspection>();
        Algorithm = null;
        Algorithms = Array.Empty<string>();
        KeyManagementAlgorithm = null;
        EncryptionAlgorithm = null;
        KeyId = null;
        KeyIds = Array.Empty<string>();
        TokenType = null;
        Issuer = null;
        Subject = null;
        Audiences = Array.Empty<string>();
        ExpirationTime = DateTimeOffset.UnixEpoch;
        HasExpirationTime = false;
        NotBefore = DateTimeOffset.UnixEpoch;
        HasNotBefore = false;
        IssuedAt = DateTimeOffset.UnixEpoch;
        HasIssuedAt = false;
        SignatureValidated = false;
        EncryptionValidated = false;
        Nested = false;
    }
}
