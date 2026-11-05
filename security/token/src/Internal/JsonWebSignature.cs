using System;
namespace token;

/// <summary>
/// Parses, signs, verifies, and performs policy-gated diagnostic reads of compact JWS structures.
/// </summary>
internal static class JsonWebSignature
{
    /// <summary>Creates an explicitly policy-authorized unsecured compact JWS.</summary>
    /// <param name="payload">The payload bytes.</param>
    /// <param name="protectedHeader">The protected header whose algorithm must be <c>none</c>.</param>
    /// <param name="policy">The policy that must allow unsecured tokens and the <c>none</c> algorithm.</param>
    /// <returns>A result containing an unsecured compact JWS or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenTextResult CreateUnsecured(
        byte[] payload,
        JoseHeader protectedHeader,
        TokenPolicy policy)
    {
        if (payload == null)
        {
            throw new ArgumentNullException("payload");
        }

        return CreateUnsecured(payload.AsSpan(), protectedHeader, policy);
    }

    /// <summary>Creates an explicitly policy-authorized unsecured compact JWS from a payload span.</summary>
    /// <param name="payload">The payload bytes.</param>
    /// <param name="protectedHeader">The protected header whose algorithm must be <c>none</c>.</param>
    /// <param name="policy">The policy that must allow unsecured tokens and the <c>none</c> algorithm.</param>
    /// <returns>A result containing an unsecured compact JWS or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static TokenTextResult CreateUnsecured(
        ReadOnlySpan<byte> payload,
        JoseHeader protectedHeader,
        TokenPolicy policy)
    {
        if (protectedHeader == null)
        {
            throw new ArgumentNullException("protectedHeader");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult policyResult = ValidateUnsecuredPolicy(policy);
        if (!policyResult.Success)
        {
            return TextFailure(policyResult.Error);
        }

        TokenBytesResult headerBytes = protectedHeader.WriteUtf8();
        if (!headerBytes.Success)
        {
            return TextFailure(headerBytes.Error);
        }

        if ((headerBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length > policy.MaximumTokenSizeInBytes
            || payload.Length > policy.MaximumTokenSizeInBytes)
        {
            return TextFailure(
                "token_too_large",
                "The unsecured compact JWS exceeds the configured size limit.",
                policy.MaximumTokenSizeInBytes);
        }

        JoseHeaderResult bounded = JoseHeader.Read(
            headerBytes.Bytes,
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

        TokenResult algorithmResult = ValidateUnsecuredAlgorithm(bounded.Header);
        if (!algorithmResult.Success)
        {
            return TextFailure(algorithmResult.Error);
        }

        string? protectedSegment = Base64Url.Encode(headerBytes.Bytes).Text;
        string? payloadSegment;
        if (IsPayloadBase64UrlEncoded(bounded.Header))
        {
            payloadSegment = Base64Url.Encode(payload).Text;
        }
        else
        {
            TokenTextResult decoded = Utf8Codec.Decode(payload);
            if (!decoded.Success)
            {
                return TextFailure(decoded.Error);
            }

            int period = (decoded.Text ?? throw new InvalidOperationException("A required value is null.")).IndexOf('.');
            if (period >= 0)
            {
                return TextFailure(
                    "invalid_unencoded_payload",
                    "An attached unencoded compact JWS payload must not contain a period.",
                    period);
            }

            payloadSegment = decoded.Text;
        }

        string text = protectedSegment + "." + payloadSegment + ".";
        TokenBytesResult encodedText = Utf8Codec.Encode(text);
        if (!encodedText.Success)
        {
            return TextFailure(encodedText.Error);
        }

        if ((encodedText.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length > policy.MaximumTokenSizeInBytes)
        {
            return TextFailure(
                "token_too_large",
                "The unsecured compact JWS exceeds the configured size limit.",
                policy.MaximumTokenSizeInBytes);
        }

        return TextSuccess(text);
    }

    /// <summary>Creates a signed compact JWS from payload bytes and a protected header.</summary>
    /// <param name="payload">The payload bytes to integrity protect.</param>
    /// <param name="protectedHeader">The protected JOSE header containing the signing algorithm.</param>
    /// <param name="key">The signing key and its operation metadata.</param>
    /// <param name="policy">The explicit algorithm allowlist, critical extensions, and safety limits.</param>
    /// <returns>A result containing compact JWS text or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenTextResult Sign(
        byte[] payload,
        JoseHeader protectedHeader,
        TokenKey key,
        TokenPolicy policy)
    {
        if (payload == null)
        {
            throw new ArgumentNullException("payload");
        }

        return Sign(payload.AsSpan(), protectedHeader, key, policy);
    }

    /// <summary>Creates a signed compact JWS from a payload span and a protected header.</summary>
    /// <param name="payload">The payload bytes to integrity protect.</param>
    /// <param name="protectedHeader">The protected JOSE header containing the signing algorithm.</param>
    /// <param name="key">The signing key and its operation metadata.</param>
    /// <param name="policy">The explicit algorithm allowlist, critical extensions, and safety limits.</param>
    /// <returns>A result containing compact JWS text or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static TokenTextResult Sign(
        ReadOnlySpan<byte> payload,
        JoseHeader protectedHeader,
        TokenKey key,
        TokenPolicy policy)
    {
        return SignCore(payload, protectedHeader, key, policy, false);
    }

    /// <summary>Creates a compact JWS whose payload is authenticated but omitted from the serialization.</summary>
    /// <param name="payload">The detached payload bytes to integrity protect.</param>
    /// <param name="protectedHeader">The protected JOSE header containing the signing algorithm.</param>
    /// <param name="key">The signing key and its operation metadata.</param>
    /// <param name="policy">The explicit algorithm allowlist, critical extensions, and safety limits.</param>
    /// <returns>A result containing compact JWS text with an empty payload segment or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenTextResult SignDetached(
        byte[] payload,
        JoseHeader protectedHeader,
        TokenKey key,
        TokenPolicy policy)
    {
        if (payload == null)
        {
            throw new ArgumentNullException("payload");
        }

        return SignDetached(payload.AsSpan(), protectedHeader, key, policy);
    }

    /// <summary>Creates a compact JWS whose payload span is authenticated but omitted from the serialization.</summary>
    /// <param name="payload">The detached payload bytes to integrity protect.</param>
    /// <param name="protectedHeader">The protected JOSE header containing the signing algorithm.</param>
    /// <param name="key">The signing key and its operation metadata.</param>
    /// <param name="policy">The explicit algorithm allowlist, critical extensions, and safety limits.</param>
    /// <returns>A result containing compact JWS text with an empty payload segment or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static TokenTextResult SignDetached(
        ReadOnlySpan<byte> payload,
        JoseHeader protectedHeader,
        TokenKey key,
        TokenPolicy policy)
    {
        return SignCore(payload, protectedHeader, key, policy, true);
    }

    static TokenTextResult SignCore(
        ReadOnlySpan<byte> payload,
        JoseHeader protectedHeader,
        TokenKey key,
        TokenPolicy policy,
        bool detached)
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

        TokenResult policyValidation = ValidatePolicy(policy);
        if (!policyValidation.Success)
        {
            return TextFailure(policyValidation.Error);
        }

        TokenBytesResult headerBytes = protectedHeader.WriteUtf8();
        if (!headerBytes.Success)
        {
            return TextFailure(headerBytes.Error);
        }

        if ((headerBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length > policy.MaximumTokenSizeInBytes
            || payload.Length > policy.MaximumTokenSizeInBytes)
        {
            return TextFailure(
                "token_too_large",
                "The compact JWS exceeds the configured token size limit.",
                policy.MaximumTokenSizeInBytes);
        }

        JoseHeaderResult boundedHeader = JoseHeader.Read(
            headerBytes.Bytes,
            policy.MaximumJsonDepth);
        if (!boundedHeader.Success)
        {
            return TextFailure(boundedHeader.Error);
        }

        TokenResult headerValidation = ValidateHeader(
            boundedHeader.Header,
            policy.UnderstoodCriticalHeaders);
        if (!headerValidation.Success)
        {
            return TextFailure(headerValidation.Error);
        }

        JsonValue? algorithmValue = (boundedHeader.Header ?? throw new InvalidOperationException("A required value is null.")).Get(
            JoseHeaderNames.Algorithm);
        if (algorithmValue == null)
        {
            return TextFailure(
                "missing_algorithm",
                "A compact JWS protected header requires the alg parameter.",
                -1);
        }

        string? algorithm = algorithmValue.StringValue;
        if (string.IsNullOrEmpty(algorithm))
        {
            return TextFailure(
                "invalid_algorithm",
                "The compact JWS algorithm identifier must not be empty.",
                -1);
        }

        AlgorithmMetadata metadata = AlgorithmMetadataRegistry.GetJose(
            algorithm);
        if (metadata.Known && !JoseAlgorithmsOperations.IsSignature(algorithm))
        {
            return TextFailure(
                "invalid_jws_algorithm",
                "The registered JOSE algorithm is not a JWS signature or MAC algorithm.",
                -1);
        }

        if (string.Equals(algorithm, JoseAlgorithms.None, StringComparison.Ordinal))
        {
            return TextFailure(
                "unsecured_jws_not_supported",
                "Unsecured JWS creation is not supported by this operation.",
                -1);
        }

        TokenResult availability = CryptoAlgorithmAvailability.CheckJoseAlgorithm(
            algorithm);
        if (!availability.Success)
        {
            return TextFailure(availability.Error);
        }

        if (!RegistryLookup.Contains(policy.AllowedAlgorithms, algorithm, false))
        {
            return TextFailure(
                "algorithm_not_allowed",
                "The JWS signing algorithm is not allowed by policy.",
                -1);
        }

        bool encodePayload = IsPayloadBase64UrlEncoded(boundedHeader.Header);
        TokenTextResult headerSegment = Base64Url.Encode(headerBytes.Bytes);
        TokenBytesResult signingInput = CreateSigningInput(
            headerSegment.Text,
            payload,
            encodePayload);
        if (!signingInput.Success)
        {
            return TextFailure(signingInput.Error);
        }

        if (!detached
            && (signingInput.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length >= policy.MaximumTokenSizeInBytes)
        {
            return TextFailure(
                "token_too_large",
                "The compact JWS exceeds the configured token size limit.",
                policy.MaximumTokenSizeInBytes);
        }

        string unencodedPayloadText = string.Empty;
        if (!encodePayload && !detached)
        {
            TokenTextResult decodedPayload = Utf8Codec.Decode(payload);
            if (!decodedPayload.Success)
            {
                return TextFailure(decodedPayload.Error);
            }

            int periodPosition = (decodedPayload.Text ?? throw new InvalidOperationException("A required value is null.")).IndexOf('.');
            if (periodPosition >= 0)
            {
                return TextFailure(
                    "invalid_unencoded_payload",
                    "An attached unencoded compact JWS payload must not contain a period.",
                    periodPosition);
            }

            unencodedPayloadText = decodedPayload.Text;
        }

        TokenBytesResult signature = SignBytes(
            signingInput.Bytes,
            key,
            algorithm);
        if (!signature.Success)
        {
            return TextFailure(signature.Error);
        }

        TokenTextResult compact;
        if (encodePayload)
        {
            ReadOnlySpan<byte> compactPayload = detached
                ? Array.Empty<byte>().AsSpan()
                : payload;
            compact = JoseCompact.WriteJws(
                headerBytes.Bytes.AsSpan(),
                compactPayload,
                signature.Bytes.AsSpan());
        }
        else
        {
            TokenTextResult signatureSegment = Base64Url.Encode((signature.Bytes ?? throw new InvalidOperationException("A required value is null.")));
            compact = TextSuccess(
                headerSegment.Text + "." + unencodedPayloadText + "."
                + signatureSegment.Text);
        }
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
                "The compact JWS exceeds the configured token size limit.",
                policy.MaximumTokenSizeInBytes);
        }

        return compact;
    }

    /// <summary>Parses compact JWS structure using conservative default limits.</summary>
    /// <remarks>The payload is deliberately withheld because no signature verification is performed.</remarks>
    /// <param name="text">The compact JWS text.</param>
    /// <returns>A result containing unverified header metadata and signature bytes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    internal static JwsCompactResult Parse(string text)
    {
        return Parse(text, new TokenPolicy());
    }

    /// <summary>Parses compact JWS structure using explicit safety limits.</summary>
    /// <remarks>The payload is deliberately withheld because no signature verification is performed.</remarks>
    /// <param name="text">The compact JWS text.</param>
    /// <param name="policy">The parsing limits and understood critical extensions.</param>
    /// <returns>A result containing unverified header metadata and signature bytes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwsCompactResult Parse(string text, TokenPolicy policy)
    {
        return ReadCore(text, policy, false);
    }

    /// <summary>Reads unverified compact JWS payload bytes when policy explicitly permits diagnostics.</summary>
    /// <param name="text">The compact JWS text.</param>
    /// <param name="policy">The policy that must enable unauthenticated decode.</param>
    /// <returns>A result containing unverified header, payload, and signature data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwsCompactResult ReadDiagnostic(
        string text,
        TokenPolicy policy)
    {
        return ReadCore(text, policy, true);
    }

    /// <summary>Verifies a compact JWS and returns its authenticated payload and protected header.</summary>
    /// <param name="text">The compact JWS text.</param>
    /// <param name="key">The verification key and its operation metadata.</param>
    /// <param name="policy">The explicit algorithm allowlist, critical extensions, and safety limits.</param>
    /// <returns>A result containing authenticated JWS data or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwsCompactResult Verify(
        string text,
        TokenKey key,
        TokenPolicy policy)
    {
        return VerifyCore(
            text,
            Array.Empty<byte>().AsSpan(),
            key,
            policy,
            false);
    }

    /// <summary>Validates an explicitly policy-authorized unsecured compact JWS.</summary>
    /// <param name="text">The unsecured compact JWS text.</param>
    /// <param name="policy">The policy that must allow unsecured tokens and the <c>none</c> algorithm.</param>
    /// <returns>A result containing the policy-validated payload and header.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwsCompactResult ValidateUnsecured(
        string text,
        TokenPolicy policy)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult policyResult = ValidateUnsecuredPolicy(policy);
        if (!policyResult.Success)
        {
            return Failure(policyResult.Error);
        }

        JwsCompactResult parsed = ReadCore(text, policy, false);
        if (!parsed.Success)
        {
            return parsed;
        }

        if (!(parsed.Token ?? throw new InvalidOperationException("A required value is null.")).Unsecured)
        {
            return Failure(
                "invalid_unsecured_algorithm",
                "Unsecured validation requires the none algorithm.",
                -1);
        }

        parsed.Token.Payload = parsed.Token.ParsedPayload.AsSpan().ToArray();
        parsed.Token.PayloadAvailable = true;
        return parsed;
    }

    /// <summary>Verifies a compact JWS against an externally supplied detached payload.</summary>
    /// <param name="text">The compact JWS text whose payload segment must be empty.</param>
    /// <param name="payload">The detached payload bytes to authenticate.</param>
    /// <param name="key">The verification key and its operation metadata.</param>
    /// <param name="policy">The explicit algorithm allowlist, critical extensions, and safety limits.</param>
    /// <returns>A result containing authenticated JWS data or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwsCompactResult VerifyDetached(
        string text,
        byte[] payload,
        TokenKey key,
        TokenPolicy policy)
    {
        if (payload == null)
        {
            throw new ArgumentNullException("payload");
        }

        return VerifyDetached(text, payload.AsSpan(), key, policy);
    }

    /// <summary>Verifies a compact JWS against an externally supplied detached payload span.</summary>
    /// <param name="text">The compact JWS text whose payload segment must be empty.</param>
    /// <param name="payload">The detached payload bytes to authenticate.</param>
    /// <param name="key">The verification key and its operation metadata.</param>
    /// <param name="policy">The explicit algorithm allowlist, critical extensions, and safety limits.</param>
    /// <returns>A result containing authenticated JWS data or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static JwsCompactResult VerifyDetached(
        string text,
        ReadOnlySpan<byte> payload,
        TokenKey key,
        TokenPolicy policy)
    {
        return VerifyCore(text, payload, key, policy, true);
    }

    static JwsCompactResult VerifyCore(
        string text,
        ReadOnlySpan<byte> payload,
        TokenKey key,
        TokenPolicy policy,
        bool detached)
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

        JwsCompactResult parsed = ReadCore(text, policy, false);
        if (!parsed.Success)
        {
            return parsed;
        }

        if (detached
            && ((parsed.Token ?? throw new InvalidOperationException("A required value is null.")).PayloadSegment ?? throw new InvalidOperationException("A required value is null.")).Length != 0)
        {
            return Failure(
                "invalid_detached_jws",
                "A detached compact JWS must have an empty payload segment.",
                1);
        }

        if (detached && payload.Length > policy.MaximumTokenSizeInBytes)
        {
            return Failure(
                "token_too_large",
                "The detached JWS payload exceeds the configured token size limit.",
                policy.MaximumTokenSizeInBytes);
        }

        string? algorithm = (parsed.Token ?? throw new InvalidOperationException("A required value is null.")).Algorithm;
        if (parsed.Token.Unsecured)
        {
            return Failure(
                "unsecured_jws_not_supported",
                "Ordinary JWS verification does not accept unsecured tokens.",
                -1);
        }

        TokenResult availability = CryptoAlgorithmAvailability.CheckJoseAlgorithm(
            (algorithm ?? throw new InvalidOperationException("A required value is null.")));
        if (!availability.Success)
        {
            return Failure(availability.Error);
        }

        if (!RegistryLookup.Contains(policy.AllowedAlgorithms, algorithm, false))
        {
            return Failure(
                "algorithm_not_allowed",
                "The JWS verification algorithm is not allowed by policy.",
                -1);
        }

        ReadOnlySpan<byte> verificationPayload = detached
            ? payload
            : parsed.Token.ParsedPayload.AsSpan();
        TokenBytesResult signingInput = CreateSigningInput(
            parsed.Token.ProtectedSegment,
            verificationPayload,
            parsed.Token.PayloadBase64UrlEncoded);
        if (!signingInput.Success)
        {
            return Failure(signingInput.Error);
        }

        TokenResult verification = VerifyBytes(
            signingInput.Bytes,
            parsed.Token.Signature,
            key,
            algorithm);
        if (!verification.Success)
        {
            return Failure(verification.Error);
        }

        parsed.Token.Payload = detached
            ? payload.ToArray()
            : parsed.Token.ParsedPayload.AsSpan().ToArray();
        parsed.Token.PayloadAvailable = true;
        parsed.Token.SignatureVerified = true;
        return parsed;
    }

    static JwsCompactResult ReadCore(
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

        TokenResult policyValidation = ValidatePolicy(policy);
        if (!policyValidation.Success)
        {
            return Failure(policyValidation.Error);
        }

        if (diagnostic && !policy.AllowUnauthenticatedDecode)
        {
            return Failure(
                "unauthenticated_decode_not_allowed",
                "Diagnostic JWS payload decoding requires explicit policy opt-in.",
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
                "The compact JWS exceeds the configured token size limit.",
                policy.MaximumTokenSizeInBytes);
        }

        int firstSeparator = -1;
        int secondSeparator = -1;
        int separatorCount = 0;
        int textIndex = 0;
        while (textIndex < text.Length)
        {
            if (text[textIndex] == '.')
            {
                separatorCount += 1;
                if (separatorCount == 1)
                {
                    firstSeparator = textIndex;
                }
                else if (separatorCount == 2)
                {
                    secondSeparator = textIndex;
                }
            }

            textIndex += 1;
        }

        if (separatorCount != 2)
        {
            if (separatorCount == 4)
            {
                return Failure(
                    "invalid_jws_compact",
                    "A compact JWS must contain exactly three segments.",
                    -1);
            }

            return Failure(
                "invalid_compact_token",
                "A compact JWS must contain exactly three segments.",
                text.Length);
        }

        if (firstSeparator == 0)
        {
            return Failure(
                "missing_protected_header",
                "A compact JWS requires a protected header.",
                0);
        }

        string protectedSegment = text.Substring(0, firstSeparator);
        string payloadSegment = text.Substring(
            firstSeparator + 1,
            secondSeparator - firstSeparator - 1);
        string signatureSegment = text.Substring(secondSeparator + 1);
        TokenBytesResult protectedBytes = Base64Url.Decode(protectedSegment);
        if (!protectedBytes.Success)
        {
            return InvalidCompactSegment(
                "The compact JWS protected header is not canonical base64url.",
                (protectedBytes.Error ?? throw new InvalidOperationException("A required value is null.")).Position);
        }

        JoseHeaderResult header = JoseHeader.Read(
            (protectedBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")),
            policy.MaximumJsonDepth);
        if (!header.Success)
        {
            TokenBytesResult ordinaryPayload = Base64Url.Decode(payloadSegment);
            if (!ordinaryPayload.Success)
            {
                return InvalidCompactSegment(
                    "The compact JWS payload is not canonical base64url.",
                    firstSeparator + 1 + (ordinaryPayload.Error ?? throw new InvalidOperationException("A required value is null.")).Position);
            }

            return Failure(header.Error);
        }

        TokenResult headerValidation = ValidateHeader(
            header.Header,
            policy.UnderstoodCriticalHeaders);
        if (!headerValidation.Success)
        {
            return Failure(headerValidation.Error);
        }

        JsonValue? algorithmValue = (header.Header ?? throw new InvalidOperationException("A required value is null.")).Get(
            JoseHeaderNames.Algorithm);
        if (algorithmValue == null)
        {
            return Failure(
                "missing_algorithm",
                "A compact JWS protected header requires the alg parameter.",
                -1);
        }

        string? algorithm = algorithmValue.StringValue;
        if (string.IsNullOrEmpty(algorithm))
        {
            return Failure(
                "invalid_algorithm",
                "The compact JWS algorithm identifier must not be empty.",
                -1);
        }

        AlgorithmMetadata metadata = AlgorithmMetadataRegistry.GetJose(
            algorithm);
        if (metadata.Known && !JoseAlgorithmsOperations.IsSignature(algorithm))
        {
            return Failure(
                "invalid_jws_algorithm",
                "The registered JOSE algorithm is not a JWS signature or MAC algorithm.",
                -1);
        }

        bool encodePayload = IsPayloadBase64UrlEncoded(header.Header);
        TokenBytesResult payloadBytes = encodePayload
            ? Base64Url.Decode(payloadSegment)
            : Utf8Codec.Encode(payloadSegment);
        if (!payloadBytes.Success)
        {
            if (encodePayload)
            {
                return InvalidCompactSegment(
                    "The compact JWS payload is not canonical base64url.",
                    firstSeparator + 1 + (payloadBytes.Error ?? throw new InvalidOperationException("A required value is null.")).Position);
            }

            return Failure(payloadBytes.Error);
        }

        TokenBytesResult signatureBytes = Base64Url.Decode(signatureSegment);
        if (!signatureBytes.Success)
        {
            return InvalidCompactSegment(
                "The compact JWS signature is not canonical base64url.",
                secondSeparator + 1 + (signatureBytes.Error ?? throw new InvalidOperationException("A required value is null.")).Position);
        }

        bool unsecured = string.Equals(
            algorithm,
            JoseAlgorithms.None,
            StringComparison.Ordinal);
        if (unsecured && (signatureBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length != 0)
        {
            return Failure(
                "invalid_unsecured_signature",
                "An unsecured JWS signature segment must be empty.",
                2);
        }

        if (!unsecured && (signatureBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length == 0)
        {
            return Failure(
                "missing_signature",
                "A signed compact JWS requires a signature or MAC value.",
                2);
        }

        JwsCompactToken token = new JwsCompactToken();
        token.ProtectedHeader = header.Header;
        token.Algorithm = algorithm;
        token.AlgorithmMetadata = metadata;
        token.AlgorithmAvailable = CryptoAlgorithmAvailability.IsJoseAlgorithmAvailable(
            algorithm);
        token.PayloadBase64UrlEncoded = encodePayload;
        token.Unsecured = unsecured;
        token.Signature = signatureBytes.Bytes;
        token.ProtectedSegment = protectedSegment;
        token.PayloadSegment = payloadSegment;
        token.ParsedPayload = payloadBytes.Bytes;

        JsonValue? keyId = header.Header.Get(JoseHeaderNames.KeyId);
        if (keyId != null)
        {
            token.KeyId = keyId.StringValue;
        }

        if (diagnostic)
        {
            token.Payload = payloadBytes.Bytes.AsSpan().ToArray();
            token.PayloadAvailable = true;
        }

        JwsCompactResult result = new JwsCompactResult();
        result.Token = token;
        result.Success = true;
        return result;
    }

    internal static TokenResult ValidateHeader(
        JoseHeader? header,
        string[]? understoodCriticalHeaders)
    {
        int index = 0;
        while (index < (header ?? throw new InvalidOperationException("A required value is null.")).Parameters.Members.Length)
        {
            string? name = header.Parameters.Members[index].Name;
            if (JoseHeaderNamesOperations.IsRegistered((name ?? throw new InvalidOperationException("A required value is null.")))
                && !JoseHeaderNamesOperations.IsForJws(name))
            {
                return ValidationFailure(
                    "invalid_jws_header_parameter",
                    "A registered header parameter is not defined for JWS.",
                    index);
            }

            index += 1;
        }

        JsonValue? payloadEncoding = header.Get(
            JoseHeaderNames.Base64UrlEncodePayload);
        if (payloadEncoding != null)
        {
            if (!CriticalContains(
                header,
                JoseHeaderNames.Base64UrlEncodePayload))
            {
                return ValidationFailure(
                    "missing_critical_header",
                    "The JWS b64 parameter must be listed by crit.",
                    -1);
            }

            string[] understood = (AddUnderstoodPayloadEncoding(
                understoodCriticalHeaders) ?? throw new InvalidOperationException("A required value is null."));
            return JoseCriticalHeaderValidator.Validate(header, (understood ?? throw new InvalidOperationException("A required value is null.")));
        }

        return JoseCriticalHeaderValidator.Validate(
            header,
            (understoodCriticalHeaders ?? throw new InvalidOperationException("A required value is null.")));
    }

    internal static bool IsPayloadBase64UrlEncoded(JoseHeader? header)
    {
        JsonValue? value = (header ?? throw new InvalidOperationException("A required value is null.")).Get(JoseHeaderNames.Base64UrlEncodePayload);
        return value == null || value.BooleanValue;
    }

    static bool CriticalContains(JoseHeader header, string name)
    {
        JsonValue? critical = header.Get(JoseHeaderNames.Critical);
        if (critical == null
            || critical.Kind != TokenJsonValueKind.Array
            || critical.ArrayValue == null)
        {
            return false;
        }

        int index = 0;
        while (index < critical.ArrayValue.Length)
        {
            JsonValue item = critical.ArrayValue[index];
            if (item != null
                && item.Kind == TokenJsonValueKind.String
                && string.Equals(item.StringValue, name, StringComparison.Ordinal))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal static string[]? AddUnderstoodPayloadEncoding(string[]? understood)
    {
        if (RegistryLookup.Contains(
            understood,
            JoseHeaderNames.Base64UrlEncodePayload,
            false))
        {
            return understood;
        }

        string[] expanded = new string[(understood ?? throw new InvalidOperationException("A required value is null.")).Length + 1];
        understood.AsSpan().CopyTo(expanded);
        expanded[expanded.Length - 1] = JoseHeaderNames.Base64UrlEncodePayload;
        return expanded;
    }

    internal static TokenResult ValidatePolicy(TokenPolicy policy)
    {
        if (policy.MaximumTokenSizeInBytes <= 0
            || policy.MaximumJsonDepth <= 0
            || policy.AllowedAlgorithms == null
            || policy.UnderstoodCriticalHeaders == null)
        {
            return ValidationFailure(
                "invalid_policy",
                "The compact JWS parsing policy is invalid.",
                -1);
        }

        return ValidationSuccess();
    }

    internal static TokenResult ValidateUnsecuredPolicy(TokenPolicy policy)
    {
        TokenResult basic = ValidatePolicy(policy);
        if (!basic.Success)
        {
            return basic;
        }

        if (!policy.AllowUnsecuredTokens)
        {
            return ValidationFailure(
                "unsecured_tokens_not_allowed",
                "Unsecured token creation and validation require explicit policy opt-in.",
                -1);
        }

        if (!RegistryLookup.Contains(
            policy.AllowedAlgorithms,
            JoseAlgorithms.None,
            false))
        {
            return ValidationFailure(
                "algorithm_not_allowed",
                "The none algorithm is not allowed by policy.",
                -1);
        }

        return ValidationSuccess();
    }

    internal static TokenResult ValidateUnsecuredAlgorithm(JoseHeader? header)
    {
        JsonValue? algorithm = (header ?? throw new InvalidOperationException("A required value is null.")).Get(JoseHeaderNames.Algorithm);
        if (algorithm == null)
        {
            return ValidationFailure(
                "missing_algorithm",
                "An unsecured JWS protected header requires alg.",
                -1);
        }

        if (!string.Equals(
            algorithm.StringValue,
            JoseAlgorithms.None,
            StringComparison.Ordinal))
        {
            return ValidationFailure(
                "invalid_unsecured_algorithm",
                "Unsecured JWS creation and validation require the none algorithm.",
                -1);
        }

        return ValidationSuccess();
    }

    internal static TokenBytesResult SignBytes(
        byte[]? data,
        TokenKey key,
        string? algorithm)
    {
        if (string.Equals(algorithm, JoseAlgorithms.HmacSha256, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.HmacSha384, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.HmacSha512, StringComparison.Ordinal))
        {
            return HmacAlgorithms.Sign((data ?? throw new InvalidOperationException("A required value is null.")), key, (algorithm ?? throw new InvalidOperationException("A required value is null.")));
        }

        if (string.Equals(algorithm, JoseAlgorithms.RsaSha256, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaSha384, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaSha512, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaPssSha256, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaPssSha384, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaPssSha512, StringComparison.Ordinal))
        {
            return RsaAlgorithms.Sign((data ?? throw new InvalidOperationException("A required value is null.")), key, (algorithm ?? throw new InvalidOperationException("A required value is null.")));
        }

        if (string.Equals(algorithm, JoseAlgorithms.EcdsaSha256, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.EcdsaSha384, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.EcdsaSha512, StringComparison.Ordinal))
        {
            return EcdsaAlgorithms.Sign((data ?? throw new InvalidOperationException("A required value is null.")), key, (algorithm ?? throw new InvalidOperationException("A required value is null.")));
        }

        TokenBytesResult result = new TokenBytesResult();
        result.Error = new TokenError(
            "unsupported_algorithm",
            "The JWS algorithm has no compact signing implementation.",
            -1);
        return result;
    }

    internal static TokenBytesResult CreateSigningInput(
        string? protectedSegment,
        ReadOnlySpan<byte> payload,
        bool encodePayload)
    {
        TokenBytesResult prefix = Utf8Codec.Encode(protectedSegment + ".");
        if (!prefix.Success)
        {
            return prefix;
        }

        if (encodePayload)
        {
            TokenTextResult encoded = Base64Url.Encode(payload);
            return Utf8Codec.Encode(protectedSegment + "." + encoded.Text);
        }

        byte[] bytes = new byte[(prefix.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length + payload.Length];
        prefix.Bytes.AsSpan().CopyTo(bytes);
        payload.CopyTo(bytes.AsSpan(prefix.Bytes.Length));
        TokenBytesResult result = new TokenBytesResult();
        result.Bytes = bytes;
        result.Success = true;
        return result;
    }

    internal static TokenResult VerifyBytes(
        byte[]? data,
        byte[]? signature,
        TokenKey key,
        string? algorithm)
    {
        if (string.Equals(algorithm, JoseAlgorithms.HmacSha256, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.HmacSha384, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.HmacSha512, StringComparison.Ordinal))
        {
            return HmacAlgorithms.Verify((data ?? throw new InvalidOperationException("A required value is null.")), (signature ?? throw new InvalidOperationException("A required value is null.")), key, (algorithm ?? throw new InvalidOperationException("A required value is null.")));
        }

        if (string.Equals(algorithm, JoseAlgorithms.RsaSha256, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaSha384, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaSha512, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaPssSha256, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaPssSha384, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaPssSha512, StringComparison.Ordinal))
        {
            return RsaAlgorithms.Verify((data ?? throw new InvalidOperationException("A required value is null.")), (signature ?? throw new InvalidOperationException("A required value is null.")), key, (algorithm ?? throw new InvalidOperationException("A required value is null.")));
        }

        if (string.Equals(algorithm, JoseAlgorithms.EcdsaSha256, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.EcdsaSha384, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.EcdsaSha512, StringComparison.Ordinal))
        {
            return EcdsaAlgorithms.Verify((data ?? throw new InvalidOperationException("A required value is null.")), (signature ?? throw new InvalidOperationException("A required value is null.")), key, (algorithm ?? throw new InvalidOperationException("A required value is null.")));
        }

        return ValidationFailure(
            "unsupported_algorithm",
            "The JWS algorithm has no compact verification implementation.",
            -1);
    }

    static TokenResult ValidationSuccess()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult ValidationFailure(
        string code,
        string message,
        int position)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, position);
        return result;
    }

    static JwsCompactResult Failure(
        string code,
        string message,
        int position)
    {
        return Failure(new TokenError(code, message, position));
    }

    static JwsCompactResult Failure(TokenError? error)
    {
        JwsCompactResult result = new JwsCompactResult();
        result.Error = error;
        return result;
    }

    static JwsCompactResult InvalidCompactSegment(
        string message,
        int position)
    {
        return Failure(
            "invalid_compact_token",
            message,
            position);
    }

    static TokenTextResult TextFailure(
        string code,
        string message,
        int position)
    {
        return TextFailure(new TokenError(code, message, position));
    }

    static TokenTextResult TextFailure(TokenError? error)
    {
        TokenTextResult result = new TokenTextResult();
        result.Error = error;
        return result;
    }

    static TokenTextResult TextSuccess(string text)
    {
        TokenTextResult result = new TokenTextResult();
        result.Text = text;
        result.Success = true;
        return result;
    }
}
