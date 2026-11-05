using System;
using System.Security.Cryptography;
using System.Text;

namespace token;

/// <summary>Validates RFC 9901 Key Binding JWT signatures and transaction claims.</summary>
internal static class SdJwtKeyBindingValidator
{
    /// <summary>Validates a Key Binding JWT against processed SD-JWT claims.</summary>
    /// <param name="presentation">The complete SD-JWT+KB presentation.</param>
    /// <param name="keyBindingJwt">The compact Key Binding JWT.</param>
    /// <param name="claims">The processed issuer claims containing the holder key.</param>
    /// <param name="hashAlgorithm">The Disclosure and presentation hash algorithm.</param>
    /// <param name="policy">The verifier transaction and cryptographic policy.</param>
    /// <returns>Authenticated Key Binding JWT metadata, or stable failure details.</returns>
    internal static JwtValidationResult Validate(
        string presentation,
        string keyBindingJwt,
        JwtClaims claims,
        string hashAlgorithm,
        SelectiveDisclosureJwtValidationPolicy policy)
    {
        if (presentation == null)
        {
            throw new ArgumentNullException("presentation");
        }

        if (keyBindingJwt == null)
        {
            throw new ArgumentNullException("keyBindingJwt");
        }

        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        if (hashAlgorithm == null)
        {
            throw new ArgumentNullException("hashAlgorithm");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenKeyResult holder = ReadHolderKey(claims);
        if (!holder.Success)
        {
            return Failure(holder.Error);
        }

        JwtValidationPolicy bindingPolicy = CreatePolicy(policy);
        TokenKeyResolver resolver = new TokenKeyResolver(
            context => KeySuccess((holder.Key ?? throw new InvalidOperationException("A required value is null."))));
        JwtValidationResult validated = JwtValidator.ValidateSigned(
            keyBindingJwt,
            resolver,
            bindingPolicy);
        (holder.Key ?? throw new InvalidOperationException("A required value is null.")).ReleaseOwnedMaterial();
        if (!validated.Success)
        {
            return validated;
        }

        JsonValue? audience = (validated.Claims ?? throw new InvalidOperationException("A required value is null.")).Get(
            JwtClaimNames.Audience);
        if (audience == null
            || audience.Kind != TokenJsonValueKind.String
            || audience.StringValue == null)
        {
            return Failure(
                "invalid_sd_jwt_key_binding_audience",
                "The Key Binding JWT aud claim must be one string.");
        }

        JsonValue? nonce = validated.Claims.Get(JwtClaimNames.Nonce);
        if (!Matches(nonce, policy.ExpectedKeyBindingNonce))
        {
            return Failure(
                "sd_jwt_key_binding_nonce_mismatch",
                "The Key Binding JWT nonce does not match the verifier challenge.");
        }

        JsonValue? actualHash = validated.Claims.Get(
            JwtClaimNames.SelectiveDisclosureHash);
        string? expectedHash = CalculatePresentationHash(
            presentation,
            hashAlgorithm);
        if (!Matches(actualHash, expectedHash))
        {
            return Failure(
                "sd_jwt_key_binding_hash_mismatch",
                "The Key Binding JWT sd_hash does not match the presented SD-JWT.");
        }

        return validated;
    }

    static TokenKeyResult ReadHolderKey(JwtClaims claims)
    {
        JsonValue? confirmation = claims.Get(JwtClaimNames.Confirmation);
        if (confirmation == null
            || confirmation.Kind != TokenJsonValueKind.Object
            || confirmation.ObjectValue == null)
        {
            return KeyFailure(
                "missing_sd_jwt_holder_key",
                "Required SD-JWT key binding needs a cnf object containing a public JWK.");
        }

        JsonValue? jwk = confirmation.ObjectValue.Get("jwk");
        if (jwk == null
            || jwk.Kind != TokenJsonValueKind.Object
            || jwk.ObjectValue == null)
        {
            return KeyFailure(
                "missing_sd_jwt_holder_key",
                "Required SD-JWT key binding needs a cnf.jwk public key.");
        }

        JwkResult parsed = JsonWebKey.ReadObject(jwk.ObjectValue);
        if (!parsed.Success)
        {
            return KeyFailure(
                "invalid_sd_jwt_holder_key",
                "The SD-JWT cnf.jwk holder key is invalid.");
        }

        TokenKey key = (parsed.Key ?? throw new InvalidOperationException("A required value is null."));
        if (string.Equals(
                key.KeyType,
                TokenKeyTypes.Symmetric,
                StringComparison.Ordinal)
            || key.ContainsPrivateMaterial())
        {
            key.ReleaseOwnedMaterial();
            return KeyFailure(
                "invalid_sd_jwt_holder_key",
                "The SD-JWT cnf.jwk value must contain only an asymmetric public key.");
        }

        TokenKeyResult result = new TokenKeyResult();
        result.Key = key;
        result.Success = true;
        return result;
    }

    static JwtValidationPolicy CreatePolicy(
        SelectiveDisclosureJwtValidationPolicy policy)
    {
        JwtValidationPolicy result = JwtValidationPolicy.Create(
            (policy.JwtPolicy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")));
        result.Clock = policy.JwtPolicy.Clock;
        result.ValidateIssuer = false;
        result.ValidateAudience = true;
        result.ValidateExpirationTime = false;
        result.ValidateNotBefore = false;
        result.ValidateIssuedAt = true;
        result.RequireExpirationTime = false;
        result.ValidateTokenType = true;
        result.ValidateHeaderClaimReplication = false;
        result.AllowSignedTokens = true;
        result.AllowEncryptedTokens = false;
        result.AllowNestedTokens = false;
        result.MaximumTokenAgeSeconds = policy.MaximumKeyBindingAgeSeconds;
        result.ValidAudiences = new string[]
        {
            (policy.ExpectedKeyBindingAudience ?? throw new InvalidOperationException("A required value is null."))
        };
        result.ValidTokenTypes = new string[] { JoseMediaTypes.KeyBindingJwt };
        (result.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).RequiredClaims = new string[]
        {
            JwtClaimNames.IssuedAt,
            JwtClaimNames.Audience,
            JwtClaimNames.Nonce,
            JwtClaimNames.SelectiveDisclosureHash
        };
        return result;
    }

    static string? CalculatePresentationHash(
        string presentation,
        string algorithm)
    {
        int separator = presentation.LastIndexOf('~');
        if (separator < 0)
        {
            return null;
        }

        string sdJwt = presentation.Substring(0, separator + 1);
        byte[] bytes = Encoding.ASCII.GetBytes(sdJwt);
        byte[] digest;
        if (string.Equals(
            algorithm,
            SelectiveDisclosureHashAlgorithms.Sha384,
            StringComparison.Ordinal))
        {
            digest = SHA384.HashData(bytes);
        }
        else if (string.Equals(
            algorithm,
            SelectiveDisclosureHashAlgorithms.Sha512,
            StringComparison.Ordinal))
        {
            digest = SHA512.HashData(bytes);
        }
        else
        {
            digest = SHA256.HashData(bytes);
        }

        TokenTextResult encoded = Base64Url.Encode(digest);
        return encoded.Success ? encoded.Text : null;
    }

    static bool Matches(JsonValue? actual, string? expected)
    {
        if (actual == null
            || actual.Kind != TokenJsonValueKind.String
            || actual.StringValue == null
            || expected == null)
        {
            return false;
        }

        TokenBytesResult actualBytes = Utf8Codec.Encode(actual.StringValue);
        TokenBytesResult expectedBytes = Utf8Codec.Encode(expected);
        return actualBytes.Success
            && expectedBytes.Success
            && ConstantTime.AreEqual(
                (actualBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                (expectedBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")));
    }

    static TokenKeyResult KeySuccess(TokenKey key)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Key = key;
        result.Success = true;
        return result;
    }

    static TokenKeyResult KeyFailure(string code, string message)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }

    static JwtValidationResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static JwtValidationResult Failure(TokenError? error)
    {
        JwtValidationResult result = new JwtValidationResult();
        result.Error = error;
        return result;
    }
}
