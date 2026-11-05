namespace docx;

/// <summary>
/// Creates, inspects, and inserts hyperlinks, bookmarks, and fields.
/// </summary>
public interface IDocxLinkBookmarkFieldManagement
{
    /// <summary>
    /// Creates an external hyperlink and its main-document relationship.
    /// </summary>
    /// <param name="document">The document that owns the relationship.</param>
    /// <param name="target">The external target URI.</param>
    /// <param name="displayText">The displayed link text.</param>
    /// <returns>The created hyperlink.</returns>
    DocxHyperlink CreateExternalHyperlink(DocxDocument document, string target, string displayText);

    /// <summary>
    /// Creates a hyperlink to a bookmark anchor in the same document.
    /// </summary>
    /// <param name="anchor">The bookmark name.</param>
    /// <param name="displayText">The displayed link text.</param>
    /// <returns>The created hyperlink.</returns>
    DocxHyperlink CreateInternalHyperlink(string anchor, string displayText);

    /// <summary>
    /// Appends a hyperlink to a paragraph.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="hyperlink">The hyperlink to append.</param>
    void AppendHyperlink(DocxParagraph paragraph, DocxHyperlink hyperlink);

    /// <summary>
    /// Resolves a hyperlink through the main-document relationship set.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="hyperlink">The hyperlink to resolve.</param>
    /// <returns>True when a hyperlink relationship was resolved.</returns>
    bool ResolveMainDocumentHyperlink(DocxDocument document, DocxHyperlink hyperlink);

    /// <summary>
    /// Returns the resolved hyperlink target.
    /// </summary>
    /// <param name="hyperlink">The hyperlink to inspect.</param>
    /// <returns>The target, or an empty string for an unresolved link.</returns>
    string GetHyperlinkTarget(DocxHyperlink hyperlink);

    /// <summary>
    /// Returns the internal bookmark anchor.
    /// </summary>
    /// <param name="hyperlink">The hyperlink to inspect.</param>
    /// <returns>The anchor, or an empty string.</returns>
    string GetHyperlinkAnchor(DocxHyperlink hyperlink);

    /// <summary>
    /// Returns the hyperlink relationship identifier.
    /// </summary>
    /// <param name="hyperlink">The hyperlink to inspect.</param>
    /// <returns>The relationship identifier, or an empty string.</returns>
    string GetHyperlinkRelationshipId(DocxHyperlink hyperlink);

    /// <summary>
    /// Returns whether the resolved hyperlink relationship is external.
    /// </summary>
    /// <param name="hyperlink">The hyperlink to inspect.</param>
    /// <returns>True when the resolved target is external.</returns>
    bool IsExternalHyperlink(DocxHyperlink hyperlink);

    /// <summary>
    /// Sets the hyperlink tooltip.
    /// </summary>
    /// <param name="hyperlink">The hyperlink to edit.</param>
    /// <param name="tooltip">The tooltip text, or an empty string to remove it.</param>
    void SetHyperlinkTooltip(DocxHyperlink hyperlink, string tooltip);

    /// <summary>
    /// Returns the hyperlink tooltip.
    /// </summary>
    /// <param name="hyperlink">The hyperlink to inspect.</param>
    /// <returns>The tooltip text, or an empty string.</returns>
    string GetHyperlinkTooltip(DocxHyperlink hyperlink);

    /// <summary>
    /// Returns all bookmark start markers in a paragraph.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <returns>A new list containing the start markers.</returns>
    List<DocxBookmark> GetBookmarkStarts(DocxParagraph paragraph);

    /// <summary>
    /// Finds a bookmark start by name in a paragraph.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <param name="name">The bookmark name.</param>
    /// <returns>The matching start marker, or null.</returns>
    DocxBookmark? FindBookmarkStart(DocxParagraph paragraph, string name);

    /// <summary>
    /// Finds a bookmark end by identifier in a paragraph.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <param name="id">The bookmark identifier.</param>
    /// <returns>The matching end marker, or null.</returns>
    DocxBookmark? FindBookmarkEnd(DocxParagraph paragraph, string id);

    /// <summary>
    /// Finds a bookmark start by name in direct main-body paragraphs.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="name">The bookmark name.</param>
    /// <returns>The matching start marker, or null.</returns>
    DocxBookmark? FindDocumentBookmarkStart(DocxDocument document, string name);

    /// <summary>
    /// Inserts a bookmark start marker at a paragraph child position.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="index">The child insertion position.</param>
    /// <param name="id">The bookmark identifier.</param>
    /// <param name="name">The bookmark name.</param>
    /// <returns>The inserted marker.</returns>
    DocxBookmark InsertBookmarkStart(DocxParagraph paragraph, int index, string id, string name);

    /// <summary>
    /// Inserts a bookmark end marker at a paragraph child position.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="index">The child insertion position.</param>
    /// <param name="id">The bookmark identifier.</param>
    /// <returns>The inserted marker.</returns>
    DocxBookmark InsertBookmarkEnd(DocxParagraph paragraph, int index, string id);

    /// <summary>
    /// Inserts a uniquely identified bookmark around a paragraph child range.
    /// </summary>
    /// <param name="document">The document used to allocate the identifier.</param>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="startIndex">The first child in the range.</param>
    /// <param name="endIndex">The exclusive end child position.</param>
    /// <param name="name">The bookmark name.</param>
    /// <returns>The allocated bookmark identifier.</returns>
    string InsertBookmarkRange(DocxDocument document, DocxParagraph paragraph, int startIndex, int endIndex, string name);

