using System;

namespace token;

/// <summary>Provides built-in JOSE and COSE cryptographic algorithm operations.</summary>
public interface ITokenAlgorithmOperations
{
    /// <summary>Creates a JWS HMAC signature.</summary>
    /// <param name="data">The bytes to authenticate.</param>
    /// <param name="key">The symmetric signing key.</param>
    /// <param name="algorithm">The JOSE HMAC algorithm name.</param>
    /// <returns>The signature, or stable failure details.</returns>
    TokenBytesResult SignJoseHmac(byte[] data, TokenKey key, string algorithm);

    /// <summary>Verifies a JWS HMAC signature.</summary>
    /// <param name="data">The authenticated bytes.</param>
    /// <param name="signature">The signature to verify.</param>
    /// <param name="key">The symmetric verification key.</param>
    /// <param name="algorithm">The JOSE HMAC algorithm name.</param>
    /// <returns>A success result, or stable failure details.</returns>
    TokenResult VerifyJoseHmac(byte[] data, byte[] signature, TokenKey key, string algorithm);

    /// <summary>Creates a JWS ECDSA signature.</summary>
    /// <param name="data">The bytes to sign.</param>
    /// <param name="key">The EC signing key.</param>
    /// <param name="algorithm">The JOSE ECDSA algorithm name.</param>
    /// <returns>The fixed-width signature, or stable failure details.</returns>
    TokenBytesResult SignJoseEcdsa(byte[] data, TokenKey key, string algorithm);

    /// <summary>Verifies a JWS ECDSA signature.</summary>
    /// <param name="data">The signed bytes.</param>
    /// <param name="signature">The fixed-width signature.</param>
    /// <param name="key">The EC verification key.</param>
    /// <param name="algorithm">The JOSE ECDSA algorithm name.</param>
    /// <returns>A success result, or stable failure details.</returns>
    TokenResult VerifyJoseEcdsa(byte[] data, byte[] signature, TokenKey key, string algorithm);

    /// <summary>Creates a JWS RSA signature.</summary>
    /// <param name="data">The bytes to sign.</param>
    /// <param name="key">The RSA signing key.</param>
    /// <param name="algorithm">The JOSE RSA algorithm name.</param>
    /// <returns>The signature, or stable failure details.</returns>
    TokenBytesResult SignJoseRsa(byte[] data, TokenKey key, string algorithm);

    /// <summary>Verifies a JWS RSA signature.</summary>
    /// <param name="data">The signed bytes.</param>
    /// <param name="signature">The signature to verify.</param>
    /// <param name="key">The RSA verification key.</param>
    /// <param name="algorithm">The JOSE RSA algorithm name.</param>
    /// <returns>A success result, or stable failure details.</returns>
    TokenResult VerifyJoseRsa(byte[] data, byte[] signature, TokenKey key, string algorithm);

    /// <summary>Creates a COSE ECDSA signature.</summary>
    /// <param name="data">The bytes to sign.</param>
    /// <param name="key">The EC signing key.</param>
    /// <param name="algorithm">The COSE ECDSA algorithm label.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic labels are allowed.</param>
    /// <returns>The fixed-width signature, or stable failure details.</returns>
    TokenBytesResult SignCoseEcdsa(byte[] data, TokenKey key, int algorithm, bool allowCompatibility);

    /// <summary>Verifies a COSE ECDSA signature.</summary>
    /// <param name="data">The signed bytes.</param>
    /// <param name="signature">The fixed-width signature.</param>
    /// <param name="key">The EC verification key.</param>
    /// <param name="algorithm">The COSE ECDSA algorithm label.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic labels are allowed.</param>
    /// <returns>A success result, or stable failure details.</returns>
    TokenResult VerifyCoseEcdsa(byte[] data, byte[] signature, TokenKey key, int algorithm, bool allowCompatibility);

    /// <summary>Creates a COSE RSA signature.</summary>
    /// <param name="data">The bytes to sign.</param>
    /// <param name="key">The RSA signing key.</param>
    /// <param name="algorithm">The COSE RSA algorithm label.</param>
    /// <returns>The signature, or stable failure details.</returns>
    TokenBytesResult SignCoseRsa(byte[] data, TokenKey key, int algorithm);

