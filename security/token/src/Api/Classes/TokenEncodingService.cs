using System;
namespace token;

/// <summary>Implements strict foundational encoding operations for token formats.</summary>
public class TokenEncodingService : ITokenEncodingService
{
    /// <summary>Initializes a stateless token encoding service.</summary>
    public TokenEncodingService()
    {
    }

    /// <summary>Encodes text as strict UTF-8.</summary>
    /// <param name="text">The text to encode.</param>
    /// <returns>The UTF-8 bytes, or stable failure details.</returns>
    public TokenBytesResult EncodeUtf8(string text)
    {
        return Utf8Codec.Encode(text);
    }

    /// <summary>Encodes a character span as strict UTF-8.</summary>
    /// <param name="text">The characters to encode.</param>
    /// <returns>The UTF-8 bytes, or stable failure details.</returns>
    public TokenBytesResult EncodeUtf8Span(ReadOnlySpan<char> text)
    {
        return Utf8Codec.Encode(text);
    }

    /// <summary>Decodes strict UTF-8 bytes.</summary>
    /// <param name="bytes">The bytes to decode.</param>
    /// <returns>The decoded text, or stable failure details.</returns>
    public TokenTextResult DecodeUtf8(byte[] bytes)
    {
        return Utf8Codec.Decode(bytes);
    }

    /// <summary>Decodes a strict UTF-8 byte span.</summary>
    /// <param name="bytes">The bytes to decode.</param>
    /// <returns>The decoded text, or stable failure details.</returns>
    public TokenTextResult DecodeUtf8Span(ReadOnlySpan<byte> bytes)
    {
        return Utf8Codec.Decode(bytes);
    }

    /// <summary>Encodes bytes as canonical unpadded base64url text.</summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The canonical base64url text.</returns>
    public TokenTextResult EncodeBase64Url(byte[] bytes)
    {
        return Base64Url.Encode(bytes);
    }

    /// <summary>Encodes a byte span as canonical unpadded base64url text.</summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The canonical base64url text.</returns>
    public TokenTextResult EncodeBase64UrlSpan(ReadOnlySpan<byte> bytes)
    {
        return Base64Url.Encode(bytes);
    }

    /// <summary>Decodes canonical unpadded base64url text.</summary>
    /// <param name="text">The base64url text.</param>
    /// <returns>The decoded bytes, or stable failure details.</returns>
    public TokenBytesResult DecodeBase64Url(string text)
    {
        return Base64Url.Decode(text);
    }

    /// <summary>Decodes a canonical unpadded base64url span.</summary>
    /// <param name="text">The base64url characters.</param>
    /// <returns>The decoded bytes, or stable failure details.</returns>
    public TokenBytesResult DecodeBase64UrlSpan(ReadOnlySpan<char> text)
    {
        return Base64Url.Decode(text);
    }

    /// <summary>Decodes base64url text with valid optional padding.</summary>
    /// <param name="text">The compatible base64url text.</param>
    /// <returns>The decoded bytes, or stable failure details.</returns>
    public TokenBytesResult DecodeCompatibleBase64Url(string text)
    {
        return Base64Url.Decode(text, true);
    }

    /// <summary>Decodes a base64url span with valid optional padding.</summary>
    /// <param name="text">The compatible base64url characters.</param>
    /// <returns>The decoded bytes, or stable failure details.</returns>
    public TokenBytesResult DecodeCompatibleBase64UrlSpan(ReadOnlySpan<char> text)
    {
        return Base64Url.Decode(text, true);
    }

    /// <summary>Encodes bytes as canonical padded standard base64 text.</summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The canonical standard base64 text.</returns>
    public TokenTextResult EncodeBase64(byte[] bytes)
    {
        return Base64Codec.Encode(bytes);
    }

    /// <summary>Encodes a byte span as canonical padded standard base64 text.</summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The canonical standard base64 text.</returns>
    public TokenTextResult EncodeBase64Span(ReadOnlySpan<byte> bytes)
    {
        return Base64Codec.Encode(bytes);
    }

    /// <summary>Decodes canonical padded standard base64 text.</summary>
    /// <param name="text">The standard base64 text.</param>
    /// <returns>The decoded bytes, or stable failure details.</returns>
    public TokenBytesResult DecodeBase64(string text)
    {
        return Base64Codec.Decode(text);
    }

