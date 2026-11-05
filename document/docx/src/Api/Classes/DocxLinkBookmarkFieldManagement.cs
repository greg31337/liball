namespace docx;

/// <summary>
/// Creates, inspects, and inserts hyperlinks, bookmarks, and fields.
/// </summary>
public class DocxLinkBookmarkFieldManagement : IDocxLinkBookmarkFieldManagement
{
    /// <summary>
    /// Creates a link, bookmark, and field management service.
    /// </summary>
    public DocxLinkBookmarkFieldManagement()
    {
    }

    /// <summary>
    /// Creates an external hyperlink and its main-document relationship.
    /// </summary>
    /// <param name="document">The document that owns the relationship.</param>
    /// <param name="target">The external target URI.</param>
    /// <param name="displayText">The displayed link text.</param>
    /// <returns>The created hyperlink.</returns>
    public DocxHyperlink CreateExternalHyperlink(DocxDocument document, string target, string displayText)
    {
        ValidateDocument(document);
        DocxRelationshipSet relationships = document.GetOrCreateMainDocumentRelationshipSet();
        DocxHyperlink hyperlink = DocxHyperlink.CreateExternal(relationships, target, displayText);
        document.WriteRelationshipSetPart(relationships);
        return hyperlink;
    }

    /// <summary>
    /// Creates a hyperlink to a bookmark anchor in the same document.
    /// </summary>
    /// <param name="anchor">The bookmark name.</param>
    /// <param name="displayText">The displayed link text.</param>
    /// <returns>The created hyperlink.</returns>
    public DocxHyperlink CreateInternalHyperlink(string anchor, string displayText)
    {
        return DocxHyperlink.CreateAnchor(anchor, displayText);
    }

    /// <summary>
    /// Appends a hyperlink to a paragraph.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="hyperlink">The hyperlink to append.</param>
    public void AppendHyperlink(DocxParagraph paragraph, DocxHyperlink hyperlink)
    {
        ValidateParagraph(paragraph);
        ValidateHyperlink(hyperlink);
        paragraph.InsertHyperlink(paragraph.ChildCount(), hyperlink);
    }

    /// <summary>
    /// Resolves a hyperlink through the main-document relationship set.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="hyperlink">The hyperlink to resolve.</param>
    /// <returns>True when a hyperlink relationship was resolved.</returns>
    public bool ResolveMainDocumentHyperlink(DocxDocument document, DocxHyperlink hyperlink)
    {
        ValidateDocument(document);
        ValidateHyperlink(hyperlink);
        string partName = document.Package.GetMainDocumentPartName();

        if (partName == "")
        {
            return false;
        }

        DocxRelationshipSet? relationships = document.Package.FindRelationshipSet(partName);

        if (relationships == null)
        {
            return false;
        }

        return hyperlink.ResolveRelationship(relationships);
    }

    /// <summary>
    /// Returns the resolved hyperlink target.
    /// </summary>
    /// <param name="hyperlink">The hyperlink to inspect.</param>
    /// <returns>The target, or an empty string.</returns>
    public string GetHyperlinkTarget(DocxHyperlink hyperlink)
    {
        ValidateHyperlink(hyperlink);
        return hyperlink.Target;
    }

    /// <summary>
    /// Returns the internal bookmark anchor.
    /// </summary>
    /// <param name="hyperlink">The hyperlink to inspect.</param>
    /// <returns>The anchor, or an empty string.</returns>
    public string GetHyperlinkAnchor(DocxHyperlink hyperlink)
    {
        ValidateHyperlink(hyperlink);
        return hyperlink.Anchor;
    }

    /// <summary>
    /// Returns the hyperlink relationship identifier.
    /// </summary>
    /// <param name="hyperlink">The hyperlink to inspect.</param>
    /// <returns>The relationship identifier, or an empty string.</returns>
    public string GetHyperlinkRelationshipId(DocxHyperlink hyperlink)
    {
        ValidateHyperlink(hyperlink);
        return hyperlink.RelationshipId;
    }

    /// <summary>
    /// Returns whether the resolved hyperlink relationship is external.
    /// </summary>
    /// <param name="hyperlink">The hyperlink to inspect.</param>
    /// <returns>True when the resolved target is external.</returns>
    public bool IsExternalHyperlink(DocxHyperlink hyperlink)
    {
        ValidateHyperlink(hyperlink);
        return hyperlink.IsExternal;
    }

    /// <summary>
    /// Sets the hyperlink tooltip.
    /// </summary>
    /// <param name="hyperlink">The hyperlink to edit.</param>
    /// <param name="tooltip">The tooltip text, or an empty string.</param>
    public void SetHyperlinkTooltip(DocxHyperlink hyperlink, string tooltip)
    {
        ValidateHyperlink(hyperlink);
        hyperlink.SetTooltip(tooltip);
    }

