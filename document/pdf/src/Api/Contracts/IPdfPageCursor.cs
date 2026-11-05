using System;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfPageCursor"/>.
/// </summary>
public interface IPdfPageCursor
{
    /// <summary>
    /// Adds a page to this page cursor and returns the stored value.
    /// </summary>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfPage AddPage();

    /// <summary>
    /// Returns the remaining height calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    double GetRemainingHeight();

    /// <summary>
    /// Returns the available width calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    double GetAvailableWidth();

    /// <summary>
    /// Returns the measure context calculated from the object's current state.
    /// </summary>
    /// <returns>The resulting layout context.</returns>
    PdfLayoutContext GetMeasureContext();

    /// <summary>
    /// Returns the draw context calculated from the object's current state.
    /// </summary>
    /// <param name="result">The result to use for this operation.</param>
    /// <returns>The resulting layout draw context.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfLayoutDrawContext GetDrawContext(PdfLayoutResult result);

    /// <summary>
    /// Measures, paginates, draws, and advances past a layout element.
    /// </summary>
    /// <param name="item">The layout element to flow through the current page.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfLayoutResult DrawItem(IPdfLayoutElement item);

    /// <summary>
    /// Appends drawing commands for a block to the target PDF canvas.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfLayoutResult DrawBlock(PdfBlock block);

    /// <summary>
    /// Appends drawing commands for a text block to the target PDF canvas.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfLayoutResult DrawTextBlock(PdfTextBlock block);

    /// <summary>
    /// Appends drawing commands for a page break block to the target PDF canvas.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfLayoutResult DrawPageBreakBlock(PdfPageBreakBlock block);

    /// <summary>
    /// Appends drawing commands for an ensure space block to the target PDF canvas.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfLayoutResult DrawEnsureSpaceBlock(PdfEnsureSpaceBlock block);

    /// <summary>
    /// Appends drawing commands for an image block to the target PDF canvas.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfLayoutResult DrawImageBlock(PdfImageBlock block);

    /// <summary>
    /// Appends drawing commands for a keep together item to the target PDF canvas.
    /// </summary>
    /// <param name="item">The item to use for this operation.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfLayoutResult DrawKeepTogetherItem(PdfLayoutItem item);

    /// <summary>
    /// Appends drawing commands for a stack block to the target PDF canvas.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfLayoutResult DrawStackBlock(PdfStackBlock block);

    /// <summary>
    /// Appends drawing commands for a row block to the target PDF canvas.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfLayoutResult DrawRowBlock(PdfRowBlock block);

    /// <summary>
    /// Appends drawing commands for a column block to the target PDF canvas.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfLayoutResult DrawColumnBlock(PdfColumnBlock block);

    /// <summary>
    /// Appends drawing commands for a grid block to the target PDF canvas.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfLayoutResult DrawGridBlock(PdfGridBlock block);

    /// <summary>
    /// Appends drawing commands for a table block to the target PDF canvas.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfLayoutResult DrawTableBlock(PdfTableBlock block);

    /// <summary>
    /// Appends drawing commands for a list block to the target PDF canvas.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfLayoutResult DrawListBlock(PdfListBlock block);

    /// <summary>
    /// Sets the header template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetHeaderTemplate(PdfPageTemplate template);

    /// <summary>
    /// Sets the footer template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetFooterTemplate(PdfPageTemplate template);

    /// <summary>
    /// Clears the current header template.
    /// </summary>
    void ClearHeaderTemplate();

    /// <summary>
    /// Clears the current footer template.
    /// </summary>
    void ClearFooterTemplate();

    /// <summary>
    /// Sets the first page header template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetFirstPageHeaderTemplate(PdfPageTemplate template);

    /// <summary>
    /// Sets the first page footer template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetFirstPageFooterTemplate(PdfPageTemplate template);

    /// <summary>
    /// Sets the odd page header template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetOddPageHeaderTemplate(PdfPageTemplate template);

    /// <summary>
    /// Sets the odd page footer template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetOddPageFooterTemplate(PdfPageTemplate template);

    /// <summary>
    /// Sets the even page header template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetEvenPageHeaderTemplate(PdfPageTemplate template);

    /// <summary>
    /// Sets the even page footer template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetEvenPageFooterTemplate(PdfPageTemplate template);

    /// <summary>
    /// Clears the current first page header template.
    /// </summary>
    void ClearFirstPageHeaderTemplate();

    /// <summary>
    /// Clears the current first page footer template.
    /// </summary>
    void ClearFirstPageFooterTemplate();

    /// <summary>
    /// Clears the current odd page header template.
    /// </summary>
    void ClearOddPageHeaderTemplate();

    /// <summary>
    /// Clears the current odd page footer template.
    /// </summary>
    void ClearOddPageFooterTemplate();

    /// <summary>
    /// Clears the current even page header template.
    /// </summary>
    void ClearEvenPageHeaderTemplate();

    /// <summary>
    /// Clears the current even page footer template.
    /// </summary>
    void ClearEvenPageFooterTemplate();

    /// <summary>
    /// Enables debug overlay for subsequent operations.
    /// </summary>
    void EnableDebugOverlay();

    /// <summary>
    /// Enables debug overlay for subsequent operations.
    /// </summary>
    /// <param name="color">The PDF color to apply.</param>
    /// <param name="lineWidth">The line width in PDF points.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    void EnableDebugOverlayWithStyle(PdfColor color, double lineWidth);

    /// <summary>
    /// Disables debug overlay for subsequent operations.
    /// </summary>
    void DisableDebugOverlay();

    /// <summary>
    /// Appends drawing commands for a final page templates to the target PDF canvas.
    /// </summary>
    void DrawFinalPageTemplates();

    /// <summary>
    /// Appends drawing commands for a background layer to the target PDF canvas.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void DrawBackgroundLayer(PdfPageTemplate template);

    /// <summary>
    /// Appends drawing commands for a watermark layer to the target PDF canvas.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void DrawWatermarkLayer(PdfPageTemplate template);

    /// <summary>
    /// Ensures that space, creating a new page when necessary.
    /// </summary>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    void EnsureSpace(double height);

    /// <summary>
    /// Moves the active layout position by the requested amount.
    /// </summary>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    void Advance(double height);
}
