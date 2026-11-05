namespace docx;

/// <summary>
/// Generates common document content through cohesive, save-ready operations.
/// </summary>
public class DocxGenerationManagement : IDocxGenerationManagement
{
    /// <summary>
    /// Creates a generation management service.
    /// </summary>
    public DocxGenerationManagement()
    {
    }

    /// <summary>
    /// Creates a document backed by a minimal WordprocessingML package.
    /// </summary>
    /// <returns>The created document.</returns>
    public DocxDocument CreateMinimalDocument()
    {
        DocxPackagePersistence persistence = new DocxPackagePersistence();
        return persistence.CreateDocument();
    }

    /// <summary>
    /// Appends a heading paragraph and ensures that its paragraph style exists.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="text">The heading text.</param>
    /// <param name="level">The heading level from one through nine.</param>
    /// <returns>The created paragraph.</returns>
    public DocxParagraph AppendHeading(DocxDocument document, string text, int level)
    {
        ValidateDocument(document);

        if (level < 1 || level > 9)
        {
            throw new ArgumentOutOfRangeException("level");
        }

        string styleId = "Heading" + level.ToString();
        DocxStyleManagement styles = new DocxStyleManagement();

        if (styles.FindStyleById(document.GetStyles(), styleId) == null)
        {
            styles.CreateStyle(document, DocxStyle.TypeParagraph, styleId, "heading " + level.ToString());
        }

        DocxParagraph paragraph = CreateTextParagraph(text);
        DocxParagraphFormat format = new DocxParagraphFormat();
        format.StyleId = styleId;
        paragraph.SetFormat(format);
        document.Body.AddBlock(DocxBlock.FromParagraph(paragraph));
        SynchronizeDocument(document);
        return paragraph;
    }

    /// <summary>
    /// Appends a paragraph containing one text run.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="text">The paragraph text.</param>
    /// <returns>The created paragraph.</returns>
    public DocxParagraph AppendParagraph(DocxDocument document, string text)
    {
        ValidateDocument(document);
        DocxParagraph paragraph = CreateTextParagraph(text);
        document.Body.AddBlock(DocxBlock.FromParagraph(paragraph));
        SynchronizeDocument(document);
        return paragraph;
    }

    /// <summary>
    /// Appends an unformatted text run to a paragraph.
    /// </summary>
    /// <param name="document">The document containing the paragraph.</param>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="text">The run text.</param>
    /// <returns>The created run.</returns>
    public DocxRun AppendRun(DocxDocument document, DocxParagraph paragraph, string text)
    {
        ValidateDocument(document);
        ValidateParagraph(paragraph);
        DocxContentFactory factory = new DocxContentFactory();
        DocxRun run = factory.CreateTextRun(text);
        paragraph.InsertRun(paragraph.ChildCount(), run);
        run = paragraph.GetRun(paragraph.RunCount() - 1);
        SynchronizeDocument(document);
        return run;
    }

    /// <summary>
    /// Appends a directly formatted text run to a paragraph.
    /// </summary>
    /// <param name="document">The document containing the paragraph.</param>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="text">The run text.</param>
    /// <param name="format">The direct run format.</param>
    /// <returns>The created run.</returns>
    public DocxRun AppendFormattedText(DocxDocument document, DocxParagraph paragraph, string text, DocxRunFormat format)
    {
        ValidateDocument(document);
        ValidateParagraph(paragraph);

        if (format == null)
        {
            throw new ArgumentNullException("format");
        }

        DocxContentFactory factory = new DocxContentFactory();
        DocxRun run = factory.CreateTextRun(text);
        run.SetFormat(format);
        paragraph.InsertRun(paragraph.ChildCount(), run);
        run = paragraph.GetRun(paragraph.RunCount() - 1);
        SynchronizeDocument(document);
        return run;
    }

    /// <summary>
    /// Creates and appends a table to the document body.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="rows">The number of rows.</param>
    /// <param name="columns">The number of cells in each row.</param>
    /// <returns>The created table.</returns>
    public DocxTable AppendTable(DocxDocument document, int rows, int columns)
    {
        ValidateDocument(document);
        DocxTableEditing tables = new DocxTableEditing();
        DocxTable table = tables.AppendTableToBody(document.Body, rows, columns);
        SynchronizeDocument(document);
        return table;
    }

