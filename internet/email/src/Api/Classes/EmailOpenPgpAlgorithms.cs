using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Provides operations for working with openPGP algorithms.
/// </summary>
public class EmailOpenPgpAlgorithms : IEmailOpenPgpAlgorithmsContract
{
    /// <summary>
    /// The public key RSA value exposed by this API.
    /// </summary>
    internal const int PublicKeyRsa = 1;
    /// <summary>
    /// The public key RSA encrypt only value exposed by this API.
    /// </summary>
    internal const int PublicKeyRsaEncryptOnly = 2;
    /// <summary>
    /// The public key RSA sign only value exposed by this API.
    /// </summary>
    internal const int PublicKeyRsaSignOnly = 3;
    /// <summary>
    /// The public key ECDSA value exposed by this API.
    /// </summary>
    internal const int PublicKeyEcdsa = 19;
    /// <summary>
    /// The public key x 25519 value exposed by this API.
    /// </summary>
    internal const int PublicKeyX25519 = 25;
    /// <summary>
    /// The public key ed 25519 value exposed by this API.
    /// </summary>
    internal const int PublicKeyEd25519 = 27;
    /// <summary>
    /// The symmetric aes 128 value exposed by this API.
    /// </summary>
    internal const int SymmetricAes128 = 7;
    /// <summary>
    /// The symmetric aes 192 value exposed by this API.
    /// </summary>
    internal const int SymmetricAes192 = 8;
    /// <summary>
    /// The symmetric aes 256 value exposed by this API.
    /// </summary>
    internal const int SymmetricAes256 = 9;
    /// <summary>
    /// The compression uncompressed value exposed by this API.
    /// </summary>
    internal const int CompressionUncompressed = 0;
    /// <summary>
    /// The compression zip value exposed by this API.
    /// </summary>
    internal const int CompressionZip = 1;
    /// <summary>
    /// The compression zlib value exposed by this API.
    /// </summary>
    internal const int CompressionZlib = 2;
    /// <summary>
    /// The hash sha 1 value exposed by this API.
    /// </summary>
    internal const int HashSha1 = 2;
    /// <summary>
    /// The hash sha 256 value exposed by this API.
    /// </summary>
    internal const int HashSha256 = 8;
    /// <summary>
    /// The hash sha 384 value exposed by this API.
    /// </summary>
    internal const int HashSha384 = 9;
    /// <summary>
    /// The hash sha 512 value exposed by this API.
    /// </summary>
    internal const int HashSha512 = 10;
    /// <summary>
    /// The hash sha 3_256 value exposed by this API.
    /// </summary>
    internal const int HashSha3_256 = 12;
    /// <summary>
    /// The hash sha 3_512 value exposed by this API.
    /// </summary>
    internal const int HashSha3_512 = 14;
    /// <summary>
    /// The aead GCM value exposed by this API.
    /// </summary>
    internal const int AeadGcm = 3;
    internal static EmailOpenPgpAlgorithmSupport GetSupportImplementation(EmailOpenPgpAlgorithmCategory category, int identifier)
    {
        if (identifier < 0 || identifier > 255)
            throw new ArgumentOutOfRangeException("identifier");
        if (category == EmailOpenPgpAlgorithmCategory.Packet)
        {
            bool supported = identifier == 1 || identifier == 2 || identifier == 3 || identifier == 4 || identifier == 5 || identifier == 6 || identifier == 7 || identifier == 8 || identifier == 11 || identifier == 14 || identifier == 18 || identifier == 21;
            return Support(category, identifier, supported ? ((EmailOpenPgpPacketKind)identifier).ToString() : "Packet " + identifier, supported, false, supported ? "The packet has an implemented typed or primitive operation." : "No primitive operation is implemented for this packet type.");
        }

        if (category == EmailOpenPgpAlgorithmCategory.PublicKey)
        {
            if (identifier == 1)
                return Support(category, identifier, "RSA", true, true, "RSA is available through the BCL but deprecated for new OpenPGP keys.");
            if (identifier == 2)
                return Support(category, identifier, "RSA Encrypt-Only", true, true, "RSA Encrypt-Only is accepted for legacy interoperability and never enabled for generation by default.");
            if (identifier == 3)
                return Support(category, identifier, "RSA Sign-Only", true, true, "RSA Sign-Only is accepted for legacy interoperability and never enabled for generation by default.");
            if (identifier == 19)
                return Support(category, identifier, "ECDSA", true, false, "ECDSA is implemented with caller-supplied BCL keys.");
            return Support(category, identifier, PublicKeyName(identifier), false, identifier == 16 || identifier == 17 || identifier == 22, "The target runtime does not expose the complete safe primitive set required by this OpenPGP public-key algorithm.");
        }

        if (category == EmailOpenPgpAlgorithmCategory.Symmetric)
        {
            if (identifier == 7 || identifier == 8 || identifier == 9)
                return Support(category, identifier, SymmetricName(identifier), true, false, "AES is implemented by the BCL.");
            return Support(category, identifier, SymmetricName(identifier), false, identifier >= 1 && identifier <= 4, "Only AES-128, AES-192, and AES-256 are enabled.");
        }

        if (category == EmailOpenPgpAlgorithmCategory.Compression)
        {
            if (identifier >= 0 && identifier <= 2)
                return Support(category, identifier, CompressionName(identifier), true, false, "The compression format is implemented by BCL streams.");
            return Support(category, identifier, CompressionName(identifier), false, false, "The BCL does not provide this OpenPGP compression format.");
        }

        if (category == EmailOpenPgpAlgorithmCategory.Hash)
        {
            if (identifier == 2)
                return Support(category, identifier, "SHA-1", true, true, "SHA-1 is available only for explicitly enabled legacy verification.");
            if (identifier == 8 || identifier == 9 || identifier == 10)
                return Support(category, identifier, HashName(identifier), true, false, "The SHA-2 hash is implemented by the BCL.");
            if (identifier == 12)
                return Support(category, identifier, "SHA3-256", SHA3_256.IsSupported, false, SHA3_256.IsSupported ? "SHA3-256 is implemented by the runtime." : "SHA3-256 is unavailable from the runtime provider.");
            if (identifier == 14)
                return Support(category, identifier, "SHA3-512", SHA3_512.IsSupported, false, SHA3_512.IsSupported ? "SHA3-512 is implemented by the runtime." : "SHA3-512 is unavailable from the runtime provider.");
            return Support(category, identifier, HashName(identifier), false, identifier == 1 || identifier == 3, "The hash is unsafe, reserved, or unavailable from the runtime provider.");
        }

        if (category == EmailOpenPgpAlgorithmCategory.Aead)
        {
            if (identifier == 3)
                return Support(category, identifier, "GCM", AesGcm.IsSupported, false, AesGcm.IsSupported ? "AES-GCM is implemented by the runtime." : "AES-GCM is unavailable from the runtime provider.");
            return Support(category, identifier, identifier == 1 ? "EAX" : identifier == 2 ? "OCB" : "AEAD " + identifier, false, false, "The runtime does not expose this AEAD mode through a safe BCL primitive.");
        }

        if (category == EmailOpenPgpAlgorithmCategory.StringToKey)
        {
            if (identifier == 0 || identifier == 1)
                return Support(category, identifier, identifier == 0 ? "Simple S2K" : "Salted S2K", true, true, "The legacy S2K is implemented for explicitly enabled reading only.");
            if (identifier == 3)
                return Support(category, identifier, "Iterated and Salted S2K", true, false, "The iterated S2K is implemented with bounded incremental hashing.");
            return Support(category, identifier, identifier == 4 ? "Argon2 S2K" : "S2K " + identifier, false, false, "The runtime has no built-in Argon2id primitive and private S2K types are not interpreted.");
        }

        return Support(category, identifier, "Algorithm " + identifier, false, false, "The algorithm category is unsupported.");
    }