    /// <summary>Verifies a COSE RSA signature.</summary>
    /// <param name="data">The signed bytes.</param>
    /// <param name="signature">The signature to verify.</param>
    /// <param name="key">The RSA verification key.</param>
    /// <param name="algorithm">The COSE RSA algorithm label.</param>
    /// <returns>A success result, or stable failure details.</returns>
    TokenResult VerifyCoseRsa(byte[] data, byte[] signature, TokenKey key, int algorithm);

    /// <summary>Creates a COSE HMAC authentication tag.</summary>
    /// <param name="data">The bytes to authenticate.</param>
    /// <param name="key">The symmetric MAC key.</param>
    /// <param name="algorithm">The COSE HMAC algorithm label.</param>
    /// <returns>The authentication tag, or stable failure details.</returns>
    TokenBytesResult CreateCoseHmac(byte[] data, TokenKey key, int algorithm);

    /// <summary>Verifies a COSE HMAC authentication tag.</summary>
    /// <param name="data">The authenticated bytes.</param>
    /// <param name="tag">The authentication tag.</param>
    /// <param name="key">The symmetric MAC key.</param>
    /// <param name="algorithm">The COSE HMAC algorithm label.</param>
    /// <returns>A success result, or stable failure details.</returns>
    TokenResult VerifyCoseHmac(byte[] data, byte[] tag, TokenKey key, int algorithm);

    /// <summary>Derives JOSE ECDH key material with Concat KDF and SHA-256.</summary>
    /// <param name="sharedSecret">The raw shared secret.</param>
    /// <param name="algorithmId">The JOSE AlgorithmID value.</param>
    /// <param name="partyUInfo">The PartyUInfo bytes.</param>
    /// <param name="partyVInfo">The PartyVInfo bytes.</param>
    /// <param name="keySizeInBits">The requested key size in bits.</param>
    /// <returns>The derived key, or stable failure details.</returns>
    TokenBytesResult DeriveJoseConcatKey(byte[] sharedSecret, string algorithmId, byte[] partyUInfo, byte[] partyVInfo, int keySizeInBits);

    /// <summary>Compresses JWE plaintext using a policy-gated algorithm.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="algorithm">The JWE compression algorithm.</param>
    /// <param name="policy">The compression and expansion policy.</param>
    /// <returns>The compressed bytes, or stable failure details.</returns>
    TokenBytesResult CompressJose(byte[] plaintext, string algorithm, TokenPolicy policy);

    /// <summary>Decompresses JWE plaintext while enforcing expansion limits.</summary>
    /// <param name="compressed">The compressed bytes.</param>
    /// <param name="algorithm">The JWE compression algorithm.</param>
    /// <param name="policy">The compression and expansion policy.</param>
    /// <returns>The plaintext, or stable failure details.</returns>
    TokenBytesResult DecompressJose(byte[] compressed, string algorithm, TokenPolicy policy);

    /// <summary>Wraps key material with JWE AES Key Wrap.</summary>
    /// <param name="keyMaterial">The key material to wrap.</param>
    /// <param name="key">The key-encryption key.</param>
    /// <param name="algorithm">The JWE key-management algorithm.</param>
    /// <returns>The wrapped key, or stable failure details.</returns>
    TokenBytesResult WrapAesKey(byte[] keyMaterial, TokenKey key, string algorithm);

    /// <summary>Unwraps key material with JWE AES Key Wrap.</summary>
    /// <param name="wrappedKey">The wrapped key material.</param>
    /// <param name="key">The key-encryption key.</param>
    /// <param name="algorithm">The JWE key-management algorithm.</param>
    /// <returns>The unwrapped key, or stable failure details.</returns>
    TokenBytesResult UnwrapAesKey(byte[] wrappedKey, TokenKey key, string algorithm);

    /// <summary>Wraps key material with JWE AES-GCM Key Wrap.</summary>
    /// <param name="keyMaterial">The key material to wrap.</param>
    /// <param name="key">The key-encryption key.</param>
    /// <param name="algorithm">The JWE key-management algorithm.</param>
    /// <returns>The encrypted key and authenticated parameters, or stable failure details.</returns>
    AesGcmKeyWrapResult WrapAesGcmKey(byte[] keyMaterial, TokenKey key, string algorithm);

