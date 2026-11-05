using System;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfImage"/>.
/// </summary>
public interface IPdfImage
{
    /// <summary>
    /// Returns the source representation used to create this image.
    /// </summary>
    /// <returns>The image source format.</returns>
    PdfImageFormat GetFormat();

    /// <summary>
    /// Returns the device color space used by this image.
    /// </summary>
    /// <returns>The image device color space.</returns>
    PdfImageColorSpace GetColorSpace();

    /// <summary>
    /// Returns the image width in pixels.
    /// </summary>
    /// <returns>The positive pixel width.</returns>
    int GetPixelWidth();

    /// <summary>
    /// Returns the image height in pixels.
    /// </summary>
    /// <returns>The positive pixel height.</returns>
    int GetPixelHeight();

    /// <summary>
    /// Indicates whether the image has alpha or transparency samples.
    /// </summary>
    /// <returns><c>true</c> when the image uses a soft mask; otherwise <c>false</c>.</returns>
    bool HasAlpha();

    /// <summary>
    /// Indicates whether physical resolution metadata is available.
    /// </summary>
    /// <returns><c>true</c> when horizontal and vertical DPI values are available; otherwise <c>false</c>.</returns>
    bool HasDpi();

    /// <summary>
    /// Returns horizontal resolution in dots per inch, or zero when unavailable.
    /// </summary>
    /// <returns>The horizontal dots per inch.</returns>
    double GetHorizontalDpi();

    /// <summary>
    /// Returns vertical resolution in dots per inch, or zero when unavailable.
    /// </summary>
    /// <returns>The vertical dots per inch.</returns>
    double GetVerticalDpi();

    /// <summary>
    /// Sets physical resolution metadata for this image.
    /// </summary>
    /// <param name="horizontalDpi">The positive horizontal dots per inch.</param>
    /// <param name="verticalDpi">The positive vertical dots per inch.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a DPI value is not positive and finite.</exception>
    void SetDpi(double horizontalDpi, double verticalDpi);

    /// <summary>
    /// Sets the alternate text used for subsequent PDF generation.
    /// </summary>
    /// <param name="alternateText">The alternate text to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    void SetAlternateText(string alternateText);

    /// <summary>
    /// Marks this image as decorative.
    /// </summary>
    void MarkDecorative();
}
