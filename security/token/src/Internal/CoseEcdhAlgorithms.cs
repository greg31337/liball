using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace token;

/// <summary>Applies COSE EC2 ECDH direct agreement and agreement with AES Key Wrap.</summary>
internal static class CoseEcdhAlgorithms
{
    /// <summary>Derives a direct content key for a COSE recipient.</summary>
    /// <param name="recipientPublicKey">The recipient static public EC2 key.</param>
    /// <param name="senderPrivateKey">The sender static private key for ECDH-SS, or <c>null</c> for ECDH-ES.</param>
    /// <param name="algorithm">The COSE direct ECDH algorithm label.</param>
    /// <param name="salt">The HKDF salt, or <c>null</c> when omitted.</param>
    /// <param name="parameters">The COSE KDF context inputs.</param>
    /// <returns>The derived content key, empty recipient ciphertext, and generated ephemeral key when applicable.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required reference is null.</exception>
    internal static CoseEcdhResult Derive(
        TokenKey recipientPublicKey,
        TokenKey? senderPrivateKey,
        int algorithm,
        byte[]? salt,
        CoseKdfContextParameters parameters)
    {
        if (recipientPublicKey == null)
        {
            throw new ArgumentNullException("recipientPublicKey");
        }

        if (parameters == null)
        {
            throw new ArgumentNullException("parameters");
        }

        TokenResult algorithmResult = ValidateAlgorithm(algorithm);
        if (!algorithmResult.Success)
        {
            return Failure(algorithmResult.Error);
        }

        if (IsKeyWrap(algorithm))
        {
            return Failure(
                "invalid_algorithm",
                "The COSE ECDH algorithm derives a wrapping key rather than a content key.");
        }

        TokenResult contextResult = ValidateContext(
            parameters,
            algorithm,
            salt);
        if (!contextResult.Success)
        {
            return Failure(contextResult.Error);
        }

        return AgreeForSender(
            recipientPublicKey,
            senderPrivateKey,
            algorithm,
            salt,
            parameters,
            null);
    }

    /// <summary>Wraps a content key for a COSE ECDH recipient.</summary>
    /// <param name="contentKey">The content key to wrap.</param>
    /// <param name="recipientPublicKey">The recipient static public EC2 key.</param>
    /// <param name="senderPrivateKey">The sender static private key for ECDH-SS, or <c>null</c> for ECDH-ES.</param>
    /// <param name="algorithm">The COSE ECDH with AES Key Wrap algorithm label.</param>
    /// <param name="salt">The HKDF salt, or <c>null</c> when omitted.</param>
    /// <param name="parameters">The COSE KDF context inputs.</param>
    /// <returns>The copied content key, wrapped recipient ciphertext, and generated ephemeral key when applicable.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required reference is null.</exception>
    internal static CoseEcdhResult Wrap(
        byte[] contentKey,
        TokenKey recipientPublicKey,
        TokenKey? senderPrivateKey,
        int algorithm,
        byte[]? salt,
        CoseKdfContextParameters parameters)
    {
        if (contentKey == null)
        {
            throw new ArgumentNullException("contentKey");
        }

        return Wrap(
            contentKey.AsSpan(),
            recipientPublicKey,
            senderPrivateKey,
            algorithm,
            salt,
            parameters);
    }

