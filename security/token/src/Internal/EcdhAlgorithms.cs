using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace token;

/// <summary>
/// Applies JWE ECDH-ES direct and AES Key Wrap modes with the SHA-256 Concat KDF.
/// </summary>
internal static class EcdhAlgorithms
{
    /// <summary>Generates an ephemeral key and obtains a content-encryption key.</summary>
    /// <param name="recipientPublicKey">The recipient's static public EC key.</param>
    /// <param name="algorithm">The case-sensitive JWE ECDH key-management algorithm name.</param>
    /// <param name="contentEncryptionAlgorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <returns>A result containing the content key, encrypted-key value, and public ephemeral key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static EcdhKeyManagementResult Encrypt(
        TokenKey recipientPublicKey,
        string algorithm,
        string contentEncryptionAlgorithm)
    {
        return Encrypt(
            recipientPublicKey,
            algorithm,
            contentEncryptionAlgorithm,
            Array.Empty<byte>(),
            Array.Empty<byte>());
    }

    /// <summary>Generates an ephemeral key and obtains a content-encryption key with PartyInfo values.</summary>
    /// <param name="recipientPublicKey">The recipient's static public EC key.</param>
    /// <param name="algorithm">The case-sensitive JWE ECDH key-management algorithm name.</param>
    /// <param name="contentEncryptionAlgorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <param name="partyUInfo">The decoded JWE <c>apu</c> value.</param>
    /// <param name="partyVInfo">The decoded JWE <c>apv</c> value.</param>
    /// <returns>A result containing the content key, encrypted-key value, and public ephemeral key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static EcdhKeyManagementResult Encrypt(
        TokenKey recipientPublicKey,
        string algorithm,
        string contentEncryptionAlgorithm,
        byte[] partyUInfo,
        byte[] partyVInfo)
    {
        if (partyUInfo == null)
        {
            throw new ArgumentNullException("partyUInfo");
        }

        if (partyVInfo == null)
        {
            throw new ArgumentNullException("partyVInfo");
        }

        return Encrypt(
            recipientPublicKey,
            algorithm,
            contentEncryptionAlgorithm,
            partyUInfo.AsSpan(),
            partyVInfo.AsSpan());
    }

    /// <summary>Generates an ephemeral key and obtains a content-encryption key with PartyInfo spans.</summary>
    /// <param name="recipientPublicKey">The recipient's static public EC key.</param>
    /// <param name="algorithm">The case-sensitive JWE ECDH key-management algorithm name.</param>
    /// <param name="contentEncryptionAlgorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <param name="partyUInfo">The decoded JWE <c>apu</c> value.</param>
    /// <param name="partyVInfo">The decoded JWE <c>apv</c> value.</param>
    /// <returns>A result containing the content key, encrypted-key value, and public ephemeral key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static EcdhKeyManagementResult Encrypt(
        TokenKey recipientPublicKey,
        string algorithm,
        string contentEncryptionAlgorithm,
        ReadOnlySpan<byte> partyUInfo,
        ReadOnlySpan<byte> partyVInfo)
    {
        ValidateReferences(
            recipientPublicKey,
            algorithm,
            contentEncryptionAlgorithm);
        if (IsKeyWrapAlgorithm(algorithm))
        {
            int contentEncryptionKeySize = GetContentEncryptionKeySize(
                contentEncryptionAlgorithm);
            TokenResult contentValidation = ValidateContentEncryptionAlgorithm(
                contentEncryptionAlgorithm);
            if (!contentValidation.Success)
            {
                return Failure(contentValidation.Error);
            }

            byte[] contentEncryptionKey = RandomBytes.Generate(
                contentEncryptionKeySize);
            EcdhKeyManagementResult wrapped = Wrap(
                contentEncryptionKey.AsSpan(),
                recipientPublicKey,
                algorithm,
                contentEncryptionAlgorithm,
                partyUInfo,
                partyVInfo);
            CryptographicOperations.ZeroMemory(contentEncryptionKey);
            return wrapped;
        }

        TokenResult validation = Validate(
            recipientPublicKey,
            algorithm,
            contentEncryptionAlgorithm,
            false);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        ECParameters recipientParameters = (recipientPublicKey.Ecdsa ?? throw new InvalidOperationException("A required value is null.")).ExportParameters(false);
        using (ECDiffieHellman ephemeral = ECDiffieHellman.Create(
            recipientParameters.Curve))
        {
            ECParameters ephemeralPrivate = ephemeral.ExportParameters(true);
            ECParameters ephemeralPublic = ephemeral.ExportParameters(false);
            ECDsa ephemeralEcdsa = ECDsa.Create(ephemeralPublic);
            TokenKeyResult imported = EcdsaKeyImporter.Import(
                ephemeralEcdsa,
                false);
            if (!imported.Success)
            {
                ephemeralEcdsa.Dispose();
                ZeroPrivate(ephemeralPrivate);
                return Failure(imported.Error);
            }

            (imported.Key ?? throw new InvalidOperationException("A required value is null.")).OwnsNativeKey = true;
            TokenBytesResult derived = Derive(
                ephemeralPrivate,
                recipientParameters,
                contentEncryptionAlgorithm,
                partyUInfo,
                partyVInfo,
                GetContentEncryptionKeySize(contentEncryptionAlgorithm));
            ZeroPrivate(ephemeralPrivate);
            if (!derived.Success)
            {
                imported.Key.ReleaseOwnedMaterial();
                return Failure(derived.Error);
            }

            EcdhKeyManagementResult result = new EcdhKeyManagementResult();
            result.Success = true;
            result.ContentEncryptionKey = derived.Bytes;
            result.EncryptedKey = Array.Empty<byte>();
            result.EphemeralPublicKey = imported.Key;
            return result;
        }
    }

    /// <summary>Wraps a caller-supplied content-encryption key with a generated ephemeral key.</summary>
    /// <param name="contentEncryptionKey">The content-encryption key to wrap.</param>
    /// <param name="recipientPublicKey">The recipient's static public EC key.</param>
    /// <param name="algorithm">The case-sensitive JWE ECDH-ES with AES Key Wrap algorithm name.</param>
    /// <param name="contentEncryptionAlgorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <returns>A result containing copied content-key bytes, the wrapped key, and public ephemeral key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static EcdhKeyManagementResult Wrap(
        byte[] contentEncryptionKey,
        TokenKey recipientPublicKey,
        string algorithm,
        string contentEncryptionAlgorithm)
    {
        return Wrap(
            contentEncryptionKey,
            recipientPublicKey,
            algorithm,
            contentEncryptionAlgorithm,
            Array.Empty<byte>(),
            Array.Empty<byte>());
    }

    /// <summary>Wraps a caller-supplied content-encryption key using decoded JWE PartyInfo values.</summary>
    /// <param name="contentEncryptionKey">The content-encryption key to wrap.</param>
    /// <param name="recipientPublicKey">The recipient's static public EC key.</param>
    /// <param name="algorithm">The case-sensitive JWE ECDH-ES with AES Key Wrap algorithm name.</param>
    /// <param name="contentEncryptionAlgorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <param name="partyUInfo">The decoded JWE <c>apu</c> value.</param>
    /// <param name="partyVInfo">The decoded JWE <c>apv</c> value.</param>
    /// <returns>A result containing copied content-key bytes, the wrapped key, and public ephemeral key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static EcdhKeyManagementResult Wrap(
        byte[] contentEncryptionKey,
        TokenKey recipientPublicKey,
        string algorithm,
        string contentEncryptionAlgorithm,
        byte[] partyUInfo,
        byte[] partyVInfo)
    {
        if (contentEncryptionKey == null)
        {
            throw new ArgumentNullException("contentEncryptionKey");
        }

        if (partyUInfo == null)
        {
            throw new ArgumentNullException("partyUInfo");
        }

        if (partyVInfo == null)
        {
            throw new ArgumentNullException("partyVInfo");
        }

        return Wrap(
            contentEncryptionKey.AsSpan(),
            recipientPublicKey,
            algorithm,
            contentEncryptionAlgorithm,
            partyUInfo.AsSpan(),
            partyVInfo.AsSpan());
    }

    /// <summary>Wraps a caller-supplied content-encryption key and PartyInfo spans.</summary>
    /// <param name="contentEncryptionKey">The content-encryption key to wrap.</param>
    /// <param name="recipientPublicKey">The recipient's static public EC key.</param>
    /// <param name="algorithm">The case-sensitive JWE ECDH-ES with AES Key Wrap algorithm name.</param>
    /// <param name="contentEncryptionAlgorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <param name="partyUInfo">The decoded JWE <c>apu</c> value.</param>
    /// <param name="partyVInfo">The decoded JWE <c>apv</c> value.</param>
    /// <returns>A result containing copied content-key bytes, the wrapped key, and public ephemeral key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static EcdhKeyManagementResult Wrap(
        ReadOnlySpan<byte> contentEncryptionKey,
        TokenKey recipientPublicKey,
        string algorithm,
        string contentEncryptionAlgorithm,
        ReadOnlySpan<byte> partyUInfo,
        ReadOnlySpan<byte> partyVInfo)
    {
        ValidateReferences(
            recipientPublicKey,
            algorithm,
            contentEncryptionAlgorithm);
        TokenResult validation = Validate(
            recipientPublicKey,
            algorithm,
            contentEncryptionAlgorithm,
            false);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        if (!IsKeyWrapAlgorithm(algorithm))
        {
            return Failure(
                "invalid_algorithm",
                "The JOSE algorithm is not ECDH-ES with AES Key Wrap.");
        }

        int expectedSize = GetContentEncryptionKeySize(
            contentEncryptionAlgorithm);
        if (contentEncryptionKey.Length != expectedSize)
        {
            return Failure(
                "invalid_key_size",
                "The content-encryption key does not match the JWE enc algorithm.");
        }

        ECParameters recipientParameters = (recipientPublicKey.Ecdsa ?? throw new InvalidOperationException("A required value is null.")).ExportParameters(false);
        using (ECDiffieHellman ephemeral = ECDiffieHellman.Create(
            recipientParameters.Curve))
        {
            ECParameters ephemeralPrivate = ephemeral.ExportParameters(true);
            ECParameters ephemeralPublic = ephemeral.ExportParameters(false);
            ECDsa ephemeralEcdsa = ECDsa.Create(ephemeralPublic);
            TokenKeyResult imported = EcdsaKeyImporter.Import(
                ephemeralEcdsa,
                false);
            if (!imported.Success)
            {
                ephemeralEcdsa.Dispose();
                ZeroPrivate(ephemeralPrivate);
                return Failure(imported.Error);
            }

            (imported.Key ?? throw new InvalidOperationException("A required value is null.")).OwnsNativeKey = true;
            TokenBytesResult derived = Derive(
                ephemeralPrivate,
                recipientParameters,
                algorithm,
                partyUInfo,
                partyVInfo,
                GetKeyWrapKeySize(algorithm));
            ZeroPrivate(ephemeralPrivate);
            if (!derived.Success)
            {
                imported.Key.ReleaseOwnedMaterial();
                return Failure(derived.Error);
            }

            TokenKey keyEncryptionKey = new TokenKey();
            keyEncryptionKey.KeyType = TokenKeyTypes.Symmetric;
            keyEncryptionKey.SymmetricKey = derived.Bytes;
            TokenBytesResult encrypted = AesKeyWrap.Wrap(
                contentEncryptionKey,
                keyEncryptionKey,
                GetKeyWrapAlgorithm(algorithm));
            keyEncryptionKey.ReleaseOwnedMaterial();
            if (!encrypted.Success)
            {
                imported.Key.ReleaseOwnedMaterial();
                return Failure(encrypted.Error);
            }

            EcdhKeyManagementResult result = new EcdhKeyManagementResult();
            result.Success = true;
            result.ContentEncryptionKey = contentEncryptionKey.ToArray();
            result.EncryptedKey = encrypted.Bytes;
            result.EphemeralPublicKey = imported.Key;
            return result;
        }
    }

    /// <summary>Derives a content-encryption key from the recipient private key and public ephemeral key.</summary>
    /// <param name="recipientPrivateKey">The recipient's static private EC key.</param>
    /// <param name="ephemeralPublicKey">The public EC key from the JWE <c>epk</c> header.</param>
    /// <param name="algorithm">The case-sensitive JWE ECDH key-management algorithm name.</param>
    /// <param name="contentEncryptionAlgorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <returns>A result containing the derived key and empty encrypted-key value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static EcdhKeyManagementResult Decrypt(
        TokenKey recipientPrivateKey,
        TokenKey ephemeralPublicKey,
        string algorithm,
        string contentEncryptionAlgorithm)
    {
        return Decrypt(
            recipientPrivateKey,
            ephemeralPublicKey,
            algorithm,
            contentEncryptionAlgorithm,
            Array.Empty<byte>(),
            Array.Empty<byte>());
    }

    /// <summary>Derives a content-encryption key using decoded JWE PartyInfo values.</summary>
    /// <param name="recipientPrivateKey">The recipient's static private EC key.</param>
    /// <param name="ephemeralPublicKey">The public EC key from the JWE <c>epk</c> header.</param>
    /// <param name="algorithm">The case-sensitive JWE ECDH key-management algorithm name.</param>
    /// <param name="contentEncryptionAlgorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <param name="partyUInfo">The decoded JWE <c>apu</c> value.</param>
    /// <param name="partyVInfo">The decoded JWE <c>apv</c> value.</param>
    /// <returns>A result containing the derived key and empty encrypted-key value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static EcdhKeyManagementResult Decrypt(
        TokenKey recipientPrivateKey,
        TokenKey ephemeralPublicKey,
        string algorithm,
        string contentEncryptionAlgorithm,
        byte[] partyUInfo,
        byte[] partyVInfo)
    {
        if (partyUInfo == null)
        {
            throw new ArgumentNullException("partyUInfo");
        }

        if (partyVInfo == null)
        {
            throw new ArgumentNullException("partyVInfo");
        }

        return Decrypt(
            recipientPrivateKey,
            ephemeralPublicKey,
            algorithm,
            contentEncryptionAlgorithm,
            partyUInfo.AsSpan(),
            partyVInfo.AsSpan());
    }

    /// <summary>Derives a content-encryption key using decoded JWE PartyInfo spans.</summary>
    /// <param name="recipientPrivateKey">The recipient's static private EC key.</param>
    /// <param name="ephemeralPublicKey">The public EC key from the JWE <c>epk</c> header.</param>
    /// <param name="algorithm">The case-sensitive JWE ECDH key-management algorithm name.</param>
    /// <param name="contentEncryptionAlgorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <param name="partyUInfo">The decoded JWE <c>apu</c> value.</param>
    /// <param name="partyVInfo">The decoded JWE <c>apv</c> value.</param>
    /// <returns>A result containing the derived key and empty encrypted-key value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static EcdhKeyManagementResult Decrypt(
        TokenKey recipientPrivateKey,
        TokenKey ephemeralPublicKey,
        string algorithm,
        string contentEncryptionAlgorithm,
        ReadOnlySpan<byte> partyUInfo,
        ReadOnlySpan<byte> partyVInfo)
    {
        ValidateReferences(
            recipientPrivateKey,
            algorithm,
            contentEncryptionAlgorithm);
        if (ephemeralPublicKey == null)
        {
            throw new ArgumentNullException("ephemeralPublicKey");
        }

        TokenResult recipientValidation = Validate(
            recipientPrivateKey,
            algorithm,
            contentEncryptionAlgorithm,
            true);
        if (!recipientValidation.Success)
        {
            return Failure(recipientValidation.Error);
        }

        if (IsKeyWrapAlgorithm(algorithm))
        {
            return Failure(
                "invalid_algorithm",
                "ECDH-ES with AES Key Wrap requires an encrypted-key value.");
        }

        TokenResult ephemeralValidation = ValidateEphemeral(
            ephemeralPublicKey,
            algorithm);
        if (!ephemeralValidation.Success)
        {
            return Failure(ephemeralValidation.Error);
        }

        ECParameters recipientParameters = (recipientPrivateKey.Ecdsa ?? throw new InvalidOperationException("A required value is null.")).ExportParameters(true);
        ECParameters ephemeralParameters = (ephemeralPublicKey.Ecdsa ?? throw new InvalidOperationException("A required value is null.")).ExportParameters(false);
        if (!SameCurve(recipientParameters.Curve, ephemeralParameters.Curve))
        {
            ZeroPrivate(recipientParameters);
            return Failure(
                "curve_mismatch",
                "The recipient and ephemeral keys must use the same curve.");
        }

        TokenBytesResult derived = Derive(
            recipientParameters,
            ephemeralParameters,
            contentEncryptionAlgorithm,
            partyUInfo,
            partyVInfo,
            GetContentEncryptionKeySize(contentEncryptionAlgorithm));
        ZeroPrivate(recipientParameters);
        if (!derived.Success)
        {
            return Failure(derived.Error);
        }

        EcdhKeyManagementResult result = new EcdhKeyManagementResult();
        result.Success = true;
        result.ContentEncryptionKey = derived.Bytes;
        result.EncryptedKey = Array.Empty<byte>();
        return result;
    }

    /// <summary>Derives an AES wrapping key and unwraps a JWE encrypted-key value.</summary>
    /// <param name="encryptedKey">The JWE encrypted-key value.</param>
    /// <param name="recipientPrivateKey">The recipient's static private EC key.</param>
    /// <param name="ephemeralPublicKey">The public EC key from the JWE <c>epk</c> header.</param>
    /// <param name="algorithm">The case-sensitive JWE ECDH-ES with AES Key Wrap algorithm name.</param>
    /// <param name="contentEncryptionAlgorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <returns>A result containing the authenticated content-encryption key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static EcdhKeyManagementResult Decrypt(
        byte[] encryptedKey,
        TokenKey recipientPrivateKey,
        TokenKey ephemeralPublicKey,
        string algorithm,
        string contentEncryptionAlgorithm)
    {
        return Decrypt(
            encryptedKey,
            recipientPrivateKey,
            ephemeralPublicKey,
            algorithm,
            contentEncryptionAlgorithm,
            Array.Empty<byte>(),
            Array.Empty<byte>());
    }

    /// <summary>Derives an AES wrapping key and unwraps using decoded JWE PartyInfo values.</summary>
    /// <param name="encryptedKey">The JWE encrypted-key value.</param>
    /// <param name="recipientPrivateKey">The recipient's static private EC key.</param>
    /// <param name="ephemeralPublicKey">The public EC key from the JWE <c>epk</c> header.</param>
    /// <param name="algorithm">The case-sensitive JWE ECDH-ES with AES Key Wrap algorithm name.</param>
    /// <param name="contentEncryptionAlgorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <param name="partyUInfo">The decoded JWE <c>apu</c> value.</param>
    /// <param name="partyVInfo">The decoded JWE <c>apv</c> value.</param>
    /// <returns>A result containing the authenticated content-encryption key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static EcdhKeyManagementResult Decrypt(
        byte[] encryptedKey,
        TokenKey recipientPrivateKey,
        TokenKey ephemeralPublicKey,
        string algorithm,
        string contentEncryptionAlgorithm,
        byte[] partyUInfo,
        byte[] partyVInfo)
    {
        if (encryptedKey == null)
        {
            throw new ArgumentNullException("encryptedKey");
        }

        if (partyUInfo == null)
        {
            throw new ArgumentNullException("partyUInfo");
        }

        if (partyVInfo == null)
        {
            throw new ArgumentNullException("partyVInfo");
        }

        return Decrypt(
            encryptedKey.AsSpan(),
            recipientPrivateKey,
            ephemeralPublicKey,
            algorithm,
            contentEncryptionAlgorithm,
            partyUInfo.AsSpan(),
            partyVInfo.AsSpan());
    }

    /// <summary>Derives an AES wrapping key and unwraps encrypted-key and PartyInfo spans.</summary>
    /// <param name="encryptedKey">The JWE encrypted-key value.</param>
    /// <param name="recipientPrivateKey">The recipient's static private EC key.</param>
    /// <param name="ephemeralPublicKey">The public EC key from the JWE <c>epk</c> header.</param>
    /// <param name="algorithm">The case-sensitive JWE ECDH-ES with AES Key Wrap algorithm name.</param>
    /// <param name="contentEncryptionAlgorithm">The case-sensitive JWE <c>enc</c> algorithm name.</param>
    /// <param name="partyUInfo">The decoded JWE <c>apu</c> value.</param>
    /// <param name="partyVInfo">The decoded JWE <c>apv</c> value.</param>
    /// <returns>A result containing the authenticated content-encryption key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static EcdhKeyManagementResult Decrypt(
        ReadOnlySpan<byte> encryptedKey,
        TokenKey recipientPrivateKey,
        TokenKey ephemeralPublicKey,
        string algorithm,
        string contentEncryptionAlgorithm,
        ReadOnlySpan<byte> partyUInfo,
        ReadOnlySpan<byte> partyVInfo)
    {
        ValidateReferences(
            recipientPrivateKey,
            algorithm,
            contentEncryptionAlgorithm);
        if (ephemeralPublicKey == null)
        {
            throw new ArgumentNullException("ephemeralPublicKey");
        }

        TokenResult recipientValidation = Validate(
            recipientPrivateKey,
            algorithm,
            contentEncryptionAlgorithm,
            true);
        if (!recipientValidation.Success)
        {
            return Failure(recipientValidation.Error);
        }

        if (!IsKeyWrapAlgorithm(algorithm))
        {
            return Failure(
                "invalid_algorithm",
                "The JOSE algorithm is not ECDH-ES with AES Key Wrap.");
        }

        int expectedSize = GetContentEncryptionKeySize(
            contentEncryptionAlgorithm);
        if (encryptedKey.Length != expectedSize + 8)
        {
            return UnwrapFailure();
        }

        TokenResult ephemeralValidation = ValidateEphemeral(
            ephemeralPublicKey,
            algorithm);
        if (!ephemeralValidation.Success)
        {
            return Failure(ephemeralValidation.Error);
        }

        ECParameters recipientParameters = (recipientPrivateKey.Ecdsa ?? throw new InvalidOperationException("A required value is null.")).ExportParameters(true);
        ECParameters ephemeralParameters = (ephemeralPublicKey.Ecdsa ?? throw new InvalidOperationException("A required value is null.")).ExportParameters(false);
        if (!SameCurve(recipientParameters.Curve, ephemeralParameters.Curve))
        {
            ZeroPrivate(recipientParameters);
            return Failure(
                "curve_mismatch",
                "The recipient and ephemeral keys must use the same curve.");
        }

        TokenBytesResult derived = Derive(
            recipientParameters,
            ephemeralParameters,
            algorithm,
            partyUInfo,
            partyVInfo,
            GetKeyWrapKeySize(algorithm));
        ZeroPrivate(recipientParameters);
        if (!derived.Success)
        {
            return Failure(derived.Error);
        }

        TokenKey keyEncryptionKey = new TokenKey();
        keyEncryptionKey.KeyType = TokenKeyTypes.Symmetric;
        keyEncryptionKey.SymmetricKey = derived.Bytes;
        TokenBytesResult unwrapped = AesKeyWrap.Unwrap(
            encryptedKey,
            keyEncryptionKey,
            GetKeyWrapAlgorithm(algorithm));
        keyEncryptionKey.ReleaseOwnedMaterial();
        if (!unwrapped.Success || (unwrapped.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length != expectedSize)
        {
            if (unwrapped.Bytes != null)
            {
                CryptographicOperations.ZeroMemory(unwrapped.Bytes);
            }

            return UnwrapFailure();
        }

        EcdhKeyManagementResult result = new EcdhKeyManagementResult();
        result.Success = true;
        result.ContentEncryptionKey = unwrapped.Bytes;
        result.EncryptedKey = encryptedKey.ToArray();
        return result;
    }

    static TokenBytesResult Derive(
        ECParameters privateParameters,
        ECParameters publicParameters,
        string algorithmId,
        ReadOnlySpan<byte> partyUInfo,
        ReadOnlySpan<byte> partyVInfo,
        int keySizeInBytes)
    {
        byte[] partyUCopy = partyUInfo.ToArray();
        byte[] partyVCopy = partyVInfo.ToArray();
        Task<byte[]?> operation = Task.Run(() => DeriveCore(
            privateParameters,
            publicParameters,
            algorithmId,
            partyUCopy,
            partyVCopy,
            keySizeInBytes));
        Wait(operation);
        if (!operation.IsCompletedSuccessfully || operation.Result == null)
        {
            ObserveFailure(operation);
            return BytesFailure(
                "key_agreement_failed",
                "The ECDH provider could not derive the content-encryption key.");
        }

        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = operation.Result;
        return result;
    }

    static byte[]? DeriveCore(
        ECParameters privateParameters,
        ECParameters publicParameters,
        string algorithmId,
        byte[] partyUInfo,
        byte[] partyVInfo,
        int keySizeInBytes)
    {
        byte[] sharedSecret;
        using (ECDiffieHellman privateAgreement = ECDiffieHellman.Create(
            privateParameters))
        using (ECDiffieHellman publicAgreement = ECDiffieHellman.Create(
            publicParameters))
        {
            sharedSecret = privateAgreement.DeriveRawSecretAgreement(
                publicAgreement.PublicKey);
        }

        TokenBytesResult derived = ConcatKdfOperations.DeriveKey(
            sharedSecret,
            algorithmId,
            partyUInfo,
            partyVInfo,
            keySizeInBytes * 8);
        CryptographicOperations.ZeroMemory(sharedSecret);
        return derived.Bytes;
    }

    static TokenResult Validate(
        TokenKey key,
        string algorithm,
        string contentEncryptionAlgorithm,
        bool requirePrivate)
    {
        TokenResult algorithmValidation = ValidateAlgorithm(algorithm);
        if (!algorithmValidation.Success)
        {
            return algorithmValidation;
        }

        TokenResult contentValidation = ValidateContentEncryptionAlgorithm(
            contentEncryptionAlgorithm);
        if (!contentValidation.Success)
        {
            return contentValidation;
        }

        TokenResult keyValidation = EcdsaKeyImporter.Validate(
            key,
            requirePrivate,
            false);
        if (!keyValidation.Success)
        {
            return keyValidation;
        }

        if (key.Algorithm != null
            && !string.Equals(key.Algorithm, algorithm, StringComparison.Ordinal))
        {
            return ValidationFailure(
                "conflicting_key_algorithm",
                "The key algorithm does not match JWE ECDH-ES.");
        }

        if (key.HasCoseAlgorithm)
        {
            int[] mapped = JoseCoseAlgorithmMap.GetCoseLabels(algorithm);
            if (!RegistryLookup.Contains(mapped, key.CoseAlgorithm))
            {
                return ValidationFailure(
                    "conflicting_key_algorithm",
                    "The COSE key algorithm is not equivalent to the JWE ECDH algorithm.");
            }
        }

        if (key.Use != null
            && !string.Equals(
                key.Use,
                TokenKeyUses.Encryption,
                StringComparison.Ordinal))
        {
            return ValidationFailure(
                "key_use_algorithm_mismatch",
                "The key use does not permit JWE ECDH key agreement.");
        }

        if (key.KeyOperations != null
            && key.KeyOperations.Length > 0
            && !RegistryLookup.Contains(
                key.KeyOperations,
                TokenKeyOperations.DeriveKey,
                false))
        {
            return ValidationFailure(
                "key_operation_not_permitted",
                "The key metadata does not permit key derivation.");
        }

        if (key.CoseKeyOperations != null
            && key.CoseKeyOperations.Length > 0
            && !RegistryLookup.Contains(
                key.CoseKeyOperations,
                CoseKeyLabels.OperationDeriveKey))
        {
            return ValidationFailure(
                "key_operation_not_permitted",
                "The COSE key metadata does not permit key derivation.");
        }

        return ValidationSuccess();
    }

    static TokenResult ValidateEphemeral(TokenKey key, string algorithm)
    {
        TokenResult validation = EcdsaKeyImporter.Validate(key, false, false);
        if (!validation.Success)
        {
            return validation;
        }

        if (key.NativePrivateMaterial)
        {
            return ValidationFailure(
                "invalid_ephemeral_key",
                "The JWE ephemeral key must contain only public key material.");
        }

        if (key.Algorithm != null
            && !string.Equals(
                key.Algorithm,
                algorithm,
                StringComparison.Ordinal))
        {
            return ValidationFailure(
                "conflicting_key_algorithm",
                "The ephemeral key algorithm does not match JWE ECDH-ES.");
        }

        if (key.HasCoseAlgorithm)
        {
            int[] mapped = JoseCoseAlgorithmMap.GetCoseLabels(algorithm);
            if (!RegistryLookup.Contains(mapped, key.CoseAlgorithm))
            {
                return ValidationFailure(
                    "conflicting_key_algorithm",
                    "The ephemeral COSE key algorithm is not equivalent to the JWE ECDH algorithm.");
            }
        }

        return ValidationSuccess();
    }

    static TokenResult ValidateAlgorithm(string algorithm)
    {
        if (string.Equals(algorithm, JoseAlgorithms.EcdhEs, StringComparison.Ordinal)
            || IsKeyWrapAlgorithm(algorithm))
        {
            return ValidationSuccess();
        }

        TokenResult availability = CryptoAlgorithmAvailability.CheckJoseAlgorithm(
            algorithm);
        if (!availability.Success)
        {
            return availability;
        }

        return ValidationFailure(
            "invalid_algorithm",
            "The JOSE algorithm is not a supported ECDH-ES mode.");
    }

    static TokenResult ValidateContentEncryptionAlgorithm(string algorithm)
    {
        if (GetContentEncryptionKeySize(algorithm) != 0)
        {
            return ValidationSuccess();
        }

        TokenResult availability = CryptoAlgorithmAvailability.CheckJoseAlgorithm(
            algorithm);
        if (!availability.Success)
        {
            return availability;
        }

        return ValidationFailure(
            "invalid_algorithm",
            "The JOSE algorithm is not a supported content-encryption algorithm.");
    }

    static int GetContentEncryptionKeySize(string algorithm)
    {
        if (string.Equals(algorithm, JoseAlgorithms.Aes128Gcm, StringComparison.Ordinal))
        {
            return 16;
        }

        if (string.Equals(algorithm, JoseAlgorithms.Aes192Gcm, StringComparison.Ordinal))
        {
            return 24;
        }

        if (string.Equals(algorithm, JoseAlgorithms.Aes256Gcm, StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.Aes128CbcHmacSha256,
                StringComparison.Ordinal))
        {
            return 32;
        }

        if (string.Equals(
            algorithm,
            JoseAlgorithms.Aes192CbcHmacSha384,
            StringComparison.Ordinal))
        {
            return 48;
        }

        if (string.Equals(
            algorithm,
            JoseAlgorithms.Aes256CbcHmacSha512,
            StringComparison.Ordinal))
        {
            return 64;
        }

        return 0;
    }

    static bool IsKeyWrapAlgorithm(string algorithm)
    {
        return string.Equals(
                algorithm,
                JoseAlgorithms.EcdhEsAes128KeyWrap,
                StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.EcdhEsAes192KeyWrap,
                StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.EcdhEsAes256KeyWrap,
                StringComparison.Ordinal);
    }

    static int GetKeyWrapKeySize(string algorithm)
    {
        if (string.Equals(
            algorithm,
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            StringComparison.Ordinal))
        {
            return 16;
        }

        if (string.Equals(
            algorithm,
            JoseAlgorithms.EcdhEsAes192KeyWrap,
            StringComparison.Ordinal))
        {
            return 24;
        }

        return 32;
    }

    static string GetKeyWrapAlgorithm(string algorithm)
    {
        if (string.Equals(
            algorithm,
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            StringComparison.Ordinal))
        {
            return JoseAlgorithms.Aes128KeyWrap;
        }

        if (string.Equals(
            algorithm,
            JoseAlgorithms.EcdhEsAes192KeyWrap,
            StringComparison.Ordinal))
        {
            return JoseAlgorithms.Aes192KeyWrap;
        }

        return JoseAlgorithms.Aes256KeyWrap;
    }

    static bool SameCurve(ECCurve first, ECCurve second)
    {
        return string.Equals(
            first.Oid.Value,
            second.Oid.Value,
            StringComparison.Ordinal);
    }

    static void ZeroPrivate(ECParameters parameters)
    {
        if (parameters.D != null)
        {
            CryptographicOperations.ZeroMemory(parameters.D);
        }
    }

    static void ValidateReferences(
        TokenKey key,
        string algorithm,
        string contentEncryptionAlgorithm)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (algorithm == null)
        {
            throw new ArgumentNullException("algorithm");
        }

        if (contentEncryptionAlgorithm == null)
        {
            throw new ArgumentNullException("contentEncryptionAlgorithm");
        }
    }

    static void Wait(Task<byte[]?> operation)
    {
        while (!operation.IsCompleted)
        {
            Thread.Yield();
        }
    }

    static void ObserveFailure(Task<byte[]?> operation)
    {
        AggregateException? failure = operation.Exception;
        if (failure != null)
        {
            failure.Flatten();
        }
    }

    static EcdhKeyManagementResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static EcdhKeyManagementResult Failure(TokenError? error)
    {
        EcdhKeyManagementResult result = new EcdhKeyManagementResult();
        result.Error = error;
        return result;
    }

    static EcdhKeyManagementResult UnwrapFailure()
    {
        return Failure(
            "unwrap_failed",
            "The ECDH-derived AES key could not unwrap the content-encryption key.");
    }

    static TokenBytesResult BytesFailure(string code, string message)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }

    static TokenResult ValidationSuccess()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult ValidationFailure(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }
}
