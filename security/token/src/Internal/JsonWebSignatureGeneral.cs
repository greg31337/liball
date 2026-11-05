using System;
using System.Text;

namespace token;

/// <summary>Reads, writes, signs, and verifies general JWS JSON serializations.</summary>
internal static class JsonWebSignatureGeneral
{
    /// <summary>Creates an explicitly policy-authorized unsecured general JWS.</summary>
    /// <param name="payload">The attached shared payload bytes.</param>
    /// <param name="protectedHeaders">The protected headers, each using <c>none</c>.</param>
    /// <param name="unprotectedHeaders">Matching unprotected headers, or <c>null</c>.</param>
    /// <param name="policy">The policy that must allow unsecured tokens and the <c>none</c> algorithm.</param>
    /// <returns>A result containing unsecured general JWS JSON.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static TokenTextResult CreateUnsecured(
        byte[] payload,
        JoseHeader[] protectedHeaders,
        JoseHeader?[]? unprotectedHeaders,
        TokenPolicy policy)
    {
        if (payload == null)
        {
            throw new ArgumentNullException("payload");
        }

        if (protectedHeaders == null)
        {
            throw new ArgumentNullException("protectedHeaders");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult allowed = JsonWebSignature.ValidateUnsecuredPolicy(policy);
        if (!allowed.Success)
        {
            return TextFailure(allowed.Error);
        }

        TokenResult counts = ValidateCounts(
            protectedHeaders.Length,
            unprotectedHeaders,
            protectedHeaders.Length);
        if (!counts.Success)
        {
            return TextFailure(counts.Error);
        }

        TokenResult encoding = ValidatePayloadEncoding(protectedHeaders);
        if (!encoding.Success)
        {
            return TextFailure(encoding.Error);
        }

        string[] flattened = new string[protectedHeaders.Length];
        int index = 0;
        while (index < protectedHeaders.Length)
        {
            JoseHeader? unprotected = GetUnprotected(unprotectedHeaders, index);
            TokenTextResult created = JsonWebSignatureFlattened.CreateUnsecured(
                payload,
                protectedHeaders[index],
                unprotected,
                policy);
            if (!created.Success)
            {
                return created;
            }

            flattened[index] = (created.Text ?? throw new InvalidOperationException("A required value is null."));
            index += 1;
        }

        return CombineFlattened(flattened, false, policy);
    }

    /// <summary>Parses a general JWS using conservative default limits.</summary>
    /// <param name="json">The general JWS JSON text.</param>
    /// <returns>A result containing unverified signature metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>json</c> is null.</exception>
    internal static JwsGeneralResult Parse(string json)
    {
        return Parse(json, new TokenPolicy());
    }

    /// <summary>Parses a general JWS while withholding unauthenticated payload bytes.</summary>
    /// <param name="json">The general JWS JSON text.</param>
    /// <param name="policy">The header and size policy.</param>
    /// <returns>A result containing unverified signature metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwsGeneralResult Parse(string json, TokenPolicy policy)
    {
        return ReadCore(json, policy, false);
    }

    /// <summary>Reads an attached shared payload when diagnostic access is enabled.</summary>
    /// <param name="json">The general JWS JSON text.</param>
    /// <param name="policy">The policy that must permit unauthenticated decode.</param>
    /// <returns>A result containing unverified payload and signature metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwsGeneralResult ReadDiagnostic(
        string json,
        TokenPolicy policy)
    {
        return ReadCore(json, policy, true);
    }

    /// <summary>Writes an attached general JWS from decoded components.</summary>
    /// <param name="payload">The shared payload bytes.</param>
    /// <param name="protectedHeaders">The protected headers.</param>
    /// <param name="unprotectedHeaders">Matching unprotected headers, or <c>null</c>.</param>
    /// <param name="signatures">Matching signature or MAC byte arrays.</param>
    /// <param name="policy">The header and size policy.</param>
    /// <returns>A result containing general JWS JSON.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static TokenTextResult Write(
        byte[] payload,
        JoseHeader[] protectedHeaders,
        JoseHeader?[]? unprotectedHeaders,
        byte[][] signatures,
        TokenPolicy policy)
    {
        if (payload == null)
        {
            throw new ArgumentNullException("payload");
        }

        return WriteCore(
            payload,
            protectedHeaders,
            unprotectedHeaders,
            signatures,
            policy,
            false);
    }

    /// <summary>Writes a detached general JWS from decoded signature components.</summary>
    /// <param name="protectedHeaders">The protected headers.</param>
    /// <param name="unprotectedHeaders">Matching unprotected headers, or <c>null</c>.</param>
    /// <param name="signatures">Matching signature or MAC byte arrays.</param>
    /// <param name="policy">The header and size policy.</param>
    /// <returns>A result containing general JWS JSON without a payload member.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static TokenTextResult WriteDetached(
        JoseHeader[] protectedHeaders,
        JoseHeader?[]? unprotectedHeaders,
        byte[][] signatures,
        TokenPolicy policy)
    {
        return WriteCore(
            Array.Empty<byte>(),
            protectedHeaders,
            unprotectedHeaders,
            signatures,
            policy,
            true);
    }

    /// <summary>Signs one shared attached payload with multiple keys.</summary>
    /// <param name="payload">The shared payload bytes.</param>
    /// <param name="protectedHeaders">The protected headers.</param>
    /// <param name="unprotectedHeaders">Matching unprotected headers, or <c>null</c>.</param>
    /// <param name="keys">The matching signing keys.</param>
    /// <param name="policy">The algorithm, header, and size policy.</param>
    /// <returns>A result containing signed general JWS JSON.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static TokenTextResult Sign(
        byte[] payload,
        JoseHeader[] protectedHeaders,
        JoseHeader?[]? unprotectedHeaders,
        TokenKey[] keys,
        TokenPolicy policy)
    {
        if (payload == null)
        {
            throw new ArgumentNullException("payload");
        }

        return SignCore(
            payload,
            protectedHeaders,
            unprotectedHeaders,
            keys,
            policy,
            false);
    }

    /// <summary>Signs one shared detached payload with multiple keys.</summary>
    /// <param name="payload">The external shared payload bytes.</param>
    /// <param name="protectedHeaders">The protected headers.</param>
    /// <param name="unprotectedHeaders">Matching unprotected headers, or <c>null</c>.</param>
    /// <param name="keys">The matching signing keys.</param>
    /// <param name="policy">The algorithm, header, and size policy.</param>
    /// <returns>A result containing signed general JWS JSON without a payload member.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static TokenTextResult SignDetached(
        byte[] payload,
        JoseHeader[] protectedHeaders,
        JoseHeader?[]? unprotectedHeaders,
        TokenKey[] keys,
        TokenPolicy policy)
    {
        if (payload == null)
        {
            throw new ArgumentNullException("payload");
        }

        return SignCore(
            payload,
            protectedHeaders,
            unprotectedHeaders,
            keys,
            policy,
            true);
    }

    /// <summary>Verifies every signature over an attached general JWS payload.</summary>
    /// <param name="json">The general JWS JSON text.</param>
    /// <param name="keys">Verification keys corresponding to signature order.</param>
    /// <param name="policy">The algorithm, header, and size policy.</param>
    /// <returns>A result containing payload only when every signature verifies.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwsGeneralResult Verify(
        string json,
        TokenKey[] keys,
        TokenPolicy policy)
    {
        return VerifyCore(json, Array.Empty<byte>(), keys, policy, false);
    }

    /// <summary>Validates an explicitly policy-authorized unsecured general JWS.</summary>
    /// <param name="json">The unsecured general JWS JSON text.</param>
    /// <param name="policy">The policy that must allow unsecured tokens and the <c>none</c> algorithm.</param>
    /// <returns>A result containing the policy-validated attached payload.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwsGeneralResult ValidateUnsecured(
        string json,
        TokenPolicy policy)
    {
        if (json == null)
        {
            throw new ArgumentNullException("json");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult allowed = JsonWebSignature.ValidateUnsecuredPolicy(policy);
        if (!allowed.Success)
        {
            return Failure(allowed.Error);
        }

        JwsGeneralResult parsed = ReadCore(json, policy, false);
        if (!parsed.Success)
        {
            return parsed;
        }

        if (!(parsed.Token ?? throw new InvalidOperationException("A required value is null.")).Unsecured)
        {
            return Failure(
                "invalid_unsecured_algorithm",
                "Unsecured validation requires every signature to use none.");
        }

        if (parsed.Token.DetachedPayload)
        {
            return Failure(
                "missing_payload",
                "Unsecured validation requires an attached payload.");
        }

        parsed.Token.Payload = parsed.Token.ParsedPayload.AsSpan().ToArray();
        parsed.Token.PayloadAvailable = true;
        return parsed;
    }

    /// <summary>Verifies every signature over an external detached payload.</summary>
    /// <param name="json">The general JWS JSON text without a payload member.</param>
    /// <param name="payload">The external shared payload bytes.</param>
    /// <param name="keys">Verification keys corresponding to signature order.</param>
    /// <param name="policy">The algorithm, header, and size policy.</param>
    /// <returns>A result containing payload only when every signature verifies.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwsGeneralResult VerifyDetached(
        string json,
        byte[] payload,
        TokenKey[] keys,
        TokenPolicy policy)
    {
        if (payload == null)
        {
            throw new ArgumentNullException("payload");
        }

        return VerifyCore(json, payload, keys, policy, true);
    }

    static TokenTextResult SignCore(
        byte[] payload,
        JoseHeader[] protectedHeaders,
        JoseHeader?[]? unprotectedHeaders,
        TokenKey[] keys,
        TokenPolicy policy,
        bool detached)
    {
        ValidateSignArguments(protectedHeaders, keys, policy);
        TokenResult counts = ValidateCounts(
            protectedHeaders.Length,
            unprotectedHeaders,
            keys.Length);
        if (!counts.Success)
        {
            return TextFailure(counts.Error);
        }

        TokenResult encoding = ValidatePayloadEncoding(protectedHeaders);
        if (!encoding.Success)
        {
            return TextFailure(encoding.Error);
        }

        string[] flattened = new string[protectedHeaders.Length];
        int index = 0;
        while (index < protectedHeaders.Length)
        {
            if (keys[index] == null)
            {
                return TextFailure(
                    "invalid_key",
                    "A signing key is missing.");
            }

            JoseHeader? unprotected = GetUnprotected(unprotectedHeaders, index);
            TokenTextResult signed = detached
                ? JsonWebSignatureFlattened.SignDetached(
                    payload,
                    protectedHeaders[index],
                    unprotected,
                    keys[index],
                    policy)
                : JsonWebSignatureFlattened.Sign(
                    payload,
                    protectedHeaders[index],
                    unprotected,
                    keys[index],
                    policy);
            if (!signed.Success)
            {
                return TextFailure(signed.Error);
            }

            flattened[index] = (signed.Text ?? throw new InvalidOperationException("A required value is null."));
            index += 1;
        }

        return CombineFlattened(flattened, detached, policy);
    }

    static TokenTextResult WriteCore(
        byte[] payload,
        JoseHeader[] protectedHeaders,
        JoseHeader?[]? unprotectedHeaders,
        byte[][] signatures,
        TokenPolicy policy,
        bool detached)
    {
        if (protectedHeaders == null)
        {
            throw new ArgumentNullException("protectedHeaders");
        }

        if (signatures == null)
        {
            throw new ArgumentNullException("signatures");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult counts = ValidateCounts(
            protectedHeaders.Length,
            unprotectedHeaders,
            signatures.Length);
        if (!counts.Success)
        {
            return TextFailure(counts.Error);
        }

        TokenResult encoding = ValidatePayloadEncoding(protectedHeaders);
        if (!encoding.Success)
        {
            return TextFailure(encoding.Error);
        }

        string[] flattened = new string[protectedHeaders.Length];
        int index = 0;
        while (index < protectedHeaders.Length)
        {
            if (signatures[index] == null)
            {
                return TextFailure(
                    "invalid_signature",
                    "A general JWS signature entry is missing.");
            }

            JoseHeader? unprotected = GetUnprotected(unprotectedHeaders, index);
            TokenTextResult written = detached
                ? JsonWebSignatureFlattened.WriteDetached(
                    protectedHeaders[index],
                    unprotected,
                    signatures[index],
                    policy)
                : JsonWebSignatureFlattened.Write(
                    payload,
                    protectedHeaders[index],
                    unprotected,
                    signatures[index],
                    policy);
            if (!written.Success)
            {
                return written;
            }

            flattened[index] = (written.Text ?? throw new InvalidOperationException("A required value is null."));
            index += 1;
        }

        return CombineFlattened(flattened, detached, policy);
    }

    static JwsGeneralResult VerifyCore(
        string json,
        byte[] externalPayload,
        TokenKey[] keys,
        TokenPolicy policy,
        bool detached)
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

        JwsGeneralResult parsed = ReadCore(json, policy, false);
        if (!parsed.Success)
        {
            return parsed;
        }

        int unsecuredIndex = 0;
        while (unsecuredIndex < (parsed.Token ?? throw new InvalidOperationException("A required value is null.")).Signatures.Length)
        {
            if (parsed.Token.Signatures[unsecuredIndex].Unsecured)
            {
                return Failure(
                    "unsecured_jws_not_supported",
                    "Ordinary JWS verification does not accept unsecured tokens.");
            }

            unsecuredIndex += 1;
        }

        if (detached != parsed.Token.DetachedPayload)
        {
            return Failure(
                detached ? "invalid_detached_jws" : "missing_payload",
                detached
                    ? "Detached verification requires the payload member to be absent."
                    : "Attached verification requires a payload member.");
        }

        if (keys.Length != parsed.Token.Signatures.Length)
        {
            return Failure(
                "signature_key_count_mismatch",
                "A verification key is required for every signature entry.");
        }

        int index = 0;
        while (index < keys.Length)
        {
            if (keys[index] == null)
            {
                return Failure(
                    "invalid_key",
                    "A verification key is missing.");
            }

            JwsFlattenedResult verified = detached
                ? JsonWebSignatureFlattened.VerifyDetached(
                    (parsed.Token.Signatures[index].FlattenedJson ?? throw new InvalidOperationException("A required value is null.")),
                    externalPayload,
                    keys[index],
                    policy)
                : JsonWebSignatureFlattened.Verify(
                    (parsed.Token.Signatures[index].FlattenedJson ?? throw new InvalidOperationException("A required value is null.")),
                    keys[index],
                    policy);
            if (!verified.Success)
            {
                return Failure(verified.Error);
            }

            parsed.Token.Signatures[index].SignatureVerified = true;
            index += 1;
        }

        parsed.Token.Payload = detached
            ? externalPayload.AsSpan().ToArray()
            : parsed.Token.ParsedPayload.AsSpan().ToArray();
        parsed.Token.PayloadAvailable = true;
        return parsed;
    }

    static JwsGeneralResult ReadCore(
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
                "Diagnostic JWS payload decoding requires explicit policy opt-in.");
        }

        TokenBytesResult encodedJson = Utf8Codec.Encode(json);
        if (!encodedJson.Success)
        {
            return Failure(encodedJson.Error);
        }

        if ((encodedJson.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length > policy.MaximumTokenSizeInBytes)
        {
            return Failure(
                "token_too_large",
                "The general JWS exceeds the configured size limit.");
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

        JsonValue? signaturesValue = (root.Object ?? throw new InvalidOperationException("A required value is null.")).Get("signatures");
        if (signaturesValue == null
            || signaturesValue.Kind != TokenJsonValueKind.Array
            || signaturesValue.ArrayValue == null
            || signaturesValue.ArrayValue.Length == 0)
        {
            return Failure(
                "invalid_signatures",
                "A general JWS requires a nonempty signatures array.");
        }

        JsonValue? payloadValue = root.Object.Get("payload");
        bool detached = payloadValue == null;
        if (!detached
            && ((payloadValue ?? throw new InvalidOperationException("A required value is null.")).Kind != TokenJsonValueKind.String
                || payloadValue.StringValue == null))
        {
            return Failure(
                "invalid_payload",
                "The general JWS payload member must be a string.");
        }

        JwsGeneralSignature[] signatures = new JwsGeneralSignature[
            signaturesValue.ArrayValue.Length];
        byte[] parsedPayload = Array.Empty<byte>();
        bool encodePayload = true;
        bool unsecured = true;
        int index = 0;
        while (index < signatures.Length)
        {
            JsonValue entry = signaturesValue.ArrayValue[index];
            if (entry == null
                || entry.Kind != TokenJsonValueKind.Object
                || entry.ObjectValue == null)
            {
                return Failure(
                    "invalid_signature_entry",
                    "Each general JWS signature entry must be an object.");
            }

            TokenResult entryShape = ValidateSignatureShape(entry.ObjectValue);
            if (!entryShape.Success)
            {
                return Failure(entryShape.Error);
            }

            TokenTextResult flattened = CreateFlattenedJson(
                payloadValue,
                entry.ObjectValue);
            if (!flattened.Success)
            {
                return Failure(flattened.Error);
            }

            JwsFlattenedResult parsed = JsonWebSignatureFlattened.Parse(
                (flattened.Text ?? throw new InvalidOperationException("A required value is null.")),
                policy);
            if (!parsed.Success)
            {
                return Failure(parsed.Error);
            }

            if (index == 0)
            {
                encodePayload = (parsed.Token ?? throw new InvalidOperationException("A required value is null.")).PayloadBase64UrlEncoded;
                parsedPayload = parsed.Token.ParsedPayload.AsSpan().ToArray();
            }
            else if (encodePayload != (parsed.Token ?? throw new InvalidOperationException("A required value is null.")).PayloadBase64UrlEncoded)
            {
                return Failure(
                    "inconsistent_payload_encoding",
                    "All signatures must use the same b64 payload encoding value.");
            }

            signatures[index] = FromFlattened(parsed.Token, flattened.Text);
            if (!parsed.Token.Unsecured)
            {
                unsecured = false;
            }

            index += 1;
        }

        JwsGeneralToken token = new JwsGeneralToken();
        token.DetachedPayload = detached;
        token.PayloadBase64UrlEncoded = encodePayload;
        token.Unsecured = unsecured;
        token.Signatures = signatures;
        token.ParsedPayload = parsedPayload;
        if (diagnostic && !detached)
        {
            token.Payload = parsedPayload.AsSpan().ToArray();
            token.PayloadAvailable = true;
        }

        JwsGeneralResult result = new JwsGeneralResult();
        result.Token = token;
        result.Success = true;
        return result;
    }

    static TokenTextResult CombineFlattened(
        string[] flattened,
        bool detached,
        TokenPolicy policy)
    {
        JsonObject[] objects = new JsonObject[flattened.Length];
        string? payload = null;
        int index = 0;
        while (index < flattened.Length)
        {
            JsonObjectResult parsed = JsonObject.Read(
                flattened[index],
                policy.MaximumJsonDepth);
            if (!parsed.Success)
            {
                return TextFailure(parsed.Error);
            }

            objects[index] = (parsed.Object ?? throw new InvalidOperationException("A required value is null."));
            if (!detached)
            {
                string? currentPayload = (parsed.Object.Get("payload") ?? throw new InvalidOperationException("A required value is null.")).StringValue;
                if (index == 0)
                {
                    payload = currentPayload;
                }
                else if (!string.Equals(payload, currentPayload, StringComparison.Ordinal))
                {
                    return TextFailure(
                        "inconsistent_payload_encoding",
                        "All signatures must use the same payload representation.");
                }
            }

            index += 1;
        }

        StringBuilder builder = new StringBuilder();
        builder.Append('{');
        if (!detached)
        {
            builder.Append("\"payload\":");
            builder.Append(JsonWriter.WriteStringValue(payload).Text);
            builder.Append(',');
        }

        builder.Append("\"signatures\":[");
        index = 0;
        while (index < objects.Length)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            AppendSignatureEntry(builder, objects[index]);
            index += 1;
        }

        builder.Append("]}");
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
                "The general JWS exceeds the configured size limit.");
        }

        JsonObjectResult bounded = JsonObject.Read(
            json,
            policy.MaximumJsonDepth);
        if (!bounded.Success)
        {
            return TextFailure(bounded.Error);
        }

        TokenTextResult result = new TokenTextResult();
        result.Text = json;
        result.Success = true;
        return result;
    }

    static void AppendSignatureEntry(StringBuilder builder, JsonObject value)
    {
        builder.Append("{\"protected\":");
        builder.Append(JsonWriter.WriteStringValue(
            (value.Get("protected") ?? throw new InvalidOperationException("A required value is null.")).StringValue).Text);
        JsonValue? header = value.Get("header");
        if (header != null)
        {
            JoseHeaderResult converted = JoseHeader.FromObject((header.ObjectValue ?? throw new InvalidOperationException("A required value is null.")));
            builder.Append(",\"header\":");
            builder.Append((converted.Header ?? throw new InvalidOperationException("A required value is null.")).Write().Text);
        }

        builder.Append(",\"signature\":");
        builder.Append(JsonWriter.WriteStringValue(
            (value.Get("signature") ?? throw new InvalidOperationException("A required value is null.")).StringValue).Text);
        builder.Append('}');
    }

    static TokenTextResult CreateFlattenedJson(
        JsonValue? payload,
        JsonObject signature)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append('{');
        if (payload != null)
        {
            builder.Append("\"payload\":");
            builder.Append(JsonWriter.WriteStringValue(payload.StringValue).Text);
            builder.Append(',');
        }

        JsonValue? protectedValue = signature.Get("protected");
        JsonValue? signatureValue = signature.Get("signature");
        if (protectedValue == null
            || protectedValue.Kind != TokenJsonValueKind.String
            || protectedValue.StringValue == null
            || signatureValue == null
            || signatureValue.Kind != TokenJsonValueKind.String
            || signatureValue.StringValue == null)
        {
            return TextFailure(
                "invalid_signature_entry",
                "A signature entry requires protected and signature strings.");
        }

        builder.Append("\"protected\":");
        builder.Append(JsonWriter.WriteStringValue(protectedValue.StringValue).Text);
        JsonValue? header = signature.Get("header");
        if (header != null)
        {
            if (header.Kind != TokenJsonValueKind.Object
                || header.ObjectValue == null)
            {
                return TextFailure(
                    "invalid_unprotected_header",
                    "A signature header member must be an object.");
            }

            JoseHeaderResult converted = JoseHeader.FromObject(header.ObjectValue);
            if (!converted.Success)
            {
                return TextFailure(converted.Error);
            }

            builder.Append(",\"header\":");
            builder.Append((converted.Header ?? throw new InvalidOperationException("A required value is null.")).Write().Text);
        }

        builder.Append(",\"signature\":");
        builder.Append(JsonWriter.WriteStringValue(signatureValue.StringValue).Text);
        builder.Append('}');
        TokenTextResult result = new TokenTextResult();
        result.Text = builder.ToString();
        result.Success = true;
        return result;
    }

    static JwsGeneralSignature FromFlattened(
        JwsFlattenedToken token,
        string flattenedJson)
    {
        JwsGeneralSignature result = new JwsGeneralSignature();
        result.ProtectedHeader = token.ProtectedHeader;
        result.UnprotectedHeader = token.UnprotectedHeader;
        result.Algorithm = token.Algorithm;
        result.AlgorithmMetadata = token.AlgorithmMetadata;
        result.AlgorithmAvailable = token.AlgorithmAvailable;
        result.Unsecured = token.Unsecured;
        result.KeyId = token.KeyId;
        result.Signature = token.Signature;
        result.FlattenedJson = flattenedJson;
        return result;
    }

    static TokenResult ValidateRootShape(JsonObject? root)
    {
        int index = 0;
        while (index < (root ?? throw new InvalidOperationException("A required value is null.")).Members.Length)
        {
            string? name = root.Members[index].Name;
            if (!string.Equals(name, "payload", StringComparison.Ordinal)
                && !string.Equals(name, "signatures", StringComparison.Ordinal))
            {
                return ResultFailure(
                    "invalid_jws_json_member",
                    "The general JWS contains an unexpected member.");
            }

            index += 1;
        }

        return Success();
    }

    static TokenResult ValidateSignatureShape(JsonObject signature)
    {
        int index = 0;
        while (index < signature.Members.Length)
        {
            string? name = signature.Members[index].Name;
            if (!string.Equals(name, "protected", StringComparison.Ordinal)
                && !string.Equals(name, "header", StringComparison.Ordinal)
                && !string.Equals(name, "signature", StringComparison.Ordinal))
            {
                return ResultFailure(
                    "invalid_signature_entry",
                    "A general JWS signature entry contains an unexpected member.");
            }

            index += 1;
        }

        return Success();
    }

    static TokenResult ValidatePayloadEncoding(JoseHeader[] protectedHeaders)
    {
        if (protectedHeaders.Length == 0)
        {
            return ResultFailure(
                "invalid_signatures",
                "A general JWS requires at least one signature.");
        }

        bool encodePayload = true;
        int index = 0;
        while (index < protectedHeaders.Length)
        {
            if (protectedHeaders[index] == null)
            {
                return ResultFailure(
                    "invalid_protected_header",
                    "A protected header is missing.");
            }

            bool current = JsonWebSignature.IsPayloadBase64UrlEncoded(
                protectedHeaders[index]);
            if (index == 0)
            {
                encodePayload = current;
            }
            else if (encodePayload != current)
            {
                return ResultFailure(
                    "inconsistent_payload_encoding",
                    "All signatures must use the same b64 payload encoding value.");
            }

            index += 1;
        }

        return Success();
    }

    static TokenResult ValidateCounts(
        int signatureCount,
        JoseHeader?[]? unprotectedHeaders,
        int valueCount)
    {
        if (signatureCount == 0)
        {
            return ResultFailure(
                "invalid_signatures",
                "A general JWS requires at least one signature.");
        }

        if (valueCount != signatureCount
            || unprotectedHeaders != null
                && unprotectedHeaders.Length != signatureCount)
        {
            return ResultFailure(
                "signature_count_mismatch",
                "Protected headers, unprotected headers, and signature values must align.");
        }

        return Success();
    }

    static JoseHeader? GetUnprotected(
        JoseHeader?[]? unprotectedHeaders,
        int index)
    {
        return unprotectedHeaders == null ? null : unprotectedHeaders[index];
    }

    static void ValidateSignArguments(
        JoseHeader[] protectedHeaders,
        TokenKey[] keys,
        TokenPolicy policy)
    {
        if (protectedHeaders == null)
        {
            throw new ArgumentNullException("protectedHeaders");
        }

        if (keys == null)
        {
            throw new ArgumentNullException("keys");
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

    static JwsGeneralResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static JwsGeneralResult Failure(TokenError? error)
    {
        JwsGeneralResult result = new JwsGeneralResult();
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
