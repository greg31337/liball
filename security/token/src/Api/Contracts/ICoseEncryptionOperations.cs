using System;
namespace token;

/// <summary>Provides COSE encryption, decryption, message authentication, and verification operations.</summary>
public interface ICoseEncryptionOperations
{
    /// <summary>Reads COSE_Encrypt0 metadata while withholding unauthenticated ciphertext.</summary>
    /// <param name="cbor">The encoded message.</param>
    /// <param name="policy">The resource policy.</param>
    /// <returns>The parsed metadata or stable failure details.</returns>
    CoseEncrypt0Result ReadCoseEncrypt0(byte[] cbor, TokenPolicy policy);

    /// <summary>Reads COSE_Encrypt0 ciphertext when diagnostic access is enabled.</summary>
    /// <param name="cbor">The encoded message.</param>
    /// <param name="policy">The diagnostic and resource policy.</param>
    /// <returns>The parsed message or stable failure details.</returns>
    CoseEncrypt0Result ReadCoseEncrypt0Diagnostic(byte[] cbor, TokenPolicy policy);

    /// <summary>Writes a COSE_Encrypt0 model.</summary>
    /// <param name="message">The message to write.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 16.</param>
    /// <param name="policy">The output policy.</param>
    /// <returns>The encoded message or stable failure details.</returns>
    TokenBytesResult WriteCoseEncrypt0(CoseEncrypt0 message, bool tagged, TokenPolicy policy);

    /// <summary>Encrypts attached plaintext as COSE_Encrypt0.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="header">The content headers.</param>
    /// <param name="key">The content-encryption key.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 16.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The encrypted message or stable failure details.</returns>
    TokenBytesResult EncryptCoseEncrypt0(byte[] plaintext, byte[] externalAuthenticatedData, CoseHeader header, TokenKey key, bool tagged, TokenPolicy policy);

    /// <summary>Encrypts COSE_Encrypt0 plaintext with separately transported ciphertext.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="header">The content headers.</param>
    /// <param name="key">The content-encryption key.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 16.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The encoded message and detached ciphertext or stable failure details.</returns>
    CoseDetachedEncryptionResult EncryptDetachedCoseEncrypt0(byte[] plaintext, byte[] externalAuthenticatedData, CoseHeader header, TokenKey key, bool tagged, TokenPolicy policy);

    /// <summary>Decrypts attached COSE_Encrypt0 ciphertext.</summary>
    /// <param name="cbor">The encoded message.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="key">The content-decryption key.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated plaintext or stable failure details.</returns>
    CoseEncrypt0Result DecryptCoseEncrypt0(byte[] cbor, byte[] externalAuthenticatedData, TokenKey key, TokenPolicy policy);

    /// <summary>Decrypts separately transported COSE_Encrypt0 ciphertext.</summary>
    /// <param name="cbor">The encoded message containing a null ciphertext field.</param>
    /// <param name="ciphertext">The detached ciphertext and authentication tag.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="key">The content-decryption key.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated plaintext or stable failure details.</returns>
    CoseEncrypt0Result DecryptDetachedCoseEncrypt0(byte[] cbor, byte[] ciphertext, byte[] externalAuthenticatedData, TokenKey key, TokenPolicy policy);

    /// <summary>Reads COSE_Encrypt metadata while withholding unauthenticated ciphertext.</summary>
    /// <param name="cbor">The encoded message.</param>
    /// <param name="policy">The resource policy.</param>
    /// <returns>The parsed metadata and recipients or stable failure details.</returns>
    CoseEncryptResult ReadCoseEncrypt(byte[] cbor, TokenPolicy policy);

    /// <summary>Reads COSE_Encrypt ciphertext when diagnostic access is enabled.</summary>
    /// <param name="cbor">The encoded message.</param>
    /// <param name="policy">The diagnostic and resource policy.</param>
    /// <returns>The parsed message or stable failure details.</returns>
    CoseEncryptResult ReadCoseEncryptDiagnostic(byte[] cbor, TokenPolicy policy);

