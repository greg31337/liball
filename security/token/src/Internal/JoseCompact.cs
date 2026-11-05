using System;
using System.Text;

namespace token;

/// <summary>
/// Scans compact JWS, JWT, and JWE serialization segments.
/// </summary>
internal static class JoseCompact
{
    /// <summary>
    /// Scans a compact token into three JWS or five JWE segment views.
    /// </summary>
    /// <param name="text">The compact token text to scan.</param>
    /// <returns>A result containing segment boundaries, or failure details for malformed input.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    internal static JoseCompactScanResult Scan(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        JoseCompactScanResult result = new JoseCompactScanResult();
        int[] starts = new int[5];
        int[] lengths = new int[5];
        int segmentIndex = 0;
        int segmentStart = 0;
        int position = 0;

        while (position < text.Length)
        {
            char value = text[position];

            if (value == '.')
            {
                if (segmentIndex == 4)
                {
                    result.Error = new TokenError(
                        "invalid_compact_token",
                        "A compact JOSE token must contain three or five segments.",
                        position);
                    return result;
                }

                starts[segmentIndex] = segmentStart;
                lengths[segmentIndex] = position - segmentStart;
                segmentIndex += 1;
                segmentStart = position + 1;
            }
            else if (!IsBase64UrlCharacter(value))
            {
                result.Error = new TokenError(
                    "invalid_compact_token",
                    "A compact JOSE segment contains a character outside the base64url alphabet.",
                    position);
                return result;
            }

            position += 1;
        }

        starts[segmentIndex] = segmentStart;
        lengths[segmentIndex] = text.Length - segmentStart;
        segmentIndex += 1;

        if (segmentIndex != 3 && segmentIndex != 5)
        {
            result.Error = new TokenError(
                "invalid_compact_token",
                "A compact JOSE token must contain three or five segments.",
                text.Length);
            return result;
        }

