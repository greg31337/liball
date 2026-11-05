using System;
using System.IO;

namespace webp;

/// <summary>Decodes complete RIFF WebP files supplied in memory, through streams, or by file path.</summary>
/// <remarks>This narrow contract is intended for dependency injection.</remarks>
public interface IWebpDecoder
{
    /// <summary>Decodes a complete RIFF WebP file using the supplied options.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="options">The settings that control parsing and decoded output.</param>
    /// <returns>The decoded image and any format diagnostics.</returns>
    WebpDecodeResult Decode(byte[] data, WebpDecodeOptions options);

    /// <summary>Decodes a complete RIFF WebP file from a read-only span using the supplied options.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="options">The settings that control parsing and decoded output.</param>
    /// <returns>The decoded image and any format diagnostics.</returns>
    WebpDecodeResult DecodeSpan(ReadOnlySpan<byte> data, WebpDecodeOptions options);

    /// <summary>Decodes a complete RIFF WebP file from the current position of a readable stream.</summary>
    /// <remarks>The stream may be seekable or non-seekable, is advanced to its end, and remains open.</remarks>
    /// <param name="stream">The caller-owned stream to read.</param>
    /// <param name="options">The settings that control parsing and decoded output.</param>
    /// <returns>The decoded image and any format diagnostics.</returns>
    WebpDecodeResult DecodeStream(Stream stream, WebpDecodeOptions options);

    /// <summary>Reads and decodes a complete RIFF WebP file from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="options">The settings that control parsing and decoded output.</param>
    /// <returns>The decoded image and any format diagnostics.</returns>
    WebpDecodeResult DecodeFile(string path, WebpDecodeOptions options);
}

/// <summary>Encodes pixel buffers and image models as complete RIFF WebP byte arrays, streams, or files.</summary>
/// <remarks>This narrow contract is intended for dependency injection.</remarks>
public interface IWebpEncoder
{
    /// <summary>Encodes one RGBA image using the supplied options.</summary>
    /// <param name="buffer">The RGBA pixels to encode.</param>
    /// <param name="options">The compression, alpha, preservation, and safety settings.</param>
    /// <returns>A complete RIFF WebP file.</returns>
    byte[] Encode(WebpPixelBuffer buffer, WebpEncodeOptions options);

    /// <summary>Encodes a still or animated image model using the supplied options.</summary>
    /// <param name="image">The image model containing at least one frame.</param>
    /// <param name="options">The compression, alpha, preservation, and safety settings.</param>
    /// <returns>A complete RIFF WebP file.</returns>
    byte[] EncodeImage(WebpImage image, WebpEncodeOptions options);

    /// <summary>Encodes one RGBA image at the current position of a caller-owned stream.</summary>
    /// <remarks>The stream remains open and its position advances by the encoded file length.</remarks>
    /// <param name="buffer">The RGBA pixels to encode.</param>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="options">The compression, alpha, preservation, and safety settings.</param>
    void EncodeStream(WebpPixelBuffer buffer, Stream stream, WebpEncodeOptions options);

    /// <summary>Encodes one RGBA image to a file, replacing any existing file contents.</summary>
    /// <param name="buffer">The RGBA pixels to encode.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">The compression, alpha, preservation, and safety settings.</param>
    void EncodeFile(WebpPixelBuffer buffer, string path, WebpEncodeOptions options);

    /// <summary>Encodes a still or animated image model at the current position of a caller-owned stream.</summary>
    /// <remarks>The stream remains open and its position advances by the encoded file length.</remarks>
    /// <param name="image">The image model containing at least one frame.</param>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="options">The compression, alpha, preservation, and safety settings.</param>
    void EncodeImageStream(WebpImage image, Stream stream, WebpEncodeOptions options);

    /// <summary>Encodes a still or animated image model to a file, replacing any existing file contents.</summary>
    /// <param name="image">The image model containing at least one frame.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">The compression, alpha, preservation, and safety settings.</param>
    void EncodeImageFile(WebpImage image, string path, WebpEncodeOptions options);
}

/// <summary>Reads WebP metadata and structure from memory, streams, or file paths without decoding pixels.</summary>
/// <remarks>This narrow contract is intended for dependency injection. It uses decode options so callers can apply the same strictness policy as decoding and validation.</remarks>
public interface IWebpInfoReader
{
    /// <summary>Identifies a complete RIFF WebP file using the supplied options.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="options">Decode options whose <c>Strictness</c> setting controls format diagnostics.</param>
    /// <returns>Container information and any format diagnostics.</returns>
    WebpInfo Read(byte[] data, WebpDecodeOptions options);

    /// <summary>Identifies a complete RIFF WebP file from a read-only span using the supplied options.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="options">Decode options whose <c>Strictness</c> setting controls format diagnostics.</param>
    /// <returns>Container information and any format diagnostics.</returns>
    WebpInfo ReadSpan(ReadOnlySpan<byte> data, WebpDecodeOptions options);

    /// <summary>Identifies a complete RIFF WebP file from the current position of a readable stream.</summary>
    /// <remarks>The stream may be seekable or non-seekable, is advanced to its end, and remains open.</remarks>
    /// <param name="stream">The caller-owned stream to read.</param>
    /// <param name="options">Decode options whose <c>Strictness</c> setting controls format diagnostics.</param>
    /// <returns>Container information and any format diagnostics.</returns>
    WebpInfo ReadStream(Stream stream, WebpDecodeOptions options);

    /// <summary>Reads and identifies a complete RIFF WebP file from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="options">Decode options whose <c>Strictness</c> setting controls format diagnostics.</param>
    /// <returns>Container information and any format diagnostics.</returns>
    WebpInfo ReadFile(string path, WebpDecodeOptions options);
}

/// <summary>Validates complete RIFF WebP files supplied in memory, through streams, or by file path without decoding pixels.</summary>
/// <remarks>This narrow contract is intended for dependency injection. It uses decode options so callers can share strictness policy across inspection and decoding.</remarks>
public interface IWebpValidator
{
    /// <summary>Validates a complete RIFF WebP file using the supplied options.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="options">Decode options whose <c>Strictness</c> setting controls warnings and errors.</param>
    /// <returns>A validity flag and all discovered diagnostics.</returns>
    WebpValidationResult Validate(byte[] data, WebpDecodeOptions options);

    /// <summary>Validates a complete RIFF WebP file from a read-only span using the supplied options.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="options">Decode options whose <c>Strictness</c> setting controls warnings and errors.</param>
    /// <returns>A validity flag and all discovered diagnostics.</returns>
    WebpValidationResult ValidateSpan(ReadOnlySpan<byte> data, WebpDecodeOptions options);

    /// <summary>Validates a complete RIFF WebP file from the current position of a readable stream.</summary>
    /// <remarks>The stream may be seekable or non-seekable, is advanced to its end, and remains open.</remarks>
    /// <param name="stream">The caller-owned stream to read.</param>
    /// <param name="options">Decode options whose <c>Strictness</c> setting controls warnings and errors.</param>
    /// <returns>A validity flag and all discovered diagnostics.</returns>
    WebpValidationResult ValidateStream(Stream stream, WebpDecodeOptions options);

    /// <summary>Reads and validates a complete RIFF WebP file from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="options">Decode options whose <c>Strictness</c> setting controls warnings and errors.</param>
    /// <returns>A validity flag and all discovered diagnostics.</returns>
    WebpValidationResult ValidateFile(string path, WebpDecodeOptions options);
}
