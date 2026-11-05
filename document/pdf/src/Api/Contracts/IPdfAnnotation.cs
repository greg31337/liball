using System;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfAnnotation"/>.
/// </summary>
public interface IPdfAnnotation
{
    /// <summary>
    /// Sets the color used for subsequent PDF generation.
    /// </summary>
    /// <param name="color">The PDF color to apply.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetColor(PdfColor color);

    /// <summary>
    /// Sets the open used for subsequent PDF generation.
    /// </summary>
    /// <param name="open">The open to use for this operation.</param>
    void SetOpen(bool open);

    /// <summary>
    /// Sets the icon name used for subsequent PDF generation.
    /// </summary>
    /// <param name="iconName">The icon name to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    void SetIconName(string iconName);
}
