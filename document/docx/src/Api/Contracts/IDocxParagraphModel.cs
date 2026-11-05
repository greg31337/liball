namespace docx;

/// <summary>
/// Provides ordered navigation and editing for direct paragraph children.
/// </summary>
public interface IDocxParagraphModel
{
    /// <summary>
    /// Returns whether direct paragraph formatting is present.
    /// </summary>
    /// <returns>True when direct formatting is present; otherwise false.</returns>
    bool HasFormat();

    /// <summary>
    /// Returns the direct paragraph format model.
    /// </summary>
    /// <returns>The paragraph format model.</returns>
    DocxParagraphFormat GetFormat();

    /// <summary>
    /// Replaces the direct paragraph format model.
    /// </summary>
    /// <param name="format">The replacement paragraph format.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>format</c> is null.</exception>
    void SetFormat(DocxParagraphFormat format);

    /// <summary>
    /// Returns all direct paragraph children in document order.
    /// </summary>
    /// <returns>A new list containing the paragraph children.</returns>
    List<DocxParagraphChild> GetChildren();

    /// <summary>
    /// Returns the number of direct paragraph children.
    /// </summary>
    /// <returns>The child count.</returns>
    int ChildCount();

    /// <summary>
    /// Returns a direct paragraph child by position.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <returns>The requested child.</returns>
    DocxParagraphChild GetChild(int index);

    /// <summary>
    /// Returns all direct runs in paragraph order.
    /// </summary>
    /// <returns>A new list containing the runs.</returns>
    List<DocxRun> GetRuns();

    /// <summary>
    /// Returns the number of direct runs.
    /// </summary>
    /// <returns>The run count.</returns>
    int RunCount();

    /// <summary>
    /// Returns a direct run by its position among runs.
    /// </summary>
    /// <param name="index">The zero-based run position.</param>
    /// <returns>The requested run.</returns>
    DocxRun GetRun(int index);

    /// <summary>
    /// Returns all direct hyperlinks in paragraph order.
    /// </summary>
    /// <returns>A new list containing the hyperlinks.</returns>
    List<DocxHyperlink> GetHyperlinks();

    /// <summary>
    /// Returns the number of direct hyperlinks.
    /// </summary>
    /// <returns>The hyperlink count.</returns>
    int HyperlinkCount();

    /// <summary>
    /// Returns a direct hyperlink by its position among hyperlinks.
    /// </summary>
    /// <param name="index">The zero-based hyperlink position.</param>
    /// <returns>The requested hyperlink.</returns>
    DocxHyperlink GetHyperlink(int index);

    /// <summary>
    /// Returns all direct bookmark markers in paragraph order.
    /// </summary>
    /// <returns>A new list containing the bookmark markers.</returns>
    List<DocxBookmark> GetBookmarks();

    /// <summary>
    /// Returns the number of direct bookmark markers.
    /// </summary>
    /// <returns>The bookmark count.</returns>
    int BookmarkCount();

    /// <summary>
    /// Returns a bookmark marker by position.
    /// </summary>
    /// <param name="index">The zero-based bookmark position.</param>
    /// <returns>The requested bookmark marker.</returns>
    DocxBookmark GetBookmark(int index);

    /// <summary>
    /// Returns all direct simple fields in paragraph order.
    /// </summary>
    /// <returns>A new list containing the fields.</returns>
    List<DocxField> GetFields();

    /// <summary>
    /// Returns the number of direct simple fields.
    /// </summary>
    /// <returns>The field count.</returns>
    int FieldCount();

    /// <summary>
    /// Returns a direct simple field by its position among fields.
    /// </summary>
    /// <param name="index">The zero-based field position.</param>
    /// <returns>The requested field.</returns>
    DocxField GetField(int index);

    /// <summary>
    /// Returns all direct content controls in paragraph order.
    /// </summary>
    /// <returns>A new list containing the content controls.</returns>
    List<DocxContentControl> GetContentControls();

