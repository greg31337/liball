using System;

namespace token;

/// <summary>Implements built-in JOSE and COSE cryptographic algorithm operations.</summary>
public class TokenAlgorithmService : ITokenAlgorithmOperations
{
    /// <summary>Initializes a stateless algorithm service.</summary>
    public TokenAlgorithmService()
    {
    }

    /// <summary>Creates a JWS HMAC signature.</summary>
    public TokenBytesResult SignJoseHmac(byte[] data, TokenKey key, string algorithm)
    {
        return HmacAlgorithms.Sign(data, key, algorithm);
    }

    /// <summary>Verifies a JWS HMAC signature.</summary>
    public TokenResult VerifyJoseHmac(byte[] data, byte[] signature, TokenKey key, string algorithm)
    {
        return HmacAlgorithms.Verify(data, signature, key, algorithm);
    }

    /// <summary>Creates a JWS ECDSA signature.</summary>
    public TokenBytesResult SignJoseEcdsa(byte[] data, TokenKey key, string algorithm)
    {
        return EcdsaAlgorithms.Sign(data, key, algorithm);
    }

    /// <summary>Verifies a JWS ECDSA signature.</summary>
    public TokenResult VerifyJoseEcdsa(byte[] data, byte[] signature, TokenKey key, string algorithm)
    {
        return EcdsaAlgorithms.Verify(data, signature, key, algorithm);
    }

    /// <summary>Creates a JWS RSA signature.</summary>
    public TokenBytesResult SignJoseRsa(byte[] data, TokenKey key, string algorithm)
    {
        return RsaAlgorithms.Sign(data, key, algorithm);
    }

    /// <summary>Verifies a JWS RSA signature.</summary>
    public TokenResult VerifyJoseRsa(byte[] data, byte[] signature, TokenKey key, string algorithm)
    {
        return RsaAlgorithms.Verify(data, signature, key, algorithm);
    }

    /// <summary>Creates a COSE ECDSA signature.</summary>
    public TokenBytesResult SignCoseEcdsa(byte[] data, TokenKey key, int algorithm, bool allowCompatibility)
    {
        return CoseEcdsaAlgorithms.Sign(data, key, algorithm, allowCompatibility);
    }

    /// <summary>Verifies a COSE ECDSA signature.</summary>
    public TokenResult VerifyCoseEcdsa(byte[] data, byte[] signature, TokenKey key, int algorithm, bool allowCompatibility)
    {
        return CoseEcdsaAlgorithms.Verify(data, signature, key, algorithm, allowCompatibility);
    }

    /// <summary>Creates a COSE RSA signature.</summary>
    public TokenBytesResult SignCoseRsa(byte[] data, TokenKey key, int algorithm)
    {
        ValidateSignatureReferences(data, key);
        TokenResult availability = CryptoAlgorithmAvailability.CheckCoseAlgorithm(algorithm);
        if (!availability.Success)
        {
            return BytesFailureFromError(availability.Error);
        }

        AlgorithmMappingResult mapping = JoseCoseAlgorithmMap.FromCose(algorithm);
        if (!mapping.Mapped)
        {
            return BytesFailure("invalid_algorithm", "The COSE RSA algorithm has no JOSE primitive mapping.");
        }

        return RsaAlgorithms.Sign(data, key, mapping.JoseName ?? throw new InvalidOperationException("A required value is null."));
    }

    /// <summary>Verifies a COSE RSA signature.</summary>
    public TokenResult VerifyCoseRsa(byte[] data, byte[] signature, TokenKey key, int algorithm)
    {
        ValidateSignatureReferences(data, key);
        if (signature == null)
        {
            throw new ArgumentNullException("signature");
        }

        TokenResult availability = CryptoAlgorithmAvailability.CheckCoseAlgorithm(algorithm);
        if (!availability.Success)
        {
            return availability;
        }

        AlgorithmMappingResult mapping = JoseCoseAlgorithmMap.FromCose(algorithm);
        if (!mapping.Mapped)
        {
            return ResultFailure("invalid_algorithm", "The COSE RSA algorithm has no JOSE primitive mapping.");
        }

        return RsaAlgorithms.Verify(data, signature, key, mapping.JoseName ?? throw new InvalidOperationException("A required value is null."));
    }

    /// <summary>Creates a COSE HMAC authentication tag.</summary>
    public TokenBytesResult CreateCoseHmac(byte[] data, TokenKey key, int algorithm)
    {
        return CoseHmacAlgorithms.Create(data, key, algorithm);
    }

    /// <summary>Verifies a COSE HMAC authentication tag.</summary>
    public TokenResult VerifyCoseHmac(byte[] data, byte[] tag, TokenKey key, int algorithm)
    {
        return CoseHmacAlgorithms.Verify(data, tag, key, algorithm);
    }