    /// <summary>Wraps a content-key span for a COSE ECDH recipient.</summary>
    /// <param name="contentKey">The content key to wrap.</param>
    /// <param name="recipientPublicKey">The recipient static public EC2 key.</param>
    /// <param name="senderPrivateKey">The sender static private key for ECDH-SS, or <c>null</c> for ECDH-ES.</param>
    /// <param name="algorithm">The COSE ECDH with AES Key Wrap algorithm label.</param>
    /// <param name="salt">The HKDF salt, or <c>null</c> when omitted.</param>
    /// <param name="parameters">The COSE KDF context inputs.</param>
    /// <returns>The copied content key, wrapped recipient ciphertext, and generated ephemeral key when applicable.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required reference is null.</exception>
    internal static CoseEcdhResult Wrap(
        ReadOnlySpan<byte> contentKey,
        TokenKey recipientPublicKey,
        TokenKey? senderPrivateKey,
        int algorithm,
        byte[]? salt,
        CoseKdfContextParameters parameters)
    {
        if (recipientPublicKey == null)
        {
            throw new ArgumentNullException("recipientPublicKey");
        }

        if (parameters == null)
        {
            throw new ArgumentNullException("parameters");
        }

        TokenResult algorithmResult = ValidateAlgorithm(algorithm);
        if (!algorithmResult.Success)
        {
            return Failure(algorithmResult.Error);
        }

        if (!IsKeyWrap(algorithm))
        {
            return Failure(
                "invalid_algorithm",
                "The COSE ECDH algorithm does not use AES Key Wrap.");
        }

        if (contentKey.Length < 16 || contentKey.Length % 8 != 0)
        {
            return Failure(
                "invalid_key_size",
                "AES Key Wrap content must contain at least two 64-bit blocks.");
        }

        TokenResult contextResult = ValidateContext(
            parameters,
            algorithm,
            salt);
        if (!contextResult.Success)
        {
            return Failure(contextResult.Error);
        }

        return AgreeForSender(
            recipientPublicKey,
            senderPrivateKey,
            algorithm,
            salt,
            parameters,
            contentKey.ToArray());
    }

    /// <summary>Processes a COSE ECDH recipient during decryption.</summary>
    /// <param name="encryptedKey">The empty or wrapped recipient ciphertext.</param>
    /// <param name="recipientPrivateKey">The recipient static private EC2 key.</param>
    /// <param name="senderPublicKey">The sender ephemeral or static public EC2 key.</param>
    /// <param name="algorithm">The COSE ECDH recipient algorithm label.</param>
    /// <param name="salt">The HKDF salt, or <c>null</c> when omitted.</param>
    /// <param name="parameters">The COSE KDF context inputs.</param>
    /// <returns>The derived or unwrapped content key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required reference is null.</exception>
    internal static CoseEcdhResult Decrypt(
        byte[] encryptedKey,
        TokenKey recipientPrivateKey,
        TokenKey senderPublicKey,
        int algorithm,
        byte[]? salt,
        CoseKdfContextParameters parameters)
    {
        if (encryptedKey == null)
        {
            throw new ArgumentNullException("encryptedKey");
        }

        return Decrypt(
            encryptedKey.AsSpan(),
            recipientPrivateKey,
            senderPublicKey,
            algorithm,
            salt,
            parameters);
    }

