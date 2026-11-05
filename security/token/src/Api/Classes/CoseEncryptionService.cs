using System;
namespace token;

/// <summary>Implements public COSE encryption, decryption, MAC, and verification operations.</summary>
public class CoseEncryptionService : ICoseEncryptionOperations
{
    /// <summary>Initializes a stateless COSE encryption and MAC service.</summary>
    public CoseEncryptionService()
    {
    }

    /// <summary>Reads COSE_Encrypt0 metadata while withholding unauthenticated ciphertext.</summary>
    public CoseEncrypt0Result ReadCoseEncrypt0(byte[] cbor, TokenPolicy policy)
    {
        CoseEncrypt0Result result = CoseEncrypt0.Read(cbor, policy);
        if (result.Success
            && (result.Message ?? throw new InvalidOperationException("A required value is null.")).Ciphertext != null)
        {
            result.Message.Ciphertext = null;
            result.Message.CiphertextAvailable = false;
        }

        return result;
    }

    /// <summary>Reads COSE_Encrypt0 ciphertext when diagnostics are enabled.</summary>
    public CoseEncrypt0Result ReadCoseEncrypt0Diagnostic(byte[] cbor, TokenPolicy policy)
    {
        if (cbor == null)
        {
            throw new ArgumentNullException("cbor");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult policyResult = CoseEncrypt0.ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return CoseEncrypt0.Failure(policyResult.Error);
        }

        if (!policy.AllowUnauthenticatedDecode)
        {
            return CoseEncrypt0.Failure(new TokenError(
                "unauthenticated_decode_not_allowed",
                "Diagnostic COSE_Encrypt0 ciphertext decoding requires explicit policy opt-in.",
                -1));
        }

        CoseEncrypt0Result result = CoseEncrypt0.Read(cbor, policy);
        if (result.Success
            && (result.Message ?? throw new InvalidOperationException("A required value is null.")).Ciphertext != null)
        {
            result.Message.CiphertextAvailable = true;
        }

        return result;
    }

    /// <summary>Writes a COSE_Encrypt0 model.</summary>
    public TokenBytesResult WriteCoseEncrypt0(CoseEncrypt0 message, bool tagged, TokenPolicy policy)
    {
        return CoseEncrypt0.Write(message, tagged, policy);
    }

    /// <summary>Encrypts attached plaintext as COSE_Encrypt0.</summary>
    public TokenBytesResult EncryptCoseEncrypt0(byte[] plaintext, byte[] externalAuthenticatedData, CoseHeader header, TokenKey key, bool tagged, TokenPolicy policy)
    {
        return CoseEncrypt0Operations.CreateWithExternalAuthenticatedData(
            plaintext,
            externalAuthenticatedData,
            header,
            key,
            tagged,
            policy);
    }

    /// <summary>Encrypts COSE_Encrypt0 with separately transported ciphertext.</summary>
    public CoseDetachedEncryptionResult EncryptDetachedCoseEncrypt0(byte[] plaintext, byte[] externalAuthenticatedData, CoseHeader header, TokenKey key, bool tagged, TokenPolicy policy)
    {
        return CoseEncrypt0Operations.CreateDetached(
            plaintext,
            externalAuthenticatedData,
            header,
            key,
            tagged,
            policy);
    }

    /// <summary>Decrypts attached COSE_Encrypt0 ciphertext.</summary>
    public CoseEncrypt0Result DecryptCoseEncrypt0(byte[] cbor, byte[] externalAuthenticatedData, TokenKey key, TokenPolicy policy)
    {
        CoseEncrypt0Result result =
            CoseEncrypt0Operations.DecryptWithExternalAuthenticatedData(
                cbor,
                externalAuthenticatedData,
                key,
                policy);
        if (result.Success)
        {
            (result.Message ?? throw new InvalidOperationException("A required value is null.")).CiphertextAvailable = true;
        }

        return result;
    }