    /// <summary>Writes a COSE_Encrypt model and nested recipient tree.</summary>
    /// <param name="message">The message to write.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 96.</param>
    /// <param name="policy">The output policy.</param>
    /// <returns>The encoded message or stable failure details.</returns>
    TokenBytesResult WriteCoseEncrypt(CoseEncrypt message, bool tagged, TokenPolicy policy);

    /// <summary>Encrypts attached plaintext for one or more COSE recipients.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="header">The shared content headers.</param>
    /// <param name="recipientHeaders">The recipient headers in serialization order.</param>
    /// <param name="keys">The recipient keys in serialization order.</param>
    /// <param name="senderKeys">Static sender keys, or <c>null</c> for non-static modes.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 96.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The encrypted message or stable failure details.</returns>
    TokenBytesResult EncryptCoseEncrypt(byte[] plaintext, byte[] externalAuthenticatedData, CoseHeader header, CoseHeader[] recipientHeaders, TokenKey[] keys, TokenKey[]? senderKeys, bool tagged, TokenPolicy policy);

    /// <summary>Encrypts for COSE recipients with separately transported ciphertext.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="header">The shared content headers.</param>
    /// <param name="recipientHeaders">The recipient headers in serialization order.</param>
    /// <param name="keys">The recipient keys in serialization order.</param>
    /// <param name="senderKeys">Static sender keys, or <c>null</c> for non-static modes.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 96.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The encoded message and detached ciphertext or stable failure details.</returns>
    CoseDetachedEncryptionResult EncryptDetachedCoseEncrypt(byte[] plaintext, byte[] externalAuthenticatedData, CoseHeader header, CoseHeader[] recipientHeaders, TokenKey[] keys, TokenKey[]? senderKeys, bool tagged, TokenPolicy policy);

    /// <summary>Decrypts one selected recipient of attached COSE_Encrypt ciphertext.</summary>
    /// <param name="cbor">The encoded message.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="recipientIndex">The selected recipient index.</param>
    /// <param name="key">The selected recipient key.</param>
    /// <param name="senderKey">The static sender public key, or <c>null</c>.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated plaintext or stable failure details.</returns>
    CoseEncryptResult DecryptCoseEncryptRecipient(byte[] cbor, byte[] externalAuthenticatedData, int recipientIndex, TokenKey key, TokenKey? senderKey, TokenPolicy policy);

    /// <summary>Decrypts one selected recipient using detached ciphertext.</summary>
    /// <param name="cbor">The encoded message containing a null ciphertext field.</param>
    /// <param name="ciphertext">The detached ciphertext.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="recipientIndex">The selected recipient index.</param>
    /// <param name="key">The selected recipient key.</param>
    /// <param name="senderKey">The static sender public key, or <c>null</c>.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated plaintext or stable failure details.</returns>
    CoseEncryptResult DecryptDetachedCoseEncryptRecipient(byte[] cbor, byte[] ciphertext, byte[] externalAuthenticatedData, int recipientIndex, TokenKey key, TokenKey? senderKey, TokenPolicy policy);

    /// <summary>Decrypts and authenticates every top-level COSE_Encrypt recipient.</summary>
    /// <param name="cbor">The encoded message.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="keys">The recipient keys in serialization order.</param>
    /// <param name="senderKeys">Static sender public keys, or <c>null</c>.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated plaintext or stable failure details.</returns>
    CoseEncryptResult DecryptCoseEncryptAll(byte[] cbor, byte[] externalAuthenticatedData, TokenKey[] keys, TokenKey[]? senderKeys, TokenPolicy policy);

    /// <summary>Decrypts every top-level recipient using detached ciphertext.</summary>
    /// <param name="cbor">The encoded message containing a null ciphertext field.</param>
    /// <param name="ciphertext">The detached ciphertext.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="keys">The recipient keys in serialization order.</param>
    /// <param name="senderKeys">Static sender public keys, or <c>null</c>.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated plaintext or stable failure details.</returns>
    CoseEncryptResult DecryptDetachedCoseEncryptAll(byte[] cbor, byte[] ciphertext, byte[] externalAuthenticatedData, TokenKey[] keys, TokenKey[]? senderKeys, TokenPolicy policy);

