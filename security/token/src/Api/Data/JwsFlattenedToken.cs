using System;
using System.Text;

namespace token;

/// <summary>Represents one flattened JWS JSON serialization.</summary>
public class JwsFlattenedToken
{
    /// <summary>Identifies this representation as JWS JSON.</summary>
    public TokenKind Kind;

    /// <summary>Contains the integrity-protected JOSE header.</summary>
    public JoseHeader? ProtectedHeader;

    /// <summary>Contains the unprotected JOSE header, or <c>null</c> when absent.</summary>
    public JoseHeader? UnprotectedHeader;

    /// <summary>Contains the protected JWS algorithm identifier.</summary>
    public string? Algorithm;

    /// <summary>Contains registry metadata for <see cref="Algorithm"/>.</summary>
    public AlgorithmMetadata? AlgorithmMetadata;

    /// <summary>Indicates whether the algorithm has a usable built-in implementation.</summary>
    public bool AlgorithmAvailable;

    /// <summary>Contains the key identifier from either header, or <c>null</c> when absent.</summary>
    public string? KeyId;

    /// <summary>Indicates whether the payload is omitted from the JSON serialization.</summary>
    public bool DetachedPayload;

    /// <summary>Indicates whether the payload uses base64url encoding.</summary>
    public bool PayloadBase64UrlEncoded;

    /// <summary>Indicates whether the JWS uses the unsecured <c>none</c> algorithm.</summary>
    public bool Unsecured;

    /// <summary>Contains payload bytes after verification or an authorized diagnostic read.</summary>
    public byte[]? Payload;

    /// <summary>Indicates whether <see cref="Payload"/> is available.</summary>
    public bool PayloadAvailable;

    /// <summary>Contains the decoded signature or MAC bytes.</summary>
    public byte[]? Signature;

    /// <summary>Indicates whether <see cref="Signature"/> has been verified.</summary>
    public bool SignatureVerified;

    internal string? ProtectedSegment;
    internal byte[]? ParsedPayload;

    /// <summary>Initializes an empty, unverified flattened JWS model.</summary>
    public JwsFlattenedToken()
    {
        Kind = TokenKind.JwsJson;
        ProtectedHeader = null;
        UnprotectedHeader = null;
        Algorithm = null;
        AlgorithmMetadata = null;
        AlgorithmAvailable = false;
        KeyId = null;
        DetachedPayload = false;
        PayloadBase64UrlEncoded = true;
        Unsecured = false;
        Payload = null;
        PayloadAvailable = false;
        Signature = null;
        SignatureVerified = false;
        ProtectedSegment = null;
        ParsedPayload = null;
    }
}