    /// <summary>Unwraps key material with JWE AES-GCM Key Wrap.</summary>
    /// <param name="encryptedKey">The encrypted key.</param>
    /// <param name="initializationVector">The key-wrap initialization vector.</param>
    /// <param name="authenticationTag">The key-wrap authentication tag.</param>
    /// <param name="key">The key-encryption key.</param>
    /// <param name="algorithm">The JWE key-management algorithm.</param>
    /// <returns>The unwrapped key, or stable failure details.</returns>
    TokenBytesResult UnwrapAesGcmKey(byte[] encryptedKey, byte[] initializationVector, byte[] authenticationTag, TokenKey key, string algorithm);

    /// <summary>Selects a JWE direct content-encryption key.</summary>
    /// <param name="key">The direct key.</param>
    /// <param name="contentEncryptionAlgorithm">The JWE content-encryption algorithm.</param>
    /// <returns>The content-encryption key, or stable failure details.</returns>
    DirectKeyManagementResult ManageDirectKey(TokenKey key, string contentEncryptionAlgorithm);

    /// <summary>Recovers a JWE direct content-encryption key.</summary>
    /// <param name="encryptedKey">The encrypted-key value, which must be empty.</param>
    /// <param name="key">The direct key.</param>
    /// <param name="contentEncryptionAlgorithm">The JWE content-encryption algorithm.</param>
    /// <returns>The content-encryption key, or stable failure details.</returns>
    DirectKeyManagementResult RecoverDirectKey(byte[] encryptedKey, TokenKey key, string contentEncryptionAlgorithm);

    /// <summary>Performs JWE ECDH key agreement for encryption.</summary>
    /// <param name="recipientKey">The recipient public key.</param>
    /// <param name="algorithm">The JWE key-management algorithm.</param>
    /// <param name="contentEncryptionAlgorithm">The JWE content-encryption algorithm.</param>
    /// <returns>The managed key and ephemeral public key, or stable failure details.</returns>
    EcdhKeyManagementResult ManageEcdhKey(TokenKey recipientKey, string algorithm, string contentEncryptionAlgorithm);

    /// <summary>Performs JWE ECDH key agreement for decryption.</summary>
    /// <param name="encryptedKey">The empty or wrapped encrypted-key value.</param>
    /// <param name="recipientKey">The recipient private key.</param>
    /// <param name="ephemeralKey">The sender ephemeral public key.</param>
    /// <param name="algorithm">The JWE key-management algorithm.</param>
    /// <param name="contentEncryptionAlgorithm">The JWE content-encryption algorithm.</param>
    /// <returns>The recovered content key, or stable failure details.</returns>
    EcdhKeyManagementResult RecoverEcdhKey(byte[] encryptedKey, TokenKey recipientKey, TokenKey ephemeralKey, string algorithm, string contentEncryptionAlgorithm);

    /// <summary>Wraps a content key with PBES2.</summary>
    /// <param name="contentEncryptionKey">The key to wrap.</param>
    /// <param name="password">The password octets.</param>
    /// <param name="algorithm">The PBES2 algorithm.</param>
    /// <param name="iterationCount">The PBKDF2 iteration count.</param>
    /// <returns>The wrapped key and PBES2 parameters, or stable failure details.</returns>
    Pbes2KeyWrapResult WrapPbes2Key(byte[] contentEncryptionKey, byte[] password, string algorithm, int iterationCount);

    /// <summary>Unwraps a content key with PBES2.</summary>
    /// <param name="encryptedKey">The wrapped key.</param>
    /// <param name="password">The password octets.</param>
    /// <param name="algorithm">The PBES2 algorithm.</param>
    /// <param name="saltInput">The PBES2 salt input.</param>
    /// <param name="iterationCount">The PBKDF2 iteration count.</param>
    /// <returns>The unwrapped key, or stable failure details.</returns>
    TokenBytesResult UnwrapPbes2Key(byte[] encryptedKey, byte[] password, string algorithm, byte[] saltInput, int iterationCount);

    /// <summary>Wraps a content key with RSA key management.</summary>
    /// <param name="contentEncryptionKey">The key to wrap.</param>
    /// <param name="key">The RSA public key.</param>
    /// <param name="algorithm">The RSA key-management algorithm.</param>
    /// <param name="contentEncryptionAlgorithm">The JWE content-encryption algorithm.</param>
    /// <param name="allowCompatibility">Whether RSA1_5 is explicitly allowed.</param>
    /// <returns>The wrapped key, or stable failure details.</returns>
    RsaKeyManagementResult WrapRsaKey(byte[] contentEncryptionKey, TokenKey key, string algorithm, string contentEncryptionAlgorithm, bool allowCompatibility);