    /// <summary>Processes a COSE ECDH recipient ciphertext span during decryption.</summary>
    /// <param name="encryptedKey">The empty or wrapped recipient ciphertext.</param>
    /// <param name="recipientPrivateKey">The recipient static private EC2 key.</param>
    /// <param name="senderPublicKey">The sender ephemeral or static public EC2 key.</param>
    /// <param name="algorithm">The COSE ECDH recipient algorithm label.</param>
    /// <param name="salt">The HKDF salt, or <c>null</c> when omitted.</param>
    /// <param name="parameters">The COSE KDF context inputs.</param>
    /// <returns>The derived or unwrapped content key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required reference is null.</exception>
    internal static CoseEcdhResult Decrypt(
        ReadOnlySpan<byte> encryptedKey,
        TokenKey recipientPrivateKey,
        TokenKey senderPublicKey,
        int algorithm,
        byte[]? salt,
        CoseKdfContextParameters parameters)
    {
        if (recipientPrivateKey == null)
        {
            throw new ArgumentNullException("recipientPrivateKey");
        }

        if (senderPublicKey == null)
        {
            throw new ArgumentNullException("senderPublicKey");
        }

        if (parameters == null)
        {
            throw new ArgumentNullException("parameters");
        }

        TokenResult algorithmResult = ValidateAlgorithm(algorithm);
        if (!algorithmResult.Success)
        {
            return Failure(algorithmResult.Error);
        }

        TokenResult contextResult = ValidateContext(
            parameters,
            algorithm,
            salt);
        if (!contextResult.Success)
        {
            return Failure(contextResult.Error);
        }

        if (!IsKeyWrap(algorithm) && encryptedKey.Length != 0)
        {
            return Failure(
                "invalid_encrypted_key",
                "A direct ECDH recipient ciphertext must be empty.");
        }

        if (IsKeyWrap(algorithm)
            && (encryptedKey.Length < 24
                || encryptedKey.Length % 8 != 0))
        {
            return UnwrapFailure();
        }

        TokenResult recipientResult = ValidatePrivateKey(
            recipientPrivateKey,
            algorithm);
        if (!recipientResult.Success)
        {
            return Failure(recipientResult.Error);
        }

        TokenResult senderResult = ValidatePublicKey(
            senderPublicKey,
            algorithm);
        if (!senderResult.Success)
        {
            return Failure(senderResult.Error);
        }

        TokenBytesResult agreement = Agree(
            recipientPrivateKey,
            senderPublicKey,
            algorithm,
            salt,
            parameters);
        if (!agreement.Success)
        {
            return Failure(agreement.Error);
        }

        if (!IsKeyWrap(algorithm))
        {
            return Success(
                agreement.Bytes,
                Array.Empty<byte>(),
                null);
        }

        TokenKey keyEncryptionKey = SymmetricKey(
            agreement.Bytes,
            GetKeyWrapAlgorithm(algorithm));
        TokenBytesResult unwrapped = CoseAesKeyWrap.Unwrap(
            encryptedKey,
            keyEncryptionKey,
            GetKeyWrapAlgorithm(algorithm));
        keyEncryptionKey.ReleaseOwnedMaterial();
        if (!unwrapped.Success)
        {
            return UnwrapFailure();
        }

        return Success(
            unwrapped.Bytes,
            encryptedKey.ToArray(),
            null);
    }

    static CoseEcdhResult AgreeForSender(
        TokenKey recipientPublicKey,
        TokenKey? senderPrivateKey,
        int algorithm,
        byte[]? salt,
        CoseKdfContextParameters parameters,
        byte[]? contentKey)
    {
        TokenResult recipientResult = ValidatePublicKey(
            recipientPublicKey,
            algorithm);
        if (!recipientResult.Success)
        {
            return Failure(recipientResult.Error);
        }

        if (IsStaticStatic(algorithm))
        {
            if (senderPrivateKey == null)
            {
                throw new ArgumentNullException("senderPrivateKey");
            }

            TokenResult senderResult = ValidatePrivateKey(
                senderPrivateKey,
                algorithm);
            if (!senderResult.Success)
            {
                return Failure(senderResult.Error);
            }

            TokenBytesResult staticAgreement = Agree(
                senderPrivateKey,
                recipientPublicKey,
                algorithm,
                salt,
                parameters);
            if (!staticAgreement.Success)
            {
                return Failure(staticAgreement.Error);
            }

            return FinishSender(
                staticAgreement.Bytes,
                contentKey,
                algorithm,
                null);
        }

        if (senderPrivateKey != null)
        {
            return Failure(
                "invalid_sender_key",
                "ECDH-ES generates a fresh ephemeral sender key.");
        }

        ECParameters recipientParameters =
            (recipientPublicKey.Ecdsa ?? throw new InvalidOperationException("A required value is null.")).ExportParameters(false);
        using (ECDiffieHellman ephemeral = ECDiffieHellman.Create(
            recipientParameters.Curve))
        {
            ECParameters ephemeralPrivate =
                ephemeral.ExportParameters(true);
            ECParameters ephemeralPublic =
                ephemeral.ExportParameters(false);
            ECDsa publicEcdsa = ECDsa.Create(ephemeralPublic);
            TokenKeyResult imported = EcdsaKeyImporter.Import(
                publicEcdsa,
                false);
            if (!imported.Success)
            {
                publicEcdsa.Dispose();
                ZeroPrivate(ephemeralPrivate);
                return Failure(imported.Error);
            }

            (imported.Key ?? throw new InvalidOperationException("A required value is null.")).OwnsNativeKey = true;
            TokenBytesResult agreement = AgreeParameters(
                ephemeralPrivate,
                recipientParameters,
                algorithm,
                salt,
                parameters);
            ZeroPrivate(ephemeralPrivate);
            if (!agreement.Success)
            {
                imported.Key.ReleaseOwnedMaterial();
                return Failure(agreement.Error);
            }

            return FinishSender(
                agreement.Bytes,
                contentKey,
                algorithm,
                imported.Key);
        }
    }

