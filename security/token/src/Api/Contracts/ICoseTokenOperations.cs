using System;
namespace token;

/// <summary>Provides COSE structure and detached-content operations.</summary>
public interface ICoseTokenOperations
{
    /// <summary>Reads protected and unprotected COSE header buckets.</summary>
    /// <param name="cbor">The encoded two-item COSE Headers array.</param>
    /// <param name="policy">The token size and CBOR depth limits.</param>
    /// <returns>The parsed header, including its exact protected bytes, or stable failure details.</returns>
    CoseHeaderResult ReadCoseHeader(byte[] cbor, TokenPolicy policy);

    /// <summary>Writes protected and unprotected COSE header buckets deterministically.</summary>
    /// <param name="header">The header buckets to write.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded two-item COSE Headers array, or stable failure details.</returns>
    TokenBytesResult WriteCoseHeader(CoseHeader header, TokenPolicy policy);

    /// <summary>Returns protected map bytes while preserving an exact parsed encoding when it still represents the header.</summary>
    /// <param name="header">The header containing parsed or newly supplied parameters.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The exact or deterministically encoded protected map bytes, or stable failure details.</returns>
    TokenBytesResult GetCoseProtectedBytes(CoseHeader header, TokenPolicy policy);

    /// <summary>Validates protected critical parameters and caller-understood extension labels.</summary>
    /// <param name="header">The protected and unprotected header buckets.</param>
    /// <param name="understoodLabels">Additional integer or text labels processed by the caller.</param>
    /// <returns>A success result, or stable failure details.</returns>
    TokenResult ValidateCoseCriticalHeaders(CoseHeader header, CborItem[] understoodLabels);

    /// <summary>Detects a tagged COSE message or validates an untagged message against application context.</summary>
    /// <param name="cbor">The encoded COSE message.</param>
    /// <param name="expectedKind">The contextual kind, or <see cref="CoseMessageKind.Unknown"/> to require a registered tag.</param>
    /// <param name="policy">The token size and CBOR depth limits.</param>
    /// <returns>The detected message kind and decoded array, or stable failure details.</returns>
    CoseMessageResult DetectCoseMessage(byte[] cbor, CoseMessageKind expectedKind, TokenPolicy policy);

    /// <summary>Encodes a COSE_Sign1 Sig_structure over attached or detached payload bytes.</summary>
    /// <param name="bodyProtected">The exact protected body bytes.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="payload">The attached or separately transported payload.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded Sig_structure, or stable failure details.</returns>
    TokenBytesResult EncodeCoseSign1Structure(byte[] bodyProtected, byte[] externalAuthenticatedData, byte[] payload, TokenPolicy policy);

    /// <summary>Encodes a COSE_Sign Sig_structure over attached or detached payload bytes.</summary>
    /// <param name="bodyProtected">The exact protected body bytes.</param>
    /// <param name="signerProtected">The exact protected signer bytes.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="payload">The attached or separately transported payload.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded Sig_structure, or stable failure details.</returns>
    TokenBytesResult EncodeCoseSignStructure(byte[] bodyProtected, byte[] signerProtected, byte[] externalAuthenticatedData, byte[] payload, TokenPolicy policy);

    /// <summary>Encodes an Enc_structure with exact protected bytes and external authenticated data.</summary>
    /// <param name="context">The encryption or recipient context.</param>
    /// <param name="protectedParameters">The exact protected map bytes.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded Enc_structure, or stable failure details.</returns>
    TokenBytesResult EncodeCoseEncryptionStructure(CoseEncStructureContext context, byte[] protectedParameters, byte[] externalAuthenticatedData, TokenPolicy policy);

    /// <summary>Encodes a COSE_Mac MAC_structure over attached or detached payload bytes.</summary>
    /// <param name="protectedParameters">The exact protected map bytes.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="payload">The attached or separately transported payload.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded MAC_structure, or stable failure details.</returns>
    TokenBytesResult EncodeCoseMacStructure(byte[] protectedParameters, byte[] externalAuthenticatedData, byte[] payload, TokenPolicy policy);