    /// <summary>Unwraps a content key with RSA key management.</summary>
    /// <param name="encryptedKey">The encrypted key.</param>
    /// <param name="key">The RSA private key.</param>
    /// <param name="algorithm">The RSA key-management algorithm.</param>
    /// <param name="contentEncryptionAlgorithm">The JWE content-encryption algorithm.</param>
    /// <param name="allowCompatibility">Whether RSA1_5 is explicitly allowed.</param>
    /// <returns>The unwrapped key, or stable failure details.</returns>
    RsaKeyManagementResult UnwrapRsaKey(byte[] encryptedKey, TokenKey key, string algorithm, string contentEncryptionAlgorithm, bool allowCompatibility);

    /// <summary>Encrypts content with JWE AES-CBC-HMAC-SHA2.</summary>
    /// <param name="plaintext">The plaintext.</param>
    /// <param name="additionalAuthenticatedData">The authenticated bytes.</param>
    /// <param name="key">The content-encryption key.</param>
    /// <param name="algorithm">The JWE content-encryption algorithm.</param>
    /// <returns>The authenticated ciphertext, or stable failure details.</returns>
    AesCbcHmacContentEncryptionResult EncryptAesCbcHmac(byte[] plaintext, byte[] additionalAuthenticatedData, TokenKey key, string algorithm);

    /// <summary>Encrypts content with JWE AES-CBC-HMAC-SHA2 and a supplied IV.</summary>
    /// <param name="plaintext">The plaintext.</param>
    /// <param name="initializationVector">The initialization vector.</param>
    /// <param name="additionalAuthenticatedData">The authenticated bytes.</param>
    /// <param name="key">The content-encryption key.</param>
    /// <param name="algorithm">The JWE content-encryption algorithm.</param>
    /// <returns>The authenticated ciphertext, or stable failure details.</returns>
    AesCbcHmacContentEncryptionResult EncryptAesCbcHmacWithInitializationVector(byte[] plaintext, byte[] initializationVector, byte[] additionalAuthenticatedData, TokenKey key, string algorithm);

    /// <summary>Decrypts JWE AES-CBC-HMAC-SHA2 content.</summary>
    /// <param name="ciphertext">The ciphertext.</param>
    /// <param name="initializationVector">The initialization vector.</param>
    /// <param name="authenticationTag">The authentication tag.</param>
    /// <param name="additionalAuthenticatedData">The authenticated bytes.</param>
    /// <param name="key">The content-encryption key.</param>
    /// <param name="algorithm">The JWE content-encryption algorithm.</param>
    /// <returns>The authenticated plaintext, or stable failure details.</returns>
    TokenBytesResult DecryptAesCbcHmac(byte[] ciphertext, byte[] initializationVector, byte[] authenticationTag, byte[] additionalAuthenticatedData, TokenKey key, string algorithm);

    /// <summary>Encrypts content with JWE AES-GCM.</summary>
    /// <param name="plaintext">The plaintext.</param>
    /// <param name="additionalAuthenticatedData">The authenticated bytes.</param>
    /// <param name="key">The content-encryption key.</param>
    /// <param name="algorithm">The JWE content-encryption algorithm.</param>
    /// <returns>The authenticated ciphertext, or stable failure details.</returns>
    AesGcmContentEncryptionResult EncryptAesGcm(byte[] plaintext, byte[] additionalAuthenticatedData, TokenKey key, string algorithm);

    /// <summary>Encrypts content with JWE AES-GCM and a supplied IV.</summary>
    /// <param name="plaintext">The plaintext.</param>
    /// <param name="initializationVector">The initialization vector.</param>
    /// <param name="additionalAuthenticatedData">The authenticated bytes.</param>
    /// <param name="key">The content-encryption key.</param>
    /// <param name="algorithm">The JWE content-encryption algorithm.</param>
    /// <returns>The authenticated ciphertext, or stable failure details.</returns>
    AesGcmContentEncryptionResult EncryptAesGcmWithInitializationVector(byte[] plaintext, byte[] initializationVector, byte[] additionalAuthenticatedData, TokenKey key, string algorithm);

