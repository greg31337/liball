using System;
using System.Security.Cryptography;
using System.Text;

namespace token;

/// <summary>Reads, writes, encrypts, and decrypts general JWE JSON serializations.</summary>
internal static class JsonWebEncryptionGeneral
{
    /// <summary>Parses general JWE JSON using conservative default limits.</summary>
    /// <param name="json">The general JWE JSON text.</param>
    /// <returns>A result containing unauthenticated metadata and encrypted components.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>json</c> is null.</exception>
    internal static JweGeneralResult Parse(string json)
    {
        return Parse(json, new TokenPolicy());
    }

    /// <summary>Parses general JWE JSON while withholding ciphertext bytes.</summary>
    /// <param name="json">The general JWE JSON text.</param>
    /// <param name="policy">The header and size policy.</param>
    /// <returns>A result containing unauthenticated metadata and encrypted components.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JweGeneralResult Parse(string json, TokenPolicy policy)
    {
        return ReadCore(json, policy, false);
    }

    /// <summary>Reads general JWE ciphertext when diagnostic access is enabled.</summary>
    /// <param name="json">The general JWE JSON text.</param>
    /// <param name="policy">The policy that must permit unauthenticated decode.</param>
    /// <returns>A result containing ciphertext and unauthenticated metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JweGeneralResult ReadDiagnostic(
        string json,
        TokenPolicy policy)
    {
        return ReadCore(json, policy, true);
    }

    /// <summary>Writes general JWE JSON from decoded encrypted components.</summary>
    /// <param name="ciphertext">The shared ciphertext bytes.</param>
    /// <param name="protectedHeader">The integrity-protected header.</param>
    /// <param name="sharedUnprotectedHeader">The shared unprotected header, or <c>null</c>.</param>
    /// <param name="recipientHeaders">Recipient headers in recipient order.</param>
    /// <param name="encryptedKeys">Encrypted content-encryption keys in recipient order.</param>
    /// <param name="initializationVector">The shared content-encryption initialization vector.</param>
    /// <param name="authenticationTag">The shared content-encryption authentication tag.</param>
    /// <param name="policy">The header and size policy.</param>
    /// <returns>A result containing general JWE JSON.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static TokenTextResult Write(
        byte[] ciphertext,
        JoseHeader protectedHeader,
        JoseHeader? sharedUnprotectedHeader,
        JoseHeader?[] recipientHeaders,
        byte[][] encryptedKeys,
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
            recipientHeaders,
            encryptedKeys,
            initializationVector,
            authenticationTag,
            policy,
            null,
            false);
    }

    /// <summary>Writes general JWE JSON with external Additional Authenticated Data.</summary>
    /// <param name="ciphertext">The shared ciphertext bytes.</param>
    /// <param name="additionalAuthenticatedData">The decoded external authenticated data bytes.</param>
    /// <param name="protectedHeader">The integrity-protected header.</param>
    /// <param name="sharedUnprotectedHeader">The shared unprotected header, or <c>null</c>.</param>
    /// <param name="recipientHeaders">Recipient headers in recipient order.</param>
    /// <param name="encryptedKeys">Encrypted content-encryption keys in recipient order.</param>
    /// <param name="initializationVector">The shared content-encryption initialization vector.</param>
    /// <param name="authenticationTag">The shared content-encryption authentication tag.</param>
    /// <param name="policy">The header and size policy.</param>
    /// <returns>A result containing general JWE JSON with an <c>aad</c> member.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static TokenTextResult Write(
        byte[] ciphertext,
        byte[] additionalAuthenticatedData,
        JoseHeader protectedHeader,
        JoseHeader? sharedUnprotectedHeader,
        JoseHeader?[] recipientHeaders,
        byte[][] encryptedKeys,
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
            recipientHeaders,
            encryptedKeys,
            initializationVector,
            authenticationTag,
            policy,
            additionalAuthenticatedData,
            true);
    }

    static TokenTextResult WriteCore(
        byte[]? ciphertext,
        JoseHeader protectedHeader,
        JoseHeader? sharedUnprotectedHeader,
        JoseHeader?[] recipientHeaders,
        byte[][] encryptedKeys,
        byte[]? initializationVector,
        byte[]? authenticationTag,
        TokenPolicy policy,
        byte[]? additionalAuthenticatedData,
        bool additionalAuthenticatedDataPresent)
    {

        ValidateArrays(recipientHeaders, encryptedKeys, policy);
        TokenTextResult[] flattened = new TokenTextResult[recipientHeaders.Length];
        int index = 0;
        while (index < recipientHeaders.Length)
        {
            if (encryptedKeys[index] == null)
            {
                return TextFailure(
                    "invalid_encrypted_key",
                    "A general JWE encrypted-key entry is missing.");
            }

            flattened[index] = additionalAuthenticatedDataPresent
                ? JsonWebEncryptionFlattened.Write(
                    (ciphertext ?? throw new InvalidOperationException("A required value is null.")),
                    (additionalAuthenticatedData ?? throw new InvalidOperationException("A required value is null.")),
                    protectedHeader,
                    sharedUnprotectedHeader,
                    recipientHeaders[index],
                    encryptedKeys[index],
                    (initializationVector ?? throw new InvalidOperationException("A required value is null.")),
                    (authenticationTag ?? throw new InvalidOperationException("A required value is null.")),
                    policy)
                : JsonWebEncryptionFlattened.Write(
                    (ciphertext ?? throw new InvalidOperationException("A required value is null.")),
                    protectedHeader,
                    sharedUnprotectedHeader,
                    recipientHeaders[index],
                    encryptedKeys[index],
                    (initializationVector ?? throw new InvalidOperationException("A required value is null.")),
                    (authenticationTag ?? throw new InvalidOperationException("A required value is null.")),
                    policy);
            if (!flattened[index].Success)
            {
                return flattened[index];
            }

            index += 1;
        }

        return CombineFlattened(flattened, policy);
    }

    /// <summary>Encrypts plaintext once and wraps its content key for multiple recipients.</summary>
    /// <param name="plaintext">The shared plaintext bytes.</param>
    /// <param name="protectedHeader">The integrity-protected header.</param>
    /// <param name="sharedUnprotectedHeader">The shared unprotected header, or <c>null</c>.</param>
    /// <param name="recipientHeaders">Recipient headers in recipient order.</param>
    /// <param name="keys">Recipient or key-encryption keys in recipient order.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>A result containing encrypted general JWE JSON.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static TokenTextResult Encrypt(
        byte[] plaintext,
        JoseHeader protectedHeader,
        JoseHeader? sharedUnprotectedHeader,
        JoseHeader?[] recipientHeaders,
        TokenKey[] keys,
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
            recipientHeaders,
            keys,
            policy,
            null,
            false);
    }

    /// <summary>Encrypts shared plaintext with external Additional Authenticated Data for multiple recipients.</summary>
    /// <param name="plaintext">The shared plaintext bytes.</param>
    /// <param name="additionalAuthenticatedData">The external authenticated data bytes.</param>
    /// <param name="protectedHeader">The integrity-protected header.</param>
    /// <param name="sharedUnprotectedHeader">The shared unprotected header, or <c>null</c>.</param>
    /// <param name="recipientHeaders">Recipient headers in recipient order.</param>
    /// <param name="keys">Recipient or key-encryption keys in recipient order.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>A result containing encrypted general JWE JSON with an <c>aad</c> member.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static TokenTextResult Encrypt(
        byte[] plaintext,
        byte[] additionalAuthenticatedData,
        JoseHeader protectedHeader,
        JoseHeader? sharedUnprotectedHeader,
        JoseHeader?[] recipientHeaders,
        TokenKey[] keys,
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
            recipientHeaders,
            keys,
            policy,
            additionalAuthenticatedData,
            true);
    }

    static TokenTextResult EncryptCore(
        byte[] plaintext,
        JoseHeader protectedHeader,
        JoseHeader? sharedUnprotectedHeader,
        JoseHeader?[] recipientHeaders,
        TokenKey[] keys,
        TokenPolicy policy,
        byte[]? additionalAuthenticatedData,
        bool additionalAuthenticatedDataPresent)
    {

        if (protectedHeader == null)
        {
            throw new ArgumentNullException("protectedHeader");
        }

        if (keys == null)
        {
            throw new ArgumentNullException("keys");
        }

        ValidateArrays(recipientHeaders, keys, policy);
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

        JoseHeaderResult protectedBounded =
            JsonWebEncryptionFlattened.BoundHeader(protectedHeader, policy);
        if (!protectedBounded.Success)
        {
            return TextFailure(protectedBounded.Error);
        }

        JoseHeaderResult sharedBounded =
            JsonWebEncryptionFlattened.BoundOptionalHeader(
                sharedUnprotectedHeader,
                policy);
        if (!sharedBounded.Success)
        {
            return TextFailure(sharedBounded.Error);
        }

        JoseHeader?[] finalHeaders = new JoseHeader?[recipientHeaders.Length];
        byte[][] encryptedKeys = new byte[recipientHeaders.Length][];
        JweKeyManagementResult? firstManaged = null;
        byte[]? contentEncryptionKey = null;
        string? encryptionAlgorithm = null;
        string? compressionAlgorithm = null;
        int index = 0;
        while (index < recipientHeaders.Length)
        {
            if (keys[index] == null)
            {
                Clear(contentEncryptionKey);
                return TextFailure("invalid_key", "A recipient key is missing.");
            }

            JoseHeaderResult recipientBounded =
                JsonWebEncryptionFlattened.BoundOptionalHeader(
                    recipientHeaders[index],
                    policy);
            if (!recipientBounded.Success)
            {
                Clear(contentEncryptionKey);
                return TextFailure(recipientBounded.Error);
            }

            JoseHeaderResult effective =
                JsonWebEncryptionFlattened.ValidateAndMergeHeaders(
                    protectedBounded.Header,
                    sharedBounded.Header,
                    recipientBounded.Header,
                    policy);
            if (!effective.Success)
            {
                Clear(contentEncryptionKey);
                return TextFailure(effective.Error);
            }

            JsonValue? algorithmValue = (effective.Header ?? throw new InvalidOperationException("A required value is null.")).Get(
                JoseHeaderNames.Algorithm);
            JsonValue? encryptionValue = effective.Header.Get(
                JoseHeaderNames.EncryptionAlgorithm);
            TokenResult algorithms =
                JsonWebEncryption.ValidateEncryptionAlgorithms(
                    algorithmValue,
                    encryptionValue,
                    policy);
            if (!algorithms.Success)
            {
                Clear(contentEncryptionKey);
                return TextFailure(algorithms.Error);
            }

            string? currentEncryption = (encryptionValue ?? throw new InvalidOperationException("A required value is null.")).StringValue;
            JsonValue? compressionValue = effective.Header.Get(
                JoseHeaderNames.CompressionAlgorithm);
            string? currentCompression = compressionValue == null
                ? null
                : compressionValue.StringValue;
            if (index == 0)
            {
                encryptionAlgorithm = currentEncryption;
                compressionAlgorithm = currentCompression;
            }
            else if (!string.Equals(
                    encryptionAlgorithm,
                    currentEncryption,
                    StringComparison.Ordinal)
                || !string.Equals(
                    compressionAlgorithm,
                    currentCompression,
                    StringComparison.Ordinal))
            {
                Clear(contentEncryptionKey);
                return TextFailure(
                    "inconsistent_content_encryption",
                    "All JWE recipients must use the same enc and zip values.");
            }

            string? algorithm = (algorithmValue ?? throw new InvalidOperationException("A required value is null.")).StringValue;
            JweKeyManagementResult managed;
            if (recipientHeaders.Length == 1)
            {
                managed = JsonWebEncryption.ManageEncryptionKey(
                    effective.Header,
                    keys[index],
                    algorithm,
                    currentEncryption,
                    policy);
                firstManaged = managed;
                if (managed.Success)
                {
                    contentEncryptionKey = managed.ContentEncryptionKey;
                }
            }
            else
            {
                if (contentEncryptionKey == null)
                {
                    contentEncryptionKey = RandomBytes.Generate(
                        JsonWebEncryption.GetContentEncryptionKeySize(
                            currentEncryption));
                }

                managed = JsonWebEncryption.WrapEncryptionKey(
                    effective.Header,
                    contentEncryptionKey,
                    keys[index],
                    algorithm,
                    currentEncryption,
                    policy);
            }

            if (!managed.Success)
            {
                Clear(contentEncryptionKey);
                return TextFailure(managed.Error);
            }

            finalHeaders[index] =
                JsonWebEncryptionFlattened.CopyGeneratedParameters(
                    effective.Header,
                    recipientBounded.Header,
                    algorithm);
            encryptedKeys[index] = (managed.EncryptedKey ?? throw new InvalidOperationException("A required value is null."));
            if (recipientHeaders.Length > 1)
            {
                JsonWebEncryption.ClearManaged(managed);
            }

            index += 1;
        }

        byte[]? content = plaintext.AsSpan().ToArray();
        if (compressionAlgorithm != null)
        {
            TokenBytesResult compressed = JoseCompression.Compress(
                content,
                compressionAlgorithm,
                policy);
            CryptographicOperations.ZeroMemory(content);
            if (!compressed.Success)
            {
                ClearManagedAndKey(firstManaged, contentEncryptionKey);
                return TextFailure(compressed.Error);
            }

            content = compressed.Bytes;
        }

        TokenBytesResult protectedBytes = (protectedBounded.Header ?? throw new InvalidOperationException("A required value is null.")).WriteUtf8();
        if (!protectedBytes.Success)
        {
            ClearManagedAndKey(firstManaged, contentEncryptionKey);
            CryptographicOperations.ZeroMemory(content);
            return TextFailure(protectedBytes.Error);
        }

        string? protectedSegment = Base64Url.Encode((protectedBytes.Bytes ?? throw new InvalidOperationException("A required value is null."))).Text;
        TokenBytesResult authenticatedData =
            JsonWebEncryptionFlattened.CreateAuthenticatedData(
                protectedSegment,
                additionalAuthenticatedData,
                additionalAuthenticatedDataPresent);
        if (!authenticatedData.Success)
        {
            ClearManagedAndKey(firstManaged, contentEncryptionKey);
            CryptographicOperations.ZeroMemory(content);
            return TextFailure(authenticatedData.Error);
        }

        TokenKey contentKey = JsonWebEncryptionFlattened.ContentKey(
            contentEncryptionKey,
            encryptionAlgorithm,
            TokenKeyOperations.Encrypt);
        JweFlattenedContentResult encrypted =
            JsonWebEncryptionFlattened.EncryptContent(
                content,
                authenticatedData.Bytes,
                contentKey,
                encryptionAlgorithm);
        contentKey.SymmetricKey = null;
        ClearManagedAndKey(firstManaged, contentEncryptionKey);
        CryptographicOperations.ZeroMemory(content);
        CryptographicOperations.ZeroMemory(authenticatedData.Bytes);
        if (!encrypted.Success)
        {
            return TextFailure(encrypted.Error);
        }

        TokenTextResult result = WriteCore(
            encrypted.Ciphertext,
            protectedBounded.Header,
            sharedBounded.Header,
            finalHeaders,
            encryptedKeys,
            encrypted.InitializationVector,
            encrypted.AuthenticationTag,
            policy,
            additionalAuthenticatedData,
            additionalAuthenticatedDataPresent);
        CryptographicOperations.ZeroMemory(encrypted.Ciphertext);
        return result;
    }

    /// <summary>Decrypts one selected recipient entry and authenticates the shared ciphertext.</summary>
    /// <param name="json">The general JWE JSON text.</param>
    /// <param name="recipientIndex">The zero-based recipient index.</param>
    /// <param name="key">The selected recipient or key-encryption key.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>A result containing authenticated plaintext or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static JweGeneralResult Decrypt(
        string json,
        int recipientIndex,
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

        JweGeneralResult parsed = ReadCore(json, policy, false);
        if (!parsed.Success)
        {
            return parsed;
        }

        if (recipientIndex < 0
            || recipientIndex >= (parsed.Token ?? throw new InvalidOperationException("A required value is null.")).Recipients.Length)
        {
            return Failure(
                "recipient_index_out_of_range",
                "The selected JWE recipient index is outside the recipients array.");
        }

        JweFlattenedResult decrypted = JsonWebEncryptionFlattened.Decrypt(
            (parsed.Token.Recipients[recipientIndex].FlattenedJson ?? throw new InvalidOperationException("A required value is null.")),
            key,
            policy);
        if (!decrypted.Success)
        {
            return Failure(decrypted.Error);
        }

        parsed.Token.Plaintext = (decrypted.Token ?? throw new InvalidOperationException("A required value is null.")).Plaintext;
        parsed.Token.PlaintextAvailable = true;
        parsed.Token.AuthenticationTagVerified = true;
        parsed.Token.Recipients[recipientIndex].Decrypted = true;
        return parsed;
    }

    /// <summary>Decrypts and authenticates every recipient entry with matching keys.</summary>
    /// <param name="json">The general JWE JSON text.</param>
    /// <param name="keys">Keys corresponding to recipient order.</param>
    /// <param name="policy">The algorithm allowlist and safety policy.</param>
    /// <returns>A result containing plaintext only when every recipient succeeds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JweGeneralResult Decrypt(
        string json,
        TokenKey[] keys,
        TokenPolicy policy)
    {
        if (json == null)
        {
            throw new ArgumentNullException("json");
        }

        if (keys == null)
        {
            throw new ArgumentNullException("keys");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        JweGeneralResult parsed = ReadCore(json, policy, false);
        if (!parsed.Success)
        {
            return parsed;
        }

        if (keys.Length != (parsed.Token ?? throw new InvalidOperationException("A required value is null.")).Recipients.Length)
        {
            return Failure(
                "recipient_count_mismatch",
                "The key count must match the JWE recipient count.");
        }

        byte[]? plaintext = null;
        int index = 0;
        while (index < keys.Length)
        {
            if (keys[index] == null)
            {
                Clear(plaintext);
                return Failure("invalid_key", "A recipient key is missing.");
            }

            JweFlattenedResult decrypted = JsonWebEncryptionFlattened.Decrypt(
                (parsed.Token.Recipients[index].FlattenedJson ?? throw new InvalidOperationException("A required value is null.")),
                keys[index],
                policy);
            if (!decrypted.Success)
            {
                Clear(plaintext);
                return Failure(decrypted.Error);
            }

            if (plaintext == null)
            {
                plaintext = (decrypted.Token ?? throw new InvalidOperationException("A required value is null.")).Plaintext;
            }
            else
            {
                CryptographicOperations.ZeroMemory((decrypted.Token ?? throw new InvalidOperationException("A required value is null.")).Plaintext);
            }

            index += 1;
        }

        parsed.Token.Plaintext = plaintext;
        parsed.Token.PlaintextAvailable = true;
        parsed.Token.AuthenticationTagVerified = true;
        index = 0;
        while (index < parsed.Token.Recipients.Length)
        {
            parsed.Token.Recipients[index].Decrypted = true;
            index += 1;
        }

        return parsed;
    }

    static JweGeneralResult ReadCore(
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

        TokenBytesResult encoded = Utf8Codec.Encode(json);
        if (!encoded.Success)
        {
            return Failure(encoded.Error);
        }

        if ((encoded.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length > policy.MaximumTokenSizeInBytes)
        {
            return Failure(
                "token_too_large",
                "The general JWE exceeds the configured token size limit.");
        }

        JsonObjectResult root = JsonObject.Read(json, policy.MaximumJsonDepth);
        if (!root.Success)
        {
            return Failure(root.Error);
        }

        TokenResult shape = ValidateRootShape(root.Object);
        if (!shape.Success)
        {
            return Failure(shape.Error);
        }

        JsonValue? recipientsValue = (root.Object ?? throw new InvalidOperationException("A required value is null.")).Get("recipients");
        if (recipientsValue == null
            || recipientsValue.Kind != TokenJsonValueKind.Array
            || recipientsValue.ArrayValue == null
            || recipientsValue.ArrayValue.Length == 0)
        {
            return Failure(
                "invalid_recipients",
                "A general JWE requires a nonempty recipients array.");
        }

        JweGeneralRecipient[] recipients = new JweGeneralRecipient[
            recipientsValue.ArrayValue.Length];
        JweFlattenedToken? first = null;
        int index = 0;
        while (index < recipients.Length)
        {
            JsonValue recipientValue = recipientsValue.ArrayValue[index];
            if (recipientValue == null
                || recipientValue.Kind != TokenJsonValueKind.Object
                || recipientValue.ObjectValue == null)
            {
                return Failure(
                    "invalid_recipient_entry",
                    "Each general JWE recipient entry must be an object.");
            }

            TokenResult recipientShape = ValidateRecipientShape(
                recipientValue.ObjectValue);
            if (!recipientShape.Success)
            {
                return Failure(recipientShape.Error);
            }

            TokenTextResult flattened = CreateFlattenedJson(
                root.Object,
                recipientValue.ObjectValue);
            if (!flattened.Success)
            {
                return Failure(flattened.Error);
            }

            JweFlattenedResult parsed = JsonWebEncryptionFlattened.Parse(
                (flattened.Text ?? throw new InvalidOperationException("A required value is null.")),
                policy);
            if (!parsed.Success)
            {
                return Failure(parsed.Error);
            }

            if (index == 0)
            {
                first = parsed.Token;
            }
            else if (!string.Equals(
                    (first ?? throw new InvalidOperationException("A required value is null.")).EncryptionAlgorithm,
                    (parsed.Token ?? throw new InvalidOperationException("A required value is null.")).EncryptionAlgorithm,
                    StringComparison.Ordinal)
                || !string.Equals(
                    first.CompressionAlgorithm,
                    parsed.Token.CompressionAlgorithm,
                    StringComparison.Ordinal))
            {
                return Failure(
                    "inconsistent_content_encryption",
                    "All JWE recipients must use the same enc and zip values.");
            }

            recipients[index] = FromFlattened(parsed.Token, flattened.Text);
            index += 1;
        }

        JweGeneralToken token = new JweGeneralToken();
        token.ProtectedHeader = (first ?? throw new InvalidOperationException("A required value is null.")).ProtectedHeader;
        token.SharedUnprotectedHeader = first.SharedUnprotectedHeader;
        token.EncryptionAlgorithm = first.EncryptionAlgorithm;
        token.EncryptionAlgorithmMetadata = first.EncryptionAlgorithmMetadata;
        token.EncryptionAlgorithmAvailable = first.EncryptionAlgorithmAvailable;
        token.CompressionAlgorithm = first.CompressionAlgorithm;
        token.CompressionAlgorithmKnown = first.CompressionAlgorithmKnown;
        token.CompressionAlgorithmAvailable = first.CompressionAlgorithmAvailable;
        token.Recipients = recipients;
        token.InitializationVector = first.InitializationVector;
        token.AuthenticationTag = first.AuthenticationTag;
        token.AdditionalAuthenticatedData = first.AdditionalAuthenticatedData;
        token.AdditionalAuthenticatedDataPresent =
            first.AdditionalAuthenticatedDataPresent;
        token.ParsedCiphertext = first.ParsedCiphertext;
        if (diagnostic)
        {
            token.Ciphertext = first.ParsedCiphertext.AsSpan().ToArray();
            token.CiphertextAvailable = true;
        }

        JweGeneralResult result = new JweGeneralResult();
        result.Token = token;
        result.Success = true;
        return result;
    }

    static TokenTextResult CombineFlattened(
        TokenTextResult[] flattened,
        TokenPolicy policy)
    {
        JsonObject[] objects = new JsonObject[flattened.Length];
        JweFlattenedToken? first = null;
        int index = 0;
        while (index < flattened.Length)
        {
            JsonObjectResult parsedObject = JsonObject.Read(
                (flattened[index].Text ?? throw new InvalidOperationException("A required value is null.")),
                policy.MaximumJsonDepth);
            if (!parsedObject.Success)
            {
                return TextFailure(parsedObject.Error);
            }

            objects[index] = (parsedObject.Object ?? throw new InvalidOperationException("A required value is null."));
            JweFlattenedResult parsed = JsonWebEncryptionFlattened.Parse(
                (flattened[index].Text ?? throw new InvalidOperationException("A required value is null.")),
                policy);
            if (!parsed.Success)
            {
                return TextFailure(parsed.Error);
            }

            if (index == 0)
            {
                first = parsed.Token;
            }
            else if (!string.Equals(
                    (first ?? throw new InvalidOperationException("A required value is null.")).EncryptionAlgorithm,
                    (parsed.Token ?? throw new InvalidOperationException("A required value is null.")).EncryptionAlgorithm,
                    StringComparison.Ordinal)
                || !string.Equals(
                    first.CompressionAlgorithm,
                    parsed.Token.CompressionAlgorithm,
                    StringComparison.Ordinal)
                || first.AdditionalAuthenticatedDataPresent
                    != parsed.Token.AdditionalAuthenticatedDataPresent
                || !string.Equals(
                    first.AdditionalAuthenticatedDataSegment,
                    parsed.Token.AdditionalAuthenticatedDataSegment,
                    StringComparison.Ordinal))
            {
                return TextFailure(
                    "inconsistent_content_encryption",
                    "All JWE recipients must use the same enc and zip values.");
            }

            index += 1;
        }

        JsonObject firstObject = objects[0];
        StringBuilder builder = new StringBuilder();
        builder.Append('{');
        AppendStringMember(
            builder,
            "protected",
            (firstObject.Get("protected") ?? throw new InvalidOperationException("A required value is null.")).StringValue,
            false);
        JsonValue? shared = firstObject.Get("unprotected");
        if (shared != null)
        {
            builder.Append(",\"unprotected\":");
            builder.Append(JoseHeader.Create((shared.ObjectValue ?? throw new InvalidOperationException("A required value is null."))).Write().Text);
        }

        JsonValue? additionalAuthenticatedData = firstObject.Get("aad");
        if (additionalAuthenticatedData != null)
        {
            AppendStringMember(
                builder,
                "aad",
                additionalAuthenticatedData.StringValue,
                true);
        }

        builder.Append(",\"recipients\":[");
        index = 0;
        while (index < objects.Length)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            AppendRecipient(builder, objects[index]);
            index += 1;
        }

        builder.Append(']');
        AppendStringMember(
            builder,
            "iv",
            (firstObject.Get("iv") ?? throw new InvalidOperationException("A required value is null.")).StringValue,
            true);
        AppendStringMember(
            builder,
            "ciphertext",
            (firstObject.Get("ciphertext") ?? throw new InvalidOperationException("A required value is null.")).StringValue,
            true);
        AppendStringMember(
            builder,
            "tag",
            (firstObject.Get("tag") ?? throw new InvalidOperationException("A required value is null.")).StringValue,
            true);
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
                "The general JWE exceeds the configured token size limit.");
        }

        TokenTextResult result = new TokenTextResult();
        result.Text = json;
        result.Success = true;
        return result;
    }

    static TokenTextResult CreateFlattenedJson(
        JsonObject root,
        JsonObject recipient)
    {
        JsonValue? protectedValue = RequiredString(root, "protected");
        JsonValue? initializationVector = RequiredString(root, "iv");
        JsonValue? ciphertext = RequiredString(root, "ciphertext");
        JsonValue? authenticationTag = RequiredString(root, "tag");
        if (protectedValue == null
            || initializationVector == null
            || ciphertext == null
            || authenticationTag == null)
        {
            return TextFailure(
                "invalid_jwe_json",
                "A general JWE requires protected, iv, ciphertext, and tag strings.");
        }

        StringBuilder builder = new StringBuilder();
        builder.Append('{');
        AppendStringMember(
            builder,
            "protected",
            protectedValue.StringValue,
            false);
        JsonValue? shared = root.Get("unprotected");
        if (shared != null)
        {
            if (shared.Kind != TokenJsonValueKind.Object
                || shared.ObjectValue == null)
            {
                return TextFailure(
                    "invalid_unprotected_header",
                    "The shared unprotected header must be an object.");
            }

            builder.Append(",\"unprotected\":");
            builder.Append(JoseHeader.Create(shared.ObjectValue).Write().Text);
        }

        JsonValue? additionalAuthenticatedData = root.Get("aad");
        if (additionalAuthenticatedData != null)
        {
            if (additionalAuthenticatedData.Kind != TokenJsonValueKind.String
                || additionalAuthenticatedData.StringValue == null)
            {
                return TextFailure(
                    "invalid_additional_authenticated_data",
                    "The JWE aad member must be a base64url string.");
            }

            AppendStringMember(
                builder,
                "aad",
                additionalAuthenticatedData.StringValue,
                true);
        }

        JsonValue? header = recipient.Get("header");
        if (header != null)
        {
            if (header.Kind != TokenJsonValueKind.Object
                || header.ObjectValue == null)
            {
                return TextFailure(
                    "invalid_recipient_header",
                    "A recipient header must be an object.");
            }

            builder.Append(",\"header\":");
            builder.Append(JoseHeader.Create(header.ObjectValue).Write().Text);
        }

        JsonValue? encryptedKey = recipient.Get("encrypted_key");
        if (encryptedKey != null)
        {
            if (encryptedKey.Kind != TokenJsonValueKind.String
                || encryptedKey.StringValue == null)
            {
                return TextFailure(
                    "invalid_encrypted_key",
                    "A recipient encrypted_key member must be a string.");
            }

            AppendStringMember(
                builder,
                "encrypted_key",
                encryptedKey.StringValue,
                true);
        }

        AppendStringMember(
            builder,
            "iv",
            initializationVector.StringValue,
            true);
        AppendStringMember(
            builder,
            "ciphertext",
            ciphertext.StringValue,
            true);
        AppendStringMember(
            builder,
            "tag",
            authenticationTag.StringValue,
            true);
        builder.Append('}');
        TokenTextResult result = new TokenTextResult();
        result.Text = builder.ToString();
        result.Success = true;
        return result;
    }

    static JweGeneralRecipient FromFlattened(
        JweFlattenedToken? token,
        string flattenedJson)
    {
        JweGeneralRecipient result = new JweGeneralRecipient();
        result.Header = (token ?? throw new InvalidOperationException("A required value is null.")).RecipientHeader;
        result.Algorithm = token.Algorithm;
        result.AlgorithmMetadata = token.AlgorithmMetadata;
        result.AlgorithmAvailable = token.AlgorithmAvailable;
        result.KeyId = token.KeyId;
        result.EncryptedKey = token.EncryptedKey;
        result.FlattenedJson = flattenedJson;
        return result;
    }

    static TokenResult ValidateRootShape(JsonObject? root)
    {
        int index = 0;
        while (index < (root ?? throw new InvalidOperationException("A required value is null.")).Members.Length)
        {
            string? name = root.Members[index].Name;
            if (!string.Equals(name, "protected", StringComparison.Ordinal)
                && !string.Equals(name, "unprotected", StringComparison.Ordinal)
                && !string.Equals(name, "aad", StringComparison.Ordinal)
                && !string.Equals(name, "recipients", StringComparison.Ordinal)
                && !string.Equals(name, "iv", StringComparison.Ordinal)
                && !string.Equals(name, "ciphertext", StringComparison.Ordinal)
                && !string.Equals(name, "tag", StringComparison.Ordinal))
            {
                return ResultFailure(
                    "invalid_jwe_json_member",
                    "The general JWE contains an unexpected member.");
            }

            index += 1;
        }

        return Success();
    }

    static TokenResult ValidateRecipientShape(JsonObject recipient)
    {
        int index = 0;
        while (index < recipient.Members.Length)
        {
            string? name = recipient.Members[index].Name;
            if (!string.Equals(name, "header", StringComparison.Ordinal)
                && !string.Equals(
                    name,
                    "encrypted_key",
                    StringComparison.Ordinal))
            {
                return ResultFailure(
                    "invalid_recipient_entry",
                    "A general JWE recipient contains an unexpected member.");
            }

            index += 1;
        }

        return Success();
    }

    static JsonValue? RequiredString(JsonObject root, string name)
    {
        JsonValue? value = root.Get(name);
        if (value == null
            || value.Kind != TokenJsonValueKind.String
            || value.StringValue == null)
        {
            return null;
        }

        return value;
    }

    static void AppendRecipient(StringBuilder builder, JsonObject value)
    {
        builder.Append('{');
        bool comma = false;
        JsonValue? header = value.Get("header");
        if (header != null)
        {
            builder.Append("\"header\":");
            builder.Append(JoseHeader.Create((header.ObjectValue ?? throw new InvalidOperationException("A required value is null."))).Write().Text);
            comma = true;
        }

        JsonValue? encryptedKey = value.Get("encrypted_key");
        if (encryptedKey != null)
        {
            if (comma)
            {
                builder.Append(',');
            }

            builder.Append("\"encrypted_key\":");
            builder.Append(JsonWriter.WriteStringValue(
                encryptedKey.StringValue).Text);
        }

        builder.Append('}');
    }

    static void AppendStringMember(
        StringBuilder builder,
        string name,
        string? value,
        bool comma)
    {
        if (comma)
        {
            builder.Append(',');
        }

        builder.Append('"');
        builder.Append(name);
        builder.Append("\":");
        builder.Append(JsonWriter.WriteStringValue(value).Text);
    }

    static void ValidateArrays(
        JoseHeader?[] recipientHeaders,
        byte[][] encryptedKeys,
        TokenPolicy policy)
    {
        if (recipientHeaders == null)
        {
            throw new ArgumentNullException("recipientHeaders");
        }

        if (encryptedKeys == null)
        {
            throw new ArgumentNullException("encryptedKeys");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        if (recipientHeaders.Length == 0
            || recipientHeaders.Length != encryptedKeys.Length)
        {
            throw new ArgumentException(
                "Recipient header and encrypted-key arrays must have equal nonzero lengths.",
                "encryptedKeys");
        }
    }

    static void ValidateArrays(
        JoseHeader?[] recipientHeaders,
        TokenKey[] keys,
        TokenPolicy policy)
    {
        if (recipientHeaders == null)
        {
            throw new ArgumentNullException("recipientHeaders");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        if (recipientHeaders.Length == 0
            || recipientHeaders.Length != keys.Length)
        {
            throw new ArgumentException(
                "Recipient header and key arrays must have equal nonzero lengths.",
                "keys");
        }
    }

    static void ClearManagedAndKey(
        JweKeyManagementResult? managed,
        byte[]? contentEncryptionKey)
    {
        if (managed != null)
        {
            JsonWebEncryption.ClearManaged(managed);
            return;
        }

        Clear(contentEncryptionKey);
    }

    static void Clear(byte[]? bytes)
    {
        if (bytes != null)
        {
            CryptographicOperations.ZeroMemory(bytes);
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

    static JweGeneralResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static JweGeneralResult Failure(TokenError? error)
    {
        JweGeneralResult result = new JweGeneralResult();
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