    /// <summary>Derives JOSE ECDH key material with Concat KDF and SHA-256.</summary>
    public TokenBytesResult DeriveJoseConcatKey(byte[] sharedSecret, string algorithmId, byte[] partyUInfo, byte[] partyVInfo, int keySizeInBits)
    {
        return ConcatKdfOperations.DeriveKey(sharedSecret, algorithmId, partyUInfo, partyVInfo, keySizeInBits);
    }

    /// <summary>Compresses JWE plaintext using a policy-gated algorithm.</summary>
    public TokenBytesResult CompressJose(byte[] plaintext, string algorithm, TokenPolicy policy)
    {
        return JoseCompression.Compress(plaintext, algorithm, policy);
    }

    /// <summary>Decompresses JWE plaintext while enforcing expansion limits.</summary>
    public TokenBytesResult DecompressJose(byte[] compressed, string algorithm, TokenPolicy policy)
    {
        return JoseCompression.Decompress(compressed, algorithm, policy);
    }

    /// <summary>Wraps key material with JWE AES Key Wrap.</summary>
    public TokenBytesResult WrapAesKey(byte[] keyMaterial, TokenKey key, string algorithm)
    {
        return AesKeyWrap.Wrap(keyMaterial, key, algorithm);
    }

    /// <summary>Unwraps key material with JWE AES Key Wrap.</summary>
    public TokenBytesResult UnwrapAesKey(byte[] wrappedKey, TokenKey key, string algorithm)
    {
        return AesKeyWrap.Unwrap(wrappedKey, key, algorithm);
    }

    /// <summary>Wraps key material with JWE AES-GCM Key Wrap.</summary>
    public AesGcmKeyWrapResult WrapAesGcmKey(byte[] keyMaterial, TokenKey key, string algorithm)
    {
        return AesGcmKeyWrap.Wrap(keyMaterial, key, algorithm);
    }

    /// <summary>Unwraps key material with JWE AES-GCM Key Wrap.</summary>
    public TokenBytesResult UnwrapAesGcmKey(byte[] encryptedKey, byte[] initializationVector, byte[] authenticationTag, TokenKey key, string algorithm)
    {
        return AesGcmKeyWrap.Unwrap(encryptedKey, initializationVector, authenticationTag, key, algorithm);
    }

    /// <summary>Selects a JWE direct content-encryption key.</summary>
    public DirectKeyManagementResult ManageDirectKey(TokenKey key, string contentEncryptionAlgorithm)
    {
        return DirectKeyManagement.Encrypt(key, contentEncryptionAlgorithm);
    }

    /// <summary>Recovers a JWE direct content-encryption key.</summary>
    public DirectKeyManagementResult RecoverDirectKey(byte[] encryptedKey, TokenKey key, string contentEncryptionAlgorithm)
    {
        return DirectKeyManagement.Decrypt(encryptedKey, key, contentEncryptionAlgorithm);
    }

    /// <summary>Performs JWE ECDH key agreement for encryption.</summary>
    public EcdhKeyManagementResult ManageEcdhKey(TokenKey recipientKey, string algorithm, string contentEncryptionAlgorithm)
    {
        return EcdhAlgorithms.Encrypt(recipientKey, algorithm, contentEncryptionAlgorithm);
    }

    /// <summary>Performs JWE ECDH key agreement for decryption.</summary>
    public EcdhKeyManagementResult RecoverEcdhKey(byte[] encryptedKey, TokenKey recipientKey, TokenKey ephemeralKey, string algorithm, string contentEncryptionAlgorithm)
    {
        if (encryptedKey == null)
        {
            throw new ArgumentNullException("encryptedKey");
        }

        if (encryptedKey.Length == 0
            && string.Equals(algorithm, JoseAlgorithms.EcdhEs, StringComparison.Ordinal))
        {
            return EcdhAlgorithms.Decrypt(
                recipientKey,
                ephemeralKey,
                algorithm,
                contentEncryptionAlgorithm);
        }

        return EcdhAlgorithms.Decrypt(encryptedKey, recipientKey, ephemeralKey, algorithm, contentEncryptionAlgorithm);
    }

    /// <summary>Wraps a content key with PBES2.</summary>
    public Pbes2KeyWrapResult WrapPbes2Key(byte[] contentEncryptionKey, byte[] password, string algorithm, int iterationCount)
    {
        return Pbes2AlgorithmsOperations.Wrap(contentEncryptionKey, password, algorithm, iterationCount);
    }