    /// <summary>
    /// Returns the number of direct content controls.
    /// </summary>
    /// <returns>The content-control count.</returns>
    int ContentControlCount();

    /// <summary>
    /// Returns a direct content control by its position among content controls.
    /// </summary>
    /// <param name="index">The zero-based content-control position.</param>
    /// <returns>The requested content control.</returns>
    DocxContentControl GetContentControl(int index);

    /// <summary>
    /// Returns direct proofing-error markers in paragraph order.
    /// </summary>
    /// <returns>A new list containing the proofing markers.</returns>
    List<DocxParagraphChild> GetProofingMarkers();

    /// <summary>
    /// Returns the number of direct proofing-error markers.
    /// </summary>
    /// <returns>The proofing-marker count.</returns>
    int ProofingMarkerCount();

    /// <summary>
    /// Returns a direct proofing-error marker by position.
    /// </summary>
    /// <param name="index">The zero-based proofing-marker position.</param>
    /// <returns>The requested proofing marker.</returns>
    DocxParagraphChild GetProofingMarker(int index);

    /// <summary>
    /// Returns direct permission-range markers in paragraph order.
    /// </summary>
    /// <returns>A new list containing the permission-range markers.</returns>
    List<DocxParagraphChild> GetPermissionRanges();

    /// <summary>
    /// Returns the number of direct permission-range markers.
    /// </summary>
    /// <returns>The permission-range marker count.</returns>
    int PermissionRangeCount();

    /// <summary>
    /// Returns a direct permission-range marker by position.
    /// </summary>
    /// <param name="index">The zero-based permission-range marker position.</param>
    /// <returns>The requested permission-range marker.</returns>
    DocxParagraphChild GetPermissionRange(int index);

    /// <summary>
    /// Returns direct smart-tag children in paragraph order.
    /// </summary>
    /// <returns>A new list containing the smart-tag children.</returns>
    List<DocxParagraphChild> GetSmartTags();

    /// <summary>
    /// Returns the number of direct smart-tag children.
    /// </summary>
    /// <returns>The smart-tag count.</returns>
    int SmartTagCount();

    /// <summary>
    /// Returns a direct smart-tag child by position.
    /// </summary>
    /// <param name="index">The zero-based smart-tag position.</param>
    /// <returns>The requested smart-tag child.</returns>
    DocxParagraphChild GetSmartTag(int index);

    /// <summary>
    /// Returns direct custom XML markup children in paragraph order.
    /// </summary>
    /// <returns>A new list containing the custom XML children.</returns>
    List<DocxParagraphChild> GetCustomXmlMarkup();

    /// <summary>
    /// Returns the number of direct custom XML markup children.
    /// </summary>
    /// <returns>The custom XML markup count.</returns>
    int CustomXmlMarkupCount();

    /// <summary>
    /// Returns a direct custom XML markup child by position.
    /// </summary>
    /// <param name="index">The zero-based custom XML markup position.</param>
    /// <returns>The requested custom XML markup child.</returns>
    DocxParagraphChild GetCustomXmlMarkupChild(int index);

    /// <summary>
    /// Returns preserved direct children not represented by another child kind.
    /// </summary>
    /// <returns>A new list containing the preserved unknown children.</returns>
    List<DocxParagraphChild> GetPreservedUnknownChildren();

    /// <summary>
    /// Returns the number of preserved direct children not represented by another kind.
    /// </summary>
    /// <returns>The preserved unknown child count.</returns>
    int PreservedUnknownChildCount();

    /// <summary>
    /// Returns a preserved unknown child by position.
    /// </summary>
    /// <param name="index">The zero-based preserved unknown child position.</param>
    /// <returns>The requested preserved unknown child.</returns>
    DocxParagraphChild GetPreservedUnknownChild(int index);

    /// <summary>
    /// Appends a direct paragraph child.
    /// </summary>
    /// <param name="child">The child to append.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>child</c> is null.</exception>
    void AddParagraphChild(DocxParagraphChild child);