    /// <summary>Encodes a COSE_Mac0 MAC_structure over attached or detached payload bytes.</summary>
    /// <param name="protectedParameters">The exact protected map bytes.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="payload">The attached or separately transported payload.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded MAC_structure, or stable failure details.</returns>
    TokenBytesResult EncodeCoseMac0Structure(byte[] protectedParameters, byte[] externalAuthenticatedData, byte[] payload, TokenPolicy policy);

    /// <summary>Encodes a full RFC 9338 Countersign_structure.</summary>
    /// <param name="bodyProtected">The exact protected target bytes.</param>
    /// <param name="signerProtected">The exact protected countersigner bytes.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="payload">The target payload, ciphertext, or signature bytes.</param>
    /// <param name="otherFields">Additional target byte strings in serialization order.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded Countersign_structure, or stable failure details.</returns>
    TokenBytesResult EncodeCoseCounterSignatureStructure(byte[] bodyProtected, byte[] signerProtected, byte[] externalAuthenticatedData, byte[] payload, byte[][] otherFields, TokenPolicy policy);

    /// <summary>Encodes an abbreviated RFC 9338 Countersign_structure.</summary>
    /// <param name="bodyProtected">The exact protected target bytes.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="payload">The target payload, ciphertext, or signature bytes.</param>
    /// <param name="otherFields">Additional target byte strings in serialization order.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded Countersign_structure, or stable failure details.</returns>
    TokenBytesResult EncodeCoseAbbreviatedCounterSignatureStructure(byte[] bodyProtected, byte[] externalAuthenticatedData, byte[] payload, byte[][] otherFields, TokenPolicy policy);

    /// <summary>Creates a full countersignature over a finalized COSE target.</summary>
    /// <param name="target">The target protected bytes and payload fields.</param>
    /// <param name="counterSignature">The countersignature headers to use.</param>
    /// <param name="key">The countersigning private key.</param>
    /// <param name="policy">The algorithm and resource controls.</param>
    /// <returns>The completed countersignature, or stable failure details.</returns>
    CoseCounterSignatureResult CreateCoseCounterSignature(
        CoseCounterSignatureTarget target,
        CoseCounterSignature counterSignature,
        TokenKey key,
        TokenPolicy policy);

    /// <summary>Encodes a size-limited COSE key-derivation context.</summary>
    /// <param name="parameters">The key-derivation context parameters.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded context, or stable failure details.</returns>
    TokenBytesResult EncodeCoseKdfContext(
        CoseKdfContextParameters parameters,
        TokenPolicy policy);

    /// <summary>Reads one standalone COSE recipient, including nested recipients.</summary>
    /// <param name="cbor">The encoded recipient array.</param>
    /// <param name="policy">The token size and CBOR depth limits.</param>
    /// <returns>The parsed recipient, or stable failure details.</returns>
    CoseRecipientResult ReadCoseRecipient(byte[] cbor, TokenPolicy policy);

    /// <summary>Writes one standalone COSE recipient, including nested recipients.</summary>
    /// <param name="recipient">The recipient to write.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The deterministic recipient array, or stable failure details.</returns>
    TokenBytesResult WriteCoseRecipient(CoseRecipient recipient, TokenPolicy policy);

    /// <summary>Reads a tagged or untagged COSE_Key.</summary>
    /// <param name="cbor">The encoded COSE_Key.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels are accepted.</param>
    /// <returns>The normalized key, or stable failure details.</returns>
    CoseKeyResult ReadCoseKey(byte[] cbor, bool allowCompatibility);

    /// <summary>Writes a deterministic untagged COSE_Key.</summary>
    /// <param name="key">The normalized key to write.</param>
    /// <returns>The encoded COSE_Key, or stable failure details.</returns>
    TokenBytesResult WriteCoseKey(TokenKey key);

    /// <summary>Creates a COSE_Encrypt message with separately transported ciphertext.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="message">The message and recipient headers.</param>
    /// <param name="recipientKeys">The recipient keys in serialization order.</param>
    /// <param name="policy">The algorithm and resource controls.</param>
    /// <returns>The encoded message and detached ciphertext, or stable failure details.</returns>
    CoseDetachedEncryptionResult EncryptDetachedCose(
        byte[] plaintext,
        CoseEncrypt message,
        TokenKey[] recipientKeys,
        TokenPolicy policy);
}