    /// <summary>Decrypts JWE AES-GCM content.</summary>
    /// <param name="ciphertext">The ciphertext.</param>
    /// <param name="initializationVector">The initialization vector.</param>
    /// <param name="authenticationTag">The authentication tag.</param>
    /// <param name="additionalAuthenticatedData">The authenticated bytes.</param>
    /// <param name="key">The content-encryption key.</param>
    /// <param name="algorithm">The JWE content-encryption algorithm.</param>
    /// <returns>The authenticated plaintext, or stable failure details.</returns>
    TokenBytesResult DecryptAesGcm(byte[] ciphertext, byte[] initializationVector, byte[] authenticationTag, byte[] additionalAuthenticatedData, TokenKey key, string algorithm);

    /// <summary>Wraps key material with COSE AES Key Wrap.</summary>
    /// <param name="keyMaterial">The key material to wrap.</param>
    /// <param name="key">The key-encryption key.</param>
    /// <param name="algorithm">The COSE AES Key Wrap label.</param>
    /// <returns>The wrapped key, or stable failure details.</returns>
    TokenBytesResult WrapCoseAesKey(byte[] keyMaterial, TokenKey key, int algorithm);

    /// <summary>Unwraps key material with COSE AES Key Wrap.</summary>
    /// <param name="wrappedKey">The wrapped key material.</param>
    /// <param name="key">The key-encryption key.</param>
    /// <param name="algorithm">The COSE AES Key Wrap label.</param>
    /// <returns>The unwrapped key, or stable failure details.</returns>
    TokenBytesResult UnwrapCoseAesKey(byte[] wrappedKey, TokenKey key, int algorithm);

    /// <summary>Distributes a content key with a COSE direct algorithm.</summary>
    /// <param name="key">The direct key material.</param>
    /// <param name="algorithm">The COSE direct algorithm label.</param>
    /// <param name="salt">The optional HKDF salt.</param>
    /// <param name="parameters">The COSE KDF context.</param>
    /// <returns>The selected content key, or stable failure details.</returns>
    CoseDirectKeyDistributionResult DistributeCoseDirectKey(TokenKey key, int algorithm, byte[]? salt, CoseKdfContextParameters parameters);

    /// <summary>Recovers a content key with a COSE direct algorithm.</summary>
    /// <param name="encryptedKey">The recipient ciphertext, which must be empty.</param>
    /// <param name="key">The direct key material.</param>
    /// <param name="algorithm">The COSE direct algorithm label.</param>
    /// <param name="salt">The optional HKDF salt.</param>
    /// <param name="parameters">The COSE KDF context.</param>
    /// <returns>The selected content key, or stable failure details.</returns>
    CoseDirectKeyDistributionResult RecoverCoseDirectKey(byte[] encryptedKey, TokenKey key, int algorithm, byte[]? salt, CoseKdfContextParameters parameters);

    /// <summary>Derives a direct COSE ECDH content key.</summary>
    /// <param name="recipientKey">The recipient public key.</param>
    /// <param name="senderKey">The optional static sender private key.</param>
    /// <param name="algorithm">The COSE ECDH algorithm label.</param>
    /// <param name="salt">The optional HKDF salt.</param>
    /// <param name="parameters">The COSE KDF context.</param>
    /// <returns>The derived key and sender public key, or stable failure details.</returns>
    CoseEcdhResult DeriveCoseEcdhKey(TokenKey recipientKey, TokenKey? senderKey, int algorithm, byte[]? salt, CoseKdfContextParameters parameters);

    /// <summary>Wraps a content key with COSE ECDH and AES Key Wrap.</summary>
    /// <param name="contentKey">The content key.</param>
    /// <param name="recipientKey">The recipient public key.</param>
    /// <param name="senderKey">The optional static sender private key.</param>
    /// <param name="algorithm">The COSE ECDH algorithm label.</param>
    /// <param name="salt">The optional HKDF salt.</param>
    /// <param name="parameters">The COSE KDF context.</param>
    /// <returns>The wrapped key and sender public key, or stable failure details.</returns>
    CoseEcdhResult WrapCoseEcdhKey(byte[] contentKey, TokenKey recipientKey, TokenKey? senderKey, int algorithm, byte[]? salt, CoseKdfContextParameters parameters);