    static CoseEcdhResult FinishSender(
        byte[]? derivedKey,
        byte[]? contentKey,
        int algorithm,
        TokenKey? senderPublicKey)
    {
        if (!IsKeyWrap(algorithm))
        {
            return Success(
                derivedKey,
                Array.Empty<byte>(),
                senderPublicKey);
        }

        TokenKey keyEncryptionKey = SymmetricKey(
            derivedKey,
            GetKeyWrapAlgorithm(algorithm));
        TokenBytesResult wrapped = CoseAesKeyWrap.Wrap(
            (contentKey ?? throw new InvalidOperationException("A required value is null.")),
            keyEncryptionKey,
            GetKeyWrapAlgorithm(algorithm));
        keyEncryptionKey.ReleaseOwnedMaterial();
        if (!wrapped.Success)
        {
            if (senderPublicKey != null)
            {
                senderPublicKey.ReleaseOwnedMaterial();
            }

            return Failure(wrapped.Error);
        }

        return Success(
            Copy(contentKey),
            wrapped.Bytes,
            senderPublicKey);
    }

    static TokenBytesResult Agree(
        TokenKey privateKey,
        TokenKey publicKey,
        int algorithm,
        byte[]? salt,
        CoseKdfContextParameters parameters)
    {
        ECParameters privateParameters =
            (privateKey.Ecdsa ?? throw new InvalidOperationException("A required value is null.")).ExportParameters(true);
        ECParameters publicParameters =
            (publicKey.Ecdsa ?? throw new InvalidOperationException("A required value is null.")).ExportParameters(false);
        if (!SameCurve(
            privateParameters.Curve,
            publicParameters.Curve))
        {
            ZeroPrivate(privateParameters);
            return BytesFailure(
                "curve_mismatch",
                "The ECDH keys must use the same curve.");
        }

        TokenBytesResult result = AgreeParameters(
            privateParameters,
            publicParameters,
            algorithm,
            salt,
            parameters);
        ZeroPrivate(privateParameters);
        return result;
    }

    static TokenBytesResult AgreeParameters(
        ECParameters privateParameters,
        ECParameters publicParameters,
        int algorithm,
        byte[]? salt,
        CoseKdfContextParameters parameters)
    {
        TokenBytesResult context = CoseKdfContext.Create(parameters);
        if (!context.Success)
        {
            return context;
        }

        byte[] actualSalt = salt == null
            ? Array.Empty<byte>()
            : Copy(salt);
        Task<byte[]> operation = Task.Run(() => AgreeCore(
            privateParameters,
            publicParameters,
            algorithm,
            actualSalt,
            context.Bytes,
            (int)(parameters.KeyDataLength / 8UL)));
        Wait(operation);
        CryptographicOperations.ZeroMemory(actualSalt);
        CryptographicOperations.ZeroMemory(context.Bytes);
        if (!operation.IsCompletedSuccessfully)
        {
            ObserveFailure(operation);
            return BytesFailure(
                "key_agreement_failed",
                "The ECDH provider could not derive the COSE recipient key.");
        }

        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = operation.Result;
        return result;
    }