    /// <summary>Decrypts separately transported COSE_Encrypt0 ciphertext.</summary>
    public CoseEncrypt0Result DecryptDetachedCoseEncrypt0(byte[] cbor, byte[] ciphertext, byte[] externalAuthenticatedData, TokenKey key, TokenPolicy policy)
    {
        CoseEncrypt0Result result = CoseEncrypt0Operations.DecryptDetached(
            cbor,
            ciphertext,
            externalAuthenticatedData,
            key,
            policy);
        if (result.Success)
        {
            (result.Message ?? throw new InvalidOperationException("A required value is null.")).CiphertextAvailable = true;
        }

        return result;
    }

    /// <summary>Reads COSE_Encrypt metadata while withholding unauthenticated ciphertext.</summary>
    public CoseEncryptResult ReadCoseEncrypt(byte[] cbor, TokenPolicy policy)
    {
        CoseEncryptResult result = CoseEncrypt.Read(cbor, policy);
        if (result.Success
            && (result.Message ?? throw new InvalidOperationException("A required value is null.")).Ciphertext != null)
        {
            result.Message.Ciphertext = null;
            result.Message.CiphertextAvailable = false;
        }

        return result;
    }

    /// <summary>Reads COSE_Encrypt ciphertext when diagnostics are enabled.</summary>
    public CoseEncryptResult ReadCoseEncryptDiagnostic(byte[] cbor, TokenPolicy policy)
    {
        if (cbor == null)
        {
            throw new ArgumentNullException("cbor");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult policyResult = CoseEncrypt.ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return CoseEncrypt.Failure(policyResult.Error);
        }

        if (!policy.AllowUnauthenticatedDecode)
        {
            return CoseEncrypt.Failure(new TokenError(
                "unauthenticated_decode_not_allowed",
                "Diagnostic COSE_Encrypt ciphertext decoding requires explicit policy opt-in.",
                -1));
        }

        CoseEncryptResult result = CoseEncrypt.Read(cbor, policy);
        if (result.Success
            && (result.Message ?? throw new InvalidOperationException("A required value is null.")).Ciphertext != null)
        {
            result.Message.CiphertextAvailable = true;
        }

        return result;
    }

    /// <summary>Writes a COSE_Encrypt model.</summary>
    public TokenBytesResult WriteCoseEncrypt(CoseEncrypt message, bool tagged, TokenPolicy policy)
    {
        return CoseEncrypt.Write(message, tagged, policy);
    }

    /// <summary>Encrypts attached plaintext for one or more COSE recipients.</summary>
    public TokenBytesResult EncryptCoseEncrypt(byte[] plaintext, byte[] externalAuthenticatedData, CoseHeader header, CoseHeader[] recipientHeaders, TokenKey[] keys, TokenKey[]? senderKeys, bool tagged, TokenPolicy policy)
    {
        return CoseEncryptOperations.CreateWithExternalAuthenticatedData(
            plaintext,
            externalAuthenticatedData,
            header,
            recipientHeaders,
            keys,
            senderKeys,
            tagged,
            policy);
    }

    /// <summary>Encrypts for COSE recipients with separately transported ciphertext.</summary>
    public CoseDetachedEncryptionResult EncryptDetachedCoseEncrypt(byte[] plaintext, byte[] externalAuthenticatedData, CoseHeader header, CoseHeader[] recipientHeaders, TokenKey[] keys, TokenKey[]? senderKeys, bool tagged, TokenPolicy policy)
    {
        return CoseEncryptOperations.CreateDetached(
            plaintext,
            externalAuthenticatedData,
            header,
            recipientHeaders,
            keys,
            senderKeys,
            tagged,
            policy);
    }