    /// <summary>
    /// Returns the hyperlink tooltip.
    /// </summary>
    /// <param name="hyperlink">The hyperlink to inspect.</param>
    /// <returns>The tooltip text, or an empty string.</returns>
    public string GetHyperlinkTooltip(DocxHyperlink hyperlink)
    {
        ValidateHyperlink(hyperlink);
        return hyperlink.Tooltip;
    }

    /// <summary>
    /// Returns all bookmark start markers in a paragraph.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <returns>A new list containing the start markers.</returns>
    public List<DocxBookmark> GetBookmarkStarts(DocxParagraph paragraph)
    {
        ValidateParagraph(paragraph);
        List<DocxBookmark> starts = new List<DocxBookmark>();

        foreach (DocxBookmark bookmark in paragraph.Bookmarks)
        {
            if (bookmark.IsStart())
            {
                starts.Add(bookmark);
            }
        }

        return starts;
    }

    /// <summary>
    /// Finds a bookmark start by name in a paragraph.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <param name="name">The bookmark name.</param>
    /// <returns>The matching marker, or null.</returns>
    public DocxBookmark? FindBookmarkStart(DocxParagraph paragraph, string name)
    {
        ValidateParagraph(paragraph);
        return paragraph.FindBookmarkStartByName(name);
    }

    /// <summary>
    /// Finds a bookmark end by identifier in a paragraph.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <param name="id">The bookmark identifier.</param>
    /// <returns>The matching marker, or null.</returns>
    public DocxBookmark? FindBookmarkEnd(DocxParagraph paragraph, string id)
    {
        ValidateParagraph(paragraph);
        return paragraph.FindBookmarkEndById(id);
    }

    /// <summary>
    /// Finds a bookmark start by name in direct main-body paragraphs.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="name">The bookmark name.</param>
    /// <returns>The matching marker, or null.</returns>
    public DocxBookmark? FindDocumentBookmarkStart(DocxDocument document, string name)
    {
        ValidateDocument(document);

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        foreach (DocxParagraph paragraph in document.Body.GetParagraphs())
        {
            DocxBookmark? bookmark = paragraph.FindBookmarkStartByName(name);

            if (bookmark != null)
            {
                return bookmark;
            }
        }

        return null;
    }

    /// <summary>
    /// Inserts a bookmark start marker at a paragraph child position.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="index">The child insertion position.</param>
    /// <param name="id">The bookmark identifier.</param>
    /// <param name="name">The bookmark name.</param>
    /// <returns>The inserted marker.</returns>
    public DocxBookmark InsertBookmarkStart(DocxParagraph paragraph, int index, string id, string name)
    {
        ValidateParagraph(paragraph);
        DocxBookmark bookmark = DocxBookmark.CreateStart(id, name);
        paragraph.InsertBookmark(index, bookmark);
        return bookmark;
    }

    /// <summary>
    /// Inserts a bookmark end marker at a paragraph child position.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="index">The child insertion position.</param>
    /// <param name="id">The bookmark identifier.</param>
    /// <returns>The inserted marker.</returns>
    public DocxBookmark InsertBookmarkEnd(DocxParagraph paragraph, int index, string id)
    {
        ValidateParagraph(paragraph);
        DocxBookmark bookmark = DocxBookmark.CreateEnd(id);
        paragraph.InsertBookmark(index, bookmark);
        return bookmark;
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
    public string InsertBookmarkRange(DocxDocument document, DocxParagraph paragraph, int startIndex, int endIndex, string name)
    {
        ValidateDocument(document);
        ValidateParagraph(paragraph);

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name == "")
        {
            throw new ArgumentException("Bookmark name is required.", "name");
        }

        if (startIndex < 0 || startIndex > paragraph.ChildCount())
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (endIndex < startIndex || endIndex > paragraph.ChildCount())
        {
            throw new ArgumentOutOfRangeException("endIndex");
        }

        string id = AllocateDocumentBookmarkId(document, paragraph);
        InsertBookmarkEnd(paragraph, endIndex, id);
        InsertBookmarkStart(paragraph, startIndex, id, name);
        return id;
    }

    /// <summary>
    /// Creates a simple field with an instruction and displayed result.
    /// </summary>
    /// <param name="instruction">The field instruction.</param>
    /// <param name="resultText">The displayed result.</param>
    /// <returns>The created field.</returns>
    public DocxField CreateField(string instruction, string resultText)
    {
        return DocxField.Create(instruction, resultText);
    }

