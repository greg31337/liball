using System;
using System.Security.Cryptography;

namespace token;

/// <summary>
/// Authenticates and validates RFC 9449 DPoP proof JWTs.
/// </summary>
internal static class DpopProofValidator
{
    static readonly string[] requiredClaims = new string[]
    {
        JwtClaimNames.JwtId,
        JwtClaimNames.HttpMethod,
        JwtClaimNames.HttpUri,
        JwtClaimNames.IssuedAt
    };

    /// <summary>Validates a DPoP proof against the current HTTP request and token binding.</summary>
    /// <param name="proof">The compact DPoP proof JWT.</param>
    /// <param name="policy">The cryptographic policy and current request context.</param>
    /// <returns>An authenticated proof result or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static DpopProofValidationResult Validate(
        string proof,
        DpopProofValidationPolicy policy)
    {
        if (proof == null)
        {
            throw new ArgumentNullException("proof");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult policyResult = policy.Validate();
        if (!policyResult.Success)
        {
            return Failure(policyResult.Error);
        }

        JwsCompactResult parsed = JsonWebSignature.Parse(
            proof,
            (policy.JwtPolicy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")));
        if (!parsed.Success)
        {
            return Failure(parsed.Error);
        }

        TokenResult type = ValidateType((parsed.Token ?? throw new InvalidOperationException("A required value is null.")).ProtectedHeader);
        if (!type.Success)
        {
            return Failure(type.Error);
        }

        if (!AsymmetricAlgorithm(parsed.Token.Algorithm))
        {
            return Failure(
                "invalid_dpop_algorithm",
                "A DPoP proof requires a registered asymmetric JWS algorithm.");
        }

        JwkResult jwk = ReadPublicJwk(parsed.Token.ProtectedHeader);
        if (!jwk.Success)
        {
            return Failure(jwk.Error);
        }

        TokenBytesResult thumbprint = JsonWebKey.Thumbprint((jwk.Key ?? throw new InvalidOperationException("A required value is null.")));
        if (!thumbprint.Success)
        {
            jwk.Key.ReleaseOwnedMaterial();
            return Failure(thumbprint.Error);
        }

        TokenTextResult thumbprintText = Base64Url.Encode((thumbprint.Bytes ?? throw new InvalidOperationException("A required value is null.")));
        if (!thumbprintText.Success)
        {
            jwk.Key.ReleaseOwnedMaterial();
            return Failure(thumbprintText.Error);
        }

        JwtValidationPolicy profile = ProfilePolicy(policy);
        TokenKeyResolver resolver = new TokenKeyResolver(
            context => KeySuccess(jwk.Key));
        JwtValidationResult validated = JwtValidator.ValidateSigned(
            proof,
            resolver,
            profile);
        if (!validated.Success)
        {
            jwk.Key.ReleaseOwnedMaterial();
            return Failure(validated.Error);
        }

        TokenResult binding = ValidateBinding(
            validated,
            thumbprint.Bytes,
            policy);
        if (!binding.Success)
        {
            jwk.Key.ReleaseOwnedMaterial();
            return Failure(binding.Error);
        }

        TokenResult replay = ValidateReplay(validated.Claims, policy);
        if (!replay.Success)
        {
            jwk.Key.ReleaseOwnedMaterial();
            return Failure(replay.Error);
        }

        DpopProofValidationResult result = new DpopProofValidationResult();
        result.Jwt = validated;
        result.PublicKey = jwk.Key;
        result.JwkThumbprint = thumbprintText.Text;
        result.Success = true;
        return result;
    }

    static JwtValidationPolicy ProfilePolicy(DpopProofValidationPolicy policy)
    {
        JwtValidationPolicy result = policy.JwtPolicy.Copy();
        result.ValidateIssuer = false;
        result.ValidateAudience = false;
        result.ValidateExpirationTime = false;
        result.ValidateNotBefore = false;
        result.ValidateIssuedAt = true;
        result.RequireExpirationTime = false;
        result.ValidateTokenType = false;
        result.AllowSignedTokens = true;
        result.AllowEncryptedTokens = false;
        result.AllowNestedTokens = false;
        result.MaximumTokenAgeSeconds = policy.MaximumProofAgeSeconds;
        (result.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).RequiredClaims = MergeRequiredClaims(
            (policy.JwtPolicy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).RequiredClaims);
        result.ReplayValidator = null;
        return result;
    }

    static TokenResult ValidateType(JoseHeader? header)
    {
        JsonValue? value = (header ?? throw new InvalidOperationException("A required value is null.")).Get(JoseHeaderNames.Type);
        if (value == null)
        {
            return TokenFailure(
                "missing_dpop_type",
                "A DPoP proof requires the protected typ parameter.");
        }

        if (value.Kind != TokenJsonValueKind.String
            || !string.Equals(
                value.StringValue,
                "dpop+jwt",
                StringComparison.Ordinal))
        {
            return TokenFailure(
                "invalid_dpop_type",
                "The DPoP proof protected typ parameter must be dpop+jwt.");
        }

        return TokenSuccess();
    }

    static bool AsymmetricAlgorithm(string? algorithm)
    {
        return algorithm != null
            && JoseAlgorithmsOperations.IsRegistered(algorithm)
            && JoseAlgorithmsOperations.IsSignature(algorithm)
            && !string.Equals(
                algorithm,
                JoseAlgorithms.None,
                StringComparison.Ordinal)
            && !string.Equals(
                algorithm,
                JoseAlgorithms.HmacSha256,
                StringComparison.Ordinal)
            && !string.Equals(
                algorithm,
                JoseAlgorithms.HmacSha384,
                StringComparison.Ordinal)
            && !string.Equals(
                algorithm,
                JoseAlgorithms.HmacSha512,
                StringComparison.Ordinal);
    }

    static JwkResult ReadPublicJwk(JoseHeader? header)
    {
        JsonValue? value = (header ?? throw new InvalidOperationException("A required value is null.")).Get(JoseHeaderNames.Jwk);
        if (value == null)
        {
            return JwkFailure(
                "missing_dpop_jwk",
                "A DPoP proof requires an embedded public jwk parameter.");
        }

        if (value.Kind != TokenJsonValueKind.Object
            || value.ObjectValue == null)
        {
            return JwkFailure(
                "invalid_dpop_jwk",
                "The DPoP proof jwk parameter must be a public JWK object.");
        }

        JwkResult parsed = JsonWebKey.ReadObject(value.ObjectValue);
        if (!parsed.Success)
        {
            return JwkFailure(
                "invalid_dpop_jwk",
                "The DPoP proof jwk parameter is invalid.");
        }

        if ((parsed.Key ?? throw new InvalidOperationException("A required value is null.")).ContainsPrivateMaterial()
            || !parsed.Key.ContainsPublicMaterial()
            || string.Equals(
                parsed.Key.KeyType,
                TokenKeyTypes.Symmetric,
                StringComparison.Ordinal))
        {
            parsed.Key.ReleaseOwnedMaterial();
            return JwkFailure(
                "invalid_dpop_jwk",
                "The DPoP proof jwk parameter must contain only an asymmetric public key.");
        }

        return parsed;
    }

    static TokenResult ValidateBinding(
        JwtValidationResult validated,
        byte[] thumbprint,
        DpopProofValidationPolicy policy)
    {
        TokenResult identifier = ValidateIdentifier(
            validated.Claims,
            policy.MaximumJwtIdLength);
        if (!identifier.Success)
        {
            return identifier;
        }

        TokenResult method = ValidateMethod(
            validated.Claims,
            policy.HttpMethod);
        if (!method.Success)
        {
            return method;
        }

        TokenResult uri = ValidateUri(
            validated.Claims,
            policy.HttpUri);
        if (!uri.Success)
        {
            return uri;
        }

        TokenResult nonce = ValidateNonce(
            (validated.Claims ?? throw new InvalidOperationException("A required value is null.")),
            policy.Nonce);
        if (!nonce.Success)
        {
            return nonce;
        }

        TokenResult accessToken = ValidateAccessToken(
            validated.Claims,
            policy.AccessToken);
        if (!accessToken.Success)
        {
            return accessToken;
        }

        return ValidateThumbprint(
            thumbprint,
            policy.ExpectedJwkThumbprint);
    }

    static TokenResult ValidateIdentifier(JwtClaims? claims, int maximumLength)
    {
        JsonValue? value = (claims ?? throw new InvalidOperationException("A required value is null.")).Get(JwtClaimNames.JwtId);
        if (!ValidString(value)
            || ((value ?? throw new InvalidOperationException("A required value is null.")).StringValue ?? throw new InvalidOperationException("A required value is null.")).Length > maximumLength)
        {
            return TokenFailure(
                "invalid_dpop_jwt_id",
                "The DPoP proof jti claim must be a bounded nonempty string.");
        }

        return TokenSuccess();
    }

    static TokenResult ValidateMethod(JwtClaims? claims, string? expected)
    {
        JsonValue? value = (claims ?? throw new InvalidOperationException("A required value is null.")).Get(JwtClaimNames.HttpMethod);
        if (!ValidString(value))
        {
            return TokenFailure(
                "invalid_dpop_http_method",
                "The DPoP proof htm claim must be a nonempty string.");
        }

        if (!string.Equals(
            (value ?? throw new InvalidOperationException("A required value is null.")).StringValue,
            expected,
            StringComparison.Ordinal))
        {
            return TokenFailure(
                "dpop_http_method_mismatch",
                "The DPoP proof htm claim does not match the current HTTP method.");
        }

        return TokenSuccess();
    }

    static TokenResult ValidateUri(JwtClaims? claims, string? requestUriText)
    {
        JsonValue? value = (claims ?? throw new InvalidOperationException("A required value is null.")).Get(JwtClaimNames.HttpUri);
        if (!ValidString(value)
            || !Uri.IsWellFormedUriString(
                (value ?? throw new InvalidOperationException("A required value is null.")).StringValue,
                UriKind.Absolute))
        {
            return TokenFailure(
                "invalid_dpop_http_uri",
                "The DPoP proof htu claim must be an absolute HTTPS URI.");
        }

        Uri proofUri = new Uri((value.StringValue ?? throw new InvalidOperationException("A required value is null.")), UriKind.Absolute);
        if (!string.Equals(
                proofUri.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase)
            || proofUri.Query.Length != 0
            || proofUri.Fragment.Length != 0)
        {
            return TokenFailure(
                "invalid_dpop_http_uri",
                "The DPoP proof htu claim must omit query and fragment components.");
        }

        Uri requestUri = new Uri((requestUriText ?? throw new InvalidOperationException("A required value is null.")), UriKind.Absolute);
        string proofNormalized = NormalizeUri(proofUri);
        string requestNormalized = NormalizeUri(requestUri);
        if (!string.Equals(
            proofNormalized,
            requestNormalized,
            StringComparison.Ordinal))
        {
            return TokenFailure(
                "dpop_http_uri_mismatch",
                "The DPoP proof htu claim does not match the current HTTP URI.");
        }

        return TokenSuccess();
    }

    static string NormalizeUri(Uri uri)
    {
        return uri.GetComponents(
            UriComponents.SchemeAndServer | UriComponents.Path,
            UriFormat.UriEscaped);
    }

    static TokenResult ValidateNonce(JwtClaims claims, string? expected)
    {
        JsonValue? value = claims.Get(JwtClaimNames.Nonce);
        if (value != null && !ValidString(value))
        {
            return TokenFailure(
                "invalid_dpop_nonce",
                "The DPoP proof nonce claim must be a nonempty string.");
        }

        if (expected == null)
        {
            return TokenSuccess();
        }

        if (value == null)
        {
            return TokenFailure(
                "missing_dpop_nonce",
                "The DPoP proof is missing the server-provided nonce.");
        }

        if (!EqualText(value.StringValue, expected))
        {
            return TokenFailure(
                "dpop_nonce_mismatch",
                "The DPoP proof nonce does not match the server-provided value.");
        }

        return TokenSuccess();
    }

    static TokenResult ValidateAccessToken(JwtClaims claims, string? accessToken)
    {
        JsonValue? value = claims.Get(JwtClaimNames.DpopAccessTokenHash);
        if (value != null && !ValidString(value))
        {
            return TokenFailure(
                "invalid_dpop_access_token_hash",
                "The DPoP proof ath claim must be a base64url SHA-256 value.");
        }

        if (accessToken == null)
        {
            return TokenSuccess();
        }

        if (value == null)
        {
            return TokenFailure(
                "missing_dpop_access_token_hash",
                "The DPoP proof is missing the required ath claim.");
        }

        TokenBytesResult encoded = AsciiBytes(accessToken);
        if (!encoded.Success)
        {
            return TokenFailure(encoded.Error);
        }

        byte[] expected = SHA256.HashData((encoded.Bytes ?? throw new InvalidOperationException("A required value is null.")));
        TokenBytesResult actual = Base64Url.Decode((value.StringValue ?? throw new InvalidOperationException("A required value is null.")));
        bool matches = actual.Success
            && (actual.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length == expected.Length
            && ConstantTime.AreEqual(actual.Bytes, expected);
        CryptographicOperations.ZeroMemory(expected);
        if (!matches)
        {
            return TokenFailure(
                "dpop_access_token_hash_mismatch",
                "The DPoP proof ath claim does not match the presented access token.");
        }

        return TokenSuccess();
    }

    static TokenResult ValidateThumbprint(byte[] actual, string? expectedText)
    {
        if (expectedText == null)
        {
            return TokenSuccess();
        }

        TokenBytesResult expected = Base64Url.Decode(expectedText);
        if (!expected.Success
            || !ConstantTime.AreEqual(actual, (expected.Bytes ?? throw new InvalidOperationException("A required value is null."))))
        {
            return TokenFailure(
                "dpop_jwk_thumbprint_mismatch",
                "The DPoP proof public key does not match the expected token binding.");
        }

        return TokenSuccess();
    }

    static TokenResult ValidateReplay(
        JwtClaims? claims,
        DpopProofValidationPolicy policy)
    {
        if (policy.JwtPolicy.ReplayValidator == null)
        {
            return TokenSuccess();
        }

        JsonValue? identifier = (claims ?? throw new InvalidOperationException("A required value is null.")).Get(JwtClaimNames.JwtId);
        DateTimeOffset current = (policy.JwtPolicy.Clock ?? throw new InvalidOperationException("A required value is null.")).GetUtcNow();
        DateTimeOffset threshold = DateTimeOffset.MaxValue.AddSeconds(
            -policy.MaximumProofAgeSeconds);
        DateTimeOffset expiration = current > threshold
            ? DateTimeOffset.MaxValue
            : current.AddSeconds(policy.MaximumProofAgeSeconds);
        TokenResult replay = policy.JwtPolicy.ReplayValidator(
            ((identifier ?? throw new InvalidOperationException("A required value is null.")).StringValue ?? throw new InvalidOperationException("A required value is null.")),
            expiration);
        if (replay == null)
        {
            return TokenFailure(
                "invalid_replay_validation_result",
                "The DPoP replay validator returned no result.");
        }

        if (replay.Success && replay.Error != null)
        {
            return TokenFailure(
                "invalid_replay_validation_result",
                "The DPoP replay validator returned contradictory success details.");
        }

        if (!replay.Success && replay.Error == null)
        {
            return TokenFailure(
                "dpop_proof_replay_detected",
                "The DPoP replay validator rejected the proof.");
        }

        return replay;
    }

    static bool ValidString(JsonValue? value)
    {
        return value != null
            && value.Kind == TokenJsonValueKind.String
            && value.StringValue != null
            && value.StringValue.Length > 0
            && Utf8Codec.Encode(value.StringValue).Success;
    }

    static bool EqualText(string? left, string right)
    {
        TokenBytesResult leftBytes = Utf8Codec.Encode((left ?? throw new InvalidOperationException("A required value is null.")));
        TokenBytesResult rightBytes = Utf8Codec.Encode(right);
        return leftBytes.Success
            && rightBytes.Success
            && ConstantTime.AreEqual((leftBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")), (rightBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")));
    }

    static TokenBytesResult AsciiBytes(string value)
    {
        byte[] bytes = new byte[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 0x7f)
            {
                TokenBytesResult failure = new TokenBytesResult();
                failure.Error = new TokenError(
                    "invalid_dpop_access_token",
                    "A DPoP-bound access token must contain only ASCII characters.",
                    index);
                return failure;
            }

            bytes[index] = (byte)value[index];
            index += 1;
        }

        TokenBytesResult result = new TokenBytesResult();
        result.Bytes = bytes;
        result.Success = true;
        return result;
    }

    static string[] MergeRequiredClaims(string[]? configured)
    {
        string[] merged = new string[(configured ?? throw new InvalidOperationException("A required value is null.")).Length + requiredClaims.Length];
        int count = 0;
        int index = 0;
        while (index < configured.Length)
        {
            merged[count] = configured[index];
            count += 1;
            index += 1;
        }

        index = 0;
        while (index < requiredClaims.Length)
        {
            string name = requiredClaims[index];
            if (!Contains(merged, count, name))
            {
                merged[count] = name;
                count += 1;
            }

            index += 1;
        }

        string[] result = new string[count];
        index = 0;
        while (index < count)
        {
            result[index] = merged[index];
            index += 1;
        }

        return result;
    }

    static bool Contains(string[] values, int count, string expected)
    {
        int index = 0;
        while (index < count)
        {
            if (string.Equals(
                values[index],
                expected,
                StringComparison.Ordinal))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static TokenKeyResult KeySuccess(TokenKey key)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Key = key;
        result.Success = true;
        return result;
    }

    static JwkResult JwkFailure(string code, string message)
    {
        JwkResult result = new JwkResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }

    static TokenResult TokenSuccess()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult TokenFailure(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }

    static TokenResult TokenFailure(TokenError? error)
    {
        TokenResult result = new TokenResult();
        result.Error = error;
        return result;
    }

    static DpopProofValidationResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static DpopProofValidationResult Failure(TokenError? error)
    {
        DpopProofValidationResult result = new DpopProofValidationResult();
        result.Error = error;
        return result;
    }
}
