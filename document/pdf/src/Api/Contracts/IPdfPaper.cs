using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfPaper"/>.
/// </summary>
public interface IPdfPaper
{
    /// <summary>
    /// Adds an author to this paper.
    /// </summary>
    /// <param name="name">The name used to identify the value in the document.</param>
    /// <param name="affiliation">The affiliation to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddAuthor(string name, string affiliation);

    /// <summary>
    /// Adds a reusable affiliation line to this paper.
    /// </summary>
    /// <param name="affiliation">The affiliation to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddAffiliation(PdfPaperAffiliation affiliation);

    /// <summary>
    /// Adds an author associated with previously added affiliation identifiers.
    /// </summary>
    /// <param name="name">The author name.</param>
    /// <param name="affiliationIdentifiers">The affiliation identifiers to display and validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddAuthorWithAffiliations(string name, string[] affiliationIdentifiers);

    /// <summary>
    /// Sets the abstract used for subsequent PDF generation.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetAbstract(string text);

    /// <summary>
    /// Adds a keyword to this paper.
    /// </summary>
    /// <param name="keyword">The keyword to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddKeyword(string keyword);

    /// <summary>
    /// Adds a section to this paper.
    /// </summary>
    /// <param name="heading">The heading to use for this operation.</param>
    void AddSection(string heading);

    /// <summary>
    /// Adds a heading at an explicit hierarchy level.
    /// </summary>
    /// <param name="heading">The heading text.</param>
    /// <param name="level">The heading hierarchy level.</param>
    void AddHeading(string heading, PdfPaperHeadingLevel level);

    /// <summary>
    /// Adds a heading that can be targeted by paper cross-references.
    /// </summary>
    /// <param name="referenceName">The unique cross-reference name.</param>
    /// <param name="heading">The heading text.</param>
    /// <param name="level">The heading hierarchy level.</param>
    void AddReferencedHeading(string referenceName, string heading, PdfPaperHeadingLevel level);

    /// <summary>
    /// Adds linked text that targets a referenced heading.
    /// </summary>
    /// <param name="referenceName">The target cross-reference name.</param>
    /// <param name="text">The linked display text.</param>
    void AddCrossReference(string referenceName, string text);

    /// <summary>
    /// Adds a section to this paper.
    /// </summary>
    /// <param name="section">The section to add to the paper.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddPaperSection(PdfPaperSection section);

    /// <summary>
    /// Adds a paragraph to this paper.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddParagraph(string text);

    /// <summary>
    /// Adds a numbered footnote at the current content position.
    /// </summary>
    /// <param name="text">The footnote text.</param>
    void AddFootnote(string text);

    /// <summary>
    /// Collects a numbered endnote for the generated endnotes section.
    /// </summary>
    /// <param name="text">The endnote text.</param>
    void AddEndnote(string text);

    /// <summary>
    /// Adds a figure to this paper.
    /// </summary>
    /// <param name="image">The image resource to place or store.</param>
    /// <param name="caption">The caption to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddFigure(PdfImage image, string caption);

    /// <summary>
    /// Adds a table to this paper.
    /// </summary>
    /// <param name="table">The table to use for this operation.</param>
    /// <param name="caption">The caption to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddTable(PdfReportTable table, string caption);

    /// <summary>
    /// Adds a citation to this paper.
    /// </summary>
    /// <param name="citation">The citation to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddCitation(PdfCitation citation);

    /// <summary>
    /// Adds a bibliography entry to this paper.
    /// </summary>
    /// <param name="entry">The entry to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddBibliographyEntry(PdfBibliographyEntry entry);

    /// <summary>
    /// Enables internal links from collected citations to matching bibliography entries.
    /// </summary>
    void EnableCitationLinks();

    /// <summary>
    /// Includes a generated linked list of figures in the built document.
    /// </summary>
    void IncludeListOfFigures();

    /// <summary>
    /// Includes a generated linked list of tables in the built document.
    /// </summary>
    void IncludeListOfTables();

    /// <summary>
    /// Adds an appendix to this paper.
    /// </summary>
    /// <param name="heading">The heading to use for this operation.</param>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddAppendix(string heading, string text);

    /// <summary>
    /// Adds an independently paginated two-column paper section.
    /// </summary>
    /// <param name="heading">The section heading.</param>
    /// <param name="paragraphs">The paragraphs flowed through the columns.</param>
    /// <param name="columnGap">The non-negative gap between columns in PDF points.</param>
    void AddTwoColumnSection(string heading, string[] paragraphs, double columnGap);

    /// <summary>
    /// Sets the running head rendered by the paper header template.
    /// </summary>
    /// <param name="text">The running-head text.</param>
    void SetRunningHead(string text);

    /// <summary>
    /// Configures logical labels for all generated paper pages.
    /// </summary>
    /// <param name="style">The page-label numbering style.</param>
    /// <param name="prefix">The text placed before each generated number.</param>
    /// <param name="startNumber">The positive first generated number.</param>
    void SetPageLabels(PdfPageLabelStyle style, string prefix, int startNumber);

    /// <summary>
    /// Builds a paper from the values collected so far and returns the result.
    /// </summary>
    /// <returns>The newly built document.</returns>
    PdfDocument Build();
}
