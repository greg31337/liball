using System;
namespace token;

/// <summary>Provides foundational COSE header, structure, recipient, and key operations.</summary>
public class CoseTokenService : ICoseTokenOperations
{
    /// <summary>Initializes a stateless COSE token service.</summary>
    public CoseTokenService()
    {
    }

    /// <summary>Reads protected and unprotected COSE header buckets.</summary>
    public CoseHeaderResult ReadCoseHeader(byte[] cbor, TokenPolicy policy)
    {
        return CoseHeader.Read(cbor, policy);
    }

    /// <summary>Writes protected and unprotected COSE header buckets deterministically.</summary>
    public TokenBytesResult WriteCoseHeader(CoseHeader header, TokenPolicy policy)
    {
        return CoseHeader.Write(header, policy);
    }

    /// <summary>Returns exact parsed or deterministic protected map bytes.</summary>
    public TokenBytesResult GetCoseProtectedBytes(CoseHeader header, TokenPolicy policy)
    {
        return CoseSign.GetProtectedBytes(header, policy);
    }

    /// <summary>Validates protected critical parameters.</summary>
    public TokenResult ValidateCoseCriticalHeaders(CoseHeader header, CborItem[] understoodLabels)
    {
        return CoseCriticalHeaderValidator.Validate(
            header,
            understoodLabels);
    }

    /// <summary>Detects a tagged or contextually identified untagged COSE message.</summary>
    public CoseMessageResult DetectCoseMessage(byte[] cbor, CoseMessageKind expectedKind, TokenPolicy policy)
    {
        return CoseMessage.Read(cbor, expectedKind, policy);
    }

    /// <summary>Encodes a COSE_Sign1 Sig_structure.</summary>
    public TokenBytesResult EncodeCoseSign1Structure(byte[] bodyProtected, byte[] externalAuthenticatedData, byte[] payload, TokenPolicy policy)
    {
        return CoseSigStructure.CreateSignature1(
            bodyProtected,
            externalAuthenticatedData,
            payload,
            policy);
    }

    /// <summary>Encodes a COSE_Sign Sig_structure.</summary>
    public TokenBytesResult EncodeCoseSignStructure(byte[] bodyProtected, byte[] signerProtected, byte[] externalAuthenticatedData, byte[] payload, TokenPolicy policy)
    {
        return CoseSigStructure.CreateSignature(
            bodyProtected,
            signerProtected,
            externalAuthenticatedData,
            payload,
            policy);
    }

    /// <summary>Encodes an Enc_structure.</summary>
    public TokenBytesResult EncodeCoseEncryptionStructure(CoseEncStructureContext context, byte[] protectedParameters, byte[] externalAuthenticatedData, TokenPolicy policy)
    {
        return CoseEncStructure.Create(
            context,
            protectedParameters,
            externalAuthenticatedData,
            policy);
    }

    /// <summary>Encodes a COSE_Mac MAC_structure.</summary>
    public TokenBytesResult EncodeCoseMacStructure(byte[] protectedParameters, byte[] externalAuthenticatedData, byte[] payload, TokenPolicy policy)
    {
        return CoseMacStructure.CreateMac(
            protectedParameters,
            externalAuthenticatedData,
            payload,
            policy);
    }

    /// <summary>Encodes a COSE_Mac0 MAC_structure.</summary>
    public TokenBytesResult EncodeCoseMac0Structure(byte[] protectedParameters, byte[] externalAuthenticatedData, byte[] payload, TokenPolicy policy)
    {
        return CoseMacStructure.CreateMac0(
            protectedParameters,
            externalAuthenticatedData,
            payload,
            policy);
    }

    /// <summary>Encodes a full RFC 9338 Countersign_structure.</summary>
    public TokenBytesResult EncodeCoseCounterSignatureStructure(byte[] bodyProtected, byte[] signerProtected, byte[] externalAuthenticatedData, byte[] payload, byte[][] otherFields, TokenPolicy policy)
    {
        return CoseCounterSignatureStructure.Create(
            bodyProtected,
            signerProtected,
            externalAuthenticatedData,
            payload,
            otherFields,
            policy);
    }

