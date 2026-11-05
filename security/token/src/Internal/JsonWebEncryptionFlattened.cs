using System;
using System.Security.Cryptography;
using System.Text;

namespace token;

/// <summary>Reads, writes, encrypts, and decrypts flattened JWE JSON serializations.</summary>
internal static class JsonWebEncryptionFlattened
{
    /// <summary>Parses flattened JWE JSON using conservative default limits.</summary>
    /// <param name="json">The flattened JWE JSON text.</param>
    /// <returns>A result containing unauthenticated metadata and encrypted components.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>json</c> is null.</exception>
    internal static JweFlattenedResult Parse(string json)
    {
        return Parse(json, new TokenPolicy());
    }

    /// <summary>Parses flattened JWE JSON while withholding ciphertext bytes.</summary>
    /// <param name="json">The flattened JWE JSON text.</param>
    /// <param name="policy">The header and size policy.</param>
    /// <returns>A result containing unauthenticated metadata and encrypted components.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JweFlattenedResult Parse(string json, TokenPolicy policy)
    {
        return ReadCore(json, policy, false);
    }

    /// <summary>Reads flattened JWE ciphertext when diagnostic access is enabled.</summary>
    /// <param name="json">The flattened JWE JSON text.</param>
    /// <param name="policy">The policy that must permit unauthenticated decode.</param>
    /// <returns>A result containing ciphertext and unauthenticated metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JweFlattenedResult ReadDiagnostic(
        string json,
        TokenPolicy policy)
    {
        return ReadCore(json, policy, true);
    }

    /// <summary>Writes flattened JWE JSON from decoded encrypted components.</summary>
    /// <param name="ciphertext">The ciphertext bytes.</param>
    /// <param name="protectedHeader">The integrity-protected header.</param>
    /// <param name="sharedUnprotectedHeader">The shared unprotected header, or <c>null</c>.</param>
    /// <param name="recipientHeader">The recipient header, or <c>null</c>.</param>
    /// <param name="encryptedKey">The encrypted content-encryption key.</param>
    /// <param name="initializationVector">The content-encryption initialization vector.</param>
    /// <param name="authenticationTag">The content-encryption authentication tag.</param>
    /// <param name="policy">The header and size policy.</param>
    /// <returns>A result containing flattened JWE JSON.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static TokenTextResult Write(
        byte[] ciphertext,
        JoseHeader protectedHeader,
        JoseHeader? sharedUnprotectedHeader,
        JoseHeader? recipientHeader,
        byte[] encryptedKey,
        byte[] initializationVector,
        byte[] authenticationTag,
        TokenPolicy policy)
    {
        if (ciphertext == null)
        {
            throw new ArgumentNullException("ciphertext");
        }

        return WriteCore(
            ciphertext,
            protectedHeader,
            sharedUnprotectedHeader,
            recipientHeader,
            encryptedKey,
            initializationVector,
            authenticationTag,
            policy,
            null,
            false);
    }

    /// <summary>Writes flattened JWE JSON with external Additional Authenticated Data.</summary>
    /// <param name="ciphertext">The ciphertext bytes.</param>
    /// <param name="additionalAuthenticatedData">The decoded external authenticated data bytes.</param>
    /// <param name="protectedHeader">The integrity-protected header.</param>
    /// <param name="sharedUnprotectedHeader">The shared unprotected header, or <c>null</c>.</param>
    /// <param name="recipientHeader">The recipient header, or <c>null</c>.</param>
    /// <param name="encryptedKey">The encrypted content-encryption key.</param>
    /// <param name="initializationVector">The content-encryption initialization vector.</param>
    /// <param name="authenticationTag">The content-encryption authentication tag.</param>
    /// <param name="policy">The header and size policy.</param>
    /// <returns>A result containing flattened JWE JSON with an <c>aad</c> member.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static TokenTextResult Write(
        byte[] ciphertext,
        byte[] additionalAuthenticatedData,
        JoseHeader protectedHeader,
        JoseHeader? sharedUnprotectedHeader,
        JoseHeader? recipientHeader,
        byte[] encryptedKey,
        byte[] initializationVector,
        byte[] authenticationTag,
        TokenPolicy policy)
    {
        if (ciphertext == null)
        {
            throw new ArgumentNullException("ciphertext");
        }

        if (additionalAuthenticatedData == null)
        {
            throw new ArgumentNullException("additionalAuthenticatedData");
        }

        return WriteCore(
            ciphertext,
            protectedHeader,
            sharedUnprotectedHeader,
            recipientHeader,
            encryptedKey,
            initializationVector,
            authenticationTag,
            policy,
            additionalAuthenticatedData,
            true);
    }

    /// <summary>Encrypts plaintext as flattened JWE JSON for one recipient.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="protectedHeader">The integrity-protected header.</param>
    /// <param name="sharedUnprotectedHeader">The shared unprotected header, or <c>null</c>.</param>
    /// <param name="recipientHeader">The recipient header, or <c>null</c>.</param>
    /// <param name="key">The recipient or key-encryption key.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>A result containing encrypted flattened JWE JSON.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static TokenTextResult Encrypt(
        byte[] plaintext,
        JoseHeader protectedHeader,
        JoseHeader? sharedUnprotectedHeader,
        JoseHeader? recipientHeader,
        TokenKey key,
        TokenPolicy policy)
    {
        if (plaintext == null)
        {
            throw new ArgumentNullException("plaintext");
        }

        return EncryptCore(
            plaintext,
            protectedHeader,
            sharedUnprotectedHeader,
            recipientHeader,
            key,
            policy,
            null,
            false);
    }

    /// <summary>Encrypts plaintext with external Additional Authenticated Data as flattened JWE JSON.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="additionalAuthenticatedData">The external authenticated data bytes.</param>
    /// <param name="protectedHeader">The integrity-protected header.</param>
    /// <param name="sharedUnprotectedHeader">The shared unprotected header, or <c>null</c>.</param>
    /// <param name="recipientHeader">The recipient header, or <c>null</c>.</param>
    /// <param name="key">The recipient or key-encryption key.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>A result containing encrypted flattened JWE JSON with an <c>aad</c> member.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static TokenTextResult Encrypt(
        byte[] plaintext,
        byte[] additionalAuthenticatedData,
        JoseHeader protectedHeader,
        JoseHeader? sharedUnprotectedHeader,
        JoseHeader? recipientHeader,
        TokenKey key,
        TokenPolicy policy)
    {
        if (plaintext == null)
        {
            throw new ArgumentNullException("plaintext");
        }

        if (additionalAuthenticatedData == null)
        {
            throw new ArgumentNullException("additionalAuthenticatedData");
        }

        return EncryptCore(
            plaintext,
            protectedHeader,
            sharedUnprotectedHeader,
            recipientHeader,
            key,
            policy,
            additionalAuthenticatedData,
            true);
    }

    static TokenTextResult EncryptCore(
        byte[] plaintext,
        JoseHeader protectedHeader,
        JoseHeader? sharedUnprotectedHeader,
        JoseHeader? recipientHeader,
        TokenKey key,
        TokenPolicy policy,
        byte[]? additionalAuthenticatedData,
        bool additionalAuthenticatedDataPresent)
    {

        ValidateRequired(protectedHeader, key, policy);
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

        if (additionalAuthenticatedDataPresent
            && (additionalAuthenticatedData ?? throw new InvalidOperationException("A required value is null.")).Length
                > policy.MaximumTokenSizeInBytes)
        {
            return TextFailure(
                "token_too_large",
                "The JWE external authenticated data exceeds the configured token size limit.");
        }

        JoseHeaderResult protectedBounded = BoundHeader(protectedHeader, policy);
        if (!protectedBounded.Success)
        {
            return TextFailure(protectedBounded.Error);
        }

        JoseHeaderResult sharedBounded = BoundOptionalHeader(
            sharedUnprotectedHeader,
            policy);
        if (!sharedBounded.Success)
        {
            return TextFailure(sharedBounded.Error);
        }

        JoseHeaderResult recipientBounded = BoundOptionalHeader(
            recipientHeader,
            policy);
        if (!recipientBounded.Success)
        {
            return TextFailure(recipientBounded.Error);
        }

        JoseHeaderResult effective = ValidateAndMergeHeaders(
            protectedBounded.Header,
            sharedBounded.Header,
            recipientBounded.Header,
            policy);
        if (!effective.Success)
        {
            return TextFailure(effective.Error);
        }

        TokenResult algorithms = JsonWebEncryption.ValidateEncryptionAlgorithms(
            (effective.Header ?? throw new InvalidOperationException("A required value is null.")).Get(JoseHeaderNames.Algorithm),
            effective.Header.Get(JoseHeaderNames.EncryptionAlgorithm),
            policy);
        if (!algorithms.Success)
        {
            return TextFailure(algorithms.Error);
        }

        string? algorithm = (effective.Header.Get(
            JoseHeaderNames.Algorithm) ?? throw new InvalidOperationException("A required value is null.")).StringValue;
        string? encryptionAlgorithm = (effective.Header.Get(
            JoseHeaderNames.EncryptionAlgorithm) ?? throw new InvalidOperationException("A required value is null.")).StringValue;
        byte[]? content = plaintext.AsSpan().ToArray();
        JsonValue? compression = effective.Header.Get(
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

        JweKeyManagementResult managed = JsonWebEncryption.ManageEncryptionKey(
            effective.Header,
            key,
            algorithm,
            encryptionAlgorithm,
            policy);
        if (!managed.Success)
        {
            CryptographicOperations.ZeroMemory(content);
            return TextFailure(managed.Error);
        }

        JoseHeader? finalRecipient = CopyGeneratedParameters(
            effective.Header,
            recipientBounded.Header,
            algorithm);
        TokenBytesResult protectedBytes = (protectedBounded.Header ?? throw new InvalidOperationException("A required value is null.")).WriteUtf8();
        if (!protectedBytes.Success)
        {
            JsonWebEncryption.ClearManaged(managed);
            CryptographicOperations.ZeroMemory(content);
            return TextFailure(protectedBytes.Error);
        }

        string? protectedSegment = Base64Url.Encode((protectedBytes.Bytes ?? throw new InvalidOperationException("A required value is null."))).Text;
        TokenBytesResult authenticatedData = CreateAuthenticatedData(
            protectedSegment,
            additionalAuthenticatedData,
            additionalAuthenticatedDataPresent);
        if (!authenticatedData.Success)
        {
            JsonWebEncryption.ClearManaged(managed);
            CryptographicOperations.ZeroMemory(content);
            return TextFailure(authenticatedData.Error);
        }

        TokenKey contentKey = ContentKey(
            managed.ContentEncryptionKey,
            encryptionAlgorithm,
            TokenKeyOperations.Encrypt);
        JweFlattenedContentResult encrypted = EncryptContent(
            content,
            authenticatedData.Bytes,
            contentKey,
            encryptionAlgorithm);
        contentKey.SymmetricKey = null;
        JsonWebEncryption.ClearManaged(managed);
        CryptographicOperations.ZeroMemory(content);
        CryptographicOperations.ZeroMemory(authenticatedData.Bytes);
        if (!encrypted.Success)
        {
            return TextFailure(encrypted.Error);
        }

        TokenTextResult written = WriteCore(
            encrypted.Ciphertext,
            protectedBounded.Header,
            sharedBounded.Header,
            finalRecipient,
            managed.EncryptedKey,
            encrypted.InitializationVector,
            encrypted.AuthenticationTag,
            policy,
            additionalAuthenticatedData,
            additionalAuthenticatedDataPresent);
        CryptographicOperations.ZeroMemory(encrypted.Ciphertext);
        return written;
    }

    /// <summary>Authenticates and decrypts flattened JWE JSON.</summary>
    /// <param name="json">The flattened JWE JSON text.</param>
    /// <param name="key">The recipient or key-encryption key.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>A result containing authenticated plaintext or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JweFlattenedResult Decrypt(
        string json,
        TokenKey key,
        TokenPolicy policy)
    {
        if (json == null)
        {
            throw new ArgumentNullException("json");
        }

        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        JweFlattenedResult parsed = ReadCore(json, policy, false);
        if (!parsed.Success)
        {
            return parsed;
        }

        TokenResult algorithms = JsonWebEncryption.ValidateEncryptionAlgorithms(
            ((parsed.Token ?? throw new InvalidOperationException("A required value is null.")).EffectiveHeader ?? throw new InvalidOperationException("A required value is null.")).Get(JoseHeaderNames.Algorithm),
            (parsed.Token.EffectiveHeader ?? throw new InvalidOperationException("A required value is null.")).Get(JoseHeaderNames.EncryptionAlgorithm),
            policy);
        if (!algorithms.Success)
        {
            return Failure(algorithms.Error);
        }

        JweKeyManagementResult managed = JsonWebEncryption.ManageDecryptionKey(
            parsed.Token.EffectiveHeader,
            parsed.Token.EncryptedKey,
            parsed.Token.Algorithm,
            parsed.Token.EncryptionAlgorithm,
            key,
            policy);
        if (!managed.Success)
        {
            return Failure(managed.Error);
        }

        TokenBytesResult authenticatedData = CreateAuthenticatedData(
            parsed.Token.ProtectedSegment,
            parsed.Token.AdditionalAuthenticatedData,
            parsed.Token.AdditionalAuthenticatedDataPresent);
        if (!authenticatedData.Success)
        {
            JsonWebEncryption.ClearManaged(managed);
            return Failure(authenticatedData.Error);
        }

        TokenKey contentKey = ContentKey(
            managed.ContentEncryptionKey,
            parsed.Token.EncryptionAlgorithm,
            TokenKeyOperations.Decrypt);
        TokenBytesResult plaintext = JsonWebEncryption.IsGcmContentEncryption(
            parsed.Token.EncryptionAlgorithm)
            ? AesGcmContentEncryption.Decrypt(
                (parsed.Token.ParsedCiphertext ?? throw new InvalidOperationException("A required value is null.")),
                (parsed.Token.InitializationVector ?? throw new InvalidOperationException("A required value is null.")),
                (parsed.Token.AuthenticationTag ?? throw new InvalidOperationException("A required value is null.")),
                (authenticatedData.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                contentKey,
                (parsed.Token.EncryptionAlgorithm ?? throw new InvalidOperationException("A required value is null.")))
            : AesCbcHmacContentEncryption.Decrypt(
                (parsed.Token.ParsedCiphertext ?? throw new InvalidOperationException("A required value is null.")),
                (parsed.Token.InitializationVector ?? throw new InvalidOperationException("A required value is null.")),
                (parsed.Token.AuthenticationTag ?? throw new InvalidOperationException("A required value is null.")),
                (authenticatedData.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                contentKey,
                (parsed.Token.EncryptionAlgorithm ?? throw new InvalidOperationException("A required value is null.")));
        contentKey.SymmetricKey = null;
        JsonWebEncryption.ClearManaged(managed);
        CryptographicOperations.ZeroMemory(authenticatedData.Bytes);
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
                "The JWE plaintext exceeds the configured token size limit.");
        }

        parsed.Token.Plaintext = authenticatedPlaintext;
        parsed.Token.PlaintextAvailable = true;
        parsed.Token.AuthenticationTagVerified = true;
        return parsed;
    }

    static JweFlattenedResult ReadCore(
        string json,
        TokenPolicy policy,
        bool diagnostic)
    {
        if (json == null)
        {
            throw new ArgumentNullException("json");
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
                "Diagnostic JWE ciphertext decoding requires explicit policy opt-in.");
        }

        TokenBytesResult jsonBytes = Utf8Codec.Encode(json);
        if (!jsonBytes.Success)
        {
            return Failure(jsonBytes.Error);
        }

        if ((jsonBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length > policy.MaximumTokenSizeInBytes)
        {
            return Failure(
                "token_too_large",
                "The flattened JWE exceeds the configured token size limit.");
        }

        JsonObjectResult root = JsonObject.Read(json, policy.MaximumJsonDepth);
        if (!root.Success)
        {
            return Failure(root.Error);
        }

        TokenResult shape = ValidateShape(root.Object);
        if (!shape.Success)
        {
            return Failure(shape.Error);
        }

        JsonValue? protectedValue = (root.Object ?? throw new InvalidOperationException("A required value is null.")).Get("protected");
        string protectedSegment = string.Empty;
        JoseHeaderResult protectedHeader = new JoseHeaderResult();
        if (protectedValue == null)
        {
            protectedHeader.Success = true;
            protectedHeader.Header = new JoseHeader();
        }
        else if (protectedValue.Kind != TokenJsonValueKind.String
            || protectedValue.StringValue == null
            || protectedValue.StringValue.Length == 0)
        {
            return Failure(
                "invalid_protected_header",
                "The protected member must be a nonempty string.");
        }
        else
        {
            protectedSegment = protectedValue.StringValue;
            TokenBytesResult protectedBytes = Base64Url.Decode(
                protectedSegment);
            if (!protectedBytes.Success)
            {
                return Failure(
                    "invalid_protected_header",
                    "The protected member is not canonical base64url.");
            }

            protectedHeader = JoseHeader.Read(
                (protectedBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                policy.MaximumJsonDepth);
            if (!protectedHeader.Success)
            {
                return Failure(protectedHeader.Error);
            }
        }

        JoseHeaderResult sharedHeader = ReadOptionalHeader(
            root.Object,
            "unprotected",
            policy);
        if (!sharedHeader.Success)
        {
            return Failure(sharedHeader.Error);
        }

        JoseHeaderResult recipientHeader = ReadOptionalHeader(
            root.Object,
            "header",
            policy);
        if (!recipientHeader.Success)
        {
            return Failure(recipientHeader.Error);
        }

        JoseHeaderResult effective = ValidateAndMergeHeaders(
            protectedHeader.Header,
            sharedHeader.Header,
            recipientHeader.Header,
            policy);
        if (!effective.Success)
        {
            return Failure(effective.Error);
        }

        TokenResult algorithmShape = ValidateAlgorithmShape(effective.Header);
        if (!algorithmShape.Success)
        {
            return Failure(algorithmShape.Error);
        }

        string? algorithm = ((effective.Header ?? throw new InvalidOperationException("A required value is null.")).Get(
            JoseHeaderNames.Algorithm) ?? throw new InvalidOperationException("A required value is null.")).StringValue;
        string? encryptionAlgorithm = (effective.Header.Get(
            JoseHeaderNames.EncryptionAlgorithm) ?? throw new InvalidOperationException("A required value is null.")).StringValue;
        TokenBytesResult encryptedKey = ReadEncryptedKey(root.Object);
        if (!encryptedKey.Success)
        {
            return Failure(encryptedKey.Error);
        }

        bool direct = string.Equals(
            algorithm,
            JoseAlgorithms.Direct,
            StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.EcdhEs,
                StringComparison.Ordinal);
        if (direct && (encryptedKey.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length != 0)
        {
            return Failure(
                "invalid_encrypted_key",
                "Direct JWE key management requires an empty encrypted key.");
        }

        AlgorithmMetadata algorithmMetadata = AlgorithmMetadataRegistry.GetJose(
            (algorithm ?? throw new InvalidOperationException("A required value is null.")));
        if (!direct && algorithmMetadata.Known
            && (encryptedKey.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length == 0)
        {
            return Failure(
                "missing_encrypted_key",
                "The JWE key-management algorithm requires an encrypted key.");
        }

        TokenBytesResult initializationVector = ReadComponent(
            root.Object,
            "iv",
            true);
        if (!initializationVector.Success)
        {
            return Failure(initializationVector.Error);
        }

        TokenBytesResult ciphertext = ReadComponent(
            root.Object,
            "ciphertext",
            false);
        if (!ciphertext.Success)
        {
            return Failure(ciphertext.Error);
        }

        TokenBytesResult authenticationTag = ReadComponent(
            root.Object,
            "tag",
            true);
        if (!authenticationTag.Success)
        {
            return Failure(authenticationTag.Error);
        }

        byte[]? additionalAuthenticatedData = null;
        string? additionalAuthenticatedDataSegment = null;
        JsonValue? additionalAuthenticatedDataValue = root.Object.Get("aad");
        if (additionalAuthenticatedDataValue != null)
        {
            if (additionalAuthenticatedDataValue.Kind != TokenJsonValueKind.String
                || additionalAuthenticatedDataValue.StringValue == null)
            {
                return Failure(
                    "invalid_additional_authenticated_data",
                    "The JWE aad member must be a base64url string.");
            }

            TokenBytesResult decoded = Base64Url.Decode(
                additionalAuthenticatedDataValue.StringValue);
            if (!decoded.Success)
            {
                return Failure(
                    "invalid_additional_authenticated_data",
                    "The JWE aad member is not canonical base64url.");
            }

            additionalAuthenticatedData = decoded.Bytes;
            additionalAuthenticatedDataSegment =
                additionalAuthenticatedDataValue.StringValue;
        }

        JweFlattenedToken token = new JweFlattenedToken();
        token.ProtectedHeader = protectedHeader.Header;
        token.SharedUnprotectedHeader = sharedHeader.Header;
        token.RecipientHeader = recipientHeader.Header;
        token.EffectiveHeader = effective.Header;
        token.Algorithm = algorithm;
        token.AlgorithmMetadata = algorithmMetadata;
        token.AlgorithmAvailable = CryptoAlgorithmAvailability.IsJoseAlgorithmAvailable(
            algorithm);
        token.EncryptionAlgorithm = encryptionAlgorithm;
        token.EncryptionAlgorithmMetadata = AlgorithmMetadataRegistry.GetJose(
            (encryptionAlgorithm ?? throw new InvalidOperationException("A required value is null.")));
        token.EncryptionAlgorithmAvailable =
            CryptoAlgorithmAvailability.IsJoseAlgorithmAvailable(
                encryptionAlgorithm);
        token.EncryptedKey = encryptedKey.Bytes;
        token.InitializationVector = initializationVector.Bytes;
        token.AuthenticationTag = authenticationTag.Bytes;
        token.AdditionalAuthenticatedData = additionalAuthenticatedData;
        token.AdditionalAuthenticatedDataPresent =
            additionalAuthenticatedDataValue != null;
        token.ProtectedSegment = protectedSegment;
        token.AdditionalAuthenticatedDataSegment =
            additionalAuthenticatedDataSegment;
        token.ParsedCiphertext = ciphertext.Bytes;
        JsonValue? compression = effective.Header.Get(
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

        JsonValue? keyId = effective.Header.Get(JoseHeaderNames.KeyId);
        if (keyId != null)
        {
            token.KeyId = keyId.StringValue;
        }

        if (diagnostic)
        {
            token.Ciphertext = ciphertext.Bytes.AsSpan().ToArray();
            token.CiphertextAvailable = true;
        }

        JweFlattenedResult result = new JweFlattenedResult();
        result.Token = token;
        result.Success = true;
        return result;
    }

    static TokenTextResult WriteCore(
        byte[]? ciphertext,
        JoseHeader protectedHeader,
        JoseHeader? sharedUnprotectedHeader,
        JoseHeader? recipientHeader,
        byte[]? encryptedKey,
        byte[]? initializationVector,
        byte[]? authenticationTag,
        TokenPolicy policy,
        byte[]? additionalAuthenticatedData,
        bool additionalAuthenticatedDataPresent)
    {
        if (protectedHeader == null)
        {
            throw new ArgumentNullException("protectedHeader");
        }

        if (encryptedKey == null)
        {
            throw new ArgumentNullException("encryptedKey");
        }

        if (initializationVector == null)
        {
            throw new ArgumentNullException("initializationVector");
        }

        if (authenticationTag == null)
        {
            throw new ArgumentNullException("authenticationTag");
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

        if ((ciphertext ?? throw new InvalidOperationException("A required value is null.")).Length > policy.MaximumTokenSizeInBytes
            || encryptedKey.Length > policy.MaximumTokenSizeInBytes
            || initializationVector.Length > policy.MaximumTokenSizeInBytes
            || authenticationTag.Length > policy.MaximumTokenSizeInBytes
            || additionalAuthenticatedDataPresent
                && (additionalAuthenticatedData ?? throw new InvalidOperationException("A required value is null.")).Length
                    > policy.MaximumTokenSizeInBytes)
        {
            return TextFailure(
                "token_too_large",
                "A flattened JWE component exceeds the configured size limit.");
        }

        if (initializationVector.Length == 0)
        {
            return TextFailure(
                "missing_initialization_vector",
                "A flattened JWE requires an initialization vector.");
        }

        if (authenticationTag.Length == 0)
        {
            return TextFailure(
                "missing_authentication_tag",
                "A flattened JWE requires an authentication tag.");
        }

        JoseHeaderResult protectedBounded = BoundHeader(
            protectedHeader,
            policy);
        if (!protectedBounded.Success)
        {
            return TextFailure(protectedBounded.Error);
        }

        JoseHeaderResult sharedBounded = BoundOptionalHeader(
            sharedUnprotectedHeader,
            policy);
        if (!sharedBounded.Success)
        {
            return TextFailure(sharedBounded.Error);
        }

        JoseHeaderResult recipientBounded = BoundOptionalHeader(
            recipientHeader,
            policy);
        if (!recipientBounded.Success)
        {
            return TextFailure(recipientBounded.Error);
        }

        JoseHeaderResult effective = ValidateAndMergeHeaders(
            protectedBounded.Header,
            sharedBounded.Header,
            recipientBounded.Header,
            policy);
        if (!effective.Success)
        {
            return TextFailure(effective.Error);
        }

        TokenResult algorithms = ValidateAlgorithmShape(effective.Header);
        if (!algorithms.Success)
        {
            return TextFailure(algorithms.Error);
        }

        string? algorithm = ((effective.Header ?? throw new InvalidOperationException("A required value is null.")).Get(
            JoseHeaderNames.Algorithm) ?? throw new InvalidOperationException("A required value is null.")).StringValue;
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
            return TextFailure(
                "invalid_encrypted_key",
                "Direct JWE key management requires an empty encrypted key.");
        }

        if (!direct
            && AlgorithmMetadataRegistry.GetJose((algorithm ?? throw new InvalidOperationException("A required value is null."))).Known
            && encryptedKey.Length == 0)
        {
            return TextFailure(
                "missing_encrypted_key",
                "The JWE key-management algorithm requires an encrypted key.");
        }

        TokenBytesResult protectedBytes = (protectedBounded.Header ?? throw new InvalidOperationException("A required value is null.")).WriteUtf8();
        if (!protectedBytes.Success)
        {
            return TextFailure(protectedBytes.Error);
        }

        string? protectedSegment = Base64Url.Encode((protectedBytes.Bytes ?? throw new InvalidOperationException("A required value is null."))).Text;
        StringBuilder builder = new StringBuilder();
        builder.Append('{');
        builder.Append("\"protected\":");
        builder.Append(JsonWriter.WriteStringValue(protectedSegment).Text);
        if (additionalAuthenticatedDataPresent)
        {
            AppendComponent(
                builder,
                "aad",
                additionalAuthenticatedData);
        }

        if (sharedBounded.Header != null)
        {
            TokenTextResult sharedText = sharedBounded.Header.Write();
            if (!sharedText.Success)
            {
                return TextFailure(sharedText.Error);
            }

            builder.Append(",\"unprotected\":");
            builder.Append(sharedText.Text);
        }

        if (recipientBounded.Header != null)
        {
            TokenTextResult recipientText = recipientBounded.Header.Write();
            if (!recipientText.Success)
            {
                return TextFailure(recipientText.Error);
            }

            builder.Append(",\"header\":");
            builder.Append(recipientText.Text);
        }

        if (encryptedKey.Length != 0)
        {
            AppendComponent(builder, "encrypted_key", encryptedKey);
        }

        AppendComponent(builder, "iv", initializationVector);
        AppendComponent(builder, "ciphertext", ciphertext);
        AppendComponent(builder, "tag", authenticationTag);
        builder.Append('}');
        string json = builder.ToString();
        TokenBytesResult encoded = Utf8Codec.Encode(json);
        if (!encoded.Success)
        {
            return TextFailure(encoded.Error);
        }

        if ((encoded.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length > policy.MaximumTokenSizeInBytes)
        {
            return TextFailure(
                "token_too_large",
                "The flattened JWE exceeds the configured token size limit.");
        }

        TokenTextResult result = new TokenTextResult();
        result.Text = json;
        result.Success = true;
        return result;
    }

    internal static JweFlattenedContentResult EncryptContent(
        byte[]? plaintext,
        byte[]? authenticatedData,
        TokenKey contentKey,
        string? encryptionAlgorithm)
    {
        JweFlattenedContentResult result = new JweFlattenedContentResult();
        if (JsonWebEncryption.IsGcmContentEncryption(encryptionAlgorithm))
        {
            AesGcmContentEncryptionResult encrypted =
                AesGcmContentEncryption.Encrypt(
                    (plaintext ?? throw new InvalidOperationException("A required value is null.")),
                    (authenticatedData ?? throw new InvalidOperationException("A required value is null.")),
                    contentKey,
                    (encryptionAlgorithm ?? throw new InvalidOperationException("A required value is null.")));
            if (!encrypted.Success)
            {
                result.Error = encrypted.Error;
                return result;
            }

            result.InitializationVector = encrypted.InitializationVector;
            result.Ciphertext = encrypted.Ciphertext;
            result.AuthenticationTag = encrypted.AuthenticationTag;
        }
        else
        {
            AesCbcHmacContentEncryptionResult encrypted =
                AesCbcHmacContentEncryption.Encrypt(
                    (plaintext ?? throw new InvalidOperationException("A required value is null.")),
                    (authenticatedData ?? throw new InvalidOperationException("A required value is null.")),
                    contentKey,
                    (encryptionAlgorithm ?? throw new InvalidOperationException("A required value is null.")));
            if (!encrypted.Success)
            {
                result.Error = encrypted.Error;
                return result;
            }

            result.InitializationVector = encrypted.InitializationVector;
            result.Ciphertext = encrypted.Ciphertext;
            result.AuthenticationTag = encrypted.AuthenticationTag;
        }

        result.Success = true;
        return result;
    }

    internal static TokenBytesResult CreateAuthenticatedData(
        string? protectedSegment,
        byte[]? additionalAuthenticatedData,
        bool additionalAuthenticatedDataPresent)
    {
        if (!additionalAuthenticatedDataPresent)
        {
            return Utf8Codec.Encode((protectedSegment ?? throw new InvalidOperationException("A required value is null.")));
        }

        string? additionalSegment = Base64Url.Encode(
            (additionalAuthenticatedData ?? throw new InvalidOperationException("A required value is null."))).Text;
        return Utf8Codec.Encode(protectedSegment + "." + additionalSegment);
    }

    internal static JoseHeaderResult ValidateAndMergeHeaders(
        JoseHeader? protectedHeader,
        JoseHeader? sharedUnprotectedHeader,
        JoseHeader? recipientHeader,
        TokenPolicy policy)
    {
        if (protectedHeader == null)
        {
            return HeaderFailure(
                "missing_protected_header",
                "A flattened JWE requires a protected header.");
        }

        TokenResult protectedResult = ValidateHeaderMembers(protectedHeader);
        if (!protectedResult.Success)
        {
            return HeaderFailure(protectedResult.Error);
        }

        if (sharedUnprotectedHeader != null)
        {
            TokenResult sharedResult = ValidateHeaderMembers(
                sharedUnprotectedHeader);
            if (!sharedResult.Success)
            {
                return HeaderFailure(sharedResult.Error);
            }
        }

        if (recipientHeader != null)
        {
            TokenResult recipientResult = ValidateHeaderMembers(recipientHeader);
            if (!recipientResult.Success)
            {
                return HeaderFailure(recipientResult.Error);
            }
        }

        int count = (sharedUnprotectedHeader == null ? 0 : 1)
            + (recipientHeader == null ? 0 : 1);
        JoseHeader[] unprotected = new JoseHeader[count];
        int destination = 0;
        if (sharedUnprotectedHeader != null)
        {
            unprotected[destination] = sharedUnprotectedHeader;
            destination += 1;
        }

        if (recipientHeader != null)
        {
            unprotected[destination] = recipientHeader;
        }

        TokenResult critical = JoseCriticalHeaderValidator.Validate(
            protectedHeader,
            unprotected,
            (policy.UnderstoodCriticalHeaders ?? throw new InvalidOperationException("A required value is null.")));
        if (!critical.Success)
        {
            return HeaderFailure(critical.Error);
        }

        int memberCount = protectedHeader.Parameters.Members.Length;
        if (sharedUnprotectedHeader != null)
        {
            memberCount += sharedUnprotectedHeader.Parameters.Members.Length;
        }

        if (recipientHeader != null)
        {
            memberCount += recipientHeader.Parameters.Members.Length;
        }

        JsonMember[] members = new JsonMember[memberCount];
        destination = CopyMembers(protectedHeader, members, 0);
        if (sharedUnprotectedHeader != null)
        {
            destination = CopyMembers(
                sharedUnprotectedHeader,
                members,
                destination);
        }

        if (recipientHeader != null)
        {
            CopyMembers(recipientHeader, members, destination);
        }

        JoseHeaderResult result = new JoseHeaderResult();
        result.Header = JoseHeader.Create(new JsonObject(members));
        result.Success = true;
        return result;
    }

    static TokenResult ValidateHeaderMembers(JoseHeader header)
    {
        if (header.Parameters == null
            || header.Parameters.Members == null)
        {
            return ResultFailure(
                "invalid_header",
                "A flattened JWE header is incomplete.");
        }

        int index = 0;
        while (index < header.Parameters.Members.Length)
        {
            string? name = header.Parameters.Members[index].Name;
            if (JoseHeaderNamesOperations.IsRegistered((name ?? throw new InvalidOperationException("A required value is null.")))
                && !JoseHeaderNamesOperations.IsForJwe(name))
            {
                return ResultFailure(
                    "invalid_jwe_header_parameter",
                    "A registered header parameter is not defined for JWE.");
            }

            index += 1;
        }

        return Success();
    }

    static TokenResult ValidateAlgorithmShape(JoseHeader? header)
    {
        JsonValue? algorithmValue = (header ?? throw new InvalidOperationException("A required value is null.")).Get(JoseHeaderNames.Algorithm);
        if (algorithmValue == null)
        {
            return ResultFailure(
                "missing_algorithm",
                "A flattened JWE header requires alg.");
        }

        string? algorithm = algorithmValue.StringValue;
        if (string.IsNullOrEmpty(algorithm))
        {
            return ResultFailure(
                "invalid_algorithm",
                "The JWE key-management algorithm must not be empty.");
        }

        AlgorithmMetadata algorithmMetadata = AlgorithmMetadataRegistry.GetJose(
            algorithm);
        if (algorithmMetadata.Known
            && !JoseAlgorithmsOperations.IsKeyManagement(algorithm))
        {
            return ResultFailure(
                "invalid_jwe_algorithm",
                "The registered algorithm is not for JWE key management.");
        }

        JsonValue? encryptionValue = header.Get(
            JoseHeaderNames.EncryptionAlgorithm);
        if (encryptionValue == null)
        {
            return ResultFailure(
                "missing_encryption_algorithm",
                "A flattened JWE header requires enc.");
        }

        string? encryptionAlgorithm = encryptionValue.StringValue;
        if ((encryptionAlgorithm ?? throw new InvalidOperationException("A required value is null.")).Length == 0)
        {
            return ResultFailure(
                "invalid_encryption_algorithm",
                "The JWE content-encryption algorithm must not be empty.");
        }

        AlgorithmMetadata encryptionMetadata = AlgorithmMetadataRegistry.GetJose(
            encryptionAlgorithm);
        if (encryptionMetadata.Known
            && !JoseAlgorithmsOperations.IsContentEncryption(encryptionAlgorithm))
        {
            return ResultFailure(
                "invalid_content_encryption_algorithm",
                "The registered algorithm is not for JWE content encryption.");
        }

        return Success();
    }

    static TokenResult ValidateShape(JsonObject? root)
    {
        int index = 0;
        while (index < (root ?? throw new InvalidOperationException("A required value is null.")).Members.Length)
        {
            string? name = root.Members[index].Name;
            if (!string.Equals(name, "protected", StringComparison.Ordinal)
                && !string.Equals(name, "aad", StringComparison.Ordinal)
                && !string.Equals(name, "unprotected", StringComparison.Ordinal)
                && !string.Equals(name, "header", StringComparison.Ordinal)
                && !string.Equals(name, "encrypted_key", StringComparison.Ordinal)
                && !string.Equals(name, "iv", StringComparison.Ordinal)
                && !string.Equals(name, "ciphertext", StringComparison.Ordinal)
                && !string.Equals(name, "tag", StringComparison.Ordinal))
            {
                return ResultFailure(
                    "invalid_jwe_json_member",
                    "The flattened JWE contains an unexpected member.");
            }

            index += 1;
        }

        return Success();
    }

    static TokenBytesResult ReadComponent(
        JsonObject root,
        string name,
        bool nonempty)
    {
        JsonValue? value = root.Get(name);
        if (value == null
            || value.Kind != TokenJsonValueKind.String
            || value.StringValue == null)
        {
            return BytesFailure(
                "missing_" + ComponentCode(name),
                "A flattened JWE requires the " + name + " string member.");
        }

        TokenBytesResult decoded = Base64Url.Decode(value.StringValue);
        if (!decoded.Success)
        {
            return BytesFailure(
                "invalid_" + ComponentCode(name),
                "A flattened JWE component is not canonical base64url.");
        }

        if (nonempty && (decoded.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length == 0)
        {
            return BytesFailure(
                "missing_" + ComponentCode(name),
                "A required flattened JWE component is empty.");
        }

        return decoded;
    }

    static TokenBytesResult ReadEncryptedKey(JsonObject root)
    {
        JsonValue? value = root.Get("encrypted_key");
        if (value == null)
        {
            TokenBytesResult empty = new TokenBytesResult();
            empty.Bytes = Array.Empty<byte>();
            empty.Success = true;
            return empty;
        }

        if (value.Kind != TokenJsonValueKind.String
            || value.StringValue == null)
        {
            return BytesFailure(
                "invalid_encrypted_key",
                "The encrypted_key member must be a base64url string.");
        }

        TokenBytesResult decoded = Base64Url.Decode(value.StringValue);
        if (!decoded.Success)
        {
            return BytesFailure(
                "invalid_encrypted_key",
                "The encrypted_key member is not canonical base64url.");
        }

        return decoded;
    }

    static string ComponentCode(string name)
    {
        if (string.Equals(name, "iv", StringComparison.Ordinal))
        {
            return "initialization_vector";
        }

        if (string.Equals(name, "tag", StringComparison.Ordinal))
        {
            return "authentication_tag";
        }

        return name;
    }

    static JoseHeaderResult ReadOptionalHeader(
        JsonObject root,
        string name,
        TokenPolicy policy)
    {
        JsonValue? value = root.Get(name);
        if (value == null)
        {
            JoseHeaderResult absent = new JoseHeaderResult();
            absent.Success = true;
            return absent;
        }

        if (value.Kind != TokenJsonValueKind.Object
            || value.ObjectValue == null)
        {
            return HeaderFailure(
                "invalid_unprotected_header",
                "A flattened JWE unprotected header must be an object.");
        }

        TokenTextResult text = JoseHeader.Create(value.ObjectValue).Write();
        if (!text.Success)
        {
            return HeaderFailure(text.Error);
        }

        return JoseHeader.Read((text.Text ?? throw new InvalidOperationException("A required value is null.")), policy.MaximumJsonDepth);
    }

    internal static JoseHeaderResult BoundHeader(
        JoseHeader header,
        TokenPolicy policy)
    {
        TokenBytesResult bytes = header.WriteUtf8();
        if (!bytes.Success)
        {
            return HeaderFailure(bytes.Error);
        }

        return JoseHeader.Read((bytes.Bytes ?? throw new InvalidOperationException("A required value is null.")), policy.MaximumJsonDepth);
    }

    internal static JoseHeaderResult BoundOptionalHeader(
        JoseHeader? header,
        TokenPolicy policy)
    {
        if (header == null)
        {
            JoseHeaderResult absent = new JoseHeaderResult();
            absent.Success = true;
            return absent;
        }

        return BoundHeader(header, policy);
    }

    internal static JoseHeader? CopyGeneratedParameters(
        JoseHeader effective,
        JoseHeader? recipient,
        string? algorithm)
    {
        JoseHeader? result = recipient;
        if (result == null)
        {
            result = new JoseHeader();
        }

        if (string.Equals(
            algorithm,
            JoseAlgorithms.Aes128GcmKeyWrap,
            StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.Aes192GcmKeyWrap,
                StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.Aes256GcmKeyWrap,
                StringComparison.Ordinal))
        {
            CopyParameter(effective, result, JoseHeaderNames.InitializationVector);
            CopyParameter(effective, result, JoseHeaderNames.AuthenticationTag);
        }

        if (algorithm != null
            && algorithm.StartsWith("PBES2-", StringComparison.Ordinal))
        {
            CopyParameter(effective, result, JoseHeaderNames.Pbes2SaltInput);
            CopyParameter(effective, result, JoseHeaderNames.Pbes2Count);
        }

        if (algorithm != null
            && algorithm.StartsWith("ECDH-ES", StringComparison.Ordinal))
        {
            CopyParameter(effective, result, JoseHeaderNames.EphemeralPublicKey);
        }

        return result.Parameters.Members.Length == 0 ? null : result;
    }

    static void CopyParameter(
        JoseHeader source,
        JoseHeader destination,
        string name)
    {
        JsonValue? value = source.Get(name);
        if (value != null)
        {
            destination.Set(name, value);
        }
    }

    static int CopyMembers(
        JoseHeader source,
        JsonMember[] destination,
        int offset)
    {
        int index = 0;
        while (index < source.Parameters.Members.Length)
        {
            JsonMember member = source.Parameters.Members[index];
            destination[offset + index] = new JsonMember((member.Name ?? throw new InvalidOperationException("A required value is null.")), (member.Value ?? throw new InvalidOperationException("A required value is null.")));
            index += 1;
        }

        return offset + source.Parameters.Members.Length;
    }

    static void AppendComponent(
        StringBuilder builder,
        string name,
        byte[]? bytes)
    {
        builder.Append(",\"");
        builder.Append(name);
        builder.Append("\":");
        builder.Append(JsonWriter.WriteStringValue(Base64Url.Encode((bytes ?? throw new InvalidOperationException("A required value is null."))).Text).Text);
    }

    internal static TokenKey ContentKey(
        byte[]? bytes,
        string? algorithm,
        string operation)
    {
        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.Symmetric;
        key.SymmetricKey = bytes;
        key.Algorithm = algorithm;
        key.Use = TokenKeyUses.Encryption;
        key.KeyOperations = new string[] { operation };
        return key;
    }

    static void ValidateRequired(
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
    }

    static TokenResult Success()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult ResultFailure(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }

    static TokenBytesResult BytesFailure(string code, string message)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }

    static JoseHeaderResult HeaderFailure(string code, string message)
    {
        return HeaderFailure(new TokenError(code, message, -1));
    }

    static JoseHeaderResult HeaderFailure(TokenError? error)
    {
        JoseHeaderResult result = new JoseHeaderResult();
        result.Error = error;
        return result;
    }

    static JweFlattenedResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static JweFlattenedResult Failure(TokenError? error)
    {
        JweFlattenedResult result = new JweFlattenedResult();
        result.Error = error;
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
}
