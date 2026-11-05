using System;
using System.IO;

namespace image;

/// <summary>Detects image formats from signatures and container data.</summary>
public class ImageFormatDetector : IImageFormatDetector
{
    /// <summary>Creates a stateless image format detector.</summary>
    public ImageFormatDetector()
    {
    }

    /// <summary>Detects an image format from an in-memory byte sequence.</summary>
    /// <param name="bytes">The bytes to inspect, or null.</param>
    /// <returns>A description of the detected format.</returns>
    public ImageFormatInfo DetectBytes(byte[]? bytes)
    {
        return ImageFormatDetectorCore.DetectBytes(bytes);
    }

    /// <summary>Detects an image format from an in-memory byte sequence and a filename or extension hint.</summary>
    /// <param name="bytes">The bytes to inspect, or null.</param>
    /// <param name="fileNameOrExtension">A filename, path, or extension such as <c>.png</c>, or null.</param>
    /// <returns>A description of the detected format.</returns>
    public ImageFormatInfo DetectBytesWithHint(byte[]? bytes, string? fileNameOrExtension)
    {
        return ImageFormatDetectorCore.DetectBytesWithHint(bytes, fileNameOrExtension);
    }

    /// <summary>Detects an image format from a caller-owned byte span without copying the probe.</summary>
    /// <param name="bytes">The bytes to inspect.</param>
    /// <returns>A description of the detected format.</returns>
    public ImageFormatInfo DetectSpan(ReadOnlySpan<byte> bytes)
    {
        return ImageFormatDetectorCore.DetectSpan(bytes);
    }

    /// <summary>Detects an image format from a caller-owned byte span and a filename or extension hint without copying the probe.</summary>
    /// <param name="bytes">The bytes to inspect.</param>
    /// <param name="fileNameOrExtension">A filename, path, or extension such as <c>.png</c>, or null.</param>
    /// <returns>A description of the detected format.</returns>
    public ImageFormatInfo DetectSpanWithHint(ReadOnlySpan<byte> bytes, string? fileNameOrExtension)
    {
        return ImageFormatDetectorCore.DetectSpanWithHint(bytes, fileNameOrExtension);
    }

    /// <summary>Detects an image format from the current position of a stream.</summary>
    /// <param name="stream">The readable stream to inspect, or null.</param>
    /// <returns>A description of the detected format.</returns>
    public ImageFormatInfo DetectStream(Stream? stream)
    {
        return ImageFormatDetectorCore.DetectStream(stream);
    }

    /// <summary>Detects an image format from the current position of a stream and a filename or extension hint.</summary>
    /// <param name="stream">The readable stream to inspect, or null.</param>
    /// <param name="fileNameOrExtension">A filename, path, or extension such as <c>.png</c>, or null.</param>
    /// <returns>A description of the detected format.</returns>
    public ImageFormatInfo DetectStreamWithHint(Stream? stream, string? fileNameOrExtension)
    {
        return ImageFormatDetectorCore.DetectStreamWithHint(stream, fileNameOrExtension);
    }
}