    static byte[] AgreeCore(
        ECParameters privateParameters,
        ECParameters publicParameters,
        int algorithm,
        byte[] salt,
        byte[]? context,
        int outputLength)
    {
        byte[] sharedSecret;
        using (ECDiffieHellman privateAgreement =
            ECDiffieHellman.Create(privateParameters))
        using (ECDiffieHellman publicAgreement =
            ECDiffieHellman.Create(publicParameters))
        {
            sharedSecret = privateAgreement.DeriveRawSecretAgreement(
                publicAgreement.PublicKey);
        }

        HashAlgorithmName hashAlgorithm = UsesSha512(algorithm)
            ? HashAlgorithmName.SHA512
            : HashAlgorithmName.SHA256;
        byte[] derived = HKDF.DeriveKey(
            hashAlgorithm,
            sharedSecret,
            outputLength,
            salt,
            context);
        CryptographicOperations.ZeroMemory(sharedSecret);
        return derived;
    }

    static TokenResult ValidateContext(
        CoseKdfContextParameters parameters,
        int algorithm,
        byte[]? salt)
    {
        if (parameters.KeyDataLength == 0
            || parameters.KeyDataLength % 8UL != 0
            || parameters.KeyDataLength
                > (ulong)CoseDirectKeyDistribution.MaximumKeySizeInBits)
        {
            return ValidationFailure(
                "invalid_key_size",
                "The COSE ECDH output key size is outside the permitted range.");
        }

        if (!UsesSha512(algorithm)
            && parameters.KeyDataLength / 8UL > 8160UL)
        {
            return ValidationFailure(
                "invalid_key_size",
                "HKDF SHA-256 output cannot exceed 255 hash-length blocks.");
        }

        int expectedAlgorithm = IsKeyWrap(algorithm)
            ? GetKeyWrapAlgorithm(algorithm)
            : 0;
        if (IsKeyWrap(algorithm)
            && (parameters.KeyDataLength
                    != (ulong)(GetKeyWrapKeySize(algorithm) * 8)
                || !IsIntegerAlgorithm(
                    parameters.AlgorithmIdentifier,
                    expectedAlgorithm)))
        {
            return ValidationFailure(
                "invalid_cose_kdf_context",
                "The COSE KDF context does not match the AES Key Wrap algorithm.");
        }

        if (IsStaticStatic(algorithm)
            && salt == null
            && !HasPartyUNonce(parameters))
        {
            return ValidationFailure(
                "missing_kdf_uniqueness",
                "ECDH-SS requires a salt or PartyU nonce.");
        }

        TokenBytesResult context = CoseKdfContext.Create(parameters);
        if (!context.Success)
        {
            return ValidationFailure(context.Error);
        }

        CryptographicOperations.ZeroMemory(context.Bytes);
        return ValidationSuccess();
    }

    static TokenResult ValidatePrivateKey(
        TokenKey key,
        int algorithm)
    {
        TokenResult validation = ValidateEc2(key, true);
        if (!validation.Success)
        {
            return validation;
        }

        TokenResult metadata = ValidateKeyMetadata(key, algorithm);
        if (!metadata.Success)
        {
            return metadata;
        }

        if (key.KeyOperations != null
            && key.KeyOperations.Length > 0
            && !RegistryLookup.Contains(
                key.KeyOperations,
                TokenKeyOperations.DeriveKey,
                false)
            && !RegistryLookup.Contains(
                key.KeyOperations,
                TokenKeyOperations.DeriveBits,
                false))
        {
            return ValidationFailure(
                "key_operation_not_permitted",
                "The private ECDH key does not permit key derivation.");
        }

        if (key.CoseKeyOperations != null
            && key.CoseKeyOperations.Length > 0
            && !RegistryLookup.Contains(
                key.CoseKeyOperations,
                CoseKeyLabels.OperationDeriveKey)
            && !RegistryLookup.Contains(
                key.CoseKeyOperations,
                CoseKeyLabels.OperationDeriveBits))
        {
            return ValidationFailure(
                "key_operation_not_permitted",
                "The private COSE ECDH key does not permit key derivation.");
        }

        return ValidationSuccess();
    }

