namespace docx;

/// <summary>
/// Generates common document content through cohesive, save-ready operations.
/// </summary>
public interface IDocxGenerationManagement
{
    /// <summary>
    /// Creates a document backed by a minimal WordprocessingML package.
    /// </summary>
    /// <returns>The created document.</returns>
    DocxDocument CreateMinimalDocument();

    /// <summary>
    /// Appends a heading paragraph and ensures that its paragraph style exists.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="text">The heading text.</param>
    /// <param name="level">The heading level from one through nine.</param>
    /// <returns>The created paragraph.</returns>
    DocxParagraph AppendHeading(DocxDocument document, string text, int level);

    /// <summary>
    /// Appends a paragraph containing one text run.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="text">The paragraph text.</param>
    /// <returns>The created paragraph.</returns>
    DocxParagraph AppendParagraph(DocxDocument document, string text);

    /// <summary>
    /// Appends an unformatted text run to a paragraph.
    /// </summary>
    /// <param name="document">The document containing the paragraph.</param>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="text">The run text.</param>
    /// <returns>The created run.</returns>
    DocxRun AppendRun(DocxDocument document, DocxParagraph paragraph, string text);

    /// <summary>
    /// Appends a directly formatted text run to a paragraph.
    /// </summary>
    /// <param name="document">The document containing the paragraph.</param>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="text">The run text.</param>
    /// <param name="format">The direct run format.</param>
    /// <returns>The created run.</returns>
    DocxRun AppendFormattedText(DocxDocument document, DocxParagraph paragraph, string text, DocxRunFormat format);

    /// <summary>
    /// Creates and appends a table to the document body.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="rows">The number of rows.</param>
    /// <param name="columns">The number of cells in each row.</param>
    /// <returns>The created table.</returns>
    DocxTable AppendTable(DocxDocument document, int rows, int columns);

    /// <summary>
    /// Appends text to a table cell as a paragraph.
    /// </summary>
    /// <param name="document">The document containing the table.</param>
    /// <param name="table">The table to edit.</param>
    /// <param name="rowIndex">The zero-based row position.</param>
    /// <param name="columnIndex">The zero-based cell position.</param>
    /// <param name="text">The text to append.</param>
    /// <returns>The created paragraph.</returns>
    DocxParagraph AppendTableCellText(DocxDocument document, DocxTable table, int rowIndex, int columnIndex, string text);

    /// <summary>
    /// Adds an image part and appends an inline drawing to a paragraph.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="bytes">The image bytes.</param>
    /// <param name="contentType">The image content type, or empty to detect it.</param>
    /// <param name="name">The non-visual image name.</param>
    /// <param name="alternativeText">The accessibility description.</param>
    /// <param name="widthEmu">The width in EMUs.</param>
    /// <param name="heightEmu">The height in EMUs.</param>
    /// <returns>The created drawing.</returns>
    DocxDrawing AppendInlineImage(DocxDocument document, DocxParagraph paragraph, byte[] bytes, string contentType, string name, string alternativeText, long widthEmu, long heightEmu);

    /// <summary>
    /// Creates and appends an external hyperlink to a paragraph.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="target">The external target URI text.</param>
    /// <param name="displayText">The displayed link text.</param>
    /// <returns>The created hyperlink.</returns>
    DocxHyperlink AppendExternalHyperlink(DocxDocument document, DocxParagraph paragraph, string target, string displayText);

    /// <summary>
    /// Creates and appends an internal bookmark hyperlink to a paragraph.
    /// </summary>
    /// <param name="document">The document containing the paragraph.</param>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="anchor">The bookmark name.</param>
    /// <param name="displayText">The displayed link text.</param>
    /// <returns>The created hyperlink.</returns>
    DocxHyperlink AppendInternalHyperlink(DocxDocument document, DocxParagraph paragraph, string anchor, string displayText);