    /// <summary>Unwraps a content key with PBES2.</summary>
    public TokenBytesResult UnwrapPbes2Key(byte[] encryptedKey, byte[] password, string algorithm, byte[] saltInput, int iterationCount)
    {
        return Pbes2AlgorithmsOperations.Unwrap(encryptedKey, password, algorithm, saltInput, iterationCount);
    }

    /// <summary>Wraps a content key with RSA key management.</summary>
    public RsaKeyManagementResult WrapRsaKey(byte[] contentEncryptionKey, TokenKey key, string algorithm, string contentEncryptionAlgorithm, bool allowCompatibility)
    {
        return RsaKeyManagement.Wrap(contentEncryptionKey, key, algorithm, contentEncryptionAlgorithm, allowCompatibility);
    }

    /// <summary>Unwraps a content key with RSA key management.</summary>
    public RsaKeyManagementResult UnwrapRsaKey(byte[] encryptedKey, TokenKey key, string algorithm, string contentEncryptionAlgorithm, bool allowCompatibility)
    {
        return RsaKeyManagement.Decrypt(encryptedKey, key, algorithm, contentEncryptionAlgorithm, allowCompatibility);
    }

    /// <summary>Encrypts content with JWE AES-CBC-HMAC-SHA2.</summary>
    public AesCbcHmacContentEncryptionResult EncryptAesCbcHmac(byte[] plaintext, byte[] additionalAuthenticatedData, TokenKey key, string algorithm)
    {
        return AesCbcHmacContentEncryption.Encrypt(plaintext, additionalAuthenticatedData, key, algorithm);
    }

    /// <summary>Encrypts content with JWE AES-CBC-HMAC-SHA2 and a supplied IV.</summary>
    public AesCbcHmacContentEncryptionResult EncryptAesCbcHmacWithInitializationVector(byte[] plaintext, byte[] initializationVector, byte[] additionalAuthenticatedData, TokenKey key, string algorithm)
    {
        return AesCbcHmacContentEncryption.Encrypt(plaintext, initializationVector, additionalAuthenticatedData, key, algorithm);
    }

    /// <summary>Decrypts JWE AES-CBC-HMAC-SHA2 content.</summary>
    public TokenBytesResult DecryptAesCbcHmac(byte[] ciphertext, byte[] initializationVector, byte[] authenticationTag, byte[] additionalAuthenticatedData, TokenKey key, string algorithm)
    {
        return AesCbcHmacContentEncryption.Decrypt(ciphertext, initializationVector, authenticationTag, additionalAuthenticatedData, key, algorithm);
    }

    /// <summary>Encrypts content with JWE AES-GCM.</summary>
    public AesGcmContentEncryptionResult EncryptAesGcm(byte[] plaintext, byte[] additionalAuthenticatedData, TokenKey key, string algorithm)
    {
        return AesGcmContentEncryption.Encrypt(plaintext, additionalAuthenticatedData, key, algorithm);
    }

    /// <summary>Encrypts content with JWE AES-GCM and a supplied IV.</summary>
    public AesGcmContentEncryptionResult EncryptAesGcmWithInitializationVector(byte[] plaintext, byte[] initializationVector, byte[] additionalAuthenticatedData, TokenKey key, string algorithm)
    {
        return AesGcmContentEncryption.Encrypt(plaintext, initializationVector, additionalAuthenticatedData, key, algorithm);
    }

    /// <summary>Decrypts JWE AES-GCM content.</summary>
    public TokenBytesResult DecryptAesGcm(byte[] ciphertext, byte[] initializationVector, byte[] authenticationTag, byte[] additionalAuthenticatedData, TokenKey key, string algorithm)
    {
        return AesGcmContentEncryption.Decrypt(ciphertext, initializationVector, authenticationTag, additionalAuthenticatedData, key, algorithm);
    }

    /// <summary>Wraps key material with COSE AES Key Wrap.</summary>
    public TokenBytesResult WrapCoseAesKey(byte[] keyMaterial, TokenKey key, int algorithm)
    {
        return CoseAesKeyWrap.Wrap(keyMaterial, key, algorithm);
    }

    /// <summary>Unwraps key material with COSE AES Key Wrap.</summary>
    public TokenBytesResult UnwrapCoseAesKey(byte[] wrappedKey, TokenKey key, int algorithm)
    {
        return CoseAesKeyWrap.Unwrap(wrappedKey, key, algorithm);
    }

    /// <summary>Distributes a content key with a COSE direct algorithm.</summary>
    public CoseDirectKeyDistributionResult DistributeCoseDirectKey(TokenKey key, int algorithm, byte[]? salt, CoseKdfContextParameters parameters)
    {
        return CoseDirectKeyDistributionOperations.Encrypt(key, algorithm, salt, parameters);
    }

