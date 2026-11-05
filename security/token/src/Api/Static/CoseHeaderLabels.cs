using System;
namespace token;

/// <summary>
/// Provides registered COSE header and algorithm-header parameter labels.
/// </summary>
public static class CoseHeaderLabels
{
    /// <summary>The algorithm header parameter.</summary>
    public const int Algorithm = 1;

    /// <summary>The critical headers parameter.</summary>
    public const int Critical = 2;

    /// <summary>The content type parameter.</summary>
    public const int ContentType = 3;

    /// <summary>The key identifier parameter.</summary>
    public const int KeyId = 4;

    /// <summary>The full initialization vector parameter.</summary>
    public const int InitializationVector = 5;

    /// <summary>The partial initialization vector parameter.</summary>
    public const int PartialInitializationVector = 6;

    /// <summary>The original countersignature parameter.</summary>
    public const int CounterSignature = 7;

    /// <summary>The original compact countersignature parameter.</summary>
    public const int CounterSignature0 = 9;

    /// <summary>The key identifier context parameter.</summary>
    public const int KeyIdContext = 10;

    /// <summary>The version 2 countersignature parameter.</summary>
    public const int CounterSignatureV2 = 11;

    /// <summary>The version 2 compact countersignature parameter.</summary>
    public const int CounterSignature0V2 = 12;

    /// <summary>The CWT-form proof-of-possession key parameter.</summary>
    public const int KeyCwt = 13;

    /// <summary>The CCS-form proof-of-possession key parameter.</summary>
    public const int KeyCcs = 14;

    /// <summary>The CWT claims header parameter.</summary>
    public const int CwtClaims = 15;

    /// <summary>The COSE object type parameter.</summary>
    public const int Type = 16;

    /// <summary>The selective-disclosure claims parameter.</summary>
    public const int SelectiveDisclosureClaims = 17;

    /// <summary>The C509 certificate thumbprint parameter.</summary>
    public const int C509Thumbprint = 22;

    /// <summary>The C509 certificate URI parameter.</summary>
    public const int C509Uri = 23;

    /// <summary>The C509 certificate bundle parameter.</summary>
    public const int C509Bundle = 24;

    /// <summary>The C509 certificate chain parameter.</summary>
    public const int C509Chain = 25;

    /// <summary>The X.509 certificate bag parameter.</summary>
    public const int X509Bag = 32;

    /// <summary>The X.509 certificate chain parameter.</summary>
    public const int X509Chain = 33;

    /// <summary>The X.509 certificate thumbprint parameter.</summary>
    public const int X509Thumbprint = 34;

    /// <summary>The X.509 certificate URI parameter.</summary>
    public const int X509Uri = 35;

    /// <summary>The selective-disclosure algorithm parameter.</summary>
    public const int SelectiveDisclosureAlgorithm = 170;

    /// <summary>The encrypted selective-disclosure claims parameter.</summary>
    public const int SelectiveDisclosureEncryptedClaims = 171;

    /// <summary>The selective-disclosure authenticated-encryption parameter.</summary>
    public const int SelectiveDisclosureAuthenticatedEncryption = 172;

    /// <summary>The COSE unprotected-header nonce parameter.</summary>
    public const int CuphNonce = 256;

    /// <summary>The COSE unprotected-header owner public key parameter.</summary>
    public const int CuphOwnerPublicKey = 257;

    /// <summary>The payload hash algorithm parameter.</summary>
    public const int PayloadHashAlgorithm = 258;

    /// <summary>The preimage content type parameter.</summary>
    public const int PreimageContentType = 259;

    /// <summary>The detached payload location parameter.</summary>
    public const int PayloadLocation = 260;

    /// <summary>The signed X.509 timestamp parameter.</summary>
    public const int X509Timestamp = 261;

    /// <summary>The CMS signature reference parameter.</summary>
    public const int CmsSignatureReference = 262;

    /// <summary>The signature policy parameter.</summary>
    public const int SignaturePolicy = 263;

    /// <summary>The archive timestamp reference parameter.</summary>
    public const int ArchiveTimestampReference = 264;

    /// <summary>The application data object timestamp parameter.</summary>
    public const int ApplicationDataObjectTimestamp = 265;

    /// <summary>The signature policy identifier parameter.</summary>
    public const int SignaturePolicyIdentifier = 266;

    /// <summary>The signature digest parameter.</summary>
    public const int SignatureDigest = 267;

    /// <summary>The unprotected headers parameter.</summary>
    public const int UnprotectedHeaders = 268;

    /// <summary>The RFC 3161 time-stamp token content parameter.</summary>
    public const int Rfc3161TimestampContent = 269;

    /// <summary>The RFC 3161 content time-stamp parameter.</summary>
    public const int Rfc3161ContentTimestamp = 270;

    /// <summary>The receipts parameter.</summary>
    public const int Receipts = 394;

    /// <summary>The verifiable data structure parameter.</summary>
    public const int VerifiableDataStructure = 395;

    /// <summary>The verifiable data proof parameter.</summary>
    public const int VerifiableDataProof = 396;

    /// <summary>The ephemeral key algorithm-header parameter.</summary>
    public const int EphemeralKey = -1;

    /// <summary>The static key algorithm-header parameter.</summary>
    public const int StaticKey = -2;

    /// <summary>The static key identifier algorithm-header parameter.</summary>
    public const int StaticKeyId = -3;

    /// <summary>The PartyU identity algorithm-header parameter.</summary>
    public const int PartyUIdentity = -21;

    /// <summary>The PartyU nonce algorithm-header parameter.</summary>
    public const int PartyUNonce = -22;

    /// <summary>The PartyU other information algorithm-header parameter.</summary>
    public const int PartyUOther = -23;

    /// <summary>The PartyV identity algorithm-header parameter.</summary>
    public const int PartyVIdentity = -24;

    /// <summary>The PartyV nonce algorithm-header parameter.</summary>
    public const int PartyVNonce = -25;

    /// <summary>The PartyV other information algorithm-header parameter.</summary>
    public const int PartyVOther = -26;

    /// <summary>The sender X.509 certificate thumbprint algorithm-header parameter.</summary>
    public const int SenderX509Thumbprint = -27;

    /// <summary>The sender X.509 certificate URI algorithm-header parameter.</summary>
    public const int SenderX509Uri = -28;

    /// <summary>The sender X.509 certificate chain algorithm-header parameter.</summary>
    public const int SenderX509Chain = -29;

    /// <summary>The salt algorithm-header parameter.</summary>
    public const int Salt = -20;
}
