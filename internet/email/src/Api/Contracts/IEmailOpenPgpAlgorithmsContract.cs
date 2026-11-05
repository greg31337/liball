using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpAlgorithms"/>.
/// </summary>
public interface IEmailOpenPgpAlgorithmsContract
{
    /// <summary>
    /// Gets the support.
    /// </summary>
    /// <param name = "category">The category used by the operation.</param>
    /// <param name = "identifier">The identifier used by the operation.</param>
    /// <returns>The current support.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailOpenPgpAlgorithmSupport GetSupport(EmailOpenPgpAlgorithmCategory category, int identifier);
    /// <summary>
    /// Computes the hash.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "hashAlgorithm">The hash algorithm used by the operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    EmailOpenPgpOperationResult ComputeHashWithDataAndHashAlgorithm(ReadOnlySpan<byte> data, int hashAlgorithm);
    /// <summary>
    /// Computes the hash.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "hashAlgorithm">The hash algorithm used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    EmailOpenPgpOperationResult ComputeHashWithDataAndHashAlgorithmAndOptions(ReadOnlySpan<byte> data, int hashAlgorithm, EmailOpenPgpAlgorithmOptions options);
    /// <summary>
    /// Computes the hash for verification.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "hashAlgorithm">The hash algorithm used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    EmailOpenPgpOperationResult ComputeHashForVerification(ReadOnlySpan<byte> data, int hashAlgorithm, EmailOpenPgpAlgorithmOptions options);
    /// <summary>
    /// Compresses the supplied value.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "compressionAlgorithm">The compression algorithm used by the operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    EmailOpenPgpOperationResult CompressWithDataAndCompressionAlgorithm(ReadOnlySpan<byte> data, int compressionAlgorithm);
    /// <summary>
    /// Compresses the supplied value.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "compressionAlgorithm">The compression algorithm used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailOpenPgpOperationResult CompressWithDataAndCompressionAlgorithmAndOptions(ReadOnlySpan<byte> data, int compressionAlgorithm, EmailOpenPgpAlgorithmOptions options);
    /// <summary>
    /// Decompresses the supplied value.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "compressionAlgorithm">The compression algorithm used by the operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    EmailOpenPgpOperationResult? DecompressWithDataAndCompressionAlgorithm(ReadOnlySpan<byte> data, int compressionAlgorithm);
    /// <summary>
    /// Decompresses the supplied value.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "compressionAlgorithm">The compression algorithm used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailOpenPgpOperationResult? DecompressWithDataAndCompressionAlgorithmAndOptions(ReadOnlySpan<byte> data, int compressionAlgorithm, EmailOpenPgpAlgorithmOptions options);
    /// <summary>
    /// Derives the string to key.
    /// </summary>
    /// <param name = "passphrase">The passphrase used by the operation.</param>
    /// <param name = "specifier">The specifier used by the operation.</param>
    /// <param name = "keyBytes">The source bytes or data to process.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    EmailOpenPgpOperationResult? DeriveStringToKeyWithPassphraseAndSpecifierAndKeyBytes(ReadOnlySpan<byte> passphrase, ReadOnlySpan<byte> specifier, int keyBytes);
    /// <summary>
    /// Derives the string to key.
    /// </summary>
    /// <param name = "passphrase">The passphrase used by the operation.</param>
    /// <param name = "specifier">The specifier used by the operation.</param>
    /// <param name = "keyBytes">The source bytes or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailOpenPgpOperationResult? DeriveStringToKeyWithPassphraseAndSpecifierAndKeyBytesAndOptions(ReadOnlySpan<byte> passphrase, ReadOnlySpan<byte> specifier, int keyBytes, EmailOpenPgpAlgorithmOptions options);
    /// <summary>
    /// Signs the hash.
    /// </summary>
    /// <param name = "hash">The hash used by the operation.</param>
    /// <param name = "hashAlgorithm">The hash algorithm used by the operation.</param>
    /// <param name = "publicKeyAlgorithm">The public key algorithm used by the operation.</param>
    /// <param name = "key">The key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailOpenPgpOperationResult? SignHashWithHashAndHashAlgorithmAndPublicKeyAlgorithmAndKeyAndOptions(ReadOnlySpan<byte> hash, int hashAlgorithm, int publicKeyAlgorithm, AsymmetricAlgorithm key, EmailOpenPgpAlgorithmOptions options);
    /// <summary>
    /// Signs the hash.
    /// </summary>
    /// <param name = "hash">The hash used by the operation.</param>
    /// <param name = "hashAlgorithm">The hash algorithm used by the operation.</param>
    /// <param name = "publicKeyAlgorithm">The public key algorithm used by the operation.</param>
    /// <param name = "key">The key used by the operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    EmailOpenPgpOperationResult? SignHashWithHashAndHashAlgorithmAndPublicKeyAlgorithmAndKey(ReadOnlySpan<byte> hash, int hashAlgorithm, int publicKeyAlgorithm, AsymmetricAlgorithm key);
    /// <summary>
    /// Verifies the hash.
    /// </summary>
    /// <param name = "hash">The hash used by the operation.</param>
    /// <param name = "signatureMaterial">The signature material used by the operation.</param>
    /// <param name = "hashAlgorithm">The hash algorithm used by the operation.</param>
    /// <param name = "publicKeyAlgorithm">The public key algorithm used by the operation.</param>
    /// <param name = "key">The key used by the operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    EmailOpenPgpOperationResult? VerifyHashWithHashAndSignatureMaterialAndHashAlgorithmAndPublicKeyAlgorithmAndKey(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signatureMaterial, int hashAlgorithm, int publicKeyAlgorithm, AsymmetricAlgorithm key);
    /// <summary>
    /// Verifies the hash.
    /// </summary>
    /// <param name = "hash">The hash used by the operation.</param>
    /// <param name = "signatureMaterial">The signature material used by the operation.</param>
    /// <param name = "hashAlgorithm">The hash algorithm used by the operation.</param>
    /// <param name = "publicKeyAlgorithm">The public key algorithm used by the operation.</param>
    /// <param name = "key">The key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailOpenPgpOperationResult? VerifyHashWithHashAndSignatureMaterialAndHashAlgorithmAndPublicKeyAlgorithmAndKeyAndOptions(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signatureMaterial, int hashAlgorithm, int publicKeyAlgorithm, AsymmetricAlgorithm key, EmailOpenPgpAlgorithmOptions options);
    /// <summary>
    /// Encrypts the session key.
    /// </summary>
    /// <param name = "sessionKeyData">The session key data used by the operation.</param>
    /// <param name = "publicKeyAlgorithm">The public key algorithm used by the operation.</param>
    /// <param name = "key">The key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailOpenPgpOperationResult? EncryptSessionKeyWithSessionKeyDataAndPublicKeyAlgorithmAndKeyAndOptions(ReadOnlySpan<byte> sessionKeyData, int publicKeyAlgorithm, RSA key, EmailOpenPgpAlgorithmOptions options);
    /// <summary>
    /// Encrypts the session key.
    /// </summary>
    /// <param name = "sessionKeyData">The session key data used by the operation.</param>
    /// <param name = "publicKeyAlgorithm">The public key algorithm used by the operation.</param>
    /// <param name = "key">The key used by the operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    EmailOpenPgpOperationResult? EncryptSessionKeyWithSessionKeyDataAndPublicKeyAlgorithmAndKey(ReadOnlySpan<byte> sessionKeyData, int publicKeyAlgorithm, RSA key);
    /// <summary>
    /// Decrypts the session key.
    /// </summary>
    /// <param name = "encryptedSessionKey">The encrypted session key used by the operation.</param>
    /// <param name = "publicKeyAlgorithm">The public key algorithm used by the operation.</param>
    /// <param name = "key">The key used by the operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    EmailOpenPgpOperationResult DecryptSessionKeyWithEncryptedSessionKeyAndPublicKeyAlgorithmAndKey(ReadOnlySpan<byte> encryptedSessionKey, int publicKeyAlgorithm, RSA key);
    /// <summary>
    /// Decrypts the session key.
    /// </summary>
    /// <param name = "encryptedSessionKey">The encrypted session key used by the operation.</param>
    /// <param name = "publicKeyAlgorithm">The public key algorithm used by the operation.</param>
    /// <param name = "key">The key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailOpenPgpOperationResult DecryptSessionKeyWithEncryptedSessionKeyAndPublicKeyAlgorithmAndKeyAndOptions(ReadOnlySpan<byte> encryptedSessionKey, int publicKeyAlgorithm, RSA key, EmailOpenPgpAlgorithmOptions options);
    /// <summary>
    /// Encrypts the aead.
    /// </summary>
    /// <param name = "plaintext">The plaintext used by the operation.</param>
    /// <param name = "key">The key used by the operation.</param>
    /// <param name = "symmetricAlgorithm">The symmetric algorithm used by the operation.</param>
    /// <param name = "aeadAlgorithm">The aead algorithm used by the operation.</param>
    /// <param name = "nonce">The nonce used by the operation.</param>
    /// <param name = "associatedData">The associated data used by the operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    EmailOpenPgpOperationResult EncryptAeadWithPlaintextAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedData(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key, int symmetricAlgorithm, int aeadAlgorithm, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> associatedData);
    /// <summary>
    /// Encrypts the aead.
    /// </summary>
    /// <param name = "plaintext">The plaintext used by the operation.</param>
    /// <param name = "key">The key used by the operation.</param>
    /// <param name = "symmetricAlgorithm">The symmetric algorithm used by the operation.</param>
    /// <param name = "aeadAlgorithm">The aead algorithm used by the operation.</param>
    /// <param name = "nonce">The nonce used by the operation.</param>
    /// <param name = "associatedData">The associated data used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailOpenPgpOperationResult EncryptAeadWithPlaintextAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedDataAndOptions(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key, int symmetricAlgorithm, int aeadAlgorithm, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> associatedData, EmailOpenPgpAlgorithmOptions options);
    /// <summary>
    /// Decrypts the aead.
    /// </summary>
    /// <param name = "encrypted">The encrypted used by the operation.</param>
    /// <param name = "key">The key used by the operation.</param>
    /// <param name = "symmetricAlgorithm">The symmetric algorithm used by the operation.</param>
    /// <param name = "aeadAlgorithm">The aead algorithm used by the operation.</param>
    /// <param name = "nonce">The nonce used by the operation.</param>
    /// <param name = "associatedData">The associated data used by the operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    EmailOpenPgpOperationResult DecryptAeadWithEncryptedAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedData(ReadOnlySpan<byte> encrypted, ReadOnlySpan<byte> key, int symmetricAlgorithm, int aeadAlgorithm, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> associatedData);
    /// <summary>
    /// Decrypts the aead.
    /// </summary>
    /// <param name = "encrypted">The encrypted used by the operation.</param>
    /// <param name = "key">The key used by the operation.</param>
    /// <param name = "symmetricAlgorithm">The symmetric algorithm used by the operation.</param>
    /// <param name = "aeadAlgorithm">The aead algorithm used by the operation.</param>
    /// <param name = "nonce">The nonce used by the operation.</param>
    /// <param name = "associatedData">The associated data used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailOpenPgpOperationResult DecryptAeadWithEncryptedAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedDataAndOptions(ReadOnlySpan<byte> encrypted, ReadOnlySpan<byte> key, int symmetricAlgorithm, int aeadAlgorithm, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> associatedData, EmailOpenPgpAlgorithmOptions options);
}
