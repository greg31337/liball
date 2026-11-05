using System;
using System.Text;

namespace token;

/// <summary>Represents a general JWS JSON serialization with multiple signatures.</summary>
public class JwsGeneralToken
{
    /// <summary>Identifies this representation as JWS JSON.</summary>
    public TokenKind Kind;

    /// <summary>Indicates whether the shared payload is omitted from the serialization.</summary>
    public bool DetachedPayload;

    /// <summary>Indicates whether all signatures use base64url payload encoding.</summary>
    public bool PayloadBase64UrlEncoded;

    /// <summary>Indicates whether every signature entry uses the unsecured <c>none</c> algorithm.</summary>
    public bool Unsecured;

    /// <summary>Contains shared payload bytes after verification or an authorized diagnostic read.</summary>
    public byte[]? Payload;

    /// <summary>Indicates whether <see cref="Payload"/> is available.</summary>
    public bool PayloadAvailable;

    /// <summary>Contains the signature entries in serialization order.</summary>
    public JwsGeneralSignature[] Signatures;

    internal byte[]? ParsedPayload;

    /// <summary>Initializes an empty, unverified general JWS model.</summary>
    public JwsGeneralToken()
    {
        Kind = TokenKind.JwsJson;
        DetachedPayload = false;
        PayloadBase64UrlEncoded = true;
        Unsecured = false;
        Payload = null;
        PayloadAvailable = false;
        Signatures = Array.Empty<JwsGeneralSignature>();
        ParsedPayload = null;
    }
}