    /// <summary>
    /// Gets the support.
    /// </summary>
    /// <param name = "category">The category used by the operation.</param>
    /// <param name = "identifier">The identifier used by the operation.</param>
    /// <returns>The current support.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailOpenPgpAlgorithmSupport GetSupport(EmailOpenPgpAlgorithmCategory category, int identifier)
    {
        return EmailOpenPgpAlgorithms.GetSupportImplementation(category, identifier);
    }

    internal static EmailOpenPgpOperationResult ComputeHashImplementationWithDataAndHashAlgorithm(ReadOnlySpan<byte> data, int hashAlgorithm)
    {
        return ComputeHashWithDataAndHashAlgorithmAndVerificationAndOptions(data, hashAlgorithm, false, new EmailOpenPgpAlgorithmOptions());
    }

    /// <summary>
    /// Computes the hash.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "hashAlgorithm">The hash algorithm used by the operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    public EmailOpenPgpOperationResult ComputeHashWithDataAndHashAlgorithm(ReadOnlySpan<byte> data, int hashAlgorithm)
    {
        return EmailOpenPgpAlgorithms.ComputeHashImplementationWithDataAndHashAlgorithm(data, hashAlgorithm);
    }

    internal static EmailOpenPgpOperationResult ComputeHashImplementationWithDataAndHashAlgorithmAndOptions(ReadOnlySpan<byte> data, int hashAlgorithm, EmailOpenPgpAlgorithmOptions options)
    {
        return ComputeHashWithDataAndHashAlgorithmAndVerificationAndOptions(data, hashAlgorithm, false, options);
    }

    /// <summary>
    /// Computes the hash.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "hashAlgorithm">The hash algorithm used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    public EmailOpenPgpOperationResult ComputeHashWithDataAndHashAlgorithmAndOptions(ReadOnlySpan<byte> data, int hashAlgorithm, EmailOpenPgpAlgorithmOptions options)
    {
        return EmailOpenPgpAlgorithms.ComputeHashImplementationWithDataAndHashAlgorithmAndOptions(data, hashAlgorithm, options);
    }

    internal static EmailOpenPgpOperationResult ComputeHashForVerificationImplementation(ReadOnlySpan<byte> data, int hashAlgorithm, EmailOpenPgpAlgorithmOptions options)
    {
        return ComputeHashWithDataAndHashAlgorithmAndVerificationAndOptions(data, hashAlgorithm, true, options);
    }

    /// <summary>
    /// Computes the hash for verification.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "hashAlgorithm">The hash algorithm used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    public EmailOpenPgpOperationResult ComputeHashForVerification(ReadOnlySpan<byte> data, int hashAlgorithm, EmailOpenPgpAlgorithmOptions options)
    {
        return EmailOpenPgpAlgorithms.ComputeHashForVerificationImplementation(data, hashAlgorithm, options);
    }

    internal static EmailOpenPgpOperationResult CompressImplementationWithDataAndCompressionAlgorithm(ReadOnlySpan<byte> data, int compressionAlgorithm)
    {
        return CompressImplementationWithDataAndCompressionAlgorithmAndOptions(data, compressionAlgorithm, new EmailOpenPgpAlgorithmOptions());
    }

    /// <summary>
    /// Compresses the supplied value.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "compressionAlgorithm">The compression algorithm used by the operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    public EmailOpenPgpOperationResult CompressWithDataAndCompressionAlgorithm(ReadOnlySpan<byte> data, int compressionAlgorithm)
    {
        return EmailOpenPgpAlgorithms.CompressImplementationWithDataAndCompressionAlgorithm(data, compressionAlgorithm);
    }

    internal static EmailOpenPgpOperationResult CompressImplementationWithDataAndCompressionAlgorithmAndOptions(ReadOnlySpan<byte> data, int compressionAlgorithm, EmailOpenPgpAlgorithmOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        EmailOpenPgpAlgorithmOptions snapshot = options.Clone();
        byte[] input = BoundedInput(data, snapshot.GetLimits());
        if (!GetSupportImplementation(EmailOpenPgpAlgorithmCategory.Compression, compressionAlgorithm).IsSupported())
            return Unsupported(input, "The OpenPGP compression algorithm is unsupported.");
        if (compressionAlgorithm == 0)
            return Success(input, input);
        using (MemoryStream output = new MemoryStream())
        {
            Stream compressor = compressionAlgorithm == 1 ? new DeflateStream(output, snapshot.GetCompressionLevel(), true) : new ZLibStream(output, snapshot.GetCompressionLevel(), true);
            using (compressor)
                compressor.Write(input, 0, input.Length);
            byte[] result = output.ToArray();
            if (result.LongLength > snapshot.GetLimits().GetMaxInputBytes())
                throw new EmailLimitException("MaxOpenPgpCompressedBytes", snapshot.GetLimits().GetMaxInputBytes(), result.LongLength);
            return Success(result, input);
        }
    }

    /// <summary>
    /// Compresses the supplied value.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "compressionAlgorithm">The compression algorithm used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailOpenPgpOperationResult CompressWithDataAndCompressionAlgorithmAndOptions(ReadOnlySpan<byte> data, int compressionAlgorithm, EmailOpenPgpAlgorithmOptions options)
    {
        return EmailOpenPgpAlgorithms.CompressImplementationWithDataAndCompressionAlgorithmAndOptions(data, compressionAlgorithm, options);
    }

    internal static EmailOpenPgpOperationResult? DecompressImplementationWithDataAndCompressionAlgorithm(ReadOnlySpan<byte> data, int compressionAlgorithm)
    {
        return DecompressImplementationWithDataAndCompressionAlgorithmAndOptions(data, compressionAlgorithm, new EmailOpenPgpAlgorithmOptions());
    }

    /// <summary>
    /// Decompresses the supplied value.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "compressionAlgorithm">The compression algorithm used by the operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    public EmailOpenPgpOperationResult? DecompressWithDataAndCompressionAlgorithm(ReadOnlySpan<byte> data, int compressionAlgorithm)
    {
        return EmailOpenPgpAlgorithms.DecompressImplementationWithDataAndCompressionAlgorithm(data, compressionAlgorithm);
    }