    /// <summary>
    /// Inserts a direct paragraph child.
    /// </summary>
    /// <param name="index">The zero-based insertion position.</param>
    /// <param name="child">The child to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>child</c> is null.</exception>
    void InsertParagraphChild(int index, DocxParagraphChild child);

    /// <summary>
    /// Inserts a run at a direct child position.
    /// </summary>
    /// <param name="index">The zero-based child insertion position.</param>
    /// <param name="run">The run to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>run</c> is null.</exception>
    void InsertRun(int index, DocxRun run);

    /// <summary>
    /// Inserts a hyperlink at a direct child position.
    /// </summary>
    /// <param name="index">The zero-based child insertion position.</param>
    /// <param name="hyperlink">The hyperlink to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>hyperlink</c> is null.</exception>
    void InsertHyperlink(int index, DocxHyperlink hyperlink);

    /// <summary>
    /// Inserts a bookmark marker at a direct child position.
    /// </summary>
    /// <param name="index">The zero-based child insertion position.</param>
    /// <param name="bookmark">The bookmark marker to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>bookmark</c> is null.</exception>
    void InsertBookmark(int index, DocxBookmark bookmark);

    /// <summary>
    /// Inserts a simple field at a direct child position.
    /// </summary>
    /// <param name="index">The zero-based child insertion position.</param>
    /// <param name="field">The field to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>field</c> is null.</exception>
    void InsertField(int index, DocxField field);

    /// <summary>
    /// Inserts a content control at a direct child position.
    /// </summary>
    /// <param name="index">The zero-based child insertion position.</param>
    /// <param name="contentControl">The content control to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>contentControl</c> is null.</exception>
    void InsertContentControl(int index, DocxContentControl contentControl);

    /// <summary>
    /// Replaces a direct paragraph child.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <param name="child">The replacement child.</param>
    /// <returns>The replaced child.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>child</c> is null.</exception>
    DocxParagraphChild ReplaceParagraphChild(int index, DocxParagraphChild child);

    /// <summary>
    /// Replaces a direct paragraph child with a run.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <param name="run">The replacement run.</param>
    /// <returns>The replaced child.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>run</c> is null.</exception>
    DocxParagraphChild ReplaceChildWithRun(int index, DocxRun run);

    /// <summary>
    /// Replaces a direct paragraph child with a hyperlink.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <param name="hyperlink">The replacement hyperlink.</param>
    /// <returns>The replaced child.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>hyperlink</c> is null.</exception>
    DocxParagraphChild ReplaceChildWithHyperlink(int index, DocxHyperlink hyperlink);

    /// <summary>
    /// Replaces a direct paragraph child with a bookmark marker.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <param name="bookmark">The replacement bookmark marker.</param>
    /// <returns>The replaced child.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>bookmark</c> is null.</exception>
    DocxParagraphChild ReplaceChildWithBookmark(int index, DocxBookmark bookmark);

    /// <summary>
    /// Replaces a direct paragraph child with a simple field.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <param name="field">The replacement field.</param>
    /// <returns>The replaced child.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>field</c> is null.</exception>
    DocxParagraphChild ReplaceChildWithField(int index, DocxField field);

    /// <summary>
    /// Replaces a direct paragraph child with a content control.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <param name="contentControl">The replacement content control.</param>
    /// <returns>The replaced child.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>contentControl</c> is null.</exception>
    DocxParagraphChild ReplaceChildWithContentControl(int index, DocxContentControl contentControl);

    /// <summary>
    /// Moves a direct paragraph child to another position.
    /// </summary>
    /// <param name="sourceIndex">The current zero-based child position.</param>
    /// <param name="destinationIndex">The resulting zero-based child position.</param>
    void MoveParagraphChild(int sourceIndex, int destinationIndex);

    /// <summary>
    /// Removes a direct paragraph child.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <returns>The removed child.</returns>
    DocxParagraphChild RemoveParagraphChildAt(int index);
}