        result.Segments = new JoseCompactSegments(text, starts, lengths, segmentIndex);
        result.Success = true;
        return result;
    }

    /// <summary>Parses a three-segment JWS or five-segment JWE compact serialization.</summary>
    /// <param name="text">The compact JOSE serialization.</param>
    /// <returns>A result containing exact encoded views and independently decoded segment bytes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    internal static JoseCompactParseResult Parse(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        JoseCompactScanResult scanned = Scan(text);
        if (!scanned.Success)
        {
            return ParseFailure(scanned.Error);
        }

        byte[][] decoded = new byte[(scanned.Segments ?? throw new InvalidOperationException("A required value is null.")).SegmentCount][];
        int index = 0;
        while (index < decoded.Length)
        {
            TokenBytesResult segment = Base64Url.Decode(
                scanned.Segments.GetSegment(index));
            if (!segment.Success)
            {
                int position = scanned.Segments.GetSegmentStart(index)
                    + (segment.Error ?? throw new InvalidOperationException("A required value is null.")).Position;
                return ParseFailure(new TokenError(
                    "invalid_compact_token",
                    "A compact JOSE segment is not canonical base64url.",
                    position));
            }

            decoded[index] = (segment.Bytes ?? throw new InvalidOperationException("A required value is null."));
            index += 1;
        }

        JoseCompactParseResult result = new JoseCompactParseResult();
        result.Serialization = new JoseCompactSerialization(
            scanned.Segments,
            decoded);
        result.Success = true;
        return result;
    }

    /// <summary>Writes three decoded segments as a canonical JWS compact serialization.</summary>
    /// <param name="protectedHeader">The protected-header UTF-8 bytes.</param>
    /// <param name="payload">The payload bytes.</param>
    /// <param name="signature">The signature or MAC bytes.</param>
    /// <returns>A result containing canonical compact text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenTextResult WriteJws(
        byte[] protectedHeader,
        byte[] payload,
        byte[] signature)
    {
        if (protectedHeader == null)
        {
            throw new ArgumentNullException("protectedHeader");
        }

        if (payload == null)
        {
            throw new ArgumentNullException("payload");
        }

        if (signature == null)
        {
            throw new ArgumentNullException("signature");
        }

        return WriteJws(
            protectedHeader.AsSpan(),
            payload.AsSpan(),
            signature.AsSpan());
    }

    /// <summary>Writes three decoded spans as a canonical JWS compact serialization.</summary>
    /// <param name="protectedHeader">The protected-header UTF-8 bytes.</param>
    /// <param name="payload">The payload bytes.</param>
    /// <param name="signature">The signature or MAC bytes.</param>
    /// <returns>A result containing canonical compact text.</returns>
    internal static TokenTextResult WriteJws(
        ReadOnlySpan<byte> protectedHeader,
        ReadOnlySpan<byte> payload,
        ReadOnlySpan<byte> signature)
    {
        StringBuilder builder = new StringBuilder();
        AppendSegment(builder, protectedHeader, false);
        AppendSegment(builder, payload, true);
        AppendSegment(builder, signature, true);
        return WriteSuccess(builder);
    }

    /// <summary>Writes five decoded segments as a canonical JWE compact serialization.</summary>
    /// <param name="protectedHeader">The protected-header UTF-8 bytes.</param>
    /// <param name="encryptedKey">The encrypted content-encryption key bytes.</param>
    /// <param name="initializationVector">The initialization-vector bytes.</param>
    /// <param name="ciphertext">The ciphertext bytes.</param>
    /// <param name="authenticationTag">The authentication-tag bytes.</param>
    /// <returns>A result containing canonical compact text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenTextResult WriteJwe(
        byte[] protectedHeader,
        byte[] encryptedKey,
        byte[] initializationVector,
        byte[] ciphertext,
        byte[] authenticationTag)
    {
        if (protectedHeader == null)
        {
            throw new ArgumentNullException("protectedHeader");
        }

        if (encryptedKey == null)
        {
            throw new ArgumentNullException("encryptedKey");
        }

        if (initializationVector == null)
        {
            throw new ArgumentNullException("initializationVector");
        }

        if (ciphertext == null)
        {
            throw new ArgumentNullException("ciphertext");
        }

        if (authenticationTag == null)
        {
            throw new ArgumentNullException("authenticationTag");
        }

        return WriteJwe(
            protectedHeader.AsSpan(),
            encryptedKey.AsSpan(),
            initializationVector.AsSpan(),
            ciphertext.AsSpan(),
            authenticationTag.AsSpan());
    }

    /// <summary>Writes five decoded spans as a canonical JWE compact serialization.</summary>
    /// <param name="protectedHeader">The protected-header UTF-8 bytes.</param>
    /// <param name="encryptedKey">The encrypted content-encryption key bytes.</param>
    /// <param name="initializationVector">The initialization-vector bytes.</param>
    /// <param name="ciphertext">The ciphertext bytes.</param>
    /// <param name="authenticationTag">The authentication-tag bytes.</param>
    /// <returns>A result containing canonical compact text.</returns>
    internal static TokenTextResult WriteJwe(
        ReadOnlySpan<byte> protectedHeader,
        ReadOnlySpan<byte> encryptedKey,
        ReadOnlySpan<byte> initializationVector,
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> authenticationTag)
    {
        StringBuilder builder = new StringBuilder();
        AppendSegment(builder, protectedHeader, false);
        AppendSegment(builder, encryptedKey, true);
        AppendSegment(builder, initializationVector, true);
        AppendSegment(builder, ciphertext, true);
        AppendSegment(builder, authenticationTag, true);
        return WriteSuccess(builder);
    }

    /// <summary>Writes three or five decoded segments as canonical compact JOSE text.</summary>
    /// <param name="segments">The decoded JWS or JWE segments in serialization order.</param>
    /// <returns>A result containing canonical compact text, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>segments</c> is null.</exception>
    internal static TokenTextResult Write(byte[][] segments)
    {
        if (segments == null)
        {
            throw new ArgumentNullException("segments");
        }

        if (segments.Length != 3 && segments.Length != 5)
        {
            return WriteFailure(
                "invalid_compact_segment_count",
                "A compact JOSE serialization must contain three or five segments.",
                segments.Length);
        }

        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < segments.Length)
        {
            if (segments[index] == null)
            {
                return WriteFailure(
                    "invalid_compact_segment",
                    "A compact JOSE segment must not be null.",
                    index);
            }

            AppendSegment(builder, segments[index].AsSpan(), index > 0);
            index += 1;
        }

        return WriteSuccess(builder);
    }

    static bool IsBase64UrlCharacter(char value)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return true;
        }

        if (value >= 'a' && value <= 'z')
        {
            return true;
        }

        if (value >= '0' && value <= '9')
        {
            return true;
        }

        return value == '-' || value == '_';
    }

    static JoseCompactParseResult ParseFailure(TokenError? error)
    {
        JoseCompactParseResult result = new JoseCompactParseResult();
        result.Error = error;
        return result;
    }

    static void AppendSegment(
        StringBuilder builder,
        ReadOnlySpan<byte> segment,
        bool separator)
    {
        if (separator)
        {
            builder.Append('.');
        }

        TokenTextResult encoded = Base64Url.Encode(segment);
        builder.Append(encoded.Text);
    }

    static TokenTextResult WriteSuccess(StringBuilder builder)
    {
        TokenTextResult result = new TokenTextResult();
        result.Text = builder.ToString();
        result.Success = true;
        return result;
    }

    static TokenTextResult WriteFailure(
        string code,
        string message,
        int position)
    {
        TokenTextResult result = new TokenTextResult();
        result.Error = new TokenError(code, message, position);
        return result;
    }
}