    /// <summary>Encodes an abbreviated RFC 9338 Countersign_structure.</summary>
    public TokenBytesResult EncodeCoseAbbreviatedCounterSignatureStructure(byte[] bodyProtected, byte[] externalAuthenticatedData, byte[] payload, byte[][] otherFields, TokenPolicy policy)
    {
        return CoseCounterSignatureStructure.CreateAbbreviated(
            bodyProtected,
            externalAuthenticatedData,
            payload,
            otherFields,
            policy);
    }

    /// <summary>Creates a full countersignature over a finalized COSE target.</summary>
    public CoseCounterSignatureResult CreateCoseCounterSignature(CoseCounterSignatureTarget target, CoseCounterSignature counterSignature, TokenKey key, TokenPolicy policy)
    {
        if (counterSignature == null)
        {
            throw new ArgumentNullException("counterSignature");
        }

        if (counterSignature.Header == null)
        {
            throw new ArgumentException(
                "A countersignature header is required.",
                "counterSignature");
        }

        return CoseCounterSignatureOperations.Create(
            target,
            Array.Empty<byte>(),
            counterSignature.Header,
            key,
            counterSignature.Tagged,
            false,
            policy);
    }

    /// <summary>Encodes a size-limited COSE key-derivation context.</summary>
    public TokenBytesResult EncodeCoseKdfContext(CoseKdfContextParameters parameters, TokenPolicy policy)
    {
        return CoseKdfContext.Create(parameters, policy);
    }

    /// <summary>Reads one standalone COSE recipient.</summary>
    public CoseRecipientResult ReadCoseRecipient(byte[] cbor, TokenPolicy policy)
    {
        return CoseRecipientCodec.Read(cbor, policy);
    }

    /// <summary>Writes one standalone COSE recipient.</summary>
    public TokenBytesResult WriteCoseRecipient(CoseRecipient recipient, TokenPolicy policy)
    {
        return CoseRecipientCodec.Write(recipient, policy);
    }

    /// <summary>Reads a tagged or untagged COSE_Key.</summary>
    public CoseKeyResult ReadCoseKey(byte[] cbor, bool allowCompatibility)
    {
        return CoseKey.Read(cbor, allowCompatibility);
    }

    /// <summary>Writes a deterministic untagged COSE_Key.</summary>
    public TokenBytesResult WriteCoseKey(TokenKey key)
    {
        return CoseKey.Write(key);
    }

    /// <summary>Creates a COSE_Encrypt message with separately transported ciphertext.</summary>
    public CoseDetachedEncryptionResult EncryptDetachedCose(byte[] plaintext, CoseEncrypt message, TokenKey[] recipientKeys, TokenPolicy policy)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (message.Header == null)
        {
            throw new ArgumentException(
                "COSE_Encrypt headers are required.",
                "message");
        }

        if (message.Recipients == null)
        {
            throw new ArgumentException(
                "COSE_Encrypt recipients are required.",
                "message");
        }

        CoseHeader[] recipientHeaders =
            new CoseHeader[message.Recipients.Length];
        int index = 0;
        while (index < message.Recipients.Length)
        {
            CoseRecipient recipient = message.Recipients[index];
            if (recipient == null || recipient.Header == null)
            {
                throw new ArgumentException(
                    "Every COSE_Encrypt recipient requires headers.",
                    "message");
            }

            recipientHeaders[index] = recipient.Header;
            index += 1;
        }

        return CoseEncryptOperations.CreateDetached(
            plaintext,
            Array.Empty<byte>(),
            message.Header,
            recipientHeaders,
            recipientKeys,
            null,
            message.Tagged,
            policy);
    }
}