    /// <summary>
    /// Creates a cross-reference field to a bookmark.
    /// </summary>
    /// <param name="bookmarkName">The referenced bookmark name.</param>
    /// <param name="resultText">The displayed result.</param>
    /// <returns>The created field.</returns>
    public DocxField CreateCrossReferenceField(string bookmarkName, string resultText)
    {
        if (bookmarkName == null)
        {
            throw new ArgumentNullException("bookmarkName");
        }

        if (bookmarkName == "")
        {
            throw new ArgumentException("Bookmark name is required.", "bookmarkName");
        }

        return CreateField("REF " + bookmarkName + " \\h", resultText);
    }

    /// <summary>
    /// Creates a current-page-number field.
    /// </summary>
    /// <param name="resultText">The displayed result.</param>
    /// <returns>The created field.</returns>
    public DocxField CreatePageField(string resultText)
    {
        return CreateField("PAGE", resultText);
    }

    /// <summary>
    /// Creates a date field with an optional Word date format.
    /// </summary>
    /// <param name="format">The Word date format, or an empty string.</param>
    /// <param name="resultText">The displayed result.</param>
    /// <returns>The created field.</returns>
    public DocxField CreateDateField(string format, string resultText)
    {
        if (format == null)
        {
            throw new ArgumentNullException("format");
        }

        if (format == "")
        {
            return CreateField("DATE", resultText);
        }

        return CreateField("DATE \\@ \"" + format + "\"", resultText);
    }

    /// <summary>
    /// Creates an author field.
    /// </summary>
    /// <param name="resultText">The displayed result.</param>
    /// <returns>The created field.</returns>
    public DocxField CreateAuthorField(string resultText)
    {
        return CreateField("AUTHOR", resultText);
    }

    /// <summary>
    /// Creates a title field.
    /// </summary>
    /// <param name="resultText">The displayed result.</param>
    /// <returns>The created field.</returns>
    public DocxField CreateTitleField(string resultText)
    {
        return CreateField("TITLE", resultText);
    }

    /// <summary>
    /// Creates a table-of-contents field using Word's common switches.
    /// </summary>
    /// <param name="resultText">The displayed result.</param>
    /// <returns>The created field.</returns>
    public DocxField CreateTableOfContentsField(string resultText)
    {
        return CreateField("TOC \\o \"1-3\" \\h \\z \\u", resultText);
    }

    /// <summary>
    /// Appends a simple field to a paragraph.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="field">The field to append.</param>
    public void AppendField(DocxParagraph paragraph, DocxField field)
    {
        ValidateParagraph(paragraph);
        ValidateField(field);
        paragraph.InsertField(paragraph.ChildCount(), field);
    }

    /// <summary>
    /// Returns a simple field instruction without changing its switches.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    /// <returns>The field instruction.</returns>
    public string GetFieldInstruction(DocxField field)
    {
        ValidateField(field);
        return field.Instruction;
    }

    /// <summary>
    /// Returns the displayed simple field result.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    /// <returns>The displayed result.</returns>
    public string GetFieldResult(DocxField field)
    {
        ValidateField(field);
        return field.GetText();
    }

    /// <summary>
    /// Replaces a simple field instruction.
    /// </summary>
    /// <param name="field">The field to edit.</param>
    /// <param name="instruction">The replacement instruction.</param>
    public void SetFieldInstruction(DocxField field, string instruction)
    {
        ValidateField(field);

        if (instruction == null)
        {
            throw new ArgumentNullException("instruction");
        }

        if (instruction == "")
        {
            throw new ArgumentException("Field instruction is required.", "instruction");
        }

        field.SetInstruction(instruction);
    }

    /// <summary>
    /// Replaces a simple field result with one text run.
    /// </summary>
    /// <param name="field">The field to edit.</param>
    /// <param name="resultText">The replacement result.</param>
    public void SetFieldResult(DocxField field, string resultText)
    {
        ValidateField(field);
        field.SetResultText(resultText);
    }

    /// <summary>
    /// Marks a field for update when Word opens the document.
    /// </summary>
    /// <param name="field">The field to edit.</param>
    public void MarkFieldForUpdate(DocxField field)
    {
        ValidateField(field);
        field.SetDirty(true);
    }

    /// <summary>
    /// Removes a field's update marker.
    /// </summary>
    /// <param name="field">The field to edit.</param>
    public void ClearFieldUpdateMarker(DocxField field)
    {
        ValidateField(field);
        field.ClearDirty();
    }

    /// <summary>
    /// Returns whether a field is marked for update.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    /// <returns>True when the field is marked dirty.</returns>
    public bool IsFieldUpdateRequested(DocxField field)
    {
        ValidateField(field);
        return field.IsDirty();
    }

