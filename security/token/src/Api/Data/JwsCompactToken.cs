using System;
namespace token;

/// <summary>
/// Represents a parsed compact JWS without implying signature verification.
/// </summary>
public class JwsCompactToken
{
    /// <summary>Identifies this representation as compact JWS.</summary>
    public TokenKind Kind;

    /// <summary>Contains the protected JOSE header, authenticated only when <see cref="SignatureVerified"/> is true.</summary>
    public JoseHeader? ProtectedHeader;

    /// <summary>Contains the JWS algorithm identifier, authenticated only when <see cref="SignatureVerified"/> is true.</summary>
    public string? Algorithm;

    /// <summary>Contains the key identifier, authenticated only when <see cref="SignatureVerified"/> is true, or <c>null</c> when absent.</summary>
    public string? KeyId;

    /// <summary>Contains registry metadata for <see cref="Algorithm"/>.</summary>
    public AlgorithmMetadata? AlgorithmMetadata;

    /// <summary>Indicates whether the algorithm has a usable built-in cryptographic implementation.</summary>
    public bool AlgorithmAvailable;

    /// <summary>Contains payload bytes after verification or a policy-authorized diagnostic read.</summary>
    public byte[]? Payload;

    /// <summary>Indicates whether <see cref="Payload"/> is available to the caller.</summary>
    public bool PayloadAvailable;

    /// <summary>Indicates whether the serialized payload uses base64url encoding.</summary>
    public bool PayloadBase64UrlEncoded;

    /// <summary>Indicates whether the JWS uses the unsecured <c>none</c> algorithm.</summary>
    public bool Unsecured;

    /// <summary>Contains the decoded JWS signature or MAC bytes.</summary>
    public byte[]? Signature;

    /// <summary>Indicates whether the signature has been cryptographically verified.</summary>
    public bool SignatureVerified;

    internal string? ProtectedSegment;
    internal string? PayloadSegment;
    internal byte[]? ParsedPayload;

    /// <summary>Initializes an empty, unverified compact JWS model.</summary>
    public JwsCompactToken()
    {
        Kind = TokenKind.JwsCompact;
        ProtectedHeader = null;
        Algorithm = null;
        KeyId = null;
        AlgorithmMetadata = null;
        AlgorithmAvailable = false;
        Payload = null;
        PayloadAvailable = false;
        PayloadBase64UrlEncoded = true;
        Unsecured = false;
        Signature = null;
        SignatureVerified = false;
        ProtectedSegment = null;
        PayloadSegment = null;
        ParsedPayload = null;
    }
}
