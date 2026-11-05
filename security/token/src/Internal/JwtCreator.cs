using System;
namespace token;

/// <summary>
/// Creates compact JSON Web Tokens from claim sets.
/// </summary>
internal static class JwtCreator
{
    /// <summary>
    /// Creates a signed compact JWT and encrypts that complete token as a compact JWE.
    /// </summary>
    /// <param name="claims">The claim set to sign and encrypt.</param>
    /// <param name="signingKey">The key used for the inner JWS.</param>
    /// <param name="encryptionKey">The key used for the outer JWE.</param>
    /// <param name="contentEncryptionAlgorithm">The outer JWE content-encryption algorithm.</param>
    /// <param name="policy">The algorithm allowlist and safety limits for both layers.</param>
    /// <returns>A result containing a nested signed-then-encrypted JWT or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenTextResult CreateSignedThenEncrypted(
        JwtClaims claims,
        TokenKey signingKey,
        TokenKey encryptionKey,
        string contentEncryptionAlgorithm,
        TokenPolicy policy)
    {
        if (encryptionKey == null)
        {
            throw new ArgumentNullException("encryptionKey");
        }

        if (contentEncryptionAlgorithm == null)
        {
            throw new ArgumentNullException("contentEncryptionAlgorithm");
        }

        JoseHeader encryptionHeader = new JoseHeader();
        AddStringDefault(
            encryptionHeader,
            JoseHeaderNames.Algorithm,
            encryptionKey.Algorithm);

        AddStringDefault(
            encryptionHeader,
            JoseHeaderNames.EncryptionAlgorithm,
            contentEncryptionAlgorithm);
        return CreateSignedThenEncrypted(
            claims,
            signingKey,
            encryptionKey,
            new JoseHeader(),
            encryptionHeader,
            policy);
    }

    /// <summary>
    /// Creates a signed compact JWT and encrypts it using caller-supplied headers for both layers.
    /// </summary>
    /// <param name="claims">The claim set to sign and encrypt.</param>
    /// <param name="signingKey">The key used for the inner JWS.</param>
    /// <param name="encryptionKey">The key used for the outer JWE.</param>
    /// <param name="signingHeader">The protected header to copy for the inner JWS.</param>
    /// <param name="encryptionHeader">The protected header to copy for the outer JWE.</param>
    /// <param name="policy">The algorithm allowlist and safety limits for both layers.</param>
    /// <returns>A result containing a nested signed-then-encrypted JWT or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenTextResult CreateSignedThenEncrypted(
        JwtClaims claims,
        TokenKey signingKey,
        TokenKey encryptionKey,
        JoseHeader signingHeader,
        JoseHeader encryptionHeader,
        TokenPolicy policy)
    {
        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        if (signingKey == null)
        {
            throw new ArgumentNullException("signingKey");
        }

        if (encryptionKey == null)
        {
            throw new ArgumentNullException("encryptionKey");
        }

        if (signingHeader == null)
        {
            throw new ArgumentNullException("signingHeader");
        }

        if (encryptionHeader == null)
        {
            throw new ArgumentNullException("encryptionHeader");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        JoseHeaderResult outerCopy = CopyHeader(encryptionHeader);
        if (!outerCopy.Success)
        {
            return Failure(outerCopy.Error);
        }

        JoseHeader? outer = outerCopy.Header;
        JsonValue? contentType = (outer ?? throw new InvalidOperationException("A required value is null.")).Get(JoseHeaderNames.ContentType);
        if (contentType != null
            && !string.Equals(
                JoseMediaTypesOperations.Normalize((contentType.StringValue ?? throw new InvalidOperationException("A required value is null."))),
                JoseMediaTypes.Jwt,
                StringComparison.Ordinal))
        {
            return Failure(
                "invalid_nested_content_type",
                "A signed-then-encrypted JWT requires an outer cty value of JWT.");
        }

        AddStringDefault(outer, JoseHeaderNames.ContentType, "JWT");
        AddStringDefault(
            outer,
            JoseHeaderNames.Algorithm,
            encryptionKey.Algorithm);
        AddStringDefault(outer, JoseHeaderNames.Type, "JWT");
        AddStringDefault(
            outer,
            JoseHeaderNames.KeyId,
            encryptionKey.KeyId);

        TokenTextResult signed = CreateSigned(
            claims,
            signingKey,
            signingHeader,
            policy);
        if (!signed.Success)
        {
            return signed;
        }

        TokenBytesResult signedBytes = Utf8Codec.Encode((signed.Text ?? throw new InvalidOperationException("A required value is null.")));
        if (!signedBytes.Success)
        {
            return Failure(signedBytes.Error);
        }

        return JsonWebEncryption.Encrypt(
            (signedBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")),
            outer,
            encryptionKey,
            policy);
    }

    /// <summary>
    /// Creates an encrypted compact JWT using key metadata and a content-encryption algorithm.
    /// </summary>
    /// <param name="claims">The claim set to encrypt.</param>
    /// <param name="encryptionKey">The recipient or key-encryption key.</param>
    /// <param name="contentEncryptionAlgorithm">The JWE content-encryption algorithm.</param>
    /// <param name="policy">The explicit algorithm allowlist and safety limits.</param>
    /// <returns>A result containing compact JWT text or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenTextResult CreateEncrypted(
        JwtClaims claims,
        TokenKey encryptionKey,
        string contentEncryptionAlgorithm,
        TokenPolicy policy)
    {
        if (encryptionKey == null)
        {
            throw new ArgumentNullException("encryptionKey");
        }

        if (contentEncryptionAlgorithm == null)
        {
            throw new ArgumentNullException("contentEncryptionAlgorithm");
        }

        JoseHeader header = new JoseHeader();
        AddStringDefault(
            header,
            JoseHeaderNames.Algorithm,
            encryptionKey.Algorithm);

        AddStringDefault(
            header,
            JoseHeaderNames.EncryptionAlgorithm,
            contentEncryptionAlgorithm);
        return CreateEncrypted(claims, encryptionKey, header, policy);
    }

    /// <summary>
    /// Creates an encrypted compact JWT using caller-supplied protected-header parameters.
    /// </summary>
    /// <param name="claims">The claim set to encrypt.</param>
    /// <param name="encryptionKey">The recipient or key-encryption key.</param>
    /// <param name="protectedHeader">The protected JWE header to copy and complete.</param>
    /// <param name="policy">The explicit algorithm allowlist and safety limits.</param>
    /// <returns>A result containing compact JWT text or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenTextResult CreateEncrypted(
        JwtClaims claims,
        TokenKey encryptionKey,
        JoseHeader protectedHeader,
        TokenPolicy policy)
    {
        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        if (encryptionKey == null)
        {
            throw new ArgumentNullException("encryptionKey");
        }

        if (protectedHeader == null)
        {
            throw new ArgumentNullException("protectedHeader");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenTextResult claimText = claims.WriteWithPolicy(policy);
        if (!claimText.Success)
        {
            return Failure(claimText.Error);
        }

        JoseHeaderResult headerCopy = CopyHeader(protectedHeader);
        if (!headerCopy.Success)
        {
            return Failure(headerCopy.Error);
        }

        JoseHeader? header = headerCopy.Header;
        AddStringDefault(
            header,
            JoseHeaderNames.Algorithm,
            encryptionKey.Algorithm);
        AddStringDefault(header, JoseHeaderNames.Type, "JWT");
        AddStringDefault(
            header,
            JoseHeaderNames.KeyId,
            encryptionKey.KeyId);

        TokenBytesResult claimBytes = Utf8Codec.Encode((claimText.Text ?? throw new InvalidOperationException("A required value is null.")));
        if (!claimBytes.Success)
        {
            return Failure(claimBytes.Error);
        }

        return JsonWebEncryption.Encrypt(
            (claimBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")),
            (header ?? throw new InvalidOperationException("A required value is null.")),
            encryptionKey,
            policy);
    }

    /// <summary>
    /// Creates a signed compact JWT using protected-header defaults from the signing key.
    /// </summary>
    /// <param name="claims">The claim set to sign.</param>
    /// <param name="signingKey">The signing key and its operation metadata.</param>
    /// <param name="policy">The explicit algorithm allowlist and safety limits.</param>
    /// <returns>A result containing compact JWT text or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenTextResult CreateSigned(
        JwtClaims claims,
        TokenKey signingKey,
        TokenPolicy policy)
    {
        return CreateSigned(claims, signingKey, new JoseHeader(), policy);
    }

    /// <summary>
    /// Creates a signed compact JWT using caller-supplied protected-header parameters.
    /// </summary>
    /// <param name="claims">The claim set to sign.</param>
    /// <param name="signingKey">The signing key and its operation metadata.</param>
    /// <param name="protectedHeader">The protected JOSE header to copy and complete.</param>
    /// <param name="policy">The explicit algorithm allowlist and safety limits.</param>
    /// <returns>A result containing compact JWT text or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenTextResult CreateSigned(
        JwtClaims claims,
        TokenKey signingKey,
        JoseHeader protectedHeader,
        TokenPolicy policy)
    {
        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        if (signingKey == null)
        {
            throw new ArgumentNullException("signingKey");
        }

        if (protectedHeader == null)
        {
            throw new ArgumentNullException("protectedHeader");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenTextResult claimText = claims.WriteWithPolicy(policy);
        if (!claimText.Success)
        {
            return Failure(claimText.Error);
        }

        JoseHeaderResult headerCopy = CopyHeader(protectedHeader);
        if (!headerCopy.Success)
        {
            return Failure(headerCopy.Error);
        }

        JoseHeader? header = headerCopy.Header;
        JsonValue? payloadEncoding = (header ?? throw new InvalidOperationException("A required value is null.")).Get(
            JoseHeaderNames.Base64UrlEncodePayload);
        if (payloadEncoding != null && !payloadEncoding.BooleanValue)
        {
            return Failure(
                "unencoded_jwt_payload",
                "A compact JWT claim set must use base64url payload encoding.");
        }

        AddStringDefault(
            header,
            JoseHeaderNames.Algorithm,
            signingKey.Algorithm);
        AddStringDefault(header, JoseHeaderNames.Type, "JWT");
        AddStringDefault(
            header,
            JoseHeaderNames.KeyId,
            signingKey.KeyId);

        TokenBytesResult claimBytes = Utf8Codec.Encode((claimText.Text ?? throw new InvalidOperationException("A required value is null.")));
        if (!claimBytes.Success)
        {
            return Failure(claimBytes.Error);
        }

        return JsonWebSignature.Sign(
            (claimBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")),
            header,
            signingKey,
            policy);
    }

    static JoseHeaderResult CopyHeader(JoseHeader protectedHeader)
    {
        if (protectedHeader.Parameters == null)
        {
            JoseHeaderResult failure = new JoseHeaderResult();
            failure.Error = new TokenError(
                "invalid_header",
                "The JWT protected-header parameter object is missing.",
                -1);
            return failure;
        }

        return JoseHeader.FromObject(protectedHeader.Parameters);
    }

    static void AddStringDefault(
        JoseHeader? header,
        string name,
        string? value)
    {
        if (value == null || (header ?? throw new InvalidOperationException("A required value is null.")).Get(name) != null)
        {
            return;
        }

        JsonValue json = new JsonValue();
        json.Kind = TokenJsonValueKind.String;
        json.StringValue = value;
        header.Set(name, json);
    }

    static TokenTextResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static TokenTextResult Failure(TokenError? error)
    {
        TokenTextResult result = new TokenTextResult();
        result.Error = error;
        return result;
    }
}
