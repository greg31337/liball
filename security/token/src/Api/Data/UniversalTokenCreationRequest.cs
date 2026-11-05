using System;
namespace token;

/// <summary>Describes one representation-neutral token creation operation.</summary>
/// <remarks>
/// <para>
/// For JWS, protected headers and keys correspond by index; one entry creates compact or
/// flattened output and multiple entries create general JSON output. For JWE JSON, the first
/// protected header is shared and unprotected headers correspond to recipient keys.
/// </para>
/// <para>
/// A nested compact JWT uses two protected headers and keys ordered signing then encryption.
/// COSE creation uses <see cref="CoseKind"/>, <see cref="CoseHeader"/>, participant headers,
/// and keys. CWT creation uses <see cref="CwtClaims"/> and protection layers.
/// </para>
/// </remarks>
public class UniversalTokenCreationRequest
{
    /// <summary>Identifies the requested outer representation.</summary>
    public TokenKind Kind;

    /// <summary>Identifies the exact COSE message shape when <see cref="Kind"/> is COSE.</summary>
    public CoseMessageKind CoseKind;

    /// <summary>Contains a raw JOSE or COSE payload when no claim-set model is supplied.</summary>
    public byte[]? Payload;

    /// <summary>Contains JWT claims for compact JWT creation.</summary>
    public JwtClaims? JwtClaims;

    /// <summary>Contains CWT claims for protected CWT creation.</summary>
    public CwtClaims? CwtClaims;

    /// <summary>Contains JOSE protected headers in signing or nesting order.</summary>
    public JoseHeader[] JoseProtectedHeaders;

    /// <summary>Contains optional JWS unprotected or JWE recipient headers.</summary>
    public JoseHeader?[] JoseUnprotectedHeaders;

    /// <summary>Contains an optional shared unprotected JWE JSON header.</summary>
    public JoseHeader? JoseSharedUnprotectedHeader;

    /// <summary>Contains the message-level COSE header.</summary>
    public CoseHeader? CoseHeader;

    /// <summary>Contains COSE signer or recipient headers in participant order.</summary>
    public CoseHeader[] CoseParticipantHeaders;

    /// <summary>Contains signing, encryption, MAC, or recipient keys in operation order.</summary>
    public TokenKey[] Keys;

    /// <summary>Contains optional static ECDH sender keys in recipient order.</summary>
    public TokenKey[]? SenderKeys;

    /// <summary>Contains CWT protection layers ordered innermost to outermost.</summary>
    public CwtProtectionLayer[] CwtProtectionLayers;

    /// <summary>Contains the JWE content-encryption algorithm when headers do not supply it.</summary>
    public string? ContentEncryptionAlgorithm;

    /// <summary>Contains external authenticated data for JWE JSON or COSE creation.</summary>
    public byte[] ExternalAuthenticatedData;

    /// <summary>Indicates whether supported JWS or COSE content is transported separately.</summary>
    public bool Detached;

    /// <summary>Indicates whether COSE or CWT output includes its registered CBOR tag.</summary>
    public bool Tagged;

    /// <summary>Indicates that compact JWT creation signs and then encrypts the claims.</summary>
    public bool Nested;

    /// <summary>Allows deprecated polymorphic COSE signature labels.</summary>
    public bool AllowCompatibility;

    /// <summary>Initializes an empty request for an unknown representation.</summary>
    public UniversalTokenCreationRequest()
    {
        Kind = TokenKind.Unknown;
        CoseKind = CoseMessageKind.Unknown;
        Payload = null;
        JwtClaims = null;
        CwtClaims = null;
        JoseProtectedHeaders = Array.Empty<JoseHeader>();
        JoseUnprotectedHeaders = Array.Empty<JoseHeader?>();
        JoseSharedUnprotectedHeader = null;
        CoseHeader = null;
        CoseParticipantHeaders = Array.Empty<CoseHeader>();
        Keys = Array.Empty<TokenKey>();
        SenderKeys = null;
        CwtProtectionLayers = Array.Empty<CwtProtectionLayer>();
        ContentEncryptionAlgorithm = null;
        ExternalAuthenticatedData = Array.Empty<byte>();
        Detached = false;
        Tagged = false;
        Nested = false;
        AllowCompatibility = false;
    }
}
