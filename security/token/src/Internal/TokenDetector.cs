using System;
namespace token;

/// <summary>
/// Detects supported text and binary token representations without performing
/// cryptographic validation or exposing unauthenticated payload content.
/// </summary>
internal static class TokenDetector
{
    const ulong CwtTag = 61;

    /// <summary>Detects a supported text token representation.</summary>
    /// <param name="text">The token text to inspect.</param>
    /// <returns>The detected kind, an unknown result, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    internal static TokenKindResult Detect(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        TokenPolicy policy = new TokenPolicy();
        TokenBytesResult encoded = Utf8Codec.Encode(text);
        if (!encoded.Success)
        {
            return Failure(encoded.Error);
        }

        if ((encoded.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length > policy.MaximumTokenSizeInBytes)
        {
            return Failure(
                "token_too_large",
                "The token exceeds the default token size limit.",
                policy.MaximumTokenSizeInBytes);
        }

        if (text.IndexOf('~') >= 0)
        {
            SelectiveDisclosureJwtResult selective =
                SelectiveDisclosureJwt.Parse(text);
            if (selective.Success)
            {
                return Recognized(TokenKind.SdJwt);
            }

            return Unknown();
        }

        JoseCompactParseResult compact = JoseCompact.Parse(text);
        if (compact.Success)
        {
            if ((compact.Serialization ?? throw new InvalidOperationException("A required value is null.")).Kind == TokenKind.JwsCompact)
            {
                JwsCompactResult signature = JsonWebSignature.Parse(
                    text,
                    policy);
                if (signature.Success)
                {
                    return Recognized(TokenKind.JwsCompact);
                }
            }
            else
            {
                JweCompactResult encryption = JsonWebEncryption.Parse(
                    text,
                    policy);
                if (encryption.Success)
                {
                    return Recognized(TokenKind.JweCompact);
                }
            }

            return Unknown();
        }

        return DetectJson(text, policy);
    }

    /// <summary>Detects a supported binary token representation.</summary>
    /// <remarks>
    /// Untagged COSE and CWT structures remain unknown because their binary
    /// representations are not universally distinguishable without context.
    /// Valid UTF-8 bytes are also considered as text token input.
    /// </remarks>
    /// <param name="bytes">The encoded token bytes to inspect.</param>
    /// <returns>The detected kind, an unknown result, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>bytes</c> is null.</exception>
    internal static TokenKindResult DetectBytes(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        TokenPolicy policy = new TokenPolicy();
        if (bytes.Length > policy.MaximumTokenSizeInBytes)
        {
            return Failure(
                "token_too_large",
                "The token exceeds the default token size limit.",
                policy.MaximumTokenSizeInBytes);
        }

        CborReader reader = CborReader.Create(
            bytes,
            policy.MaximumTokenSizeInBytes,
            policy.MaximumCborDepth);
        CborReadResult decoded = reader.Read();
        if (decoded.Success && reader.GetRemainingLength() == 0)
        {
            if (IsCwt(decoded.Item, policy))
            {
                return Recognized(TokenKind.Cwt);
            }

            CoseMessageResult cose = CoseMessage.Read(bytes, policy);
            if (cose.Success)
            {
                return Recognized(TokenKind.Cose);
            }
        }

        TokenTextResult text = Utf8Codec.Decode(bytes);
        if (text.Success)
        {
            return Detect((text.Text ?? throw new InvalidOperationException("A required value is null.")));
        }

        return Unknown();
    }

    static TokenKindResult DetectJson(string text, TokenPolicy policy)
    {
        JsonObjectResult root = JsonObject.Read(
            text,
            policy.MaximumJsonDepth);
        if (!root.Success)
        {
            return Unknown();
        }

        if ((root.Object ?? throw new InvalidOperationException("A required value is null.")).Get("signatures") != null)
        {
            JwsGeneralResult generalSignature =
                JsonWebSignatureGeneral.Parse(text, policy);
            return generalSignature.Success
                ? Recognized(TokenKind.JwsJson)
                : Unknown();
        }

        if (root.Object.Get("signature") != null)
        {
            JwsFlattenedResult flattenedSignature =
                JsonWebSignatureFlattened.Parse(text, policy);
            return flattenedSignature.Success
                ? Recognized(TokenKind.JwsJson)
                : Unknown();
        }

        if (root.Object.Get("recipients") != null)
        {
            JweGeneralResult generalEncryption =
                JsonWebEncryptionGeneral.Parse(text, policy);
            return generalEncryption.Success
                ? Recognized(TokenKind.JweJson)
                : Unknown();
        }

        if (root.Object.Get("ciphertext") != null)
        {
            JweFlattenedResult flattenedEncryption =
                JsonWebEncryptionFlattened.Parse(text, policy);
            return flattenedEncryption.Success
                ? Recognized(TokenKind.JweJson)
                : Unknown();
        }

        return Unknown();
    }

    static bool IsCwt(CborItem? item, TokenPolicy policy)
    {
        if (item == null
            || item.Kind != TokenCborValueKind.Tag
            || item.TagValue != CwtTag
            || item.TaggedValue == null)
        {
            return false;
        }

        CborWriter writer = CborWriter.Create(
            policy.MaximumTokenSizeInBytes,
            policy.MaximumCborDepth);
        TokenResult written = writer.WriteDeterministicItem(item.TaggedValue);
        if (!written.Success)
        {
            return false;
        }

        CoseMessageResult cose = CoseMessage.Read(writer.ToArray(), policy);
        return cose.Success;
    }

    static TokenKindResult Recognized(TokenKind kind)
    {
        TokenKindResult result = new TokenKindResult();
        result.Success = true;
        result.Kind = kind;
        return result;
    }

    static TokenKindResult Unknown()
    {
        TokenKindResult result = new TokenKindResult();
        result.Success = true;
        return result;
    }

    static TokenKindResult Failure(TokenError? error)
    {
        TokenKindResult result = new TokenKindResult();
        result.Error = error;
        return result;
    }

    static TokenKindResult Failure(
        string code,
        string message,
        int position)
    {
        return Failure(new TokenError(code, message, position));
    }
}