    /// <summary>Recovers a content key with COSE ECDH.</summary>
    /// <param name="encryptedKey">The empty or wrapped recipient ciphertext.</param>
    /// <param name="recipientKey">The recipient private key.</param>
    /// <param name="senderKey">The sender public key.</param>
    /// <param name="algorithm">The COSE ECDH algorithm label.</param>
    /// <param name="salt">The optional HKDF salt.</param>
    /// <param name="parameters">The COSE KDF context.</param>
    /// <returns>The recovered content key, or stable failure details.</returns>
    CoseEcdhResult RecoverCoseEcdhKey(byte[] encryptedKey, TokenKey recipientKey, TokenKey senderKey, int algorithm, byte[]? salt, CoseKdfContextParameters parameters);

    /// <summary>Encrypts content with COSE AES-CCM.</summary>
    /// <param name="plaintext">The plaintext.</param>
    /// <param name="additionalAuthenticatedData">The authenticated bytes.</param>
    /// <param name="key">The content-encryption key.</param>
    /// <param name="algorithm">The COSE AES-CCM label.</param>
    /// <returns>The authenticated ciphertext, or stable failure details.</returns>
    CoseAesCcmResult EncryptCoseAesCcm(byte[] plaintext, byte[] additionalAuthenticatedData, TokenKey key, int algorithm);

    /// <summary>Encrypts content with COSE AES-CCM and a supplied nonce.</summary>
    /// <param name="plaintext">The plaintext.</param>
    /// <param name="initializationVector">The nonce.</param>
    /// <param name="additionalAuthenticatedData">The authenticated bytes.</param>
    /// <param name="key">The content-encryption key.</param>
    /// <param name="algorithm">The COSE AES-CCM label.</param>
    /// <returns>The authenticated ciphertext, or stable failure details.</returns>
    CoseAesCcmResult EncryptCoseAesCcmWithInitializationVector(byte[] plaintext, byte[] initializationVector, byte[] additionalAuthenticatedData, TokenKey key, int algorithm);

    /// <summary>Decrypts COSE AES-CCM content.</summary>
    /// <param name="ciphertext">The ciphertext.</param>
    /// <param name="initializationVector">The nonce.</param>
    /// <param name="authenticationTag">The authentication tag.</param>
    /// <param name="additionalAuthenticatedData">The authenticated bytes.</param>
    /// <param name="key">The content-encryption key.</param>
    /// <param name="algorithm">The COSE AES-CCM label.</param>
    /// <returns>The authenticated plaintext, or stable failure details.</returns>
    TokenBytesResult DecryptCoseAesCcm(byte[] ciphertext, byte[] initializationVector, byte[] authenticationTag, byte[] additionalAuthenticatedData, TokenKey key, int algorithm);

    /// <summary>Encrypts content with COSE AES-GCM.</summary>
    /// <param name="plaintext">The plaintext.</param>
    /// <param name="additionalAuthenticatedData">The authenticated bytes.</param>
    /// <param name="key">The content-encryption key.</param>
    /// <param name="algorithm">The COSE AES-GCM label.</param>
    /// <returns>The authenticated ciphertext, or stable failure details.</returns>
    CoseAesGcmResult EncryptCoseAesGcm(byte[] plaintext, byte[] additionalAuthenticatedData, TokenKey key, int algorithm);

    /// <summary>Encrypts content with COSE AES-GCM and a supplied nonce.</summary>
    /// <param name="plaintext">The plaintext.</param>
    /// <param name="initializationVector">The nonce.</param>
    /// <param name="additionalAuthenticatedData">The authenticated bytes.</param>
    /// <param name="key">The content-encryption key.</param>
    /// <param name="algorithm">The COSE AES-GCM label.</param>
    /// <returns>The authenticated ciphertext, or stable failure details.</returns>
    CoseAesGcmResult EncryptCoseAesGcmWithInitializationVector(byte[] plaintext, byte[] initializationVector, byte[] additionalAuthenticatedData, TokenKey key, int algorithm);

    /// <summary>Decrypts COSE AES-GCM content.</summary>
    /// <param name="ciphertext">The ciphertext.</param>
    /// <param name="initializationVector">The nonce.</param>
    /// <param name="authenticationTag">The authentication tag.</param>
    /// <param name="additionalAuthenticatedData">The authenticated bytes.</param>
    /// <param name="key">The content-encryption key.</param>
    /// <param name="algorithm">The COSE AES-GCM label.</param>
    /// <returns>The authenticated plaintext, or stable failure details.</returns>
    TokenBytesResult DecryptCoseAesGcm(byte[] ciphertext, byte[] initializationVector, byte[] authenticationTag, byte[] additionalAuthenticatedData, TokenKey key, int algorithm);
}
