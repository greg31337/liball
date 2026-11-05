using System;

namespace webp;

/// <summary>Detects WebP data, MIME types, and file extensions without parsing or decoding an image.</summary>
/// <remarks>This narrow contract is intended for dependency injection and performs case-insensitive MIME type and file-extension comparisons.</remarks>
public interface IWebpFormatDetector
{
    /// <summary>Determines whether a byte array starts with the RIFF WEBP signature.</summary>
    /// <param name="data">The bytes to inspect. Only the twelve-byte RIFF header is required.</param>
    /// <returns>True when the bytes start with <c>RIFF....WEBP</c>; otherwise, false.</returns>
    bool HasRiffWebpSignature(byte[] data);

    /// <summary>Determines whether a byte span starts with the RIFF WEBP signature.</summary>
    /// <param name="data">The bytes to inspect. Only the twelve-byte RIFF header is required.</param>
    /// <returns>True when the bytes start with <c>RIFF....WEBP</c>; otherwise, false.</returns>
    bool HasRiffWebpSignatureSpan(ReadOnlySpan<byte> data);

    /// <summary>Determines whether a MIME type is the WebP MIME type.</summary>
    /// <param name="mimeType">The MIME type to inspect.</param>
    /// <returns>True for <c>image/webp</c>, compared without case sensitivity; otherwise, false.</returns>
    bool IsWebpMimeType(string mimeType);

    /// <summary>Determines whether a file extension is a WebP extension.</summary>
    /// <param name="extension">The extension to inspect, with or without a leading period.</param>
    /// <returns>True for <c>.webp</c> or <c>webp</c>, compared without case sensitivity; otherwise, false.</returns>
    bool IsWebpFileExtension(string extension);
}