    /// <summary>Reads COSE_Mac0 metadata while withholding an unauthenticated payload.</summary>
    /// <param name="cbor">The encoded message.</param>
    /// <param name="policy">The resource policy.</param>
    /// <returns>The parsed metadata or stable failure details.</returns>
    CoseMac0Result ReadCoseMac0(byte[] cbor, TokenPolicy policy);

    /// <summary>Reads a COSE_Mac0 payload when diagnostic access is enabled.</summary>
    /// <param name="cbor">The encoded message.</param>
    /// <param name="policy">The diagnostic and resource policy.</param>
    /// <returns>The parsed message or stable failure details.</returns>
    CoseMac0Result ReadCoseMac0Diagnostic(byte[] cbor, TokenPolicy policy);

    /// <summary>Writes a COSE_Mac0 model.</summary>
    /// <param name="message">The message to write.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 17.</param>
    /// <param name="policy">The output policy.</param>
    /// <returns>The encoded message or stable failure details.</returns>
    TokenBytesResult WriteCoseMac0(CoseMac0 message, bool tagged, TokenPolicy policy);

    /// <summary>Creates an attached COSE_Mac0 message.</summary>
    /// <param name="payload">The payload bytes.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="header">The MAC headers.</param>
    /// <param name="key">The symmetric MAC key.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 17.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The MAC message or stable failure details.</returns>
    TokenBytesResult CreateCoseMac0(byte[] payload, byte[] externalAuthenticatedData, CoseHeader header, TokenKey key, bool tagged, TokenPolicy policy);

    /// <summary>Creates a COSE_Mac0 message with a detached payload.</summary>
    /// <param name="payload">The detached payload bytes.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="header">The MAC headers.</param>
    /// <param name="key">The symmetric MAC key.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 17.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The MAC message or stable failure details.</returns>
    TokenBytesResult CreateDetachedCoseMac0(byte[] payload, byte[] externalAuthenticatedData, CoseHeader header, TokenKey key, bool tagged, TokenPolicy policy);

    /// <summary>Verifies an attached COSE_Mac0 message.</summary>
    /// <param name="cbor">The encoded message.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="key">The symmetric MAC key.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated payload or stable failure details.</returns>
    CoseMac0Result VerifyCoseMac0(byte[] cbor, byte[] externalAuthenticatedData, TokenKey key, TokenPolicy policy);

    /// <summary>Verifies COSE_Mac0 against a detached payload.</summary>
    /// <param name="cbor">The encoded message containing a null payload field.</param>
    /// <param name="payload">The detached payload.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="key">The symmetric MAC key.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated payload or stable failure details.</returns>
    CoseMac0Result VerifyDetachedCoseMac0(byte[] cbor, byte[] payload, byte[] externalAuthenticatedData, TokenKey key, TokenPolicy policy);

    /// <summary>Reads COSE_Mac metadata while withholding an unauthenticated payload.</summary>
    /// <param name="cbor">The encoded message.</param>
    /// <param name="policy">The resource policy.</param>
    /// <returns>The parsed metadata and recipients or stable failure details.</returns>
    CoseMacResult ReadCoseMac(byte[] cbor, TokenPolicy policy);

    /// <summary>Reads a COSE_Mac payload when diagnostic access is enabled.</summary>
    /// <param name="cbor">The encoded message.</param>
    /// <param name="policy">The diagnostic and resource policy.</param>
    /// <returns>The parsed message or stable failure details.</returns>
    CoseMacResult ReadCoseMacDiagnostic(byte[] cbor, TokenPolicy policy);

    /// <summary>Writes a COSE_Mac model and nested recipient tree.</summary>
    /// <param name="message">The message to write.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 97.</param>
    /// <param name="policy">The output policy.</param>
    /// <returns>The encoded message or stable failure details.</returns>
    TokenBytesResult WriteCoseMac(CoseMac message, bool tagged, TokenPolicy policy);