    /// <summary>
    /// Appends text to a table cell as a paragraph.
    /// </summary>
    /// <param name="document">The document containing the table.</param>
    /// <param name="table">The table to edit.</param>
    /// <param name="rowIndex">The zero-based row position.</param>
    /// <param name="columnIndex">The zero-based cell position.</param>
    /// <param name="text">The text to append.</param>
    /// <returns>The created paragraph.</returns>
    public DocxParagraph AppendTableCellText(DocxDocument document, DocxTable table, int rowIndex, int columnIndex, string text)
    {
        ValidateDocument(document);

        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        DocxParagraph paragraph = CreateTextParagraph(text);
        DocxTableCell cell = table.GetRow(rowIndex).GetCell(columnIndex);
        DocxTableEditing tables = new DocxTableEditing();
        tables.AppendParagraphToCell(cell, paragraph);
        SynchronizeDocument(document);
        return paragraph;
    }

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
    public DocxDrawing AppendInlineImage(DocxDocument document, DocxParagraph paragraph, byte[] bytes, string contentType, string name, string alternativeText, long widthEmu, long heightEmu)
    {
        DocxMediaManagement media = new DocxMediaManagement();
        DocxDrawing drawing = media.AddInlineImageToParagraph(document, paragraph, bytes, contentType, name, alternativeText, widthEmu, heightEmu);
        SynchronizeDocument(document);
        return drawing;
    }

    /// <summary>
    /// Creates and appends an external hyperlink to a paragraph.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="target">The external target URI text.</param>
    /// <param name="displayText">The displayed link text.</param>
    /// <returns>The created hyperlink.</returns>
    public DocxHyperlink AppendExternalHyperlink(DocxDocument document, DocxParagraph paragraph, string target, string displayText)
    {
        DocxLinkBookmarkFieldManagement links = new DocxLinkBookmarkFieldManagement();
        DocxHyperlink hyperlink = links.CreateExternalHyperlink(document, target, displayText);
        links.AppendHyperlink(paragraph, hyperlink);
        SynchronizeDocument(document);
        return hyperlink;
    }

    /// <summary>
    /// Creates and appends an internal bookmark hyperlink to a paragraph.
    /// </summary>
    /// <param name="document">The document containing the paragraph.</param>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="anchor">The bookmark name.</param>
    /// <param name="displayText">The displayed link text.</param>
    /// <returns>The created hyperlink.</returns>
    public DocxHyperlink AppendInternalHyperlink(DocxDocument document, DocxParagraph paragraph, string anchor, string displayText)
    {
        ValidateDocument(document);
        DocxLinkBookmarkFieldManagement links = new DocxLinkBookmarkFieldManagement();
        DocxHyperlink hyperlink = links.CreateInternalHyperlink(anchor, displayText);
        links.AppendHyperlink(paragraph, hyperlink);
        SynchronizeDocument(document);
        return hyperlink;
    }

    /// <summary>
    /// Inserts a uniquely identified bookmark around a paragraph child range.
    /// </summary>
    /// <param name="document">The document used to allocate the identifier.</param>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="startIndex">The first child in the range.</param>
    /// <param name="endIndex">The exclusive end child position.</param>
    /// <param name="name">The bookmark name.</param>
    /// <returns>The allocated bookmark identifier.</returns>
    public string InsertBookmark(DocxDocument document, DocxParagraph paragraph, int startIndex, int endIndex, string name)
    {
        DocxLinkBookmarkFieldManagement links = new DocxLinkBookmarkFieldManagement();
        string id = links.InsertBookmarkRange(document, paragraph, startIndex, endIndex, name);
        SynchronizeDocument(document);
        return id;
    }

    /// <summary>
    /// Appends a line break to a run.
    /// </summary>
    /// <param name="document">The document containing the run.</param>
    /// <param name="run">The run to edit.</param>
    /// <returns>The created break.</returns>
    public DocxBreak AppendLineBreak(DocxDocument document, DocxRun run)
    {
        ValidateDocument(document);
        ValidateRun(run);
        DocxContentFactory factory = new DocxContentFactory();
        DocxBreak lineBreak = factory.CreateLineBreak();
        run.InsertBreak(run.ChildCount(), lineBreak);
        SynchronizeDocument(document);
        return lineBreak;
    }

    /// <summary>
    /// Appends a page break to a run.
    /// </summary>
    /// <param name="document">The document containing the run.</param>
    /// <param name="run">The run to edit.</param>
    /// <returns>The created break.</returns>
    public DocxBreak AppendPageBreak(DocxDocument document, DocxRun run)
    {
        ValidateDocument(document);
        ValidateRun(run);
        DocxContentFactory factory = new DocxContentFactory();
        DocxBreak pageBreak = factory.CreatePageBreak();
        run.InsertBreak(run.ChildCount(), pageBreak);
        SynchronizeDocument(document);
        return pageBreak;
    }

    /// <summary>
    /// Appends a section-break paragraph with the supplied format.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="format">The section format.</param>
    /// <returns>The created section.</returns>
    public DocxSection AppendSection(DocxDocument document, DocxSectionFormat format)
    {
        ValidateDocument(document);

        if (format == null)
        {
            throw new ArgumentNullException("format");
        }

        DocxSection section = new DocxSection("");
        section.SetFormat(format);
        DocxSectionManagement sections = new DocxSectionManagement();
        sections.AppendSectionBreak(document, section);
        SynchronizeDocument(document);
        return section;
    }