    internal static EmailOpenPgpOperationResult? DecompressImplementationWithDataAndCompressionAlgorithmAndOptions(ReadOnlySpan<byte> data, int compressionAlgorithm, EmailOpenPgpAlgorithmOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        EmailOpenPgpAlgorithmOptions snapshot = options.Clone();
        byte[] input = BoundedInput(data, snapshot.GetLimits());
        if (!GetSupportImplementation(EmailOpenPgpAlgorithmCategory.Compression, compressionAlgorithm).IsSupported())
            return Unsupported(input, "The OpenPGP compression algorithm is unsupported.");
        if (compressionAlgorithm == 0)
            return Success(input, input);
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingFunc(() =>
        {
            using (MemoryStream source = new MemoryStream(input, false))
            using (Stream decompressor = compressionAlgorithm == 1 ? new DeflateStream(source, CompressionMode.Decompress) : new ZLibStream(source, CompressionMode.Decompress))
            using (MemoryStream output = new MemoryStream())
            {
                CopyBounded(decompressor, output, snapshot.GetLimits().GetMaxDecompressedBytes());
                return Success(output.ToArray(), input);
            }
        });
        if (captured.IsSuccess())
            return (EmailOpenPgpOperationResult? )captured.GetValue();
        if (captured.GetException() is InvalidDataException)
            return Failure(EmailOpenPgpOperationStatus.InvalidInput, input, EmailIssueCode.OpenPgpOperationFailed, "The compressed OpenPGP data is invalid or truncated.");
        if (captured.GetException() is IOException)
            return Failure(EmailOpenPgpOperationStatus.Failed, input, EmailIssueCode.OpenPgpOperationFailed, "The compressed OpenPGP data could not be read.");
        throw (captured.GetException() ?? throw new InvalidOperationException());
    }

    /// <summary>
    /// Decompresses the supplied value.
    /// </summary>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "compressionAlgorithm">The compression algorithm used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpOperationResult? DecompressWithDataAndCompressionAlgorithmAndOptions(ReadOnlySpan<byte> data, int compressionAlgorithm, EmailOpenPgpAlgorithmOptions options)
    {
        return EmailOpenPgpAlgorithms.DecompressImplementationWithDataAndCompressionAlgorithmAndOptions(data, compressionAlgorithm, options);
    }

    internal static EmailOpenPgpOperationResult? DeriveStringToKeyImplementationWithPassphraseAndSpecifierAndKeyBytes(ReadOnlySpan<byte> passphrase, ReadOnlySpan<byte> specifier, int keyBytes)
    {
        return DeriveStringToKeyImplementationWithPassphraseAndSpecifierAndKeyBytesAndOptions(passphrase, specifier, keyBytes, new EmailOpenPgpAlgorithmOptions());
    }

    /// <summary>
    /// Derives the string to key.
    /// </summary>
    /// <param name = "passphrase">The passphrase used by the operation.</param>
    /// <param name = "specifier">The specifier used by the operation.</param>
    /// <param name = "keyBytes">The source bytes or data to process.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    public EmailOpenPgpOperationResult? DeriveStringToKeyWithPassphraseAndSpecifierAndKeyBytes(ReadOnlySpan<byte> passphrase, ReadOnlySpan<byte> specifier, int keyBytes)
    {
        return EmailOpenPgpAlgorithms.DeriveStringToKeyImplementationWithPassphraseAndSpecifierAndKeyBytes(passphrase, specifier, keyBytes);
    }

    internal static EmailOpenPgpOperationResult? DeriveStringToKeyImplementationWithPassphraseAndSpecifierAndKeyBytesAndOptions(ReadOnlySpan<byte> passphrase, ReadOnlySpan<byte> specifier, int keyBytes, EmailOpenPgpAlgorithmOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        if (keyBytes <= 0)
            throw new ArgumentOutOfRangeException("keyBytes");
        EmailOpenPgpAlgorithmOptions snapshot = options.Clone();
        EmailCryptographicLimits limits = snapshot.GetLimits();
        if (passphrase.Length > limits.GetMaxInputBytes())
            throw new EmailLimitException("MaxOpenPgpPassphraseBytes", limits.GetMaxInputBytes(), passphrase.Length);
        if (specifier.Length > limits.GetMaxKeyBytes())
            throw new EmailLimitException("MaxOpenPgpStringToKeySpecifierBytes", limits.GetMaxKeyBytes(), specifier.Length);
        if (keyBytes > limits.GetMaxKeyBytes())
            throw new EmailLimitException("MaxOpenPgpDerivedKeyBytes", limits.GetMaxKeyBytes(), keyBytes);
        byte[] preserved = specifier.ToArray();
        if (specifier.Length < 1)
            return Failure(EmailOpenPgpOperationStatus.InvalidInput, preserved, EmailIssueCode.OpenPgpOperationFailed, "The OpenPGP S2K specifier is empty.");
        int type = specifier[0];
        EmailOpenPgpAlgorithmSupport support = GetSupportImplementation(EmailOpenPgpAlgorithmCategory.StringToKey, type);
        if (!support.IsSupported())
            return Unsupported(preserved, support.GetReason());
        if (support.IsDeprecated() && !snapshot.GetAllowDeprecatedStringToKey())
            return PolicyRejected(preserved, "Deprecated Simple and Salted S2K derivation requires explicit opt-in.");
        int expectedLength = type == 0 ? 2 : type == 1 ? 10 : 11;
        if (specifier.Length != expectedLength)
            return Failure(EmailOpenPgpOperationStatus.InvalidInput, preserved, EmailIssueCode.OpenPgpOperationFailed, "The OpenPGP S2K specifier length is invalid.");
        int hashAlgorithm = specifier[1];
        EmailOpenPgpAlgorithmSupport hashSupport = GetSupportImplementation(EmailOpenPgpAlgorithmCategory.Hash, hashAlgorithm);
        if (!hashSupport.IsSupported() || hashSupport.IsDeprecated())
            return Unsupported(preserved, "The S2K hash algorithm is unsupported or prohibited.");
        byte[] password = passphrase.ToArray();
        byte[] repeated = type == 0 ? password : Join(specifier.Slice(2, 8), passphrase);
        long hashedBytes = repeated.Length;
        if (type == 3)
        {
            long decodedCount = ((long)16 + (specifier[10] & 15)) << ((specifier[10] >> 4) + 6);
            hashedBytes = Math.Max(decodedCount, repeated.Length);
        }

        if (hashedBytes > snapshot.GetMaximumStringToKeyBytes())
            throw new EmailLimitException("MaxOpenPgpStringToKeyBytes", snapshot.GetMaximumStringToKeyBytes(), hashedBytes);
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingFunc(() => DeriveHashedKey(repeated, hashedBytes, hashAlgorithm, keyBytes));
        byte[]? derived = captured.IsSuccess() ? (byte[]? )captured.GetValue() : null;
        EmailOpenPgpOperationResult? result = derived == null ? null : Success(derived, preserved);
        if (derived != null)
            CryptographicOperations.ZeroMemory(derived);
        CryptographicOperations.ZeroMemory(password);
        if (!ReferenceEquals(repeated, password))
            CryptographicOperations.ZeroMemory(repeated);
        if (!captured.IsSuccess())
            throw (captured.GetException() ?? throw new InvalidOperationException());
        return result;
    }

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
    public EmailOpenPgpOperationResult? DeriveStringToKeyWithPassphraseAndSpecifierAndKeyBytesAndOptions(ReadOnlySpan<byte> passphrase, ReadOnlySpan<byte> specifier, int keyBytes, EmailOpenPgpAlgorithmOptions options)
    {
        return EmailOpenPgpAlgorithms.DeriveStringToKeyImplementationWithPassphraseAndSpecifierAndKeyBytesAndOptions(passphrase, specifier, keyBytes, options);
    }