    /// <summary>Recovers a content key with a COSE direct algorithm.</summary>
    public CoseDirectKeyDistributionResult RecoverCoseDirectKey(byte[] encryptedKey, TokenKey key, int algorithm, byte[]? salt, CoseKdfContextParameters parameters)
    {
        return CoseDirectKeyDistributionOperations.Decrypt(encryptedKey, key, algorithm, salt, parameters);
    }

    /// <summary>Derives a direct COSE ECDH content key.</summary>
    public CoseEcdhResult DeriveCoseEcdhKey(TokenKey recipientKey, TokenKey? senderKey, int algorithm, byte[]? salt, CoseKdfContextParameters parameters)
    {
        return CoseEcdhAlgorithms.Derive(recipientKey, senderKey, algorithm, salt, parameters);
    }

    /// <summary>Wraps a content key with COSE ECDH and AES Key Wrap.</summary>
    public CoseEcdhResult WrapCoseEcdhKey(byte[] contentKey, TokenKey recipientKey, TokenKey? senderKey, int algorithm, byte[]? salt, CoseKdfContextParameters parameters)
    {
        return CoseEcdhAlgorithms.Wrap(contentKey, recipientKey, senderKey, algorithm, salt, parameters);
    }

    /// <summary>Recovers a content key with COSE ECDH.</summary>
    public CoseEcdhResult RecoverCoseEcdhKey(byte[] encryptedKey, TokenKey recipientKey, TokenKey senderKey, int algorithm, byte[]? salt, CoseKdfContextParameters parameters)
    {
        return CoseEcdhAlgorithms.Decrypt(encryptedKey, recipientKey, senderKey, algorithm, salt, parameters);
    }

    /// <summary>Encrypts content with COSE AES-CCM.</summary>
    public CoseAesCcmResult EncryptCoseAesCcm(byte[] plaintext, byte[] additionalAuthenticatedData, TokenKey key, int algorithm)
    {
        return CoseAesCcm.Encrypt(plaintext, additionalAuthenticatedData, key, algorithm);
    }

    /// <summary>Encrypts content with COSE AES-CCM and a supplied nonce.</summary>
    public CoseAesCcmResult EncryptCoseAesCcmWithInitializationVector(byte[] plaintext, byte[] initializationVector, byte[] additionalAuthenticatedData, TokenKey key, int algorithm)
    {
        return CoseAesCcm.Encrypt(plaintext, initializationVector, additionalAuthenticatedData, key, algorithm);
    }

    /// <summary>Decrypts COSE AES-CCM content.</summary>
    public TokenBytesResult DecryptCoseAesCcm(byte[] ciphertext, byte[] initializationVector, byte[] authenticationTag, byte[] additionalAuthenticatedData, TokenKey key, int algorithm)
    {
        return CoseAesCcm.Decrypt(ciphertext, initializationVector, authenticationTag, additionalAuthenticatedData, key, algorithm);
    }

    /// <summary>Encrypts content with COSE AES-GCM.</summary>
    public CoseAesGcmResult EncryptCoseAesGcm(byte[] plaintext, byte[] additionalAuthenticatedData, TokenKey key, int algorithm)
    {
        return CoseAesGcm.Encrypt(plaintext, additionalAuthenticatedData, key, algorithm);
    }

    /// <summary>Encrypts content with COSE AES-GCM and a supplied nonce.</summary>
    public CoseAesGcmResult EncryptCoseAesGcmWithInitializationVector(byte[] plaintext, byte[] initializationVector, byte[] additionalAuthenticatedData, TokenKey key, int algorithm)
    {
        return CoseAesGcm.Encrypt(plaintext, initializationVector, additionalAuthenticatedData, key, algorithm);
    }

    /// <summary>Decrypts COSE AES-GCM content.</summary>
    public TokenBytesResult DecryptCoseAesGcm(byte[] ciphertext, byte[] initializationVector, byte[] authenticationTag, byte[] additionalAuthenticatedData, TokenKey key, int algorithm)
    {
        return CoseAesGcm.Decrypt(ciphertext, initializationVector, authenticationTag, additionalAuthenticatedData, key, algorithm);
    }

    static void ValidateSignatureReferences(byte[] data, TokenKey key)
    {
        if (data == null)
        {
            throw new ArgumentNullException("data");
        }

        if (key == null)
        {
            throw new ArgumentNullException("key");
        }
    }

    static TokenBytesResult BytesFailureFromError(TokenError? error)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = error;
        return result;
    }

    static TokenBytesResult BytesFailure(string code, string message)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }

    static TokenResult ResultFailure(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }
}