    /// <summary>Creates an attached COSE_Mac message for one or more recipients.</summary>
    /// <param name="payload">The payload bytes.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="header">The shared MAC headers.</param>
    /// <param name="recipientHeaders">The recipient headers in serialization order.</param>
    /// <param name="keys">The recipient keys in serialization order.</param>
    /// <param name="senderKeys">Static sender keys, or <c>null</c>.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 97.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The MAC message or stable failure details.</returns>
    TokenBytesResult CreateCoseMac(byte[] payload, byte[] externalAuthenticatedData, CoseHeader header, CoseHeader[] recipientHeaders, TokenKey[] keys, TokenKey[]? senderKeys, bool tagged, TokenPolicy policy);

    /// <summary>Creates a COSE_Mac message with a detached payload.</summary>
    /// <param name="payload">The detached payload bytes.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="header">The shared MAC headers.</param>
    /// <param name="recipientHeaders">The recipient headers in serialization order.</param>
    /// <param name="keys">The recipient keys in serialization order.</param>
    /// <param name="senderKeys">Static sender keys, or <c>null</c>.</param>
    /// <param name="tagged">Whether to include registered CBOR tag 97.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The MAC message or stable failure details.</returns>
    TokenBytesResult CreateDetachedCoseMac(byte[] payload, byte[] externalAuthenticatedData, CoseHeader header, CoseHeader[] recipientHeaders, TokenKey[] keys, TokenKey[]? senderKeys, bool tagged, TokenPolicy policy);

    /// <summary>Verifies one selected recipient of an attached COSE_Mac message.</summary>
    /// <param name="cbor">The encoded message.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="recipientIndex">The selected recipient index.</param>
    /// <param name="key">The selected recipient key.</param>
    /// <param name="senderKey">The static sender public key, or <c>null</c>.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated payload or stable failure details.</returns>
    CoseMacResult VerifyCoseMacRecipient(byte[] cbor, byte[] externalAuthenticatedData, int recipientIndex, TokenKey key, TokenKey? senderKey, TokenPolicy policy);

    /// <summary>Verifies one selected recipient against a detached COSE_Mac payload.</summary>
    /// <param name="cbor">The encoded message containing a null payload field.</param>
    /// <param name="payload">The detached payload.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="recipientIndex">The selected recipient index.</param>
    /// <param name="key">The selected recipient key.</param>
    /// <param name="senderKey">The static sender public key, or <c>null</c>.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated payload or stable failure details.</returns>
    CoseMacResult VerifyDetachedCoseMacRecipient(byte[] cbor, byte[] payload, byte[] externalAuthenticatedData, int recipientIndex, TokenKey key, TokenKey? senderKey, TokenPolicy policy);

    /// <summary>Verifies every top-level recipient of an attached COSE_Mac message.</summary>
    /// <param name="cbor">The encoded message.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="keys">The recipient keys in serialization order.</param>
    /// <param name="senderKeys">Static sender public keys, or <c>null</c>.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated payload or stable failure details.</returns>
    CoseMacResult VerifyCoseMacAll(byte[] cbor, byte[] externalAuthenticatedData, TokenKey[] keys, TokenKey[]? senderKeys, TokenPolicy policy);

    /// <summary>Verifies every top-level recipient against a detached COSE_Mac payload.</summary>
    /// <param name="cbor">The encoded message containing a null payload field.</param>
    /// <param name="payload">The detached payload.</param>
    /// <param name="externalAuthenticatedData">The external authenticated data.</param>
    /// <param name="keys">The recipient keys in serialization order.</param>
    /// <param name="senderKeys">Static sender public keys, or <c>null</c>.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated payload or stable failure details.</returns>
    CoseMacResult VerifyDetachedCoseMacAll(byte[] cbor, byte[] payload, byte[] externalAuthenticatedData, TokenKey[] keys, TokenKey[]? senderKeys, TokenPolicy policy);
}
