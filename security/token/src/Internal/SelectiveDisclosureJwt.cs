using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace token;

/// <summary>
/// Parses and validates RFC 9901 compact Selective Disclosure JWTs.
/// </summary>
internal static class SelectiveDisclosureJwt
{
    /// <summary>Parses an SD-JWT using conservative default limits.</summary>
    /// <param name="text">The compact SD-JWT or SD-JWT+KB text.</param>
    /// <returns>The separated Issuer JWT, Disclosures, and optional Key Binding JWT.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    internal static SelectiveDisclosureJwtResult Parse(string text)
    {
        return Parse(text, new SelectiveDisclosureJwtValidationPolicy());
    }

    /// <summary>Parses an SD-JWT using explicit resource limits.</summary>
    /// <param name="text">The compact SD-JWT or SD-JWT+KB text.</param>
    /// <param name="policy">The parsing and validation limits.</param>
    /// <returns>The separated Issuer JWT, Disclosures, and optional Key Binding JWT.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static SelectiveDisclosureJwtResult Parse(
        string text,
        SelectiveDisclosureJwtValidationPolicy policy)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
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

        TokenBytesResult encoded = Utf8Codec.Encode(text);
        if (!encoded.Success)
        {
            return Failure(
                "invalid_sd_jwt",
                "The SD-JWT contains invalid text.");
        }

        if ((encoded.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length
            > (policy.JwtPolicy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).MaximumTokenSizeInBytes)
        {
            return Failure(
                "token_too_large",
                "The SD-JWT exceeds the configured token size limit.");
        }

        string[] parts = text.Split('~');
        if (parts.Length < 2 || parts[0].Length == 0)
        {
            return Failure(
                "invalid_sd_jwt_serialization",
                "An SD-JWT requires an Issuer JWT followed by a tilde separator.");
        }

        JwsCompactResult issuer = JsonWebSignature.Parse(
            parts[0],
            policy.JwtPolicy.TokenPolicy);
        if (!issuer.Success
            || (issuer.Token ?? throw new InvalidOperationException("A required value is null.")).Unsecured
            || !issuer.Token.PayloadBase64UrlEncoded)
        {
            return Failure(
                "invalid_sd_jwt_issuer",
                "The SD-JWT Issuer component must be a signed compact JWT.");
        }

        int last = parts.Length - 1;
        string? keyBinding = parts[last].Length == 0 ? null : parts[last];
        int disclosureCount = last - 1;
        if (disclosureCount > policy.MaximumDisclosures)
        {
            return Failure(
                "too_many_sd_jwt_disclosures",
                "The SD-JWT exceeds the configured Disclosure count limit.");
        }

        if (keyBinding != null)
        {
            JwsCompactResult binding = JsonWebSignature.Parse(
                keyBinding,
                policy.JwtPolicy.TokenPolicy);
            if (!binding.Success
                || (binding.Token ?? throw new InvalidOperationException("A required value is null.")).Unsecured
                || !binding.Token.PayloadBase64UrlEncoded)
            {
                return Failure(
                    "invalid_sd_jwt_key_binding",
                    "The final SD-JWT+KB component must be a signed compact JWT.");
            }
        }

        SelectiveDisclosure[] disclosures =
            new SelectiveDisclosure[disclosureCount];
        int index = 0;
        while (index < disclosureCount)
        {
            string component = parts[index + 1];
            if (component.Length == 0)
            {
                return Failure(
                    "invalid_sd_jwt_serialization",
                    "An SD-JWT contains an empty Disclosure component.");
            }

            SelectiveDisclosureJwtResult disclosure = ParseDisclosure(
                component,
                policy);
            if (!disclosure.Success)
            {
                return disclosure;
            }

            disclosures[index] = (disclosure.Disclosures ?? throw new InvalidOperationException("A required value is null."))[0];
            index += 1;
        }

        SelectiveDisclosureJwtResult result =
            new SelectiveDisclosureJwtResult();
        result.Kind = TokenKind.SdJwt;
        result.IssuerSignedJwt = parts[0];
        result.Disclosures = disclosures;
        result.KeyBindingJwt = keyBinding;
        result.Success = true;
        return result;
    }

    /// <summary>Authenticates an Issuer JWT and verifies and applies all supplied Disclosures.</summary>
    /// <param name="text">The compact SD-JWT or SD-JWT+KB text.</param>
    /// <param name="resolver">The resolver that selects the Issuer verification key.</param>
    /// <param name="policy">The cryptographic, claim, hash, and resource policy.</param>
    /// <returns>Authenticated and processed claims, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static SelectiveDisclosureJwtResult Validate(
        string text,
        ITokenKeyResolver resolver,
        SelectiveDisclosureJwtValidationPolicy policy)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (resolver == null)
        {
            throw new ArgumentNullException("resolver");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        SelectiveDisclosureJwtResult parsed = Parse(text, policy);
        if (!parsed.Success)
        {
            return parsed;
        }

        JwtValidationResult authenticated = JwtValidator.ValidateSigned(
            (parsed.IssuerSignedJwt ?? throw new InvalidOperationException("A required value is null.")),
            resolver,
            SignaturePolicy(policy.JwtPolicy));
        if (!authenticated.Success)
        {
            return Failure(authenticated.Error);
        }

        string? hashAlgorithm = ReadHashAlgorithm(
            authenticated.Claims,
            policy);
        if (hashAlgorithm == null)
        {
            return Failure(
                "unsupported_sd_jwt_hash_algorithm",
                "The SD-JWT Disclosure hash algorithm is missing, invalid, or not allowed.");
        }

        byte[][]? disclosureDigests = CalculateDisclosureDigests(
            parsed.Disclosures,
            hashAlgorithm);
        if (disclosureDigests == null)
        {
            return Failure(
                "duplicate_sd_jwt_disclosure",
                "The SD-JWT contains duplicate Disclosure digests.");
        }

        SelectiveDisclosureProcessingState state =
            new SelectiveDisclosureProcessingState(
                parsed.Disclosures,
                disclosureDigests,
                hashAlgorithm);
        JsonValue root = new JsonValue();
        root.Kind = TokenJsonValueKind.Object;
        root.ObjectValue = (authenticated.Claims ?? throw new InvalidOperationException("A required value is null.")).Values;
        if (!ProcessValue(root, true, state))
        {
            return Failure(state.Error);
        }

        int disclosureIndex = 0;
        while (disclosureIndex < state.UsedDisclosures.Length)
        {
            if (!state.UsedDisclosures[disclosureIndex])
            {
                return Failure(
                    "unreferenced_sd_jwt_disclosure",
                    "Every supplied Disclosure must be referenced by an embedded digest.");
            }

            disclosureIndex += 1;
        }

        JwtClaimsResult processed = JwtClaims.FromObject(root.ObjectValue);
        if (!processed.Success)
        {
            return Failure(processed.Error);
        }

        TokenResult claimsValidation = ValidateProcessedClaims(
            processed.Claims,
            policy.JwtPolicy);
        if (!claimsValidation.Success)
        {
            return Failure(claimsValidation.Error);
        }

        JwtValidationResult? keyBindingValidation = null;
        if (policy.RequireKeyBinding)
        {
            if (parsed.KeyBindingJwt == null)
            {
                return Failure(
                    "missing_sd_jwt_key_binding",
                    "The verifier policy requires an RFC 9901 Key Binding JWT.");
            }

            keyBindingValidation = SdJwtKeyBindingValidator.Validate(
                text,
                parsed.KeyBindingJwt,
                (processed.Claims ?? throw new InvalidOperationException("A required value is null.")),
                hashAlgorithm,
                policy);
            if (!keyBindingValidation.Success)
            {
                return Failure(keyBindingValidation.Error);
            }
        }

        TokenResult applicationValidation =
            JwtApplicationClaimValidator.Validate(
                (processed.Claims ?? throw new InvalidOperationException("A required value is null.")),
                policy.JwtPolicy);
        if (!applicationValidation.Success)
        {
            return Failure(applicationValidation.Error);
        }

        authenticated.Claims = processed.Claims;
        SelectiveDisclosureJwtResult result =
            new SelectiveDisclosureJwtResult();
        result.Kind = TokenKind.SdJwt;
        result.IssuerSignedJwt = parsed.IssuerSignedJwt;
        result.Disclosures = parsed.Disclosures;
        result.KeyBindingJwt = parsed.KeyBindingJwt;
        result.HashAlgorithm = hashAlgorithm;
        result.IssuerJwt = authenticated;
        result.Claims = processed.Claims;
        result.DigestsVerified = true;
        result.KeyBindingJwtValidation = keyBindingValidation;
        result.KeyBindingValidated = keyBindingValidation != null;
        result.Success = true;
        return result;
    }

    static SelectiveDisclosureJwtResult ParseDisclosure(
        string encoded,
        SelectiveDisclosureJwtValidationPolicy policy)
    {
        TokenBytesResult decoded = Base64Url.Decode(encoded);
        if (!decoded.Success)
        {
            return Failure(
                "invalid_sd_jwt_disclosure",
                "An SD-JWT Disclosure must use canonical base64url encoding.");
        }

        if ((decoded.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length > policy.MaximumDisclosureSizeInBytes)
        {
            return Failure(
                "sd_jwt_disclosure_too_large",
                "An SD-JWT Disclosure exceeds the configured size limit.");
        }

        JsonReadResult read = JsonReader.Read(
            decoded.Bytes,
            (policy.JwtPolicy.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).MaximumJsonDepth);
        if (!read.Success)
        {
            return Failure(
                "invalid_sd_jwt_disclosure",
                "An SD-JWT Disclosure must contain valid JSON.");
        }

        using (read.Document)
        {
            JsonElement root = (read.Document ?? throw new InvalidOperationException("A required value is null.")).RootElement;
            if (root.ValueKind != System.Text.Json.JsonValueKind.Array
                || root.GetArrayLength() < 2
                || root.GetArrayLength() > 3)
            {
                return Failure(
                    "invalid_sd_jwt_disclosure",
                    "An SD-JWT Disclosure must be a two- or three-element JSON array.");
            }

            JsonElement salt = root[0];
            if (salt.ValueKind != System.Text.Json.JsonValueKind.String)
            {
                return Failure(
                    "invalid_sd_jwt_disclosure",
                    "An SD-JWT Disclosure salt must be a string.");
            }

            SelectiveDisclosure disclosure = new SelectiveDisclosure();
            disclosure.Encoded = encoded;
            disclosure.Salt = salt.GetString();
            disclosure.ArrayElement = root.GetArrayLength() == 2;
            if (disclosure.ArrayElement)
            {
                disclosure.Value = CreateValue(root[1]);
            }
            else
            {
                JsonElement name = root[1];
                if (name.ValueKind != System.Text.Json.JsonValueKind.String)
                {
                    return Failure(
                        "invalid_sd_jwt_disclosure",
                        "An object-property Disclosure claim name must be a string.");
                }

                disclosure.ClaimName = name.GetString();
                if (string.Equals(
                    disclosure.ClaimName,
                    JwtClaimNames.SelectiveDisclosureDigests,
                    StringComparison.Ordinal)
                    || string.Equals(
                        disclosure.ClaimName,
                        JwtClaimNames.SelectiveDisclosureArrayElement,
                        StringComparison.Ordinal))
                {
                    return Failure(
                        "invalid_sd_jwt_disclosure",
                        "An SD-JWT Disclosure cannot use a reserved claim name.");
                }

                disclosure.Value = CreateValue(root[2]);
            }

            SelectiveDisclosureJwtResult result =
                new SelectiveDisclosureJwtResult();
            result.Kind = TokenKind.SdJwt;
            result.Disclosures = new SelectiveDisclosure[] { disclosure };
            result.Success = true;
            return result;
        }
    }

    static JsonValue? CreateValue(JsonElement element)
    {
        JsonValue result = new JsonValue();
        switch ((int)element.ValueKind)
        {
            case (int)System.Text.Json.JsonValueKind.Null:
                result.Kind = TokenJsonValueKind.Null;
                break;
            case (int)System.Text.Json.JsonValueKind.False:
                result.Kind = TokenJsonValueKind.Boolean;
                result.BooleanValue = false;
                break;
            case (int)System.Text.Json.JsonValueKind.True:
                result.Kind = TokenJsonValueKind.Boolean;
                result.BooleanValue = true;
                break;
            case (int)System.Text.Json.JsonValueKind.Number:
                result.Kind = TokenJsonValueKind.Number;
                result.NumberText = element.GetRawText();
                break;
            case (int)System.Text.Json.JsonValueKind.String:
                result.Kind = TokenJsonValueKind.String;
                result.StringValue = element.GetString();
                break;
            case (int)System.Text.Json.JsonValueKind.Array:
                result.Kind = TokenJsonValueKind.Array;
                result.ArrayValue = CreateArray(element);
                break;
            case (int)System.Text.Json.JsonValueKind.Object:
                result.Kind = TokenJsonValueKind.Object;
                result.ObjectValue = CreateObject(element);
                break;
            default:
                return null;
        }

        return result;
    }

    static JsonValue[] CreateArray(JsonElement element)
    {
        List<JsonValue> values = new List<JsonValue>();
        foreach (JsonElement item in element.EnumerateArray())
        {
            values.Add((CreateValue(item) ?? throw new InvalidOperationException("A required value is null.")));
        }

        return values.ToArray();
    }

    static JsonObject CreateObject(JsonElement element)
    {
        List<JsonMember> members = new List<JsonMember>();
        foreach (JsonProperty property in element.EnumerateObject())
        {
            members.Add(new JsonMember(
                property.Name,
                (CreateValue(property.Value) ?? throw new InvalidOperationException("A required value is null."))));
        }

        return new JsonObject(members.ToArray());
    }

    static JwtValidationPolicy SignaturePolicy(JwtValidationPolicy policy)
    {
        JwtValidationPolicy result = policy.Copy();
        result.ValidateIssuer = false;
        result.ValidateAudience = false;
        result.ValidateExpirationTime = false;
        result.ValidateNotBefore = false;
        result.ValidateIssuedAt = false;
        result.RequireExpirationTime = false;
        result.AllowSignedTokens = true;
        result.AllowEncryptedTokens = false;
        result.AllowNestedTokens = false;
        result.MaximumTokenAgeSeconds = 0;
        (result.TokenPolicy ?? throw new InvalidOperationException("A required value is null.")).RequiredClaims = Array.Empty<string>();
        result.SubjectValidator = null;
        result.ClaimValidator = null;
        result.ReplayValidator = null;
        return result;
    }

    static string? ReadHashAlgorithm(
        JwtClaims? claims,
        SelectiveDisclosureJwtValidationPolicy policy)
    {
        JsonValue? value = (claims ?? throw new InvalidOperationException("A required value is null.")).Get(
            JwtClaimNames.SelectiveDisclosureAlgorithm);
        string algorithm = SelectiveDisclosureHashAlgorithms.Sha256;
        if (value != null)
        {
            if (value.Kind != TokenJsonValueKind.String
                || value.StringValue == null)
            {
                return null;
            }

            algorithm = value.StringValue;
        }

        int index = 0;
        while (index < policy.AllowedHashAlgorithms.Length)
        {
            if (string.Equals(
                policy.AllowedHashAlgorithms[index],
                algorithm,
                StringComparison.Ordinal))
            {
                return algorithm;
            }

            index += 1;
        }

        return null;
    }

    static byte[][]? CalculateDisclosureDigests(
        SelectiveDisclosure[]? disclosures,
        string algorithm)
    {
        byte[][] result = new byte[(disclosures ?? throw new InvalidOperationException("A required value is null.")).Length][];
        int index = 0;
        while (index < disclosures.Length)
        {
            byte[] digest = HashDisclosure(
                disclosures[index].Encoded,
                algorithm);
            int previous = 0;
            while (previous < index)
            {
                if (ConstantTime.AreEqual(result[previous], digest))
                {
                    return null;
                }

                previous += 1;
            }

            result[index] = digest;
            index += 1;
        }

        return result;
    }

    static byte[] HashDisclosure(string? disclosure, string algorithm)
    {
        byte[] bytes = Encoding.ASCII.GetBytes((disclosure ?? throw new InvalidOperationException("A required value is null.")));
        if (string.Equals(
            algorithm,
            SelectiveDisclosureHashAlgorithms.Sha384,
            StringComparison.Ordinal))
        {
            return SHA384.HashData(bytes);
        }

        if (string.Equals(
            algorithm,
            SelectiveDisclosureHashAlgorithms.Sha512,
            StringComparison.Ordinal))
        {
            return SHA512.HashData(bytes);
        }

        return SHA256.HashData(bytes);
    }

    static bool ProcessValue(
        JsonValue? value,
        bool topLevel,
        SelectiveDisclosureProcessingState state)
    {
        if (value == null)
        {
            SetError(
                state,
                "invalid_sd_jwt_payload",
                "The SD-JWT payload contains an incomplete JSON value.");
            return false;
        }

        if (value.Kind == TokenJsonValueKind.Object)
        {
            return ProcessObject(value.ObjectValue, topLevel, state);
        }

        if (value.Kind == TokenJsonValueKind.Array)
        {
            return ProcessArray(value, state);
        }

        return true;
    }

    static bool ProcessObject(
        JsonObject? value,
        bool topLevel,
        SelectiveDisclosureProcessingState state)
    {
        if (value == null || value.Members == null)
        {
            SetError(
                state,
                "invalid_sd_jwt_payload",
                "The SD-JWT payload contains an incomplete object.");
            return false;
        }

        if (!topLevel
            && value.Get(JwtClaimNames.SelectiveDisclosureAlgorithm) != null)
        {
            SetError(
                state,
                "nested_sd_jwt_hash_algorithm",
                "The _sd_alg claim is only allowed at the top level.");
            return false;
        }

        if (value.Get(JwtClaimNames.SelectiveDisclosureArrayElement) != null)
        {
            SetError(
                state,
                "invalid_sd_jwt_array_placeholder",
                "The ... digest placeholder is only allowed as a sole array element member.");
            return false;
        }

        List<JsonMember> output = new List<JsonMember>();
        int index = 0;
        while (index < value.Members.Length)
        {
            JsonMember member = value.Members[index];
            bool digestMember = string.Equals(
                member.Name,
                JwtClaimNames.SelectiveDisclosureDigests,
                StringComparison.Ordinal);
            bool algorithmMember = topLevel
                && string.Equals(
                    member.Name,
                    JwtClaimNames.SelectiveDisclosureAlgorithm,
                    StringComparison.Ordinal);
            if (!digestMember && !algorithmMember)
            {
                if (!ProcessValue(member.Value, false, state))
                {
                    return false;
                }

                output.Add(member);
            }

            index += 1;
        }

        JsonValue? digests = value.Get(
            JwtClaimNames.SelectiveDisclosureDigests);
        if (digests != null)
        {
            if (digests.Kind != TokenJsonValueKind.Array
                || digests.ArrayValue == null)
            {
                SetError(
                    state,
                    "invalid_sd_jwt_digest_array",
                    "Every _sd member must contain an array of digest strings.");
                return false;
            }

            index = 0;
            while (index < digests.ArrayValue.Length)
            {
                JsonValue item = digests.ArrayValue[index];
                if (item == null
                    || item.Kind != TokenJsonValueKind.String
                    || item.StringValue == null)
                {
                    SetError(
                        state,
                        "invalid_sd_jwt_digest_array",
                        "Every _sd array item must be a digest string.");
                    return false;
                }

                byte[]? digest = ReadEmbeddedDigest(item.StringValue, state);
                if (digest == null)
                {
                    return false;
                }

                int disclosureIndex = FindDisclosure(digest, state);
                if (disclosureIndex >= 0)
                {
                    SelectiveDisclosure disclosure =
                        (state.Disclosures ?? throw new InvalidOperationException("A required value is null."))[disclosureIndex];
                    if (disclosure.ArrayElement)
                    {
                        SetError(
                            state,
                            "invalid_sd_jwt_disclosure_context",
                            "An object digest must reference a three-element Disclosure.");
                        return false;
                    }

                    if (ContainsMember(output, disclosure.ClaimName))
                    {
                        SetError(
                            state,
                            "sd_jwt_claim_collision",
                            "A disclosed claim name conflicts with an existing object member.");
                        return false;
                    }

                    if (state.UsedDisclosures[disclosureIndex])
                    {
                        SetError(
                            state,
                            "duplicate_sd_jwt_disclosure_reference",
                            "A Disclosure digest is referenced more than once.");
                        return false;
                    }

                    state.UsedDisclosures[disclosureIndex] = true;
                    if (!ProcessValue(disclosure.Value, false, state))
                    {
                        return false;
                    }

                    output.Add(new JsonMember(
                        (disclosure.ClaimName ?? throw new InvalidOperationException("A required value is null.")),
                        (disclosure.Value ?? throw new InvalidOperationException("A required value is null."))));
                }

                index += 1;
            }
        }

        value.Members = output.ToArray();
        return true;
    }

    static bool ProcessArray(
        JsonValue value,
        SelectiveDisclosureProcessingState state)
    {
        if (value.ArrayValue == null)
        {
            SetError(
                state,
                "invalid_sd_jwt_payload",
                "The SD-JWT payload contains an incomplete array.");
            return false;
        }

        List<JsonValue> output = new List<JsonValue>();
        int index = 0;
        while (index < value.ArrayValue.Length)
        {
            JsonValue item = value.ArrayValue[index];
            JsonValue? placeholder = ArrayPlaceholder(item);
            if (placeholder == null)
            {
                if (!ProcessValue(item, false, state))
                {
                    return false;
                }

                output.Add(item);
                index += 1;
                continue;
            }

            if (placeholder.Kind != TokenJsonValueKind.String
                || placeholder.StringValue == null)
            {
                SetError(
                    state,
                    "invalid_sd_jwt_array_placeholder",
                    "An array Disclosure placeholder must contain one digest string.");
                return false;
            }

            byte[]? digest = ReadEmbeddedDigest(
                placeholder.StringValue,
                state);
            if (digest == null)
            {
                return false;
            }

            int disclosureIndex = FindDisclosure(digest, state);
            if (disclosureIndex >= 0)
            {
                SelectiveDisclosure disclosure =
                    (state.Disclosures ?? throw new InvalidOperationException("A required value is null."))[disclosureIndex];
                if (!disclosure.ArrayElement)
                {
                    SetError(
                        state,
                        "invalid_sd_jwt_disclosure_context",
                        "An array digest must reference a two-element Disclosure.");
                    return false;
                }

                if (state.UsedDisclosures[disclosureIndex])
                {
                    SetError(
                        state,
                        "duplicate_sd_jwt_disclosure_reference",
                        "A Disclosure digest is referenced more than once.");
                    return false;
                }

                state.UsedDisclosures[disclosureIndex] = true;
                if (!ProcessValue(disclosure.Value, false, state))
                {
                    return false;
                }

                output.Add((disclosure.Value ?? throw new InvalidOperationException("A required value is null.")));
            }

            index += 1;
        }

        value.ArrayValue = output.ToArray();
        return true;
    }

    static JsonValue? ArrayPlaceholder(JsonValue value)
    {
        if (value == null
            || value.Kind != TokenJsonValueKind.Object
            || value.ObjectValue == null
            || value.ObjectValue.Members == null
            || value.ObjectValue.Members.Length != 1)
        {
            return null;
        }

        JsonMember member = value.ObjectValue.Members[0];
        if (!string.Equals(
            member.Name,
            JwtClaimNames.SelectiveDisclosureArrayElement,
            StringComparison.Ordinal))
        {
            return null;
        }

        return member.Value;
    }

    static byte[]? ReadEmbeddedDigest(
        string encoded,
        SelectiveDisclosureProcessingState state)
    {
        TokenBytesResult decoded = Base64Url.Decode(encoded);
        int expectedLength = DigestLength(state.HashAlgorithm);
        if (!decoded.Success || (decoded.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length != expectedLength)
        {
            SetError(
                state,
                "invalid_sd_jwt_digest",
                "An embedded Disclosure digest is invalid for the selected hash algorithm.");
            return null;
        }

        int index = 0;
        while (index < state.EmbeddedDigests.Count)
        {
            if (ConstantTime.AreEqual(
                state.EmbeddedDigests[index],
                decoded.Bytes))
            {
                SetError(
                    state,
                    "duplicate_sd_jwt_digest",
                    "The same Disclosure digest appears more than once in the SD-JWT.");
                return null;
            }

            index += 1;
        }

        state.EmbeddedDigests.Add(decoded.Bytes);
        return decoded.Bytes;
    }

    static int FindDisclosure(
        byte[] digest,
        SelectiveDisclosureProcessingState state)
    {
        int found = -1;
        int index = 0;
        while (index < state.DisclosureDigests.Length)
        {
            bool equal = ConstantTime.AreEqual(
                digest,
                state.DisclosureDigests[index]);
            if (equal)
            {
                found = index;
            }

            index += 1;
        }

        return found;
    }

    static bool ContainsMember(List<JsonMember> members, string? name)
    {
        int index = 0;
        while (index < members.Count)
        {
            if (string.Equals(
                members[index].Name,
                name,
                StringComparison.Ordinal))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static int DigestLength(string algorithm)
    {
        if (string.Equals(
            algorithm,
            SelectiveDisclosureHashAlgorithms.Sha384,
            StringComparison.Ordinal))
        {
            return 48;
        }

        if (string.Equals(
            algorithm,
            SelectiveDisclosureHashAlgorithms.Sha512,
            StringComparison.Ordinal))
        {
            return 64;
        }

        return 32;
    }

    static TokenResult ValidateProcessedClaims(
        JwtClaims? claims,
        JwtValidationPolicy policy)
    {
        TokenResult required = JwtRequiredClaimValidator.Validate(
            (claims ?? throw new InvalidOperationException("A required value is null.")),
            policy);
        if (!required.Success)
        {
            return required;
        }

        TokenResult issuer = JwtIssuerValidator.Validate(claims, policy);
        if (!issuer.Success)
        {
            return issuer;
        }

        TokenResult audience = JwtAudienceValidator.Validate(claims, policy);
        if (!audience.Success)
        {
            return audience;
        }

        TokenResult lifetime = JwtLifetimeValidator.Validate(claims, policy);
        if (!lifetime.Success)
        {
            return lifetime;
        }

        return JwtReplayValidator.Validate(claims, policy);
    }

    static void SetError(
        SelectiveDisclosureProcessingState state,
        string code,
        string message)
    {
        state.Error = new TokenError(code, message, -1);
    }

    static SelectiveDisclosureJwtResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static SelectiveDisclosureJwtResult Failure(TokenError? error)
    {
        SelectiveDisclosureJwtResult result =
            new SelectiveDisclosureJwtResult();
        result.Error = error;
        return result;
    }
}
