using System;
using System.IO;

namespace image;

/// <summary>Defines image format detection operations.</summary>
public interface IImageFormatDetector
{
    /// <summary>Detects an image format from an in-memory byte sequence.</summary>
    /// <param name="bytes">The bytes to inspect, or null.</param>
    /// <returns>A description of the detected format.</returns>
    ImageFormatInfo DetectBytes(byte[]? bytes);

    /// <summary>Detects an image format from an in-memory byte sequence and a filename or extension hint.</summary>
    /// <param name="bytes">The bytes to inspect, or null.</param>
    /// <param name="fileNameOrExtension">A filename, path, or extension such as <c>.png</c>, or null.</param>
    /// <returns>A description of the detected format.</returns>
    ImageFormatInfo DetectBytesWithHint(byte[]? bytes, string? fileNameOrExtension);

    /// <summary>Detects an image format from a caller-owned byte span without copying the probe.</summary>
    /// <param name="bytes">The bytes to inspect.</param>
    /// <returns>A description of the detected format.</returns>
    ImageFormatInfo DetectSpan(ReadOnlySpan<byte> bytes);

    /// <summary>Detects an image format from a caller-owned byte span and a filename or extension hint without copying the probe.</summary>
    /// <param name="bytes">The bytes to inspect.</param>
    /// <param name="fileNameOrExtension">A filename, path, or extension such as <c>.png</c>, or null.</param>
    /// <returns>A description of the detected format.</returns>
    ImageFormatInfo DetectSpanWithHint(ReadOnlySpan<byte> bytes, string? fileNameOrExtension);

    /// <summary>Detects an image format from the current position of a stream.</summary>
    /// <param name="stream">The readable stream to inspect, or null.</param>
    /// <returns>A description of the detected format.</returns>
    ImageFormatInfo DetectStream(Stream? stream);

    /// <summary>Detects an image format from the current position of a stream and a filename or extension hint.</summary>
    /// <param name="stream">The readable stream to inspect, or null.</param>
    /// <param name="fileNameOrExtension">A filename, path, or extension such as <c>.png</c>, or null.</param>
    /// <returns>A description of the detected format.</returns>
    ImageFormatInfo DetectStreamWithHint(Stream? stream, string? fileNameOrExtension);
}
