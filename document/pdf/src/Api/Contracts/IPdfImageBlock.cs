using System;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfImageBlock"/>.
/// </summary>
public interface IPdfImageBlock
{
    /// <summary>
    /// Sets how the image is placed and clipped inside its layout box.
    /// </summary>
    /// <param name="fitMode">The image fit mode to use.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the fit mode is unsupported.</exception>
    void SetFitMode(PdfImageFitMode fitMode);

    /// <summary>
    /// Returns how the image is placed and clipped inside its layout box.
    /// </summary>
    /// <returns>The current image fit mode.</returns>
    PdfImageFitMode GetFitMode();
}
