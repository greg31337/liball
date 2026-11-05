using System;
namespace token;

/// <summary>Reads one inner JOSE layer after authenticating its enclosing JWS or JWE.</summary>
internal static class NestedJose
{
    /// <summary>Reads the payload of a verified compact JWS according to protected <c>cty</c>.</summary>
    /// <param name="outer">The successful compact JWS verification result.</param>
    /// <param name="policy">The parsing and safety policy for the inner layer.</param>
    /// <returns>A result containing unauthenticated inner JOSE metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static NestedJoseResult Read(
        JwsCompactResult outer,
        TokenPolicy policy)
    {
        ValidateArguments(outer, policy);
        if (!outer.Success
            || outer.Token == null
            || !outer.Token.SignatureVerified
            || !outer.Token.PayloadAvailable
            || outer.Token.Payload == null)
        {
            return Failure(
                "outer_token_not_authenticated",
                "Nested JOSE reading requires a verified outer JWS payload.");
        }

        return ReadCore(
            outer.Token.Payload,
            ContentType(outer.Token.ProtectedHeader),
            policy);
    }

    /// <summary>Reads the plaintext of an authenticated compact JWE according to protected <c>cty</c>.</summary>
    /// <param name="outer">The successful compact JWE decryption result.</param>
    /// <param name="policy">The parsing and safety policy for the inner layer.</param>
    /// <returns>A result containing unauthenticated inner JOSE metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static NestedJoseResult Read(
        JweCompactResult outer,
        TokenPolicy policy)
    {
        ValidateArguments(outer, policy);
        if (!outer.Success
            || outer.Token == null
            || !outer.Token.AuthenticationTagVerified
            || !outer.Token.PlaintextAvailable
            || outer.Token.Plaintext == null)
        {
            return Failure(
                "outer_token_not_authenticated",
                "Nested JOSE reading requires authenticated outer JWE plaintext.");
        }

        return ReadCore(
            outer.Token.Plaintext,
            ContentType(outer.Token.ProtectedHeader),
            policy);
    }

    /// <summary>Reads the payload of a verified flattened JWS according to protected <c>cty</c>.</summary>
    /// <param name="outer">The successful flattened JWS verification result.</param>
    /// <param name="policy">The parsing and safety policy for the inner layer.</param>
    /// <returns>A result containing unauthenticated inner JOSE metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static NestedJoseResult Read(
        JwsFlattenedResult outer,
        TokenPolicy policy)
    {
        ValidateArguments(outer, policy);
        if (!outer.Success
            || outer.Token == null
            || !outer.Token.SignatureVerified
            || !outer.Token.PayloadAvailable
            || outer.Token.Payload == null)
        {
            return Failure(
                "outer_token_not_authenticated",
                "Nested JOSE reading requires a verified outer JWS payload.");
        }

        return ReadCore(
            outer.Token.Payload,
            ContentType(outer.Token.ProtectedHeader),
            policy);
    }

    /// <summary>Reads the payload of a fully verified general JWS according to protected <c>cty</c>.</summary>
    /// <param name="outer">The successful general JWS verification result.</param>
    /// <param name="policy">The parsing and safety policy for the inner layer.</param>
    /// <returns>A result containing unauthenticated inner JOSE metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static NestedJoseResult Read(
        JwsGeneralResult outer,
        TokenPolicy policy)
    {
        ValidateArguments(outer, policy);
        if (!outer.Success
            || outer.Token == null
            || !outer.Token.PayloadAvailable
            || outer.Token.Payload == null
            || outer.Token.Signatures == null
            || outer.Token.Signatures.Length == 0)
        {
            return Failure(
                "outer_token_not_authenticated",
                "Nested JOSE reading requires a fully verified outer JWS payload.");
        }

        string? contentType = null;
        int index = 0;
        while (index < outer.Token.Signatures.Length)
        {
            JwsGeneralSignature signature = outer.Token.Signatures[index];
            if (signature == null || !signature.SignatureVerified)
            {
                return Failure(
                    "outer_token_not_authenticated",
                    "Every outer JWS signature must be verified before nested reading.");
            }

            string? current = ContentType(signature.ProtectedHeader);
            if (index == 0)
            {
                contentType = current;
            }
            else if (!string.Equals(
                contentType,
                current,
                StringComparison.OrdinalIgnoreCase))
            {
                return Failure(
                    "inconsistent_nested_content_type",
                    "All verified JWS signatures must protect the same cty value.");
            }

            index += 1;
        }

        return ReadCore(outer.Token.Payload, contentType, policy);
    }

    /// <summary>Reads the plaintext of an authenticated flattened JWE according to protected <c>cty</c>.</summary>
    /// <param name="outer">The successful flattened JWE decryption result.</param>
    /// <param name="policy">The parsing and safety policy for the inner layer.</param>
    /// <returns>A result containing unauthenticated inner JOSE metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static NestedJoseResult Read(
        JweFlattenedResult outer,
        TokenPolicy policy)
    {
        ValidateArguments(outer, policy);
        if (!outer.Success
            || outer.Token == null
            || !outer.Token.AuthenticationTagVerified
            || !outer.Token.PlaintextAvailable
            || outer.Token.Plaintext == null)
        {
            return Failure(
                "outer_token_not_authenticated",
                "Nested JOSE reading requires authenticated outer JWE plaintext.");
        }

        return ReadCore(
            outer.Token.Plaintext,
            ContentType(outer.Token.ProtectedHeader),
            policy);
    }

    /// <summary>Reads the plaintext of an authenticated general JWE according to protected <c>cty</c>.</summary>
    /// <param name="outer">The successful general JWE decryption result.</param>
    /// <param name="policy">The parsing and safety policy for the inner layer.</param>
    /// <returns>A result containing unauthenticated inner JOSE metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static NestedJoseResult Read(
        JweGeneralResult outer,
        TokenPolicy policy)
    {
        ValidateArguments(outer, policy);
        if (!outer.Success
            || outer.Token == null
            || !outer.Token.AuthenticationTagVerified
            || !outer.Token.PlaintextAvailable
            || outer.Token.Plaintext == null)
        {
            return Failure(
                "outer_token_not_authenticated",
                "Nested JOSE reading requires authenticated outer JWE plaintext.");
        }

        return ReadCore(
            outer.Token.Plaintext,
            ContentType(outer.Token.ProtectedHeader),
            policy);
    }

    static NestedJoseResult ReadCore(
        byte[] content,
        string? contentType,
        TokenPolicy policy)
    {
        if (contentType == null || contentType.Length == 0)
        {
            return Failure(
                "missing_nested_content_type",
                "Nested JOSE reading requires an integrity-protected cty value.");
        }

        string? normalized = JoseMediaTypesOperations.Normalize(contentType);
        if (normalized == null)
        {
            return Failure(
                "unknown_nested_content_type",
                "The protected cty value is not a registered JOSE or JWT media type.");
        }

        bool compact = string.Equals(
            normalized,
            JoseMediaTypes.Jose,
            StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                normalized,
                JoseMediaTypes.Jwt,
                StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith("+jwt", StringComparison.OrdinalIgnoreCase);
        bool json = string.Equals(
            normalized,
            JoseMediaTypes.JoseJson,
            StringComparison.OrdinalIgnoreCase);
        if (!compact && !json)
        {
            return Failure(
                "unsupported_nested_content_type",
                "The registered cty value does not identify a supported nested JOSE serialization.");
        }

        TokenTextResult text = Utf8Codec.Decode(content);
        if (!text.Success)
        {
            return Failure(
                "invalid_nested_token_encoding",
                "Nested JOSE content must be valid UTF-8 text.");
        }

        if (compact)
        {
            return ReadCompact(text.Text, contentType, normalized, policy);
        }

        return ReadJson(text.Text, contentType, normalized, policy);
    }

    static NestedJoseResult ReadCompact(
        string? text,
        string contentType,
        string normalized,
        TokenPolicy policy)
    {
        JoseCompactParseResult shape = JoseCompact.Parse((text ?? throw new InvalidOperationException("A required value is null.")));
        if (!shape.Success)
        {
            return Failure(
                "nested_content_type_mismatch",
                "The protected cty value requires compact JOSE content.");
        }

        NestedJoseToken token = NewToken(contentType, normalized);
        if ((shape.Serialization ?? throw new InvalidOperationException("A required value is null.")).Kind == TokenKind.JwsCompact)
        {
            JwsCompactResult parsed = JsonWebSignature.Parse(text, policy);
            if (!parsed.Success)
            {
                return Failure(parsed.Error);
            }

            token.Kind = TokenKind.JwsCompact;
            token.CompactJws = parsed.Token;
        }
        else
        {
            JweCompactResult parsed = JsonWebEncryption.Parse(text, policy);
            if (!parsed.Success)
            {
                return Failure(parsed.Error);
            }

            token.Kind = TokenKind.JweCompact;
            token.CompactJwe = parsed.Token;
        }

        return Success(token);
    }

    static NestedJoseResult ReadJson(
        string? text,
        string contentType,
        string normalized,
        TokenPolicy policy)
    {
        JsonObjectResult root = JsonObject.Read((text ?? throw new InvalidOperationException("A required value is null.")), policy.MaximumJsonDepth);
        if (!root.Success)
        {
            return Failure(
                "nested_content_type_mismatch",
                "The protected cty value requires a JOSE JSON serialization.");
        }

        NestedJoseToken token = NewToken(contentType, normalized);
        if ((root.Object ?? throw new InvalidOperationException("A required value is null.")).Get("signatures") != null)
        {
            JwsGeneralResult parsed = JsonWebSignatureGeneral.Parse(text, policy);
            if (!parsed.Success)
            {
                return Failure(parsed.Error);
            }

            token.Kind = TokenKind.JwsJson;
            token.GeneralJws = parsed.Token;
            return Success(token);
        }

        if (root.Object.Get("signature") != null)
        {
            JwsFlattenedResult parsed = JsonWebSignatureFlattened.Parse(
                text,
                policy);
            if (!parsed.Success)
            {
                return Failure(parsed.Error);
            }

            token.Kind = TokenKind.JwsJson;
            token.FlattenedJws = parsed.Token;
            return Success(token);
        }

        if (root.Object.Get("recipients") != null)
        {
            JweGeneralResult parsed = JsonWebEncryptionGeneral.Parse(text, policy);
            if (!parsed.Success)
            {
                return Failure(parsed.Error);
            }

            token.Kind = TokenKind.JweJson;
            token.GeneralJwe = parsed.Token;
            return Success(token);
        }

        if (root.Object.Get("ciphertext") != null)
        {
            JweFlattenedResult parsed = JsonWebEncryptionFlattened.Parse(
                text,
                policy);
            if (!parsed.Success)
            {
                return Failure(parsed.Error);
            }

            token.Kind = TokenKind.JweJson;
            token.FlattenedJwe = parsed.Token;
            return Success(token);
        }

        return Failure(
            "nested_content_type_mismatch",
            "The protected cty value requires a recognized JOSE JSON serialization.");
    }

    static NestedJoseToken NewToken(string contentType, string normalized)
    {
        NestedJoseToken token = new NestedJoseToken();
        token.ContentType = contentType;
        token.NormalizedContentType = normalized;
        return token;
    }

    static string? ContentType(JoseHeader? header)
    {
        if (header == null)
        {
            return null;
        }

        JsonValue? value = header.Get(JoseHeaderNames.ContentType);
        return value == null ? null : value.StringValue;
    }

    static void ValidateArguments(object outer, TokenPolicy policy)
    {
        if (outer == null)
        {
            throw new ArgumentNullException("outer");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }
    }

    static NestedJoseResult Success(NestedJoseToken token)
    {
        NestedJoseResult result = new NestedJoseResult();
        result.Token = token;
        result.Success = true;
        return result;
    }

    static NestedJoseResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static NestedJoseResult Failure(TokenError? error)
    {
        NestedJoseResult result = new NestedJoseResult();
        result.Error = error;
        return result;
    }
}
