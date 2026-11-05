using System;
namespace token;

/// <summary>Provides strict token text, binary, PEM, and compact-segment encoding operations.</summary>
public interface ITokenEncodingService
{
    /// <summary>Encodes text as strict UTF-8.</summary>
    /// <param name="text">The text to encode.</param>
    /// <returns>The UTF-8 bytes, or stable failure details.</returns>
    TokenBytesResult EncodeUtf8(string text);

    /// <summary>Encodes a character span as strict UTF-8 without an intermediate string.</summary>
    /// <param name="text">The characters to encode.</param>
    /// <returns>The UTF-8 bytes, or stable failure details.</returns>
    TokenBytesResult EncodeUtf8Span(ReadOnlySpan<char> text);

    /// <summary>Decodes strict UTF-8 bytes.</summary>
    /// <param name="bytes">The bytes to decode.</param>
    /// <returns>The decoded text, or stable failure details.</returns>
    TokenTextResult DecodeUtf8(byte[] bytes);

    /// <summary>Decodes a strict UTF-8 byte span without an intermediate array.</summary>
    /// <param name="bytes">The bytes to decode.</param>
    /// <returns>The decoded text, or stable failure details.</returns>
    TokenTextResult DecodeUtf8Span(ReadOnlySpan<byte> bytes);

    /// <summary>Encodes bytes as canonical unpadded base64url text.</summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The canonical base64url text.</returns>
    TokenTextResult EncodeBase64Url(byte[] bytes);

    /// <summary>Encodes a byte span as canonical unpadded base64url text.</summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The canonical base64url text.</returns>
    TokenTextResult EncodeBase64UrlSpan(ReadOnlySpan<byte> bytes);

    /// <summary>Decodes canonical unpadded base64url text.</summary>
    /// <param name="text">The base64url text.</param>
    /// <returns>The decoded bytes, or stable failure details.</returns>
    TokenBytesResult DecodeBase64Url(string text);

    /// <summary>Decodes a canonical unpadded base64url character span.</summary>
    /// <param name="text">The base64url characters.</param>
    /// <returns>The decoded bytes, or stable failure details.</returns>
    TokenBytesResult DecodeBase64UrlSpan(ReadOnlySpan<char> text);

    /// <summary>Decodes base64url text while accepting valid optional padding.</summary>
    /// <param name="text">The compatible base64url text.</param>
    /// <returns>The decoded bytes, or stable failure details.</returns>
    TokenBytesResult DecodeCompatibleBase64Url(string text);

    /// <summary>Decodes a base64url span while accepting valid optional padding.</summary>
    /// <param name="text">The compatible base64url characters.</param>
    /// <returns>The decoded bytes, or stable failure details.</returns>
    TokenBytesResult DecodeCompatibleBase64UrlSpan(ReadOnlySpan<char> text);

    /// <summary>Encodes bytes as canonical padded standard base64 text.</summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The canonical standard base64 text.</returns>
    TokenTextResult EncodeBase64(byte[] bytes);

    /// <summary>Encodes a byte span as canonical padded standard base64 text.</summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The canonical standard base64 text.</returns>
    TokenTextResult EncodeBase64Span(ReadOnlySpan<byte> bytes);

    /// <summary>Decodes canonical padded standard base64 text without whitespace.</summary>
    /// <param name="text">The standard base64 text.</param>
    /// <returns>The decoded bytes, or stable failure details.</returns>
    TokenBytesResult DecodeBase64(string text);

    /// <summary>Decodes a canonical padded standard base64 span without whitespace.</summary>
    /// <param name="text">The standard base64 characters.</param>
    /// <returns>The decoded bytes, or stable failure details.</returns>
    TokenBytesResult DecodeBase64Span(ReadOnlySpan<char> text);

    /// <summary>Decodes standard base64 text while accepting ASCII whitespace.</summary>
    /// <param name="text">The whitespace-delimited base64 text.</param>
    /// <returns>The decoded bytes, or stable failure details.</returns>
    TokenBytesResult DecodeWhitespaceBase64(string text);

    /// <summary>Decodes a standard base64 span while accepting ASCII whitespace.</summary>
    /// <param name="text">The whitespace-delimited base64 characters.</param>
    /// <returns>The decoded bytes, or stable failure details.</returns>
    TokenBytesResult DecodeWhitespaceBase64Span(ReadOnlySpan<char> text);

    /// <summary>Encodes bytes as lowercase hexadecimal text.</summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The lowercase hexadecimal text.</returns>
    TokenTextResult EncodeHex(byte[] bytes);

    /// <summary>Encodes a byte span as lowercase hexadecimal text.</summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The lowercase hexadecimal text.</returns>
    TokenTextResult EncodeHexSpan(ReadOnlySpan<byte> bytes);

    /// <summary>Decodes strict hexadecimal text using either letter case.</summary>
    /// <param name="text">The hexadecimal text.</param>
    /// <returns>The decoded bytes, or stable failure details.</returns>
    TokenBytesResult DecodeHex(string text);

    /// <summary>Decodes a strict hexadecimal span using either letter case.</summary>
    /// <param name="text">The hexadecimal characters.</param>
    /// <returns>The decoded bytes, or stable failure details.</returns>
    TokenBytesResult DecodeHexSpan(ReadOnlySpan<char> text);

    /// <summary>Writes a canonical PEM block.</summary>
    /// <param name="label">The PEM label.</param>
    /// <param name="bytes">The binary block contents.</param>
    /// <returns>The PEM text, or stable failure details.</returns>
    TokenTextResult WritePemBlock(string label, byte[] bytes);

    /// <summary>Writes a canonical PEM block from a byte span.</summary>
    /// <param name="label">The PEM label.</param>
    /// <param name="bytes">The binary block contents.</param>
    /// <returns>The PEM text, or stable failure details.</returns>
    TokenTextResult WritePemBlockSpan(string label, ReadOnlySpan<byte> bytes);

    /// <summary>Reads one PEM block with optional surrounding ASCII whitespace.</summary>
    /// <param name="text">The PEM text.</param>
    /// <returns>The decoded PEM block, or stable failure details.</returns>
    PemReadResult ReadPemBlock(string text);

    /// <summary>Scans a size-limited compact JWS or JWE into segment boundaries.</summary>
    /// <param name="text">The compact token text.</param>
    /// <param name="maximumLength">The maximum accepted character count.</param>
    /// <returns>The segment boundaries, or stable failure details.</returns>
    JoseCompactScanResult ScanCompactToken(string text, int maximumLength);
}
