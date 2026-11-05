namespace odf;

/// <summary>
/// Identifies a structured text run or inline control.
/// </summary>
public enum OdfTextRunKind
{
    /// <summary>
    /// The inline content kind is unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Literal XML character data.
    /// </summary>
    Text = 1,
    /// <summary>
    /// A styled text span.
    /// </summary>
    Span = 2,
    /// <summary>
    /// A hyperlink.
    /// </summary>
    Hyperlink = 3,
    /// <summary>
    /// One or more explicit spaces.
    /// </summary>
    Space = 4,
    /// <summary>
    /// A tab control.
    /// </summary>
    Tab = 5,
    /// <summary>
    /// A line-break control.
    /// </summary>
    LineBreak = 6,
    /// <summary>
    /// A soft page-break hint.
    /// </summary>
    SoftPageBreak = 7,
    /// <summary>
    /// A point bookmark.
    /// </summary>
    Bookmark = 8,
    /// <summary>
    /// A bookmark range start.
    /// </summary>
    BookmarkStart = 9,
    /// <summary>
    /// A bookmark range end.
    /// </summary>
    BookmarkEnd = 10,
    /// <summary>
    /// A point reference mark.
    /// </summary>
    ReferenceMark = 11,
    /// <summary>
    /// A reference-mark range start.
    /// </summary>
    ReferenceMarkStart = 12,
    /// <summary>
    /// A reference-mark range end.
    /// </summary>
    ReferenceMarkEnd = 13,
    /// <summary>
    /// A point table-of-contents mark.
    /// </summary>
    TableOfContentMark = 14,
    /// <summary>
    /// A table-of-contents range start.
    /// </summary>
    TableOfContentMarkStart = 15,
    /// <summary>
    /// A table-of-contents range end.
    /// </summary>
    TableOfContentMarkEnd = 16,
    /// <summary>
    /// A point user-index mark.
    /// </summary>
    UserIndexMark = 17,
    /// <summary>
    /// A user-index range start.
    /// </summary>
    UserIndexMarkStart = 18,
    /// <summary>
    /// A user-index range end.
    /// </summary>
    UserIndexMarkEnd = 19,
    /// <summary>
    /// A point alphabetical-index mark.
    /// </summary>
    AlphabeticalIndexMark = 20,
    /// <summary>
    /// An alphabetical-index range start.
    /// </summary>
    AlphabeticalIndexMarkStart = 21,
    /// <summary>
    /// An alphabetical-index range end.
    /// </summary>
    AlphabeticalIndexMarkEnd = 22,
    /// <summary>
    /// A bibliography mark.
    /// </summary>
    BibliographyMark = 23,
    /// <summary>
    /// Ruby base text with an annotation.
    /// </summary>
    Ruby = 24
}