    /// <summary>
    /// Inserts a uniquely identified bookmark around a paragraph child range.
    /// </summary>
    /// <param name="document">The document used to allocate the identifier.</param>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="startIndex">The first child in the range.</param>
    /// <param name="endIndex">The exclusive end child position.</param>
    /// <param name="name">The bookmark name.</param>
    /// <returns>The allocated bookmark identifier.</returns>
    string InsertBookmark(DocxDocument document, DocxParagraph paragraph, int startIndex, int endIndex, string name);

    /// <summary>
    /// Appends a line break to a run.
    /// </summary>
    /// <param name="document">The document containing the run.</param>
    /// <param name="run">The run to edit.</param>
    /// <returns>The created break.</returns>
    DocxBreak AppendLineBreak(DocxDocument document, DocxRun run);

    /// <summary>
    /// Appends a page break to a run.
    /// </summary>
    /// <param name="document">The document containing the run.</param>
    /// <param name="run">The run to edit.</param>
    /// <returns>The created break.</returns>
    DocxBreak AppendPageBreak(DocxDocument document, DocxRun run);

    /// <summary>
    /// Appends a section-break paragraph with the supplied format.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="format">The section format.</param>
    /// <returns>The created section.</returns>
    DocxSection AppendSection(DocxDocument document, DocxSectionFormat format);

    /// <summary>
    /// Creates a header assigned to a section and adds one text paragraph.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="section">The section to edit.</param>
    /// <param name="kind">The header reference kind.</param>
    /// <param name="text">The header text.</param>
    /// <returns>The created header.</returns>
    DocxHeader CreateTextHeader(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind, string text);

    /// <summary>
    /// Creates a footer assigned to a section and adds one text paragraph.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="section">The section to edit.</param>
    /// <param name="kind">The footer reference kind.</param>
    /// <param name="text">The footer text.</param>
    /// <returns>The created footer.</returns>
    DocxFooter CreateTextFooter(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind, string text);

    /// <summary>
    /// Creates a footnote and inserts its reference into a paragraph.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="childIndex">The paragraph child insertion position.</param>
    /// <param name="text">The footnote text.</param>
    /// <returns>The created footnote.</returns>
    DocxFootnote InsertFootnote(DocxDocument document, DocxParagraph paragraph, int childIndex, string text);

    /// <summary>
    /// Creates an endnote and inserts its reference into a paragraph.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="childIndex">The paragraph child insertion position.</param>
    /// <param name="text">The endnote text.</param>
    /// <returns>The created endnote.</returns>
    DocxEndnote InsertEndnote(DocxDocument document, DocxParagraph paragraph, int childIndex, string text);

    /// <summary>
    /// Creates a comment and inserts its range and reference around paragraph children.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="startIndex">The first child in the range.</param>
    /// <param name="endIndex">The exclusive end child position.</param>
    /// <param name="author">The comment author.</param>
    /// <param name="initials">The author initials.</param>
    /// <param name="text">The comment text.</param>
    /// <returns>The created comment.</returns>
    DocxComment InsertComment(DocxDocument document, DocxParagraph paragraph, int startIndex, int endIndex, string author, string initials, string text);

    /// <summary>
    /// Creates and persists a paragraph style definition.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="styleId">The style identifier.</param>
    /// <param name="name">The style name.</param>
    /// <returns>The created style.</returns>
    DocxStyle CreateParagraphStyle(DocxDocument document, string styleId, string name);

    /// <summary>
    /// Creates and persists a custom single-level numbering definition.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="numberFormat">The WordprocessingML number format.</param>
    /// <param name="levelText">The level text template.</param>
    /// <returns>The created numbering instance.</returns>
    DocxNumberingInstance CreateNumberingDefinition(DocxDocument document, string numberFormat, string levelText);

    /// <summary>
    /// Applies a numbering definition and level to a paragraph.
    /// </summary>
    /// <param name="document">The document containing the numbering definition.</param>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="numbering">The numbering instance to apply.</param>
    /// <param name="levelIndex">The level index.</param>
    void ApplyNumberingDefinition(DocxDocument document, DocxParagraph paragraph, DocxNumberingInstance numbering, string levelIndex);
}
