using System;
namespace token;

/// <summary>
/// Provides registered COSE header and algorithm-header parameter labels.
/// </summary>
internal static class CoseHeaderLabelsOperations
{
    /// <summary>The algorithm header parameter.</summary>
    internal const int Algorithm = CoseHeaderLabels.Algorithm;
    /// <summary>The critical headers parameter.</summary>
    internal const int Critical = CoseHeaderLabels.Critical;
    /// <summary>The content type parameter.</summary>
    internal const int ContentType = CoseHeaderLabels.ContentType;
    /// <summary>The key identifier parameter.</summary>
    internal const int KeyId = CoseHeaderLabels.KeyId;
    /// <summary>The full initialization vector parameter.</summary>
    internal const int InitializationVector = CoseHeaderLabels.InitializationVector;
    /// <summary>The partial initialization vector parameter.</summary>
    internal const int PartialInitializationVector = CoseHeaderLabels.PartialInitializationVector;
    /// <summary>The original countersignature parameter.</summary>
    internal const int CounterSignature = CoseHeaderLabels.CounterSignature;
    /// <summary>The original compact countersignature parameter.</summary>
    internal const int CounterSignature0 = CoseHeaderLabels.CounterSignature0;
    /// <summary>The key identifier context parameter.</summary>
    internal const int KeyIdContext = CoseHeaderLabels.KeyIdContext;
    /// <summary>The version 2 countersignature parameter.</summary>
    internal const int CounterSignatureV2 = CoseHeaderLabels.CounterSignatureV2;
    /// <summary>The version 2 compact countersignature parameter.</summary>
    internal const int CounterSignature0V2 = CoseHeaderLabels.CounterSignature0V2;
    /// <summary>The CWT-form proof-of-possession key parameter.</summary>
    internal const int KeyCwt = CoseHeaderLabels.KeyCwt;
    /// <summary>The CCS-form proof-of-possession key parameter.</summary>
    internal const int KeyCcs = CoseHeaderLabels.KeyCcs;
    /// <summary>The CWT claims header parameter.</summary>
    internal const int CwtClaims = CoseHeaderLabels.CwtClaims;
    /// <summary>The COSE object type parameter.</summary>
    internal const int Type = CoseHeaderLabels.Type;
    /// <summary>The selective-disclosure claims parameter.</summary>
    internal const int SelectiveDisclosureClaims = CoseHeaderLabels.SelectiveDisclosureClaims;
    /// <summary>The C509 certificate thumbprint parameter.</summary>
    internal const int C509Thumbprint = CoseHeaderLabels.C509Thumbprint;
    /// <summary>The C509 certificate URI parameter.</summary>
    internal const int C509Uri = CoseHeaderLabels.C509Uri;
    /// <summary>The C509 certificate bundle parameter.</summary>
    internal const int C509Bundle = CoseHeaderLabels.C509Bundle;
    /// <summary>The C509 certificate chain parameter.</summary>
    internal const int C509Chain = CoseHeaderLabels.C509Chain;
    /// <summary>The X.509 certificate bag parameter.</summary>
    internal const int X509Bag = CoseHeaderLabels.X509Bag;
    /// <summary>The X.509 certificate chain parameter.</summary>
    internal const int X509Chain = CoseHeaderLabels.X509Chain;
    /// <summary>The X.509 certificate thumbprint parameter.</summary>
    internal const int X509Thumbprint = CoseHeaderLabels.X509Thumbprint;
    /// <summary>The X.509 certificate URI parameter.</summary>
    internal const int X509Uri = CoseHeaderLabels.X509Uri;
    /// <summary>The selective-disclosure algorithm parameter.</summary>
    internal const int SelectiveDisclosureAlgorithm = CoseHeaderLabels.SelectiveDisclosureAlgorithm;
    /// <summary>The encrypted selective-disclosure claims parameter.</summary>
    internal const int SelectiveDisclosureEncryptedClaims = CoseHeaderLabels.SelectiveDisclosureEncryptedClaims;
    /// <summary>The selective-disclosure authenticated-encryption parameter.</summary>
    internal const int SelectiveDisclosureAuthenticatedEncryption = CoseHeaderLabels.SelectiveDisclosureAuthenticatedEncryption;
    /// <summary>The COSE unprotected-header nonce parameter.</summary>
    internal const int CuphNonce = CoseHeaderLabels.CuphNonce;
    /// <summary>The COSE unprotected-header owner public key parameter.</summary>
    internal const int CuphOwnerPublicKey = CoseHeaderLabels.CuphOwnerPublicKey;
    /// <summary>The payload hash algorithm parameter.</summary>
    internal const int PayloadHashAlgorithm = CoseHeaderLabels.PayloadHashAlgorithm;
    /// <summary>The preimage content type parameter.</summary>
    internal const int PreimageContentType = CoseHeaderLabels.PreimageContentType;
    /// <summary>The detached payload location parameter.</summary>
    internal const int PayloadLocation = CoseHeaderLabels.PayloadLocation;
    /// <summary>The signed X.509 timestamp parameter.</summary>
    internal const int X509Timestamp = CoseHeaderLabels.X509Timestamp;
    /// <summary>The CMS signature reference parameter.</summary>
    internal const int CmsSignatureReference = CoseHeaderLabels.CmsSignatureReference;
    /// <summary>The signature policy parameter.</summary>
    internal const int SignaturePolicy = CoseHeaderLabels.SignaturePolicy;
    /// <summary>The archive timestamp reference parameter.</summary>
    internal const int ArchiveTimestampReference = CoseHeaderLabels.ArchiveTimestampReference;
    /// <summary>The application data object timestamp parameter.</summary>
    internal const int ApplicationDataObjectTimestamp = CoseHeaderLabels.ApplicationDataObjectTimestamp;
    /// <summary>The signature policy identifier parameter.</summary>
    internal const int SignaturePolicyIdentifier = CoseHeaderLabels.SignaturePolicyIdentifier;
    /// <summary>The signature digest parameter.</summary>
    internal const int SignatureDigest = CoseHeaderLabels.SignatureDigest;
    /// <summary>The unprotected headers parameter.</summary>
    internal const int UnprotectedHeaders = CoseHeaderLabels.UnprotectedHeaders;
    /// <summary>The RFC 3161 time-stamp token content parameter.</summary>
    internal const int Rfc3161TimestampContent = CoseHeaderLabels.Rfc3161TimestampContent;
    /// <summary>The RFC 3161 content time-stamp parameter.</summary>
    internal const int Rfc3161ContentTimestamp = CoseHeaderLabels.Rfc3161ContentTimestamp;
    /// <summary>The receipts parameter.</summary>
    internal const int Receipts = CoseHeaderLabels.Receipts;
    /// <summary>The verifiable data structure parameter.</summary>
    internal const int VerifiableDataStructure = CoseHeaderLabels.VerifiableDataStructure;
    /// <summary>The verifiable data proof parameter.</summary>
    internal const int VerifiableDataProof = CoseHeaderLabels.VerifiableDataProof;