    internal static EmailOpenPgpOperationResult? SignHashImplementationWithHashAndHashAlgorithmAndPublicKeyAlgorithmAndKeyAndOptions(ReadOnlySpan<byte> hash, int hashAlgorithm, int publicKeyAlgorithm, AsymmetricAlgorithm key, EmailOpenPgpAlgorithmOptions options)
    {
        if (key == null)
            throw new ArgumentNullException("key");
        if (options == null)
            throw new ArgumentNullException("options");
        byte[] input = BoundedInput(hash, options.GetLimits());
        if (!IsValidHashLength(input, hashAlgorithm))
            return Failure(EmailOpenPgpOperationStatus.InvalidInput, input, EmailIssueCode.OpenPgpOperationFailed, "The digest length does not match its OpenPGP hash algorithm.");
        if (hashAlgorithm == 2)
            return PolicyRejected(input, "SHA-1 must not be used to generate OpenPGP signatures.");
        EmailOpenPgpAlgorithmSupport support = GetSupportImplementation(EmailOpenPgpAlgorithmCategory.PublicKey, publicKeyAlgorithm);
        if (!support.IsSupported() || publicKeyAlgorithm == 2)
            return Unsupported(input, "The OpenPGP signing algorithm is unsupported.");
        if ((publicKeyAlgorithm == 1 || publicKeyAlgorithm == 3) && !options.GetAllowDeprecatedRsaGeneration())
            return PolicyRejected(input, "Deprecated RSA OpenPGP signature generation requires explicit opt-in.");
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingFunc(() =>
        {
            if (publicKeyAlgorithm == 1 || publicKeyAlgorithm == 3)
            {
                RSA? rsa = key as RSA;
                if (rsa == null)
                    return Failure(EmailOpenPgpOperationStatus.InvalidInput, input, EmailIssueCode.OpenPgpOperationFailed, "The supplied key is not RSA.");
                ValidateRsaKey(rsa, options.GetLimits());
                byte[] signature = rsa.SignHash(input, ToHashAlgorithmName(hashAlgorithm), RSASignaturePadding.Pkcs1);
                return Success(EncodeMpi(signature), input);
            }

            ECDsa? ecdsa = key as ECDsa;
            if (ecdsa == null)
                return Failure(EmailOpenPgpOperationStatus.InvalidInput, input, EmailIssueCode.OpenPgpOperationFailed, "The supplied key is not ECDSA.");
            byte[] raw = ecdsa.SignHash(input, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            int half = raw.Length / 2;
            byte[] encoded = Join(EncodeMpi(new ReadOnlySpan<byte>(raw, 0, half)), EncodeMpi(new ReadOnlySpan<byte>(raw, half, half)));
            CryptographicOperations.ZeroMemory(raw);
            return Success(encoded, input);
        });
        return captured.IsSuccess() ? (EmailOpenPgpOperationResult? )captured.GetValue() : Failure(EmailOpenPgpOperationStatus.Failed, input, EmailIssueCode.OpenPgpOperationFailed, "The OpenPGP signature operation failed.");
    }

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
    public EmailOpenPgpOperationResult? SignHashWithHashAndHashAlgorithmAndPublicKeyAlgorithmAndKeyAndOptions(ReadOnlySpan<byte> hash, int hashAlgorithm, int publicKeyAlgorithm, AsymmetricAlgorithm key, EmailOpenPgpAlgorithmOptions options)
    {
        return EmailOpenPgpAlgorithms.SignHashImplementationWithHashAndHashAlgorithmAndPublicKeyAlgorithmAndKeyAndOptions(hash, hashAlgorithm, publicKeyAlgorithm, key, options);
    }

    internal static EmailOpenPgpOperationResult? SignHashImplementationWithHashAndHashAlgorithmAndPublicKeyAlgorithmAndKey(ReadOnlySpan<byte> hash, int hashAlgorithm, int publicKeyAlgorithm, AsymmetricAlgorithm key)
    {
        return SignHashImplementationWithHashAndHashAlgorithmAndPublicKeyAlgorithmAndKeyAndOptions(hash, hashAlgorithm, publicKeyAlgorithm, key, new EmailOpenPgpAlgorithmOptions());
    }

    /// <summary>
    /// Signs the hash.
    /// </summary>
    /// <param name = "hash">The hash used by the operation.</param>
    /// <param name = "hashAlgorithm">The hash algorithm used by the operation.</param>
    /// <param name = "publicKeyAlgorithm">The public key algorithm used by the operation.</param>
    /// <param name = "key">The key used by the operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    public EmailOpenPgpOperationResult? SignHashWithHashAndHashAlgorithmAndPublicKeyAlgorithmAndKey(ReadOnlySpan<byte> hash, int hashAlgorithm, int publicKeyAlgorithm, AsymmetricAlgorithm key)
    {
        return EmailOpenPgpAlgorithms.SignHashImplementationWithHashAndHashAlgorithmAndPublicKeyAlgorithmAndKey(hash, hashAlgorithm, publicKeyAlgorithm, key);
    }

    internal static EmailOpenPgpOperationResult? VerifyHashImplementationWithHashAndSignatureMaterialAndHashAlgorithmAndPublicKeyAlgorithmAndKey(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signatureMaterial, int hashAlgorithm, int publicKeyAlgorithm, AsymmetricAlgorithm key)
    {
        return VerifyHashImplementationWithHashAndSignatureMaterialAndHashAlgorithmAndPublicKeyAlgorithmAndKeyAndOptions(hash, signatureMaterial, hashAlgorithm, publicKeyAlgorithm, key, new EmailOpenPgpAlgorithmOptions());
    }

    /// <summary>
    /// Verifies the hash.
    /// </summary>
    /// <param name = "hash">The hash used by the operation.</param>
    /// <param name = "signatureMaterial">The signature material used by the operation.</param>
    /// <param name = "hashAlgorithm">The hash algorithm used by the operation.</param>
    /// <param name = "publicKeyAlgorithm">The public key algorithm used by the operation.</param>
    /// <param name = "key">The key used by the operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    public EmailOpenPgpOperationResult? VerifyHashWithHashAndSignatureMaterialAndHashAlgorithmAndPublicKeyAlgorithmAndKey(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signatureMaterial, int hashAlgorithm, int publicKeyAlgorithm, AsymmetricAlgorithm key)
    {
        return EmailOpenPgpAlgorithms.VerifyHashImplementationWithHashAndSignatureMaterialAndHashAlgorithmAndPublicKeyAlgorithmAndKey(hash, signatureMaterial, hashAlgorithm, publicKeyAlgorithm, key);
    }

    internal static EmailOpenPgpOperationResult? VerifyHashImplementationWithHashAndSignatureMaterialAndHashAlgorithmAndPublicKeyAlgorithmAndKeyAndOptions(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signatureMaterial, int hashAlgorithm, int publicKeyAlgorithm, AsymmetricAlgorithm key, EmailOpenPgpAlgorithmOptions options)
    {
        if (key == null)
            throw new ArgumentNullException("key");
        if (options == null)
            throw new ArgumentNullException("options");
        byte[] digest = BoundedInput(hash, options.GetLimits());
        byte[] preserved = BoundedInput(signatureMaterial, options.GetLimits());
        if (!IsValidHashLength(digest, hashAlgorithm))
            return Failure(EmailOpenPgpOperationStatus.InvalidInput, preserved, EmailIssueCode.OpenPgpOperationFailed, "The digest length does not match its OpenPGP hash algorithm.");
        if (hashAlgorithm == 2 && !options.GetAllowSha1Verification())
            return PolicyRejected(preserved, "SHA-1 verification requires explicit legacy opt-in.");
        EmailOpenPgpAlgorithmSupport support = GetSupportImplementation(EmailOpenPgpAlgorithmCategory.PublicKey, publicKeyAlgorithm);
        if (!support.IsSupported() || publicKeyAlgorithm == 2)
            return Unsupported(preserved, "The OpenPGP verification algorithm is unsupported.");
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingFunc(() =>
        {
            int position = 0;
            if (publicKeyAlgorithm == 1 || publicKeyAlgorithm == 3)
            {
                RSA? rsa = key as RSA;
                if (rsa == null)
                    return Failure(EmailOpenPgpOperationStatus.InvalidInput, preserved, EmailIssueCode.OpenPgpOperationFailed, "The supplied key is not RSA.");
                ValidateRsaKey(rsa, options.GetLimits());
                EmailOpenPgpMpiRead mpiRead = ReadMpi(preserved, position);
                if (!mpiRead.IsSuccess() || mpiRead.GetPosition() != preserved.Length)
                    return Failure(EmailOpenPgpOperationStatus.InvalidInput, preserved, EmailIssueCode.OpenPgpOperationFailed, "The RSA signature MPI is malformed.");
                byte[]? mpi = mpiRead.GetValue();
                byte[] signature = LeftPad(mpi, (rsa.KeySize + 7) / 8);
                bool valid = rsa.VerifyHash(digest, signature, ToHashAlgorithmName(hashAlgorithm), RSASignaturePadding.Pkcs1);
                return Verification(valid, preserved);
            }

            ECDsa? ecdsa = key as ECDsa;
            if (ecdsa == null)
                return Failure(EmailOpenPgpOperationStatus.InvalidInput, preserved, EmailIssueCode.OpenPgpOperationFailed, "The supplied key is not ECDSA.");
            EmailOpenPgpMpiRead rRead = ReadMpi(preserved, position);
            EmailOpenPgpMpiRead sRead = rRead.IsSuccess() ? ReadMpi(preserved, rRead.GetPosition()) : EmailOpenPgpMpiRead.Failure(position);
            if (!rRead.IsSuccess() || !sRead.IsSuccess() || sRead.GetPosition() != preserved.Length)
                return Failure(EmailOpenPgpOperationStatus.InvalidInput, preserved, EmailIssueCode.OpenPgpOperationFailed, "The ECDSA signature MPIs are malformed.");
            byte[]? r = rRead.GetValue();
            byte[]? s = sRead.GetValue();
            int fieldBytes = (ecdsa.KeySize + 7) / 8;
            byte[] raw = Join(LeftPad(r, fieldBytes), LeftPad(s, fieldBytes));
            bool verified = ecdsa.VerifyHash(digest, raw, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            return Verification(verified, preserved);
        });
        return captured.IsSuccess() ? (EmailOpenPgpOperationResult? )captured.GetValue() : Failure(EmailOpenPgpOperationStatus.Failed, preserved, EmailIssueCode.OpenPgpOperationFailed, "The OpenPGP verification operation failed.");
    }

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
    public EmailOpenPgpOperationResult? VerifyHashWithHashAndSignatureMaterialAndHashAlgorithmAndPublicKeyAlgorithmAndKeyAndOptions(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signatureMaterial, int hashAlgorithm, int publicKeyAlgorithm, AsymmetricAlgorithm key, EmailOpenPgpAlgorithmOptions options)
    {
        return EmailOpenPgpAlgorithms.VerifyHashImplementationWithHashAndSignatureMaterialAndHashAlgorithmAndPublicKeyAlgorithmAndKeyAndOptions(hash, signatureMaterial, hashAlgorithm, publicKeyAlgorithm, key, options);
    }

    internal static EmailOpenPgpOperationResult? EncryptSessionKeyImplementationWithSessionKeyDataAndPublicKeyAlgorithmAndKeyAndOptions(ReadOnlySpan<byte> sessionKeyData, int publicKeyAlgorithm, RSA key, EmailOpenPgpAlgorithmOptions options)
    {
        if (key == null)
            throw new ArgumentNullException("key");
        if (options == null)
            throw new ArgumentNullException("options");
        byte[] input = BoundedInput(sessionKeyData, options.GetLimits());
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingFunc(() =>
        {
            if (publicKeyAlgorithm != 1 && publicKeyAlgorithm != 2)
                return Unsupported(input, "Only legacy RSA PKESK encryption is backed by a safe runtime primitive.");
            if (!options.GetAllowDeprecatedRsaGeneration())
                return PolicyRejected(input, "Deprecated RSA OpenPGP session-key encryption requires explicit opt-in.");
            ValidateRsaKey(key, options.GetLimits());
            return Success(EncodeMpi(key.Encrypt(input, RSAEncryptionPadding.Pkcs1)), input);
        });
        EmailOpenPgpOperationResult? result = captured.IsSuccess() ? (EmailOpenPgpOperationResult? )captured.GetValue() : Failure(EmailOpenPgpOperationStatus.Failed, input, EmailIssueCode.OpenPgpOperationFailed, "RSA session-key encryption failed.");
        CryptographicOperations.ZeroMemory(input);
        return result;
    }

    /// <summary>
    /// Encrypts the session key.
    /// </summary>
    /// <param name = "sessionKeyData">The session key data used by the operation.</param>
    /// <param name = "publicKeyAlgorithm">The public key algorithm used by the operation.</param>
    /// <param name = "key">The key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpOperationResult? EncryptSessionKeyWithSessionKeyDataAndPublicKeyAlgorithmAndKeyAndOptions(ReadOnlySpan<byte> sessionKeyData, int publicKeyAlgorithm, RSA key, EmailOpenPgpAlgorithmOptions options)
    {
        return EmailOpenPgpAlgorithms.EncryptSessionKeyImplementationWithSessionKeyDataAndPublicKeyAlgorithmAndKeyAndOptions(sessionKeyData, publicKeyAlgorithm, key, options);
    }

    internal static EmailOpenPgpOperationResult? EncryptSessionKeyImplementationWithSessionKeyDataAndPublicKeyAlgorithmAndKey(ReadOnlySpan<byte> sessionKeyData, int publicKeyAlgorithm, RSA key)
    {
        return EncryptSessionKeyImplementationWithSessionKeyDataAndPublicKeyAlgorithmAndKeyAndOptions(sessionKeyData, publicKeyAlgorithm, key, new EmailOpenPgpAlgorithmOptions());
    }

    /// <summary>
    /// Encrypts the session key.
    /// </summary>
    /// <param name = "sessionKeyData">The session key data used by the operation.</param>
    /// <param name = "publicKeyAlgorithm">The public key algorithm used by the operation.</param>
    /// <param name = "key">The key used by the operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    public EmailOpenPgpOperationResult? EncryptSessionKeyWithSessionKeyDataAndPublicKeyAlgorithmAndKey(ReadOnlySpan<byte> sessionKeyData, int publicKeyAlgorithm, RSA key)
    {
        return EmailOpenPgpAlgorithms.EncryptSessionKeyImplementationWithSessionKeyDataAndPublicKeyAlgorithmAndKey(sessionKeyData, publicKeyAlgorithm, key);
    }

    internal static EmailOpenPgpOperationResult DecryptSessionKeyImplementationWithEncryptedSessionKeyAndPublicKeyAlgorithmAndKey(ReadOnlySpan<byte> encryptedSessionKey, int publicKeyAlgorithm, RSA key)
    {
        return DecryptSessionKeyImplementationWithEncryptedSessionKeyAndPublicKeyAlgorithmAndKeyAndOptions(encryptedSessionKey, publicKeyAlgorithm, key, new EmailOpenPgpAlgorithmOptions());
    }

    /// <summary>
    /// Decrypts the session key.
    /// </summary>
    /// <param name = "encryptedSessionKey">The encrypted session key used by the operation.</param>
    /// <param name = "publicKeyAlgorithm">The public key algorithm used by the operation.</param>
    /// <param name = "key">The key used by the operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    public EmailOpenPgpOperationResult DecryptSessionKeyWithEncryptedSessionKeyAndPublicKeyAlgorithmAndKey(ReadOnlySpan<byte> encryptedSessionKey, int publicKeyAlgorithm, RSA key)
    {
        return EmailOpenPgpAlgorithms.DecryptSessionKeyImplementationWithEncryptedSessionKeyAndPublicKeyAlgorithmAndKey(encryptedSessionKey, publicKeyAlgorithm, key);
    }

    internal static EmailOpenPgpOperationResult DecryptSessionKeyImplementationWithEncryptedSessionKeyAndPublicKeyAlgorithmAndKeyAndOptions(ReadOnlySpan<byte> encryptedSessionKey, int publicKeyAlgorithm, RSA key, EmailOpenPgpAlgorithmOptions options)
    {
        if (key == null)
            throw new ArgumentNullException("key");
        if (options == null)
            throw new ArgumentNullException("options");
        byte[] input = BoundedInput(encryptedSessionKey, options.GetLimits());
        if (publicKeyAlgorithm != 1 && publicKeyAlgorithm != 2)
            return Unsupported(input, "Only legacy RSA PKESK decryption is backed by a safe runtime primitive.");
        int position = 0;
        EmailOpenPgpMpiRead mpiRead = ReadMpi(input, position);
        if (!mpiRead.IsSuccess() || mpiRead.GetPosition() != input.Length)
            return Failure(EmailOpenPgpOperationStatus.InvalidInput, input, EmailIssueCode.OpenPgpOperationFailed, "The encrypted session-key MPI is malformed.");
        byte[]? mpi = mpiRead.GetValue();
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingFunc(() =>
        {
            ValidateRsaKey(key, options.GetLimits());
            return key.Decrypt(LeftPad(mpi, (key.KeySize + 7) / 8), RSAEncryptionPadding.Pkcs1);
        });
        byte[]? decrypted = captured.IsSuccess() ? (byte[]? )captured.GetValue() : null;
        EmailOpenPgpOperationResult result = decrypted == null ? Failure(EmailOpenPgpOperationStatus.AuthenticationFailed, input, EmailIssueCode.OpenPgpAuthenticationFailed, "RSA session-key decryption failed without releasing key data.") : Success(decrypted, input);
        if (decrypted != null)
            CryptographicOperations.ZeroMemory(decrypted);
        return result;
    }

    /// <summary>
    /// Decrypts the session key.
    /// </summary>
    /// <param name = "encryptedSessionKey">The encrypted session key used by the operation.</param>
    /// <param name = "publicKeyAlgorithm">The public key algorithm used by the operation.</param>
    /// <param name = "key">The key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP operation result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpOperationResult DecryptSessionKeyWithEncryptedSessionKeyAndPublicKeyAlgorithmAndKeyAndOptions(ReadOnlySpan<byte> encryptedSessionKey, int publicKeyAlgorithm, RSA key, EmailOpenPgpAlgorithmOptions options)
    {
        return EmailOpenPgpAlgorithms.DecryptSessionKeyImplementationWithEncryptedSessionKeyAndPublicKeyAlgorithmAndKeyAndOptions(encryptedSessionKey, publicKeyAlgorithm, key, options);
    }

    internal static EmailOpenPgpOperationResult EncryptAeadImplementationWithPlaintextAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedData(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key, int symmetricAlgorithm, int aeadAlgorithm, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> associatedData)
    {
        return EncryptAeadImplementationWithPlaintextAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedDataAndOptions(plaintext, key, symmetricAlgorithm, aeadAlgorithm, nonce, associatedData, new EmailOpenPgpAlgorithmOptions());
    }

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
    public EmailOpenPgpOperationResult EncryptAeadWithPlaintextAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedData(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key, int symmetricAlgorithm, int aeadAlgorithm, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> associatedData)
    {
        return EmailOpenPgpAlgorithms.EncryptAeadImplementationWithPlaintextAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedData(plaintext, key, symmetricAlgorithm, aeadAlgorithm, nonce, associatedData);
    }

    internal static EmailOpenPgpOperationResult EncryptAeadImplementationWithPlaintextAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedDataAndOptions(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key, int symmetricAlgorithm, int aeadAlgorithm, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> associatedData, EmailOpenPgpAlgorithmOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        EmailCryptographicLimits limits = options.GetLimits();
        byte[] input = BoundedInput(plaintext, limits);
        ValidateAssociatedData(associatedData, limits);
        if (!GetSupportImplementation(EmailOpenPgpAlgorithmCategory.Symmetric, symmetricAlgorithm).IsSupported())
            return Unsupported(input, "The OpenPGP symmetric algorithm is unsupported.");
        if (!IsAesKeyLength(symmetricAlgorithm, key.Length))
            return Failure(EmailOpenPgpOperationStatus.InvalidInput, input, EmailIssueCode.OpenPgpOperationFailed, "The AES key length does not match the OpenPGP symmetric algorithm.");
        if (aeadAlgorithm != 3 || !AesGcm.IsSupported)
            return Unsupported(input, "Only AES-GCM is exposed by the runtime's safe AEAD primitives.");
        if (nonce.Length != 12)
            return Failure(EmailOpenPgpOperationStatus.InvalidInput, input, EmailIssueCode.OpenPgpOperationFailed, "OpenPGP GCM requires a 12-octet nonce.");
        byte[] ciphertext = new byte[input.Length];
        byte[] tag = new byte[16];
        byte[] keyBytes = key.ToArray();
        byte[] nonceBytes = nonce.ToArray();
        byte[] associatedBytes = associatedData.ToArray();
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingAction(() =>
        {
            using (AesGcm aes = new AesGcm(keyBytes, tag.Length))
                aes.Encrypt(nonceBytes, input, ciphertext, tag, associatedBytes);
        });
        EmailOpenPgpOperationResult result = captured.IsSuccess() ? Success(Join(ciphertext, tag), input) : Failure(EmailOpenPgpOperationStatus.Failed, input, EmailIssueCode.OpenPgpOperationFailed, "AES-GCM encryption failed.");
        CryptographicOperations.ZeroMemory(keyBytes);
        CryptographicOperations.ZeroMemory(input);
        return result;
    }

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
    public EmailOpenPgpOperationResult EncryptAeadWithPlaintextAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedDataAndOptions(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key, int symmetricAlgorithm, int aeadAlgorithm, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> associatedData, EmailOpenPgpAlgorithmOptions options)
    {
        return EmailOpenPgpAlgorithms.EncryptAeadImplementationWithPlaintextAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedDataAndOptions(plaintext, key, symmetricAlgorithm, aeadAlgorithm, nonce, associatedData, options);
    }

    internal static EmailOpenPgpOperationResult DecryptAeadImplementationWithEncryptedAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedData(ReadOnlySpan<byte> encrypted, ReadOnlySpan<byte> key, int symmetricAlgorithm, int aeadAlgorithm, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> associatedData)
    {
        return DecryptAeadImplementationWithEncryptedAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedDataAndOptions(encrypted, key, symmetricAlgorithm, aeadAlgorithm, nonce, associatedData, new EmailOpenPgpAlgorithmOptions());
    }

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
    public EmailOpenPgpOperationResult DecryptAeadWithEncryptedAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedData(ReadOnlySpan<byte> encrypted, ReadOnlySpan<byte> key, int symmetricAlgorithm, int aeadAlgorithm, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> associatedData)
    {
        return EmailOpenPgpAlgorithms.DecryptAeadImplementationWithEncryptedAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedData(encrypted, key, symmetricAlgorithm, aeadAlgorithm, nonce, associatedData);
    }

    internal static EmailOpenPgpOperationResult DecryptAeadImplementationWithEncryptedAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedDataAndOptions(ReadOnlySpan<byte> encrypted, ReadOnlySpan<byte> key, int symmetricAlgorithm, int aeadAlgorithm, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> associatedData, EmailOpenPgpAlgorithmOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        EmailCryptographicLimits limits = options.GetLimits();
        byte[] input = BoundedInput(encrypted, limits);
        ValidateAssociatedData(associatedData, limits);
        if (!GetSupportImplementation(EmailOpenPgpAlgorithmCategory.Symmetric, symmetricAlgorithm).IsSupported())
            return Unsupported(input, "The OpenPGP symmetric algorithm is unsupported.");
        if (!IsAesKeyLength(symmetricAlgorithm, key.Length))
            return Failure(EmailOpenPgpOperationStatus.InvalidInput, input, EmailIssueCode.OpenPgpOperationFailed, "The AES key length does not match the OpenPGP symmetric algorithm.");
        if (aeadAlgorithm != 3 || !AesGcm.IsSupported)
            return Unsupported(input, "Only AES-GCM is exposed by the runtime's safe AEAD primitives.");
        if (nonce.Length != 12 || input.Length < 16)
            return Failure(EmailOpenPgpOperationStatus.InvalidInput, input, EmailIssueCode.OpenPgpOperationFailed, "The OpenPGP GCM nonce or authentication tag is malformed.");
        byte[] plaintext = new byte[input.Length - 16];
        byte[] keyBytes = key.ToArray();
        byte[] nonceBytes = nonce.ToArray();
        byte[] associatedBytes = associatedData.ToArray();
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingAction(() =>
        {
            using (AesGcm aes = new AesGcm(keyBytes, 16))
                aes.Decrypt(nonceBytes, new ReadOnlySpan<byte>(input, 0, plaintext.Length), new ReadOnlySpan<byte>(input, plaintext.Length, 16), plaintext, associatedBytes);
        });
        EmailOpenPgpOperationResult result = captured.IsSuccess() ? Success(plaintext, input) : Failure(EmailOpenPgpOperationStatus.AuthenticationFailed, input, EmailIssueCode.OpenPgpAuthenticationFailed, "AES-GCM authentication failed without releasing plaintext.");
        CryptographicOperations.ZeroMemory(keyBytes);
        CryptographicOperations.ZeroMemory(plaintext);
        return result;
    }

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
    public EmailOpenPgpOperationResult DecryptAeadWithEncryptedAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedDataAndOptions(ReadOnlySpan<byte> encrypted, ReadOnlySpan<byte> key, int symmetricAlgorithm, int aeadAlgorithm, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> associatedData, EmailOpenPgpAlgorithmOptions options)
    {
        return EmailOpenPgpAlgorithms.DecryptAeadImplementationWithEncryptedAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedDataAndOptions(encrypted, key, symmetricAlgorithm, aeadAlgorithm, nonce, associatedData, options);
    }

    static EmailOpenPgpOperationResult ComputeHashWithDataAndHashAlgorithmAndVerificationAndOptions(ReadOnlySpan<byte> data, int hashAlgorithm, bool verification, EmailOpenPgpAlgorithmOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        byte[] input = BoundedInput(data, options.GetLimits());
        EmailOpenPgpAlgorithmSupport support = GetSupportImplementation(EmailOpenPgpAlgorithmCategory.Hash, hashAlgorithm);
        if (!support.IsSupported())
            return Unsupported(input, support.GetReason());
        if (hashAlgorithm == 2 && (!verification || !options.GetAllowSha1Verification()))
            return PolicyRejected(input, "SHA-1 is disabled except for explicitly enabled legacy verification.");
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingFunc(() => HashData(input, hashAlgorithm));
        return captured.IsSuccess() ? Success((byte[]? )captured.GetValue(), input) : Failure(EmailOpenPgpOperationStatus.Failed, input, EmailIssueCode.OpenPgpOperationFailed, "The runtime hash provider failed.");
    }

    static byte[] DeriveHashedKey(byte[] repeated, long hashedBytes, int hashAlgorithm, int keyBytes)
    {
        byte[] output = new byte[keyBytes];
        int written = 0;
        int prefixLength = 0;
        while (written < keyBytes)
        {
            using (IncrementalHash hash = IncrementalHash.CreateHash(ToHashAlgorithmName(hashAlgorithm)))
            {
                if (prefixLength > 0)
                    hash.AppendData(new byte[prefixLength]);
                long remaining = hashedBytes;
                while (remaining > 0)
                {
                    int count = (int)Math.Min(remaining, repeated.Length);
                    hash.AppendData(repeated, 0, count);
                    remaining -= count;
                }

                byte[] digest = hash.GetHashAndReset();
                int countToCopy = Math.Min(digest.Length, keyBytes - written);
                Array.Copy(digest, 0, output, written, countToCopy);
                written += countToCopy;
                CryptographicOperations.ZeroMemory(digest);
            }

            prefixLength++;
        }

        return output;
    }

    static byte[] HashData(byte[] input, int algorithm)
    {
        if (algorithm == 2)
            return SHA1.HashData(input);
        if (algorithm == 8)
            return SHA256.HashData(input);
        if (algorithm == 9)
            return SHA384.HashData(input);
        if (algorithm == 10)
            return SHA512.HashData(input);
        if (algorithm == 12)
            return SHA3_256.HashData(input);
        if (algorithm == 14)
            return SHA3_512.HashData(input);
        throw new CryptographicException("Unsupported hash algorithm.");
    }

    static HashAlgorithmName ToHashAlgorithmName(int algorithm)
    {
        if (algorithm == 2)
            return HashAlgorithmName.SHA1;
        if (algorithm == 8)
            return HashAlgorithmName.SHA256;
        if (algorithm == 9)
            return HashAlgorithmName.SHA384;
        if (algorithm == 10)
            return HashAlgorithmName.SHA512;
        if (algorithm == 12)
            return new HashAlgorithmName("SHA3-256");
        if (algorithm == 14)
            return new HashAlgorithmName("SHA3-512");
        throw new CryptographicException("Unsupported hash algorithm.");
    }

    static int HashBytes(int algorithm)
    {
        if (algorithm == 2)
            return 20;
        if (algorithm == 8 || algorithm == 12)
            return 32;
        if (algorithm == 9)
            return 48;
        if (algorithm == 10 || algorithm == 14)
            return 64;
        return 0;
    }

    static bool IsValidHashLength(byte[] value, int algorithm)
    {
        return HashBytes(algorithm) == value.Length && GetSupportImplementation(EmailOpenPgpAlgorithmCategory.Hash, algorithm).IsSupported();
    }

    static byte[] EncodeMpi(ReadOnlySpan<byte> value)
    {
        int start = 0;
        while (start < value.Length && value[start] == 0)
            start++;
        if (start == value.Length)
            return[0, 0];
        int leading = 0;
        byte first = value[start];
        while ((first & 0x80) == 0)
        {
            leading++;
            first <<= 1;
        }

        int bytes = value.Length - start;
        int bits = bytes * 8 - leading;
        byte[] result = new byte[2 + bytes];
        result[0] = (byte)(bits >> 8);
        result[1] = (byte)(bits & 255);
        value.Slice(start).CopyTo(new Span<byte>(result, 2, bytes));
        return result;
    }

    static EmailOpenPgpMpiRead ReadMpi(byte[] input, int position)
    {
        int initialPosition = position;
        if (position > input.Length - 2)
            return EmailOpenPgpMpiRead.Failure(initialPosition);
        int bits = (input[position] << 8) | input[position + 1];
        position += 2;
        int bytes = (bits + 7) / 8;
        if (bytes > input.Length - position)
            return EmailOpenPgpMpiRead.Failure(initialPosition);
        byte[] value = new byte[bytes];
        Array.Copy(input, position, value, 0, bytes);
        position += bytes;
        if (bits == 0)
            return bytes == 0 ? EmailOpenPgpMpiRead.Success(position, value) : EmailOpenPgpMpiRead.Failure(initialPosition);
        if (bytes == 0 || value[0] == 0)
            return EmailOpenPgpMpiRead.Failure(initialPosition);
        int actualBits = bytes * 8;
        byte first = value[0];
        while ((first & 0x80) == 0)
        {
            actualBits--;
            first <<= 1;
        }

        return actualBits == bits ? EmailOpenPgpMpiRead.Success(position, value) : EmailOpenPgpMpiRead.Failure(initialPosition);
    }

    static byte[] LeftPad(byte[]? value, int length)
    {
        if ((value ?? throw new InvalidOperationException()).Length > length)
            throw new CryptographicException("MPI exceeds the key size.");
        byte[] padded = new byte[length];
        value.CopyTo(padded, length - value.Length);
        return padded;
    }

    static void ValidateRsaKey(RSA key, EmailCryptographicLimits limits)
    {
        long maximumKeyBytes = limits.GetMaxKeyBytes();
        int maximumBits = maximumKeyBytes > int.MaxValue / 8 ? int.MaxValue : (int)(maximumKeyBytes * 8);
        if (key.KeySize < 2048 || key.KeySize > maximumBits)
            throw new CryptographicException("RSA key size is outside the allowed range.");
    }

    static bool IsAesKeyLength(int algorithm, int keyBytes)
    {
        return algorithm == 7 && keyBytes == 16 || algorithm == 8 && keyBytes == 24 || algorithm == 9 && keyBytes == 32;
    }

    static byte[] BoundedInput(ReadOnlySpan<byte> data, EmailCryptographicLimits limits)
    {
        if (data.Length > limits.GetMaxInputBytes())
            throw new EmailLimitException("MaxOpenPgpAlgorithmInputBytes", limits.GetMaxInputBytes(), data.Length);
        return data.ToArray();
    }

    static void ValidateAssociatedData(ReadOnlySpan<byte> data, EmailCryptographicLimits limits)
    {
        if (data.Length > limits.GetMaxInputBytes())
            throw new EmailLimitException("MaxOpenPgpAssociatedDataBytes", limits.GetMaxInputBytes(), data.Length);
    }

    static void CopyBounded(Stream input, MemoryStream output, long maximumBytes)
    {
        byte[] buffer = new byte[8192];
        while (true)
        {
            int count = input.Read(buffer, 0, buffer.Length);
            if (count == 0)
                break;
            long observed = output.Length + count;
            if (observed > maximumBytes)
                throw new EmailLimitException("MaxDecompressedBytes", maximumBytes, observed);
            output.Write(buffer, 0, count);
        }
    }

    static byte[] Join(ReadOnlySpan<byte> first, ReadOnlySpan<byte> second)
    {
        byte[] result = new byte[first.Length + second.Length];
        first.CopyTo(result);
        second.CopyTo(new Span<byte>(result, first.Length, second.Length));
        return result;
    }

    static EmailOpenPgpAlgorithmSupport Support(EmailOpenPgpAlgorithmCategory category, int identifier, string name, bool supported, bool deprecated, string reason)
    {
        return new EmailOpenPgpAlgorithmSupport(category, identifier, name, supported, deprecated, reason);
    }

    static string PublicKeyName(int identifier)
    {
        return identifier == 16 ? "Elgamal" : identifier == 17 ? "DSA" : identifier == 18 ? "ECDH" : identifier == 22 ? "EdDSALegacy" : identifier == 25 ? "X25519" : identifier == 26 ? "X448" : identifier == 27 ? "Ed25519" : identifier == 28 ? "Ed448" : "Public Key " + identifier;
    }

    static string SymmetricName(int identifier)
    {
        return identifier == 7 ? "AES-128" : identifier == 8 ? "AES-192" : identifier == 9 ? "AES-256" : identifier == 1 ? "IDEA" : identifier == 2 ? "TripleDES" : identifier == 3 ? "CAST5" : identifier == 4 ? "Blowfish" : identifier == 10 ? "Twofish" : "Symmetric " + identifier;
    }

    static string CompressionName(int identifier)
    {
        return identifier == 0 ? "Uncompressed" : identifier == 1 ? "ZIP" : identifier == 2 ? "ZLIB" : identifier == 3 ? "BZip2" : "Compression " + identifier;
    }

    static string HashName(int identifier)
    {
        return identifier == 1 ? "MD5" : identifier == 2 ? "SHA-1" : identifier == 3 ? "RIPEMD-160" : identifier == 8 ? "SHA2-256" : identifier == 9 ? "SHA2-384" : identifier == 10 ? "SHA2-512" : identifier == 11 ? "SHA2-224" : "Hash " + identifier;
    }

    static EmailOpenPgpOperationResult Success(byte[]? output, byte[] preserved)
    {
        return new EmailOpenPgpOperationResult(EmailOpenPgpOperationStatus.Success, output, preserved, false, new EmailIssueList());
    }

    static EmailOpenPgpOperationResult Verification(bool verified, byte[] preserved)
    {
        return new EmailOpenPgpOperationResult(EmailOpenPgpOperationStatus.Success, new byte[0], preserved, verified, new EmailIssueList());
    }

    static EmailOpenPgpOperationResult Unsupported(byte[] preserved, string description)
    {
        return Failure(EmailOpenPgpOperationStatus.UnsupportedAlgorithm, preserved, EmailIssueCode.OpenPgpAlgorithmUnsupported, description);
    }

    static EmailOpenPgpOperationResult PolicyRejected(byte[] preserved, string description)
    {
        return Failure(EmailOpenPgpOperationStatus.PolicyRejected, preserved, EmailIssueCode.OpenPgpAlgorithmPolicyRejected, description);
    }

    static EmailOpenPgpOperationResult Failure(EmailOpenPgpOperationStatus status, byte[] preserved, EmailIssueCode code, string description)
    {
        EmailIssueList issues = new EmailIssueList();
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, code, description, null, null, null, "No untrusted output was released."));
        return new EmailOpenPgpOperationResult(status, new byte[0], preserved, false, issues);
    }
}
