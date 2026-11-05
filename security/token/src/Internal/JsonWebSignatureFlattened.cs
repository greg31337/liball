using System;
using System.Text;

namespace token;

/// <summary>Reads, writes, signs, and verifies flattened JWS JSON serializations.</summary>
internal static class JsonWebSignatureFlattened
{
    /// <summary>Creates an explicitly policy-authorized unsecured flattened JWS.</summary>
    /// <param name="payload">The attached payload bytes.</param>
    /// <param name="protectedHeader">The protected header whose algorithm must be <c>none</c>.</param>
    /// <param name="unprotectedHeader">The unprotected header, or <c>null</c>.</param>
    /// <param name="policy">The policy that must allow unsecured tokens and the <c>none</c> algorithm.</param>
    /// <returns>A result containing unsecured flattened JWS JSON.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static TokenTextResult CreateUnsecured(
        byte[] payload,
        JoseHeader protectedHeader,
        JoseHeader? unprotectedHeader,
        TokenPolicy policy)
    {
        if (payload == null)
        {
            throw new ArgumentNullException("payload");
        }

        if (protectedHeader == null)
        {
            throw new ArgumentNullException("protectedHeader");
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

        TokenResult algorithm = JsonWebSignature.ValidateUnsecuredAlgorithm(
            protectedHeader);
        if (!algorithm.Success)
        {
            return TextFailure(algorithm.Error);
        }

        return WriteCore(
            payload.AsSpan(),
            protectedHeader,
            unprotectedHeader,
            Array.Empty<byte>(),
            policy,
            false,
            true);
    }

    /// <summary>Parses a flattened JWS using conservative default limits.</summary>
    /// <param name="json">The flattened JWS JSON text.</param>
    /// <returns>A result containing unverified metadata or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>json</c> is null.</exception>
    internal static JwsFlattenedResult Parse(string json)
    {
        return Parse(json, new TokenPolicy());
    }

    /// <summary>Parses a flattened JWS while withholding unauthenticated payload bytes.</summary>
    /// <param name="json">The flattened JWS JSON text.</param>
    /// <param name="policy">The header and size policy.</param>
    /// <returns>A result containing unverified metadata or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwsFlattenedResult Parse(string json, TokenPolicy policy)
    {
        return ReadCore(json, policy, false);
    }

    /// <summary>Reads an attached flattened JWS payload when diagnostic access is enabled.</summary>
    /// <param name="json">The flattened JWS JSON text.</param>
    /// <param name="policy">The policy that must permit unauthenticated decode.</param>
    /// <returns>A result containing unverified payload and metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwsFlattenedResult ReadDiagnostic(
        string json,
        TokenPolicy policy)
    {
        return ReadCore(json, policy, true);
    }

    /// <summary>Writes an attached flattened JWS from decoded components.</summary>
    /// <param name="payload">The payload bytes.</param>
    /// <param name="protectedHeader">The integrity-protected header.</param>
    /// <param name="unprotectedHeader">The unprotected header, or <c>null</c>.</param>
    /// <param name="signature">The signature or MAC bytes.</param>
    /// <param name="policy">The header and size policy.</param>
    /// <returns>A result containing flattened JWS JSON.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static TokenTextResult Write(
        byte[] payload,
        JoseHeader protectedHeader,
        JoseHeader? unprotectedHeader,
        byte[] signature,
        TokenPolicy policy)
    {
        if (payload == null)
        {
            throw new ArgumentNullException("payload");
        }

        return WriteCore(
            payload.AsSpan(),
            protectedHeader,
            unprotectedHeader,
            signature,
            policy,
            false,
            false);
    }

    /// <summary>Writes a detached flattened JWS from decoded components.</summary>
    /// <param name="protectedHeader">The integrity-protected header.</param>
    /// <param name="unprotectedHeader">The unprotected header, or <c>null</c>.</param>
    /// <param name="signature">The signature or MAC bytes.</param>
    /// <param name="policy">The header and size policy.</param>
    /// <returns>A result containing flattened JWS JSON without a payload member.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static TokenTextResult WriteDetached(
        JoseHeader protectedHeader,
        JoseHeader? unprotectedHeader,
        byte[] signature,
        TokenPolicy policy)
    {
        return WriteCore(
            Array.Empty<byte>().AsSpan(),
            protectedHeader,
            unprotectedHeader,
            signature,
            policy,
            true,
            false);
    }

    /// <summary>Signs an attached payload as a flattened JWS JSON serialization.</summary>
    /// <param name="payload">The payload bytes.</param>
    /// <param name="protectedHeader">The integrity-protected header.</param>
    /// <param name="unprotectedHeader">The unprotected header, or <c>null</c>.</param>
    /// <param name="key">The signing key.</param>
    /// <param name="policy">The algorithm, header, and size policy.</param>
    /// <returns>A result containing signed flattened JWS JSON.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static TokenTextResult Sign(
        byte[] payload,
        JoseHeader protectedHeader,
        JoseHeader? unprotectedHeader,
        TokenKey key,
        TokenPolicy policy)
    {
        if (payload == null)
        {
            throw new ArgumentNullException("payload");
        }

        return SignCore(
            payload.AsSpan(),
            protectedHeader,
            unprotectedHeader,
            key,
            policy,
            false);
    }

    /// <summary>Signs a detached payload as a flattened JWS JSON serialization.</summary>
    /// <param name="payload">The external payload bytes.</param>
    /// <param name="protectedHeader">The integrity-protected header.</param>
    /// <param name="unprotectedHeader">The unprotected header, or <c>null</c>.</param>
    /// <param name="key">The signing key.</param>
    /// <param name="policy">The algorithm, header, and size policy.</param>
    /// <returns>A result containing signed flattened JWS JSON without a payload member.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static TokenTextResult SignDetached(
        byte[] payload,
        JoseHeader protectedHeader,
        JoseHeader? unprotectedHeader,
        TokenKey key,
        TokenPolicy policy)
    {
        if (payload == null)
        {
            throw new ArgumentNullException("payload");
        }

        return SignCore(
            payload.AsSpan(),
            protectedHeader,
            unprotectedHeader,
            key,
            policy,
            true);
    }

    /// <summary>Verifies an attached flattened JWS and exposes its authenticated payload.</summary>
    /// <param name="json">The flattened JWS JSON text.</param>
    /// <param name="key">The verification key.</param>
    /// <param name="policy">The algorithm, header, and size policy.</param>
    /// <returns>A result containing authenticated payload and metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwsFlattenedResult Verify(
        string json,
        TokenKey key,
        TokenPolicy policy)
    {
        return VerifyCore(
            json,
            Array.Empty<byte>().AsSpan(),
            key,
            policy,
            false);
    }

    /// <summary>Validates an explicitly policy-authorized unsecured flattened JWS.</summary>
    /// <param name="json">The unsecured flattened JWS JSON text.</param>
    /// <param name="policy">The policy that must allow unsecured tokens and the <c>none</c> algorithm.</param>
    /// <returns>A result containing the policy-validated attached payload.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwsFlattenedResult ValidateUnsecured(
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

        JwsFlattenedResult parsed = ReadCore(json, policy, false);
        if (!parsed.Success)
        {
            return parsed;
        }

        if (!(parsed.Token ?? throw new InvalidOperationException("A required value is null.")).Unsecured)
        {
            return Failure(
                "invalid_unsecured_algorithm",
                "Unsecured validation requires the none algorithm.");
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

    /// <summary>Verifies a detached flattened JWS against external payload bytes.</summary>
    /// <param name="json">The flattened JWS JSON text without a payload member.</param>
    /// <param name="payload">The external payload bytes.</param>
    /// <param name="key">The verification key.</param>
    /// <param name="policy">The algorithm, header, and size policy.</param>
    /// <returns>A result containing authenticated payload and metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwsFlattenedResult VerifyDetached(
        string json,
        byte[] payload,
        TokenKey key,
        TokenPolicy policy)
    {
        if (payload == null)
        {
            throw new ArgumentNullException("payload");
        }

        return VerifyCore(json, payload.AsSpan(), key, policy, true);
    }

    static TokenTextResult SignCore(
        ReadOnlySpan<byte> payload,
        JoseHeader protectedHeader,
        JoseHeader? unprotectedHeader,
        TokenKey key,
        TokenPolicy policy,
        bool detached)
    {
        ValidateRequired(protectedHeader, key, policy);
        TokenResult validation = ValidateOperation(
            protectedHeader,
            unprotectedHeader,
            policy);
        if (!validation.Success)
        {
            return TextFailure(validation.Error);
        }

        if (payload.Length > policy.MaximumTokenSizeInBytes)
        {
            return TextFailure("token_too_large", "The JWS payload exceeds the configured size limit.");
        }

        string? algorithm = (protectedHeader.Get(JoseHeaderNames.Algorithm) ?? throw new InvalidOperationException("A required value is null.")).StringValue;
        TokenResult allowed = ValidateAlgorithmForOperation(algorithm, policy);
        if (!allowed.Success)
        {
            return TextFailure(allowed.Error);
        }

        TokenBytesResult protectedBytes = protectedHeader.WriteUtf8();
        if (!protectedBytes.Success)
        {
            return TextFailure(protectedBytes.Error);
        }

        string? protectedSegment = Base64Url.Encode((protectedBytes.Bytes ?? throw new InvalidOperationException("A required value is null."))).Text;
        TokenBytesResult signingInput = JsonWebSignature.CreateSigningInput(
            protectedSegment,
            payload,
            JsonWebSignature.IsPayloadBase64UrlEncoded(protectedHeader));
        if (!signingInput.Success)
        {
            return TextFailure(signingInput.Error);
        }

        TokenBytesResult signature = JsonWebSignature.SignBytes(
            signingInput.Bytes,
            key,
            algorithm);
        if (!signature.Success)
        {
            return TextFailure(signature.Error);
        }

        return WriteCore(
            payload,
            protectedHeader,
            unprotectedHeader,
            signature.Bytes,
            policy,
            detached,
            false);
    }

    static JwsFlattenedResult VerifyCore(
        string json,
        ReadOnlySpan<byte> externalPayload,
        TokenKey key,
        TokenPolicy policy,
        bool detached)
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

        JwsFlattenedResult parsed = ReadCore(json, policy, false);
        if (!parsed.Success)
        {
            return parsed;
        }

        if ((parsed.Token ?? throw new InvalidOperationException("A required value is null.")).Unsecured)
        {
            return Failure(
                "unsecured_jws_not_supported",
                "Ordinary JWS verification does not accept unsecured tokens.");
        }

        if (detached != parsed.Token.DetachedPayload)
        {
            return Failure(
                detached ? "invalid_detached_jws" : "missing_payload",
                detached
                    ? "Detached verification requires the payload member to be absent."
                    : "Attached verification requires a payload member.");
        }

        ReadOnlySpan<byte> payload = detached
            ? externalPayload
            : parsed.Token.ParsedPayload.AsSpan();
        if (payload.Length > policy.MaximumTokenSizeInBytes)
        {
            return Failure("token_too_large", "The JWS payload exceeds the configured size limit.");
        }

        TokenResult allowed = ValidateAlgorithmForOperation(
            parsed.Token.Algorithm,
            policy);
        if (!allowed.Success)
        {
            return Failure(allowed.Error);
        }

        TokenBytesResult signingInput = JsonWebSignature.CreateSigningInput(
            parsed.Token.ProtectedSegment,
            payload,
            parsed.Token.PayloadBase64UrlEncoded);
        if (!signingInput.Success)
        {
            return Failure(signingInput.Error);
        }

        TokenResult verified = JsonWebSignature.VerifyBytes(
            signingInput.Bytes,
            parsed.Token.Signature,
            key,
            parsed.Token.Algorithm);
        if (!verified.Success)
        {
            return Failure(verified.Error);
        }

        parsed.Token.Payload = payload.ToArray();
        parsed.Token.PayloadAvailable = true;
        parsed.Token.SignatureVerified = true;
        return parsed;
    }

    static JwsFlattenedResult ReadCore(
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

        TokenBytesResult jsonBytes = Utf8Codec.Encode(json);
        if (!jsonBytes.Success)
        {
            return Failure(jsonBytes.Error);
        }

        if ((jsonBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length > policy.MaximumTokenSizeInBytes)
        {
            return Failure("token_too_large", "The flattened JWS exceeds the configured size limit.");
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
            return Failure("invalid_protected_header", "The protected member must be a nonempty string.");
        }
        else
        {
            protectedSegment = protectedValue.StringValue;
            TokenBytesResult protectedBytes = Base64Url.Decode(
                protectedSegment);
            if (!protectedBytes.Success)
            {
                return Failure("invalid_protected_header", "The protected member is not canonical base64url.");
            }

            protectedHeader = JoseHeader.Read(
                (protectedBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                policy.MaximumJsonDepth);
            if (!protectedHeader.Success)
            {
                return Failure(protectedHeader.Error);
            }
        }

        JoseHeader? unprotectedHeader = null;
        JsonValue? headerValue = root.Object.Get("header");
        if (headerValue != null)
        {
            if (headerValue.Kind != TokenJsonValueKind.Object
                || headerValue.ObjectValue == null)
            {
                return Failure("invalid_unprotected_header", "The header member must be an object.");
            }

            JoseHeaderResult converted = JoseHeader.FromObject(headerValue.ObjectValue);
            if (!converted.Success)
            {
                return Failure(converted.Error);
            }

            unprotectedHeader = converted.Header;
        }

        TokenResult headers = ValidateHeaders(
            protectedHeader.Header,
            unprotectedHeader,
            policy.UnderstoodCriticalHeaders);
        if (!headers.Success)
        {
            return Failure(headers.Error);
        }

        JsonValue? algorithmValue = (protectedHeader.Header ?? throw new InvalidOperationException("A required value is null.")).Get(
            JoseHeaderNames.Algorithm);
        if (algorithmValue == null && unprotectedHeader != null)
        {
            algorithmValue = unprotectedHeader.Get(
                JoseHeaderNames.Algorithm);
        }

        if (algorithmValue == null)
        {
            return Failure("missing_algorithm", "The JOSE header requires alg.");
        }

        string? algorithm = algorithmValue.StringValue;
        TokenResult algorithmResult = ValidateAlgorithmShape(algorithm);
        if (!algorithmResult.Success)
        {
            return Failure(algorithmResult.Error);
        }

        JsonValue? signatureValue = root.Object.Get("signature");
        if (signatureValue == null
            || signatureValue.Kind != TokenJsonValueKind.String
            || signatureValue.StringValue == null)
        {
            return Failure("missing_signature", "A flattened JWS requires a signature string.");
        }

        TokenBytesResult signature = Base64Url.Decode(signatureValue.StringValue);
        if (!signature.Success)
        {
            return Failure("invalid_signature", "The signature member is not canonical base64url.");
        }

        bool unsecured = string.Equals(
            algorithm,
            JoseAlgorithms.None,
            StringComparison.Ordinal);
        if (unsecured && (signature.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length != 0)
        {
            return Failure(
                "invalid_unsecured_signature",
                "An unsecured JWS signature value must be empty.");
        }

        if (!unsecured && (signature.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length == 0)
        {
            return Failure("missing_signature", "A signed flattened JWS requires a signature value.");
        }

        bool encodePayload = JsonWebSignature.IsPayloadBase64UrlEncoded(
            protectedHeader.Header);
        JsonValue? payloadValue = root.Object.Get("payload");
        bool detached = payloadValue == null;
        byte[]? payload = Array.Empty<byte>();
        if (!detached)
        {
            if ((payloadValue ?? throw new InvalidOperationException("A required value is null.")).Kind != TokenJsonValueKind.String
                || payloadValue.StringValue == null)
            {
                return Failure("invalid_payload", "The payload member must be a string.");
            }

            TokenBytesResult decoded = encodePayload
                ? Base64Url.Decode(payloadValue.StringValue)
                : Utf8Codec.Encode(payloadValue.StringValue);
            if (!decoded.Success)
            {
                return Failure(
                    encodePayload ? "invalid_payload" : (decoded.Error ?? throw new InvalidOperationException("A required value is null.")).Code,
                    encodePayload
                        ? "The payload member is not canonical base64url."
                        : (decoded.Error ?? throw new InvalidOperationException("A required value is null.")).Message);
            }

            payload = decoded.Bytes;
        }

        JwsFlattenedToken token = new JwsFlattenedToken();
        token.ProtectedHeader = protectedHeader.Header;
        token.UnprotectedHeader = unprotectedHeader;
        token.Algorithm = algorithm;
        token.AlgorithmMetadata = AlgorithmMetadataRegistry.GetJose((algorithm ?? throw new InvalidOperationException("A required value is null.")));
        token.AlgorithmAvailable = CryptoAlgorithmAvailability.IsJoseAlgorithmAvailable(
            algorithm);
        token.DetachedPayload = detached;
        token.PayloadBase64UrlEncoded = encodePayload;
        token.Unsecured = unsecured;
        token.Signature = signature.Bytes;
        token.ProtectedSegment = protectedSegment;
        token.ParsedPayload = payload;
        token.KeyId = GetKeyId(protectedHeader.Header, unprotectedHeader);
        if (diagnostic && !detached)
        {
            token.Payload = payload.AsSpan().ToArray();
            token.PayloadAvailable = true;
        }

        JwsFlattenedResult result = new JwsFlattenedResult();
        result.Token = token;
        result.Success = true;
        return result;
    }

    static TokenTextResult WriteCore(
        ReadOnlySpan<byte> payload,
        JoseHeader protectedHeader,
        JoseHeader? unprotectedHeader,
        byte[]? signature,
        TokenPolicy policy,
        bool detached,
        bool unsecured)
    {
        if (protectedHeader == null)
        {
            throw new ArgumentNullException("protectedHeader");
        }

        if (signature == null)
        {
            throw new ArgumentNullException("signature");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult validation = ValidateOperation(
            protectedHeader,
            unprotectedHeader,
            policy);
        if (!validation.Success)
        {
            return TextFailure(validation.Error);
        }

        JsonValue? algorithmValue = protectedHeader.Get(JoseHeaderNames.Algorithm);
        bool none = algorithmValue != null
            && string.Equals(
                algorithmValue.StringValue,
                JoseAlgorithms.None,
                StringComparison.Ordinal);
        if (!unsecured && none)
        {
            return TextFailure(
                "unsecured_jws_not_supported",
                "Ordinary flattened JWS writing does not create unsecured tokens.");
        }

        if (unsecured && (!none || signature.Length != 0))
        {
            return TextFailure(
                "invalid_unsecured_signature",
                "An unsecured flattened JWS requires none and an empty signature.");
        }

        if (!unsecured && signature.Length == 0)
        {
            return TextFailure("missing_signature", "A signed flattened JWS requires a signature value.");
        }

        if (!detached && payload.Length > policy.MaximumTokenSizeInBytes
            || signature.Length > policy.MaximumTokenSizeInBytes)
        {
            return TextFailure("token_too_large", "A flattened JWS component exceeds the configured size limit.");
        }

        TokenBytesResult protectedBytes = protectedHeader.WriteUtf8();
        if (!protectedBytes.Success)
        {
            return TextFailure(protectedBytes.Error);
        }

        string? protectedSegment = Base64Url.Encode((protectedBytes.Bytes ?? throw new InvalidOperationException("A required value is null."))).Text;
        string? signatureSegment = Base64Url.Encode(signature).Text;
        StringBuilder builder = new StringBuilder();
        builder.Append('{');
        if (!detached)
        {
            string? payloadText;
            if (JsonWebSignature.IsPayloadBase64UrlEncoded(protectedHeader))
            {
                payloadText = Base64Url.Encode(payload).Text;
            }
            else
            {
                TokenTextResult decoded = Utf8Codec.Decode(payload);
                if (!decoded.Success)
                {
                    return TextFailure(decoded.Error);
                }

                payloadText = decoded.Text;
            }

            builder.Append("\"payload\":");
            builder.Append(JsonWriter.WriteStringValue(payloadText).Text);
            builder.Append(',');
        }

        builder.Append("\"protected\":");
        builder.Append(JsonWriter.WriteStringValue(protectedSegment).Text);
        if (unprotectedHeader != null)
        {
            TokenTextResult headerText = unprotectedHeader.Write();
            if (!headerText.Success)
            {
                return TextFailure(headerText.Error);
            }

            builder.Append(",\"header\":");
            builder.Append(headerText.Text);
        }

        builder.Append(",\"signature\":");
        builder.Append(JsonWriter.WriteStringValue(signatureSegment).Text);
        builder.Append('}');
        string json = builder.ToString();
        TokenBytesResult encodedJson = Utf8Codec.Encode(json);
        if (!encodedJson.Success)
        {
            return TextFailure(encodedJson.Error);
        }

        if ((encodedJson.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length > policy.MaximumTokenSizeInBytes)
        {
            return TextFailure("token_too_large", "The flattened JWS exceeds the configured size limit.");
        }

        TokenTextResult result = new TokenTextResult();
        result.Text = json;
        result.Success = true;
        return result;
    }

    static TokenResult ValidateOperation(
        JoseHeader protectedHeader,
        JoseHeader? unprotectedHeader,
        TokenPolicy policy)
    {
        TokenResult policyResult = JsonWebSignature.ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return policyResult;
        }

        TokenBytesResult protectedBytes = protectedHeader.WriteUtf8();
        if (!protectedBytes.Success)
        {
            return ResultFailure(protectedBytes.Error);
        }

        JoseHeaderResult bounded = JoseHeader.Read(
            (protectedBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")),
            policy.MaximumJsonDepth);
        if (!bounded.Success)
        {
            return ResultFailure(bounded.Error);
        }

        JoseHeader? boundedUnprotected = null;
        if (unprotectedHeader != null)
        {
            TokenBytesResult unprotectedBytes = unprotectedHeader.WriteUtf8();
            if (!unprotectedBytes.Success)
            {
                return ResultFailure(unprotectedBytes.Error);
            }

            JoseHeaderResult unprotectedBounded = JoseHeader.Read(
                (unprotectedBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")),
                policy.MaximumJsonDepth);
            if (!unprotectedBounded.Success)
            {
                return ResultFailure(unprotectedBounded.Error);
            }

            boundedUnprotected = unprotectedBounded.Header;
        }

        TokenResult headers = ValidateHeaders(
            bounded.Header,
            boundedUnprotected,
            policy.UnderstoodCriticalHeaders);
        if (!headers.Success)
        {
            return headers;
        }

        JsonValue? algorithm = (bounded.Header ?? throw new InvalidOperationException("A required value is null.")).Get(JoseHeaderNames.Algorithm);
        if (algorithm == null)
        {
            return ResultFailure("missing_algorithm", "The protected header requires alg.");
        }

        return ValidateAlgorithmShape(algorithm.StringValue);
    }

    static TokenResult ValidateHeaders(
        JoseHeader? protectedHeader,
        JoseHeader? unprotectedHeader,
        string[]? understood)
    {
        TokenResult protectedResult = JsonWebSignature.ValidateHeader(
            protectedHeader,
            understood);
        if (!protectedResult.Success)
        {
            return protectedResult;
        }

        if (unprotectedHeader == null)
        {
            return Success();
        }

        int index = 0;
        while (index < unprotectedHeader.Parameters.Members.Length)
        {
            string? name = unprotectedHeader.Parameters.Members[index].Name;
            if (JoseHeaderNamesOperations.IsRegistered((name ?? throw new InvalidOperationException("A required value is null.")))
                && !JoseHeaderNamesOperations.IsForJws(name))
            {
                return ResultFailure(
                    "invalid_jws_header_parameter",
                    "A registered unprotected parameter is not defined for JWS.");
            }

            if (string.Equals(name, JoseHeaderNames.Base64UrlEncodePayload, StringComparison.Ordinal))
            {
                return ResultFailure(
                    "payload_encoding_not_protected",
                    "The JWS b64 parameter must be integrity protected.");
            }

            index += 1;
        }

        string[]? criticalUnderstood = understood;
        if ((protectedHeader ?? throw new InvalidOperationException("A required value is null.")).Get(JoseHeaderNames.Base64UrlEncodePayload) != null)
        {
            criticalUnderstood = JsonWebSignature.AddUnderstoodPayloadEncoding(
                understood);
        }

        return JoseCriticalHeaderValidator.Validate(
            protectedHeader,
            new JoseHeader[] { unprotectedHeader },
            (criticalUnderstood ?? throw new InvalidOperationException("A required value is null.")));
    }

    static TokenResult ValidateShape(JsonObject? root)
    {
        int index = 0;
        while (index < (root ?? throw new InvalidOperationException("A required value is null.")).Members.Length)
        {
            string? name = root.Members[index].Name;
            if (!string.Equals(name, "payload", StringComparison.Ordinal)
                && !string.Equals(name, "protected", StringComparison.Ordinal)
                && !string.Equals(name, "header", StringComparison.Ordinal)
                && !string.Equals(name, "signature", StringComparison.Ordinal))
            {
                return ResultFailure(
                    "invalid_jws_json_member",
                    "The flattened JWS contains an unexpected member.");
            }

            index += 1;
        }

        return Success();
    }

    static TokenResult ValidateAlgorithmShape(string? algorithm)
    {
        if (algorithm == null || algorithm.Length == 0)
        {
            return ResultFailure("invalid_algorithm", "The JWS algorithm must not be empty.");
        }

        AlgorithmMetadata metadata = AlgorithmMetadataRegistry.GetJose(algorithm);
        if (metadata.Known && !JoseAlgorithmsOperations.IsSignature(algorithm))
        {
            return ResultFailure(
                "invalid_jws_algorithm",
                "The registered JOSE algorithm is not a JWS signature algorithm.");
        }

        return Success();
    }

    static TokenResult ValidateAlgorithmForOperation(
        string? algorithm,
        TokenPolicy policy)
    {
        TokenResult shape = ValidateAlgorithmShape(algorithm);
        if (!shape.Success)
        {
            return shape;
        }

        if (string.Equals(algorithm, JoseAlgorithms.None, StringComparison.Ordinal))
        {
            return ResultFailure(
                "unsecured_jws_not_supported",
                "Ordinary JWS cryptographic operations do not accept none.");
        }

        TokenResult availability = CryptoAlgorithmAvailability.CheckJoseAlgorithm(
            (algorithm ?? throw new InvalidOperationException("A required value is null.")));
        if (!availability.Success)
        {
            return availability;
        }

        if (!RegistryLookup.Contains(policy.AllowedAlgorithms, algorithm, false))
        {
            return ResultFailure(
                "algorithm_not_allowed",
                "The JWS algorithm is not allowed by policy.");
        }

        return Success();
    }

    static string? GetKeyId(
        JoseHeader protectedHeader,
        JoseHeader? unprotectedHeader)
    {
        JsonValue? keyId = protectedHeader.Get(JoseHeaderNames.KeyId);
        if (keyId == null && unprotectedHeader != null)
        {
            keyId = unprotectedHeader.Get(JoseHeaderNames.KeyId);
        }

        return keyId == null ? null : keyId.StringValue;
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
        return ResultFailure(new TokenError(code, message, -1));
    }

    static TokenResult ResultFailure(TokenError? error)
    {
        TokenResult result = new TokenResult();
        result.Error = error;
        return result;
    }

    static JwsFlattenedResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static JwsFlattenedResult Failure(TokenError? error)
    {
        JwsFlattenedResult result = new JwsFlattenedResult();
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