    /// <summary>
    /// Creates a header assigned to a section and adds one text paragraph.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="section">The section to edit.</param>
    /// <param name="kind">The header reference kind.</param>
    /// <param name="text">The header text.</param>
    /// <returns>The created header.</returns>
    public DocxHeader CreateTextHeader(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind, string text)
    {
        DocxSectionManagement sections = new DocxSectionManagement();
        DocxHeader header = sections.CreateHeader(document, section, kind);
        sections.AppendHeaderBlock(header, DocxBlock.FromParagraph(CreateTextParagraph(text)));
        SynchronizeDocument(document);
        return header;
    }

    /// <summary>
    /// Creates a footer assigned to a section and adds one text paragraph.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="section">The section to edit.</param>
    /// <param name="kind">The footer reference kind.</param>
    /// <param name="text">The footer text.</param>
    /// <returns>The created footer.</returns>
    public DocxFooter CreateTextFooter(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind, string text)
    {
        DocxSectionManagement sections = new DocxSectionManagement();
        DocxFooter footer = sections.CreateFooter(document, section, kind);
        sections.AppendFooterBlock(footer, DocxBlock.FromParagraph(CreateTextParagraph(text)));
        SynchronizeDocument(document);
        return footer;
    }

    /// <summary>
    /// Creates a footnote and inserts its reference into a paragraph.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="childIndex">The paragraph child insertion position.</param>
    /// <param name="text">The footnote text.</param>
    /// <returns>The created footnote.</returns>
    public DocxFootnote InsertFootnote(DocxDocument document, DocxParagraph paragraph, int childIndex, string text)
    {
        DocxNoteManagement notes = new DocxNoteManagement();
        DocxFootnote footnote = notes.CreateFootnote(document, text);
        notes.InsertFootnoteReference(paragraph, childIndex, footnote);
        SynchronizeDocument(document);
        return footnote;
    }

    /// <summary>
    /// Creates an endnote and inserts its reference into a paragraph.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="childIndex">The paragraph child insertion position.</param>
    /// <param name="text">The endnote text.</param>
    /// <returns>The created endnote.</returns>
    public DocxEndnote InsertEndnote(DocxDocument document, DocxParagraph paragraph, int childIndex, string text)
    {
        DocxNoteManagement notes = new DocxNoteManagement();
        DocxEndnote endnote = notes.CreateEndnote(document, text);
        notes.InsertEndnoteReference(paragraph, childIndex, endnote);
        SynchronizeDocument(document);
        return endnote;
    }

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
    public DocxComment InsertComment(DocxDocument document, DocxParagraph paragraph, int startIndex, int endIndex, string author, string initials, string text)
    {
        DocxCommentManagement comments = new DocxCommentManagement();
        DocxComment comment = comments.CreateComment(document, author, initials, text);
        comments.InsertCommentRange(paragraph, startIndex, endIndex, comment);
        SynchronizeDocument(document);
        return comment;
    }

    /// <summary>
    /// Creates and persists a paragraph style definition.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="styleId">The style identifier.</param>
    /// <param name="name">The style name.</param>
    /// <returns>The created style.</returns>
    public DocxStyle CreateParagraphStyle(DocxDocument document, string styleId, string name)
    {
        DocxStyleManagement styles = new DocxStyleManagement();
        return styles.CreateStyle(document, DocxStyle.TypeParagraph, styleId, name);
    }

    /// <summary>
    /// Creates and persists a custom single-level numbering definition.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="numberFormat">The WordprocessingML number format.</param>
    /// <param name="levelText">The level text template.</param>
    /// <returns>The created numbering instance.</returns>
    public DocxNumberingInstance CreateNumberingDefinition(DocxDocument document, string numberFormat, string levelText)
    {
        DocxNumberingManagement numbering = new DocxNumberingManagement();
        return numbering.CreateCustomSingleLevelList(document, numberFormat, levelText);
    }

    /// <summary>
    /// Applies a numbering definition and level to a paragraph.
    /// </summary>
    /// <param name="document">The document containing the numbering definition.</param>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="numbering">The numbering instance to apply.</param>
    /// <param name="levelIndex">The level index.</param>
    public void ApplyNumberingDefinition(DocxDocument document, DocxParagraph paragraph, DocxNumberingInstance numbering, string levelIndex)
    {
        if (numbering == null)
        {
            throw new ArgumentNullException("numbering");
        }

        DocxNumberingManagement numberingManagement = new DocxNumberingManagement();
        numberingManagement.ApplyList(document, paragraph, numbering.NumberingId, levelIndex);
        SynchronizeDocument(document);
    }

    internal DocxParagraph CreateTextParagraph(string text)
    {
        DocxContentFactory factory = new DocxContentFactory();
        return factory.CreateTextParagraph(text);
    }

    internal void SynchronizeDocument(DocxDocument document)
    {
        ValidateDocument(document);
        DocxTemplate.UpdateMainDocumentBody(document);
    }

    internal void ValidateDocument(DocxDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }
    }

    internal void ValidateParagraph(DocxParagraph paragraph)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }
    }

    internal void ValidateRun(DocxRun run)
    {
        if (run == null)
        {
            throw new ArgumentNullException("run");
        }
    }
}