    /// <summary>The ephemeral key algorithm-header parameter.</summary>
    internal const int EphemeralKey = CoseHeaderLabels.EphemeralKey;
    /// <summary>The static key algorithm-header parameter.</summary>
    internal const int StaticKey = CoseHeaderLabels.StaticKey;
    /// <summary>The static key identifier algorithm-header parameter.</summary>
    internal const int StaticKeyId = CoseHeaderLabels.StaticKeyId;
    /// <summary>The PartyU identity algorithm-header parameter.</summary>
    internal const int PartyUIdentity = CoseHeaderLabels.PartyUIdentity;
    /// <summary>The PartyU nonce algorithm-header parameter.</summary>
    internal const int PartyUNonce = CoseHeaderLabels.PartyUNonce;
    /// <summary>The PartyU other information algorithm-header parameter.</summary>
    internal const int PartyUOther = CoseHeaderLabels.PartyUOther;
    /// <summary>The PartyV identity algorithm-header parameter.</summary>
    internal const int PartyVIdentity = CoseHeaderLabels.PartyVIdentity;
    /// <summary>The PartyV nonce algorithm-header parameter.</summary>
    internal const int PartyVNonce = CoseHeaderLabels.PartyVNonce;
    /// <summary>The PartyV other information algorithm-header parameter.</summary>
    internal const int PartyVOther = CoseHeaderLabels.PartyVOther;
    /// <summary>The sender X.509 certificate thumbprint algorithm-header parameter.</summary>
    internal const int SenderX509Thumbprint = CoseHeaderLabels.SenderX509Thumbprint;
    /// <summary>The sender X.509 certificate URI algorithm-header parameter.</summary>
    internal const int SenderX509Uri = CoseHeaderLabels.SenderX509Uri;
    /// <summary>The sender X.509 certificate chain algorithm-header parameter.</summary>
    internal const int SenderX509Chain = CoseHeaderLabels.SenderX509Chain;
    /// <summary>The salt algorithm-header parameter.</summary>
    internal const int Salt = CoseHeaderLabels.Salt;