    /// <summary>Decrypts one selected recipient of attached COSE_Encrypt ciphertext.</summary>
    public CoseEncryptResult DecryptCoseEncryptRecipient(byte[] cbor, byte[] externalAuthenticatedData, int recipientIndex, TokenKey key, TokenKey? senderKey, TokenPolicy policy)
    {
        CoseEncryptResult result =
            CoseEncryptOperations.DecryptWithExternalAuthenticatedData(
                cbor,
                externalAuthenticatedData,
                recipientIndex,
                key,
                senderKey,
                policy);
        if (result.Success)
        {
            (result.Message ?? throw new InvalidOperationException("A required value is null.")).CiphertextAvailable = true;
        }

        return result;
    }

    /// <summary>Decrypts one selected recipient using detached ciphertext.</summary>
    public CoseEncryptResult DecryptDetachedCoseEncryptRecipient(byte[] cbor, byte[] ciphertext, byte[] externalAuthenticatedData, int recipientIndex, TokenKey key, TokenKey? senderKey, TokenPolicy policy)
    {
        CoseEncryptResult result = CoseEncryptOperations.DecryptDetached(
            cbor,
            ciphertext,
            externalAuthenticatedData,
            recipientIndex,
            key,
            senderKey,
            policy);
        if (result.Success)
        {
            (result.Message ?? throw new InvalidOperationException("A required value is null.")).CiphertextAvailable = true;
        }

        return result;
    }

    /// <summary>Decrypts and authenticates every top-level COSE_Encrypt recipient.</summary>
    public CoseEncryptResult DecryptCoseEncryptAll(byte[] cbor, byte[] externalAuthenticatedData, TokenKey[] keys, TokenKey[]? senderKeys, TokenPolicy policy)
    {
        CoseEncryptResult result =
            CoseEncryptOperations.DecryptWithExternalAuthenticatedData(
                cbor,
                externalAuthenticatedData,
                keys,
                senderKeys,
                policy);
        if (result.Success)
        {
            (result.Message ?? throw new InvalidOperationException("A required value is null.")).CiphertextAvailable = true;
        }

        return result;
    }

    /// <summary>Decrypts every top-level recipient using detached ciphertext.</summary>
    public CoseEncryptResult DecryptDetachedCoseEncryptAll(byte[] cbor, byte[] ciphertext, byte[] externalAuthenticatedData, TokenKey[] keys, TokenKey[]? senderKeys, TokenPolicy policy)
    {
        CoseEncryptResult result = CoseEncryptOperations.DecryptDetached(
            cbor,
            ciphertext,
            externalAuthenticatedData,
            keys,
            senderKeys,
            policy);
        if (result.Success)
        {
            (result.Message ?? throw new InvalidOperationException("A required value is null.")).CiphertextAvailable = true;
        }

        return result;
    }

    /// <summary>Reads COSE_Mac0 metadata while withholding an unauthenticated payload.</summary>
    public CoseMac0Result ReadCoseMac0(byte[] cbor, TokenPolicy policy)
    {
        CoseMac0Result result = CoseMac0.Read(cbor, policy);
        if (result.Success
            && (result.Message ?? throw new InvalidOperationException("A required value is null.")).Payload != null)
        {
            result.Message.Payload = null;
            result.Message.PayloadAvailable = false;
        }

        return result;
    }

    /// <summary>Reads a COSE_Mac0 payload when diagnostics are enabled.</summary>
    public CoseMac0Result ReadCoseMac0Diagnostic(byte[] cbor, TokenPolicy policy)
    {
        if (cbor == null)
        {
            throw new ArgumentNullException("cbor");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult policyResult = CoseMac0.ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return CoseMac0.Failure(policyResult.Error);
        }

        if (!policy.AllowUnauthenticatedDecode)
        {
            return CoseMac0.Failure(new TokenError(
                "unauthenticated_decode_not_allowed",
                "Diagnostic COSE_Mac0 payload decoding requires explicit policy opt-in.",
                -1));
        }

        CoseMac0Result result = CoseMac0.Read(cbor, policy);
        if (result.Success
            && (result.Message ?? throw new InvalidOperationException("A required value is null.")).Payload != null)
        {
            result.Message.PayloadAvailable = true;
        }

        return result;
    }