    /// <summary>
    /// Creates a simple field with an instruction and displayed result.
    /// </summary>
    /// <param name="instruction">The field instruction.</param>
    /// <param name="resultText">The displayed result.</param>
    /// <returns>The created field.</returns>
    DocxField CreateField(string instruction, string resultText);

    /// <summary>
    /// Creates a cross-reference field to a bookmark.
    /// </summary>
    /// <param name="bookmarkName">The referenced bookmark name.</param>
    /// <param name="resultText">The displayed result.</param>
    /// <returns>The created field.</returns>
    DocxField CreateCrossReferenceField(string bookmarkName, string resultText);

    /// <summary>
    /// Creates a current-page-number field.
    /// </summary>
    /// <param name="resultText">The displayed result.</param>
    /// <returns>The created field.</returns>
    DocxField CreatePageField(string resultText);

    /// <summary>
    /// Creates a date field with an optional Word date format.
    /// </summary>
    /// <param name="format">The Word date format, or an empty string.</param>
    /// <param name="resultText">The displayed result.</param>
    /// <returns>The created field.</returns>
    DocxField CreateDateField(string format, string resultText);

    /// <summary>
    /// Creates an author field.
    /// </summary>
    /// <param name="resultText">The displayed result.</param>
    /// <returns>The created field.</returns>
    DocxField CreateAuthorField(string resultText);

    /// <summary>
    /// Creates a title field.
    /// </summary>
    /// <param name="resultText">The displayed result.</param>
    /// <returns>The created field.</returns>
    DocxField CreateTitleField(string resultText);

    /// <summary>
    /// Creates a table-of-contents field using Word's common switches.
    /// </summary>
    /// <param name="resultText">The displayed result.</param>
    /// <returns>The created field.</returns>
    DocxField CreateTableOfContentsField(string resultText);

    /// <summary>
    /// Appends a simple field to a paragraph.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="field">The field to append.</param>
    void AppendField(DocxParagraph paragraph, DocxField field);

    /// <summary>
    /// Returns a simple field instruction without changing its switches.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    /// <returns>The field instruction.</returns>
    string GetFieldInstruction(DocxField field);

    /// <summary>
    /// Returns the displayed simple field result.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    /// <returns>The displayed result.</returns>
    string GetFieldResult(DocxField field);

    /// <summary>
    /// Replaces a simple field instruction.
    /// </summary>
    /// <param name="field">The field to edit.</param>
    /// <param name="instruction">The replacement instruction.</param>
    void SetFieldInstruction(DocxField field, string instruction);

    /// <summary>
    /// Replaces a simple field result with one text run.
    /// </summary>
    /// <param name="field">The field to edit.</param>
    /// <param name="resultText">The replacement result.</param>
    void SetFieldResult(DocxField field, string resultText);

    /// <summary>
    /// Marks a field for update when Word opens the document.
    /// </summary>
    /// <param name="field">The field to edit.</param>
    void MarkFieldForUpdate(DocxField field);

    /// <summary>
    /// Removes a field's update marker.
    /// </summary>
    /// <param name="field">The field to edit.</param>
    void ClearFieldUpdateMarker(DocxField field);

    /// <summary>
    /// Returns whether a field is marked for update.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    /// <returns>True when the field is marked dirty.</returns>
    bool IsFieldUpdateRequested(DocxField field);

    /// <summary>
    /// Sets whether a field is locked against updates.
    /// </summary>
    /// <param name="field">The field to edit.</param>
    /// <param name="locked">Whether the field is locked.</param>
    void SetFieldLocked(DocxField field, bool locked);

    /// <summary>
    /// Returns whether a field is locked against updates.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    /// <returns>True when the field is locked.</returns>
    bool IsFieldLocked(DocxField field);

    /// <summary>
    /// Returns whether a field is a cross-reference field.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    /// <returns>True for REF, PAGEREF, or NOTEREF instructions.</returns>
    bool IsCrossReferenceField(DocxField field);

    /// <summary>
    /// Returns whether a field is a current-page-number field.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    /// <returns>True for a PAGE instruction.</returns>
    bool IsPageField(DocxField field);

    /// <summary>
    /// Returns whether a field is a date-family field.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    /// <returns>True for a supported date instruction.</returns>
    bool IsDateField(DocxField field);

    /// <summary>
    /// Returns whether a field is an author field.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    /// <returns>True for an AUTHOR instruction.</returns>
    bool IsAuthorField(DocxField field);

    /// <summary>
    /// Returns whether a field is a title field.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    /// <returns>True for a TITLE instruction.</returns>
    bool IsTitleField(DocxField field);

    /// <summary>
    /// Returns whether a field is a table-of-contents field.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    /// <returns>True for a TOC instruction.</returns>
    bool IsTableOfContentsField(DocxField field);

    /// <summary>
    /// Returns whether a paragraph contains preserved complex-field elements.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <returns>True when complex-field markup is present.</returns>
    bool ContainsComplexField(DocxParagraph paragraph);

    /// <summary>
    /// Returns the number of preserved complex-field elements in a paragraph.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <returns>The complex-field element count.</returns>
    int ComplexFieldElementCount(DocxParagraph paragraph);

    /// <summary>
    /// Returns preserved complex-field instruction text in document order.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <returns>The combined instruction text.</returns>
    string GetComplexFieldInstruction(DocxParagraph paragraph);
}