    static TokenResult ValidatePublicKey(
        TokenKey key,
        int algorithm)
    {
        TokenResult validation = ValidateEc2(key, false);
        if (!validation.Success)
        {
            return validation;
        }

        if (key.NativePrivateMaterial)
        {
            return ValidationFailure(
                "invalid_public_key",
                "A COSE ECDH public key must not expose private material.");
        }

        TokenResult metadata = ValidateKeyMetadata(key, algorithm);
        if (!metadata.Success)
        {
            return metadata;
        }

        if (key.KeyOperations != null
                && key.KeyOperations.Length > 0
            || key.CoseKeyOperations != null
                && key.CoseKeyOperations.Length > 0)
        {
            return ValidationFailure(
                "key_operation_not_permitted",
                "A public COSE ECDH key must have no key operations.");
        }

        return ValidationSuccess();
    }

    static TokenResult ValidateEc2(TokenKey key, bool requirePrivate)
    {
        if (key.KeyType != null
            && !string.Equals(
                key.KeyType,
                TokenKeyTypes.EllipticCurve,
                StringComparison.Ordinal)
            || key.CoseKeyType != 0
                && key.CoseKeyType != CoseKeyLabels.KeyTypeEc2)
        {
            return ValidationFailure(
                "invalid_key_type",
                "COSE ECDH requires a supported EC2 key.");
        }

        if (key.Ecdsa == null)
        {
            return ValidationFailure(
                "missing_key_material",
                "COSE ECDH requires a native EC2 key.");
        }

        if (requirePrivate && !key.NativePrivateMaterial)
        {
            return ValidationFailure(
                "missing_private_key",
                "COSE ECDH requires private key material.");
        }

        return EcdsaKeyImporter.Validate(key, requirePrivate, false);
    }

    static TokenResult ValidateKeyMetadata(TokenKey key, int algorithm)
    {
        if (key.Algorithm != null)
        {
            int[] mapped = JoseCoseAlgorithmMap.GetCoseLabels(
                key.Algorithm);
            if (!RegistryLookup.Contains(mapped, algorithm))
            {
                return ValidationFailure(
                    "conflicting_key_algorithm",
                    "The JOSE key algorithm does not match the COSE ECDH algorithm.");
            }
        }

        if (key.HasCoseAlgorithm
            && key.CoseAlgorithm != algorithm)
        {
            return ValidationFailure(
                "conflicting_key_algorithm",
                "The COSE key algorithm does not match the ECDH operation.");
        }

        if (key.Use != null
            && !string.Equals(
                key.Use,
                TokenKeyUses.Encryption,
                StringComparison.Ordinal))
        {
            return ValidationFailure(
                "key_use_algorithm_mismatch",
                "The key use does not permit COSE ECDH key agreement.");
        }

        return ValidationSuccess();
    }

    static TokenResult ValidateAlgorithm(int algorithm)
    {
        TokenResult availability =
            CryptoAlgorithmAvailability.CheckCoseAlgorithm(algorithm);
        if (!availability.Success)
        {
            return availability;
        }

        if (!IsEcdh(algorithm))
        {
            return ValidationFailure(
                "invalid_algorithm",
                "The COSE algorithm is not an ECDH recipient algorithm.");
        }

        return ValidationSuccess();
    }

    static bool IsEcdh(int algorithm)
    {
        return algorithm == CoseAlgorithms.EcdhEsHkdfSha256
            || algorithm == CoseAlgorithms.EcdhEsHkdfSha512
            || algorithm == CoseAlgorithms.EcdhSsHkdfSha256
            || algorithm == CoseAlgorithms.EcdhSsHkdfSha512
            || algorithm == CoseAlgorithms.EcdhEsAes128KeyWrap
            || algorithm == CoseAlgorithms.EcdhEsAes192KeyWrap
            || algorithm == CoseAlgorithms.EcdhEsAes256KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes128KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes192KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes256KeyWrap;
    }

    static bool IsStaticStatic(int algorithm)
    {
        return algorithm == CoseAlgorithms.EcdhSsHkdfSha256
            || algorithm == CoseAlgorithms.EcdhSsHkdfSha512
            || algorithm == CoseAlgorithms.EcdhSsAes128KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes192KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes256KeyWrap;
    }