    /// <summary>Writes a COSE_Mac0 model.</summary>
    public TokenBytesResult WriteCoseMac0(CoseMac0 message, bool tagged, TokenPolicy policy)
    {
        return CoseMac0.Write(message, tagged, policy);
    }

    /// <summary>Creates an attached COSE_Mac0 message.</summary>
    public TokenBytesResult CreateCoseMac0(byte[] payload, byte[] externalAuthenticatedData, CoseHeader header, TokenKey key, bool tagged, TokenPolicy policy)
    {
        return CoseMac0Operations.CreateWithExternalAuthenticatedData(
            payload,
            externalAuthenticatedData,
            header,
            key,
            tagged,
            policy);
    }

    /// <summary>Creates a COSE_Mac0 message with a detached payload.</summary>
    public TokenBytesResult CreateDetachedCoseMac0(byte[] payload, byte[] externalAuthenticatedData, CoseHeader header, TokenKey key, bool tagged, TokenPolicy policy)
    {
        return CoseMac0Operations.CreateDetached(
            payload,
            externalAuthenticatedData,
            header,
            key,
            tagged,
            policy);
    }

    /// <summary>Verifies an attached COSE_Mac0 message.</summary>
    public CoseMac0Result VerifyCoseMac0(byte[] cbor, byte[] externalAuthenticatedData, TokenKey key, TokenPolicy policy)
    {
        CoseMac0Result result =
            CoseMac0Operations.VerifyWithExternalAuthenticatedData(
                cbor,
                externalAuthenticatedData,
                key,
                policy);
        if (result.Success)
        {
            (result.Message ?? throw new InvalidOperationException("A required value is null.")).PayloadAvailable = true;
        }

        return result;
    }

    /// <summary>Verifies COSE_Mac0 against a detached payload.</summary>
    public CoseMac0Result VerifyDetachedCoseMac0(byte[] cbor, byte[] payload, byte[] externalAuthenticatedData, TokenKey key, TokenPolicy policy)
    {
        CoseMac0Result result = CoseMac0Operations.VerifyDetached(
            cbor,
            payload,
            externalAuthenticatedData,
            key,
            policy);
        if (result.Success)
        {
            (result.Message ?? throw new InvalidOperationException("A required value is null.")).PayloadAvailable = true;
        }

        return result;
    }

    /// <summary>Reads COSE_Mac metadata while withholding an unauthenticated payload.</summary>
    public CoseMacResult ReadCoseMac(byte[] cbor, TokenPolicy policy)
    {
        CoseMacResult result = CoseMac.Read(cbor, policy);
        if (result.Success
            && (result.Message ?? throw new InvalidOperationException("A required value is null.")).Payload != null)
        {
            result.Message.Payload = null;
            result.Message.PayloadAvailable = false;
        }

        return result;
    }

    /// <summary>Reads a COSE_Mac payload when diagnostics are enabled.</summary>
    public CoseMacResult ReadCoseMacDiagnostic(byte[] cbor, TokenPolicy policy)
    {
        if (cbor == null)
        {
            throw new ArgumentNullException("cbor");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult policyResult = CoseMac.ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return CoseMac.Failure(policyResult.Error);
        }

        if (!policy.AllowUnauthenticatedDecode)
        {
            return CoseMac.Failure(new TokenError(
                "unauthenticated_decode_not_allowed",
                "Diagnostic COSE_Mac payload decoding requires explicit policy opt-in.",
                -1));
        }

        CoseMacResult result = CoseMac.Read(cbor, policy);
        if (result.Success
            && (result.Message ?? throw new InvalidOperationException("A required value is null.")).Payload != null)
        {
            result.Message.PayloadAvailable = true;
        }

        return result;
    }

    /// <summary>Writes a COSE_Mac model.</summary>
    public TokenBytesResult WriteCoseMac(CoseMac message, bool tagged, TokenPolicy policy)
    {
        return CoseMac.Write(message, tagged, policy);
    }

