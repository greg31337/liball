using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxImageDetector values.
/// </summary>
public class PptxImageDetector : IPptxImageDetector
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxImageDetector()
    {
    }

    /// <summary>
    /// Detects the image format and dimensions from the supplied binary data.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <returns>The resulting image info value.</returns>
    public PptxImageInfo Detect(byte[] bytes)
    {
        return PptxImageDetectorCore.Detect(bytes);
    }

    /// <summary>
    /// Detects the image format and dimensions without requiring an array allocation.
    /// </summary>
    /// <param name="bytes">The binary payload to inspect.</param>
    /// <returns>The resulting image info value.</returns>
    public PptxImageInfo DetectSpan(System.ReadOnlySpan<byte> bytes)
    {
        return PptxImageDetectorCore.DetectSpan(bytes);
    }

    /// <summary>
    /// Detects the image format and dimensions from the supplied binary data.
    /// </summary>
    /// <param name="part">The part value to use.</param>
    /// <returns>The resulting image info value.</returns>
    public PptxImageInfo DetectPart(PptxPart part)
    {
        return PptxImageDetectorCore.DetectPart(part);
    }
}
