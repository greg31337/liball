using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxImageDetector"/>.
/// </summary>
public interface IPptxImageDetector
{
    /// <summary>
    /// Detects the image format and dimensions from the supplied binary data.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <returns>The resulting image info value.</returns>
    PptxImageInfo Detect(byte[] bytes);
    /// <summary>
    /// Detects the image format and dimensions without requiring an array allocation.
    /// </summary>
    /// <param name="bytes">The binary payload to inspect.</param>
    /// <returns>The resulting image info value.</returns>
    PptxImageInfo DetectSpan(System.ReadOnlySpan<byte> bytes);
    /// <summary>
    /// Detects the image format and dimensions from the supplied binary data.
    /// </summary>
    /// <param name="part">The part value to use.</param>
    /// <returns>The resulting image info value.</returns>
    PptxImageInfo DetectPart(PptxPart part);
}
