using System;

namespace pdf;

/// <summary>
/// Defines independently paginated content and page settings within a layout document.
/// </summary>
public interface IPdfDocumentSection
{
    /// <summary>
    /// Adds a general layout element to the section content flow.
    /// </summary>
    /// <param name="item">The layout element to add.</param>
    void AddItem(IPdfLayoutElement item);

    /// <summary>
    /// Adds a block to the section content flow.
    /// </summary>
    /// <param name="block">The block to add.</param>
    void AddBlock(PdfBlock block);

    /// <summary>
    /// Adds a text block to the section content flow.
    /// </summary>
    /// <param name="block">The text block to add.</param>
    void AddTextBlock(PdfTextBlock block);

    /// <summary>
    /// Forces subsequent section content onto a new physical page.
    /// </summary>
    void AddPageBreak();

    /// <summary>
    /// Selects the orientation applied to this section's page size.
    /// </summary>
    /// <param name="orientation">The page orientation.</param>
    void SetOrientation(PdfPageOrientation orientation);

    /// <summary>
    /// Restarts the displayed page number at the supplied positive value.
    /// </summary>
    /// <param name="pageNumber">The first displayed page number in this section.</param>
    void SetPageNumberStart(int pageNumber);

    /// <summary>
    /// Configures top-to-bottom content flow through one or more equal-width columns.
    /// </summary>
    /// <param name="columnCount">The positive number of columns.</param>
    /// <param name="columnGap">The non-negative gap between columns in PDF points.</param>
    void SetContentColumns(int columnCount, double columnGap);

    /// <summary>
    /// Sets text used by the <c>{running}</c> page-template placeholder.
    /// </summary>
    /// <param name="text">The running-head text.</param>
    void SetRunningHead(string text);

    /// <summary>
    /// Sets reusable defaults applied to subsequently added typed section elements.
    /// </summary>
    /// <param name="theme">The document theme to copy.</param>
    void SetTheme(PdfDocumentTheme theme);

    /// <summary>
    /// Sets the default header template for this section.
    /// </summary>
    /// <param name="template">The header template.</param>
    void SetHeaderTemplate(PdfPageTemplate template);

    /// <summary>
    /// Sets the default footer template for this section.
    /// </summary>
    /// <param name="template">The footer template.</param>
    void SetFooterTemplate(PdfPageTemplate template);

    /// <summary>
    /// Sets the first-page header template for this section.
    /// </summary>
    /// <param name="template">The first-page header template.</param>
    void SetFirstPageHeaderTemplate(PdfPageTemplate template);

    /// <summary>
    /// Sets the first-page footer template for this section.
    /// </summary>
    /// <param name="template">The first-page footer template.</param>
    void SetFirstPageFooterTemplate(PdfPageTemplate template);

    /// <summary>
    /// Sets the odd-numbered-page header template for this section.
    /// </summary>
    /// <param name="template">The odd-page header template.</param>
    void SetOddPageHeaderTemplate(PdfPageTemplate template);

    /// <summary>
    /// Sets the odd-numbered-page footer template for this section.
    /// </summary>
    /// <param name="template">The odd-page footer template.</param>
    void SetOddPageFooterTemplate(PdfPageTemplate template);

    /// <summary>
    /// Sets the even-numbered-page header template for this section.
    /// </summary>
    /// <param name="template">The even-page header template.</param>
    void SetEvenPageHeaderTemplate(PdfPageTemplate template);

    /// <summary>
    /// Sets the even-numbered-page footer template for this section.
    /// </summary>
    /// <param name="template">The even-page footer template.</param>
    void SetEvenPageFooterTemplate(PdfPageTemplate template);
}