    /// <summary>Creates an attached COSE_Mac message for one or more recipients.</summary>
    public TokenBytesResult CreateCoseMac(byte[] payload, byte[] externalAuthenticatedData, CoseHeader header, CoseHeader[] recipientHeaders, TokenKey[] keys, TokenKey[]? senderKeys, bool tagged, TokenPolicy policy)
    {
        return CoseMacOperations.CreateWithExternalAuthenticatedData(
            payload,
            externalAuthenticatedData,
            header,
            recipientHeaders,
            keys,
            senderKeys,
            tagged,
            policy);
    }

    /// <summary>Creates a COSE_Mac message with a detached payload.</summary>
    public TokenBytesResult CreateDetachedCoseMac(byte[] payload, byte[] externalAuthenticatedData, CoseHeader header, CoseHeader[] recipientHeaders, TokenKey[] keys, TokenKey[]? senderKeys, bool tagged, TokenPolicy policy)
    {
        return CoseMacOperations.CreateDetached(
            payload,
            externalAuthenticatedData,
            header,
            recipientHeaders,
            keys,
            senderKeys,
            tagged,
            policy);
    }

    /// <summary>Verifies one selected recipient of an attached COSE_Mac message.</summary>
    public CoseMacResult VerifyCoseMacRecipient(byte[] cbor, byte[] externalAuthenticatedData, int recipientIndex, TokenKey key, TokenKey? senderKey, TokenPolicy policy)
    {
        CoseMacResult result =
            CoseMacOperations.VerifyWithExternalAuthenticatedData(
                cbor,
                externalAuthenticatedData,
                recipientIndex,
                key,
                senderKey,
                policy);
        if (result.Success)
        {
            (result.Message ?? throw new InvalidOperationException("A required value is null.")).PayloadAvailable = true;
        }

        return result;
    }

    /// <summary>Verifies one selected recipient against a detached COSE_Mac payload.</summary>
    public CoseMacResult VerifyDetachedCoseMacRecipient(byte[] cbor, byte[] payload, byte[] externalAuthenticatedData, int recipientIndex, TokenKey key, TokenKey? senderKey, TokenPolicy policy)
    {
        CoseMacResult result = CoseMacOperations.VerifyDetached(
            cbor,
            payload,
            externalAuthenticatedData,
            recipientIndex,
            key,
            senderKey,
            policy);
        if (result.Success)
        {
            (result.Message ?? throw new InvalidOperationException("A required value is null.")).PayloadAvailable = true;
        }

        return result;
    }

    /// <summary>Verifies every top-level recipient of an attached COSE_Mac message.</summary>
    public CoseMacResult VerifyCoseMacAll(byte[] cbor, byte[] externalAuthenticatedData, TokenKey[] keys, TokenKey[]? senderKeys, TokenPolicy policy)
    {
        CoseMacResult result =
            CoseMacOperations.VerifyWithExternalAuthenticatedData(
                cbor,
                externalAuthenticatedData,
                keys,
                senderKeys,
                policy);
        if (result.Success)
        {
            (result.Message ?? throw new InvalidOperationException("A required value is null.")).PayloadAvailable = true;
        }

        return result;
    }

    /// <summary>Verifies every top-level recipient against a detached COSE_Mac payload.</summary>
    public CoseMacResult VerifyDetachedCoseMacAll(byte[] cbor, byte[] payload, byte[] externalAuthenticatedData, TokenKey[] keys, TokenKey[]? senderKeys, TokenPolicy policy)
    {
        CoseMacResult result = CoseMacOperations.VerifyDetached(
            cbor,
            payload,
            externalAuthenticatedData,
            keys,
            senderKeys,
            policy);
        if (result.Success)
        {
            (result.Message ?? throw new InvalidOperationException("A required value is null.")).PayloadAvailable = true;
        }

        return result;
    }
}