    static readonly int[] headers = new int[]
    {
        Algorithm, Critical, ContentType, KeyId, InitializationVector,
        PartialInitializationVector, CounterSignature, CounterSignature0,
        KeyIdContext, CounterSignatureV2, CounterSignature0V2, KeyCwt, KeyCcs,
        CwtClaims, Type, SelectiveDisclosureClaims, C509Thumbprint, C509Uri,
        C509Bundle, C509Chain, X509Bag, X509Chain, X509Thumbprint, X509Uri,
        SelectiveDisclosureAlgorithm, SelectiveDisclosureEncryptedClaims,
        SelectiveDisclosureAuthenticatedEncryption, CuphNonce,
        CuphOwnerPublicKey, PayloadHashAlgorithm, PreimageContentType,
        PayloadLocation, X509Timestamp, CmsSignatureReference, SignaturePolicy,
        ArchiveTimestampReference, ApplicationDataObjectTimestamp,
        SignaturePolicyIdentifier, SignatureDigest, UnprotectedHeaders,
        Rfc3161TimestampContent, Rfc3161ContentTimestamp, Receipts,
        VerifiableDataStructure, VerifiableDataProof
    };

    static readonly string[] headerNames = new string[]
    {
        "alg", "crit", "content type", "kid", "IV", "Partial IV",
        "counter signature", "CounterSignature0", "kid context",
        "Countersignature version 2", "Countersignature0 version 2", "kcwt",
        "kccs", "CWT Claims", "typ (type)", "sd_claims", "c5t", "c5u",
        "c5b", "c5c", "x5bag", "x5chain", "x5t", "x5u", "sd_alg",
        "sd_aead_encrypted_claims", "sd_aead", "CUPHNonce", "CUPHOwnerPubKey",
        "payload-hash-alg", "preimage-content-type", "payload-location", "x5ts",
        "srCms", "sigPl", "srAts", "adoTst", "sigPId", "sigD", "uHeaders",
        "3161-ttc", "3161-ctt", "receipts", "vds", "vdp"
    };

    static readonly int[] algorithmParameters = new int[]
    {
        SenderX509Chain, SenderX509Uri, SenderX509Thumbprint, PartyVOther,
        PartyVNonce, PartyVIdentity, PartyUOther, PartyUNonce, PartyUIdentity,
        Salt, StaticKeyId, StaticKey, EphemeralKey
    };

    static readonly string[] algorithmParameterNames = new string[]
    {
        "x5chain-sender", "x5u-sender", "x5t-sender", "PartyV other",
        "PartyV nonce", "PartyV identity", "PartyU other", "PartyU nonce",
        "PartyU identity", "salt", "static key id", "static key", "ephemeral key"
    };

    /// <summary>Determines whether a label is a registered general COSE header parameter.</summary>
    /// <param name="label">The integer header label.</param>
    /// <returns><c>true</c> when the label is registered; otherwise, <c>false</c>.</returns>
    internal static bool IsRegistered(int label)
    {
        return RegistryLookup.Contains(headers, label);
    }

    /// <summary>Determines whether a label is a registered algorithm-specific header parameter.</summary>
    /// <param name="label">The integer header label.</param>
    /// <returns><c>true</c> when the label is algorithm-specific; otherwise, <c>false</c>.</returns>
    internal static bool IsAlgorithmParameter(int label)
    {
        return RegistryLookup.Contains(algorithmParameters, label);
    }

    /// <summary>Gets the registered general header name for a label.</summary>
    /// <param name="label">The integer header label.</param>
    /// <returns>The registered name, or <c>null</c> when the label is unknown.</returns>
    internal static string? GetName(int label)
    {
        int index = RegistryLookup.IndexOf(headers, label);
        if (index < 0)
        {
            return null;
        }

        return headerNames[index];
    }

    /// <summary>Gets the registered algorithm-header parameter name for a label.</summary>
    /// <param name="label">The integer header label.</param>
    /// <returns>The registered name, or <c>null</c> when the label is unknown.</returns>
    internal static string? GetAlgorithmParameterName(int label)
    {
        int index = RegistryLookup.IndexOf(algorithmParameters, label);
        if (index < 0)
        {
            return null;
        }

        return algorithmParameterNames[index];
    }

    /// <summary>Returns all registered general COSE header labels.</summary>
    /// <returns>A new array containing the labels.</returns>
    internal static int[] GetAll()
    {
        return RegistryLookup.Copy(headers);
    }

    /// <summary>Returns all registered algorithm-specific COSE header labels.</summary>
    /// <returns>A new array containing the labels.</returns>
    internal static int[] GetAllAlgorithmParameters()
    {
        return RegistryLookup.Copy(algorithmParameters);
    }
}
