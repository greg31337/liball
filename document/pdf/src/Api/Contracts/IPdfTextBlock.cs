using System;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfTextBlock"/>.
/// </summary>
public interface IPdfTextBlock
{
    /// <summary>
    /// Sets the padding used for subsequent PDF generation.
    /// </summary>
    /// <param name="padding">The padding to use for this operation.</param>
    void SetPadding(PdfInsets padding);

    /// <summary>
    /// Sets the border used for subsequent PDF generation.
    /// </summary>
    /// <param name="border">The border to use for this operation.</param>
    void SetBorder(PdfInsets border);

    /// <summary>
    /// Sets the margin used for subsequent PDF generation.
    /// </summary>
    /// <param name="margin">The margin to use for this operation.</param>
    void SetMargin(PdfInsets margin);

    /// <summary>
    /// Applies a defensive copy of a reusable element style.
    /// </summary>
    /// <param name="style">The element style to apply.</param>
    void SetStyle(PdfElementStyle style);

    /// <summary>
    /// Replaces the paragraph style used to measure and draw this text block.
    /// </summary>
    /// <param name="style">The paragraph style to apply.</param>
    void SetParagraphStyle(PdfParagraphStyle style);
}
