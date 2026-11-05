using System;
using System.Security.Cryptography;

namespace token;

/// <summary>Parses, encrypts, decrypts, and performs policy-gated diagnostic reads of compact JWE structures.</summary>
internal static class JsonWebEncryption
{
    /// <summary>Encrypts plaintext as a compact JWE.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="protectedHeader">The integrity-protected JWE header.</param>
    /// <param name="key">The recipient or key-encryption key.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>A result containing compact JWE text or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenTextResult Encrypt(
        byte[] plaintext,
        JoseHeader protectedHeader,
        TokenKey key,
        TokenPolicy policy)
    {
        if (plaintext == null)
        {
            throw new ArgumentNullException("plaintext");
        }

        return Encrypt(plaintext.AsSpan(), protectedHeader, key, policy);
    }

    /// <summary>Encrypts a plaintext span as a compact JWE.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="protectedHeader">The integrity-protected JWE header.</param>
    /// <param name="key">The recipient or key-encryption key.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>A result containing compact JWE text or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static TokenTextResult Encrypt(
        ReadOnlySpan<byte> plaintext,
        JoseHeader protectedHeader,
        TokenKey key,
        TokenPolicy policy)
    {
        if (protectedHeader == null)
        {
            throw new ArgumentNullException("protectedHeader");
        }

        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult policyResult = JsonWebSignature.ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return TextFailure(policyResult.Error);
        }

        if (plaintext.Length > policy.MaximumTokenSizeInBytes)
        {
            return TextFailure(
                "token_too_large",
                "The JWE plaintext exceeds the configured token size limit.");
        }

        TokenBytesResult suppliedHeader = protectedHeader.WriteUtf8();
        if (!suppliedHeader.Success)
        {
            return TextFailure(suppliedHeader.Error);
        }

        JoseHeaderResult bounded = JoseHeader.Read(
            (suppliedHeader.Bytes ?? throw new InvalidOperationException("A required value is null.")),
            policy.MaximumJsonDepth);
        if (!bounded.Success)
        {
            return TextFailure(bounded.Error);
        }

        TokenResult headerResult = ValidateHeader(
            bounded.Header,
            policy.UnderstoodCriticalHeaders);
        if (!headerResult.Success)
        {
            return TextFailure(headerResult.Error);
        }

        JsonValue? algorithmValue = (bounded.Header ?? throw new InvalidOperationException("A required value is null.")).Get(
            JoseHeaderNames.Algorithm);
        JsonValue? encryptionValue = bounded.Header.Get(
            JoseHeaderNames.EncryptionAlgorithm);
        TokenResult algorithms = ValidateEncryptionAlgorithms(
            algorithmValue,
            encryptionValue,
            policy);
        if (!algorithms.Success)
        {
            return TextFailure(algorithms.Error);
        }

        string? algorithm = (algorithmValue ?? throw new InvalidOperationException("A required value is null.")).StringValue;
        string? encryptionAlgorithm = (encryptionValue ?? throw new InvalidOperationException("A required value is null.")).StringValue;
        byte[]? content = plaintext.ToArray();
        JsonValue? compression = bounded.Header.Get(
            JoseHeaderNames.CompressionAlgorithm);
        if (compression != null)
        {
            TokenBytesResult compressed = JoseCompression.Compress(
                content,
                (compression.StringValue ?? throw new InvalidOperationException("A required value is null.")),
                policy);
            CryptographicOperations.ZeroMemory(content);
            if (!compressed.Success)
            {
                return TextFailure(compressed.Error);
            }

            content = compressed.Bytes;
        }

        JweKeyManagementResult managed = ManageEncryptionKey(
            bounded.Header,
            key,
            algorithm,
            encryptionAlgorithm,
            policy);
        if (!managed.Success)
        {
            CryptographicOperations.ZeroMemory(content);
            return TextFailure(managed.Error);
        }

        TokenBytesResult finalHeader = bounded.Header.WriteUtf8();
        if (!finalHeader.Success)
        {
            ClearManaged(managed);
            CryptographicOperations.ZeroMemory(content);
            return TextFailure(finalHeader.Error);
        }

        JoseHeaderResult finalBounded = JoseHeader.Read(
            (finalHeader.Bytes ?? throw new InvalidOperationException("A required value is null.")),
            policy.MaximumJsonDepth);
        if (!finalBounded.Success)
        {
            ClearManaged(managed);
            CryptographicOperations.ZeroMemory(content);
            return TextFailure(finalBounded.Error);
        }

        string? protectedSegment = Base64Url.Encode(finalHeader.Bytes).Text;
        TokenBytesResult additionalAuthenticatedData = Utf8Codec.Encode(
            (protectedSegment ?? throw new InvalidOperationException("A required value is null.")));
        if (!additionalAuthenticatedData.Success)
        {
            ClearManaged(managed);
            CryptographicOperations.ZeroMemory(content);
            return TextFailure(additionalAuthenticatedData.Error);
        }

        TokenKey contentEncryptionKey = new TokenKey();
        contentEncryptionKey.KeyType = TokenKeyTypes.Symmetric;
        contentEncryptionKey.SymmetricKey = managed.ContentEncryptionKey;
        contentEncryptionKey.Algorithm = encryptionAlgorithm;
        contentEncryptionKey.Use = TokenKeyUses.Encryption;
        contentEncryptionKey.KeyOperations = new string[]
        {
            TokenKeyOperations.Encrypt
        };
        TokenTextResult encrypted = EncryptContent(
            content,
            additionalAuthenticatedData.Bytes,
            contentEncryptionKey,
            encryptionAlgorithm,
            finalHeader.Bytes,
            managed.EncryptedKey,
            policy);
        contentEncryptionKey.SymmetricKey = null;
        ClearManaged(managed);
        CryptographicOperations.ZeroMemory(content);
        CryptographicOperations.ZeroMemory(additionalAuthenticatedData.Bytes);
        return encrypted;
    }

    /// <summary>Authenticates and decrypts a compact JWE.</summary>
    /// <param name="text">The compact JWE serialization.</param>
    /// <param name="key">The recipient or key-encryption key.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>A result containing authenticated plaintext or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JweCompactResult Decrypt(
        string text,
        TokenKey key,
        TokenPolicy policy)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        JweCompactResult parsed = ReadCore(text, policy, false);
        if (!parsed.Success)
        {
            return parsed;
        }

        TokenResult algorithms = ValidateEncryptionAlgorithms(
            ((parsed.Token ?? throw new InvalidOperationException("A required value is null.")).ProtectedHeader ?? throw new InvalidOperationException("A required value is null.")).Get(JoseHeaderNames.Algorithm),
            (parsed.Token.ProtectedHeader ?? throw new InvalidOperationException("A required value is null.")).Get(
                JoseHeaderNames.EncryptionAlgorithm),
            policy);
        if (!algorithms.Success)
        {
            return Failure(algorithms.Error);
        }

        JweKeyManagementResult managed = ManageDecryptionKey(
            parsed.Token.ProtectedHeader,
            parsed.Token.EncryptedKey,
            parsed.Token.Algorithm,
            parsed.Token.EncryptionAlgorithm,
            key,
            policy);
        if (!managed.Success)
        {
            return Failure(managed.Error);
        }

        TokenBytesResult additionalAuthenticatedData = Utf8Codec.Encode(
            (parsed.Token.ProtectedSegment ?? throw new InvalidOperationException("A required value is null.")));
        if (!additionalAuthenticatedData.Success)
        {
            ClearManaged(managed);
            return Failure(additionalAuthenticatedData.Error);
        }

        TokenKey contentEncryptionKey = new TokenKey();
        contentEncryptionKey.KeyType = TokenKeyTypes.Symmetric;
        contentEncryptionKey.SymmetricKey = managed.ContentEncryptionKey;
        contentEncryptionKey.Algorithm = parsed.Token.EncryptionAlgorithm;
        contentEncryptionKey.Use = TokenKeyUses.Encryption;
        contentEncryptionKey.KeyOperations = new string[]
        {
            TokenKeyOperations.Decrypt
        };
        TokenBytesResult plaintext = IsGcmContentEncryption(
            parsed.Token.EncryptionAlgorithm)
            ? AesGcmContentEncryption.Decrypt(
                (parsed.Token.ParsedCiphertext ?? throw new InvalidOperationException("A required value is null.")),
                (parsed.Token.InitializationVector ?? throw new InvalidOperationException("A required value is null.")),
                (parsed.Token.AuthenticationTag ?? throw new InvalidOperationException("A required value is null.")),
                (additionalAuthenticatedData.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                contentEncryptionKey,
                (parsed.Token.EncryptionAlgorithm ?? throw new InvalidOperationException("A required value is null.")))
            : AesCbcHmacContentEncryption.Decrypt(
                (parsed.Token.ParsedCiphertext ?? throw new InvalidOperationException("A required value is null.")),
                (parsed.Token.InitializationVector ?? throw new InvalidOperationException("A required value is null.")),
                (parsed.Token.AuthenticationTag ?? throw new InvalidOperationException("A required value is null.")),
                (additionalAuthenticatedData.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                contentEncryptionKey,
                (parsed.Token.EncryptionAlgorithm ?? throw new InvalidOperationException("A required value is null.")));
        contentEncryptionKey.SymmetricKey = null;
        ClearManaged(managed);
        CryptographicOperations.ZeroMemory(additionalAuthenticatedData.Bytes);
        if (!plaintext.Success)
        {
            return Failure(plaintext.Error);
        }

        byte[]? authenticatedPlaintext = plaintext.Bytes;
        if (parsed.Token.CompressionAlgorithm != null)
        {
            TokenBytesResult decompressed = JoseCompression.Decompress(
                (authenticatedPlaintext ?? throw new InvalidOperationException("A required value is null.")),
                parsed.Token.CompressionAlgorithm,
                policy);
            CryptographicOperations.ZeroMemory(authenticatedPlaintext);
            if (!decompressed.Success)
            {
                return Failure(decompressed.Error);
            }

            authenticatedPlaintext = decompressed.Bytes;
        }

        if ((authenticatedPlaintext ?? throw new InvalidOperationException("A required value is null.")).Length > policy.MaximumTokenSizeInBytes)
        {
            CryptographicOperations.ZeroMemory(authenticatedPlaintext);
            return Failure(
                "token_too_large",
                "The JWE plaintext exceeds the configured token size limit.",
                policy.MaximumTokenSizeInBytes);
        }

        parsed.Token.Plaintext = authenticatedPlaintext;
        parsed.Token.PlaintextAvailable = true;
        parsed.Token.AuthenticationTagVerified = true;
        return parsed;
    }

    /// <summary>Parses compact JWE text using conservative default limits.</summary>
    /// <param name="text">The compact JWE serialization.</param>
    /// <returns>A result containing unauthenticated header metadata and encrypted components.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    internal static JweCompactResult Parse(string text)
    {
        return Parse(text, new TokenPolicy());
    }

    /// <summary>Parses compact JWE text while withholding ciphertext bytes.</summary>
    /// <param name="text">The compact JWE serialization.</param>
    /// <param name="policy">The header and size policy.</param>
    /// <returns>A result containing unauthenticated header metadata and encrypted components.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JweCompactResult Parse(string text, TokenPolicy policy)
    {
        return ReadCore(text, policy, false);
    }

    /// <summary>Reads compact JWE ciphertext when diagnostic access is explicitly enabled.</summary>
    /// <param name="text">The compact JWE serialization.</param>
    /// <param name="policy">The policy that must permit unauthenticated decode.</param>
    /// <returns>A result containing ciphertext and unauthenticated JWE metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JweCompactResult ReadDiagnostic(
        string text,
        TokenPolicy policy)
    {
        return ReadCore(text, policy, true);
    }

    static TokenTextResult EncryptContent(
        byte[]? plaintext,
        byte[]? additionalAuthenticatedData,
        TokenKey contentEncryptionKey,
        string? encryptionAlgorithm,
        byte[] protectedHeader,
        byte[]? encryptedKey,
        TokenPolicy policy)
    {
        byte[]? initializationVector;
        byte[]? ciphertext;
        byte[]? authenticationTag;
        if (IsGcmContentEncryption(encryptionAlgorithm))
        {
            AesGcmContentEncryptionResult encrypted =
                AesGcmContentEncryption.Encrypt(
                    (plaintext ?? throw new InvalidOperationException("A required value is null.")),
                    (additionalAuthenticatedData ?? throw new InvalidOperationException("A required value is null.")),
                    contentEncryptionKey,
                    (encryptionAlgorithm ?? throw new InvalidOperationException("A required value is null.")));
            if (!encrypted.Success)
            {
                return TextFailure(encrypted.Error);
            }

            initializationVector = encrypted.InitializationVector;
            ciphertext = encrypted.Ciphertext;
            authenticationTag = encrypted.AuthenticationTag;
        }
        else
        {
            AesCbcHmacContentEncryptionResult encrypted =
                AesCbcHmacContentEncryption.Encrypt(
                    (plaintext ?? throw new InvalidOperationException("A required value is null.")),
                    (additionalAuthenticatedData ?? throw new InvalidOperationException("A required value is null.")),
                    contentEncryptionKey,
                    (encryptionAlgorithm ?? throw new InvalidOperationException("A required value is null.")));
            if (!encrypted.Success)
            {
                return TextFailure(encrypted.Error);
            }

            initializationVector = encrypted.InitializationVector;
            ciphertext = encrypted.Ciphertext;
            authenticationTag = encrypted.AuthenticationTag;
        }

        TokenTextResult compact = JoseCompact.WriteJwe(
            protectedHeader,
            (encryptedKey ?? throw new InvalidOperationException("A required value is null.")),
            (initializationVector ?? throw new InvalidOperationException("A required value is null.")),
            (ciphertext ?? throw new InvalidOperationException("A required value is null.")),
            (authenticationTag ?? throw new InvalidOperationException("A required value is null.")));
        CryptographicOperations.ZeroMemory(ciphertext);
        if (!compact.Success)
        {
            return compact;
        }

        TokenBytesResult compactBytes = Utf8Codec.Encode((compact.Text ?? throw new InvalidOperationException("A required value is null.")));
        if (!compactBytes.Success)
        {
            return TextFailure(compactBytes.Error);
        }

        if ((compactBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length > policy.MaximumTokenSizeInBytes)
        {
            return TextFailure(
                "token_too_large",
                "The compact JWE exceeds the configured token size limit.");
        }

        return compact;
    }

    internal static JweKeyManagementResult ManageEncryptionKey(
        JoseHeader header,
        TokenKey key,
        string? algorithm,
        string? encryptionAlgorithm,
        TokenPolicy policy)
    {
        if (string.Equals(algorithm, JoseAlgorithms.Direct, StringComparison.Ordinal))
        {
            DirectKeyManagementResult direct = DirectKeyManagement.Encrypt(
                key,
                (encryptionAlgorithm ?? throw new InvalidOperationException("A required value is null.")));
            if (!direct.Success)
            {
                return KeyFailure(direct.Error);
            }

            return KeySuccess(direct.ContentEncryptionKey, direct.EncryptedKey);
        }

        if (IsRsaKeyManagement(algorithm))
        {
            RsaKeyManagementResult rsa = RsaKeyManagement.Encrypt(
                key,
                (algorithm ?? throw new InvalidOperationException("A required value is null.")),
                (encryptionAlgorithm ?? throw new InvalidOperationException("A required value is null.")),
                policy.AllowLegacyRsaPkcs1Encryption);
            if (!rsa.Success)
            {
                return KeyFailure(rsa.Error);
            }

            return KeySuccess(rsa.ContentEncryptionKey, rsa.EncryptedKey);
        }

        int contentEncryptionKeySize = GetContentEncryptionKeySize(
            encryptionAlgorithm);
        if (IsAesKeyWrap(algorithm))
        {
            byte[] contentEncryptionKey = RandomBytes.Generate(
                contentEncryptionKeySize);
            TokenBytesResult wrapped = AesKeyWrap.Wrap(
                contentEncryptionKey,
                key,
                (algorithm ?? throw new InvalidOperationException("A required value is null.")));
            if (!wrapped.Success)
            {
                CryptographicOperations.ZeroMemory(contentEncryptionKey);
                return KeyFailure(wrapped.Error);
            }

            return KeySuccess(contentEncryptionKey, wrapped.Bytes);
        }

        if (IsAesGcmKeyWrap((algorithm ?? throw new InvalidOperationException("A required value is null."))))
        {
            TokenResult absent = RequireAbsent(
                header,
                new string[]
                {
                    JoseHeaderNames.InitializationVector,
                    JoseHeaderNames.AuthenticationTag
                });
            if (!absent.Success)
            {
                return KeyFailure(absent.Error);
            }

            byte[] contentEncryptionKey = RandomBytes.Generate(
                contentEncryptionKeySize);
            AesGcmKeyWrapResult wrapped = AesGcmKeyWrap.Wrap(
                contentEncryptionKey,
                key,
                algorithm);
            if (!wrapped.Success)
            {
                CryptographicOperations.ZeroMemory(contentEncryptionKey);
                return KeyFailure(wrapped.Error);
            }

            header.Set(
                JoseHeaderNames.InitializationVector,
                StringValue(Base64Url.Encode((wrapped.InitializationVector ?? throw new InvalidOperationException("A required value is null."))).Text));
            header.Set(
                JoseHeaderNames.AuthenticationTag,
                StringValue(Base64Url.Encode((wrapped.AuthenticationTag ?? throw new InvalidOperationException("A required value is null."))).Text));
            return KeySuccess(contentEncryptionKey, wrapped.EncryptedKey);
        }

        if (IsPbes2(algorithm))
        {
            TokenResult keyResult = ValidatePasswordKey(key, algorithm, true);
            if (!keyResult.Success)
            {
                return KeyFailure(keyResult.Error);
            }

            TokenResult absent = RequireAbsent(
                header,
                new string[]
                {
                    JoseHeaderNames.Pbes2SaltInput,
                    JoseHeaderNames.Pbes2Count
                });
            if (!absent.Success)
            {
                return KeyFailure(absent.Error);
            }

            byte[] contentEncryptionKey = RandomBytes.Generate(
                contentEncryptionKeySize);
            Pbes2KeyWrapResult wrapped = Pbes2AlgorithmsOperations.Wrap(
                contentEncryptionKey,
                (key.SymmetricKey ?? throw new InvalidOperationException("A required value is null.")),
                algorithm);
            if (!wrapped.Success)
            {
                CryptographicOperations.ZeroMemory(contentEncryptionKey);
                return KeyFailure(wrapped.Error);
            }

            header.Set(
                JoseHeaderNames.Pbes2SaltInput,
                StringValue(Base64Url.Encode((wrapped.SaltInput ?? throw new InvalidOperationException("A required value is null."))).Text));
            header.Set(
                JoseHeaderNames.Pbes2Count,
                NumberValue(wrapped.IterationCount));
            return KeySuccess(contentEncryptionKey, wrapped.EncryptedKey);
        }

        TokenResult ephemeralAbsent = RequireAbsent(
            header,
            new string[] { JoseHeaderNames.EphemeralPublicKey });
        if (!ephemeralAbsent.Success)
        {
            return KeyFailure(ephemeralAbsent.Error);
        }

        TokenBytesResult partyUInfo = ReadHeaderBytes(
            header,
            JoseHeaderNames.AgreementPartyUInfo);
        if (!partyUInfo.Success)
        {
            return KeyFailure(partyUInfo.Error);
        }

        TokenBytesResult partyVInfo = ReadHeaderBytes(
            header,
            JoseHeaderNames.AgreementPartyVInfo);
        if (!partyVInfo.Success)
        {
            return KeyFailure(partyVInfo.Error);
        }

        EcdhKeyManagementResult ecdh = EcdhAlgorithms.Encrypt(
            key,
            algorithm,
            (encryptionAlgorithm ?? throw new InvalidOperationException("A required value is null.")),
            (partyUInfo.Bytes ?? throw new InvalidOperationException("A required value is null.")),
            (partyVInfo.Bytes ?? throw new InvalidOperationException("A required value is null.")));
        if (!ecdh.Success)
        {
            return KeyFailure(ecdh.Error);
        }

        TokenTextResult publicJwk = JsonWebKey.WritePublic(
            (ecdh.EphemeralPublicKey ?? throw new InvalidOperationException("A required value is null.")));
        ecdh.EphemeralPublicKey.ReleaseOwnedMaterial();
        if (!publicJwk.Success)
        {
            CryptographicOperations.ZeroMemory(ecdh.ContentEncryptionKey);
            return KeyFailure(publicJwk.Error);
        }

        JsonObjectResult publicObject = JsonObject.Read((publicJwk.Text ?? throw new InvalidOperationException("A required value is null.")));
        if (!publicObject.Success)
        {
            CryptographicOperations.ZeroMemory(ecdh.ContentEncryptionKey);
            return KeyFailure(publicObject.Error);
        }

        header.Set(
            JoseHeaderNames.EphemeralPublicKey,
            ObjectValue(publicObject.Object));
        return KeySuccess(ecdh.ContentEncryptionKey, ecdh.EncryptedKey);
    }

    internal static JweKeyManagementResult WrapEncryptionKey(
        JoseHeader header,
        byte[] contentEncryptionKey,
        TokenKey key,
        string? algorithm,
        string? encryptionAlgorithm,
        TokenPolicy policy)
    {
        if (IsRsaKeyManagement(algorithm))
        {
            RsaKeyManagementResult rsa = RsaKeyManagement.Wrap(
                contentEncryptionKey,
                key,
                (algorithm ?? throw new InvalidOperationException("A required value is null.")),
                (encryptionAlgorithm ?? throw new InvalidOperationException("A required value is null.")),
                policy.AllowLegacyRsaPkcs1Encryption);
            if (!rsa.Success)
            {
                return KeyFailure(rsa.Error);
            }

            return KeySuccess(rsa.ContentEncryptionKey, rsa.EncryptedKey);
        }

        if (IsAesKeyWrap(algorithm))
        {
            TokenBytesResult wrapped = AesKeyWrap.Wrap(
                contentEncryptionKey,
                key,
                (algorithm ?? throw new InvalidOperationException("A required value is null.")));
            if (!wrapped.Success)
            {
                return KeyFailure(wrapped.Error);
            }

            return KeySuccess(contentEncryptionKey.AsSpan().ToArray(), wrapped.Bytes);
        }

        if (IsAesGcmKeyWrap((algorithm ?? throw new InvalidOperationException("A required value is null."))))
        {
            TokenResult absent = RequireAbsent(
                header,
                new string[]
                {
                    JoseHeaderNames.InitializationVector,
                    JoseHeaderNames.AuthenticationTag
                });
            if (!absent.Success)
            {
                return KeyFailure(absent.Error);
            }

            AesGcmKeyWrapResult wrapped = AesGcmKeyWrap.Wrap(
                contentEncryptionKey,
                key,
                algorithm);
            if (!wrapped.Success)
            {
                return KeyFailure(wrapped.Error);
            }

            header.Set(
                JoseHeaderNames.InitializationVector,
                StringValue(Base64Url.Encode((wrapped.InitializationVector ?? throw new InvalidOperationException("A required value is null."))).Text));
            header.Set(
                JoseHeaderNames.AuthenticationTag,
                StringValue(Base64Url.Encode((wrapped.AuthenticationTag ?? throw new InvalidOperationException("A required value is null."))).Text));
            return KeySuccess(
                contentEncryptionKey.AsSpan().ToArray(),
                wrapped.EncryptedKey);
        }

        if (IsPbes2(algorithm))
        {
            TokenResult keyResult = ValidatePasswordKey(key, algorithm, true);
            if (!keyResult.Success)
            {
                return KeyFailure(keyResult.Error);
            }

            TokenResult absent = RequireAbsent(
                header,
                new string[]
                {
                    JoseHeaderNames.Pbes2SaltInput,
                    JoseHeaderNames.Pbes2Count
                });
            if (!absent.Success)
            {
                return KeyFailure(absent.Error);
            }

            Pbes2KeyWrapResult wrapped = Pbes2AlgorithmsOperations.Wrap(
                contentEncryptionKey,
                (key.SymmetricKey ?? throw new InvalidOperationException("A required value is null.")),
                algorithm);
            if (!wrapped.Success)
            {
                return KeyFailure(wrapped.Error);
            }

            header.Set(
                JoseHeaderNames.Pbes2SaltInput,
                StringValue(Base64Url.Encode((wrapped.SaltInput ?? throw new InvalidOperationException("A required value is null."))).Text));
            header.Set(
                JoseHeaderNames.Pbes2Count,
                NumberValue(wrapped.IterationCount));
            return KeySuccess(
                contentEncryptionKey.AsSpan().ToArray(),
                wrapped.EncryptedKey);
        }

        if (string.Equals(algorithm, JoseAlgorithms.Direct, StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.EcdhEs,
                StringComparison.Ordinal))
        {
            return KeyFailure(new TokenError(
                "direct_multi_recipient_not_supported",
                "Direct key-management algorithms cannot wrap a shared multi-recipient CEK.",
                -1));
        }

        TokenResult ephemeralAbsent = RequireAbsent(
            header,
            new string[] { JoseHeaderNames.EphemeralPublicKey });
        if (!ephemeralAbsent.Success)
        {
            return KeyFailure(ephemeralAbsent.Error);
        }

        TokenBytesResult partyUInfo = ReadHeaderBytes(
            header,
            JoseHeaderNames.AgreementPartyUInfo);
        if (!partyUInfo.Success)
        {
            return KeyFailure(partyUInfo.Error);
        }

        TokenBytesResult partyVInfo = ReadHeaderBytes(
            header,
            JoseHeaderNames.AgreementPartyVInfo);
        if (!partyVInfo.Success)
        {
            return KeyFailure(partyVInfo.Error);
        }

        EcdhKeyManagementResult ecdh = EcdhAlgorithms.Wrap(
            contentEncryptionKey,
            key,
            algorithm,
            (encryptionAlgorithm ?? throw new InvalidOperationException("A required value is null.")),
            (partyUInfo.Bytes ?? throw new InvalidOperationException("A required value is null.")),
            (partyVInfo.Bytes ?? throw new InvalidOperationException("A required value is null.")));
        if (!ecdh.Success)
        {
            return KeyFailure(ecdh.Error);
        }

        TokenTextResult publicJwk = JsonWebKey.WritePublic(
            (ecdh.EphemeralPublicKey ?? throw new InvalidOperationException("A required value is null.")));
        ecdh.EphemeralPublicKey.ReleaseOwnedMaterial();
        if (!publicJwk.Success)
        {
            CryptographicOperations.ZeroMemory(ecdh.ContentEncryptionKey);
            return KeyFailure(publicJwk.Error);
        }

        JsonObjectResult publicObject = JsonObject.Read((publicJwk.Text ?? throw new InvalidOperationException("A required value is null.")));
        if (!publicObject.Success)
        {
            CryptographicOperations.ZeroMemory(ecdh.ContentEncryptionKey);
            return KeyFailure(publicObject.Error);
        }

        header.Set(
            JoseHeaderNames.EphemeralPublicKey,
            ObjectValue(publicObject.Object));
        return KeySuccess(ecdh.ContentEncryptionKey, ecdh.EncryptedKey);
    }

    internal static JweKeyManagementResult ManageDecryptionKey(
        JoseHeader header,
        byte[]? encryptedKey,
        string? algorithm,
        string? encryptionAlgorithm,
        TokenKey key,
        TokenPolicy policy)
    {
        if (string.Equals(algorithm, JoseAlgorithms.Direct, StringComparison.Ordinal))
        {
            DirectKeyManagementResult direct = DirectKeyManagement.Decrypt(
                (encryptedKey ?? throw new InvalidOperationException("A required value is null.")),
                key,
                (encryptionAlgorithm ?? throw new InvalidOperationException("A required value is null.")));
            if (!direct.Success)
            {
                return KeyFailure(direct.Error);
            }

            return KeySuccess(direct.ContentEncryptionKey, direct.EncryptedKey);
        }

        if (IsRsaKeyManagement(algorithm))
        {
            RsaKeyManagementResult rsa = RsaKeyManagement.Decrypt(
                (encryptedKey ?? throw new InvalidOperationException("A required value is null.")),
                key,
                (algorithm ?? throw new InvalidOperationException("A required value is null.")),
                (encryptionAlgorithm ?? throw new InvalidOperationException("A required value is null.")),
                policy.AllowLegacyRsaPkcs1Encryption);
            if (!rsa.Success)
            {
                return KeyFailure(rsa.Error);
            }

            return KeySuccess(rsa.ContentEncryptionKey, encryptedKey);
        }

        if (IsAesKeyWrap(algorithm))
        {
            TokenBytesResult unwrapped = AesKeyWrap.Unwrap(
                (encryptedKey ?? throw new InvalidOperationException("A required value is null.")),
                key,
                (algorithm ?? throw new InvalidOperationException("A required value is null.")));
            if (!unwrapped.Success)
            {
                return KeyFailure(unwrapped.Error);
            }

            return KeySuccess(unwrapped.Bytes, encryptedKey);
        }

        if (IsAesGcmKeyWrap((algorithm ?? throw new InvalidOperationException("A required value is null."))))
        {
            TokenBytesResult initializationVector = ReadRequiredHeaderBytes(
                header,
                JoseHeaderNames.InitializationVector,
                "missing_key_initialization_vector",
                "invalid_key_initialization_vector");
            if (!initializationVector.Success)
            {
                return KeyFailure(initializationVector.Error);
            }

            TokenBytesResult authenticationTag = ReadRequiredHeaderBytes(
                header,
                JoseHeaderNames.AuthenticationTag,
                "missing_key_authentication_tag",
                "invalid_key_authentication_tag");
            if (!authenticationTag.Success)
            {
                return KeyFailure(authenticationTag.Error);
            }

            TokenBytesResult unwrapped = AesGcmKeyWrap.Unwrap(
                (encryptedKey ?? throw new InvalidOperationException("A required value is null.")),
                (initializationVector.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                (authenticationTag.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                key,
                algorithm);
            if (!unwrapped.Success)
            {
                return KeyFailure(unwrapped.Error);
            }

            return KeySuccess(unwrapped.Bytes, encryptedKey);
        }

        if (IsPbes2(algorithm))
        {
            TokenResult keyResult = ValidatePasswordKey(key, algorithm, false);
            if (!keyResult.Success)
            {
                return KeyFailure(keyResult.Error);
            }

            TokenBytesResult saltInput = ReadRequiredHeaderBytes(
                header,
                JoseHeaderNames.Pbes2SaltInput,
                "missing_pbes2_salt",
                "invalid_pbes2_salt");
            if (!saltInput.Success)
            {
                return KeyFailure(saltInput.Error);
            }

            JsonValue? countValue = header.Get(
                JoseHeaderNames.Pbes2Count);
            if (countValue == null)
            {
                return KeyFailure(new TokenError(
                    "missing_pbes2_count",
                    "PBES2 decryption requires the p2c header parameter.",
                    -1));
            }

            JweIntegerResult count = ReadPositiveInteger(countValue.NumberText);
            if (!count.Success)
            {
                return KeyFailure(count.Error);
            }

            TokenBytesResult unwrapped = Pbes2AlgorithmsOperations.Unwrap(
                (encryptedKey ?? throw new InvalidOperationException("A required value is null.")),
                (key.SymmetricKey ?? throw new InvalidOperationException("A required value is null.")),
                algorithm,
                (saltInput.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                count.Value);
            if (!unwrapped.Success)
            {
                return KeyFailure(unwrapped.Error);
            }

            return KeySuccess(unwrapped.Bytes, encryptedKey);
        }

        JsonValue? ephemeralValue = header.Get(
            JoseHeaderNames.EphemeralPublicKey);
        if (ephemeralValue == null)
        {
            return KeyFailure(new TokenError(
                "missing_ephemeral_key",
                "ECDH-ES decryption requires the epk header parameter.",
                -1));
        }

        TokenTextResult ephemeralText = JoseHeader.Create(
            (ephemeralValue.ObjectValue ?? throw new InvalidOperationException("A required value is null."))).Write();
        if (!ephemeralText.Success)
        {
            return KeyFailure(ephemeralText.Error);
        }

        JwkResult ephemeral = JsonWebKey.Read((ephemeralText.Text ?? throw new InvalidOperationException("A required value is null.")));
        if (!ephemeral.Success)
        {
            return KeyFailure(ephemeral.Error);
        }

        TokenBytesResult partyUInfo = ReadHeaderBytes(
            header,
            JoseHeaderNames.AgreementPartyUInfo);
        if (!partyUInfo.Success)
        {
            (ephemeral.Key ?? throw new InvalidOperationException("A required value is null.")).ReleaseOwnedMaterial();
            return KeyFailure(partyUInfo.Error);
        }

        TokenBytesResult partyVInfo = ReadHeaderBytes(
            header,
            JoseHeaderNames.AgreementPartyVInfo);
        if (!partyVInfo.Success)
        {
            (ephemeral.Key ?? throw new InvalidOperationException("A required value is null.")).ReleaseOwnedMaterial();
            return KeyFailure(partyVInfo.Error);
        }

        EcdhKeyManagementResult ecdh = string.Equals(
            algorithm,
            JoseAlgorithms.EcdhEs,
            StringComparison.Ordinal)
            ? EcdhAlgorithms.Decrypt(
                key,
                (ephemeral.Key ?? throw new InvalidOperationException("A required value is null.")),
                algorithm,
                (encryptionAlgorithm ?? throw new InvalidOperationException("A required value is null.")),
                (partyUInfo.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                (partyVInfo.Bytes ?? throw new InvalidOperationException("A required value is null.")))
            : EcdhAlgorithms.Decrypt(
                (encryptedKey ?? throw new InvalidOperationException("A required value is null.")),
                key,
                (ephemeral.Key ?? throw new InvalidOperationException("A required value is null.")),
                algorithm,
                (encryptionAlgorithm ?? throw new InvalidOperationException("A required value is null.")),
                (partyUInfo.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                (partyVInfo.Bytes ?? throw new InvalidOperationException("A required value is null.")));
        ephemeral.Key.ReleaseOwnedMaterial();
        if (!ecdh.Success)
        {
            return KeyFailure(ecdh.Error);
        }

        return KeySuccess(ecdh.ContentEncryptionKey, encryptedKey);
    }

    internal static TokenResult ValidateEncryptionAlgorithms(
        JsonValue? algorithmValue,
        JsonValue? encryptionValue,
        TokenPolicy policy)
    {
        if (algorithmValue == null)
        {
            return ResultFailure(
                "missing_algorithm",
                "A compact JWE protected header requires alg.",
                -1);
        }

        string? algorithm = algorithmValue.StringValue;
        if (string.IsNullOrEmpty(algorithm))
        {
            return ResultFailure(
                "invalid_algorithm",
                "The JWE key-management algorithm must not be empty.",
                -1);
        }

        AlgorithmMetadata algorithmMetadata = AlgorithmMetadataRegistry.GetJose(
            algorithm);
        if (algorithmMetadata.Known && !JoseAlgorithmsOperations.IsKeyManagement(algorithm))
        {
            return ResultFailure(
                "invalid_jwe_algorithm",
                "The registered JOSE algorithm is not a JWE key-management algorithm.",
                -1);
        }

        TokenResult algorithmAvailability =
            CryptoAlgorithmAvailability.CheckJoseAlgorithm(algorithm);
        if (!algorithmAvailability.Success)
        {
            return algorithmAvailability;
        }

        if (encryptionValue == null)
        {
            return ResultFailure(
                "missing_encryption_algorithm",
                "A compact JWE protected header requires enc.",
                -1);
        }

        string? encryptionAlgorithm = encryptionValue.StringValue;
        if ((encryptionAlgorithm ?? throw new InvalidOperationException("A required value is null.")).Length == 0)
        {
            return ResultFailure(
                "invalid_encryption_algorithm",
                "The JWE content-encryption algorithm must not be empty.",
                -1);
        }

        AlgorithmMetadata encryptionMetadata = AlgorithmMetadataRegistry.GetJose(
            encryptionAlgorithm);
        if (encryptionMetadata.Known
            && !JoseAlgorithmsOperations.IsContentEncryption(encryptionAlgorithm))
        {
            return ResultFailure(
                "invalid_content_encryption_algorithm",
                "The registered JOSE algorithm is not a JWE content-encryption algorithm.",
                -1);
        }

        TokenResult encryptionAvailability =
            CryptoAlgorithmAvailability.CheckJoseAlgorithm(encryptionAlgorithm);
        if (!encryptionAvailability.Success)
        {
            return encryptionAvailability;
        }

        if (!RegistryLookup.Contains(policy.AllowedAlgorithms, algorithm, false)
            || !RegistryLookup.Contains(
                policy.AllowedAlgorithms,
                encryptionAlgorithm,
                false))
        {
            return ResultFailure(
                "algorithm_not_allowed",
                "The JWE algorithms are not allowed by policy.",
                -1);
        }

        return ResultSuccess();
    }

    static TokenResult ValidatePasswordKey(
        TokenKey key,
        string algorithm,
        bool wrapping)
    {
        TokenResult valid = SymmetricKeyImporterOperations.Validate(key);
        if (!valid.Success)
        {
            return valid;
        }

        if (key.Algorithm != null
            && !string.Equals(key.Algorithm, algorithm, StringComparison.Ordinal))
        {
            return ResultFailure(
                "conflicting_key_algorithm",
                "The password key algorithm does not match PBES2.",
                -1);
        }

        if (key.Use != null
            && !string.Equals(key.Use, TokenKeyUses.Encryption, StringComparison.Ordinal))
        {
            return ResultFailure(
                "key_use_algorithm_mismatch",
                "The key use does not permit PBES2 key wrapping.",
                -1);
        }

        string operation = wrapping
            ? TokenKeyOperations.WrapKey
            : TokenKeyOperations.UnwrapKey;
        if (key.KeyOperations != null
            && key.KeyOperations.Length > 0
            && !RegistryLookup.Contains(
                key.KeyOperations,
                operation,
                false))
        {
            return ResultFailure(
                "key_operation_not_permitted",
                "The requested PBES2 key operation is not permitted.",
                -1);
        }

        if (key.HasCoseAlgorithm)
        {
            return ResultFailure(
                "conflicting_key_algorithm",
                "COSE does not define an algorithm equivalent to JWE PBES2.",
                -1);
        }

        int coseOperation = wrapping
            ? CoseKeyLabels.OperationWrapKey
            : CoseKeyLabels.OperationUnwrapKey;
        if (key.CoseKeyOperations != null
            && key.CoseKeyOperations.Length > 0
            && !RegistryLookup.Contains(
                key.CoseKeyOperations,
                coseOperation))
        {
            return ResultFailure(
                "key_operation_not_permitted",
                "The requested PBES2 key operation is not permitted.",
                -1);
        }

        return ResultSuccess();
    }

    static TokenResult RequireAbsent(JoseHeader header, string[] names)
    {
        int index = 0;
        while (index < names.Length)
        {
            if (header.Get(names[index]) != null)
            {
                return ResultFailure(
                    "conflicting_header_parameter",
                    "A generated JWE header parameter was already supplied.",
                    index);
            }

            index += 1;
        }

        return ResultSuccess();
    }

    static TokenBytesResult ReadHeaderBytes(JoseHeader header, string name)
    {
        JsonValue? value = header.Get(name);
        if (value == null)
        {
            TokenBytesResult empty = new TokenBytesResult();
            empty.Success = true;
            empty.Bytes = Array.Empty<byte>();
            return empty;
        }

        TokenBytesResult decoded = Base64Url.Decode((value.StringValue ?? throw new InvalidOperationException("A required value is null.")));
        if (!decoded.Success)
        {
            TokenBytesResult failure = new TokenBytesResult();
            failure.Error = new TokenError(
                "invalid_party_info",
                "A JWE party information header is not canonical base64url.",
                -1);
            return failure;
        }

        return decoded;
    }

    static TokenBytesResult ReadRequiredHeaderBytes(
        JoseHeader header,
        string name,
        string missingCode,
        string invalidCode)
    {
        JsonValue? value = header.Get(name);
        if (value == null)
        {
            TokenBytesResult missing = new TokenBytesResult();
            missing.Error = new TokenError(
                missingCode,
                "A required JWE key-management header parameter is missing.",
                -1);
            return missing;
        }

        TokenBytesResult decoded = Base64Url.Decode((value.StringValue ?? throw new InvalidOperationException("A required value is null.")));
        if (!decoded.Success || (decoded.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length == 0)
        {
            TokenBytesResult invalid = new TokenBytesResult();
            invalid.Error = new TokenError(
                invalidCode,
                "A required JWE key-management header parameter is invalid.",
                -1);
            return invalid;
        }

        return decoded;
    }

    static JweIntegerResult ReadPositiveInteger(string? text)
    {
        JweIntegerResult result = new JweIntegerResult();
        if (text == null || text.Length == 0)
        {
            result.Error = new TokenError(
                "invalid_pbes2_count",
                "The PBES2 p2c header must be a positive integer.",
                -1);
            return result;
        }

        long value = 0;
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character < '0' || character > '9')
            {
                result.Error = new TokenError(
                    "invalid_pbes2_count",
                    "The PBES2 p2c header must be a positive integer.",
                    -1);
                return result;
            }

            value = value * 10 + character - '0';
            if (value > int.MaxValue)
            {
                result.Error = new TokenError(
                    "invalid_pbes2_count",
                    "The PBES2 p2c header is outside the integer range.",
                    -1);
                return result;
            }

            index += 1;
        }

        if (value == 0)
        {
            result.Error = new TokenError(
                "invalid_pbes2_count",
                "The PBES2 p2c header must be a positive integer.",
                -1);
            return result;
        }

        result.Success = true;
        result.Value = (int)value;
        return result;
    }

    static bool IsRsaKeyManagement(string? algorithm)
    {
        return string.Equals(algorithm, JoseAlgorithms.RsaPkcs1, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaOaep, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaOaepSha256, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaOaepSha384, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaOaepSha512, StringComparison.Ordinal);
    }

    static bool IsAesKeyWrap(string? algorithm)
    {
        return string.Equals(algorithm, JoseAlgorithms.Aes128KeyWrap, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes192KeyWrap, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes256KeyWrap, StringComparison.Ordinal);
    }

    static bool IsAesGcmKeyWrap(string algorithm)
    {
        return string.Equals(algorithm, JoseAlgorithms.Aes128GcmKeyWrap, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes192GcmKeyWrap, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes256GcmKeyWrap, StringComparison.Ordinal);
    }

    static bool IsPbes2(string algorithm)
    {
        return string.Equals(
                algorithm,
                JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap,
                StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.Pbes2HmacSha384Aes192KeyWrap,
                StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.Pbes2HmacSha512Aes256KeyWrap,
                StringComparison.Ordinal);
    }

    internal static bool IsGcmContentEncryption(string? algorithm)
    {
        return string.Equals(algorithm, JoseAlgorithms.Aes128Gcm, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes192Gcm, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes256Gcm, StringComparison.Ordinal);
    }

    internal static int GetContentEncryptionKeySize(string? algorithm)
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

        return 64;
    }

    static JsonValue StringValue(string? value)
    {
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.String;
        result.StringValue = value;
        return result;
    }

    static JsonValue NumberValue(int value)
    {
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.Number;
        result.NumberText = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return result;
    }

    static JsonValue ObjectValue(JsonObject? value)
    {
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.Object;
        result.ObjectValue = value;
        return result;
    }

    static JweKeyManagementResult KeySuccess(
        byte[]? contentEncryptionKey,
        byte[]? encryptedKey)
    {
        JweKeyManagementResult result = new JweKeyManagementResult();
        result.Success = true;
        result.ContentEncryptionKey = contentEncryptionKey;
        result.EncryptedKey = encryptedKey;
        return result;
    }

    static JweKeyManagementResult KeyFailure(TokenError? error)
    {
        JweKeyManagementResult result = new JweKeyManagementResult();
        result.Error = error;
        return result;
    }

    internal static void ClearManaged(JweKeyManagementResult result)
    {
        if (result.ContentEncryptionKey != null)
        {
            CryptographicOperations.ZeroMemory(result.ContentEncryptionKey);
        }
    }

    static TokenResult ResultSuccess()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenTextResult TextFailure(string code, string message)
    {
        return TextFailure(new TokenError(code, message, -1));
    }

    static TokenTextResult TextFailure(TokenError? error)
    {
        TokenTextResult result = new TokenTextResult();
        result.Error = error;
        return result;
    }

    static JweCompactResult ReadCore(
        string text,
        TokenPolicy policy,
        bool diagnostic)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult policyResult = JsonWebSignature.ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return Failure(policyResult.Error);
        }

        if (diagnostic && !policy.AllowUnauthenticatedDecode)
        {
            return Failure(
                "unauthenticated_decode_not_allowed",
                "Diagnostic JWE ciphertext decoding requires explicit policy opt-in.",
                -1);
        }

        TokenBytesResult compactBytes = Utf8Codec.Encode(text);
        if (!compactBytes.Success)
        {
            return Failure(compactBytes.Error);
        }

        if ((compactBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length > policy.MaximumTokenSizeInBytes)
        {
            return Failure(
                "token_too_large",
                "The compact JWE exceeds the configured token size limit.",
                policy.MaximumTokenSizeInBytes);
        }

        JoseCompactParseResult compact = JoseCompact.Parse(text);
        if (!compact.Success)
        {
            return Failure(compact.Error);
        }

        if ((compact.Serialization ?? throw new InvalidOperationException("A required value is null.")).Kind != TokenKind.JweCompact)
        {
            return Failure(
                "invalid_jwe_compact",
                "A compact JWE must contain exactly five segments.",
                -1);
        }

        if (compact.Serialization.GetEncodedSegment(0).Length == 0)
        {
            return Failure(
                "missing_protected_header",
                "A compact JWE requires a protected header.",
                0);
        }

        JoseHeaderResult header = JoseHeader.Read(
            compact.Serialization.GetDecodedSegmentBytes(0),
            policy.MaximumJsonDepth);
        if (!header.Success)
        {
            return Failure(header.Error);
        }

        TokenResult headerResult = ValidateHeader(
            header.Header,
            policy.UnderstoodCriticalHeaders);
        if (!headerResult.Success)
        {
            return Failure(headerResult.Error);
        }

        JsonValue? algorithmValue = (header.Header ?? throw new InvalidOperationException("A required value is null.")).Get(
            JoseHeaderNames.Algorithm);
        if (algorithmValue == null)
        {
            return Failure(
                "missing_algorithm",
                "A compact JWE protected header requires alg.",
                -1);
        }

        string? algorithm = algorithmValue.StringValue;
        if (string.IsNullOrEmpty(algorithm))
        {
            return Failure(
                "invalid_algorithm",
                "The JWE key-management algorithm must not be empty.",
                -1);
        }

        AlgorithmMetadata algorithmMetadata = AlgorithmMetadataRegistry.GetJose(
            algorithm);
        if (algorithmMetadata.Known && !JoseAlgorithmsOperations.IsKeyManagement(algorithm))
        {
            return Failure(
                "invalid_jwe_algorithm",
                "The registered JOSE algorithm is not a JWE key-management algorithm.",
                -1);
        }

        JsonValue? encryptionValue = header.Header.Get(
            JoseHeaderNames.EncryptionAlgorithm);
        if (encryptionValue == null)
        {
            return Failure(
                "missing_encryption_algorithm",
                "A compact JWE protected header requires enc.",
                -1);
        }

        string? encryptionAlgorithm = encryptionValue.StringValue;
        if ((encryptionAlgorithm ?? throw new InvalidOperationException("A required value is null.")).Length == 0)
        {
            return Failure(
                "invalid_encryption_algorithm",
                "The JWE content-encryption algorithm must not be empty.",
                -1);
        }

        AlgorithmMetadata encryptionMetadata = AlgorithmMetadataRegistry.GetJose(
            encryptionAlgorithm);
        if (encryptionMetadata.Known
            && !JoseAlgorithmsOperations.IsContentEncryption(encryptionAlgorithm))
        {
            return Failure(
                "invalid_content_encryption_algorithm",
                "The registered JOSE algorithm is not a JWE content-encryption algorithm.",
                -1);
        }

        byte[] encryptedKey = compact.Serialization.GetDecodedSegmentBytes(1);
        bool direct = string.Equals(
            algorithm,
            JoseAlgorithms.Direct,
            StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.EcdhEs,
                StringComparison.Ordinal);
        if (direct && encryptedKey.Length != 0)
        {
            return Failure(
                "invalid_encrypted_key",
                "Direct JWE key management requires an empty encrypted-key segment.",
                1);
        }

        if (!direct
            && algorithmMetadata.Known
            && encryptedKey.Length == 0)
        {
            return Failure(
                "missing_encrypted_key",
                "The JWE key-management algorithm requires an encrypted key.",
                1);
        }

        byte[] initializationVector = compact.Serialization.GetDecodedSegmentBytes(2);
        if (initializationVector.Length == 0)
        {
            return Failure(
                "missing_initialization_vector",
                "A compact JWE requires an initialization vector.",
                2);
        }

        byte[] authenticationTag = compact.Serialization.GetDecodedSegmentBytes(4);
        if (authenticationTag.Length == 0)
        {
            return Failure(
                "missing_authentication_tag",
                "A compact JWE requires an authentication tag.",
                4);
        }

        byte[] ciphertext = compact.Serialization.GetDecodedSegmentBytes(3);
        JweCompactToken token = new JweCompactToken();
        token.ProtectedHeader = header.Header;
        token.Algorithm = algorithm;
        token.AlgorithmMetadata = algorithmMetadata;
        token.AlgorithmAvailable = CryptoAlgorithmAvailability.IsJoseAlgorithmAvailable(
            algorithm);
        token.EncryptionAlgorithm = encryptionAlgorithm;
        token.EncryptionAlgorithmMetadata = encryptionMetadata;
        token.EncryptionAlgorithmAvailable =
            CryptoAlgorithmAvailability.IsJoseAlgorithmAvailable(
                encryptionAlgorithm);
        token.EncryptedKey = encryptedKey;
        token.InitializationVector = initializationVector;
        token.AuthenticationTag = authenticationTag;
        token.ProtectedSegment = compact.Serialization.GetEncodedSegment(0).ToString();
        token.ParsedCiphertext = ciphertext;

        JsonValue? compression = header.Header.Get(
            JoseHeaderNames.CompressionAlgorithm);
        if (compression != null)
        {
            token.CompressionAlgorithm = compression.StringValue;
            token.CompressionAlgorithmKnown = JoseCompressionAlgorithmsOperations.IsRegistered(
                (compression.StringValue ?? throw new InvalidOperationException("A required value is null.")));
            token.CompressionAlgorithmAvailable = string.Equals(
                compression.StringValue,
                JoseCompressionAlgorithms.Deflate,
                StringComparison.Ordinal);
        }

        JsonValue? keyId = header.Header.Get(JoseHeaderNames.KeyId);
        if (keyId != null)
        {
            token.KeyId = keyId.StringValue;
        }

        if (diagnostic)
        {
            token.Ciphertext = ciphertext.AsSpan().ToArray();
            token.CiphertextAvailable = true;
        }

        JweCompactResult result = new JweCompactResult();
        result.Token = token;
        result.Success = true;
        return result;
    }

    static TokenResult ValidateHeader(
        JoseHeader? header,
        string[]? understoodCriticalHeaders)
    {
        int index = 0;
        while (index < (header ?? throw new InvalidOperationException("A required value is null.")).Parameters.Members.Length)
        {
            string? name = header.Parameters.Members[index].Name;
            if (JoseHeaderNamesOperations.IsRegistered((name ?? throw new InvalidOperationException("A required value is null.")))
                && !JoseHeaderNamesOperations.IsForJwe(name))
            {
                return ResultFailure(
                    "invalid_jwe_header_parameter",
                    "A registered header parameter is not defined for JWE.",
                    index);
            }

            index += 1;
        }

        return JoseCriticalHeaderValidator.Validate(
            header,
            (understoodCriticalHeaders ?? throw new InvalidOperationException("A required value is null.")));
    }

    static TokenResult ResultFailure(
        string code,
        string message,
        int position)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, position);
        return result;
    }

    static JweCompactResult Failure(
        string code,
        string message,
        int position)
    {
        return Failure(new TokenError(code, message, position));
    }

    static JweCompactResult Failure(TokenError? error)
    {
        JweCompactResult result = new JweCompactResult();
        result.Error = error;
        return result;
    }
}