    /// <summary>
    /// Sets whether a field is locked against updates.
    /// </summary>
    /// <param name="field">The field to edit.</param>
    /// <param name="locked">Whether the field is locked.</param>
    public void SetFieldLocked(DocxField field, bool locked)
    {
        ValidateField(field);
        field.SetFieldLock(locked);
    }

    /// <summary>
    /// Returns whether a field is locked against updates.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    /// <returns>True when the field is locked.</returns>
    public bool IsFieldLocked(DocxField field)
    {
        ValidateField(field);
        return field.IsLocked();
    }

    /// <summary>
    /// Returns whether a field is a cross-reference field.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    /// <returns>True for REF, PAGEREF, or NOTEREF instructions.</returns>
    public bool IsCrossReferenceField(DocxField field)
    {
        return HasFieldCode(field, "REF") || HasFieldCode(field, "PAGEREF") || HasFieldCode(field, "NOTEREF");
    }

    /// <summary>
    /// Returns whether a field is a current-page-number field.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    /// <returns>True for a PAGE instruction.</returns>
    public bool IsPageField(DocxField field)
    {
        return HasFieldCode(field, "PAGE");
    }

    /// <summary>
    /// Returns whether a field is a date-family field.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    /// <returns>True for a supported date instruction.</returns>
    public bool IsDateField(DocxField field)
    {
        return HasFieldCode(field, "DATE") || HasFieldCode(field, "CREATEDATE") || HasFieldCode(field, "SAVEDATE") || HasFieldCode(field, "PRINTDATE");
    }

    /// <summary>
    /// Returns whether a field is an author field.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    /// <returns>True for an AUTHOR instruction.</returns>
    public bool IsAuthorField(DocxField field)
    {
        return HasFieldCode(field, "AUTHOR");
    }

    /// <summary>
    /// Returns whether a field is a title field.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    /// <returns>True for a TITLE instruction.</returns>
    public bool IsTitleField(DocxField field)
    {
        return HasFieldCode(field, "TITLE");
    }

    /// <summary>
    /// Returns whether a field is a table-of-contents field.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    /// <returns>True for a TOC instruction.</returns>
    public bool IsTableOfContentsField(DocxField field)
    {
        return HasFieldCode(field, "TOC");
    }

    /// <summary>
    /// Returns whether a paragraph contains preserved complex-field elements.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <returns>True when complex-field markup is present.</returns>
    public bool ContainsComplexField(DocxParagraph paragraph)
    {
        ValidateParagraph(paragraph);
        return DocxFieldParser.ContainsComplexField(paragraph);
    }

    /// <summary>
    /// Returns the number of preserved complex-field elements in a paragraph.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <returns>The complex-field element count.</returns>
    public int ComplexFieldElementCount(DocxParagraph paragraph)
    {
        ValidateParagraph(paragraph);
        return DocxFieldParser.CountComplexFieldElements(paragraph);
    }

    /// <summary>
    /// Returns preserved complex-field instruction text in document order.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <returns>The combined instruction text.</returns>
    public string GetComplexFieldInstruction(DocxParagraph paragraph)
    {
        ValidateParagraph(paragraph);
        return DocxFieldParser.GetComplexFieldInstructionText(paragraph);
    }

    internal static string AllocateDocumentBookmarkId(DocxDocument document, DocxParagraph paragraph)
    {
        int candidate = 0;
        string id = candidate.ToString(System.Globalization.CultureInfo.InvariantCulture);

        while (DocumentContainsBookmarkId(document, paragraph, id))
        {
            candidate = candidate + 1;
            id = candidate.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return id;
    }

    internal static bool DocumentContainsBookmarkId(DocxDocument document, DocxParagraph requestedParagraph, string id)
    {
        if (requestedParagraph.ContainsBookmarkId(id))
        {
            return true;
        }

        foreach (DocxParagraph paragraph in document.Body.GetParagraphs())
        {
            if (paragraph.ContainsBookmarkId(id))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool HasFieldCode(DocxField field, string code)
    {
        ValidateField(field);
        string instruction = field.Instruction.Trim();

        if (string.Equals(instruction, code, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (instruction.StartsWith(code + " ", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return instruction.StartsWith(code + "\\", StringComparison.OrdinalIgnoreCase);
    }

    internal static void ValidateDocument(DocxDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }
    }

    internal static void ValidateParagraph(DocxParagraph paragraph)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }
    }

    internal static void ValidateHyperlink(DocxHyperlink hyperlink)
    {
        if (hyperlink == null)
        {
            throw new ArgumentNullException("hyperlink");
        }
    }

    internal static void ValidateField(DocxField field)
    {
        if (field == null)
        {
            throw new ArgumentNullException("field");
        }
    }
}
