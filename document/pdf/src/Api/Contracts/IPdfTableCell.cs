using System;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfTableCell"/>.
/// </summary>
public interface IPdfTableCell
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
    /// Applies a defensive copy of a reusable element style.
    /// </summary>
    /// <param name="style">The element style to apply.</param>
    void SetStyle(PdfElementStyle style);

    /// <summary>
    /// Sets the background used for subsequent PDF generation.
    /// </summary>
    /// <param name="color">The PDF color to apply.</param>
    void SetBackground(PdfColor color);

    /// <summary>
    /// Clears the current background.
    /// </summary>
    void ClearBackground();

    /// <summary>
    /// Sets the border color used for subsequent PDF generation.
    /// </summary>
    /// <param name="color">The PDF color to apply.</param>
    void SetBorderColor(PdfColor color);

    /// <summary>
    /// Sets the number of consecutive columns occupied by this cell.
    /// </summary>
    /// <param name="columnSpan">The positive column span.</param>
    void SetColumnSpan(int columnSpan);

    /// <summary>
    /// Sets the number of consecutive rows occupied by this cell.
    /// </summary>
    /// <param name="rowSpan">The positive row span.</param>
    void SetRowSpan(int rowSpan);

    /// <summary>
    /// Returns the configured column span.
    /// </summary>
    /// <returns>The positive column span.</returns>
    int GetColumnSpan();

    /// <summary>
    /// Returns the configured row span.
    /// </summary>
    /// <returns>The positive row span.</returns>
    int GetRowSpan();

    /// <summary>
    /// Measures this layout element against the available area without emitting drawing commands.
    /// </summary>
    /// <param name="context">The available layout area and drawing state for this operation.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    PdfLayoutResult Measure(PdfLayoutContext context);

    /// <summary>
    /// Draws this measured layout element into the supplied page area.
    /// </summary>
    /// <param name="context">The available layout area and drawing state for this operation.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    PdfLayoutResult Draw(PdfLayoutDrawContext context);
}