    /// <summary>Decodes a canonical padded standard base64 span.</summary>
    /// <param name="text">The standard base64 characters.</param>
    /// <returns>The decoded bytes, or stable failure details.</returns>
    public TokenBytesResult DecodeBase64Span(ReadOnlySpan<char> text)
    {
        return Base64Codec.Decode(text);
    }

    /// <summary>Decodes standard base64 text with ASCII whitespace.</summary>
    /// <param name="text">The whitespace-delimited base64 text.</param>
    /// <returns>The decoded bytes, or stable failure details.</returns>
    public TokenBytesResult DecodeWhitespaceBase64(string text)
    {
        return Base64Codec.Decode(text, true);
    }

    /// <summary>Decodes a standard base64 span with ASCII whitespace.</summary>
    /// <param name="text">The whitespace-delimited base64 characters.</param>
    /// <returns>The decoded bytes, or stable failure details.</returns>
    public TokenBytesResult DecodeWhitespaceBase64Span(ReadOnlySpan<char> text)
    {
        return Base64Codec.Decode(text, true);
    }

    /// <summary>Encodes bytes as lowercase hexadecimal text.</summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The lowercase hexadecimal text.</returns>
    public TokenTextResult EncodeHex(byte[] bytes)
    {
        return HexCodec.Encode(bytes);
    }

    /// <summary>Encodes a byte span as lowercase hexadecimal text.</summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The lowercase hexadecimal text.</returns>
    public TokenTextResult EncodeHexSpan(ReadOnlySpan<byte> bytes)
    {
        return HexCodec.Encode(bytes);
    }

    /// <summary>Decodes strict hexadecimal text using either letter case.</summary>
    /// <param name="text">The hexadecimal text.</param>
    /// <returns>The decoded bytes, or stable failure details.</returns>
    public TokenBytesResult DecodeHex(string text)
    {
        return HexCodec.Decode(text);
    }

    /// <summary>Decodes a strict hexadecimal span using either letter case.</summary>
    /// <param name="text">The hexadecimal characters.</param>
    /// <returns>The decoded bytes, or stable failure details.</returns>
    public TokenBytesResult DecodeHexSpan(ReadOnlySpan<char> text)
    {
        return HexCodec.Decode(text);
    }

    /// <summary>Writes a canonical PEM block.</summary>
    /// <param name="label">The PEM label.</param>
    /// <param name="bytes">The binary block contents.</param>
    /// <returns>The PEM text, or stable failure details.</returns>
    public TokenTextResult WritePemBlock(string label, byte[] bytes)
    {
        return PemCodec.Write(label, bytes);
    }

    /// <summary>Writes a canonical PEM block from a byte span.</summary>
    /// <param name="label">The PEM label.</param>
    /// <param name="bytes">The binary block contents.</param>
    /// <returns>The PEM text, or stable failure details.</returns>
    public TokenTextResult WritePemBlockSpan(string label, ReadOnlySpan<byte> bytes)
    {
        return PemCodec.Write(label, bytes);
    }

    /// <summary>Reads one PEM block.</summary>
    /// <param name="text">The PEM text.</param>
    /// <returns>The decoded PEM block, or stable failure details.</returns>
    public PemReadResult ReadPemBlock(string text)
    {
        return PemCodec.Read(text);
    }

    /// <summary>Scans a size-limited compact JWS or JWE.</summary>
    /// <param name="text">The compact token text.</param>
    /// <param name="maximumLength">The maximum accepted character count.</param>
    /// <returns>The segment boundaries, or stable failure details.</returns>
    public JoseCompactScanResult ScanCompactToken(string text, int maximumLength)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (maximumLength < 0)
        {
            throw new ArgumentOutOfRangeException("maximumLength");
        }

        if (text.Length > maximumLength)
        {
            JoseCompactScanResult result = new JoseCompactScanResult();
            result.Error = new TokenError(
                "buffer_limit_exceeded",
                "The compact token exceeds the configured maximum length.",
                maximumLength);
            return result;
        }

        return JoseCompact.Scan(text);
    }
}