    static bool IsKeyWrap(int algorithm)
    {
        return algorithm == CoseAlgorithms.EcdhEsAes128KeyWrap
            || algorithm == CoseAlgorithms.EcdhEsAes192KeyWrap
            || algorithm == CoseAlgorithms.EcdhEsAes256KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes128KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes192KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes256KeyWrap;
    }

    static bool UsesSha512(int algorithm)
    {
        return algorithm == CoseAlgorithms.EcdhEsHkdfSha512
            || algorithm == CoseAlgorithms.EcdhSsHkdfSha512;
    }

    static int GetKeyWrapAlgorithm(int algorithm)
    {
        if (algorithm == CoseAlgorithms.EcdhEsAes128KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes128KeyWrap)
        {
            return CoseAlgorithms.Aes128KeyWrap;
        }

        if (algorithm == CoseAlgorithms.EcdhEsAes192KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes192KeyWrap)
        {
            return CoseAlgorithms.Aes192KeyWrap;
        }

        return CoseAlgorithms.Aes256KeyWrap;
    }

    static int GetKeyWrapKeySize(int algorithm)
    {
        if (algorithm == CoseAlgorithms.EcdhEsAes128KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes128KeyWrap)
        {
            return 16;
        }

        if (algorithm == CoseAlgorithms.EcdhEsAes192KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes192KeyWrap)
        {
            return 24;
        }

        return 32;
    }

    static bool IsIntegerAlgorithm(CborItem item, int algorithm)
    {
        if (item == null)
        {
            return false;
        }

        if (algorithm >= 0)
        {
            return item.Kind == TokenCborValueKind.UnsignedInteger
                && item.IntegerArgument == (ulong)algorithm;
        }

        return item.Kind == TokenCborValueKind.NegativeInteger
            && item.IntegerArgument
                == (ulong)(-(long)algorithm - 1L);
    }

    static bool HasPartyUNonce(CoseKdfContextParameters parameters)
    {
        return parameters.PartyUInfo != null
            && parameters.PartyUInfo.Nonce != null
            && parameters.PartyUInfo.Nonce.Kind
                != TokenCborValueKind.Null;
    }

    static TokenKey SymmetricKey(byte[]? value, int algorithm)
    {
        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.Symmetric;
        key.CoseKeyType = CoseKeyLabels.KeyTypeSymmetric;
        key.SymmetricKey = value;
        key.CoseAlgorithm = algorithm;
        key.HasCoseAlgorithm = true;
        return key;
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

    static void Wait(Task<byte[]> operation)
    {
        while (!operation.IsCompleted)
        {
            Thread.Yield();
        }
    }

    static void ObserveFailure(Task<byte[]> operation)
    {
        AggregateException? failure = operation.Exception;
        if (failure != null)
        {
            failure.Flatten();
        }
    }

    static byte[] Copy(byte[] value)
    {
        byte[] result = new byte[value.Length];
        value.CopyTo(result, 0);
        return result;
    }

    static CoseEcdhResult Success(
        byte[]? contentKey,
        byte[]? encryptedKey,
        TokenKey? senderPublicKey)
    {
        CoseEcdhResult result = new CoseEcdhResult();
        result.Success = true;
        result.ContentKey = contentKey;
        result.EncryptedKey = encryptedKey;
        result.SenderPublicKey = senderPublicKey;
        return result;
    }

    static CoseEcdhResult UnwrapFailure()
    {
        return Failure(
            "unwrap_failed",
            "The COSE ECDH recipient key could not be unwrapped.");
    }

    static CoseEcdhResult Failure(
        string code,
        string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static CoseEcdhResult Failure(TokenError? error)
    {
        CoseEcdhResult result = new CoseEcdhResult();
        result.Error = error;
        return result;
    }

    static TokenBytesResult BytesFailure(
        string code,
        string message)
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

    static TokenResult ValidationFailure(TokenError? error)
    {
        TokenResult result = new TokenResult();
        result.Error = error;
        return result;
    }

    static TokenResult ValidationFailure(
        string code,
        string message)
    {
        return ValidationFailure(new TokenError(code, message, -1));
    }
}
